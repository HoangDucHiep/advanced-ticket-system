---
id: 0019
title: Integration event tách khỏi domain event; module phát dịch domain event sang integration event
status: accepted
date: 2026-11-27
Related: ADR-0008, ADR-0009, ADR-0018
Supersedes: một phần ADR-0018 (luật "event cho module khác là `IDomainEvent` đặt ở `.Contracts`")
---

# Integration event tách khỏi domain event; module phát dịch domain event sang integration event

Chi tiết: [`docs/design/building-blocks.md`](../design/building-blocks.md) B9, B13.

## Context

ADR-0018 cho aggregate gom `IDomainEvent`, và quy định event cần cho module khác cũng là `IDomainEvent` nhưng
đặt type ở `<Module>.Contracts`. Người học hỏi "integration event thì sao?" — đọc lại thì một loại event đang
làm hai việc, và hỏng ở bốn chỗ cụ thể của dự án:

1. **Payload khác nhau.** `Inventory` cần `ShowtimeCreated` mang danh sách ghế của phòng + loại ghế
   (architecture D4 #5), nhưng aggregate `Showtime` không giữ ghế — ghế thuộc aggregate `Room`. Domain event chỉ
   biết những gì aggregate biết; event cho module khác cần được **làm giàu**.
2. **Kiểu dữ liệu.** Domain event dùng kiểu nội bộ (value object, typed id của module). `.Contracts` chỉ được
   reference `Shared.Abstractions` (D9 luật 2) — hợp đồng phải phẳng và ổn định.
3. **Hướng phụ thuộc.** Aggregate raise thẳng type ở `.Contracts` ⇒ `Domain/` bị trói vào hợp đồng công khai;
   đổi chi tiết nội bộ thành đổi hợp đồng giữa module.
4. **Hai cách giao.** Năm ranh giới bị cắt cần giao **trong transaction** (ADR-0009); việc phụ giao **sau commit**
   qua outbox (D8). Cách giao là thuộc tính của event qua ranh giới module, không phải của domain event.

## Decision

| | Domain event | Integration event |
|---|---|---|
| Marker | `IDomainEvent` (`Shared.Abstractions/Domain/`) | `IIntegrationEvent` (`Shared.Abstractions/Events/`) |
| Nằm ở | `Domain/` của module, `internal` | `<Module>.Contracts`, `public` |
| Ai tạo | Aggregate raise khi đổi trạng thái | Handler domain event **của chính module phát** (lớp dịch), làm giàu nếu cần |
| Ai nghe | Chỉ module đó | Module khác |
| Dữ liệu | Kiểu domain | Phẳng: `Guid`, số, chuỗi, thời gian, DTO lồng; chỉ được **thêm** trường |
| Giao | In-process, trong transaction | In-tx qua `IEventPublisher` (năm ranh giới bị cắt) **hoặc** sau commit qua `IOutboxWriter` |

Một bus (`IEventPublisher` + `IEventHandler<TEvent>`) giao cả hai loại trong transaction; chọn loại nào là
việc của type, không phải của bus. Đặt tên trong code: hậu tố `DomainEvent` / `IntegrationEvent`; tài liệu
domain dùng tên khái niệm (`ShowtimeCreated`, `BookingPaid`) để chỉ integration event.

## Considered Options

| Phương án | Vì sao không chọn |
|---|---|
| Một loại event cho cả hai (ADR-0018) | Bốn vấn đề ở Context |
| Integration event luôn qua outbox | Mất atomic của năm ranh giới bị cắt (ADR-0009): suất tạo xong mà chưa có ghế, đơn `Paid` mà chưa có vé |
| Use-case handler tự tạo integration event, bỏ domain event | Hai nơi phải biết "chuyện gì xảy ra"; use case quên phát event là lỗi im lặng |
| Bảng inbox cho bên nhận | Bên nhận cùng process, cùng transaction ⇒ không trùng; chỉ cần khi tách service (D10) |

## Consequences

- Thêm một lớp dịch mỗi khi một domain event cần đi ra ngoài module — vài dòng, đổi lấy hợp đồng ổn định.
- Arch test (B18): `IDomainEvent` không xuất hiện trong `.Contracts`; `IIntegrationEvent` chỉ nằm trong
  `.Contracts` và không chứa kiểu của `Domain/`.
- Khi tách một module ra service: integration event của nó đổi sang giao qua outbox + broker; domain event
  không đổi.
