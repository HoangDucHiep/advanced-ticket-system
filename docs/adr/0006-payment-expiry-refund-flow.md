---
id: 0006
title: Luồng tiền — hết hạn theo thời gian, thất bại không kết thúc intent, hoàn tiền qua outbox
status: accepted
date: 2026-11-25
Related: ADR-0005
---

# Luồng tiền — hết hạn theo thời gian, thất bại không kết thúc intent, hoàn tiền qua outbox

## Context

Review trước khi đóng Gate B (2026-11-25) phát hiện bốn lỗ hổng trong luồng tiền ở
`docs/domain/state-machines.md` và `docs/domain/cross-context-contracts.md`:

1. **Hoàn tiền được mô tả như nằm trong DB transaction.** Máy `Payment intent` ghi "đơn đã
   `Expired` ⇒ chuyển thẳng sang `Refunded`", và hủy suất chiếu ghi "trong modular monolith, tất
   cả nằm trong **một** transaction". Hoàn tiền là **lời gọi API Stripe** — bên ngoài, không
   rollback được — nên không thể nằm trong DB transaction. Ràng buộc "≤ 1 `Refund` mỗi intent"
   không chặn được việc **gọi Stripe hai lần** khi worker timeout rồi retry.
2. **Thanh toán thất bại "bỏ" intent** — nhưng J-04.4 yêu cầu người xem **thử lại** được trong
   thời gian giữ, và ràng buộc duy nhất trên `booking_id` không cho tạo intent thứ hai. Ở Stripe,
   một PaymentIntent thất bại quay về `requires_payment_method` và **dùng lại được**.
3. **Hết hạn được định nghĩa bằng worker, không bằng thời gian.** Race "ai chuyển trạng thái trước
   thì thắng" cho phép webhook đến lúc 15:30 (đã quá hạn) mà worker chưa quét vẫn đưa đơn sang
   `Paid` — trái với `BR-04` ("quá thời điểm hết hạn là hết hạn").
4. **Hủy suất chiếu không nói gì về các đơn đang `AwaitingPayment`** — webhook thành công đến sau
   có thể cấp vé cho một suất đã hủy.

## Decision

### 1. Hết hạn theo **thời gian**, worker chỉ dọn dẹp

- Transition `Booking: AwaitingPayment → Paid` có điều kiện lệnh **`expires_at > now()`** (thời
  điểm xử lý thông báo). Quá hạn thì transition này không thể thắng, bất kể worker đã quét hay
  chưa.
- Worker giải phóng hết hạn vẫn chạy để đưa đơn sang `Expired` và trả ghế, nhưng **không còn là
  thứ quyết định** đơn có hết hạn hay không.
- Thông báo thành công đến khi đơn **đã quá hạn** (dù còn `AwaitingPayment` hay đã `Expired`)
  ⇒ đi nhánh "không cấp vé, hoàn tiền".

### 2. Thanh toán thất bại **không** kết thúc intent

- Kết quả thất bại chỉ được **ghi vào lịch sử `Payment event`**; `Payment intent` vẫn ở
  `AwaitingPayment`. Người xem thử lại bằng **cùng** intent (đúng mô hình Stripe).
- Intent chỉ ngừng hiệu lực khi đơn kết thúc mà không trả tiền (`Expired` / `Cancelled`) — khi đó
  `Payment` yêu cầu **huỷ intent phía Stripe** (best-effort, qua outbox) để giảm khả năng tiền bị
  trừ muộn.

### 3. Hoàn tiền qua **outbox**, có trạng thái trung gian

- Máy `Payment intent` có thêm trạng thái **`RefundPending`**:
  `AwaitingPayment → Captured → RefundPending → Refunded`, và nhánh tiền đến muộn
  `AwaitingPayment → RefundPending` (thu tiền nhưng không cấp vé).
- Transaction nghiệp vụ chỉ **ghi yêu cầu hoàn** (chuyển sang `RefundPending` + ghi một dòng
  outbox). Một worker đọc outbox và gọi Stripe với **`Idempotency-Key` = định danh của yêu cầu
  hoàn** — gọi lại bao nhiêu lần thì Stripe cũng chỉ hoàn một lần.
- Webhook `charge.refunded` (hoặc kết quả đồng bộ của lời gọi hoàn) đưa intent sang `Refunded`.
- "Hoàn đúng một lần" được bảo vệ ở **hai** tầng: ràng buộc duy nhất ≤ 1 yêu cầu hoàn mỗi intent
  (phía ta) + `Idempotency-Key` (phía Stripe).

### 4. Hủy suất chiếu = một transaction ngắn + N yêu cầu hoàn bất đồng bộ

Trong **một** transaction:

- `Showtime: OnSale → Cancelled`;
- mọi đơn `AwaitingPayment` của suất → `Cancelled` (nguyên nhân "hủy suất chiếu" ghi trong lịch sử
  đơn), ghế của chúng trả về `Available`;
- với mỗi đơn `Paid` của suất: intent `Captured → RefundPending` + một dòng outbox.

Sau đó worker xử lý **N** yêu cầu hoàn, mỗi cái độc lập và idempotent. Nếu hệ thống dừng giữa
chừng, các dòng outbox còn lại được xử lý tiếp — không có trạng thái "hoàn được một nửa" ở phía
nghiệp vụ. Trả lời Q29.

Thông báo thành công đến **sau** khi đơn đã bị hủy theo suất ⇒ nhánh "không cấp vé, hoàn tiền"
như mọi đơn `Cancelled`.

## Considered Options

| Phương án | Ưu | Nhược |
|---|---|---|
| **Outbox + `RefundPending` + `Idempotency-Key`** (chọn) | Không gọi ra ngoài trong DB transaction; retry an toàn; dừng giữa chừng không mất việc | Thêm một trạng thái, thêm worker |
| Gọi Stripe hoàn tiền ngay trong transaction | Đơn giản | Stripe hoàn rồi mà DB rollback ⇒ mất dấu; DB commit rồi mà Stripe lỗi ⇒ không hoàn. Không chấp nhận được |
| Stripe `capture_method=manual` (authorize trước, chỉ capture khi đơn còn hạn) | Gần như xoá nhánh "đã trừ tiền rồi mới hoàn" — chỉ cần huỷ authorization | Không loại được nhánh hủy suất chiếu (vẫn phải hoàn tiền thật); làm mất bài toán hoàn tiền idempotent mà dự án muốn học. **Ghi lại làm phương án nâng cấp**, không chọn |
| Hết hạn theo worker (giữ như cũ) | Không cần đọc thời gian trong điều kiện | Trái `BR-04`; kết quả phụ thuộc lịch chạy worker |
| Tạo intent mới mỗi lần thử lại | Đơn giản về trạng thái | Phá ràng buộc 1 đơn ↔ 1 intent; dễ trừ tiền hai lần cho một đơn |

## Consequences

- `Payment intent` có bốn trạng thái: `AwaitingPayment`, `Captured`, `RefundPending`, `Refunded`.
  **Sửa bởi ADR-0010 (2026-11-25):** `AwaitingPayment` của intent đổi tên thành `Unpaid`, thêm
  `Closed` cho intent của đơn kết thúc khi chưa thu tiền.
- Ranh giới bị cắt #4 (hủy suất chiếu) không còn là "một transaction cho mọi thứ" mà là "một
  transaction + outbox". Chỉ phần ghi **trong database** là atomic; phần gọi Stripe là bất đồng bộ
  và idempotent.
- Cần một bảng outbox và một worker — dùng chung với việc huỷ intent phía Stripe và (sau này)
  email nhắc hết hạn (`FR-19`).
- Một khoảng thời gian tiền đã thu nhưng chưa hoàn (`RefundPending`) là **chủ ý** và quan sát được
  — `FR-31` hiển thị được.
- Test bắt buộc (T22): thông báo thành công đến đúng sau `expires_at` ⇒ không cấp vé; worker hoàn
  tiền bị kill giữa chừng rồi chạy lại ⇒ Stripe chỉ hoàn một lần.
