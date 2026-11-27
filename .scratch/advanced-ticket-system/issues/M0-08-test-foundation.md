# M0-08: Test foundation — `TicketSystem.Testing`, template test module, arch test, module mẫu

Mode: pair-tdd
Status: ready
Blocked by: M0-04, M0-06, M0-07
Traces to: —
Concurrency: —
Branch: m0-08-test-foundation

## Mục tiêu
Mọi ticket sau viết test integration và concurrency bằng một bộ công cụ dùng chung, và các luật kiến trúc
được máy kiểm. Ticket này cũng chốt **demo M0**: một module mẫu chỉ sống trong code test, đi qua toàn bộ khuôn.

## Phạm vi
**Trong:**
- Project `tests/TicketSystem.Testing`: fixture Postgres + Redis (Testcontainers, collection fixture dựng một
  lần); reset dữ liệu giữa test bằng Respawn; `ApiFactory` (WebApplicationFactory) cấp connection string từ
  container + `FakeTimeProvider`; helper tạo access token theo từng vai trò; helper concurrency (N request qua
  `Barrier`, lặp N vòng, gom kết quả); cơ chế `Concurrency:UnsafeVariant`.
- Template project test module `tests/Modules/TicketSystem.<Module>.Tests` (unit domain + integration) và hướng
  dẫn dùng, kèm `InternalsVisibleTo`.
- Arch test mở rộng (cùng `TicketSystem.ArchitectureTests`): `Domain/` không phụ thuộc EF Core / ASP.NET Core /
  Npgsql / Dapper; chỉ `<Module>Module` + controller là `public`; value object không có setter; repository chỉ
  cho `AggregateRoot`; `IDomainEvent` không nằm trong `.Contracts`; `IIntegrationEvent` chỉ nằm trong `.Contracts`,
  không chứa kiểu của `Domain/` (ADR-0019).
- **Module mẫu chỉ trong test**: aggregate + value object + handler + endpoint + migration.

**Ngoài:**
- Test nghiệp vụ của module thật (từng ticket module); kiểm toán test concurrency (M2-11).
- Script chạy test, CI report (M0-09).

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B18 — Test foundation", "B9" (điểm bám arch
  test), "Cấu trúc thư mục bên trong một module".
- [`concurrency.md`](../../../docs/design/concurrency.md) — mục "Hạ tầng test concurrency (dùng chung)".
- [`architecture.md`](../../../docs/design/architecture.md) — mục "D9", đoạn "Quy tắc phụ thuộc (kiểm được)".
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — mục 7 (test), C-29, C-30.

## Test
AI viết các test này trước. Nhóm 1 kiểm hạ tầng test; nhóm 2 là arch test; nhóm 3 là demo M0 trên module mẫu.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Fixture_starts_postgres_and_redis_once` | Hai test class cùng collection dùng chung một container | Mỗi class dựng container riêng |
| `Respawn_resets_data_between_tests` | Dữ liệu test A không thấy ở test B; schema + migration còn nguyên | Rò dữ liệu, hoặc reset xoá cả bảng lịch sử migration |
| `ApiFactory_uses_container_config_and_fake_time` | Host dùng connection string container; đổi `FakeTimeProvider` ⇒ handler thấy giờ mới | Dùng config tĩnh / đồng hồ thật |
| `Token_helper_issues_token_per_role` | Token từng vai trò qua được xác thực của host (M0-06) | Helper ký sai khoá |
| `Concurrency_helper_overlaps_requests` | N task chờ `Barrier` rồi mới gửi; lặp N vòng, gom đủ kết quả | Request không chồng lấn thật |
| `UnsafeVariant_flag_switches_mechanism` | Bật `Concurrency:UnsafeVariant` ⇒ module mẫu dùng cơ chế sai và test mẫu **đỏ** | Cờ không có tác dụng |
| `Domain_does_not_reference_infrastructure` | `Domain/` không tham chiếu EF Core, ASP.NET Core, Npgsql, Dapper | Domain kéo hạ tầng |
| `Only_module_and_controllers_are_public` | Mỗi assembly `TicketSystem.<Module>` (không `.Contracts`) chỉ lộ `<Module>Module` + controller | Thêm một `public class` handler |
| `Value_objects_have_no_setters` | Mọi kiểu kế thừa `ValueObject` không có setter public | Có setter |
| `Repositories_only_for_aggregate_roots` | Repository chỉ tham số hoá bằng `AggregateRoot<TId>` | Repository cho entity con |
| `Domain_events_stay_inside_module` | Không kiểu `IDomainEvent` nào nằm trong assembly `.Contracts` | Domain event bị dùng làm hợp đồng |
| `Integration_events_live_in_contracts_and_are_flat` | Mọi `IIntegrationEvent` nằm trong `.Contracts`; property không có kiểu từ assembly nội bộ của module | Hợp đồng lộ kiểu domain |
| `Demo_invalid_request_returns_400_with_traceid_in_log` | Request sai ⇒ `400` problem+json, `traceId` tìm được trong log | Một mắt xích của M0-05 / M0-07 bị gãy |
| `Demo_business_error_returns_409` | Handler module mẫu trả lỗi `Conflict` ⇒ `409` | Map sai |
| `Demo_domain_event_published_in_transaction` | Aggregate raise domain event, handler publish trước commit; lớp dịch phát integration event, module mẫu thứ hai nhận trong cùng transaction; handler lỗi ⇒ rollback cả hai | Event chạy sau commit / không rollback |
| `Demo_role_policy_401_403` | Endpoint cần vai trò: không token ⇒ `401`; sai vai trò ⇒ `403` | Policy / contract chưa nối |

Câu hỏi thiết kế cho người học: arch test cần nạp assembly đã build — nạp bằng cách nào mà không tự tạo phụ
thuộc mới vào module? Chọn và ghi lý do.

## Câu hỏi review
1. Vì sao Respawn thay vì dựng DB lại cho mỗi test? Bảng nào phải loại khỏi reset?
2. `Concurrency:UnsafeVariant` chỉ có trong build test — vì sao không để ở production?
3. Vì sao arch test có thể kiểm "chỉ aggregate root có repository" — điều gì ở M0-02 làm nó khả thi?

## Done khi
- [ ] Các test trên xanh; CI chạy cả arch test mới
- [ ] Không có kiểu `public` thừa trong 6 module hiện có (sửa nếu có)
- [ ] Module mẫu nằm hoàn toàn trong project test, không lọt vào assembly production
