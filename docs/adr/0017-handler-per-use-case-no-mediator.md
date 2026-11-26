---
id: 0017
title: Mỗi use case một handler gọi thẳng, transaction tường minh, repository theo aggregate — không mediator
status: accepted
date: 2026-11-26
Related: ADR-0008, ADR-0009, ADR-0011, ADR-0016
---

# Mỗi use case một handler gọi thẳng, transaction tường minh, repository theo aggregate — không mediator

Chi tiết: [`docs/design/building-blocks.md`](../design/building-blocks.md) B6–B8.

## Context

ADR-0009 bỏ mediator **giữa** module và để lại câu hỏi "dùng mediator *bên trong* module" cho tầng code
(T15). T15 không trả lời. Cùng lúc còn hai câu chưa có chủ: **ai** gọi `IUnitOfWork.BeginAsync` /
`CommitAsync` (ADR-0008 chỉ nói "commit một lần ở cuối use case"), và có dùng repository hay không.
Ba câu này quyết định hình dạng của mọi use case trong 6 module.

## Decision

- **Handler**: mỗi lệnh / truy vấn một class `internal sealed <Verb><Noun>Handler` với
  `HandleAsync(command, ct)` trả `Result<T>`; đăng ký thẳng DI trong `<Module>Module.cs`; controller
  inject và gọi trực tiếp. Không MediatR, không `ICommandHandler<>` generic.
- **Transaction**: handler gọi `BeginAsync` / `CommitAsync` **tường minh**. Không commit ⇒ `DisposeAsync`
  của Unit of Work rollback. Không có filter / middleware tự mở-commit.
- **Repository**: một interface cho mỗi aggregate root (phía ghi EF Core), method mang tên nghiệp vụ,
  không trả `IQueryable`, sở hữu `SaveChangesAsync` (để dịch `23505`). Truy vấn đọc chiếu thẳng ra DTO
  (`AsNoTracking` / Dapper). Hot path dùng class SQL tay đặt tên theo việc (D7).

## Considered Options

| Phương án | Vì sao không chọn |
|---|---|
| MediatR + pipeline behavior | Che luồng gọi (F12 không đi tới handler); cross-cutting chính (transaction) không được tự động hoá (dưới); MediatR chuyển license thương mại từ v13 |
| Generic `ICommandHandler<,>` + decorator (Scrutor) | Trừu tượng thêm khi chưa có cross-cutting thứ hai; thêm khi cần, kèm ADR thay thế |
| Filter / middleware commit khi `Result.IsSuccess` | **Sai với ADR-0011**: giữ ghế thất bại vẫn phải commit bản ghi idempotency chứa `409`; che ranh giới transaction khỏi review concurrency |
| Generic `IRepository<T>` | CRUD không khớp aggregate; lộ `IQueryable`; EF đã là repository + unit of work |
| Không repository, handler dùng `DbContext` | Chạy được, nhưng ranh giới aggregate chỉ là kỷ luật và mẫu dịch `23505` lặp ở mọi handler |

## Consequences

- Đọc một handler là thấy đủ: transaction mở ở đâu, đóng ở đâu, khoá gì — đúng thứ review concurrency
  cần (`concurrency.md`).
- Có lặp lại nhỏ (`BeginAsync` / `CommitAsync` ở mỗi handler) — chấp nhận, vì đó là chỗ cần nhìn thấy.
- Nếu T25 cần log / metric cho mọi use case mà lặp quá nhiều, xem lại bằng decorator + ADR mới.
