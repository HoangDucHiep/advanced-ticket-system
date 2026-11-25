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
| Q05 | ~~Offline check-in đồng bộ lại bằng cơ chế nào?~~ | T24 | — | **ĐÃ ĐÓNG 2026-11-20 — loại khỏi scope (`NR-15`)** |
| Q06 | Có cần distributed lock ở đâu không, hay `SKIP LOCKED` + `ON CONFLICT` đã đủ? | T12 | Chốt ở Gate C | chưa biết |
| Q07 | Rate limit phân tán: token bucket trên Redis chịu được bao nhiêu request mỗi giây? | T12, T23 | Số đo | chưa biết |
| Q08 | Admission control nên giới hạn ở tầng nào — middleware, hay hàng đợi trong DB? | T23 | Chốt ở Gate C | chưa biết |
| Q09 | Refresh token: lưu hash bằng thuật toán nào, và rotate secret theo chu kỳ thế nào? | T16 | Chốt ở T16 | chưa biết |
| Q10 | Khi nào chuyển tài liệu sang tiếng Anh để đưa lên CV? | T26 | Chốt ở T26 | chưa biết |
| Q11 | Có dùng skill `teach` (sinh lesson HTML) cho phần lý thuyết concurrency không, hay chỉ giải thích trong chat? | T12 | Chốt ở Gate C | chưa biết |
| Q12 | ~~Một suất chiếu có hàng trăm ghế — aggregate có phải chia nhỏ không?~~ | T07, ADR-0003 | — | **ĐÃ ĐÓNG — KHÔNG chia theo suất chiếu. Bản 2026-11-21 chọn `Seat hold`; sửa 2026-11-25: mỗi `Showtime seat` là aggregate root, `BR-02` do một DB transaction bảo đảm (ADR-0005)** |
| Q13 | Giá vé theo loại ghế có đổi giữa các suất của cùng một phim không (vd cuối tuần đắt hơn)? | T07, T11 | Chốt ở Gate B | chưa biết |
| Q14 | ~~Ghế `Blocked` (vệ sinh, dành riêng) có được bán lại online không, và ai quyết định?~~ | T07, T08 | — | **ĐÃ ĐÓNG 2026-11-20 — `BR-11`: không bán lại online, quyền của quản lý rạp** |
| Q15 | ~~Giới hạn "10 ghế mỗi người mỗi suất" (`FR-09`) có nằm trong cùng aggregate với tồn kho ghế không?~~ | T07 | — | **ĐÃ ĐÓNG — KHÔNG nằm trong một aggregate. Sửa 2026-11-25: ép bằng khoá (người xem, suất) trong transaction giữ ghế, đếm cả `Held` + `Sold` — không còn cửa sổ race (ADR-0007)** |
| Q16 | ~~Check-in thuộc bounded context nào~~ | T06 | — | **ĐÃ ĐÓNG 2026-11-20 — `Ticketing` giữ cả vé và check-in** |
| Q17 | ~~Ranh giới aggregate quanh **thanh toán** và **hoàn tiền** (`FR-39`): đơn có phải aggregate gốc, và giao dịch thanh toán bên ngoài có thuộc aggregate không?~~ | T07, T08 | — | **ĐÃ ĐÓNG 2026-11-21 — `Payment intent` là aggregate root riêng ở context `Payment`, chứa lịch sử `Payment event` và ≤1 `Refund`. Đơn KHÔNG phải aggregate gốc. Xem `docs/domain/aggregates/04-payment.md`** |
| Q18 | ~~Đặt `Seat hold` ở đâu — `Inventory` hay `Booking`?~~ | T07 | — | **ĐÃ ĐÓNG 2026-11-25 — không còn aggregate `Seat hold`; thông tin giữ (`hold_id`, `held_until`) nằm trên `Showtime seat` ở `Inventory`, `expires_at` gốc ở `Booking` (ADR-0005)** |
| Q19 | ~~`Booking` có cần giữ bản ghi lịch sử trạng thái (cho `FR-31` truy vết) hay `Ticketing`/`Payment` giữ riêng?~~ | T07, T08 | — | **ĐÃ ĐÓNG 2026-11-21 — `Booking` giữ lịch sử trạng thái của chính nó (append-only, `INV-07`). `Ticketing` và `Payment` giữ lịch sử riêng của mình. Xem `docs/domain/aggregates/03-booking.md`** |
| Q20 | Ba "ranh giới bị cắt" có được giữ nguyên trong một transaction khi tách thành microservice không, hay phải chuyển sang saga? | T10 | Chốt ở Gate C | chưa biết |
| Q21 | ~~`BR-08` có nên nâng lên ràng buộc ở tầng lưu trữ?~~ | T11, T12, T22 | — | **ĐÃ ĐÓNG 2026-11-25 — ép bằng khoá theo (người xem, suất); cách hiện thực (advisory lock / dòng quota) chốt ở T12 (ADR-0007)** |
| Q22 | Giá trị **khoảng đệm** giữa hai suất chiếu cùng một phòng — cần đủ dài để dọn dẹp, nhưng không quá dài. Ảnh hưởng trực tiếp tới việc `BR-20` báo lỗi đúng hay không. | T13 | Chốt ở Gate C | chưa biết |
| Q23 | ~~`BR-08` cần test concurrency thật hay chỉ test logic?~~ | T22 | — | **ĐÃ ĐÓNG 2026-11-25 — test concurrency thật, phải đỏ khi bỏ khoá (ADR-0007)** |
| Q24 | ~~`Gate assignment` không thuộc aggregate nào~~ | T07, T11 | — | **ĐÃ ĐÓNG 2026-11-25 — `Gate assignment` là aggregate riêng ở `Ticketing`, nghĩa "nhân viên ↔ suất chiếu"; thêm `FR-40`** |
| Q25 | Ghế `Held` đã quá `held_until` mà worker chưa trả — lệnh giữ mới có được lấy luôn không? Từ 2026-11-25 việc này **an toàn về tính đúng đắn** (đơn cũ không thể `Paid` sau hạn — ADR-0006; lệnh trả ghế lọc theo `hold_id` — ADR-0005); còn lại là câu hỏi cơ chế. | T12, T21 | Chốt ở Gate C | chưa biết |
| Q26 | ~~`Booking` có cần trạng thái/cờ "đã cấp vé"?~~ | T08, T11 | — | **ĐÃ ĐÓNG 2026-11-25 — KHÔNG. `Paid` và cấp vé cùng một transaction ⇒ "Paid chưa có vé" không thể xảy ra** |
| Q27 | ~~Đơn `Paid` của suất bị hủy có cần trạng thái `Refunded` riêng?~~ | T08, T09, T11 | — | **ĐÃ ĐÓNG 2026-11-24 — KHÔNG. `Booking` giữ `Paid`; dấu vết hoàn tiền ở `Payment intent` (`RefundPending`/`Refunded`); `FR-31` ghép hai lịch sử, `BR-30` hiển thị vé của suất đã hủy** |
| Q28 | Trạng thái "chờ kết quả" của `Payment intent` dùng **cùng tên** `AwaitingPayment` với `Booking` — hai thứ khác nhau nhưng cùng tên. Có nên đổi tên để tránh nhầm khi đọc log? | T08, T09, T11 | Chốt ở Gate C | chưa biết |
| Q29 | ~~Hủy suất chiếu: một transaction hay N transaction?~~ | T08, T10, T12 | — | **ĐÃ ĐÓNG 2026-11-25 — một transaction ngắn (hủy suất, huỷ đơn chờ, ghi N yêu cầu hoàn) + N lượt gọi Stripe bất đồng bộ qua outbox (ADR-0006)** |

## Quy tắc

- Một câu hỏi ở đây phải **cụ thể** và có **điều kiện đóng**. "Tìm hiểu về Redis" không phải câu
  hỏi.
- Khi câu hỏi được trả lời, **di chuyển** nó vào ADR hoặc vào tài liệu design tương ứng, và
  xoá khỏi bảng này. Câu hỏi đã trả lời ở lại đây là nợ.
- Nếu một câu hỏi ở lại quá 2 tuần, đó là tín hiệu nó đang chặn một gate — hãy nâng lên
  `docs/plan/00-roadmap.md` mục "Câu hỏi còn mở".
