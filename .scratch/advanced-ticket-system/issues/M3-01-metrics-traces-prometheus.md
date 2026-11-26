# M3-01: Metric + trace + Prometheus / Grafana

Mode: you-implement-i-review
Status: ready
Blocked by: M2-11
Traces to: FR-30
Concurrency: —
Branch: m3-01-metrics-traces-prometheus

## Mục tiêu
Nhìn được bên trong hệ thống trước khi đo tải: "đo tải mà không nhìn được bên trong là đo mù"
(`gate-d-implementation.md`). Sau ticket này mọi con số ở M3-02..M3-06 đọc được từ Prometheus, và một
request đi qua API → DB → Redis → outbox truy ra được bằng một `traceId`.

## Phạm vi
**Trong:**
- OpenTelemetry **trace**: span cho request ASP.NET Core, Npgsql, Redis (StackExchange.Redis); `traceId` trong
  problem+json (B4(d)) và trong log JSON (M0-04) là **cùng một** id.
- Correlation ID đi xuyên **background worker và outbox dispatcher**: việc outbox sinh ra sau commit mang
  được trace/correlation của request gây ra nó (lưu cùng message hoặc truyền qua payload — người học chọn,
  ghi lý do trong PR).
- OpenTelemetry **metric** hạ tầng: ASP.NET Core, Npgsql (pool), `Microsoft.AspNetCore.RateLimiting`, runtime.
- Metric **nghiệp vụ** (counter, tên do người học đặt theo một quy ước thống nhất, ghi trong PR): ghế bị từ chối
  `SEATS_UNAVAILABLE`, hold hết hạn bị lấy, webhook trùng, idempotency replay (`Idempotent-Replayed`),
  outbox (đã xử lý / dead / độ trễ). Tên **đã có sẵn trong design** — dùng đúng:
  `payment_webhook_poison_total`, `seatmap_cache_requests_total{result}`, `ratelimit_redis_errors_total`
  (hai cái sau được phát ra ở M3-03 / M3-04, ticket này chỉ chốt quy ước đặt tên và đăng ký meter).
- **Không có label không giới hạn** (`ticketId`, `bookingId`, `userId`, `showtimeId`, IP, email...): chỉ
  label có tập giá trị nhỏ và biết trước (`result`, `code`, `policy`, `type`).
- `GET /metrics` cho Prometheus scrape, **không public** ([`api-contract.md`](../../../docs/design/api-contract.md)
  mục "Vận hành": chỉ mạng nội bộ). Cách chặn (cổng riêng / chỉ lắng nghe mạng Compose / không map ra host)
  do người học chọn và nêu lý do.
- Prometheus + Grafana thêm vào `docker-compose.yml`; datasource Grafana provision bằng file.

**Ngoài:**
- Dashboard + alert → M3-06. Ở ticket này chỉ cần Grafana xem được metric thô (Explore).
- Phát metric hit/miss cache → M3-03; metric rate limit Redis → M3-04 (ticket đó dùng quy ước của ticket này).
- k6, admission control → M3-02.

## Đọc trước
- [`architecture.md`](../../../docs/design/architecture.md) — mục "Stack" (Observability) và D8 (outbox).
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Vận hành".
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — B4 (`traceId` ở mọi lỗi).
- [`concurrency.md`](../../../docs/design/concurrency.md) — #9 (hit ratio), #11 (counter cần đo), #12 (`ratelimit_redis_errors_total`).
- [`data-model.md`](../../../docs/design/data-model.md) — bảng `payment.webhook_failures` (`payment_webhook_poison_total`) và "Outbox".
- [`01-requirements.md`](../../../docs/prd/01-requirements.md) FR-30; [`04-business-rules.md`](../../../docs/prd/04-business-rules.md) BR-28.

## Test
Gợi ý — người học viết (Testcontainers cho phần cần Redis / Postgres):

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Metrics_Endpoint_NotReachableFromPublicPort` | `/metrics` không trả được qua cổng/đường public của API | endpoint mở trên cùng listener public |
| `Request_TraceId_AppearsInProblemJsonAndLog` | `traceId` của response lỗi tìm thấy trong log JSON | log và problem+json dùng hai id khác nhau |
| `Outbox_Handler_RunsUnderOriginatingTrace` | message outbox xử lý sau commit mang cùng trace/correlation của request gốc | dispatcher tạo trace mới, mất liên kết |
| `BusinessMetrics_NoUnboundedLabels` | sau chuỗi request với nhiều id khác nhau, số time series của mỗi metric nghiệp vụ không tăng theo số id | có label chứa id |
| `SeatsUnavailable_Rejection_IncrementsCounter` | một `409 SEATS_UNAVAILABLE` tăng đúng counter tương ứng một lần | counter không đổi / tăng hai lần |

## Câu hỏi review
- Vì sao `showtimeId` không được làm label, và nếu vẫn muốn biết "suất nào nóng" thì lấy ở đâu (trace? log?)?
- Counter "ghế bị từ chối" tăng ở tầng nào (handler hay middleware) và vì sao đúng tầng đó?
- Trace context mất ở điểm nào khi việc chuyển từ request sang outbox dispatcher, và bạn nối lại bằng cách gì?
- Vì sao `/metrics` cần cách ly kể cả khi nó "chỉ là số"?

## Done khi
- [ ] Trace của một lần giữ ghế cho thấy span HTTP → Npgsql → (Redis nếu có), và `traceId` khớp log + problem+json
- [ ] Việc outbox xử lý sau commit nằm trong cùng trace/correlation với request gây ra nó
- [ ] Prometheus scrape được `/metrics`; Grafana (Explore) đọc được metric pool Npgsql, rate limiter, và các counter nghiệp vụ
- [ ] Không metric nào có label không giới hạn (test xanh, đã thấy đỏ khi thêm label id)
- [ ] `/metrics` không truy cập được từ cổng public
- [ ] PR liệt kê toàn bộ tên metric + label + ý nghĩa (bảng) để M3-02 / M3-06 dùng lại
