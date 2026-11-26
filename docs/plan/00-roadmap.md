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
| Database | PostgreSQL, schema riêng theo module, **mỗi module 1 `DbContext`**, chung connection + transaction qua Unit of Work (sửa ở T10 — `docs/design/architecture.md` D3) |
| Cache | Redis (`StackExchange.Redis`) trực tiếp |
| Auth | `JwtBearer` tự viết, refresh token rotation + reuse detection |
| Payments | Stripe PaymentIntent + webhook idempotent theo `event.id` |
| Tickets / check-in | HMAC-SHA256 signed QR, verify chữ ký tại server khi quét, chống quét lại (offline đã loại — `NR-15`) |
| Test | xUnit + Shouldly + Testcontainers (Postgres + Redis thật) |
| Load test | k6 (Docker image `grafana/k6`) |
| Observability | Serilog + correlation ID → OTel / Prometheus / Grafana |
| Infra | Docker Compose, GitHub Actions, GitHub repo public |
| Config | `appsettings.json` non-secret + user-secrets + biến môi trường |

Quy tắc dùng nhiều tầng: **một hot path chỉ đi qua một tầng truy cập dữ liệu**.

## 14 bài toán concurrency sẽ giải

Đây là bản chất của dự án. Mỗi bài cần một test chứng minh.

| # | Bài toán | Cơ chế dự kiến | Test chứng minh |
|---|---|---|---|
| 1 | Gán trùng ghế (double booking) | conditional `UPDATE` từng ghế | 2 người tranh cùng ghế → đúng 1 thắng, 1 thua `409` |
| 2 | **Đặt N ghế phải all-or-nothing** | một `UPDATE` nhiều dòng có điều kiện, kiểm tra số dòng ảnh hưởng | giữ A,B thành công; C thất bại → **C không bị giữ** |
| 3 | **Deadlock khi tranh ghế chồng nhau** | khoá theo thứ tự cố định (quota trước, rồi `seat_id` tăng dần) | 2 người chọn ghế ngược thứ tự, lặp nhiều lần → **0 lỗi `40P01`**; mỗi bên thắng hoặc thua với lý do nghiệp vụ. Test phải đỏ khi bỏ sắp thứ tự (chỉ kiểm "có phản hồi" thì vẫn xanh — Postgres tự huỷ một bên) |
| 4 | Hết hạn chỗ giữ đua với webhook / người mua mới | hết hạn theo **thời gian** (`expires_at > now()` ở `→ Paid`); trả ghế lọc theo `hold_id` | webhook đến ngay sau `expires_at` → không cấp vé, hoàn tiền; không mất ghế, không giữ trùng |
| 5 | Idempotency key race | bản ghi idempotency unique `(customer, key)` lưu kết quả lần đầu | 50 request cùng key → 1 đơn; lần đầu bị 409 thì mọi lần sau cũng 409 |
| 6 | Refresh token reuse | rotation chain + `family_id` | dùng lại token cũ → 401, chain bị revoke |
| 7 | Webhook Stripe trùng / sai thứ tự + hoàn tiền | idempotent theo `event.id`; hoàn tiền qua outbox + `Idempotency-Key` | gửi 3 lần → 1 transition, cấp đúng 1 bộ vé; kill worker hoàn tiền giữa chừng → Stripe hoàn đúng 1 lần |
| 8 | Quét lại QR | khoá 1 lần dùng + `UNIQUE` ở DB | quét 10 lần → 1 check-in |
| 9 | Cache stampede trên sơ đồ ghế | single-flight khi miss | 100 request 1 suất nóng → **đúng 1** lần đọc DB |
| 10 | **Invalidate cache theo từng ghế** | ghi đè trạng thái ghế, đọc bằng phiên bản | đặt 4 ghế → instance khác thấy đúng 4 ghế đã bán |
| 11 | Mở bán phim hot cạn connection pool | admission control + `max_connections` | 500 req/s → không pool timeout |
| 12 | Rate limit nhiều instance | Redis token bucket phân tán | 3 instance, 1 quota → không vượt quota |
| 13 | **Vượt 10 ghế/người/suất bằng yêu cầu song song** (thêm 2026-11-25) | advisory lock `(customer, showtime)` trước khi đếm — ADR-0007 | 20 yêu cầu song song của cùng người → tổng ≤ 10; test đỏ khi bỏ khoá |
| 14 | **Giữ ghế đua với hủy suất** (thêm 2026-11-25, T10) | giữ ghế `FOR KEY SHARE` dòng suất; hủy suất `FOR UPDATE` | 20 lệnh giữ + 1 lệnh hủy song song → 0 đơn chờ trên suất đã hủy |

> #10 và #12 cần nhiều instance — theo `NR-11` (sửa 2026-11-25), chúng được kiểm chứng với 2–3
> instance trong môi trường test/k6, không phải cách vận hành.

Bài toán **#2, #3, #10** là phần mới, sinh ra từ việc đổi sang mô hình seat-based — đây là phần
đáng khoe nhất của repo. **Cơ chế đã chốt ở Gate C** — bản đầy đủ (SQL, phương án bị loại, test, dòng
"đỏ nếu…") là [`docs/design/concurrency.md`](../design/concurrency.md); cột "Cơ chế dự kiến" ở trên là
tóm tắt. Một số cơ chế đổi so với dự kiến: #1–#3 khoá `ORDER BY id FOR UPDATE` rồi `UPDATE` có điều
kiện; #5 không lưu `InProgress` (ADR-0011); #8 không dùng Redis (ADR-0013). Gate D phải có test chứng
minh.

## Ma trận truy vết

Bảng sống của dự án. Mỗi `FR-nn` phải có ít nhất một ticket đáp ứng trước khi mở Gate D.
Requirement nào không map được → hoặc bỏ khỏi requirements, hoặc thêm ticket.

| FR | Khái niệm domain | Quyết định design | Ticket | Trạng thái |
|---|---|---|---|---|
| FR-01 | `Showtime seat` `INV-01` — `Available → Held` bằng điều kiện lệnh | Khoá ghế + `UPDATE` có điều kiện — `concurrency.md` #1, ADR-0002 | T18, T22 | đã có design |
| FR-02 | `Hold seats` — n `Showtime seat` + `Booking` trong một DB transaction (R3, ADR-0005) | Một transaction, quyết định trước khi ghi — #2 | T18, T22 | đã có design |
| FR-03 | Thứ tự khoá: quota → ghế tăng dần theo id (`BR-22`) | `SELECT … ORDER BY id FOR UPDATE`, thứ tự khoá toàn cục — #3 | T18, T22 | đã có design |
| FR-04 | `Booking.expires_at`; `Booking → Expired`; trả ghế theo `hold_id` | Worker `SKIP LOCKED` + trả ghế theo `hold_id`; lấy ghế quá hạn ngay — #4, ADR-0015 | T18, T21, T22 | đã có design |
| FR-05 | `Ticket` chỉ cấp trong transaction `Booking → Paid` (`Ticket` `INV-02`) | Event `BookingPaid` xử lý trong transaction `→ Paid`; `UNIQUE showtime_seat_id` — #7 | T20 | đã có design |
| FR-06 | `Booking` `INV-05` — `→ Paid` cần `expires_at > now()` (ADR-0006) | `→ Paid` với `expires_at > now()` — #4, ADR-0006 | T20, T21 | đã có design |
| FR-07 | `Ticket` `Issued → CheckedIn`; unique `ticket_id` ở `Check-in` | `PRIMARY KEY (ticket_id)` + `ON CONFLICT` — #8, ADR-0013 | T22, T24 | đã có design |
| FR-08 | Hệ quả của `Ticket` `INV-02` (không tồn tại vé cho đơn chưa trả) | Không có vé ⇒ `422 TICKET_INVALID` — `api-contract.md` Ticketing | T22, T24 | đã có design |
| FR-09 | `Booking` `INV-03` (≤ 6), `INV-10` (≤ 10, khoá quota — ADR-0007) | `CHECK seat_weight`; advisory lock (người, suất) rồi đếm — #13 | T18, T22 | đã có design |
| FR-10 | `Booking` `INV-09`, `Ticket` `INV-07` — kiểm tra chủ sở hữu | `404` cho tài nguyên của người khác; `tickets.customer_id` — `api-contract.md` | T16, T18, T24 | đã có design |
| FR-11 | `Movie` `INV-11` — suy ra từ `Showtime` `OnSale` | `GET /api/movies` — truy vấn suy ra, cache 30s | T13, T17, T23 | đã có design |
| FR-12 | `Showtime`, `Ticket price` (`Showtime` `INV-01`) | `GET /api/movies/{id}/showtimes`; giá ở `scheduling.ticket_prices` | T13, T17, T23 | đã có design |
| FR-13 | `Showtime seat` (không gồm `Blocked`), `Available seat count` | `GET /api/showtimes/{id}/seats` (module `Inventory`) — cache #9, #10, ADR-0012 | T13, T17, T23 | đã có design |
| FR-14 | `Hold seats`; `Booking.expires_at` = lúc giữ + 15 phút | `expires_at` = `now()` + 15 phút, chung cho đơn và ghế — #2 | T18 | đã có design |
| FR-15 | `Showtime seat` `INV-01` — điều kiện lệnh, báo ghế nào | `SELECT … FOR UPDATE` trả ghế nào thất bại ⇒ `409 unavailableSeatIds` — #2 | T18 | đã có design |
| FR-16 | `Booking → Paid`; `Booking line` giá đã chốt; `Ticket` | `POST /api/bookings/{id}/payment` + webhook; giá đã chốt ở `booking_lines.price` | T13, T20 | đã có design |
| FR-17 | `Ticket` `INV-06` | `GET /api/tickets`; mã QR tính lại, không lưu | T13, T24 | đã có design |
| FR-18 | `Booking → Cancelled`; trả ghế theo `hold_id` | `POST /api/bookings/{id}/cancel` — trả ghế theo `hold_id`, cùng transaction | T13, T18, T21 | đã có design |
| FR-19 | `Booking.expires_at` — tác dụng phụ qua outbox | Outbox `ExpiryReminderDue` hẹn `expires_at - 5 phút` | T21 | đã có design |
| FR-20 | `Room`, `Room seat`, `Seat type`; `Showtime seat` `Available ↔ Blocked` | `scheduling.room_seats`; chặn ghế có điều kiện trạng thái nguồn | T17 | đã có design |
| FR-21 | `Showtime`, `Ticket price`; exclusion constraint `BR-20` | Exclusion constraint + đệm 15 phút; đủ giá mọi loại ghế | T17 | đã có design |
| FR-22 | `Showtime` `NotOnSale → OnSale` | `UPDATE … WHERE status = 'NotOnSale'` | T17 | đã có design |
| FR-23 | R2 — bốn con số tình trạng bán | Đếm theo `showtime_id` (ghế `Held` quá hạn tính `available`) — endpoint của `Inventory` | T13, T17 | đã có design |
| FR-24 | `Showtime` `OnSale → Cancelled` + `Booking → Cancelled` + `Payment intent → RefundPending` (ranh giới bị cắt #4) | Một transaction (showtime `FOR UPDATE`) + outbox hoàn tiền — #14, ADR-0006 | T20, T21 | đã có design |
| FR-25 | Điều kiện tiên quyết check-in — 5 kết quả xác định | Năm kết quả quét, mã lỗi — `api-contract.md` Ticketing | T24 | đã có design |
| FR-26 | (đã loại khỏi scope — NR-15) | (không áp dụng) | — | **loại bỏ ở T03** |
| FR-27 | `Check-in` — đếm (`Ticket` `INV-04`) | `COUNT` trên `check_ins` theo `showtime_id` | T24 | đã có design |
| FR-28 | `Administrator`; `Movie` (danh mục chung — `BR-32`); `Showtime` `INV-04` | `/api/admin/movies`; vai trò `Administrator` ở `/api/manager/showtimes` | T17 | đã có design |
| FR-29 | `User` `INV-01..03` | `identity.role_changes` + `role_version` vô hiệu cache — ADR-0014 | T16 | đã có design |
| FR-30 | **Không có khái niệm domain** — yêu cầu vận hành, kiểm chứng bằng số đo | `/metrics` + Grafana; counters ở #11 | T23, T25 | đã có design |
| FR-31 | Lịch sử `Booking` (`INV-07`) + lịch sử `Payment event` | `GET /api/admin/bookings/{id}/history` ghép `booking_history` + `payment_events` | T20 | đã có design |
| FR-32 | `Showtime` `INV-04` — chủ sở hữu | `showtimes.owner_id`; `404` khi không phải chủ | T17 | đã có design |
| FR-33 | Vai trò `User` (`Identity` `INV-06`) | Chính sách vai trò `GateStaff` / `Administrator` | T16, T24 | đã có design |
| FR-34 | `Showtime` `INV-02` | `404` cho suất chưa mở bán; giữ ghế đọc suất `FOR KEY SHARE` | T17 | đã có design |
| FR-35 | `Gate assignment` `INV-08` | `gate_assignments` PK; kiểm trước khi xét vé ⇒ `403` | T24 | đã có design |
| FR-36 | Bảng "transitions bị từ chối" của mọi máy trạng thái | `application/problem+json` với `code` + `action`; cơ chế `Result` → problem+json (ADR-0016) | T13, T28 | đã có design |
| FR-37 | `Booking` `INV-08` — `Idempotency key` | Idempotency trong transaction + savepoint — #5, ADR-0011 | T19, T22 | đã có design |
| FR-38 | `Payment intent` `INV-01` — unique `event.id` | `UNIQUE (stripe_event_id)` + điều kiện trạng thái — #7 | T19, T20, T22 | đã có design |
| FR-39 | `Payment intent` `→ RefundPending → Refunded`; `Refund` (`INV-02`) | `RefundPending` + outbox + `Idempotency-Key = refunds.id` — #7 | T20, T21, T22 | đã có design |
| FR-40 | `Gate assignment` `INV-09` | `gate_assignments`; kiểm chủ suất (R7) + vai trò `GateStaff` | T24 | đã có design |
| FR-41 | `Showtime` `INV-02` — giờ bắt đầu (`BR-31`) | Kiểm `starts_at` trong transaction giữ ghế ⇒ `409 SHOWTIME_STARTED` | T18 | đã có design |

> **Cột "Ticket"** lấy từ trường `Traces to:` của từng ticket; mọi `FR` trong scope đều có ít nhất
> một ticket (Gate C). **Cột "Quyết định design"** điền 2026-11-25 khi đóng Gate C — chi tiết ở
> `docs/design/`.
>
> **Cột "Trạng thái"** nói mức độ hoàn thành của việc **đáp ứng** requirement, không phải việc
> viết tài liệu. `đã có domain` = đã có khái niệm ở Lớp 2; `đã có design` = có cơ chế ở Lớp 3, chưa có code + test (Lớp 4).

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
| T10 | Architecture và module boundary | `co-design` | T06, T07, T08 | — | **xong 2026-11-25** |
| T11 | Data model (`inventory.showtime_seats`, bảng ghế) | `co-design` | T07 | — | **xong 2026-11-25** |
| T12 | Concurrency design: 14 bài toán → cơ chế → test | `explain` + `co-design` | T07, T08 | — | **xong 2026-11-25** |
| T13 | API contract và idempotency contract (endpoint ghế) | `co-design` | T09, T12 | FR-11, FR-12, FR-13, FR-16, FR-17, FR-18, FR-23, FR-36 | **xong 2026-11-25** |
| T14 | Bộ ADR | `co-design` | T10, T11, T12, T13 | — | **xong 2026-11-25** |

### Gate D — Implementation (T15–T28)

| # | Ticket | Mode | Blocked by | Vấn đề concurrency |
|---|---|---|---|---|
| T15 | Skeleton + Docker Compose + CI | `ai-implements` | T14 | — | **xong 2026-11-26** |
| T28 | Building blocks: Result, problem+json, exception handler (chạy **trước** T16) | `pair-tdd` | T15 | — |
| T16 | Identity: register, login, JWT, refresh rotation | `you-implement-i-review` | T15, T28 | #6 |
| T17 | Phòng chiếu, sơ đồ ghế, lịch chiếu và cache | `you-implement-i-review` | T15 | #9 #10 |
| T18 | Đặt vé: giữ n ghế all-or-nothing | `pair-tdd` | T15, T17 | #1 #2 #3 |
| T19 | Idempotency middleware | `you-implement-i-review` | T18 | #5 |
| T20 | Stripe PaymentIntent và webhook | `pair-tdd` | T18, T19 | #7 |
| T21 | Worker hết hạn chỗ giữ ghế + dispatcher outbox | `you-implement-i-review` | T18, T20 | #4 |
| T22 | Bộ test concurrency | `you-implement-i-review` | T18, T19, T20, T21 | #1 #2 #3 #4 #5 #7 #8 |
| T23 | Load test (k6) và baseline — mở bán phim hot | `pair-tdd` | T22 | #11 #12 |
| T24 | Check-in: QR HMAC, phân công cổng, chống quét lại | `pair-tdd` | T20 | #8 |
| T25 | Observability: log, metric, trace, dashboard | `you-implement-i-review` | T23 | #11 #12 |
| T26 | README + ADR + báo cáo benchmark + CV | `co-design` | T22, T23, T24, T25 | — |
| T27 | Frontend người xem tối thiểu (`frontend/`) | `you-implement-i-review` | T22 | — |

**Thứ tự cứng:** T10 chỉ mở sau T06–T08. T11 chỉ mở sau T07. T18 chỉ mở sau T10 + T11 + T12.

**Cắt scope khi trễ tiến độ:** T27 (frontend) đầu tiên — điểm CV nằm ở concurrency chứ không ở UI;
khi chưa có frontend thì demo bằng OpenAPI. Sau đó T24 (check-in), rồi T25 (Grafana), rồi T23 (k6 mở rộng).
Lõi là T15–T22 + T26 + T28.

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
| 2026-11-21 | **`Seat hold` là aggregate root của tồn kho ghế (≤6 ghế), KHÔNG phải `Showtime`** *(đã bị thay 2026-11-25 — ADR-0005)* | `BR-02` (giữ n ghế all-or-nothing) không được phép bị cắt: nếu mỗi ghế là một aggregate thì all-or-nothing phải dùng compensating transaction ⇒ có thời điểm hệ thống vi phạm `BR-02`. Aggregate lớn (`Showtime` chứa 200 ghế) lại gây contention dồn khi mở bán phim hot | `docs/domain/aggregates/`, `docs/adr/0004-seat-hold-aggregate-boundary.md` |
| 2026-11-21 | **`BR-01` ép bằng điều kiện lệnh, không bằng khoá đọc-sửa-ghi** | `UPDATE ... WHERE status = 'Available'` không có cửa sổ TOCTOU; hai lệnh đồng thời trên cùng một ghế thì chỉ một thắng, lệnh còn lại ảnh hưởng 0 dòng và phải coi là thất bại | `docs/domain/aggregates/02-inventory.md` |
| 2026-11-21 | **`BR-08` (10 ghế/người/suất) không bảo vệ được bằng aggregate — chấp nhận cửa sổ race** *(đã bị thay 2026-11-25 — ADR-0007)* | Nó đếm theo *người* qua *nhiều đơn* ⇒ tích luỹ xuyên nhiều aggregate, về nguyên tắc không thể nằm trong một aggregate. Hậu quả của cửa sổ race là *người dùng tự gian lận*, không phải *bán quá số ghế có thật* — nên chấp nhận được, và rẻ hơn một điểm tranh chấp toàn cục ở tầng DB | `docs/domain/aggregates/03-booking.md` |
| 2026-11-21 | **`BR-10` (1 vé = 1 check-in) ép bằng ràng buộc duy nhất, không cần transaction xuyên nhiều vé** | Nó là invariant về **một vé**: hai lượt quét đồng thời là hai transaction cùng tranh một dòng. Bài học: một invariant trông "toàn cục" có thể hoá thành "cục bộ" nếu đặt ranh giới aggregate đúng chỗ | `docs/domain/aggregates/05-ticketing.md` |
| 2026-11-21 | **`BR-20` (không có suất chiếu chồng nhau) ép bằng exclusion constraint, nằm ngoài ranh giới aggregate** | Đây là quan hệ *giữa hai `Showtime` khác nhau* — aggregate không thể ghi ra ngoài ranh giới của mình. Ràng buộc loại trừ ở tầng lưu trữ mạnh hơn kiểm tra ứng dụng, vì chặn được cả hai lệnh tạo suất chạy đồng thời | `docs/domain/aggregates/01-scheduling.md` |
| 2026-11-24 | **Trạng thái "chờ thanh toán" tên là `AwaitingPayment`, không phải `Pending`** | `Pending` đã bị glossary ghi là nên tránh (dễ nhầm với `Held` của ghế). Tên trạng thái là tên sẽ dùng trong code — nên phải chốt sớm, trước khi viết schema | `docs/domain/state-machines.md` |
| 2026-11-24 | **`Payment intent` dùng `Captured` / `Refunded`, KHÔNG dùng `Paid`** | `Paid` đã là trạng thái của `Booking`. Trùng tên ở hai context khiến log dễ đọc sai — "booking `Paid`" và "payment `Paid`" là hai sự kiện khác nhau và không xảy ra cùng lúc | `docs/domain/state-machines.md` |
| 2026-11-24 | **`Payment intent` KHÔNG có trạng thái "thất bại"** | Bám theo `INV-04`: ta chỉ có **một** trạng thái "chờ thanh toán" cho tới khi có kết quả. Kết quả thất bại làm intent bị **bỏ**, đơn đi theo đường `Expired`/`Cancelled` bình thường — không tạo ra trạng thái mới | `docs/domain/state-machines.md` |
| 2026-11-24 | **Vé bị vô hiệu do hủy suất chiếu là TÁC DỤNG PHỤ, không phải trạng thái thứ ba của `Ticket`** | Máy `Ticket` chỉ có `Issued` → `CheckedIn`. "Vé này còn dùng được không" là **điều kiện tiên quyết** của transition check-in, không phải trạng thái. Thêm trạng thái thứ ba sẽ tạo ra một đường chuyển trạng thái mà không ai dùng tới | `docs/domain/state-machines.md` |
| 2026-11-24 | **Race "hết hạn đúng lúc webhook đến": ai chuyển trạng thái trước thì thắng, chốt được trên giấy** *(đã bị thay 2026-11-25 — quyết định bằng thời gian, ADR-0006)* | Cả hai luồng ghi vào **cùng một trạng thái nguồn** (`AwaitingPayment`) của cùng một aggregate bằng **điều kiện lệnh** ⇒ chỉ một lệnh thắng, nhánh thua định nghĩa sẵn. Không cần prototype vì kết quả **không** phụ thuộc thứ tự **đọc** | `docs/domain/state-machines.md` |
| 2026-11-24 | **Ranh giới bị cắt thứ tư: hủy suất chiếu** | `OnSale → Cancelled` kéo theo vô hiệu vé (`Ticketing`) + hoàn tiền mọi đơn (`Payment`) — cắt qua ba context. Khác ba chỗ đã biết ở T06 ở chỗ nó chạy **nhiều đơn**, không phải một aggregate: một suất có thể có hàng trăm đơn đã trả tiền | `docs/domain/state-machines.md` |
| 2026-11-24 | **Mô hình đọc chéo context: qua interface/contract, không đọc trực tiếp bảng** | Giữ ranh giới module rõ ràng ngay từ design stage; tất cả đọc dữ liệu xuyên context đều có "chủ sở hữu" và "khách hàng". Tránh tình trạng "nhanh hơn" thành lý do lợn lại ranh giới ở tầng code | `docs/domain/cross-context-contracts.md` |
| 2026-11-24 | **Hợp đồng R1 và R2 tách riêng (không gộp bidirectional)** | Giúp nhìn rõ R2 (`Inventory → Scheduling` đọc-duy-nhất 4 con số) là ứng viên tách service hàng đầu. Cách gom hợp đồng ảnh hưởng tới cách nhìn service boundary sau này | `docs/domain/cross-context-contracts.md` |
| 2026-11-24 | **Q27 decision: `Booking.Paid` KHÔNG chuyển sang `Refunded` khi hủy suất** | Dấu vết hoàn tiền ở `Payment intent.Refunded`. Lý do: `Booking` đã kết thúc chu kỳ chính (giữ → thanh toán → cấp vé); hoàn tiền do hủy suất là sự kiện ngoại lai. Giữ trạng thái đơn giản, không tạo ranh giới trạng thái mới | `docs/domain/cross-context-contracts.md`, Q27 |
| 2026-11-24 | **8 cặp thuật ngữ khác nghĩa + Anti-Corruption Layer cho mỗi cặp** *(sửa 2026-11-25: còn 7 cặp — bỏ `Seat hold` vs `Booking` và cờ "đã cấp vé", thêm `hold_id` vs `Booking id`)* | `Room seat` vs `Showtime seat`, `Cancelled` (booking) vs `Cancelled` (showtime), giá gốc vs giá frozen, `Payment intent` (ours) vs `payment_intent` (Stripe), `Sold` vs `Ticket`, cùng tên `AwaitingPayment` ở hai context khác nhau. Ghi tường minh nơi dịch | `docs/domain/cross-context-contracts.md` |
| 2026-11-25 | **Review toàn bộ T01–T09 trước khi đóng Gate B** | Phát hiện 7 vấn đề nghiêm trọng + 17 mâu thuẫn giữa các file. Các quyết định dưới đây đều từ review này | — |
| 2026-11-25 | **`Showtime seat` là aggregate root; bỏ aggregate `Seat hold`** — *thay quyết định 2026-11-21* | Ghế tồn tại trước và sống qua nhiều lần giữ ⇒ không thể là entity bên trong một lần giữ. Lý do bác phương án (b) sai: một DB transaction cập nhật N dòng vẫn atomic. `Seat hold` trùng 1:1 với `Booking` | ADR-0005 (thay ADR-0004) |
| 2026-11-25 | **Hết hạn theo thời gian**: `→ Paid` cần `expires_at > now()` — *thay quyết định race 2026-11-24* | "Ai chuyển trước thắng" cho webhook đến sau hạn nhưng trước khi worker quét vẫn `Paid` — trái `BR-04` | ADR-0006 |
| 2026-11-25 | **Thanh toán thất bại không kết thúc intent** | J-04.4 cần thử lại được; unique `booking_id` không cho tạo intent thứ hai; Stripe cho dùng lại PaymentIntent | ADR-0006 |
| 2026-11-25 | **Hoàn tiền qua outbox: thêm `RefundPending`, gọi Stripe với `Idempotency-Key`** | Gọi Stripe không nằm trong DB transaction được; ≤ 1 `Refund` không chặn được worker gọi Stripe hai lần | ADR-0006 |
| 2026-11-25 | **Hủy suất = 1 transaction ngắn + N yêu cầu hoàn bất đồng bộ; huỷ luôn các đơn `AwaitingPayment`** | Trả lời Q29. Đơn đang chờ thanh toán khi hủy suất trước đây không được định nghĩa | ADR-0006 |
| 2026-11-25 | **`BR-08` ép bằng khoá (người xem, suất), đếm cả `Held` + `Sold`** — *thay quyết định 2026-11-21* | Lý do chấp nhận race cũ sai: luật chống phe vé (người hại là người khác), khoá theo cặp không phải điểm tranh chấp toàn cục | ADR-0007 |
| 2026-11-25 | **Sinh `Showtime seat` lúc tạo suất, không lúc mở bán** | `Blocked` chỉ còn một nguồn sự thật (`Inventory`); quản lý rạp chặn ghế được trước khi mở bán | `docs/domain/aggregates/01-scheduling.md` |
| 2026-11-25 | **Bỏ cờ "đã cấp vé"** (đóng Q26) | `Paid` và cấp vé cùng một transaction ⇒ "Paid chưa có vé" không thể xảy ra | `docs/domain/aggregates/03-booking.md` |
| 2026-11-25 | **`Gate assignment` = nhân viên ↔ suất, thuộc `Ticketing`; thêm `FR-40`** | Trước đó ở hai context với hai nghĩa; không có FR nào để tạo phân công | `docs/domain/aggregates/05-ticketing.md` |
| 2026-11-25 | **Thêm `FR-41` / `BR-31`: ngừng bán từ giờ bắt đầu chiếu** | `BR-13` chỉ kiểm `OnSale` — bán được vé cho suất đã chiếu xong | `docs/prd/04-business-rules.md` |
| 2026-11-25 | **Thêm quan hệ R9 `Scheduling → Booking, Inventory`** | `Booking` đọc giá/trạng thái/giờ từ `Scheduling` mà context map không có quan hệ nào | `docs/domain/context-map.md` |
| 2026-11-25 | **Vai trò không đọc từ JWT; hỏi `Identity & Access`, cache có vô hiệu hoá** | Thu quyền "có hiệu lực ngay" (`BR-26`) mâu thuẫn với vai trò nằm trong token stateless | `docs/domain/aggregates/06-identity-access.md` |
| 2026-11-25 | **Idempotency của giữ ghế: bản ghi riêng `(customer, key)` lưu cả kết quả từ chối** | Unique trên `Booking` chỉ lưu lần thành công ⇒ lần đầu 409, lần sau có thể 201 | `docs/domain/aggregates/03-booking.md` |
| 2026-11-25 | **`NR-11` sửa: 1 instance khi vận hành, 2–3 instance trong test cho #10, #12** | `NR-11` cũ mâu thuẫn với bài toán #10, #12 | `docs/prd/02-non-requirements.md` |
| 2026-11-25 | **ADR-0003 → `accepted`** | Ba câu hỏi revalidate đã trả lời; ranh giới aggregate tách sang ADR-0005 | ADR-0003 |
| 2026-11-25 | **Một `GLOSSARY.md` ở root, chia mục theo context** (không dùng `GLOSSARY-MAP.md`) | Một deployable, chưa có `src/<context>/` để đặt glossary riêng; từ trùng tên ghi kèm context, vd `Cancelled (Booking)` / `Cancelled (Showtime)` | `GLOSSARY.md` |
| 2026-11-25 | **Đóng Gate B, mở Gate C** | Exit checklist Gate B tick đủ 8/8 | `docs/plan/gate-c-design.md` |
| 2026-11-25 | **6 module, mỗi module `<Module>` + `<Module>.Contracts`** (T10) | Một module một context; ranh giới project là ranh giới compile-time | `docs/design/architecture.md` D1, D2 |
| 2026-11-25 | **Mỗi module một `DbContext`, chung connection + transaction qua UoW** — *thay "1 `DbContext`"* (T10, Q20) | Một `DbContext` chung cho mọi module `Set<T>()` bảng của nhau; 5 ranh giới bị cắt vẫn là một transaction Postgres thật | `docs/design/architecture.md` D3 — đề xuất ADR |
| 2026-11-25 | **Gọi giữa module theo use case: interface / event trong tx / outbox** (T10, AI đề xuất) | Cần kết quả + atomic ⇒ interface (downstream → upstream); không cần kết quả + atomic ⇒ event in-process; ngoài DB ⇒ outbox | `docs/design/architecture.md` D4 |
| 2026-11-25 | **Hướng compile-time: `Booking → Payment.Contracts`, `Ticketing → Booking.Contracts`; R2 do `Inventory` phục vụ** (T10, AI đề xuất) | R4, R6 hai chiều về dữ liệu; chọn hướng để đồ thị module không vòng | `docs/design/architecture.md` D9 |
| 2026-11-25 | **Outbox một bảng mỗi module** (T10, AI đề xuất) | Mỗi bảng một chủ; tách service thì outbox đi theo module | `docs/design/architecture.md` D8 |
| 2026-11-25 | **Đổi hợp đồng làm việc: AI tự quyết mọi quyết định spec**; người học đọc spec rồi implement | Người học chưa đủ nền để đánh giá đánh đổi thiết kế; học bằng đọc spec có lý do rồi viết code | `AGENTS.md` |
| 2026-11-25 | **Rà toàn bộ docs sau T10** — sửa ~25 mâu thuẫn chéo file | Xem mục log 2026-11-25 "Rà toàn bộ docs" | — |
| 2026-11-25 | **`Payment intent`: `Unpaid` / `Closed`** (đóng Q28) | Trùng tên `AwaitingPayment` với `Booking`; intent của đơn hết hạn không có trạng thái để đi | ADR-0010 |
| 2026-11-25 | **Phòng thuộc quản lý rạp tạo nó; phim là danh mục chung của quản trị viên** (`BR-32`) | Trước đó không luật nào nói ai được xếp suất vào phòng nào, ai tạo phim | `docs/prd/04-business-rules.md` |
| 2026-11-25 | **Khoảng đệm giữa hai suất = 15 phút** (đóng Q22) | Hằng số toàn hệ thống để exclusion constraint đơn giản | `BR-20` |
| 2026-11-25 | **Check-in có 5 kết quả: hợp lệ / đã dùng / không hợp lệ / suất khác / suất đã hủy** | "Vé chưa trả tiền" không thể tồn tại (vé chỉ cấp khi `Paid`) — thay bằng "suất đã hủy" | `FR-25`, `docs/domain/state-machines.md` |
| 2026-11-25 | **ADR-0008, ADR-0009** cho quyết định T10 | Khó đảo ngược, đi ngược quyết định "1 `DbContext`" cũ | ADR-0008, ADR-0009 |
| 2026-11-25 | **T11 Data model**: `uuid` v7, trạng thái `text` + `CHECK`, tiền `bigint` VND, không FK xuyên schema, bảng lịch sử `REVOKE UPDATE/DELETE` | Ép invariant ở DB; ranh giới module thấy trong SQL | `docs/design/data-model.md` |
| 2026-11-25 | **Giữ n ghế = `SELECT … ORDER BY id FOR UPDATE` rồi `UPDATE` có điều kiện** (ADR-0002 → accepted) | Thứ tự khoá của `UPDATE … ANY` là chi tiết planner; `FR-15` cần biết ghế nào thất bại | ADR-0002 |
| 2026-11-25 | **Quota = advisory lock theo transaction** | Không cần bảng quota, tự nhả, trùng băm chỉ chậm hơn | ADR-0007 |
| 2026-11-25 | **Idempotency: chiếm key trong transaction nghiệp vụ, savepoint giữ kết quả lỗi** | Không kẹt key khi tiến trình chết; trùng đang chạy thì chờ rồi nhận cùng kết quả (`FR-37`) | ADR-0011 |
| 2026-11-25 | **Cache sơ đồ ghế: ghi đè theo ghế có version, qua outbox; single-flight hai tầng** | Không hot row, không miss mỗi lần giữ ghế, snapshot cũ không đè mới | ADR-0012 |
| 2026-11-25 | **Check-in chỉ bằng PK ở DB — bỏ Redis `SETNX`** | Redis chỉ thêm chỗ lệch | ADR-0013 |
| 2026-11-25 | **Refresh rotation nghiêm ngặt, không ân hạn** | Không phân biệt được gửi đúp với kẻ trộm | ADR-0014 |
| 2026-11-25 | **Lệnh giữ được lấy ghế `Held` quá hạn** (đóng Q25) | `SC-07`: hết hạn là trống ngay, không chờ worker | ADR-0015 |
| 2026-11-25 | **Bài toán #14 — giữ ghế ↔ hủy suất**: `FOR KEY SHARE` vs `FOR UPDATE` | Phát hiện ở T10 | `docs/design/concurrency.md` |
| 2026-11-25 | **Không `SERIALIZABLE`, không distributed lock** (đóng Q02, Q06) | Mọi bất biến đã có cơ chế ở DB; Redis chỉ tối ưu | `docs/design/concurrency.md` |
| 2026-11-25 | **API**: problem+json có `code` + `action`; `404` cho tài nguyên người khác; `409` vs `422`; `Idempotency-Key` bắt buộc chỉ ở giữ ghế | `FR-10`, `FR-36`; endpoint khác tự nhiên idempotent | `docs/design/api-contract.md` |
| 2026-11-25 | **Đóng Gate C, mở Gate D**; đồng bộ ticket T15–T26 với design | Exit checklist Gate C tick đủ | `docs/plan/gate-d-implementation.md` |
| 2026-11-25 | **Bố cục repo `backend/` + `frontend/`**; thêm T27 frontend người xem (AI quyết định) | PRD có giao diện người xem (`NR-06`, `NR-14`) nhưng design chưa có chỗ cho nó; mỗi toolchain một thư mục gốc | `docs/design/architecture.md` D2 |
| 2026-11-26 | **Building blocks** (AI quyết định; người học phát hiện thiếu): `Result` tự viết, mọi lỗi là problem+json qua một cơ chế, không success envelope; mỗi use case một handler, không mediator; transaction tường minh trong handler; repository theo aggregate chỉ phía ghi; `TimeProvider` thay `IClock`. Thêm T28 chạy trước T16 | Chỉ có *định dạng* lỗi (T13), chưa có *cơ chế*; "commit khi thành công" tự động sẽ sai với ADR-0011 | ADR-0016, ADR-0017, `docs/design/building-blocks.md`, `coding-conventions.md` |

## Câu hỏi còn mở

| Câu hỏi | Quyết ở gate nào |
|---|---|
| Một suất chiếu có hàng trăm ghế — aggregate có phải chia nhỏ không? | ~~Gate B (T07, ADR-0003)~~ → **đã trả lời: KHÔNG — mỗi `Showtime seat` là aggregate root (ADR-0005, 2026-11-25; bản 2026-11-21 chọn `Seat hold` đã bị thay)** |
| Giới hạn "10 ghế mỗi người mỗi suất" có nằm trong cùng aggregate với tồn kho ghế không? Nó đếm theo **người**, tồn kho ghế đếm theo **ghế** | ~~Gate B (T07, T09)~~ → **đã trả lời: KHÔNG nằm trong một aggregate — ép bằng khoá (người xem, suất), không còn race (ADR-0007, 2026-11-25)** |
| Check-in thuộc bounded context nào — cùng context với vé, hay context riêng? | ~~Gate B (T06)~~ → **đã trả lời 2026-11-20: `Ticketing` giữ cả vé và check-in** |
| Ranh giới aggregate quanh thanh toán và hoàn tiền (`FR-39`) | ~~Gate B (T07, T08)~~ → **đã trả lời 2026-11-24: `Payment intent` là aggregate root riêng ở `Payment`; hoàn tiền là transition `Captured → Refunded` của nó, còn quyết định "có hoàn không" dựa trên cả tiền đã thu lẫn trạng thái đơn; xem `docs/domain/state-machines.md`** |
| Đơn `Paid` khi hủy suất chiếu có cần trạng thái `Refunded` riêng không | ~~Gate B (T08, T09)~~ → **đã trả lời 2026-11-24: KHÔNG — `Booking` giữ `Paid`, dấu vết hoàn tiền ở `Payment intent.Refunded`; xem `docs/domain/cross-context-contracts.md`** |
| ~~Giá vé theo loại ghế có đổi giữa các suất của cùng một phim không?~~ | → **đã trả lời: CÓ — giá thuộc (suất, loại ghế)** |
| Ghế `Blocked` (vệ sinh, dành riêng) có được bán online không, ai quyết định? | ~~Gate B (T07, T08)~~ → **đã trả lời 2026-11-20: KHÔNG bán lại online; quản lý rạp quyết định; xem `BR-11`** |
| ~~Có cần `SERIALIZABLE` ở đâu không?~~ | → **đã trả lời: không** (`concurrency.md`) |
| Có tách module thành service riêng không, và ranh giới đo được ở đâu? | Sau T23 |
| ~~Hold TTL cố định hay cấu hình theo suất chiếu?~~ | → **đã trả lời: cố định 15 phút** (`api-contract.md`) |
| ~~Offline check-in sync dùng cơ chế nào?~~ | **Đã đóng — loại khỏi scope (`NR-15`)** |
| ~~Module tham gia chung DB transaction thế nào khi không đọc bảng của nhau? (Q20)~~ | → **đã trả lời 2026-11-25 (T10): mỗi module một `DbContext`, chung connection + transaction qua UoW; xem `docs/design/architecture.md` D3** |
| ~~Có cần distributed lock ở đâu không?~~ | → **đã trả lời: không** (`concurrency.md`) |
| Khi nào chuyển tài liệu sang tiếng Anh để đưa lên CV? | T26 |
| ~~Có dùng `teach` cho lý thuyết concurrency không?~~ | → **không** — phần giảng nằm trong `concurrency.md` |
