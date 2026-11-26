# M2-07: Thanh toán đến muộn → hoàn tiền qua outbox, lịch sử đơn

Mode: pair-tdd
Status: ready
Blocked by: M2-06
Traces to: FR-06, FR-31, FR-39
Concurrency: #4 #7
Branch: m2-07-late-payment-refund-history

## Mục tiêu
Tiền đã thu thì không bao giờ mất mà không có vé: thanh toán đến sau hạn (hoặc khi đơn đã đóng) được hoàn
đúng một lần, kể cả khi worker chết giữa chừng. Quản trị viên xem được dòng thời gian của một đơn.

## Phạm vi
**Trong:**
- Webhook `payment_intent.succeeded` cho đơn không còn nhận thanh toán (`expires_at <= now()`, `Expired`,
  `Cancelled`): đơn **không** `Paid`, không vé; intent `Unpaid`/`Closed → RefundPending`; `INSERT payment.refunds`
  (`UNIQUE intent_id`); outbox `RefundRequested`. Số tiền sự kiện ≠ `amount` ⇒ không cấp vé, `RefundPending` + log
  `Critical`.
- Handler outbox hoàn tiền: `POST /v1/refunds` với `Idempotency-Key = refunds.id`; lưu `stripe_refund_id`.
- `charge.refunded`: `RefundPending → Refunded`. `payment_failed` đến sau `succeeded` ⇒ bỏ qua.
- Lỗi không mong đợi khi xử lý webhook ⇒ rollback cả dòng sự kiện, `500`, ghi `payment.webhook_failures`
  ở transaction riêng; `attempts >= 5` ⇒ log `Critical` + metric `payment_webhook_poison_total`.
- `GET /api/admin/bookings/{bookingId}/history`: ghép `booking_history` và `payment_events` thành timeline.

**Ngoài:**
- Hủy suất yêu cầu hoàn đơn đã trả → M2-08 (dùng lại cơ chế hoàn tiền ở đây).
- Dispatcher / bảng outbox → M2-05; huỷ đơn / hết hạn / email → M2-06.
- Alert cho metric poison → M3-06.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — "Bài toán #7" (nhận, gửi hoàn tiền), "Bài toán #4" (webhook sau `expires_at`).
- [`state-machines.md`](../../../docs/domain/state-machines.md) — `Payment intent`, `Booking`.
- [`data-model.md`](../../../docs/design/data-model.md) — `payment.payment_events`, `payment.refunds`, `payment.webhook_failures`.
- [`api-contract.md`](../../../docs/design/api-contract.md) — "Payment" (webhook), `GET /api/admin/bookings/{bookingId}/history`.
- [`ADR-0006`](../../../docs/adr/0006-payment-expiry-refund-flow.md), [`ADR-0010`](../../../docs/adr/0010-payment-intent-states.md).

## Test
AI viết các test này trước. Test Stripe chạy khi có `STRIPE_TEST_SECRET_KEY` (xem Q30).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `PaymentSucceeded_JustAfterExpiry_NoTicketsAndRefund` | đơn `expires_at = now() − 1ms`, worker chưa chạy: đơn không `Paid`, 0 vé, intent `RefundPending`, 1 dòng outbox `RefundRequested` | bỏ `expires_at > now()` khỏi lệnh `→ Paid` |
| `PaymentSucceeded_OnClosedIntent_RefundPending` | intent `Closed` nhận thành công muộn ⇒ `RefundPending`, không vé | không xử lý nhánh `Closed` |
| `RefundWorker_KilledAfterStripeCall_RefundsOnce` | dừng worker ngay sau lời gọi Stripe, trước `processed_at`; chạy lại ⇒ `GET /v1/refunds?payment_intent=…` có **1** refund | bỏ `Idempotency-Key` |
| `Refund_AmountMismatch_NoTicketsRefundPending` | số tiền lệch ⇒ không vé, `RefundPending` | cấp vé theo sự kiện |
| `Webhook_FailedAfterSucceeded_Ignored` | đơn vẫn `Paid`, intent vẫn `Captured` | xử lý `failed` đè trạng thái |
| `ChargeRefunded_MovesRefundPendingToRefunded` | intent `Refunded`; sự kiện lặp bỏ qua | — |
| `Webhook_UnexpectedError_RollsBackEventAndRecordsFailure` | `500`, không còn dòng `payment_events`, có `webhook_failures` với `attempts` tăng | ghi failure trong transaction chính (bị rollback theo) |
| `AdminBookingHistory_MergesBookingAndPaymentTimeline` | timeline đúng thứ tự, sự kiện trùng hiện một lần; vai trò khác `Administrator` ⇒ `403` | — |

## Câu hỏi review
- Vì sao `UPDATE … expires_at > now()` là nơi quyết định "thắng", không phải worker?
- Vì sao hoàn tiền gọi Stripe ngoài transaction và `Idempotency-Key = refunds.id` giải quyết gì mà
  `UNIQUE (intent_id)` không giải quyết được?
- Vì sao `webhook_failures` ghi ở transaction riêng, trái với "một transaction mỗi request"?

## Done khi
- [ ] Test `…JustAfterExpiry…` và `…KilledAfterStripeCall…` đã thấy đỏ bằng phép thử âm tính
- [ ] Sự kiện thật từ Stripe test mode được dùng (không giả lập payload)
- [ ] Timeline không lộ dữ liệu thanh toán nhạy cảm ngoài `payload` đã có ở `payment_events`
