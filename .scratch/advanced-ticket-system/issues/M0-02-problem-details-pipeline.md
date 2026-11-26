# M0-02: Pipeline lỗi problem+json và `TimeProvider`

Mode: pair-tdd
Status: ready
Blocked by: M0-01
Traces to: FR-36
Concurrency: —
Branch: m0-02-problem-details-pipeline

## Mục tiêu
Mọi lỗi ra khỏi API — `Result` thất bại, model binding sai, exception, route không tồn tại — cùng một shape
`application/problem+json` có `code`, `title`, `action`, `traceId` (FR-36). Sau ticket này, module chỉ việc
`return result.ToProblem(this)`; không ai viết try/catch hay tự dựng body lỗi.

## Phạm vi
**Trong** (đặt ở `Shared.Infrastructure/Web/`, thêm `FrameworkReference Microsoft.AspNetCore.App`):
- `ToProblem(this Result, ControllerBase)` — `ErrorType → status` theo bảng B4; `Error.Extensions` thành
  trường **top-level** của body (như `unavailableSeatIds`).
- `GlobalExceptionHandler : IExceptionHandler` ⇒ `500 INTERNAL_ERROR`, log `Error` kèm exception, body
  **không** có message/stack.
- `InvalidModelStateResponseFactory` ⇒ `400 VALIDATION_FAILED` + `errors: { field: [msg] }`.
- `AddProblemDetails` gắn `traceId` và `type`; `UseStatusCodePages` cho 401/403/404 rỗng.
- `AddApiErrorHandling()` / `UseApiErrorHandling()`; `AddSingleton(TimeProvider.System)` ở host (B10, B12).
- `Program.cs` chỉ thêm 2–3 dòng; không có `try/catch` ở bất kỳ đâu.

**Ngoài:**
- `429` rate limiter, `503` admission control (M3-02, M3-04 — chúng sẽ gọi cùng `IProblemDetailsService`).
- Log JSON, correlation ID (M0-04) — ở đây `traceId` lấy từ `Activity.Current?.Id ?? TraceIdentifier`.
- Mã lỗi cụ thể của module.

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B4 — Một lối ra lỗi" (bảng `ErrorType → status`,
  bốn mảnh a–e), "B2" (exception khi nào), "B12", "B10".
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Định dạng lỗi — application/problem+json" và
  "Bảng mã lỗi toàn cục".
- [ADR-0016](../../../docs/adr/0016-result-and-problem-details.md).

## Test
AI viết các test này trước. Integration test qua `WebApplicationFactory<Program>`; controller **chỉ dành cho
test** được nạp qua `ApplicationParts` của factory — không thêm endpoint giả vào code production.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Result_failure_maps_status_by_error_type` (6 case) | Mỗi `ErrorType` ⇒ 400/401/403/404/409/422; `Content-Type: application/problem+json` | Sai bảng map, hoặc trả `application/json` |
| `Problem_body_has_code_title_action_traceid` | Body có `code`, `title`, `action`, `traceId`, `status`, `type` | Thiếu bất kỳ trường nào |
| `Extensions_appear_top_level` | `Error` có `unavailableSeatIds` ⇒ trường đó ở gốc body, không lồng trong `extensions` | Lồng sai chỗ |
| `Unhandled_exception_returns_500_internal_error` | `500`, `code = INTERNAL_ERROR`, body **không** chứa message/stack của exception | Lộ chi tiết nội tạng |
| `Dataannotations_failure_returns_400_validation_failed` | `400`, `code = VALIDATION_FAILED`, `errors` có tên trường sai | Trả shape mặc định của MVC (không có `code`) |
| `Unknown_route_returns_404_problem_json` | `404`, problem+json, có `traceId` | Body rỗng |

Lưu ý: host đọc `ConnectionStrings:Postgres` và `:Redis` lúc khởi động — test host phải cấp (Testcontainers
hoặc giá trị giả); chọn cách nào là việc của người học, ghi trong PR.

## Câu hỏi review
1. Vì sao dùng `IExceptionHandler` thay vì middleware try/catch hay exception filter (B4)?
2. `OperationCanceledException` của request xử lý thế nào, và vì sao không được thành 500?
3. Ba lối ra lỗi đi qua cùng một cơ chế nào để shape giống hệt nhau? Chỉ ra dòng code.

## Done khi
- [ ] Các test trên xanh; `Program.cs` thêm ≤ 3 dòng, không `catch`
- [ ] Không có endpoint test-only trong assembly production
