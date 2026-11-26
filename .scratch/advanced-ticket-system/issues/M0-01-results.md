# M0-01: Results — `Result`, `Result<T>`, `Error`, `ErrorType`, `PagedResponse<T>`

Mode: pair-tdd
Status: done
Blocked by: —
Traces to: FR-36
Concurrency: —
Branch: m0-01-results

## Mục tiêu
Có kiểu trả về chung để mọi handler và method domain nói thật "việc này có thể thất bại theo cách nghiệp
vụ", kèm `code` + `action` cho client (FR-36). Mọi ticket sau dựa vào các kiểu này.

## Phạm vi
**Trong:**
- `Result`, `Result<T>`, `Error`, `ErrorType`, `PagedResponse<T>` ở `Shared.Abstractions/Results/`.
- Hoàn tất project `tests/TicketSystem.Shared.Tests` (người học đã bắt đầu tạo): build được, nằm trong
  `TicketSystem.sln`, chạy trong CI cùng các test project khác.
- Tên `ErrorType` đúng bảng ở B4 (`Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`,
  `BusinessRule`); chỉ định nghĩa kiểu, không map sang status.

**Ngoài:**
- Map `Result` → problem+json, exception handler, `TimeProvider` (M0-05).
- Mã lỗi cụ thể của module (`UserErrors`…): thuộc module (M1-01 trở đi).
- Domain primitives (M0-02); bất kỳ kiểu nào khác "phòng khi cần".

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B1 — Result pattern", "B3 — Không có
  success envelope", "B12 — Bản đồ code dùng chung" (vì sao ở `Shared.Abstractions`).
- [ADR-0016](../../../docs/adr/0016-result-and-problem-details.md).
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-13 (không dùng `!`), C-17.

## Test
AI viết các test này trước (xanh khi người học hiện thực xong). Project: `TicketSystem.Shared.Tests`.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Success_has_no_error` | `Result.Success()`: `IsSuccess`, `!IsFailure`; đọc `Error` ném `InvalidOperationException` | `Error` trả `null`/mặc định thay vì ném |
| `Failure_exposes_error` | `Result.Failure(e)`: `IsFailure`, `Error == e` | Đảo cờ, hoặc mất `Error` |
| `Failure_with_null_error_is_rejected` | `Result.Failure(null)` ném `ArgumentNullException` | Chấp nhận null — thất bại mà không có lý do |
| `Failure_value_access_throws` | `Result<T>.Failure(e).Value` ném `InvalidOperationException` | Trả `default` — người gọi dùng giá trị rỗng mà không biết |
| `Success_value_is_returned` | `Result<T>.Success(v).Value == v`; `Error` ném | — |
| `Implicit_conversion_from_value` | `Result<int> r = 5` ⇒ thành công, `Value == 5` | Thiếu toán tử |
| `Implicit_conversion_from_error` | `Result<int> r = error` và `Result r = error` ⇒ thất bại, cùng `Error` | Thiếu toán tử, hoặc mất `Extensions` |
| `Generic_result_is_usable_as_result` | `Result<T>` gán được cho biến `Result`; cờ giữ nguyên | `Result<T>` không kế thừa `Result` |
| `Error_extensions_are_preserved` | `Error` mang `Extensions` (vd `unavailableSeatIds`) qua `Result` | Extensions bị bỏ khi bọc |
| `PagedResponse_exposes_page_metadata` | `PagedResponse<T>` giữ items + thông tin phân trang đúng như [`api-contract.md`](../../../docs/design/api-contract.md) | Thiếu trường phân trang, hoặc tên lệch hợp đồng |

Arch test hiện có (`Rule2_Shared_Abstractions_has_no_infrastructure_dependencies`) phải vẫn xanh.

## Câu hỏi review
1. `Result<T>.Failure` che `Result.Failure` — vì sao không đặt tên khác? Điều gì xảy ra nếu người gọi có biến kiểu `Result`?
2. Hai `Error` cùng `Code` nhưng khác `Extensions` có bằng nhau không? Điều đó có sao khi test so sánh lỗi?
3. Vì sao `Value` ném exception thay vì trả `default` khi thất bại (C-13, B1)?

## Done khi
- [x] Các test trên xanh; `TicketSystem.Shared.Tests` nằm trong `.sln` và CI chạy nó
- [x] Không có `!` (C-13); `Error`/`Result` không tham chiếu ASP.NET Core; arch test xanh
