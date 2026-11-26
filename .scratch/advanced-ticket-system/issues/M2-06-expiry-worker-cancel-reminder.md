# M2-06: Worker hết hạn + huỷ đơn chủ động + email nhắc hạn

Mode: you-implement-i-review
Status: ready
Blocked by: M2-01, M2-05
Traces to: FR-04, FR-18, FR-19
Concurrency: #4
Branch: m2-06-expiry-worker-cancel-reminder

## Mục tiêu
Ghế không bị giữ mãi: đơn quá hạn tự đóng và trả ghế, người xem huỷ được đơn của mình, và được nhắc trước
khi hết hạn. Hết hạn và huỷ là hai đường vào cùng một thao tác "đóng đơn chờ" nên làm chung một chỗ.

## Phạm vi
**Trong:**
- Worker hết hạn (hosted service): lô `FOR UPDATE SKIP LOCKED` các đơn `AwaitingPayment` quá `expires_at`;
  với mỗi đơn, **cùng một transaction**: đơn → `Expired`, trả ghế theo `hold_id` (chỉ ghế còn `Held` của
  đúng đơn này), ghi `booking_history`, intent `Unpaid → Closed` + outbox huỷ PaymentIntent phía Stripe
  (nếu đã có intent), outbox `SeatStatesChanged` cho phần Inventory.
- `POST /api/bookings/{bookingId}/cancel`: cùng thao tác với lý do `Customer`; `200 { bookingId, status:
  "Cancelled" }`, ghế trả ngay; `404 BOOKING_NOT_FOUND` (đơn người khác cũng `404`),
  `409 BOOKING_ALREADY_CANCELLED`, `409 BOOKING_NOT_CANCELLABLE` (`Paid` / `Expired`).
- Handler outbox huỷ PaymentIntent phía Stripe (idempotent, kèm `Idempotency-Key`).
- Email nhắc (`FR-19`): lệnh giữ ghế ghi thêm outbox `ExpiryReminderDue` hẹn `expires_at − 5 phút` trong
  cùng transaction; handler **kiểm lại** đơn còn `AwaitingPayment` và chưa quá hạn trước khi gửi (`BR-24`),
  nội dung chứa đúng `expires_at` đang dùng. Cổng gửi mail là abstraction, SMTP cho dev (Mailpit trong
  Compose là câu hỏi mở Q31 — chốt trong PR).
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

**Ngoài:**
- Thanh toán đến muộn ⇒ hoàn tiền → M2-07. Hủy suất → M2-08.
- Cache sơ đồ ghế tiêu thụ `SeatStatesChanged` → M3-03 (ở đây chỉ ghi message).
- Lấy ghế quá hạn không chờ worker đã làm ở M2-01.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — "Bài toán #4" (cơ chế, bảng diễn biến, "Tiến trình chết giữa chừng"), "Thứ tự khoá toàn cục" (hết hạn / huỷ: đơn → ghế → intent).
- [`state-machines.md`](../../../docs/domain/state-machines.md) — `Booking`, `Payment intent`.
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Booking" (`cancel`).
- [`ADR-0006`](../../../docs/adr/0006-payment-expiry-refund-flow.md), [`ADR-0015`](../../../docs/adr/0015-take-over-expired-holds.md).
- [`open-questions.md`](../../../docs/learning/open-questions.md) — Q31; [`business rules`](../../../docs/prd/04-business-rules.md) — BR-24.

## Test
Gợi ý — người học viết (tên theo design).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `ExpiredHold_NewBuyerTakesSeat_WorkerDoesNotReleaseIt` | đơn X quá hạn, Y giữ cùng ghế thành công, rồi worker chạy ⇒ ghế vẫn `Held` với `hold_id = Y`; đơn X `Expired` | lệnh trả ghế không lọc `hold_id` |
| `ExpiryWorker_TwoInstances_ReleaseOnce` | 2 worker song song trên 200 đơn quá hạn ⇒ mỗi đơn đúng 1 dòng lịch sử `→ Expired`, mỗi ghế `Available`, `version` tăng đúng 1 | bỏ `SKIP LOCKED` và điều kiện `status = 'AwaitingPayment'` |
| `ExpiryWorker_CrashBetweenStatements_Recovers` | hook ném lỗi sau khi đổi trạng thái đơn, trước khi trả ghế ⇒ rollback cả hai; lượt sau trả đủ | đổi trạng thái và trả ghế ở hai transaction |
| `CancelBooking_ReleasesSeatsAndClosesIntent` | ghế `Available`, intent `Closed`, có outbox huỷ intent, lịch sử ghi nguyên nhân `Customer` | trả ghế theo `showtime_id` thay vì `hold_id` |
| `CancelBooking_Twice_409AlreadyCancelled` / `CancelBooking_Paid_409NotCancellable` / `CancelBooking_OtherCustomersBooking_404` | các mã lỗi theo api-contract | — |
| `ExpiryReminder_BookingAlreadyPaidOrCancelled_NoEmail` | `BR-24`: không gửi cho đơn đã xong | handler không kiểm lại trạng thái |
| `ExpiryReminder_Sent_ContainsExactExpiresAt` | mail chứa đúng `expires_at` của đơn | — |

## Câu hỏi review
- Vì sao "đổi trạng thái đơn" và "trả ghế" phải cùng transaction thay vì trả ghế qua outbox?
- Vì sao trả ghế lọc `hold_id`, và chuyện gì xảy ra nếu lọc theo danh sách ghế?
- Vì sao handler email phải kiểm lại trạng thái đơn khi message đến hạn?

## Done khi
- [ ] Test race đã thấy đỏ với các biến thể ghi ở cột "Đỏ nếu…"
- [ ] Lệnh trả ghế dùng chung cho worker và `cancel` (một chỗ viết, hai đường gọi)
- [ ] Q31 được chốt trong PR (và xoá khỏi `open-questions.md` ở bước đóng ticket)
