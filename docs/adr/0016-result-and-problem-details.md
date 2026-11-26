---
id: 0016
title: Lỗi nghiệp vụ là Result; mọi lỗi ra ngoài là problem+json qua một cơ chế duy nhất
status: accepted
date: 2026-11-26
Related: ADR-0011, ADR-0017
---

# Lỗi nghiệp vụ là `Result`; mọi lỗi ra ngoài là `problem+json` qua một cơ chế duy nhất

*AI quyết định (hợp đồng 2026-11-25). Người học phát hiện thiếu thiết kế này trước T16.*
Chi tiết: [`docs/design/building-blocks.md`](../design/building-blocks.md) B1–B4.

## Context

`api-contract.md` (T13) đã chốt **định dạng** lỗi: `application/problem+json` (RFC 9457) có `code`,
`action`, `traceId`, và bảng status `400/401/403/404/409/422`. `architecture.md` D6 đặt "kiểu `Result` /
lỗi dùng chung" vào `Shared.Abstractions`. Chưa có quyết định nào về **cơ chế**: lỗi nghiệp vụ được biểu
diễn thế nào trong code, exception nào thành status nào, và làm sao mọi lối ra lỗi (validation, nghiệp
vụ, exception) cùng một shape. Quyết định này lan vào chữ ký của mọi handler ở 6 module — đổi sau rất tốn.

## Decision

- Lỗi **dự đoán được** (nghiệp vụ, trạng thái thế giới) là `Result` / `Result<T>` với
  `Error(Code, Title, Action, Type, Extensions?)`. Tự viết, ở `Shared.Abstractions/Results/`.
- Exception chỉ cho lỗi **không mong đợi**. Ngoại lệ có kiểm soát duy nhất: repository đổi unique
  violation (`23505`) của **constraint đã biết tên** thành `Result`.
- `ErrorType → status`: `Validation 400`, `Unauthorized 401`, `Forbidden 403`, `NotFound 404`,
  `Conflict 409`, `BusinessRule 422`.
- Một lối ra: `Result.ToProblem()` (qua `ProblemDetailsFactory`), `IExceptionHandler` cho 500,
  `InvalidModelStateResponseFactory` cho 400, `AddProblemDetails` gắn `traceId`, `UseStatusCodePages`
  cho 401/403/404 rỗng. Đặt ở `Shared.Infrastructure/Web/`.
- Không success envelope: 2xx trả thẳng resource; danh sách là `PagedResponse<T>`.

## Considered Options

| Phương án | Vì sao không chọn |
|---|---|
| Exception cho lỗi nghiệp vụ + middleware map exception → status | Luồng ẩn khỏi chữ ký; dễ bị `catch` nuốt; tốn stack trace ở hot path giữ ghế nơi `409` là bình thường |
| FluentResults / ErrorOr | Thêm package cho ~80 dòng; không có sẵn `action` (`FR-36`) |
| Envelope `{ success, data, error }` | Trùng HTTP status; hai định dạng lỗi song song; lệch `api-contract.md` |
| Middleware try/catch tự viết | Trùng `IExceptionHandler` có sẵn từ .NET 8 |
| Project `Shared.Web` riêng | Thêm cạnh phụ thuộc cho ~3 file; D6 giữ hai project |

## Consequences

- `Shared.Infrastructure` có `FrameworkReference Microsoft.AspNetCore.App` (được phép — `.Contracts`
  không reference nó; arch test quy tắc 2 không đổi).
- Mọi handler trả `Result`; controller luôn có dạng `IsSuccess ? 2xx : ToProblem()`.
- Body 500 không bao giờ chứa message / stack trace; tra lỗi bằng `traceId` trong log (T25).
- Test hợp đồng lỗi (T28) assert theo `code` — đổi `title` không làm đỏ test.
