# Câu hỏi còn mở

Chỉ giữ câu hỏi **chưa có đáp án**. Khi một câu hỏi được trả lời, chuyển đáp án vào ADR hoặc tài
liệu design tương ứng và **xoá hàng** khỏi bảng này.

Dùng skill `/research` để tra tài liệu chính thức (Stripe, Postgres, ASP.NET Core) — kết quả là một
file Markdown có trích dẫn trong repo.

| # | Câu hỏi | Cần gì để trả lời | Chốt ở bước |
|---|---|---|---|
| Q07 | Rate limit phân tán: token bucket Lua trên Redis chịu được bao nhiêu request mỗi giây? | Số đo | load test |
| Q10 | Khi nào chuyển tài liệu sang tiếng Anh để đưa lên CV? | Quyết định của người học khi code xong | polish CV |
| Q30 | Test Stripe webhook trong CI: chạy Stripe CLI + secret test mode trên CI, hay chỉ chạy local? | Thử Stripe CLI trên GitHub Actions, đọc tài liệu Stripe | thanh toán |
| Q31 | Gửi email thật cho `FR-19` ở môi trường dev bằng gì? (gợi ý: Mailpit trong Compose) | Thử Mailpit trong Compose | thông báo hết hạn |
| Q32 | Ngưỡng `PermitLimit` / `QueueLimit` của admission control (khởi điểm 30/100) | Số đo load test | load test |
| Q33 | Biến thể khoá ghế nào nhanh nhất (ADR-0002)? | Số đo so sánh các biến thể | load test |
| Q34 | Ngưỡng tách service (`architecture.md` D10) | Kết quả load test | sau load test |

## Quy tắc

- Một câu hỏi phải **cụ thể** và có **điều kiện đóng**. "Tìm hiểu về Redis" không phải câu hỏi.
- Câu hỏi đã trả lời thì xoá khỏi bảng (đáp án nằm ở ADR / design). Để lại là nợ.
- Nếu một câu hỏi ở lại quá 2 tuần mà đang chặn một bước, nâng lên `docs/plan/00-roadmap.md`.
- Số câu hỏi không dùng lại: id đã đóng không được tái sử dụng.
