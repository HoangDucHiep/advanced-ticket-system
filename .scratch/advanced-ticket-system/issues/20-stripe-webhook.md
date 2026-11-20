# 20: Stripe PaymentIntent và webhook

Mode: pair-tdd
Gate: D
Layer: code
Blocked by: 18, 19
Traces to: FR-05, FR-06, FR-16, FR-24, FR-31, FR-38
Concurrency problem: #7

## Vì sao có ticket này

Webhook Stripe là ví dụ kinh điển của "sự kiện đến lặp, đến trễ, đến sai thứ tự". Stripe**không** đảm bảo webhook đến đúng thứ tự, và có thể gửi lại cùng một event. Nếu bạn xử
lý webhook không idempotent, bạn sẽ cấp vé hai lần cho một lần thanh toán — hoặc hơn.

Với mô hình seat-based, vé được cấp **theo từng ghế**: một đơn 4 ghế sinh ra 4 vé, mỗi vé
một mã QR riêng ghi rõ ghế nào. Nên "cấp đúng một bộ vé" có nghĩa là **đúng số ghế, mỗi ghế
một vé, không ghế nào bị cấp hai lần** — và điều đó phải đúng cả khi webhook đến lặp.

Đây cũng là nơi **outbox pattern** xuất hiện tự nhiên: ghi thông báo xác nhận trong cùng
transaction với việc chuyển trạng thái đơn. Nếu gửi thông báo ngoài transaction, hoặc là
thông báo gửi cho đơn chưa thanh toán, hoặc là đơn thanh toán rồi mà thông báo mất.

## Bạn tạo ra

- Schema `payment.payments`, `payment.outbox_messages`.
- `POST /api/bookings/{id}/payment-intent` (chủ sở hữu):
  - Tạo Stripe PaymentIntent cho đúng tổng tiền của đơn (tổng giá của từng ghế).
  - **Nếu đã có PaymentIntent thì dùng lại** — gọi lại không được tạo charge thứ hai.
  - Trả client secret.
- `POST /api/webhooks/stripe`:
  - Xác thực chữ ký Stripe. Sai → `400`.
  - **Idempotent theo `event.id`**.
  - Khi `payment_intent.succeeded`: chuyển đơn `Pending` → `Paid`, có bảo vệ theo trạng
    thái hiện tại.
  - Chuyển trạng thái ghế từ `Held` sang `Sold` (có điều kiện).
  - Cấp vé: **một vé cho mỗi ghế** trong đơn, mỗi vé có mã QR riêng ghi rõ ghế.
  - Ghi outbox message cho thông báo xác nhận.
- Thanh toán đến **sau khi** chỗ giữ ghế đã hết hạn → đơn **không** được hồi sinh.
- Test.

## AI làm gì

AI viết **test đỏ** theo thứ tự:

1. Webhook thành công → đơn `Paid`, ghế chuyển `Held` → `Sold`, **cấp đúng số vé bằng số
   ghế**, mỗi vé ghi đúng ghế.
2. Cùng event gửi 3 lần → **đúng 1** lần transition, **đúng 1 bộ vé**.
3. Webhook đến sau khi chỗ giữ ghế hết hạn → đơn vẫn `Expired`, **không** cấp vé.
4. Chữ ký sai → `400`.
5. Gọi `payment-intent` hai lần → chỉ một PaymentIntent.

AI dừng lại sau mỗi test đỏ.

Khi review code của bạn, AI hỏi:

- "Transition đơn có được bảo vệ bằng điều kiện trạng thái không? Nếu không, webhook trễ có
  hồi sinh đơn không?"
- "Cập nhật `Held → Sold` có điều kiện trạng thái ghế không? Nếu không, chuyện gì xảy ra khi
  worker expire đã trả ghế trước?"
- "Cấp vé có **gắn với từng ghế** không, hay chỉ cấp số lượng vé chung? Nếu chỉ cấp số lượng,
  bạn sẽ mất thông tin 'vé này của ghế nào' — và check-in sẽ không biết người đó ngồi đâu."
- "Outbox message có được ghi **trong cùng transaction** với transition không?"
- "Có event nào của Stripe mà bạn **không xử lý**, và điều đó ổn không?"
- "Test trùng event có thật sự gửi lại **cùng một `event.id`** không?"

## Tiêu chí hoàn thành

- [ ] Thanh toán thành công → đơn `Paid`, ghế `Sold`, cấp **đúng số vé bằng số ghế**
- [ ] Mỗi vé ghi rõ **ghế nào**, và dùng check-in được
- [ ] Cùng event gửi 3 lần → **đúng 1** transition, **1** bộ vé
- [ ] Webhook đến sau khi chỗ giữ ghế hết hạn → đơn **vẫn** `Expired`, không cấp vé
- [ ] Chữ ký sai → `400`
- [ ] Gọi `payment-intent` hai lần → chỉ một PaymentIntent
- [ ] Outbox message ghi trong cùng transaction với transition
- [ ] Chuyển `Held → Sold` có điều kiện, và có test chứng minh
- [ ] Test trùng event dùng cùng một `event.id`
