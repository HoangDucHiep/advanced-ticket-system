# M0-03: Application primitives — handler, đăng ký theo quét, decorator, event bus + integration event, `ICurrentUser`, `IOutboxWriter`

Mode: pair-tdd
Status: done
Blocked by: M0-02
Traces to: —
Concurrency: —
Branch: m0-03-application-primitives

## Mục tiêu
Mỗi use case là một handler theo một khuôn: interface chung, tự được đăng ký, tự có log + trace, và phản ứng
chéo module chạy trong cùng transaction. Không có mediator (ADR-0017/0018).

## Phạm vi
**Trong** (`Shared.Abstractions/Application/` + `Shared.Infrastructure`):
- `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler<,>`, `IQueryHandler<,>` (trả `Result<T>`).
- Quét assembly của module để đăng ký mọi handler (Scoped) bằng một dòng trong `Add<Module>Module`.
- Một decorator dùng chung: log bắt đầu/kết thúc + thời gian + `code` khi thất bại; tạo `Activity`.
  **Không** decorator transaction (B7).
- `Shared.Abstractions/Events/`: `IIntegrationEvent` (`EventId`, `OccurredAt`), `IEventPublisher` (publish domain
  event hoặc integration event), `IEventHandler<TEvent>`; bus in-process chạy mọi handler đồng bộ trong cùng
  transaction, đăng ký handler event bằng cùng lần quét assembly (B13, ADR-0019).
- Khai báo interface `ICurrentUser` và `IOutboxWriter` (chỉ interface).

**Ngoài:**
- Hiện thực `ICurrentUser` từ `HttpContext` + vai trò (M0-06); hiện thực `IOutboxWriter` + dispatcher (M2-05).
- Metric gắn vào decorator (M3-01).
- Mediator / `Send(...)` dispatch: không làm (ADR-0017).

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B6 — Use case: handler theo interface,
  quét đăng ký, decorator; không mediator", "B7 — Ai mở và commit transaction", "B13 — Domain event và bus
  in-process", "B17 — Observability foundation" (dòng decorator log / trace).
- [ADR-0017](../../../docs/adr/0017-handler-per-use-case-no-mediator.md), [ADR-0018](../../../docs/adr/0018-domain-and-application-primitives.md),
  [ADR-0019](../../../docs/adr/0019-integration-events-separate-from-domain-events.md),
  [ADR-0008](../../../docs/adr/0008-cross-module-transaction.md), [ADR-0009](../../../docs/adr/0009-module-communication.md).
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — mục 8, C-04.

## Test
AI viết các test này trước. Unit ở `TicketSystem.Shared.Tests`; test bus + rollback là integration trên
Postgres thật với `IUnitOfWork` đã có trong skeleton (`TicketSystem.IntegrationTests` cho tới khi M0-08 có fixture chung).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Scan_registers_every_handler_in_assembly` | Assembly mẫu có N handler ⇒ container resolve được cả N theo interface, lifetime Scoped | Quên một handler, hoặc đăng ký Singleton |
| `Scan_ignores_non_handlers_and_abstracts` | Lớp abstract / không phải handler không bị đăng ký | Quét quá rộng |
| `Decorator_logs_success_with_duration` | Handler thành công ⇒ log bắt đầu + kết thúc, có thời gian | Không log, hoặc log thiếu thời gian |
| `Decorator_logs_failure_code` | Handler trả `Result` thất bại ⇒ log có `code` của lỗi | Log mất `code` |
| `Decorator_creates_activity` | Một `Activity` được tạo mỗi lần chạy, tên theo handler | Không có span |
| `Decorator_does_not_open_transaction` | Chạy handler qua decorator không mở/commit transaction | Decorator giấu ranh giới transaction (B7) |
| `EventBus_runs_all_handlers_in_same_transaction` | Publish một event có 2 handler ⇒ cả hai chạy, cùng transaction của `IUnitOfWork` | Handler chạy ngoài transaction hoặc bị bỏ sót |
| `Failing_event_handler_rolls_back_whole_use_case` | Handler event ném ⇒ exception lan ra, dòng ghi bởi handler phát **không** nằm trong DB | Nuốt exception, hoặc commit trước publish |
| `EventBus_with_no_handlers_is_noop` | Event không ai nghe ⇒ không lỗi | Ném khi không có handler |
| `EventBus_dispatches_by_runtime_type` | Publish qua tham số `IDomainEvent` ⇒ handler của kiểu cụ thể chạy | Dispatch theo kiểu khai báo ⇒ không handler nào chạy |
| `Domain_event_translated_to_integration_event_in_same_transaction` | Use case ghi schema A + publish domain event ⇒ lớp dịch phát integration event ⇒ handler "module B" ghi schema B; commit ⇒ cả hai có dữ liệu | Integration event giao sau commit, hoặc lớp dịch bị bỏ qua |
| `Failing_integration_handler_rolls_back_publisher` | Handler integration event ở "module B" ném ⇒ dữ liệu schema A **không** commit | Nuốt exception, hoặc giao ngoài transaction |
| `No_mediator_dependency` | Không project nào tham chiếu package MediatR | Thêm MediatR |

## Câu hỏi review
1. Vì sao controller inject đúng interface của handler thay vì một `Send(...)` chung?
4. Vì sao domain event không được đặt ở `.Contracts`, và lớp dịch sang integration event thuộc module nào?
2. Event bus chạy đồng bộ trong transaction: ưu / nhược điểm so với publish sau commit (B13)?
3. Vì sao decorator không mở transaction, dù nó bọc mọi handler?

## Done khi
- [x] Các test trên xanh; test rollback chạy trên Postgres thật
- [x] Quét đăng ký chỉ chạy lúc khởi động, không reflection mỗi request
- [x] `ICurrentUser`, `IOutboxWriter` chỉ là interface, không có hiện thực rỗng "cho có"
