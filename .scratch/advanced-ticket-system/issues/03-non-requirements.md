# 03: Non-requirements

Mode: co-design
Gate: A
Layer: prd
Blocked by: 01
Traces to: —

## Vì sao có ticket này

Mỗi tính năng thêm vào đều tốn thời gian. Trong một project 12 tuần, thời gian còn hạn chế là
tài nguyên khan hiếm nhất. Thứ quyết định dự án có kịp hay không là những gì bạn **không**
làm.

Non-requirements là hợp đồng với chính mình: những thứ hấp dẫn mà ta chủ động bỏ qua. Không có
nó, bạn sẽ bị kéo đi bởi mọi ý tưởng hay — và đó là cách phổ biến nhất để một project học
tập chết yênu.

Đây cũng là tài liệu bạn sẽ trả lời khi interviewer hỏi "còn gì nữa không?" — và câu trả lời
"có, tôi đã chủ động loại bỏ X vì lý do Y" mạnh hơn nhiều so với "chưa làm tới".

## Bạn tạo ra

`docs/prd/02-non-requirements.md` gồm danh sách những gì hệ thống **cố tình không làm**,
mỗi mục:

```
### NR-01 — <tên>

<Hệ thống không làm gì.>

- Lý do: <vì sao loại bỏ>
- Nếu sau này cần: <chi phí / công việc để thêm vào>
```

Câu hỏi gợi ý:

1. Bạn từng nghĩ tới những tính năng nào mà giờ thấy không cần? (SEO, chat, review, điểm thưởng,
   lịch sử đơn chi tiết, nhiều loại tiền, coupon, ghế liền kề, chuyển nhượng vé, app mobile,
   đặt vé nhóm tự động chia ghế, thanh toán tại quầy…)
2. Với mỗi thứ, bỏ vì **không phục vụ mục tiêu học tập**, hay vì **quá tốn thời gian so với giá trị**?
3. Có thứ nào bạn muốn giữ lại nhưng **giả lập** (stub) thay vì làm thật? Ví dụ email, thanh toán,
   thông báo push. Giả lập thì ghi rõ "giả lập" để không ai tưởng đã có.
4. Có thứ nào **không thể** làm trong scope này vì lý do kỹ thu? (một instance, không có
   Kubernetes, offline check-in chưa giải quyết xong)
## AI làm gì

AI đề xuất danh sách non-requirements dựa trên domain đã thống nhất ở T01, rồi hỏi bạn:

- Mục nào bạn đồng ý loại bỏ?
- Mục nào bạn muốn giữ lại? Và nếu giữ lại thì có nằm trong scope không, hay là non-goal?

AI **phản biện** khi bạn muốn thêm một thứ vào non-requirements mà lý do loại bỏ không vững —
lúc đó nói thẳng: "lý do này không đủ, vì nó chỉ mất N giờ".

AI ghi vào `docs/prd/02-non-requirements.md` với mã `NR-nn`.

## Tiêu chí hoàn thành

- [x] Có ít nhất 8 mục loại bỏ, không phải toàn bộ là "không làm UI" — 15 mục (`NR-01`..`NR-15`)
- [x] Mỗi mục có lý do cụ thể, không phải "không quan trọng"
- [x] Phân biệt rõ: loại bỏ vì lý do nghiệp vụ / vì thời gian / vì kỹ thu
- [x] Những thứ được **giả lập** được đánh dấu rõ là giả lập — dự án này **không giả lập gì cả**;
  ghi rõ trong mục "Không có phần nào được làm giả"
- [x] Đọc lại danh sách này thấy nó **thực sự giữ được dự án trong đúng thời gian**

> **Ghi chú:** người học chốt không giả lập thành phần nào (email, thanh toán, QR làm thật) vì
> test concurrency chỉ có giá trị khi đường đi thật đã được đi qua. Phần check-in khi mất
> mạng bị cắt (NR-15) → FR-26 loại khỏi scope.
