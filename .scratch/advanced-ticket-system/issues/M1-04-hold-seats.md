# M1-04: Giữ n ghế all-or-nothing

Mode: pair-tdd
Status: ready
Blocked by: M1-03
Traces to: FR-01, FR-02, FR-10, FR-14, FR-15, FR-41
Concurrency: #1 #2
Branch: m1-04-hold-seats

## Mục tiêu
Lệnh cốt lõi của hệ thống: người xem giữ n ghế trong 15 phút, **đủ n hoặc không ghế nào**, và không bao giờ hai
người cùng có một ghế — chứng minh bằng test chồng lấn thật. Đúng cơ chế khoá ngay từ M1 (khoá `showtime` bằng
`FOR KEY SHARE` → khoá ghế theo `ORDER BY id FOR UPDATE` → `UPDATE` có điều kiện trạng thái); M2 chỉ làm cứng các nhánh còn lại.

## Phạm vi
**Trong:**
- `POST /api/showtimes/{showtimeId}/bookings` (`Customer`) — luồng bước (1), (2), (4), (5a), (5b) của
  [`concurrency.md`](../../../docs/design/concurrency.md) mục "Bài toán #2": suất `OnSale` chưa bắt đầu; kiểm id ghế thuộc suất; tổng
  trọng số ≤ 6 (ghế đôi = 2); khoá ghế theo thứ tự `id`; hoặc `409 SEATS_UNAVAILABLE` + `unavailableSeatIds`,
  hoặc tạo `Booking` `AwaitingPayment` (+ `booking_lines` với giá chốt từ server, `booking_history`), ghế → `Held`.
- Mã lỗi: `SHOWTIME_NOT_FOUND` (chưa mở bán, FR-34), `SHOWTIME_STARTED` (FR-41), `INVALID_SEATS`, `BOOKING_TOO_LARGE`,
  `SEATS_UNAVAILABLE`. Thời gian giữ 15 phút là giá trị cấu hình (Options), `expires_at` tính **một lần** từ đồng hồ DB.
- Header `Idempotency-Key`: **bắt buộc có** (thiếu ⇒ `400 IDEMPOTENCY_KEY_REQUIRED`; định dạng 1–64 ký tự
  `[A-Za-z0-9_-]`) nhưng **chưa** khử trùng.
- `GET /api/bookings` (đơn của người gọi, mới nhất trước) và `GET /api/bookings/{bookingId}`: đơn của người khác ⇒
  `404 BOOKING_NOT_FOUND` (FR-10).
- `Booking` gọi `Inventory` qua interface ở `Inventory.Contracts` (R3), SQL tay nằm trong `Inventory`; `Booking` đọc
  trạng thái + giá suất qua `Scheduling.Contracts` (R9). Migration của `booking` cho `bookings`, `booking_lines`, `booking_history`.
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

**Ngoài:**
- Khử trùng theo `Idempotency-Key`, bảng `idempotency_records`, savepoint, `422 IDEMPOTENCY_KEY_REUSED` (M2-02, ADR-0011).
- Quota 10 ghế/người/suất + advisory lock (`CUSTOMER_SEAT_LIMIT`), test deadlock #3 ngược thứ tự, lấy ghế `Held` quá hạn khi giữ (M2-01).
- Hủy đơn (M2-06); `SHOWTIME_CANCELLED` (suất chưa thể bị hủy trước M2-08).
- Rate limit `hold`, admission control (M3-02, M3-04); `ExpiryReminderDue` + outbox (M2-05); phát `SeatStatesChanged` cho cache (M3-03).

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — "Phần 1" (đoạn "Thứ tự khoá toàn cục", "Hạ tầng test concurrency"), "Bài toán #1", "Bài toán #2".
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Booking" (`POST …/bookings`, `GET /api/bookings*`), "Không có partial success", đầu mục "Idempotency contract".
- [`architecture.md`](../../../docs/design/architecture.md) — mục "Đường đi của một request: giữ ghế", "D4" dòng #1, "D7".
- [`data-model.md`](../../../docs/design/data-model.md) — `inventory.showtime_seats`, `booking.bookings`, `booking_lines`, `booking_history`.
- [`cross-context-contracts.md`](../../../docs/domain/cross-context-contracts.md) — "R3".

## Test
AI viết các test này trước. Hạ tầng: Postgres thật, `Barrier(N)` để chồng lấn thật, vòng lặp ≥ 50, biến thể `Concurrency:UnsafeVariant` (chỉ build test).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `HoldSeat_100ConcurrentRequestsSameSeat_ExactlyOneWins` | Đúng 1 `201`, 99 `409 SEATS_UNAVAILABLE`; DB: ghế có đúng một `hold_id`, đúng 1 đơn `AwaitingPayment` chứa ghế đó | Bỏ điều kiện `status` trong `UPDATE` (`UnsafeVariant=ReadThenWrite`) ⇒ > 1 `201` |
| `HoldSeats_OneSeatAlreadyTaken_NoSeatIsHeld` | X giữ C; Y giữ [A,B,C,D] ⇒ `409`, `unavailableSeatIds=[C]`; **đọc DB**: A, B, D còn `Available`, `hold_id IS NULL`; Y không có đơn | Giữ từng ghế ở transaction riêng (`UnsafeVariant=PerSeatTransaction`) |
| `HoldSeats_OverlappingSets_NoPartialBookings` (20 người × 4 ghế chồng nhau, 50 vòng; tên do AI đặt vì design chưa đặt) | Mỗi ghế thuộc ≤ 1 đơn; mỗi đơn có đúng số ghế đã yêu cầu | `UnsafeVariant=PerSeatTransaction` ⇒ ghế mồ côi hoặc đơn thiếu ghế |
| `HoldSeats_missing_idempotency_key_returns_400` | `400 IDEMPOTENCY_KEY_REQUIRED` | Không kiểm header |
| `HoldSeats_over_six_weight_returns_422` | 7 ghế, hoặc 4 ghế đôi ⇒ `422 BOOKING_TOO_LARGE` kèm `maxSeats`, `requestedWeight`; không ghế bị đổi | Đếm ghế đôi = 1 |
| `HoldSeats_seat_of_other_showtime_returns_422` | `422 INVALID_SEATS` | Không kiểm ghế thuộc suất |
| `HoldSeats_not_on_sale_or_started_returns_404_or_409` | `NotOnSale` ⇒ `404 SHOWTIME_NOT_FOUND`; suất đã bắt đầu ⇒ `409 SHOWTIME_STARTED` | Không kiểm trạng thái/giờ |
| `Booking_price_comes_from_server` | `lines[].price` = giá của suất theo loại ghế; client không gửi được giá | Tin dữ liệu client |
| `GetBooking_of_other_customer_returns_404` | `404 BOOKING_NOT_FOUND`, không phải `403` | Lộ việc đơn tồn tại |

## Câu hỏi review
1. All-or-nothing đến từ đâu — aggregate, hay một transaction? Vì sao bước (4) quyết định **trước** khi ghi?
2. Vì sao dùng `SELECT … ORDER BY id FOR UPDATE` rồi mới `UPDATE`, thay vì một câu `UPDATE … WHERE id = ANY(…)` so số dòng?
3. `FOR KEY SHARE` trên dòng suất để chặn điều gì (gợi ý: M2-08)? Vì sao không `FOR UPDATE` cả suất?
4. Giữ 3/4 ghế bị coi là lỗi `409`, không phải thành công một phần — client cần gì từ `unavailableSeatIds`?

## Done khi
- [ ] Các test trên xanh; hai phép thử âm tính (`ReadThenWrite`, `PerSeatTransaction`) đã chạy và cho **đỏ** (ghi trong PR)
- [ ] Mọi câu SQL tay có comment vì sao ORM không biểu đạt được + tham chiếu `concurrency.md #1`/`#2` (C-20)
- [ ] `Booking` không đọc/ghi bảng của `inventory` hay `scheduling` trực tiếp
