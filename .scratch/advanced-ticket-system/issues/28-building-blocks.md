# 28: Building blocks — Result, problem+json, exception handler

Mode: pair-tdd
Status: open
Gate: D
Layer: code
Blocked by: 15
Traces to: FR-36

> **Nguồn design:** [`docs/design/building-blocks.md`](../../../docs/design/building-blocks.md) (B1–B12),
> [ADR-0016](../../../docs/adr/0016-result-and-problem-details.md),
> [ADR-0017](../../../docs/adr/0017-handler-per-use-case-no-mediator.md),
> [`docs/design/coding-conventions.md`](../../../docs/design/coding-conventions.md).
> Khi ticket này mâu thuẫn với design, **design thắng**.
>
> **Số 28 dù chạy trước T16:** số ticket là ID, thứ tự nằm ở bảng của `docs/plan/gate-d-implementation.md`.
> Đánh số lại T16–T27 sẽ làm gãy mọi tham chiếu.

## Vì sao có ticket này

Người học phát hiện (2026-11-26): T16 là ticket production code đầu tiên, nhưng chưa có khối nền nào
để mọi module viết theo cùng một khuôn — lỗi nghiệp vụ biểu diễn thế nào, ra HTTP thế nào, exception
đi đâu. Tài liệu đã chốt **định dạng** (`api-contract.md`) mà chưa chốt **cơ chế**.

Ticket này dựng phần **dùng chung** của khuôn đó. Phần **theo module** (handler, repository, domain —
B5–B9) không có code chung để viết: nó được áp dụng lần đầu ở T16 và review theo `coding-conventions.md`.

## Bạn tạo ra

**`TicketSystem.Shared.Abstractions/Results/`**

- `ErrorType`, `Error`, `Result`, `Result<T>` — chữ ký ở `building-blocks.md` B1.
- `PagedResponse<T>` — B3.

**`TicketSystem.Shared.Infrastructure/Web/`** (thêm `<FrameworkReference Include="Microsoft.AspNetCore.App" />`)

- `ResultHttpExtensions.ToProblem(this Result, ControllerBase)` — map `ErrorType → status`, đặt `code`,
  `action`, `type`, chép `Extensions`.
- `GlobalExceptionHandler : IExceptionHandler` — 500 `INTERNAL_ERROR`, log exception, body không có
  message / stack trace.
- `ApiErrorHandlingExtensions`: `AddApiErrorHandling(this IServiceCollection)` (gồm `AddProblemDetails` +
  `CustomizeProblemDetails` gắn `traceId`, `AddExceptionHandler<GlobalExceptionHandler>`,
  `InvalidModelStateResponseFactory` → 400 `VALIDATION_FAILED` + `errors`) và
  `UseApiErrorHandling(this WebApplication)` (`UseExceptionHandler`, `UseStatusCodePages`).

**`TicketSystem.Api/Program.cs`**: gọi hai extension trên; `services.AddSingleton(TimeProvider.System)` (B10).

**Test** (AI viết đỏ trước, xem dưới).

## AI làm gì

Theo `pair-tdd`: AI **viết test đỏ** cho hợp đồng; người học viết production code cho xanh; AI hướng
dẫn từng bước (📍 thư mục + lệnh terminal, 📄 đường dẫn file, lý do) nhưng **không viết thân** production
code. Sau đó AI verify (build, test, phép thử âm tính, soát secret) và commit hoàn tất ticket.

Test AI sẽ viết:

| Test | Project | Đỏ nếu… |
|---|---|---|
| `Result` thành công / thất bại; truy cập `Value` khi thất bại ném `InvalidOperationException`; truy cập `Error` khi thành công ném | `tests/TicketSystem.Shared.Tests/` (mới) | `Result` cho đọc giá trị rác |
| Mỗi `ErrorType` ra đúng status (6 case) và body có `code`, `title`, `action`, `traceId`, content-type `application/problem+json` | `TicketSystem.IntegrationTests/Web/` | bảng map sai, thiếu trường |
| `Extensions` (vd `unavailableSeatIds`) xuất hiện ở top-level body | như trên | mất trường thêm của `SEATS_UNAVAILABLE` |
| Exception chưa bắt ⇒ 500 `INTERNAL_ERROR`, body **không** chứa message của exception | như trên | lộ nội tạng |
| Request DTO sai DataAnnotations ⇒ 400 `VALIDATION_FAILED` có `errors` | như trên | 400 mặc định của ASP.NET (không có `code`) |
| Route không tồn tại ⇒ 404 problem+json có `traceId` | như trên | 404 body rỗng |
| Arch test: type `public` trong project nội bộ module chỉ là `*Module` hoặc controller | `TicketSystem.ArchitectureTests` | ai đó để `public` class nội bộ |

Endpoint dùng để thử (trả từng loại lỗi, ném exception) **chỉ tồn tại trong test host**
(`WebApplicationFactory` + controller khai báo trong project test, nạp bằng `ApplicationParts`) —
không có endpoint giả nào trong production.

## Tiêu chí hoàn thành

- [ ] Mọi test ở bảng trên xanh; build Release 0 warning
- [ ] Mỗi test đã được thấy **đỏ** ít nhất một lần bằng phép thử âm tính (vd đổi `Conflict → 400` ⇒ test map đỏ;
      bỏ `CustomizeProblemDetails` ⇒ test `traceId` đỏ; đưa `exception.Message` vào `detail` ⇒ test 500 đỏ)
- [ ] `Shared.Abstractions` vẫn không reference ASP.NET Core (arch test có sẵn xanh)
- [ ] `Program.cs` chỉ thêm 2–3 dòng; không có `try/catch` nào trong module hay host
- [ ] `docs/learning/log.md` có mục T28
