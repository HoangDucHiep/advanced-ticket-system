# M2-08: Hủy suất

Mode: pair-tdd
Status: ready
Blocked by: M2-07
Traces to: FR-24
Concurrency: #14
Branch: m2-08-cancel-showtime

## Mục tiêu
Quản lý rạp hủy được suất đang bán mà không để sót đơn `AwaitingPayment` sống trên suất đã hủy và không để
ai trả tiền mà không có vé lẫn không được hoàn. Giữ ghế đua với hủy suất là chỗ dễ sai nhất của ticket này.

## Phạm vi
**Trong:**
- `POST /api/manager/showtimes/{showtimeId}/cancel` (chủ suất / `Administrator`; không phải chủ ⇒ `404`):
  `200 { id, status: "Cancelled", cancelledBookings, refundsRequested }`; gọi lại khi đã hủy ⇒ `200` cùng
  thân; `409 SHOWTIME_NOT_ON_SALE` khi `NotOnSale`.
- Một transaction theo đúng thứ tự: khoá dòng suất `FOR UPDATE` → suất `Cancelled` → đơn `AwaitingPayment`
  của suất → `Cancelled` (lý do `ShowtimeCancelled`) + trả ghế theo `hold_id` + intent `Closed` + outbox huỷ
  intent → **sau đó** mới chọn đơn `Paid` để intent `Captured → RefundPending` + `refunds` + outbox
  `RefundRequested` (dùng lại cơ chế hoàn tiền của M2-07).
- Lệnh giữ ghế (M1-04) đã khoá suất `FOR KEY SHARE`; ticket này chứng minh bằng test và thêm biến thể
  `NoShowtimeLock` cho test. Lệnh giữ đến sau hủy ⇒ `409 SHOWTIME_CANCELLED`.
- Vé của suất đã hủy hiển thị `showtime.status = "Cancelled"` (M2-09 đọc), người xem thấy
  `payment.refundStatus` ở `GET /api/bookings/{id}`.

**Ngoài:**
- Hoàn tiền thực tế gọi Stripe và `Refunded` → M2-07 (handler đã có).
- Chặn quét vé của suất hủy → M2-10 (`SHOWTIME_CANCELLED` ở check-in).
- Sửa / xoá suất `NotOnSale`, mở bán → M1-02.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — "Bài toán #14", "Thứ tự khoá toàn cục" (hủy suất: 1 `FOR UPDATE` → 3 → 4 → 5).
- [`state-machines.md`](../../../docs/domain/state-machines.md) — `Showtime` (`OnSale → Cancelled`), `Payment intent`.
- [`api-contract.md`](../../../docs/design/api-contract.md) — Scheduling (`cancel`), Booking (`GET /api/bookings/{id}`).
- [`architecture.md`](../../../docs/design/architecture.md) — "D4" (event in-tx `ShowtimeCancelled`).
- [`ADR-0006`](../../../docs/adr/0006-payment-expiry-refund-flow.md).

## Test
AI viết các test này trước.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `CancelShowtime_ConcurrentHolds_NoBookingSurvives` | `Barrier`: 20 lệnh giữ ghế + 1 lệnh hủy, hook làm chậm lệnh giữ sau khoá suất ⇒ 0 đơn `AwaitingPayment` của suất; mọi ghế `Available`; mỗi lệnh giữ là `201` (rồi bị hủy theo suất) hoặc `409 SHOWTIME_CANCELLED` | bỏ `FOR KEY SHARE` (`NoShowtimeLock`) |
| `CancelShowtime_PaidBooking_RefundPendingAndOutbox` | đơn `Paid` ⇒ intent `RefundPending`, 1 `refunds`, 1 outbox `RefundRequested`; vé còn nhưng suất `Cancelled` | không tạo refund hoặc ghi sai thứ tự |
| `CancelShowtime_ConcurrentWithPaymentWebhook_NeverTicketWithoutRefund` | webhook `→ Paid` đua với hủy: hoặc đơn bị hủy (webhook hoàn tiền) hoặc đơn `Paid` được hoàn; không nhánh nào vừa `Paid` vừa không hoàn | chọn đơn `Paid` **trước** khi huỷ đơn chờ (cùng snapshot) |
| `CancelShowtime_Twice_200SameBody` | gọi lại không đổi gì thêm, không refund thứ hai | — |
| `CancelShowtime_NotOwner_404` / `CancelShowtime_NotOnSale_409` | theo api-contract | — |
| `CancelShowtime_AwaitingBookings_SeatsReleasedByHoldId` | ghế trả đúng, intent `Unpaid` ⇒ `Closed` + outbox huỷ intent | — |

## Câu hỏi review
- Vì sao `FOR KEY SHARE` cho giữ ghế mà `FOR UPDATE` cho hủy suất, và vì sao không dùng `FOR SHARE`?
- Vì sao bước chọn đơn `Paid` phải là **lệnh sau** việc huỷ đơn chờ?
- Hủy suất có nên khoá toàn bộ ghế của suất không? Vì sao thứ tự khoá toàn cục vẫn không bị phá?

## Done khi
- [ ] `CancelShowtime_ConcurrentHolds_NoBookingSurvives` đã thấy đỏ với `NoShowtimeLock`
- [ ] Thứ tự khoá của hủy suất khớp bảng "Thứ tự khoá toàn cục"
- [ ] `cancelledBookings` / `refundsRequested` đếm đúng trong test
