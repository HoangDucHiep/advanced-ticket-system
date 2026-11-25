---
id: 0008
title: Mỗi module một DbContext, mọi module dùng chung một connection và một transaction
status: accepted
date: 2026-11-25
Related: ADR-0005, ADR-0006, ADR-0009
---

# Mỗi module một `DbContext`, mọi module dùng chung một connection và một transaction

## Context

Hệ thống là modular monolith 6 module (`docs/design/architecture.md` D1, D2). Có năm "ranh giới bị
cắt" (`docs/domain/cross-context-contracts.md`) mà một DB transaction phải chạm nhiều module, ví dụ
giữ ghế (`Inventory` + `Booking`) hay thanh toán thành công (`Payment` + `Booking` + `Inventory` +
`Ticketing`). Đồng thời quy tắc "không module nào đọc bảng của module khác" phải được giữ.

Roadmap ban đầu chốt "1 `DbContext` + `HasDefaultSchema`" — quyết định đó có trước khi có domain.

## Decision

- Mỗi module có **một `DbContext` riêng**, `HasDefaultSchema("<module>")`, migrations riêng (bảng lịch
  sử migration nằm trong schema của module).
- Host mở **một `NpgsqlConnection` và một `DbTransaction` cho mỗi request / mỗi lượt worker** qua
  `IUnitOfWork` (scope theo request). Mọi `DbContext` được tạo trên connection đó và gắn vào
  transaction đang mở (`Database.UseTransaction`); SQL tay / Dapper dùng cùng connection + transaction.
- Commit **một lần** ở cuối use case; exception ở bất kỳ module nào ⇒ rollback toàn bộ.

## Considered Options

| Phương án | Vì sao không chọn |
|---|---|
| Một `DbContext` chung cho mọi module | Transaction tự nhiên là một, nhưng `DbContext` biết entity của mọi module ⇒ module nào cũng `Set<T>()` được bảng của module khác; ranh giới chỉ còn là kỷ luật |
| `TransactionScope` bao nhiều `DbContext` | Hai connection ⇒ distributed transaction (hỗ trợ hạn chế trên Npgsql), khó kiểm soát và khó test |
| Mỗi module transaction riêng + saga | Mất atomic ở cả năm ranh giới bị cắt — có lúc ghế `Held` mà không có đơn, `Paid` mà không có vé |

## Consequences

- Năm ranh giới bị cắt là **một transaction Postgres thật** — test concurrency chứng minh được.
- Phải viết `IUnitOfWork` cẩn thận: một connection mỗi scope, mọi `DbContext` phải gắn vào cùng
  transaction trước lệnh đầu tiên. Test kiến trúc (T15) kiểm tra không `DbContext` nào tự mở
  connection riêng.
- Tách một module thành service ⇒ connection chung không còn ⇒ ranh giới bị cắt của module đó phải
  thành saga/outbox (điều kiện tách: `architecture.md` D10).
