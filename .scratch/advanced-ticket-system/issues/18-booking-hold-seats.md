# 18: Đặt vé — giữ n ghế all-or-nothing

Mode: pair-tdd
Gate: D
Layer: code
Blocked by: 15, 17
Traces to: FR-01, FR-02, FR-03, FR-04, FR-09, FR-10, FR-14, FR-15, FR-18
Concurrency problem: #1 #2 #3

> **Đồng bộ với design (2026-11-25, Gate C đóng).** Khi ticket này mâu thuẫn với `docs/design/`,
> **design thắng**. Luồng SQL đầy đủ: `concurrency.md` #2 (khoá suất `FOR KEY SHARE` → khoá quota advisory →
> `SELECT … ORDER BY id FOR UPDATE` → `UPDATE` có điều kiện). Bảng: `booking.bookings`,
> `booking.booking_lines`, `booking.booking_history`. Trạng thái đơn mới là `AwaitingPayment` (không phải
> `Pending`); giữ **15 phút**. Endpoint: `POST /api/showtimes/{id}/bookings` (Idempotency-Key bắt buộc),
> `GET /api/bookings`, `GET /api/bookings/{id}`, `POST /api/bookings/{id}/cancel`. Ghế `Held` quá hạn
> được lấy lại ngay (ADR-0015). Bài toán thêm: #13 (quota), #14 (đua với hủy suất).

## Vì sao có ticket này

Đây là **bài toán trung tâm** của cả dự án. Nếu dự án chỉ có một ticket đáng để khoe, đây là
ticket đó.

Với mô hình seat-based, ticket này chứa **hai** bài toán khó nhất mà mô hình đếm số không có:

- **#1** — không ai được bán trùng một ghế.
- **#2** — đặt 4 ghế phải giữ được đủ 4, hoặc **không giữ ghế nào**.
- **#3** — hai người tranh ghế chồng nhau theo thứ tự ngược nhau không được kẹt.

Cơ chế đã chốt ở T12: một câu `UPDATE` có điều kiện, lấy quyết định từ **số dòng bị ảnh
hưởng**. Không đọc rồi mới ghi. Không khoá rồi mới kiểm tra. Và ghế được khoá theo **thứ tự
cố định** để không kẹt.

Ticket này ở mode `pair-tdd`: **AI viết test đỏ, bạn viết code cho xanh.**

Vì sao AI viết test? Vì test concurrency khó viết đúng lần đầu — cần chờ đồng bộ, cần
đảm bảo request thật sự chồng lấn. Đó là kỹ năng riêng, và bạn sẽ cần nó cho T22 và T23.
Học nó ở đây, trên bài toán nhỏ nhất.

## Bạn tạo ra

- Schema `booking.bookings`, `booking.booking_lines`, `booking.booking_history` (`data-model.md`).
- `POST /api/bookings`:
  1. Xác thực người gọi và dữ liệu đầu vào (suất chiếu, danh sách ghế).
  2. Kiểm tra idempotency (T19 sẽ bổ sung cơ chế đầy đủ; ở ticket này cơ chế có thể tối
     giản, nhưng phải tính tới).
  3. Trong **một** transaction: giữ n ghế, tạo đơn đặt vé.
  4. Trả `201` kèm thời điểm hết hạn của chỗ giữ ghế.
- **Cơ chế giữ ghế (bài toán #2)**:
  - Khoá các ghế tường minh `SELECT … ORDER BY id FOR UPDATE`, rồi một câu `UPDATE` có điều kiện
    trên các dòng đã khoá (`concurrency.md` #2, ADR-0002).
  - Có ghế không giữ được → **không ghi gì**, trả `409` kèm danh sách ghế đã có người.
  - **Thứ tự khoá cố định** theo `showtime_seats.id` tăng dần, để tránh deadlock (#3).
- Kiểm tra giới hạn mua vé mỗi người trên 1 suất chiếu (FR-09) — tách khỏi cơ chế trên.
- Chỗ giữ ghế có thời hạn (**15 phút** — `BR-03`), hiển thị để client đếm ngược.
- `GET /api/bookings/{id}` (chủ sở hữu), `GET /api/bookings`.
- `POST /api/bookings/{id}/cancel` — hủy đơn đang chờ và trả lại ghế.
- Test.

## AI làm ghi

AI viết **test đỏ**, một test mỗi lần:

- Test đầu tiên: đặt 1 ghế thành công khi còn trống. Đơn giản, làm đường chạy.
- Test thứ hai: đặt ghế đã có người → `409`.
- Test thứ ba: **N request tranh cùng 1 ghế** → đúng 1 thành công, còn lại `409`.
- Test thứ tư: **đặt 4 ghế trong đó 1 ghế đã có người** → `409`, và **cả 4 ghế** đều còn
  trống. Đây là test quan trọng nhất của ticket — và là test chứng minh all-or-nothing.
- Test thứ năm: **2 người chọn ghế chồng nhau theo thứ tự ngược nhau** → cả hai đều nhận
  được phản hồi trong thời gian giới hạn, không treo.
- Test thứ sáu: cùng idempotency key → cùng kết quả.
- Test thứ bảy: chỗ giữ ghế hết hạn → đơn không còn chiếm ghế.

Với test concurrency, AI dùng `Barrier` hoặc cơ chế tương đương để các request **thật sự
chồng lấn** — không gọi song song rồi hy vọng.

AI dừng lại sau mỗi test đỏ và chờ bạn viết code.

Với mỗi test đỏ, AI giải thích ngắn: test này đang kiểm chứng invariant nào, và **nó sẽ
fail với cách làm sai nào**.

## Tiêu chí hoàn thành

- [ ] Đặt 1 ghế khi còn trống → `201`, đơn `AwaitingPayment`, `expiresAt` = lúc giữ + 15 phút
- [ ] Đặt ghế đã có người → `409`
- [ ] **N request đồng thời tranh 1 ghế → đúng 1 thành công, còn lại `409`**
- [ ] **Đặt 4 ghế mà 1 ghế đã có người → `409` và cả 4 ghế còn trống** (all-or-nothing)
- [ ] **2 người chọn ghế ngược thứ tự, 200 vòng → 0 lỗi `40P01`**, mỗi bên `201` hoặc `409` (không deadlock)
- [ ] Không có đường nào đọc trạng thái ghế rồi mới ghi **mà không khoá**: đọc trạng thái chỉ qua
      `SELECT … FOR UPDATE`, và `UPDATE` vẫn mang điều kiện trạng thái
- [ ] Ghế được khoá theo thứ tự cố định — và có test chứng minh (không chỉ chú thích code)
- [ ] Giới hạn 10 ghế/người/suất: 20 yêu cầu song song của cùng người → tổng ≤ 10 (#13, đỏ khi bỏ khoá)
- [ ] Chỗ giữ ghế hết hạn → đơn không còn chiếm ghế
- [ ] `GET /api/bookings/{id}` chỉ trả về đơn của chính người gọi
- [ ] Test concurrency **thật sự** chồng lấn, và **fail** nếu thay bằng cách đọc-rồi-ghi
