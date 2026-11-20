# 19: Idempotency middleware

Mode: you-implement-i-review
Gate: D
Layer: code
Blocked by: 18
Traces to: FR-37, FR-38
Concurrency problem: #5

## Vì sao có ticket này

Client không biết request của mình đã tới đâu. Mạng chập chờn, request timeout nhưng server
đã xử lý xong, người dùng bấm nút hai lần. Nếu server không có cơ chế idempotency, mỗi lần
retry là một đơn đặt vé mới — và khi mở bán phim hot, đó là **bán trùng ghế**.

Điểm khó nhất không phải là "lưu response rồi trả lại". Điểm khó nhất là **race khi hai
request cùng key đến đồng thời**: cả hai đều thấy "chưa có bản ghi" và cùng xử lý.

Cơ chế đã chốt ở T13: unique constraint ở DB là nguồn sự thật, `ON CONFLICT DO NOTHING`
rồi đọc lại.

## Bạn tạo ra

- Schema `ordering.idempotency_records` (theo T11).- **Cơ chế idempotency dùng chung** — một middleware hoặc service, áp dụng cho mọi endpoint
  mutating, không phải viết riêng cho từng cái.
- `request_hash` tính trên method + path + canonical body.
- Hành vi:
  - Key chưa từng thấy → xử lý, lưu response, trả.
  - Key đã thấy, hash **khác** → `422`.
  - Key đã thấy, hash **giống**, đã xử lý → trả lại response đã lưu.
  - Key đã thấy, hash giống, **đang xử lý** → `409` kèm `Retry-After`.
- Bản ghi hết hạn sau 24 giờ; có job dọn.
- Test.

## AI làm gì

AI **không viết code**. Khi review, AI hỏi sắm nhất:

- **"Hai request cùng key đến đồng thời — điều gì ngăn cả hai cùng xử lý?"** Đây là câu hỏi
  trung tâm. Nếu bạn đọc bản ghi rồi mới insert, đó là một race.
- "Canonical body" được tính thế nào? Hai request có **cùng ý nghĩa** nhưng khác thứ tự
  key trong JSON có ra cùng hash không? Nếu không, client gửi lại đúng request cũ có thể bị
  `422` oan.
- "Trạng thái `InProgress` được set khi nào, và ai set?" Nếu nó được set sau khi xử lý xong
  thì nó không bảo vệ được cái gì.
- "Job dọn bản ghi hết hạn chạy khi nào, và nó có xoá nhầm bản ghi đang xử lý không?"
- Test: có test nào **thật sự** đồng thời không, hay chỉ gọi tuần tự rồi khẳng định?

## Tiêu chí hoàn thành

- [ ] 50 request đồng thời cùng key → **đúng 1** đơn đặt vé được tạo, các request còn lại trả
      cùng response
- [ ] Cùng key với body khác → `422`
- [ ] Cùng key, request đang chạy → `409` kèm `Retry-After`
- [ ] Request lại sau khi xử lý xong → trả **giống hệt** response cũ, kể cả status code
- [ ] Cơ chế là **dùng chung**, áp dụng được cho endpoint khác không cần sửa code thêm
- [ ] `request_hash` ổn định với body có cùng nghĩa
- [ ] Bản ghi hết hạn và có job dọn
- [ ] Test chứng minh được: **bỏ unique constraint thì test phải fail**
