# M0-04: Persistence foundation — `ModuleDbContext`, quy ước, migration, Dapper

Mode: you-implement-i-review
Status: ready
Blocked by: M0-02
Traces to: —
Concurrency: —
Branch: m0-04-persistence-foundation

## Mục tiêu
Quy ước tên, kiểu và migration được cấu hình **một lần**, để 6 module không lệch nhau và khớp
`data-model.md` mà không phải `ToTable` / `HasColumnName` từng chỗ.

## Phạm vi
**Trong** (`Shared.Infrastructure/Persistence/`):
- Base `ModuleDbContext`: default schema của module; tên snake_case (`EFCore.NamingConventions`);
  `DateTimeOffset` ⇒ `timestamptz`; enum ⇒ `text`; converter tự động cho mọi `IEntityId`.
- Chiến lược migration: mỗi module một bộ ở `Infrastructure/Migrations`, bảng lịch sử trong schema của module;
  cách áp bằng CLI cho môi trường thật, tự động ở Development + trong test fixture; migration đầu tạo schema.
- Cơ chế khai báo extension (`btree_gist`) trong migration — Testcontainers không chạy init script.
- Dapper: type handler `IEntityId` và `DateTimeOffset` đăng ký một lần; class SQL dùng
  `IUnitOfWork.Connection` / `Transaction`.
- Cấu hình bảng outbox dùng chung (shape ở data-model) để M2-05 dùng; chưa hiện thực writer.

**Ngoài:**
- Entity / migration thật của module (M1-01 trở đi); `btree_gist` của `scheduling` khai báo ở M1-02.
- Hiện thực `IOutboxWriter` + dispatcher (M2-05).
- Fixture Postgres dùng chung (M0-08): ở ticket này dùng Testcontainers như `TicketSystem.IntegrationTests` hiện có.

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B14 — Persistence foundation", "B8 —
  Repository: chỉ cho aggregate root, chỉ phía ghi", "B9 — Domain primitives" (typed id).
- [`data-model.md`](../../../docs/design/data-model.md) — mục "Quy ước chung", "Outbox — mỗi module một bảng".
- [ADR-0008](../../../docs/adr/0008-cross-module-transaction.md);
  [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-04, C-07.

## Test
Gợi ý — người học viết. Postgres thật qua Testcontainers (C-29); dùng một context mẫu chỉ có trong test.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Tables_and_columns_are_snake_case_in_schema` | Bảng/cột của context mẫu ở đúng schema, tên snake_case như `data-model.md` | Thiếu quy ước đặt tên |
| `TypedId_roundtrips_via_EF` | Lưu rồi đọc entity có typed id bằng EF ⇒ cùng giá trị | Thiếu converter |
| `TypedId_roundtrips_via_Dapper` | Đọc/ghi typed id qua Dapper trên cùng connection/transaction ⇒ cùng giá trị | Thiếu type handler |
| `Enum_is_stored_as_text` | Cột enum là `text`, giá trị là tên enum | Lưu số nguyên |
| `DateTimeOffset_is_timestamptz` | Cột là `timestamptz`; đọc lại đúng thời điểm | Lưu `timestamp` không múi giờ |
| `Migrations_apply_on_empty_database` | Fixture áp migration lên DB trống thành công; schema + bảng lịch sử đúng chỗ | Migration phụ thuộc init script |
| `Btree_gist_is_created_by_migration` | Sau migration, `btree_gist` có trong `pg_extension` mà không cần init script | Extension chỉ có ở init script của compose |
| `Dapper_uses_unit_of_work_transaction` | Câu Dapper trong `IUnitOfWork` rollback cùng EF | Dapper mở connection riêng |

## Câu hỏi review
1. Vì sao quy ước nằm ở base context thay vì mỗi module tự cấu hình? Lệch nhau lộ ra khi nào?
2. Vì sao không tự migrate khi khởi động ở mọi môi trường?
3. Bảng lịch sử migration theo schema giúp gì khi tách module (D10)?

## Done khi
- [ ] Các test trên xanh; migration chạy từ DB trống trên Testcontainers
- [ ] Không còn init script nào là điều kiện để test xanh
