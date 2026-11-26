# M1-02: Scheduling ghi — phim, phòng, suất, giá, mở bán, sinh ghế, seed

Mode: you-implement-i-review
Status: ready
Blocked by: M1-01
Traces to: FR-20, FR-21, FR-22, FR-28, FR-32
Concurrency: —
Branch: m1-02-scheduling-write

## Mục tiêu
Có dữ liệu để bán: quản trị viên tạo phim, quản lý rạp tạo phòng + ghế + suất + giá rồi mở bán. Tạo suất sinh
luôn ghế của suất ở `Inventory` **trong cùng transaction** (ranh giới bị cắt #5) — lần đầu hai module chạm
nhau, bằng event bus in-process có sẵn từ M0-03. Kèm lệnh seed demo cho các ticket sau.

## Phạm vi
**Trong** (endpoint ở [`api-contract.md`](../../../docs/design/api-contract.md) mục "Scheduling"):
- Phim: `POST /api/admin/movies`, `PUT /api/admin/movies/{movieId}` (`Administrator`).
- Phòng: `POST /api/manager/rooms`, `GET /api/manager/rooms`, `PUT /api/manager/rooms/{roomId}/seats`.
- Suất: `POST /api/manager/showtimes`, `PUT` / `DELETE /api/manager/showtimes/{showtimeId}` (chỉ `NotOnSale`),
  `POST .../open-sale`. Giá phải đủ **mọi** loại ghế có trong phòng; chủ sở hữu = chủ phòng (FR-32 ⇒ không phải chủ thì `404`).
- Exclusion constraint chống chồng giờ kèm đệm 15 phút (BR-20) ⇒ `409 SHOWTIME_OVERLAP`.
- Tạo suất ⇒ `Inventory` sinh một `Showtime seat` (`Available`) cho mỗi `Room seat`; xoá suất `NotOnSale` ⇒ xoá ghế đã sinh.
  Aggregate `Showtime` raise domain event `ShowtimeCreated` / `ShowtimeDeleted` (định nghĩa ở `Scheduling.Contracts`,
  D4 #5); handler lấy event ra và publish **trước commit** qua `IEventPublisher` (M0-03); `Inventory` có
  `IEventHandler` sinh / xoá ghế trong cùng transaction. Ticket này **không** dựng bus.
- Migrations theo quy ước M0-04; migration đầu của schema `scheduling` khai báo `btree_gist` bằng cơ chế đã có ở
  M0-04 (Testcontainers không chạy init script) để exclusion constraint tạo được.
- Test đặt ở project test của module (tạo từ template M0-08), dùng `TicketSystem.Testing`.
- Lệnh seed dữ liệu demo: phim, phòng, suất qua cùng code path của use case (không INSERT tay), trong đó có
  **một suất nhỏ ~100 ghế** làm điểm tranh chấp cho test/demo M1-04.

**Ngoài:**
- Hủy suất `POST .../cancel` (M2-08); `GET` công khai và danh sách `GET /api/manager/showtimes` đọc (M1-03).
- Chặn / bỏ chặn ghế, availability (M1-03).
- Seed khối lượng lớn cho k6 (M3-02).

## Đọc trước
- [`data-model.md`](../../../docs/design/data-model.md) — mục "Schema scheduling" (đặc biệt `showtimes`: exclusion constraint,
  `rooms` / `room_seats`: sửa sơ đồ `INV-13`) và "Schema inventory" (`showtime_seats`).
- [`architecture.md`](../../../docs/design/architecture.md) — mục "D4" (bảng "Áp vào năm ranh giới bị cắt", dòng #5) và "D6".
- [`cross-context-contracts.md`](../../../docs/domain/cross-context-contracts.md) — mục "Hợp đồng: Scheduling → Inventory (R1)".
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B6 — Use case", "B7 — Ai mở và commit transaction", "B8 — Repository", "B9 — Domain primitives", "B13 — Domain event và bus in-process", "B14 — Persistence foundation", "Cấu trúc thư mục bên trong một module".
- [`state-machines.md`](../../../docs/domain/state-machines.md) — máy `Showtime`.

## Test
Gợi ý — người học viết (Postgres thật).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `CreateShowtime_overlapping_in_same_room_returns_409` | Suất thứ hai chồng, hoặc cách < 15 phút ⇒ `409 SHOWTIME_OVERLAP`; cách đủ 15 phút ⇒ `201` | Bỏ đệm 15 phút khỏi constraint |
| `CreateShowtime_concurrently_same_slot_one_wins` | N request tạo suất cùng khung giờ: đúng 1 `201`, còn lại `409` | Chỉ kiểm chồng ở code, không ở DB |
| `CreateShowtime_generates_one_seat_per_room_seat` | Số `inventory.showtime_seats` = số `room_seats`, tất cả `Available` | Quên sinh, hoặc sinh thiếu |
| `CreateShowtime_failure_leaves_no_seats` | Lỗi giữa chừng (vd thiếu giá) ⇒ không có suất lẫn ghế | Hai bước ở hai transaction |
| `CreateShowtime_missing_price_for_seat_type_returns_422` | Phòng có `VIP` mà thiếu giá `VIP` ⇒ `422 MISSING_PRICE` | Không kiểm đủ loại |
| `Showtime_of_other_owner_returns_404` | Quản lý B sửa/xoá/mở bán suất của A ⇒ `404` | Trả `403` hoặc cho sửa |
| `EditShowtime_after_open_sale_returns_409` | `409 SHOWTIME_NOT_EDITABLE` | Cho sửa suất đã mở bán |
| `ReplaceRoomSeats_with_active_showtime_returns_409` | `409 ROOM_HAS_ACTIVE_SHOWTIMES` | Không khoá dòng phòng |
| `Seed_creates_demo_data_with_hundred_seat_showtime` | Chạy seed trên DB trống ⇒ có suất ~100 ghế `OnSale` | Seed đi đường tắt, bỏ qua sinh ghế |

## Câu hỏi review
1. Vì sao exclusion constraint ở DB chứ không kiểm trong code (BR-20)? Cảnh nào code đơn thuần thất bại?
2. `Scheduling` là upstream của `Inventory` và không được reference `Inventory.Contracts` — vì sao sinh ghế đi bằng event, không gọi interface?
3. Giá nằm ở (suất, loại ghế), không ở phòng — nếu để ở phòng thì đổi giá gây hậu quả gì?

## Done khi
- [ ] Test trên xanh; migration chạy từ DB trống trên Testcontainers (có `btree_gist` khai báo trong migration theo cơ chế M0-04)
- [ ] Event `ShowtimeCreated` chạy **trước commit**; exception ở `Inventory` ⇒ rollback cả suất
- [ ] Lệnh seed chạy được hai lần liên tiếp mà không nhân đôi dữ liệu, có hướng dẫn
