# M0-01: `Result`, `Result<T>`, `Error`, `ErrorType`, `PagedResponse<T>`

Mode: pair-tdd
Status: ready
Blocked by: —
Traces to: FR-36
Concurrency: —
Branch: m0-01-result-types

## Mục tiêu
Có kiểu trả về chung để mọi handler và method domain nói thật "việc này có thể thất bại theo cách nghiệp
vụ", kèm `code` + `action` cho client (FR-36). Mọi ticket sau (M0-02 trở đi) dựa vào các kiểu này.

## Phạm vi
**Trong:**
- `Result`, `Result<T>`, `Error`, `ErrorType`, `PagedResponse<T>` ở `Shared.Abstractions/Results/` (B1, B3).
- Hoàn tất project `tests/TicketSystem.Shared.Tests` (người học đã bắt đầu tạo; ticket này chỉ cần nó
  build được, có trong `TicketSystem.sln`, chạy trong CI cùng các test project khác).
- Tên `ErrorType` đúng bảng ở B4 (`Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`,
  `BusinessRule`) — việc map sang status là của M0-02, không làm ở đây.

**Ngoài:**
- Map `Result` → problem+json, exception handler, `TimeProvider` (M0-02).
- Mã lỗi cụ thể của module (`UserErrors`…): thuộc module (M1-01 trở đi), không nằm ở `Shared`.
- Bất kỳ kiểu nào khác trong `Shared` ("phòng khi cần") — luật 2 của D6.

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B1 — Result pattern" (chữ ký, vì sao
  tự viết) và "B3 — Không có success envelope".
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B12" (vì sao ở `Shared.Abstractions`).
- [ADR-0016](../../../docs/adr/0016-result-and-problem-details.md).
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-13 (không dùng `!`), C-17.

## Test
AI viết các test này trước (xanh khi người học hiện thực xong). Project: `TicketSystem.Shared.Tests`.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Success_has_no_error` | `Result.Success()`: `IsSuccess`, `!IsFailure`; đọc `Error` ném `InvalidOperationException` | `Error` trả `null`/giá trị mặc định thay vì ném |
| `Failure_exposes_error` | `Result.Failure(e)`: `IsFailure`, `Error == e` | Đảo cờ, hoặc mất `Error` |
| `Failure_with_null_error_is_rejected` | `Result.Failure(null)` ném (`ArgumentNullException`) | Chấp nhận null — `IsFailure` mà không có lý do |
| `Failure_value_access_throws` | `Result<T>.Failure(e).Value` ném `InvalidOperationException` | Trả `default` — người gọi dùng giá trị rỗng mà không biết |
| `Success_value_is_returned` | `Result<T>.Success(v).Value == v`; `Error` ném | — |
| `Implicit_conversion_from_value` | `Result<int> r = 5` ⇒ thành công, `Value == 5` | Thiếu toán tử |
| `Implicit_conversion_from_error` | `Result<int> r = error` và `Result r = error` ⇒ thất bại, cùng `Error` | Thiếu toán tử, hoặc mất `Extensions` |
| `Generic_result_is_usable_as_result` | `Result<T>` gán được cho biến `Result`; cờ giữ nguyên | `Result<T>` không kế thừa `Result` |
| `Error_extensions_are_preserved` | `Error` mang `Extensions` (ví dụ `unavailableSeatIds`) qua `Result` | Extensions bị bỏ khi bọc |

Arch test hiện có (`Rule2_Shared_Abstractions_has_no_infrastructure_dependencies`) phải vẫn xanh —
`Shared.Abstractions` không được kéo ASP.NET Core vào.

## Câu hỏi review
1. `Result<T>.Failure` dùng `new` để che `Result.Failure` — vì sao không đặt tên khác? Điều gì xảy ra nếu
   người gọi có biến kiểu `Result`?
2. `Error` là `record` có `IReadOnlyDictionary` — hai `Error` cùng `Code` nhưng khác `Extensions` có bằng nhau
   không? Điều đó có sao không khi test so sánh lỗi?
3. Vì sao `Value` ném exception thay vì trả `default` khi thất bại (C-13, B1)?

## Done khi
- [ ] Các test trên xanh; `TicketSystem.Shared.Tests` nằm trong `.sln` và CI chạy nó
- [ ] Không có `!` (C-13); `Error`/`Result` không tham chiếu ASP.NET Core; arch test xanh
