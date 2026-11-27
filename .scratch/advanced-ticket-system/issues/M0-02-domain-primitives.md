# M0-02: Domain primitives — `Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`, typed id

Mode: pair-tdd
Status: done
Blocked by: M0-01
Traces to: —
Concurrency: —
Branch: m0-02-domain-primitives

## Mục tiêu
Một khuôn domain chung cho cả 6 module: nhìn một aggregate là biết luật của mọi aggregate, và có điểm bám
cho arch test (M0-08) và quy ước EF (M0-04). Domain event được gom trong aggregate, handler publish tường
minh (M0-03).

## Phạm vi
**Trong** (`Shared.Abstractions/Domain/`):
- `Entity<TId>` (bằng nhau theo `Id`), `AggregateRoot<TId>` (gom event; `DequeueDomainEvents` lấy và xoá),
  `ValueObject` (abstract record marker), `IDomainEvent` (có `OccurredAt`), `IEntityId`.
- `IEntityId<TSelf>` + lớp tĩnh `EntityId` (`New<TId>()` bằng UUIDv7) theo B9; một id mẫu trong test để
  chứng minh khuôn.
- Test nằm ở `TicketSystem.Shared.Tests` (M0-01).

**Ngoài:**
- Bus publish, `IEventHandler` (M0-03); converter EF và type handler Dapper cho `IEntityId` (M0-04).
- Arch test "value object không setter", "repository chỉ cho aggregate root" (M0-08).
- Mọi aggregate / value object thật (thuộc module).

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B9 — Domain primitives: base class dùng
  chung" (bảng thành phần, "Không chọn"), "B13 — Domain event và bus in-process" (bước 1–2).
- [ADR-0018](../../../docs/adr/0018-domain-and-application-primitives.md).
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — mục 8 (domain, handler, event), C-07.

## Test
AI viết các test này trước. Dùng các class mẫu chỉ có trong test project.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Entities_with_same_id_are_equal` | Hai entity cùng kiểu, cùng `Id`, khác thuộc tính ⇒ bằng nhau; `==` và `GetHashCode` nhất quán | So sánh theo thuộc tính, hoặc theo tham chiếu |
| `Entities_with_different_id_or_type_are_not_equal` | Khác `Id`, hoặc cùng `Id` nhưng khác kiểu entity ⇒ không bằng | Bỏ kiểm tra kiểu |
| `AggregateRoot_collects_domain_events` | Method đổi trạng thái thêm event; thứ tự được giữ | Event không được gom, hoặc bị đảo thứ tự |
| `Dequeue_returns_events_and_clears` | `DequeueDomainEvents` trả mọi event; lần gọi thứ hai trả rỗng | Không xoá ⇒ publish lặp event |
| `ValueObject_equality_is_by_value` | Hai value object cùng giá trị ⇒ bằng; khác giá trị ⇒ không | So sánh tham chiếu |
| `ValueObject_is_immutable` | Property của value object mẫu không có setter public (init-only / positional) | Có setter public |
| `Entity_has_no_public_setters` | Mọi property của `Entity<TId>` / `AggregateRoot<TId>` không có setter public | `Id` sửa được từ ngoài |
| `TypedId_new_is_uuidv7_and_ordered` | Hai id tạo liên tiếp có thứ tự tăng, version nibble = 7 | Dùng `Guid.NewGuid()` (random v4) |
| `TypedId_default_is_rejected_by_entity` | Id mặc định (`Guid.Empty`) khác id mới; tạo entity với id mặc định ⇒ `ArgumentException` | Id rỗng lọt vào domain mà không ai phát hiện |

## Câu hỏi review
1. Vì sao `ValueObject` chỉ là record marker, không có `GetEqualityComponents()`?
2. `DequeueDomainEvents` lấy và xoá — nếu handler quên gọi thì chuyện gì xảy ra? Nếu gọi hai lần?
3. Vì sao UUIDv7 thay vì `Guid.NewGuid()` cho khoá chính (liên hệ index B-tree)?

## Done khi
- [x] Các test trên xanh; `Shared.Abstractions` vẫn không phụ thuộc hạ tầng (arch test hiện có xanh)
- [x] Không có setter public ở các base class; không `!` (C-13)
