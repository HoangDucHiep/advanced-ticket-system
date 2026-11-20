# Câu hỏi còn mở

Những câu hỏi chưa có đáp án, cần research hoặc cần quay lại sau. Giữ ở đây để không bị
quên giữa các session.

Dùng skill `/research` để tra tài liệu chính thức (Stripe, Postgres, ASP.NET Core) — kết quả
research là một file Markdown có trích dẫn trong repo, đưa vào lại flow chính.

| # | Câu hỏi | Liên quan | Cần tra ở đâu | Trạng thái |
|---|---|---|---|---|
| Q01 | Chống bán trùng: conditional UPDATE nhiều dòng, UPDATE từng ghế theo thứ tự, hay `SELECT FOR UPDATE`? Khác nhau ở đâu về latency, lock contention và deadlock? | T12, ADR-0002 | Số đo thực tế (T23) | chưa biết |
| Q02 | `SERIALIZABLE` có cần thiết ở đâu không, và cái giá của việc phải retry serialization failure là bao nhiêu? | T12 | Tài liệu Postgres | chưa biết |
| Q03 | ~~Mô hình tồn kho: tier-based đủ, hay cần seat-based?~~ | — | — | **ĐÃ ĐÓNG 2026-11-19 — seat-based, xem ADR-0003** |
| Q04 | Hold TTL cố định hay cấu hình theo suất chiếu? | T21, T13 | Chốt ở Gate C | chưa biết |
| Q05 | Offline check-in đồng bộ lại bằng cơ chế nào, và xử lý xung đột thế nào? | T24 | Chốt ở Gate C | chưa biết |
| Q06 | Có cần distributed lock ở đâu không, hay `SKIP LOCKED` + `ON CONFLICT` đã đủ? | T12 | Chốt ở Gate C | chưa biết |
| Q07 | Rate limit phân tán: token bucket trên Redis chịu được bao nhiêu request mỗi giây? | T12, T23 | Số đo | chưa biết |
| Q08 | Admission control nên giới hạn ở tầng nào — middleware, hay hàng đợi trong DB? | T23 | Chốt ở Gate C | chưa biết |
| Q09 | Refresh token: lưu hash bằng thuật toán nào, và rotate secret theo chu kỳ thế nào? | T16 | Chốt ở T16 | chưa biết |
| Q10 | Khi nào chuyển tài liệu sang tiếng Anh để đưa lên CV? | T26 | Chốt ở T26 | chưa biết |
| Q11 | Có dùng skill `teach` (sinh lesson HTML) cho phần lý thuyết concurrency không, hay chỉ giải thích trong chat? | T12 | Chốt ở Gate C | chưa biết |
| Q12 | Một suất chiếu có hàng trăm ghế — aggregate có phải chia nhỏ không? Nếu chia, invariant "một ghế thuộc đúng một chủ" bảo vệ thế nào? | T07, ADR-0003 | Chốt ở Gate B | chưa biết |
| Q13 | Giá vé theo loại ghế có đổi giữa các suất của cùng một phim không (vd cuối tuần đắt hơn)? | T07, T11 | Chốt ở Gate B | chưa biết |
| Q14 | ~~Ghế `Blocked` (vệ sinh, dành riêng) có được bán lại online không, và ai quyết định?~~ | T07, T08 | — | **ĐÃ ĐÓNG 2026-11-20 — `BR-11`: không bán lại online, quyền của quản lý rạp** |
| Q15 | Giới hạn "10 ghế mỗi người mỗi suất" (`FR-09`) có nằm trong cùng aggregate với tồn kho ghế không? Nó đếm theo **người**, tồn kho ghế đếm theo **ghế** — nếu tách, thao tác giữ ghế phải đọc tồn kho của người khác, tức là đọc ngoài ranh giới. | T07, T09 | Chốt ở Gate B | chưa biết |
| Q16 | Check-in thuộc bounded context nào — cùng context với `Vé`, hay context riêng? Ảnh hưởng tới việc một vé bị quét hai lần có cần xử lý xuyên context không. | T06 | Chốt ở Gate B | chưa biết |
| Q17 | Ranh giới aggregate quanh **thanh toán** và **hoàn tiền** (`FR-39`): đơn có phải aggregate gốc, và giao dịch thanh toán bên ngoài có thuộc aggregate không? | T07, T08 | Chốt ở Gate B | chưa biết |

## Quy tắc

- Một câu hỏi ở đây phải **cụ thể** và có **điều kiện đóng**. "Tìm hiểu về Redis" không phải câu
  hỏi.
- Khi câu hỏi được trả lời, **di chuyển** nó vào ADR hoặc vào tài liệu design tương ứng, và
  xoá khỏi bảng này. Câu hỏi đã trả lời ở lại đây là nợ.
- Nếu một câu hỏi ở lại quá 2 tuần, đó là tín hiệu nó đang chặn một gate — hãy nâng lên
  `docs/plan/00-roadmap.md` mục "Câu hỏi còn mở".
