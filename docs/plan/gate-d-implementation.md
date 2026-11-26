# Gate D — Implementation

Biến design thành **code chạy được và test chứng minh được**. Người học viết production code; AI in
hướng dẫn, viết test đỏ ở ticket `pair-tdd`, review (`AGENTS.md`). Quy trình mỗi ticket:
[`../agents/dev-workflow.md`](../agents/dev-workflow.md).

**Đã có** (skeleton, xong): solution `backend/` với 6 module + `.Contracts`, `Shared.Abstractions` /
`Shared.Infrastructure`, Unit of Work một connection + một transaction (ADR-0008), architecture test quy
tắc phụ thuộc, test UoW trên Testcontainers, Docker Compose (Postgres + `btree_gist`, Redis, pgAdmin),
CI GitHub Actions, endpoint `/health`.

## Milestone và thứ tự

Thứ tự trong bảng là thứ tự thực hiện. Ticket: `.scratch/advanced-ticket-system/issues/<mã>-<slug>.md`.

### M0 — Nền móng: mọi setup trước khi chạm vào module nào

Khuôn chung cho cả 6 module ([`../design/building-blocks.md`](../design/building-blocks.md),
ADR-0016…0018): xong M0 thì mỗi module chỉ còn viết nghiệp vụ, không phát minh hạ tầng.

**Demo:** một **module mẫu chỉ có trong test** (aggregate + value object + handler + endpoint + migration)
đi qua đủ khuôn — request sai ⇒ `400` problem+json có `traceId` tìm được trong log JSON; lỗi nghiệp vụ ⇒
`409`; domain event được publish trong transaction và rollback cùng nó; endpoint cần vai trò trả `401` /
`403` đúng; arch test xanh; CI xanh kèm format check và coverage.

| Mã | Ticket | Mode | Blocked by | Bài toán |
|---|---|---|---|---|
| M0-01 | Results: `Result`, `Result<T>`, `Error`, `ErrorType`, `PagedResponse<T>` | `pair-tdd` | — | — |
| M0-02 | Domain primitives: `Entity`, `AggregateRoot` + domain event, `ValueObject`, typed id | `pair-tdd` | M0-01 | — |
| M0-03 | Application primitives: handler interface, đăng ký theo quét assembly, decorator log/trace, event bus in-process | `pair-tdd` | M0-02 | — |
| M0-04 | Persistence foundation: `ModuleDbContext` + quy ước snake_case / typed id, chiến lược migration, Dapper | `you-implement-i-review` | M0-02 | — |
| M0-05 | Web foundation: pipeline lỗi problem+json, JSON options, `ApiControllerBase`, OpenAPI, `TimeProvider` | `pair-tdd` | M0-01 | — |
| M0-06 | Auth foundation: xác thực JWT, policy vai trò, `ICurrentUser`, contract vai trò | `you-implement-i-review` | M0-03, M0-05 | — |
| M0-07 | Observability foundation: log JSON, correlation ID, `/health/live` + `/health/ready` | `you-implement-i-review` | M0-05 | — |
| M0-08 | Test foundation: `TicketSystem.Testing` (fixture, reset DB, auth, concurrency helper) + arch test lớp | `pair-tdd` | M0-04, M0-06, M0-07 | — |
| M0-09 | Tooling & CI: analyzers, format check, báo cáo test + coverage, Mailpit, script, `backend/README.md` | `ai-implements` | M0-08 | — |

### M1 — Walking skeleton

**Demo:** test E2E `J01_happy_path` — quản trị viên tạo phim, quản lý rạp tạo phòng + suất + mở bán,
người xem đăng ký → xem sơ đồ ghế → giữ 2 ghế → thanh toán Stripe test mode → có 2 vé.

| Mã | Ticket | Mode | Blocked by | Bài toán |
|---|---|---|---|---|
| M1-01 | Identity: đăng ký, đăng nhập (phát JWT), `/me`, hiện thực contract vai trò, seed tài khoản | `you-implement-i-review` | M0-09 | — |
| M1-02 | Scheduling: phim, phòng, suất, giá, mở bán, sinh ghế của suất, seed | `you-implement-i-review` | M1-01 | — |
| M1-03 | Đọc công khai: phim, lịch chiếu, sơ đồ ghế (đọc DB), availability, chặn ghế | `you-implement-i-review` | M1-02 | — |
| M1-04 | Giữ n ghế all-or-nothing | `pair-tdd` | M1-03 | #1 #2 |
| M1-05 | Thanh toán + webhook + cấp vé, E2E J-01 | `pair-tdd` | M1-04 | #7 (một phần) |

### M2 — Đúng dưới tranh chấp

**Demo:** `dotnet test --filter Category=Concurrency` xanh; mỗi test có phép thử âm tính
(`Concurrency:UnsafeVariant`) cho ra đỏ.

| Mã | Ticket | Mode | Blocked by | Bài toán |
|---|---|---|---|---|
| M2-01 | Thứ tự khoá, quota 10 ghế, lấy ghế giữ quá hạn | `pair-tdd` | M1-04 | #3 #13 |
| M2-02 | Idempotency trong transaction nghiệp vụ | `you-implement-i-review` | M2-01 | #5 |
| M2-03 | Refresh token rotation + reuse detection, logout | `you-implement-i-review` | M1-01 | #6 |
| M2-04 | Quản lý vai trò, thu quyền có hiệu lực ngay | `you-implement-i-review` | M2-03 | — |
| M2-05 | Outbox dùng chung + dispatcher | `you-implement-i-review` | M1-05 | — |
| M2-06 | Worker hết hạn, huỷ đơn, email nhắc hạn | `you-implement-i-review` | M2-01, M2-05 | #4 |
| M2-07 | Thanh toán muộn → hoàn tiền, lịch sử đơn | `pair-tdd` | M2-06 | #4 #7 |
| M2-08 | Hủy suất | `pair-tdd` | M2-07 | #14 |
| M2-09 | Vé QR ký HMAC + phân công cổng | `pair-tdd` | M1-05 | — |
| M2-10 | Quét vé check-in | `pair-tdd` | M2-09 | #8 |
| M2-11 | Kiểm toán bộ test concurrency | `you-implement-i-review` | M2-02, M2-04, M2-08, M2-10 | tất cả |

### M3 — Đo, tối ưu, đo lại

**Demo:** `perf/report.md` có bảng trước / sau (cache, biến thể khoá ghế) kèm điều kiện đo; Grafana cho
thấy pool, lock contention, rate limit khi mở bán phim hot.

| Mã | Ticket | Mode | Blocked by | Bài toán |
|---|---|---|---|---|
| M3-01 | Metric + trace + Prometheus / Grafana | `you-implement-i-review` | M2-11 | — |
| M3-02 | k6 + seed lớn + admission control; **baseline chưa có cache** | `pair-tdd` | M3-01 | #11 |
| M3-03 | Cache sơ đồ ghế theo từng ghế + version | `you-implement-i-review` | M3-02 | #9 #10 |
| M3-04 | Rate limit Redis dùng chung nhiều instance | `you-implement-i-review` | M3-02 | #12 |
| M3-05 | Đo lại, so biến thể khoá ghế, `perf/report.md` | `pair-tdd` | M3-03, M3-04 | — |
| M3-06 | Dashboard + alert | `you-implement-i-review` | M3-05 | — |

### M4 — Ra mắt

**Demo:** đi hết J-01 trên trình duyệt; người lạ đọc README 3 phút là hiểu hệ thống.

| Mã | Ticket | Mode | Blocked by | Bài toán |
|---|---|---|---|---|
| M4-01 | Frontend người xem | `you-implement-i-review` | M2-11 | — |
| M4-02 | README, báo cáo benchmark, CV | `co-design` | M3-05, M4-01 | — |

### Vì sao thứ tự này

- **Walking skeleton trước** (M1): tích hợp 5 module là rủi ro lớn nhất, nên phải lộ ra sớm nhất. Mỗi
  ticket M1 đã làm đúng cơ chế chính (khoá ghế đúng ngay từ M1-04), M2 làm cứng các nhánh còn lại.
- **Test concurrency viết cùng ticket tạo ra tính năng**; M2-11 chỉ kiểm toán.
- **Cache sau baseline** (M3-02 → M3-03): không có số "trước" thì không có câu chuyện tối ưu.
- **Metric trước load test** (M3-01 → M3-02): đo tải mà không nhìn được bên trong là đo mù.
- **Toàn bộ setup ở M0, trước module đầu tiên**: base class domain, handler + event bus, quy ước
  persistence, pipeline lỗi, auth, log, hạ tầng test, CI. Nếu để từng module tự dựng, module đầu tiên
  thành nơi phát minh khuôn và 5 module sau chép theo cả những chỗ sai.

## Câu hỏi còn mở

Xem [`../learning/open-questions.md`](../learning/open-questions.md).

## Exit checklist — Gate D

- [ ] Không bán trùng ghế dưới tranh chấp, **chứng minh bằng test**
- [ ] Giữ n ghế là all-or-nothing; không deadlock khi tranh ghế chồng nhau ngược thứ tự
- [ ] Idempotency ở giữ ghế, webhook, check-in — có test
- [ ] Mỗi bài toán #1–#14 có test xanh và phép thử âm tính đã chạy (`../design/concurrency.md`)
- [ ] Test integration trên Postgres và Redis thật (Testcontainers), không InMemory
- [ ] `perf/baseline.csv` có dòng trước và sau mỗi lần tối ưu; `perf/report.md` có điều kiện đo
- [ ] README có sơ đồ kiến trúc, bảng số liệu, link ADR
- [ ] CI xanh, repo public, không secret trong lịch sử commit
