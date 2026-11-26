# M0-04: Log JSON, correlation ID, `/health/live` và `/health/ready`

Mode: you-implement-i-review
Status: ready
Blocked by: M0-02
Traces to: FR-30
Concurrency: —
Branch: m0-04-logging-health

## Mục tiêu
Debug M1 cần nhìn được bên trong: một lỗi người dùng chụp màn hình (`traceId` trong problem+json) phải tìm
được dòng log tương ứng. Health tách hai câu hỏi — "tiến trình còn sống?" và "phụ thuộc còn nối được?" — để
orchestrator không restart app chỉ vì Redis chậm. Đây là phần đầu của FR-30 (metric Prometheus/Grafana là M3-01).

## Phạm vi
**Trong:**
- Serilog ghi log **JSON** ra console (stack đã chốt ở `architecture.md` mục "Stack").
- Correlation ID: nhận header `X-Correlation-Id` (không có thì sinh), đưa vào **log scope** của request và trả
  lại ở response header.
- `traceId` trong problem+json (M0-02) tìm được trong log của cùng request.
- `GET /health/live`: không chạm DB/Redis. `GET /health/ready`: Postgres **và** Redis; một trong hai mất ⇒ `503`.
- Thay endpoint `/health` hiện tại bằng hai endpoint trên.
- Log theo message template, không log secret/token (C-21, C-22).

**Ngoài:**
- Metric, trace OTel, Prometheus, Grafana, `/metrics` (M3-01); dashboard + alert (M3-06).
- Log nghiệp vụ của từng module (thêm ở ticket có nghiệp vụ).

## Đọc trước
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Vận hành".
- [`architecture.md`](../../../docs/design/architecture.md) — mục "Stack" (dòng Observability).
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B4", đoạn "Vì sao chọn" (`traceId` ở mọi lỗi).
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-21, C-22.

## Test
Gợi ý — người học viết (Testcontainers Postgres + Redis thật, C-29).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Live_returns_200_without_touching_dependencies` | Dừng Postgres + Redis ⇒ `/health/live` vẫn `200` | Live gọi health check của DB/Redis |
| `Ready_returns_503_when_redis_is_down` | Dừng container Redis ⇒ `/health/ready` `503`, live vẫn `200` | Ready không kiểm Redis |
| `Ready_returns_503_when_postgres_is_down` | Tương tự với Postgres | Ready không kiểm Postgres |
| `Correlation_id_is_echoed_and_generated` | Có header ⇒ trả lại cùng giá trị; không có ⇒ sinh mới | Không có header ở response |
| `Problem_trace_id_is_found_in_logs` | Gây `400`, lấy `traceId` từ body, tìm thấy dòng log JSON chứa nó (sink bộ nhớ trong test) | `traceId` không vào log, hoặc log không phải JSON |
| `Correlation_id_is_in_log_scope` | Dòng log của request mang `CorrelationId` | Chỉ có ở header, không có ở log |

## Câu hỏi review
1. Vì sao `live` không được gọi DB? Điều gì xảy ra với app khi Postgres chậm nếu `live` có gọi?
2. Correlation ID và `traceId` khác nhau thế nào, và vì sao giữ cả hai? Chọn cách nối chúng ra sao?
3. Client gửi `X-Correlation-Id` dài hoặc chứa ký tự lạ — xử lý thế nào để không làm bẩn log?

## Done khi
- [ ] Demo M0: `400` problem+json ⇒ tìm được `traceId` trong log JSON; tắt Redis ⇒ `/health/ready` đỏ, `/health/live` xanh
- [ ] `/health` cũ đã bị gỡ; CI/compose không còn trỏ vào nó
- [ ] Không log secret; log dùng message template
