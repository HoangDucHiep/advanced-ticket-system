# 17: Phòng chiếu, sơ đồ ghế, lịch chiếu và cache

Mode: you-implement-i-review
Gate: D
Layer: code
Blocked by: 15
Traces to: FR-11, FR-12, FR-13, FR-20, FR-21, FR-22, FR-23, FR-28, FR-32, FR-34
Concurrency problem: #9 #10

> **Đồng bộ với design (2026-11-25, Gate C đóng).** Khi ticket này mâu thuẫn với `docs/design/`,
> **design thắng**. Bảng thuộc **hai** module: `scheduling.movies|rooms|room_seats|showtimes|ticket_prices` và
> `inventory.showtime_seats` (sinh khi **tạo** suất, không khi mở bán). Endpoint: `api-contract.md`
> mục Scheduling + Inventory — `GET /api/showtimes/{id}/seats` thuộc module `Inventory`; bốn con số ở
> `GET /api/manager/showtimes/{id}/availability`. Cache: `concurrency.md` #9, #10, ADR-0012 (TTL không
> phải cơ chế độ mới). Exclusion constraint + đệm 15 phút: `data-model.md`.

## Vì sao có ticket này

Đây là phần **đọc nhiều, ghi ít** của hệ thống, và là nơi cache thể hiện giá trị. Nó cũng là
hai bài toán concurrency: **#9 cache stampede** và **#10 invalidate cache theo từng ghế**.

Bài toán stampede tinh vi hơn nó trông. Với dữ liệu tĩnh, 100 request cùng đọc 1 row thì
database không chết. Nhưng ở đây dữ liệu trạng thái ghế **thay đổi liên tục**, cache bị
invalidate thường xuyên, và số request cùng lúc trượt cache là số request tới đúng cái suất
chiếu đang tranh chấp.

Bài toán #10 mới hơn và cũng khó hơn: khi **4 ghế** được đặt trong một suất, instance khác
phải thấy **đúng 4 ghế** đó đã bán — không phải "cache bị xoá toàn bộ" (tốn), cũng không
phải "cache vẫn còn giá trị cũ" (sai).

## Bạn tạo ra

- Schema (theo T11):
  - `scheduling.rooms`, `scheduling.room_seats` — phòng và ghế **của phòng** (loại ghế là cột
    `seat_type`, không có bảng loại ghế riêng). Tĩnh, ghi hiếm.
  - `scheduling.movies`, `scheduling.ticket_prices` (giá theo suất × loại ghế).
  - `scheduling.showtimes` — suất chiếu: phim, phòng, giờ bắt đầu, giờ kết thúc, trạng thái
    (chưa mở bán / đang bán / đã hủy), giá theo loại ghế.
  - `inventory.showtime_seats` — **trạng thái ghế của suất chiếu**: ghế nào còn trống, đang
    được giữ, đã bán, bị chặn. Đây là bảng **ghi nóng** — tần suất ghi cao nhất hệ thống.
- CRUD cho quản lý rạp: tạo phòng, khai báo sơ đồ ghế, tạo suất chiếu, đặt giá, mở bán.
- Read API cho người xem:
  - `GET /api/movies` — phim đang chiếu.
  - `GET /api/movies/{id}/showtimes` — lịch chiếu kèm giá.
  - `GET /api/showtimes/{id}/seats` — **sơ đồ ghế**: vị trí, loại, giá, trạng thái.
  - `GET /api/showtimes/{id}/availability` — số ghế còn trống / đang giữ / đã bán.
- **Cache-aside** bằng Redis:
  - Sơ đồ ghế của suất: TTL ngắn (vài giây), vì nó đổi theo từng đơn.
  - Chi tiết phim / lịch chiếu: TTL dài hơn.
- **Single-flight**: khi nhiều request cùng trượt cache trên một key, chỉ **một** request
  đọc database; các request còn lại chờ rồi dùng kết quả.
- **Invalidate theo từng ghế**: khi một ghế đổi trạng thái, cập nhật cache của đúng suất chiếu
  đó theo hướng **ghi đè trạng thái ghế**, thay vì xoá cả sơ đồ. Đọc cache kèm phiên bản.
- Lệnh seed tạo dữ liệu demo: vài rạp, vài phòng, sơ đồ ghế thực tế, và **một suất chiếu
  nhỏ** (ví dụ 100 ghế) để có điểm tranh chấp cho mở bán phim hot.
- Test.

## AI làm gì

AI **không viết code**. Khi review, AI tập trung vào:

- **Single-flight đúng chưa**: nó có phải "một request đọc DB" thật, hay chỉ "giảm tải"? Hỏi:
  "Nếu 100 request cùng trượt cache, chắc chắn chỉ có 1 lần đọc DB — bằng cơ chế nào? Nếu
  process chết giữa chừng thì sao? Nếu Redis chết thì sao?"
- **TTL của sơ đồ ghế có phù hợp không** — và đây là câu hỏi về *tính đúng đắn*, không
  phải về hiệu năng: nếu TTL dài, người xem có thể thấy ghế còn trống rồi thử đặt ghế đã có
  người.
- **Invalidate theo từng ghế**: hỏi "đặt 4 ghế, instance khác đọc cache sẽ thấy gì? Nếu bạn
  xoá cả cache của suất thì đúng, nhưng tốn — và nếu bạn **không** xoá thì sai. Cơ chế của
  bạn xử lý cả hai trường hợp chưa?"
- **Phân biệt "ghế đang được giữ chỗ" với "ghế đã bán"** trong response sơ đồ ghế: người
  xem cần biết khác nhau (thời gian hết hạn khác nhau), và cache cũng phải phân biệt.
- **Đọc bằng Dapper, ghi bằng EF** — có đúng quy tắc "một hot path chỉ qua một tầng" không?
- **Test**: test có đếm được số lần đọc DB không? Test có chứng minh cache thực sự được dùng,
  không chỉ là "code có gọi Redis"?

## Tiêu chí hoàn thành

- [ ] Lệnh seed tạo được dữ liệu demo gồm sơ đồ ghế và suất chiếu nhỏ
- [ ] 100 request đồng thời cho một suất nóng → **đúng 1** lần đọc database (đo bằng bộ đếm
      trong test)
- [ ] Test chứng minh sơ đồ ghế được cache, không chỉ "có gọi Redis"
- [ ] **Đặt 4 ghế → instance khác đọc cache thấy đúng 4 ghế đã bán** (bài toán #10)
- [ ] Sơ đồ ghế phân biệt được ghế "đang giữ chỗ" và ghế "đã bán"
- [ ] Integration test chạy trên Redis thật (Testcontainers), không dùng in-memory fake
- [ ] Mỗi hot path chỉ đi qua một tầng truy cập dữ liệu
- [ ] Đã nối điểm invalidation (chưa hoạt động hoàn chỉnh được cho tới T18 — nhưng chỗ
      gọi invalidation phải tồn tại)
