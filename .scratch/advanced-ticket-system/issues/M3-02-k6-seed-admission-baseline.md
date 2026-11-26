# M3-02: k6 + seed lớn + admission control; baseline chưa có cache

Mode: pair-tdd
Status: ready
Blocked by: M3-01
Traces to: FR-30, FR-11, FR-12, FR-13
Concurrency: #11
Branch: m3-02-k6-seed-admission-baseline

## Mục tiêu
Có **số "trước"**: dựng admission control (bài toán #11), bộ kịch bản tải và dữ liệu lớn, rồi đo khi sơ đồ
ghế **vẫn đọc thẳng DB** (chưa cache — M3-03). Không có số này thì M3-03 không có câu chuyện tối ưu
(`SC-08..SC-11`, `SC-12`).

## Phạm vi
**Trong:**
- **Admission control** cho endpoint ghi (giữ ghế, huỷ, thanh toán): `ConcurrencyLimiter` trước pool, trả
  `503 OVERLOADED` + `Retry-After` qua `IProblemDetailsService` (cùng shape lỗi, B4); pool size, `Timeout`,
  `statement_timeout`, `PermitLimit` / `QueueLimit` theo
  [`concurrency.md`](../../../docs/design/concurrency.md) #11 (khởi điểm 30 / 100 — là **khởi điểm**, không phải kết luận).
  Giá trị đi qua Options + `ValidateOnStart`.
- Cờ `Concurrency:UnsafeVariant=NoAdmissionControl` (chỉ build test) để chạy **đối chứng không có admission control**.
- **Seed lớn** (script lặp lại được, không nằm trong migration): nhiều phim / rạp / suất, một suất "hot" 100 ghế
  (kịch bản #11) và một suất lớn cho kịch bản đọc; quy mô chốt trong PR, ghi vào `perf/README` ngắn.
- **k6 qua Docker** (`grafana/k6`), thư mục `perf/`: kịch bản `browse` (J-01: phim → suất → sơ đồ ghế),
  `hot-release` (500 req/s trong 60s vào suất 100 ghế), `checkin` (quét vé). Ngưỡng k6 (`thresholds`) khớp
  khẳng định của #11.
- `perf/baseline.csv`, **mỗi lần chạy một dòng**: ngày, kịch bản, biến thể, VU, RPS, p50/p95/p99, tỉ lệ lỗi, giá trị
  các counter, ghi chú. Phải có ít nhất: `hot-release` **có** và **không có** admission control; `browse` chưa cache.
- Đo counter bằng `dotnet-counters` (danh sách ở #11) và/hoặc Prometheus của M3-01; sau mỗi lần chạy kiểm
  invariant #1 trên DB (không ghế nào có hai đơn).

**Ngoài:**
- Cache sơ đồ ghế, số "sau" → M3-03, M3-05.
- Rate limit Redis → M3-04 (kịch bản tải ở đây tắt rate limit `hold` hoặc dùng nhiều user để không dính 429).
- Chọn lại `PermitLimit` / `QueueLimit` từ số đo, so biến thể khoá ghế, `perf/report.md` → M3-05.
- Dashboard / alert → M3-06.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — Bài toán #11 (đủ cơ chế, test, danh sách counter) và mục "Hạ tầng test concurrency".
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Bảng mã lỗi toàn cục" (`503`), endpoint giữ ghế, sơ đồ ghế.
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — B4 (429/503 ra qua `IProblemDetailsService`).
- [`open-questions.md`](../../../docs/learning/open-questions.md) — Q32 (ngưỡng limiter; đóng ở M3-05).
- [`01-requirements.md`](../../../docs/prd/01-requirements.md) FR-30; [`00-overview.md`](../../../docs/prd/00-overview.md) SC-08..SC-11.

## Test
`pair-tdd`: AI viết các test này trước (k6 script và seed là mã test, AI được viết); người học viết phần code limiter + cấu hình. Tên test là đề xuất.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Admission_OverPermitAndQueue_Returns503OverloadedWithRetryAfter` (integration, limiter hạ `PermitLimit`/`QueueLimit` nhỏ) | request vượt giới hạn nhận ngay `503`, `code = OVERLOADED`, header `Retry-After`, body problem+json có `traceId` | không có limiter, hoặc `503` thiếu header / sai shape |
| `Admission_WithinLimit_NotRejected` | request trong giới hạn thành công bình thường | limiter chặn nhầm |
| `Admission_ReadEndpoints_NotLimited` | endpoint đọc không đi qua limiter ghi | đọc bị `503` |
| k6 `hot-release` (threshold) | mọi response thuộc `{201, 409, 503}`; p99 < 2s; 0 lỗi pool exhausted trong log | có `500` / pool exhausted |
| `Invariant1_AfterHotRelease_NoSeatHasTwoBookings` (chạy sau k6) | không ghế nào có hai đơn | đỏ ⇒ bán trùng — dừng ngay, không ghi baseline |
| Đối chứng `NoAdmissionControl` | `pending-connection-requests` tăng vọt và xuất hiện lỗi pool exhausted | không đỏ ⇒ chưa đủ tải để thấy khác biệt |

## Câu hỏi review
- Vì sao `PermitLimit` phải nhỏ hơn pool size, và điều gì xảy ra với request đọc nếu bằng nhau?
- Chạy `NoAdmissionControl` thấy gì ở `pending-connection-requests` so với có limiter? Vì sao p99 của request *được nhận* lại thấp hơn khi có limiter?
- Vì sao `503` kèm `Retry-After` và client gửi lại **cùng** `Idempotency-Key`?
- Điều kiện đo nào bạn cố định (máy, cấu hình Postgres, seed) để hai dòng `baseline.csv` so sánh được?

## Done khi
- [ ] Limiter hoạt động, `503 OVERLOADED` đúng shape; giá trị cấu hình qua Options
- [ ] `docker run grafana/k6` chạy được cả 3 kịch bản từ `perf/`
- [ ] `perf/baseline.csv` có dòng `hot-release` có / không admission control và dòng `browse` trước cache, mỗi dòng đủ cột
- [ ] Invariant #1 kiểm sau mỗi lần chạy `hot-release`; không có dòng nào ghi khi invariant đỏ
- [ ] Phép thử âm tính `NoAdmissionControl` đã thấy đỏ, ghi trong PR
