# Plan tổng — Advanced Ticket System

Plan này luôn tồn tại và được cập nhật dần. Chi tiết của từng giai đoạn nằm ở
`docs/plan/gate-<X>-<tên>.md`, **tạo đúng lúc bắt đầu giai đoạn đó**.

> **Lý do tách vậy:** thiết kế chi tiết của Gate B vô nghĩa nếu Gate A chưa chốt. Viết trước
> sẽ biến lại thành một đặc tả phẳng — đúng thứ mà project này đang muốn tránh.

## Mục tiêu

Xây một hệ thống **đặt vé xem phim theo ghế** bằng ASP.NET Core, tập trung vào những phần khó
của backend: concurrency, tính đúng đắn dưới tranh chấp, idempotency, và hiệu năng **đo được**.

Người xem chọn **ghế** trong một suất chiếu, giữ chỗ có thời hạn, thanh toán, rồi mang mã QR
tới cổng để vào rạp. Đơn vị bán là **từng ghế** — mỗi ghế của một suất chiếu là một dòng dữ
liệu, không phải một con số đếm số.

Đây là project học tập. Thứ tự quan trọng hơn tốc độ. Mỗi tuần phải có một thứ chạy được.

## Nguyên tắc thứ tự

1. **PRD trước domain.** Không vẽ ranh giới context trước khi biết hệ thống phải làm gì.
2. **Domain trước design.** Không chọn cấu trúc module trước khi biết aggregate và invariant.
3. **Design trước code.** Không viết SQL trước khi chốt được cơ chế concurrency.
4. **Đo trước khi tối ưu.** Không có "trước" thì không có "sau".
5. **Test là bằng chứng.** Mọi claim về tính đúng đắn cần một test mà fail nếu logic sai.
6. **Không tự bịa.** Nội dung PRD và domain là suy nghĩ của người học.

## Bốn gate

| Gate | Mở khi | Đóng khi | Exit checklist |
|---|---|---|---|
| **A — PRD** | ngay từ đầu | `docs/prd/` đủ để làm ra domain model | xem `docs/README.md` |
| **B — Domain** | Gate A đóng | `docs/domain/` đủ để làm ra kiến trúc | xem `docs/README.md` |
| **C — Design** | Gate B đóng | `docs/design/` đủ để viết code | xem `docs/README.md` |
| **D — Code** | Gate C đóng | dự án đủ để đưa lên CV | xem `docs/README.md` |

## Stack đã chốt

| Hạng mục | Quyết định |
|---|---|
| Runtime | .NET 10 (`net10.0`, LTS) |
| API | ASP.NET Core Controllers + `[ApiController]` |
| Data | EF Core (write, migrations) + Dapper (read path nóng) + raw SQL (câu tranh chấp) |
| Database | PostgreSQL, schema riêng theo module, 1 `DbContext` + `HasDefaultSchema` |
| Cache | Redis (`StackExchange.Redis`) trực tiếp |
| Auth | `JwtBearer` tự viết, refresh token rotation + reuse detection |
| Payments | Stripe PaymentIntent + webhook idempotent theo `event.id` |
| Tickets / check-in | HMAC-SHA256 signed QR, verify offline, chống replay |
| Test | xUnit + Shouldly + Testcontainers (Postgres + Redis thật) |
| Load test | k6 (Docker image `grafana/k6`) |
| Observability | Serilog + correlation ID → OTel / Prometheus / Grafana |
| Infra | Docker Compose, GitHub Actions, GitHub repo public |
| Config | `appsettings.json` non-secret + user-secrets + biến môi trường |

Quy tắc dùng nhiều tầng: **một hot path chỉ đi qua một tầng truy cập dữ liệu**.

## 12 bài toán concurrency sẽ giải

Đây là bản chất của dự án. Mỗi bài cần một test chứng minh.

| # | Bài toán | Cơ chế dự kiến | Test chứng minh |
|---|---|---|---|
| 1 | Gán trùng ghế (double booking) | conditional `UPDATE` từng ghế | 2 người tranh cùng ghế → đúng 1 thắng, 1 thua `409` |
| 2 | **Đặt N ghế phải all-or-nothing** | một `UPDATE` nhiều dòng có điều kiện, kiểm tra số dòng ảnh hưởng | giữ A,B thành công; C thất bại → **C không bị giữ** |
| 3 | **Deadlock khi tranh ghế chồng nhau** | khoá theo thứ tự cố định (`seat_id`) | 2 người chọn ghế ngược thứ tự → không deadlock |
| 4 | Hết hạn chỗ giữ đua với người mua mới | cùng đụng 1 dòng ghế | đúng một bên thắng; không mất ghế, không giữ trùng |
| 5 | Idempotency key race | `UNIQUE` + `ON CONFLICT DO NOTHING` | 50 request cùng key → 1 đơn |
| 6 | Refresh token reuse | rotation chain + `family_id` | dùng lại token cũ → 401, chain bị revoke |
| 7 | Webhook Stripe trùng / sai thứ tự | idempotent theo `event.id` | gửi 3 lần → 1 transition, cấp đúng 1 bộ vé |
| 8 | Quét lại QR | khoá 1 lần dùng + `UNIQUE` ở DB | quét 10 lần → 1 check-in |
| 9 | Cache stampede trên sơ đồ ghế | single-flight khi miss | 100 request 1 suất nóng → **đúng 1** lần đọc DB |
| 10 | **Invalidate cache theo từng ghế** | ghi đè trạng thái ghế, đọc bằng phiên bản | đặt 4 ghế → instance khác thấy đúng 4 ghế đã bán |
| 11 | Mở bán phim hot cạn connection pool | admission control + `max_connections` | 500 req/s → không pool timeout |
| 12 | Rate limit nhiều instance | Redis token bucket phân tán | 3 instance, 1 quota → không vượt quota |

Bài toán **#2, #3, #10** là phần mới, sinh ra từ việc đổi sang mô hình seat-based — đây là phần
đáng khoe nhất của repo. Bảng này vẫn là **giả định** cho tới khi Gate C chốt cơ chế và
Gate D có test chứng minh.

## Ma trận truy vết

Bảng sống của dự án. Mỗi `FR-nn` phải có ít nhất một ticket đáp ứng trước khi mở Gate D.
Requirement nào không map được → hoặc bỏ khỏi requirements, hoặc thêm ticket.

| FR | Khái niệm domain | Quyết định design | Ticket | Trạng thái |
|---|---|---|---|---|
| FR-01 | (chưa xác định) | (chưa xác định) | T18, T22 | đã lập PRD |
| FR-02 | (chưa xác định) | (chưa xác định) | T18, T22 | đã lập PRD |
| FR-03 | (chưa xác định) | (chưa xác định) | T18, T22 | đã lập PRD |
| FR-04 | (chưa xác định) | (chưa xác định) | T18, T21, T22 | đã lập PRD |
| FR-05 | (chưa xác định) | (chưa xác định) | T20 | đã lập PRD |
| FR-06 | (chưa xác định) | (chưa xác định) | T20, T21 | đã lập PRD |
| FR-07 | (chưa xác định) | (chưa xác định) | T22, T24 | đã lập PRD |
| FR-08 | (chưa xác định) | (chưa xác định) | T22, T24 | đã lập PRD |
| FR-09 | (chưa xác định) | (chưa xác định) | T18 | đã lập PRD |
| FR-10 | (chưa xác định) | (chưa xác định) | T16, T18, T24 | đã lập PRD |
| FR-11 | (chưa xác định) | (chưa xác định) | T13, T17, T23 | đã lập PRD |
| FR-12 | (chưa xác định) | (chưa xác định) | T13, T17, T23 | đã lập PRD |
| FR-13 | (chưa xác định) | (chưa xác định) | T13, T17, T23 | đã lập PRD |
| FR-14 | (chưa xác định) | (chưa xác định) | T18 | đã lập PRD |
| FR-15 | (chưa xác định) | (chưa xác định) | T18 | đã lập PRD |
| FR-16 | (chưa xác định) | (chưa xác định) | T13, T20 | đã lập PRD |
| FR-17 | (chưa xác định) | (chưa xác định) | T13, T24 | đã lập PRD |
| FR-18 | (chưa xác định) | (chưa xác định) | T13, T18, T21 | đã lập PRD |
| FR-19 | (chưa xác định) | (chưa xác định) | T21 | đã lập PRD |
| FR-20 | (chưa xác định) | (chưa xác định) | T17 | đã lập PRD |
| FR-21 | (chưa xác định) | (chưa xác định) | T17 | đã lập PRD |
| FR-22 | (chưa xác định) | (chưa xác định) | T17 | đã lập PRD |
| FR-23 | (chưa xác định) | (chưa xác định) | T13, T17 | đã lập PRD |
| FR-24 | (chưa xác định) | (chưa xác định) | T20, T21 | đã lập PRD |
| FR-25 | (chưa xác định) | (chưa xác định) | T24 | đã lập PRD |
| FR-26 | (đã loại khỏi scope — NR-15) | (không áp dụng) | — | **loại bỏ ở T03** |
| FR-27 | (chưa xác định) | (chưa xác định) | T24 | đã lập PRD |
| FR-28 | (chưa xác định) | (chưa xác định) | T17 | đã lập PRD |
| FR-29 | (chưa xác định) | (chưa xác định) | T16 | đã lập PRD |
| FR-30 | (chưa xác định) | (chưa xác định) | T23, T25 | đã lập PRD |
| FR-31 | (chưa xác định) | (chưa xác định) | T20 | đã lập PRD |
| FR-32 | (chưa xác định) | (chưa xác định) | T17 | đã lập PRD |
| FR-33 | (chưa xác định) | (chưa xác định) | T16, T24 | đã lập PRD |
| FR-34 | (chưa xác định) | (chưa xác định) | T17 | đã lập PRD |
| FR-35 | (chưa xác định) | (chưa xác định) | T24 | đã lập PRD |
| FR-36 | (chưa xác định) | (chưa xác định) | T13 | đã lập PRD |
| FR-37 | (chưa xác định) | (chưa xác định) | T19, T22 | đã lập PRD |
| FR-38 | (chưa xác định) | (chưa xác định) | T19, T20, T22 | đã lập PRD |
| FR-39 | (chưa xác định) | (chưa xác định) | T20, T21, T22 | đã lập PRD |

> **Cột "Ticket" là dự kiến**, dựa trên trường `Traces to:` của từng ticket. Nó sẽ được chốt khi
> Lớp 3 sinh ra quyết định thiết kế. Cột "Khái niệm domain" và "Quyết định design" để
> `(chưa xác định)` vì Lớp 2 và Lớp 3 chưa được viết.
>
> **Cột "Trạng thái"** nói mức độ hoàn thành của việc **đáp ứng** requirement, không phải việc
> viết tài liệu. `đã lập PRD` = đã có trong Lớp 1, chưa được Lớp 2/3/4 xử lý.

## Danh sách ticket

Chi tiết từng ticket nằm ở `.scratch/advanced-ticket-system/issues/`.

### Gate A — PRD (T01–T04)

| # | Ticket | Mode | Blocked by | Traces to |
|---|---|---|---|---|
| T01 | Overview: vấn đề, actors, jobs, success criteria | `co-design` | — | — |
| T02 | Requirements (sinh ra FR-01..FR-38) | `co-design` | T01 | — |
| T03 | Non-requirements | `co-design` | T01 | — |
| T04 | User journeys + business rules (đặt vé, mở bán phim hot, check-in) | `co-design` | T02 | — |

### Gate B — Domain (T05–T09)

| # | Ticket | Mode | Blocked by | Traces to |
|---|---|---|---|---|
| T05 | Ubiquitous language workshop (phim, suất chiếu, loại ghế, chỗ giữ ghế) | `explain` | T04 | — | **xong 2026-11-20** |
| T06 | Context map | `co-design` | T05 | — | **xong 2026-11-20** |
| T07 | Aggregates và invariants (một suất chiếu = một aggregate) | `co-design` | T06 | BR-01..BR-30 | **xong 2026-11-21** |
| T08 | State machines (ghế: trống / giữ / đã bán / bị chặn; đơn: chờ / hết hạn / đã thanh toán) | `co-design` | T07 | BR-03, BR-04, BR-09, BR-10, BR-13 | **xong 2026-11-24** |
| T09 | Cross-context contracts | `co-design` | T07 | — | **xong 2026-11-24** |

### Gate C — Design (T10–T14)

| # | Ticket | Mode | Blocked by | Traces to |
|---|---|---|---|---|
| T10 | Architecture và module boundary | `co-design` | T06, T07, T08 | — |
| T11 | Data model (`inventory.showtime_seats`, bảng ghế) | `co-design` | T07 | — |
| T12 | Concurrency design: 12 bài toán → cơ chế → test | `explain` + `co-design` | T07, T08 | — |
| T13 | API contract và idempotency contract (endpoint ghế) | `co-design` | T09, T12 | FR-11, FR-12, FR-13, FR-16, FR-17, FR-18, FR-23, FR-36 |
| T14 | Bộ ADR | `co-design` | T10, T11, T12, T13 | — |

### Gate D — Implementation (T15–T26)

| # | Ticket | Mode | Blocked by | Vấn đề concurrency |
|---|---|---|---|---|
| T15 | Skeleton + Docker Compose + CI | `ai-implements` | T14 | — |
| T16 | Identity: register, login, JWT, refresh rotation | `you-implement-i-review` | T15 | #6 |
| T17 | Phòng chiếu, sơ đồ ghế, lịch chiếu và cache | `you-implement-i-review` | T15 | #9 #10 |
| T18 | Đặt vé: giữ n ghế all-or-nothing | `pair-tdd` | T15, T17 | #1 #2 #3 |
| T19 | Idempotency middleware | `you-implement-i-review` | T18 | #5 |
| T20 | Stripe PaymentIntent và webhook | `pair-tdd` | T18, T19 | #7 |
| T21 | Worker hết hạn chỗ giữ ghế + dispatcher outbox | `you-implement-i-review` | T18, T20 | #4 |
| T22 | Bộ test concurrency | `you-implement-i-review` | T18, T19, T20, T21 | #1 #2 #3 #4 #5 #7 #8 |
| T23 | Load test (k6) và baseline — mở bán phim hot | `pair-tdd` | T22 | #11 #12 |
| T24 | Check-in: QR HMAC, offline, chống replay | `pair-tdd` | T20 | #8 |
| T25 | Observability: log, metric, trace, dashboard | `you-implement-i-review` | T23 | #11 #12 |
| T26 | README + ADR + báo cáo benchmark + CV | `co-design` | T22, T23, T24, T25 | — |

**Thứ tự cứng:** T10 chỉ mở sau T06–T08. T11 chỉ mở sau T07. T18 chỉ mở sau T10 + T11 + T12.

**Cắt scope khi trễ tiến độ:** T24 (check-in) trước, rồi T25 (Grafana), rồi T23 (k6 mở rộng).
Lõi là T15–T22 + T26.

## Log quyết định

Ghi lại ở đây mỗi khi có quyết định nào ảnh hưởng tới toàn dự án. Chi tiết thì để trong ADR.

| Ngày | Quyết định | Lý do | ADR |
|---|---|---|---|
| 2026-11-18 | Tài liệu chia 4 lớp nối tiếp, có gate | Chưa có PRD mà đã kêu gọi dựng cấu trúc module là sai thứ tự | — |
| 2026-11-18 | AI giữ vai trò mixed theo từng ticket (`Mode:`) | Project học tập: người học phải là người viết | — |
| 2026-11-18 | Xoá `/implement` và `/implement-spec` | Chúng cho AI tự viết code và tự điều phối subagent | — |
| 2026-11-18 | Mô hình tồn kho: tier-based (provisional) | Chốt tạm để code chạy được; xem lại ở Gate B | `docs/adr/0001-inventory-model.md` |
| 2026-11-18 | Chống oversell bằng atomic conditional UPDATE (provisional) | So sánh với `FOR UPDATE` bằng số đo ở Gate C | `docs/adr/0002-oversell-prevention.md` |
| 2026-11-19 | **Đổi nghiệp vụ: bán vé sự kiện → đặt vé xem phim** | Người học chốt lại khi nhớ ra nghiệp vụ thật có chỗ ngồi. Mô hình seat-based cho bài toán concurrency **khó hơn và đáng học hơn** (giữ n ghế all-or-nothing + tránh deadlock) | `docs/adr/0003-seat-based-inventory.md` |
| 2026-11-19 | 1 ghế = 1 vé = 1 mã QR | Giữ được yêu cầu "mỗi vé chỉ check-in một lần" ở mức từng ghế; check-in vẫn là một bài toán concurrency | `docs/adr/0003-seat-based-inventory.md` |
| 2026-11-19 | Một đơn đặt vé được phép trộn nhiều loại ghế, giá tính theo từng ghế | Đúng thực tế người xem chọn ghế bất kỳ; tách giá khỏi số lượng | `docs/adr/0003-seat-based-inventory.md` |
| 2026-11-19 | "Loại ghế" chỉ mang giá, **không mang số lượng** | Số ghế của một suất = số dòng ghế của suất đó. Tồn kho không còn là con số đếm số | `docs/adr/0003-seat-based-inventory.md` |
| 2026-11-20 | **Giữ ghế 15 phút** | Đủ dài để người xem đổi thẻ, ngắn để ghế không bị giữ vô thời hạn khi mở bán phim hot | — |
| 2026-11-20 | **Tối đa 6 ghế mỗi đơn, 10 ghế mỗi người mỗi suất** | `BR-06` chặn *một lần đặt* quá lớn; `BR-08` chặn *một người* tích luỹ quá nhiều trong một suất (chống mua số lượng lớn rồi chuyển nhượng ra ngoài) | — |
| 2026-11-20 | **Ghế đôi tính là 2 ghế khi đếm giới hạn, nhưng vẫn sinh 1 vé** | Một ghế là một chỗ ngồi nên là một vé; nhưng ghế đôi chiếm chỗ của 2 người nên phải tính 2 ghế khi áp giới hạn số lượng | — |
| 2026-11-20 | **Trả tiền muộn sau khi đơn hết hạn → hoàn tự động toàn bộ** | Tiền đã lấy mà không có vé là lỗ hổng nghiệp vụ nặng nhất với người dùng; bổ sung `FR-39` | — |
| 2026-11-20 | **Cắt FR-26 (check-in khi mất mạng)** | Cơ chế đồng bộ offline là một dự án riêng; giữ nguyên bài toán chống quét lại vé là đủ. Xem `NR-15` | — |
| 2026-11-20 | **Không giả lập bất kỳ thành phần nào** | Test concurrency chỉ có giá trị khi đường đi thật đã được đi qua — webhook giả lập không bao giờ tạo ra tình huống "đến lặp ba lần" | — |
| 2026-11-20 | **Phân biệt "ghế của phòng" và "ghế của suất chiếu"** | Chốt ở T05. Cùng một từ "ghế" cho cả hai khiến người đọc hiểu một người đã mua D7 thì D7 hết ở *mọi* suất — và hệ thống báo lỗi sai | `docs/domain/ubiquitous-language.md` |
| 2026-11-20 | **`FR-23` có bốn con số, không phải ba** | Sửa mâu thuẫn với `BR-11`: ghế `Bị chặn` vẫn tính vào tổng số ghế phòng nhưng không thuộc nhóm "trống / giữ / bán" | `docs/prd/01-requirements.md` |
| 2026-11-20 | **Mỗi thuật ngữ domain có cả tên tiếng Việt lẫn tên tiếng Anh** | Code viết bằng tiếng Anh — tên class, biến, bảng, trạng thái lấy từ tên tiếng Anh trong glossary. Tên tiếng Việt chỉ dùng để trao đổi trong tài liệu | `docs/domain/ubiquitous-language.md` |
| 2026-11-20 | **Sáu bounded context: `Scheduling`, `Inventory`, `Booking`, `Payment`, `Ticketing`, `Identity & Access`** | Chốt ở T06 từ 7 journey ở T04. Ranh giới chính: `Seat hold` ở `Inventory` (bất biến hết hạn không bị cắt), vé + check-in ở `Ticketing` (bất biến 1 vé = 1 check-in không bị cắt), Stripe là hệ thống bên ngoài nên `Payment` → Stripe là ACL | `docs/domain/context-map.md` |
| 2026-11-20 | **Bounded context ≠ transaction boundary** | Trong modular monolith hai thứ không trùng nhau. Ba nơi bất biến bị cắt ngang ranh giới được liệt kê tường minh kèm hậu quả khi tách service | `docs/domain/context-map.md` |
| 2026-11-21 | **`Seat hold` là aggregate root của tồn kho ghế (≤6 ghế), KHÔNG phải `Showtime`** | `BR-02` (giữ n ghế all-or-nothing) không được phép bị cắt: nếu mỗi ghế là một aggregate thì all-or-nothing phải dùng compensating transaction ⇒ có thời điểm hệ thống vi phạm `BR-02`. Aggregate lớn (`Showtime` chứa 200 ghế) lại gây contention dồn khi mở bán phim hot | `docs/domain/aggregates/`, `docs/adr/0004-seat-hold-aggregate-boundary.md` |
| 2026-11-21 | **`BR-01` ép bằng điều kiện lệnh, không bằng khoá đọc-sửa-ghi** | `UPDATE ... WHERE status = 'Available'` không có cửa sổ TOCTOU; hai lệnh đồng thời trên cùng một ghế thì chỉ một thắng, lệnh còn lại ảnh hưởng 0 dòng và phải coi là thất bại | `docs/domain/aggregates/02-inventory.md` |
| 2026-11-21 | **`BR-08` (10 ghế/người/suất) không bảo vệ được bằng aggregate — chấp nhận cửa sổ race** | Nó đếm theo *người* qua *nhiều đơn* ⇒ tích luỹ xuyên nhiều aggregate, về nguyên tắc không thể nằm trong một aggregate. Hậu quả của cửa sổ race là *người dùng tự gian lận*, không phải *bán quá số ghế có thật* — nên chấp nhận được, và rẻ hơn một điểm tranh chấp toàn cục ở tầng DB | `docs/domain/aggregates/03-booking.md` |
| 2026-11-21 | **`BR-10` (1 vé = 1 check-in) ép bằng ràng buộc duy nhất, không cần transaction xuyên nhiều vé** | Nó là invariant về **một vé**: hai lượt quét đồng thời là hai transaction cùng tranh một dòng. Bài học: một invariant trông "toàn cục" có thể hoá thành "cục bộ" nếu đặt ranh giới aggregate đúng chỗ | `docs/domain/aggregates/05-ticketing.md` |
| 2026-11-21 | **`BR-20` (không có suất chiếu chồng nhau) ép bằng exclusion constraint, nằm ngoài ranh giới aggregate** | Đây là quan hệ *giữa hai `Showtime` khác nhau* — aggregate không thể ghi ra ngoài ranh giới của mình. Ràng buộc loại trừ ở tầng lưu trữ mạnh hơn kiểm tra ứng dụng, vì chặn được cả hai lệnh tạo suất chạy đồng thời | `docs/domain/aggregates/01-scheduling.md` |
| 2026-11-24 | **Trạng thái "chờ thanh toán" tên là `AwaitingPayment`, không phải `Pending`** | `Pending` đã bị glossary ghi là nên tránh (dễ nhầm với `Held` của ghế). Tên trạng thái là tên sẽ dùng trong code — nên phải chốt sớm, trước khi viết schema | `docs/domain/state-machines.md` |
| 2026-11-24 | **`Payment intent` dùng `Captured` / `Refunded`, KHÔNG dùng `Paid`** | `Paid` đã là trạng thái của `Booking`. Trùng tên ở hai context khiến log dễ đọc sai — "booking `Paid`" và "payment `Paid`" là hai sự kiện khác nhau và không xảy ra cùng lúc | `docs/domain/state-machines.md` |
| 2026-11-24 | **`Payment intent` KHÔNG có trạng thái "thất bại"** | Bám theo `INV-04`: ta chỉ có **một** trạng thái "chờ thanh toán" cho tới khi có kết quả. Kết quả thất bại làm intent bị **bỏ**, đơn đi theo đường `Expired`/`Cancelled` bình thường — không tạo ra trạng thái mới | `docs/domain/state-machines.md` |
| 2026-11-24 | **Vé bị vô hiệu do hủy suất chiếu là TÁC DỤNG PHỤ, không phải trạng thái thứ ba của `Ticket`** | Máy `Ticket` chỉ có `Issued` → `CheckedIn`. "Vé này còn dùng được không" là **điều kiện tiên quyết** của transition check-in, không phải trạng thái. Thêm trạng thái thứ ba sẽ tạo ra một đường chuyển trạng thái mà không ai dùng tới | `docs/domain/state-machines.md` |
| 2026-11-24 | **Race "hết hạn đúng lúc webhook đến": ai chuyển trạng thái trước thì thắng, chốt được trên giấy** | Cả hai luồng ghi vào **cùng một trạng thái nguồn** (`AwaitingPayment`) của cùng một aggregate bằng **điều kiện lệnh** ⇒ chỉ một lệnh thắng, nhánh thua định nghĩa sẵn. Không cần prototype vì kết quả **không** phụ thuộc thứ tự **đọc** | `docs/domain/state-machines.md` |
| 2026-11-24 | **Ranh giới bị cắt thứ tư: hủy suất chiếu** | `OnSale → Cancelled` kéo theo vô hiệu vé (`Ticketing`) + hoàn tiền mọi đơn (`Payment`) — cắt qua ba context. Khác ba chỗ đã biết ở T06 ở chỗ nó chạy **nhiều đơn**, không phải một aggregate: một suất có thể có hàng trăm đơn đã trả tiền | `docs/domain/state-machines.md` |
| 2026-11-24 | **Mô hình đọc chéo context: qua interface/contract, không đọc trực tiếp bảng** | Giữ ranh giới module rõ ràng ngay từ design stage; tất cả đọc dữ liệu xuyên context đều có "chủ sở hữu" và "khách hàng". Tránh tình trạng "nhanh hơn" thành lý do lợn lại ranh giới ở tầng code | `docs/domain/cross-context-contracts.md` |
| 2026-11-24 | **Hợp đồng R1 và R2 tách riêng (không gộp bidirectional)** | Giúp nhìn rõ R2 (`Inventory → Scheduling` đọc-duy-nhất 4 con số) là ứng viên tách service hàng đầu. Cách gom hợp đồng ảnh hưởng tới cách nhìn service boundary sau này | `docs/domain/cross-context-contracts.md` |
| 2026-11-24 | **Q27 decision: `Booking.Paid` KHÔNG chuyển sang `Refunded` khi hủy suất** | Dấu vết hoàn tiền ở `Payment intent.Refunded`. Lý do: `Booking` đã kết thúc chu kỳ chính (giữ → thanh toán → cấp vé); hoàn tiền do hủy suất là sự kiện ngoại lai. Giữ trạng thái đơn giản, không tạo ranh giới trạng thái mới | `docs/domain/cross-context-contracts.md`, Q27 |
| 2026-11-24 | **7 cặp thuật ngữ khác nghĩa + Anti-Corruption Layer cho mỗi cặp** | `Room seat` vs `Showtime seat`, `Cancelled` (booking) vs `Cancelled` (showtime), giá gốc vs giá frozen, `Seat hold` vs `Booking`, `Payment intent` (ours) vs `payment_intent` (Stripe), `Sold` vs `Ticket`, cùng tên `AwaitingPayment` ở hai context khác nhau. Ghi tường minh nơi dịch | `docs/domain/cross-context-contracts.md` |

## Câu hỏi còn mở

| Câu hỏi | Quyết ở gate nào |
|---|---|
| Một suất chiếu có hàng trăm ghế — aggregate có phải chia nhỏ không? | ~~Gate B (T07, ADR-0003)~~ → **đã trả lời 2026-11-21: KHÔNG — `Seat hold` là aggregate root (≤6 ghế); xem `docs/domain/aggregates/`** |
| Giới hạn "10 ghế mỗi người mỗi suất" có nằm trong cùng aggregate với tồn kho ghế không? Nó đếm theo **người**, tồn kho ghế đếm theo **ghế** | ~~Gate B (T07, T09)~~ → **đã trả lời 2026-11-21: KHÔNG — tích luỹ xuyên nhiều aggregate, chấp nhận cửa sổ race; xem `docs/domain/aggregates/03-booking.md`** |
| Check-in thuộc bounded context nào — cùng context với vé, hay context riêng? | ~~Gate B (T06)~~ → **đã trả lời 2026-11-20: `Ticketing` giữ cả vé và check-in** |
| Ranh giới aggregate quanh thanh toán và hoàn tiền (`FR-39`) | ~~Gate B (T07, T08)~~ → **đã trả lời 2026-11-24: `Payment intent` là aggregate root riêng ở `Payment`; hoàn tiền là transition `Captured → Refunded` của nó, còn quyết định "có hoàn không" dựa trên cả tiền đã thu lẫn trạng thái đơn; xem `docs/domain/state-machines.md`** |
| Đơn `Paid` khi hủy suất chiếu có cần trạng thái `Refunded` riêng không | ~~Gate B (T08, T09)~~ → **đã trả lời 2026-11-24: KHÔNG — `Booking` giữ `Paid`, dấu vết hoàn tiền ở `Payment intent.Refunded`; xem `docs/domain/cross-context-contracts.md`** |
| Giá vé theo loại ghế có đổi giữa các suất của cùng một phim không? | Gate B (T07, T11) |
| Ghế `Blocked` (vệ sinh, dành riêng) có được bán online không, ai quyết định? | ~~Gate B (T07, T08)~~ → **đã trả lời 2026-11-20: KHÔNG bán lại online; quản lý rạp quyết định; xem `BR-11`** |
| Có cần `SERIALIZABLE` ở đâu không, hay conditional update đủ? | Gate C (ADR-0002) |
| Có tách module thành service riêng không, và ranh giới đo được ở đâu? | Sau T23 |
| Hold TTL cố định hay cấu hình theo suất chiếu? | Gate C (T13) |
| Offline check-in sync dùng cơ chế nào? | Gate C (T24) |
| Có cần distributed lock ở đâu không? | Gate C (T12) |
| Khi nào chuyển tài liệu sang tiếng Anh để đưa lên CV? | T26 |
| Có dùng `teach` (sinh lesson HTML) cho phần lý thuyết concurrency không? | Gate C |
