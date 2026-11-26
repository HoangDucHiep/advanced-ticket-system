# M2-01: Thứ tự khoá chống deadlock, quota 10 ghế/người/suất, lấy ghế Held quá hạn

Mode: pair-tdd
Status: ready
Blocked by: M1-04
Traces to: FR-03, FR-09
Concurrency: #3 #13
Branch: m2-01-lock-order-quota-takeover

## Mục tiêu
Làm cứng lệnh giữ ghế của M1-04 trước ba tình huống mà M1-04 cố tình chưa xử lý: hai người tranh ghế
chồng nhau theo thứ tự ngược (không được kẹt), một người gửi nhiều yêu cầu song song để vượt quota, và
ghế `Held` đã quá hạn mà worker chưa dọn (người khác phải giữ được ngay).

## Phạm vi
**Trong:**
- Khoá ghế tường minh theo `id` tăng dần trong lệnh giữ ghế (bước (4) của luồng giữ ghế), thay vì
  phụ thuộc thứ tự quét của một câu `UPDATE … = ANY(...)`.
- Khoá quota bằng advisory lock theo transaction cho cặp (người xem, suất) **trước** khoá ghế, rồi đếm
  `SUM(seat_weight)`; vượt 10 ⇒ `422 CUSTOMER_SEAT_LIMIT` kèm `limit`, `currentWeight`. Chọn advisory
  lock thay vì bảng quota: ADR-0007.
- Điều kiện giữ ghế lấy được ghế `Held` có `held_until <= now()` (ADR-0015); ghế đó hiển thị là
  `Available` ở các đường đọc của M1-03 (sơ đồ ghế, availability) nếu chưa làm.
- Hook test chèn `pg_sleep` giữa hai lần khoá ghế và cờ `Concurrency:UnsafeVariant` cho hai biến thể
  `ClientOrderLocking`, `NoQuotaLock` (chỉ có trong build test).

**Ngoài:**
- Dedupe `Idempotency-Key` và savepoint giữ kết quả lỗi → M2-02.
- Worker hết hạn, huỷ đơn, trả ghế theo `hold_id` → M2-06 (ở đây chỉ chứng minh lấy ghế quá hạn không
  cần worker).
- Bộ khung `UnsafeVariant` thống nhất cho mọi bài toán → M2-11 (ở đây chỉ cần đủ cho hai biến thể trên).

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — mục "Thứ tự khoá toàn cục", "Bài toán #2" (luồng
  giữ ghế đầy đủ), "Bài toán #3", "Bài toán #13", "Hạ tầng test concurrency".
- [`ADR-0007`](../../../docs/adr/0007-per-customer-showtime-quota.md),
  [`ADR-0015`](../../../docs/adr/0015-take-over-expired-holds.md).
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Booking" (bảng lỗi của giữ ghế).
- [`business rules`](../../../docs/prd/04-business-rules.md) — BR-08, BR-22.

## Test
AI viết các test này trước (đỏ trên code M1-04).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `HoldSeats_ReverseOrderContention_NoDeadlock` | 200 vòng, X gửi `[S1,S2]`, Y gửi `[S2,S1]` qua `Barrier(2)`: 0 lỗi `40P01`; mỗi vòng đúng 1 người `201`, người kia `409 SEATS_UNAVAILABLE`; mỗi response < 2s | khoá theo thứ tự client gửi (`ClientOrderLocking`) |
| `HoldSeats_20ParallelSameCustomer_TotalAtMostTen` | `Barrier(20)`, mỗi yêu cầu 2 ghế khác nhau: tổng `SUM(seat_weight)` = 10 (đúng 5 thắng), 15 yêu cầu `422 CUSTOMER_SEAT_LIMIT` | bỏ advisory lock (`NoQuotaLock`) |
| `HoldSeats_CoupleSeatsCountDouble_QuotaUsesWeight` | ghế đôi tính 2 vào quota | đếm số dòng thay vì `seat_weight` |
| `HoldSeats_ExpiredHeldSeat_NewBuyerTakesItWithoutWorker` | ghế `Held` có `held_until` đã qua: người khác giữ được (`201`), `hold_id` = đơn mới | điều kiện giữ ghế chỉ nhận `Available` |
| `HoldSeats_ExpiredHoldOfSameCustomer_NotCountedInQuota` | đơn `AwaitingPayment` quá `expires_at` không tính vào quota | đếm cả đơn quá hạn |

Mỗi biến thể `UnsafeVariant` phải được chạy và thấy đỏ; ghi kết quả vào PR.

## Câu hỏi review
- Vì sao `ORDER BY id FOR UPDATE` đúng còn `UPDATE … WHERE id = ANY(...)` chỉ "tình cờ" đúng?
- Vì sao khoá quota phải đứng **trước** khoá ghế trong thứ tự khoá toàn cục?
- Hai cặp (người, suất) khác nhau trùng băm thì chuyện gì xảy ra, có sai không?

## Done khi
- [ ] Cả 5 test xanh; hai biến thể `UnsafeVariant` đã thấy đỏ
- [ ] Không còn đường nào khoá ghế theo thứ tự client gửi
- [ ] `CUSTOMER_SEAT_LIMIT` có `limit` và `currentWeight` đúng như `api-contract.md`
