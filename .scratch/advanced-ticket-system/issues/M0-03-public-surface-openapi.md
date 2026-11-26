# M0-03: Arch test bề mặt `public` và OpenAPI

Mode: pair-tdd
Status: ready
Blocked by: M0-02
Traces to: —
Concurrency: —
Branch: m0-03-public-surface-openapi

## Mục tiêu
Hai việc nền cho M1: (1) khoá bằng test quy ước "project nội bộ của module chỉ lộ `<Module>Module` và
controller" trước khi có module nào đầy code; (2) có tài liệu OpenAPI để demo M0 ("OpenAPI liệt kê
endpoint") và để xem API khi phát triển.

## Phạm vi
**Trong:**
- Arch test: trong mỗi project nội bộ `TicketSystem.<Module>` (không phải `.Contracts`), kiểu `public` duy nhất
  là `<Module>Module` và controller. Project test được thấy `internal` qua
  `<InternalsVisibleTo Include="TicketSystem.<Module>.Tests" />` trong `.csproj`.
- OpenAPI: `Microsoft.AspNetCore.OpenApi` sinh `/openapi/v1.json`; một UI chỉ bật ở môi trường
  Development. Response lỗi được mô tả là `application/problem+json`.

**Ngoài:**
- Mô tả chi tiết từng endpoint (thêm dần ở ticket có endpoint).
- Quy tắc phụ thuộc giữa project (đã có, đọc `.csproj`).
- Bảo vệ OpenAPI bằng auth (Development-only là đủ).

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "Cấu trúc thư mục bên trong một module"
  (đoạn "Hiển thị").
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-10.
- [`architecture.md`](../../../docs/design/architecture.md) — mục "D9", đoạn "Quy tắc phụ thuộc (kiểm được)".
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Định dạng lỗi".

## Test
AI viết các test này trước.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Module_internal_assemblies_expose_only_module_and_controllers` | Với từng assembly `TicketSystem.<Module>` (không `.Contracts`): mọi kiểu `public` là `<Module>Module` hoặc kế thừa `ControllerBase` | Thêm một `public class` (vd handler) vào module |
| `Contracts_assemblies_may_be_public` (đối chứng) | `.Contracts` không bị rule trên áp vào | Rule quét nhầm `.Contracts` |
| `OpenApi_document_is_served_in_development` | `GET /openapi/v1.json` ⇒ 200 và là JSON OpenAPI hợp lệ | Quên đăng ký |
| `OpenApi_not_exposed_outside_development` | Môi trường `Production` ⇒ `404` cho `/openapi/v1.json` và UI | Lộ tài liệu API ra ngoài dev |
| `OpenApi_describes_errors_as_problem_json` | Response lỗi của controller test-only có media type `application/problem+json` | Mô tả `application/json` |

**Câu hỏi thiết kế cho người học:** các arch test hiện có đọc `ProjectReference` trong `.csproj`; rule này cần
**nạp assembly đã build** và duyệt kiểu bằng reflection. Test project nạp assembly bằng cách nào mà không tự
tạo ra phụ thuộc mới vào module (ví dụ `Assembly.Load` theo tên, hay duyệt thư mục output)? Chọn và ghi lý do.

## Câu hỏi review
1. `internal` + `InternalsVisibleTo` cho test: khác gì so với để `public` rồi tin vào review?
2. Vì sao controller phải `public` dù handler/domain `internal`?
3. Vì sao UI OpenAPI chỉ ở Development — rủi ro của việc để ở Production là gì?

## Done khi
- [ ] Các test trên xanh; CI chạy cả arch test mới
- [ ] Không có kiểu `public` thừa trong 6 module hiện có (sửa nếu có)
