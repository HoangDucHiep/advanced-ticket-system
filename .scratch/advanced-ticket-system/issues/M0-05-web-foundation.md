# M0-05: Web foundation — problem+json, JSON options, `ApiControllerBase`, OpenAPI, `TimeProvider`

Mode: pair-tdd
Status: ready
Blocked by: M0-01
Traces to: FR-36
Concurrency: —
Branch: m0-05-web-foundation

## Mục tiêu
Mọi lỗi ra khỏi API — `Result` thất bại, model binding sai, exception, route không tồn tại — cùng một shape
`application/problem+json` có `code`, `title`, `action`, `traceId` (FR-36). Controller module chỉ kế thừa
`ApiControllerBase` và trả `Result`; không ai viết try/catch hay tự dựng body lỗi.

## Phạm vi
**Trong** (`Shared.Infrastructure/Web/`, thêm `FrameworkReference Microsoft.AspNetCore.App`):
- `Result` ⇒ problem+json: `ErrorType → status` theo bảng B4; `Error.Extensions` thành trường **top-level**.
- `IExceptionHandler` ⇒ `500 INTERNAL_ERROR`, log `Error`, body **không** có message/stack.
- Model binding sai ⇒ `400 VALIDATION_FAILED` + `errors`; 401/403/404 rỗng cũng ra problem+json có `traceId`.
- JSON options một chỗ: camelCase, enum dạng chuỗi, `DateTimeOffset` ISO-8601.
- `ApiControllerBase` (`[ApiController]`, route `api/...`, một method chung biến `Result` thành `IActionResult`).
- OpenAPI built-in: `/openapi/v1.json` liệt kê endpoint và mô tả lỗi là `application/problem+json`;
  UI chỉ ở Development.
- Đăng ký `TimeProvider.System` ở host. `Program.cs` chỉ thêm vài dòng, không `try/catch`.

**Ngoài:**
- `429` rate limiter, `503` admission control (M3-02, M3-04) — chúng gọi cùng `IProblemDetailsService`.
- Log JSON, correlation ID (M0-07); JWT / policy (M0-06).
- Mô tả chi tiết từng endpoint (thêm dần ở ticket có endpoint); mã lỗi của module.

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B4 — Một lối ra lỗi: `problem+json`",
  "B15 — Web foundation", "B2 — Khi nào exception, khi nào `Result`", "B10 — `TimeProvider`".
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Định dạng lỗi — application/problem+json",
  "Bảng mã lỗi toàn cục".
- [ADR-0016](../../../docs/adr/0016-result-and-problem-details.md).

## Test
AI viết các test này trước. Integration qua `WebApplicationFactory<Program>`; controller **chỉ dành cho test**
nạp qua `ApplicationParts` — không thêm endpoint giả vào code production. Host đọc `ConnectionStrings:*` lúc
khởi động: test host cấp giá trị tạm cho tới khi M0-08 có `ApiFactory`.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Result_failure_maps_status_by_error_type` (6 case) | Mỗi `ErrorType` ⇒ 400/401/403/404/409/422; `Content-Type: application/problem+json` | Sai bảng map, hoặc trả `application/json` |
| `Problem_body_has_code_title_action_traceid` | Body có `code`, `title`, `action`, `traceId`, `status`, `type` | Thiếu trường nào |
| `Extensions_appear_top_level` | `Error` có `unavailableSeatIds` ⇒ trường ở gốc body, không lồng `extensions` | Lồng sai chỗ |
| `Unhandled_exception_returns_500_internal_error` | `500`, `code = INTERNAL_ERROR`, body không chứa message/stack | Lộ chi tiết nội tạng |
| `Dataannotations_failure_returns_400_validation_failed` | `400`, `code = VALIDATION_FAILED`, `errors` có tên trường sai | Trả shape mặc định của MVC |
| `Unknown_route_returns_404_problem_json` | `404`, problem+json, có `traceId` | Body rỗng |
| `Json_is_camelcase_with_string_enums` | Response camelCase, enum là chuỗi, `DateTimeOffset` ISO-8601 | Mặc định PascalCase / enum số |
| `ApiControllerBase_maps_result_in_one_place` | Controller test trả `Result` thành công ⇒ status do controller chọn; thất bại ⇒ problem | Mỗi controller tự `if` |
| `OpenApi_document_lists_endpoints_and_problem_json` | `GET /openapi/v1.json` ⇒ `200`, có endpoint, response lỗi là `application/problem+json` | Mô tả `application/json` |
| `OpenApi_ui_only_in_development` | Môi trường `Production` ⇒ UI và tài liệu trả `404` | Lộ ra ngoài dev |
| `TimeProvider_is_registered` | Container resolve được `TimeProvider`; test thay được bằng `FakeTimeProvider` | Không đăng ký, hoặc có `IClock` tự viết |

## Câu hỏi review
1. Vì sao `IExceptionHandler` thay vì middleware try/catch hay exception filter (B4)?
2. `OperationCanceledException` do client ngắt xử lý thế nào, và vì sao không được thành 500?
3. Ba lối ra lỗi đi qua cùng một cơ chế nào để shape giống hệt nhau?
4. Vì sao `ApiControllerBase` không tự chọn status thành công (200 / 201 / 204)?

## Done khi
- [ ] Các test trên xanh; không có endpoint test-only trong assembly production
- [ ] `Program.cs` không có `catch`; UI OpenAPI chỉ bật ở Development
