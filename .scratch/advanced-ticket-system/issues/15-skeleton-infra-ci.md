# 15: Skeleton — solution, Docker Compose, CI

Mode: ai-implements
Gate: D
Layer: code
Blocked by: 14
Traces to: —

> **Đồng bộ với design (2026-11-25, Gate C đóng).** Khi ticket này mâu thuẫn với `docs/design/`,
> **design thắng**. Cấu trúc solution theo `docs/design/architecture.md` D2 (6 module `Scheduling`, `Inventory`,
> `Booking`, `Payment`, `Ticketing`, `IdentityAccess`, mỗi module `<Module>` + `<Module>.Contracts`;
> `Shared.Abstractions`, `Shared.Infrastructure`; host `TicketSystem.Api`). Unit of Work theo request
> (ADR-0008) và khung architecture test (quy tắc 1–8 của D9) thuộc ticket này. Postgres cần extension
> `btree_gist`.
>
> **Bố cục repo (2026-11-25):** mọi thứ của .NET (`TicketSystem.sln`, `global.json`,
> `Directory.Build.props`, `src/`, `tests/`) nằm trong `backend/`. Gốc repo chỉ giữ compose, `infra/`,
> `.github/`, `.gitignore` chung (architecture.md D2 — "Bố cục repo").

## Vì sao có ticket này

Mọi ticket sau đều cần một nơi để chạy code. Ticket này dựng nền: solution, hạ tầng, CI.
Nó là ticket duy nhất trong Gate D mà AI làm phần lớn, vì nó **không chứa quyết định nghiệp vụ
nào** — chỉ là wiring.

Sau ticket này, mỗi ticket tiếp theo chỉ viết code nghiệp vụ mà không phải sửa hạ tầng.

## Bạn tạo ra

- **Solution** đúng cấu trúc đã chốt ở T10:
  xem cây thư mục ở `docs/design/architecture.md` D2 (thay cho bản phác thảo cũ ở đây).
- **`docker-compose.yml`**: Postgres + Redis, có healthcheck, có named volume, có port
  được cấu hình (không hardcode).
- **CI** (GitHub Actions): restore → build → test. Từ ticket sau, thêm integration test với
  Testcontainers.
- **`.gitignore`**: phải loại `.env`, `secrets.json`, `bin/`, `obj/`, `*.user`. **Không được
  có secret nào được commit.**
- **`.editorconfig`** và format cơ bản.
- **Cấu hình secrets**: `appsettings.json` chỉ chứa giá trị không nhạy cảm; biến môi trường
  cho CI; user-secrets cho local.
- **Lệnh chạy được từ sạch**: `docker compose up -d` → `dotnet build` → `dotnet test` → app
  chạy và trả về `200` ở endpoint health.

## AI làm gì

AI làm toàn bộ phần wiring, scaffolding, cấu hình hạ tầng và CI. AI **không** viết logic
nghiệp vụ nào ở ticket này — không có model, không có endpoint nghiệp vụ.

AI báo cáo lại: cấu trúc đã tạo, cách chạy, và những chỗ cần bạn điền (ví dụ chuỗi kết nối
Postgres, secret Stripe).

## Tiêu chí hoàn thành

- [ ] `docker compose up -d` dựng Postgres và Redis, cả hai báo healthy
- [ ] `dotnet build` sạch, không warning
- [ ] `dotnet test` chạy và qua (unit test có thể chưa có, nhưng lệnh phải thành công)
- [ ] App chạy được và trả `200` ở endpoint health
- [ ] CI xanh trên GitHub
- [ ] `.gitignore` loại `.env`, `secrets.json`, `bin/`, `obj/`
- [ ] **Không có secret nào trong file nào được commit** — kiểm tra bằng cách tìm chuỗi khóa
- [ ] Cấu trúc thư mục khớp với T10 (`architecture.md` D2)
- [ ] Architecture test kiểm quy tắc phụ thuộc 1–3 của D9 (các quy tắc còn lại thêm dần theo module)
- [ ] `IUnitOfWork`: một connection + một transaction mỗi request, mọi `DbContext` gắn vào (ADR-0008)
