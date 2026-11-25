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
| Q12 | ~~Một suất chiếu có hàng trăm ghế — aggregate có phải chia nhỏ không? Nếu chia, invariant "một ghế thuộc đúng một chủ" bảo vệ thế nào?~~ | T07, ADR-0003 | — | **ĐÃ ĐÓNG 2026-11-21 — KHÔNG chia theo suất chiếu. `Seat hold` là aggregate root (≤6 ghế); `BR-01` ép bằng điều kiện lệnh. Xem `docs/domain/aggregates/`** |
| Q13 | Giá vé theo loại ghế có đổi giữa các suất của cùng một phim không (vd cuối tuần đắt hơn)? | T07, T11 | Chốt ở Gate B | chưa biết |
| Q14 | ~~Ghế `Blocked` (vệ sinh, dành riêng) có được bán lại online không, và ai quyết định?~~ | T07, T08 | — | **ĐÃ ĐÓNG 2026-11-20 — `BR-11`: không bán lại online, quyền của quản lý rạp** |
| Q15 | ~~Giới hạn "10 ghế mỗi người mỗi suất" (`FR-09`) có nằm trong cùng aggregate với tồn kho ghế không?~~ | T07 | — | **ĐÃ ĐÓNG 2026-11-21 — KHÔNG. Nó đếm theo *người* qua *nhiều đơn* ⇒ tích luỹ xuyên nhiều aggregate, về nguyên tắc không thể nằm trong một aggregate. Chấp nhận cửa sổ race (hậu quả là người dùng tự gian lận, không phải bán quá số ghế). Xem `docs/domain/aggregates/03-booking.md` `INV-10`** |
| Q16 | ~~Check-in thuộc bounded context nào~~ | T06 | — | **ĐÃ ĐÓNG 2026-11-20 — `Ticketing` giữ cả vé và check-in** |
| Q17 | ~~Ranh giới aggregate quanh **thanh toán** và **hoàn tiền** (`FR-39`): đơn có phải aggregate gốc, và giao dịch thanh toán bên ngoài có thuộc aggregate không?~~ | T07, T08 | — | **ĐÃ ĐÓNG 2026-11-21 — `Payment intent` là aggregate root riêng ở context `Payment`, chứa lịch sử `Payment event` và ≤1 `Refund`. Đơn KHÔNG phải aggregate gốc. Xem `docs/domain/aggregates/04-payment.md`** |
| Q18 | ~~Ranh giới `Booking` vs `Inventory` khi tách service: đặt `Seat hold` ở đâu — ở `Inventory` hay ở `Booking`?~~ | T07 | — | **ĐÃ ĐÓNG 2026-11-21 — `Seat hold` ở `Inventory`. Lý do: bất biến "ghế `Held` luôn có thời điểm hết hạn" không được cắt ngang ranh giới. Xem `docs/domain/aggregates/02-inventory.md`** |
| Q19 | ~~`Booking` có cần giữ bản ghi lịch sử trạng thái (cho `FR-31` truy vết) hay `Ticketing`/`Payment` giữ riêng?~~ | T07, T08 | — | **ĐÃ ĐÓNG 2026-11-21 — `Booking` giữ lịch sử trạng thái của chính nó (append-only, `INV-07`). `Ticketing` và `Payment` giữ lịch sử riêng của mình. Xem `docs/domain/aggregates/03-booking.md`** |
| Q20 | Ba "ranh giới bị cắt" có được giữ nguyên trong một transaction khi tách thành microservice không, hay phải chuyển sang saga? | T10 | Chốt ở Gate C | chưa biết |
| Q21 | `BR-08` (10 ghế/người/mỗi suất) có nên nâng từ quy tắc tầng ứng dụng lên ràng buộc ở tầng lưu trữ (bảng tổng hợp số ghế theo cặp người+suất)? | T11, T12, T22 | Chốt ở Gate C | chưa biết |
| Q22 | Giá trị **khoảng đệm** giữa hai suất chiếu cùng một phòng — cần đủ dài để dọn dẹp, nhưng không quá dài. Ảnh hưởng trực tiếp tới việc `BR-20` báo lỗi đúng hay không. | T13 | Chốt ở Gate C | chưa biết |
| Q23 | `BR-08` có cần được kiểm chứng bằng **test concurrency thật** để thấy rõ cửa sổ race, hay chỉ test logic ở tầng ứng dụng? | T22 | Chốt ở Gate C | chưa biết |
| Q24 | `Gate assignment` (phân công cổng) hiện **không** thuộc aggregate nào. Nếu sau này cần bảo vệ "một cổng không phục vụ hai suất chồng nhau" bằng transaction đơn lẻ thì phải xem lại ranh giới aggregate nào. | T07, T11 | Chốt ở Gate C | chưa biết |
| Q25 | Ghế `Held` có cần **trạng thái riêng** cho "đang giữ nhưng đã quá hạn chưa được giải phóng" không? Hiện tại `Held` được giải phóng bằng tác vụ nền quét lô, nên tồn tại một khoảng thời gian ghế `Held` đã quá hạn nhưng chưa được giải phóng — trong khoảng đó người khác không đặt được ghế đó. | T08, T12, T21 | Chốt ở Gate C | chưa biết |
| Q26 | Một `Booking` có cần trạng thái **"đã cấp vé"** riêng, hay cờ + số lượng vé chỉ là thuộc tính bên trong trạng thái `Paid`? | T08, T11 | Chốt ở Gate C | chưa biết |
| Q27 | Khi quản lý rạp hủy suất chiếu, đơn `Paid` của suất đó có cần **trạng thái riêng** (ví dụ `Refunded`) để phản ánh "đã hoàn tiền", hay thuộc tính hoàn tiền phía `Payment` là đủ? | T08, T09, T11 | Chốt ở Gate C | ~~đã trả lời 2026-11-24: KHÔNG — `Booking` giữ `Paid`, dấu vết hoàn tiền ở `Payment intent.Refunded`; xem `docs/domain/cross-context-contracts.md`~~ |
| Q28 | Trạng thái "chờ kết quả" của `Payment intent` dùng **cùng tên** `AwaitingPayment` với `Booking` — hai thứ khác nhau nhưng cùng tên. Có nên đổi tên để tránh nhầm khi đọc log? | T08, T09, T11 | Chốt ở Gate C | chưa biết |
| Q29 | Hủy suất chiếu chạy **N đơn** (mọi đơn `Paid` của suất). Trong modular monolith, có nên gộp tất cả vào **một** transaction, hay chia thành N transaction riêng để tránh transaction quá dài? Cân bằng giữa tính nhất quán và thời gian khoá. | T08, T10, T12 | Chốt ở Gate C | chưa biết |

## Quy tắc

- Một câu hỏi ở đây phải **cụ thể** và có **điều kiện đóng**. "Tìm hiểu về Redis" không phải câu
  hỏi.
- Khi câu hỏi được trả lời, **di chuyển** nó vào ADR hoặc vào tài liệu design tương ứng, và
  xoá khỏi bảng này. Câu hỏi đã trả lời ở lại đây là nợ.
- Nếu một câu hỏi ở lại quá 2 tuần, đó là tín hiệu nó đang chặn một gate — hãy nâng lên
  `docs/plan/00-roadmap.md` mục "Câu hỏi còn mở".
