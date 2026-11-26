# M1-05: Thanh toán, webhook Stripe, cấp vé và E2E `J01_happy_path`

Mode: pair-tdd
Status: ready
Blocked by: M1-04
Traces to: FR-05, FR-16, FR-38
Concurrency: #7 (phần trùng event)
Branch: m1-05-payment-webhook-tickets

## Mục tiêu
Hoàn tất walking skeleton: giữ ghế → trả tiền qua Stripe test mode → webhook → đơn `Paid`, ghế `Sold`, mỗi ghế
một vé — **cùng một transaction** (ranh giới bị cắt #2, ADR-0009), nên "đơn `Paid` mà chưa có vé" không thể xảy ra.
Test E2E `J01_happy_path` là demo của milestone M1.

## Phạm vi
**Trong:**
- `POST /api/bookings/{bookingId}/payment` (chủ đơn): tạo `Payment intent` hoặc trả lại intent cũ (`UNIQUE booking_id`);
  `200 { intentId, clientSecret, amount, currency, status }`. Gọi Stripe tạo PaymentIntent **ngoài** transaction DB, kèm
  `Idempotency-Key` = id intent, `metadata.intent_id`. Lỗi: `BOOKING_EXPIRED`, `BOOKING_ALREADY_PAID`, `BOOKING_NOT_PAYABLE`,
  `502 PAYMENT_PROVIDER_UNAVAILABLE`.
- `POST /api/webhooks/stripe`: xác minh chữ ký `Stripe-Signature` (sai ⇒ `400`); `INSERT … ON CONFLICT DO NOTHING` vào
  `payment_events` (`UNIQUE stripe_event_id`; trùng ⇒ `200`); xử lý `payment_intent.succeeded`; loại sự kiện khác ⇒ `200` bỏ qua.
- Luồng thành công trong **một** transaction: event in-process `PaymentSucceeded` → `Booking` `AwaitingPayment → Paid` với
  `expires_at > now()` → `Inventory` ghế `Held → Sold` theo `hold_id` → `Payment` intent `Unpaid → Captured` → event
  `BookingPaid` → `Ticketing` chèn một vé mỗi ghế (`UNIQUE showtime_seat_id`) → `booking_history`. Số tiền sự kiện ≠ `amount` của intent ⇒ không cấp vé.
- `GET /api/bookings/{bookingId}` thêm `payment: { status, refundStatus }` để client poll thấy `Paid`; `GET /api/bookings/{bookingId}/tickets`
  (chủ đơn, khác ⇒ `404`) trả vé của đơn — **chưa** có trường `qr`.
- `StripeOptions` qua Options + `ValidateOnStart`; chỉ module `Payment` reference thư viện Stripe (ACL, R5). Test dùng Stripe test mode;
  chạy local dùng Stripe CLI để chuyển tiếp webhook tới `localhost`.
- Migration `payment` (`payment_intents`, `payment_events`) và `ticketing` (`tickets`).
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

**Ngoài:**
- Thanh toán đến muộn / đơn đã `Expired`/`Cancelled` ⇒ `RefundPending`, bảng `refunds`, gọi Stripe hoàn tiền, `charge.refunded`,
  `payment_failed` (ngoài việc ghi sự kiện), lịch sử đơn cho admin (M2-07). Ở ticket này nhánh "0 dòng" chỉ cần **không cấp vé**.
- Mã QR ký HMAC, trường `qr` trong danh sách vé (M2-09); check-in (M2-10).
- Outbox dùng chung (M2-05); worker hết hạn (M2-06); `payment.webhook_failures` (M2-07).
- Test race webhook cùng `event.id` chạy đồng thời ×3 và `NoDedup` đầy đủ của #7 (M2-11 kiểm toán; M2-07 hoàn tiền).

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — mục "Bài toán #7" (đoạn "Nhận").
- [`architecture.md`](../../../docs/design/architecture.md) — "D4" bảng dòng #2, "D9" (hướng `Booking → Payment.Contracts`, `Ticketing → Booking.Contracts`).
- [`api-contract.md`](../../../docs/design/api-contract.md) — "POST /api/bookings/{bookingId}/payment", "POST /api/webhooks/stripe", mục "Ticketing" (vé của đơn).
- [`data-model.md`](../../../docs/design/data-model.md) — `payment_intents`, `payment_events`, `ticketing.tickets`.
- [`cross-context-contracts.md`](../../../docs/domain/cross-context-contracts.md) — "R4", "R5", "R6"; [`state-machines.md`](../../../docs/domain/state-machines.md) — máy `Payment intent`.
- [ADR-0009](../../../docs/adr/0009-module-communication.md), [ADR-0010](../../../docs/adr/0010-payment-intent-states.md).

## Test
AI viết các test này trước. Dùng **sự kiện thật** của Stripe test mode, gửi vào webhook ký bằng webhook secret của ta; chạy khi có
`STRIPE_TEST_SECRET_KEY` (xem câu hỏi mở Q30 về CI).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `J01_happy_path` | Admin tạo phim → quản lý tạo phòng + suất + mở bán → khách đăng ký, xem sơ đồ, giữ 2 ghế, thanh toán (`pm_card_visa`), webhook ⇒ booking `Paid`, ghế `Sold`, **2 vé** | Bất kỳ mắt xích nào của chuỗi gãy |
| `Webhook_invalid_signature_returns_400` | `400`, DB không đổi | Bỏ xác minh chữ ký |
| `Webhook_same_event_twice_issues_tickets_once` | Gửi lại cùng `event.id` ⇒ lần hai `200`; 1 dòng `payment_events`, số vé = số ghế, 1 dòng lịch sử `→ Paid` | Bỏ `UNIQUE (stripe_event_id)` |
| `Webhook_failure_inside_transaction_rolls_back_everything` | Ép lỗi ở bước cấp vé ⇒ `500`; đơn vẫn `AwaitingPayment`, ghế vẫn `Held`, không có dòng `payment_events` | Mỗi module commit riêng |
| `Webhook_amount_mismatch_issues_no_tickets` | Số tiền lệch ⇒ không `Paid`, không vé | Không kiểm số tiền |
| `Payment_returns_same_intent_on_second_call` | Gọi hai lần ⇒ cùng `intentId`; một dòng `payment_intents` | Tạo hai intent |
| `Payment_after_expires_at_returns_409_booking_expired` | `409 BOOKING_EXPIRED` | Cho trả đơn quá hạn |
| `Payment_of_other_customer_returns_404` | `404 BOOKING_NOT_FOUND` | Lộ đơn của người khác |
| `Tickets_of_other_customer_returns_404` | `404` | Trả `403` hoặc vé |

## Câu hỏi review
1. Vì sao gọi Stripe tạo PaymentIntent nằm **ngoài** transaction DB, và webhook khớp intent bằng `metadata.intent_id` chứ không bằng `pi_…`?
2. Chống xử lý lặp ở đây gồm những lớp nào (event id, điều kiện trạng thái, unique vé)? Lớp nào là chốt chặn cuối?
3. Vì sao `Payment` không biết `Booking` là gì, còn `Booking` phát `BookingPaid` mà không biết `Ticketing`? Hướng `ProjectReference` ra sao?
4. Webhook trả `500` khi lỗi bất ngờ — vì sao đây là hành vi **đúng** với Stripe?

## Done khi
- [ ] `J01_happy_path` xanh với Stripe test mode; các test còn lại xanh
- [ ] Không secret Stripe trong diff; chỉ `Payment` reference thư viện Stripe (arch test quy tắc 7 xanh)
- [ ] Phép thử âm tính của `Webhook_same_event_twice_issues_tickets_once` (bỏ unique) đã chạy ra đỏ
