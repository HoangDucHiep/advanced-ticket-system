---
id: 0018
title: Domain primitives có base class; handler hiện thực interface chung; domain event gom trong aggregate, publish tường minh
status: accepted
date: 2026-11-26
Related: ADR-0008, ADR-0009, ADR-0011, ADR-0016, ADR-0017
Supersedes: một phần ADR-0017 (xem "Phần nào của ADR-0017 bị thay")
---

# Domain primitives có base class; handler hiện thực interface chung; domain event gom trong aggregate, publish tường minh

Chi tiết: [`docs/design/building-blocks.md`](../design/building-blocks.md) B6, B9, B13.

## Context

ADR-0017 chọn kiểu tối thiểu: không base class cho domain, không domain event trong aggregate, handler là
class trần không interface chung — với lý do "chỉ thêm khi ≥ 2 module cần ngay". Phản hồi của người học khi
đọc plan:

- **Cần một khuôn nhất quán cho 6 module.** Nhìn một aggregate phải biết luật của mọi aggregate; hiện mỗi
  module có thể tự chọn cách.
- **Base class là điểm bám.** Architecture test ("value object không setter", "chỉ aggregate root có
  repository", "domain không phụ thuộc hạ tầng") và quy ước EF (converter tự động cho typed id) cần một kiểu
  chung để quét / cấu hình một lần.
- **Decorator log / trace + đăng ký bằng quét assembly** cần interface handler chung. ADR-0017 đã ghi đúng
  điều kiện "xem lại khi" này (cross-cutting thứ hai: log / metric mỗi use case); nay đã có nhu cầu (observability
  ở M0 và metric ở M3).

Hai nhu cầu đầu thuộc nền móng (M0) phải có **trước** module đầu tiên, nên luật 2 của D6 (≥ 2 module cần ngay)
được thoả: cả 6 module dùng từ module đầu tiên.

## Decision

**D-A — Domain primitives** (`Shared.Abstractions/Domain/`):

| Thành phần | Quyết định |
|---|---|
| `Entity<TId>` | Có `Id`; bằng nhau theo `Id` (cùng kiểu + cùng Id); không setter public |
| `AggregateRoot<TId>` | Kế thừa `Entity<TId>`; gom domain event vào danh sách nội bộ; `DequeueDomainEvents` lấy và xoá; là ràng buộc generic của repository |
| `ValueObject` | `abstract record` làm marker: bất biến, bằng nhau theo giá trị, tạo qua factory trả `Result<T>` |
| `IDomainEvent` | Marker có `OccurredAt`; event cho module khác nằm ở `.Contracts`, event nội bộ ở `Domain/` |
| Typed id | Mỗi aggregate một id kiểu riêng (struct bất biến bọc `Guid`, implement `IEntityId`), tạo bằng UUIDv7; EF converter tự động; Dapper type handler |

**D-B — Application primitives** (`Shared.Abstractions/Application/`):

| Thành phần | Quyết định |
|---|---|
| Handler | `ICommand` / `IQuery` + `ICommandHandler` / `IQueryHandler`, trả `Result<T>`; controller inject **interface của handler** |
| Đăng ký | Mỗi module quét assembly đăng ký mọi handler (Scoped) |
| Decorator | Một decorator chung: log + thời gian + kết quả, tạo `Activity`; **không** transaction |
| Event bus | `IEventPublisher` + `IEventHandler<TEvent>`: publish chạy handler **đồng bộ trong cùng transaction**; lỗi ⇒ rollback. Handler (use case) lấy event từ aggregate và publish **tường minh trước commit** |
| Khác | `ICurrentUser`, `IOutboxWriter` (interface) |

### Phần nào của ADR-0017 bị thay

| Ý của ADR-0017 | Tình trạng |
|---|---|
| Không base `Entity` / `AggregateRoot` / `ValueObject` ở `Shared` | **Bị thay** bởi D-A |
| Không domain event trong aggregate | **Bị thay**: aggregate gom event; thời điểm publish vẫn do handler quyết |
| Không generic `ICommandHandler<,>` / handler đăng ký thẳng từng class | **Bị thay** bởi D-B |
| Không MediatR, không mediator dispatch | **Giữ nguyên** |
| Transaction tường minh trong handler, không filter / decorator transaction | **Giữ nguyên** |
| Repository theo aggregate, không generic `IRepository<T>`, không Specification | **Giữ nguyên** (thêm: chỉ cho `AggregateRoot`) |

## Considered Options

| Phương án | Vì sao không chọn |
|---|---|
| Giữ nguyên ADR-0017 (không base class, handler trần) | Mỗi module tự chọn khuôn; không có điểm bám cho arch test và quy ước EF; decorator không áp được cho mọi handler |
| `ValueObject` kiểu `GetEqualityComponents()` | `record` đã cho value equality; thêm cơ chế tự viết là thừa |
| Dispatch event tự động trong `SaveChanges` interceptor | Giấu thời điểm publish — lý do ADR-0017 giữ transaction tường minh vẫn đúng; handler event có thể chạy khi repository chưa sẵn sàng |
| Event do handler tự tạo, aggregate không biết | Hai nơi cùng biết "chuyện gì xảy ra"; dễ quên |
| MediatR / mediator tự viết | Che luồng gọi (mất "controller gọi thẳng handler"); MediatR đổi sang license thương mại từ v13 |
| Decorator transaction (mở-commit theo `Result`) | Sai với ADR-0011: giữ ghế thất bại vẫn phải commit bản ghi idempotency chứa `409` |
| Typed id chỉ cho id đi qua `.Contracts` | Hai kiểu id (`Guid` trần và typed) trong một codebase ⇒ quy ước EF / Dapper có hai nhánh |

## Consequences

- Mọi aggregate / handler có cùng hình dạng; review một cái là review được tất cả.
- Arch test mở rộng được: `Domain/` không phụ thuộc hạ tầng, value object không setter, repository chỉ cho
  `AggregateRoot` (kiểm bằng base class).
- Thêm một tầng kỹ thuật (decorator) nhưng **không** thêm ma thuật transaction: ranh giới transaction vẫn đọc
  được trong handler.
- Thêm việc nền móng (M0): ba ticket (domain primitives, application primitives, persistence) phải xong trước module đầu tiên.
- Aggregate chỉ đổi bằng SQL tay (`Showtime seat` ở hot path) không có class — base class áp cho aggregate
  được nạp thành object, nên hot path không bị ảnh hưởng.
- **Xem lại khi:** cần aggregate kế thừa lớp con; hoặc tách module ra service (event in-process đổi thành outbox + consumer — D10).
