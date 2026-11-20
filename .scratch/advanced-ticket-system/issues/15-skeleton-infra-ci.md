# 15: Skeleton — solution, Docker Compose, CI

Mode: ai-implements
Gate: D
Layer: code
Blocked by: 14
Traces to: —

## Vì sao có ticket này

Mọi ticket sau đều cần một nơi để chạy code. Ticket này dựng nền: solution, hạ tầng, CI.
Nó là ticket duy nhất trong Gate D mà AI làm phần lớn, vì nó **không chứa quyết định nghiệp vụ
nào** — chỉ là wiring.

Sau ticket này, mỗi ticket tiếp theo chỉ viết code nghiệp vụ mà không phải sửa hạ tầng.

## Bạn tạo ra

- **Solution** đúng cấu trúc đã chốt ở T10:
  ```
  src/
    Api/
    Modules/{Identity,Catalog,Showtimes,Booking,Payment,CheckIn}/
    Shared/
  tests/
    Unit/
    Integration/
    Load/
  docs/adr/
  perf/
  ```
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
- [ ] Cấu trúc thư mục khớp với T10
