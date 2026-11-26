# M0-09: Tooling & CI — analyzers, format check, test report + coverage, Mailpit, script, README, `.http`

Mode: ai-implements
Status: ready
Blocked by: M0-08
Traces to: —
Concurrency: —
Branch: m0-09-tooling-ci

## Mục tiêu
Những việc lặp mà con người hay quên (format, báo cáo, dựng môi trường) giao cho máy và script; người lạ
clone repo chạy lại được. Xong ticket này M0 đóng và M1-01 mở.

## Phạm vi
AI viết phần này; người học review.

**Trong:**
- Analyzers: `AnalysisLevel` latest-recommended + `EnforceCodeStyleInBuild`; `.editorconfig` hiện có; sửa các
  cảnh báo lộ ra (vẫn `TreatWarningsAsErrors`).
- CI (`.github/`): bước `dotnet format --verify-no-changes`; báo cáo test (trx) + coverage (coverlet) upload
  artifact. Coverage **không** là cổng chặn.
- Docker Compose: thêm Mailpit (SMTP dev) — đóng câu hỏi mở về email trong
  [`open-questions.md`](../../../docs/learning/open-questions.md).
- `scripts/` (PowerShell + sh): dựng môi trường, reset DB, chạy test concurrency (`Category=Concurrency`).
- `backend/README.md`: chạy local (compose, user-secrets, migrate, test).
- `backend/http/`: file `.http` mẫu cho các endpoint hiện có (health, OpenAPI).

**Ngoài:**
- README gốc của repo, báo cáo benchmark (M4-02).
- Prometheus / Grafana trong compose (M3-01).
- Đặt ngưỡng coverage làm cổng chặn: không làm (C-30).

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B19 — Tooling và CI".
- [`concurrency.md`](../../../docs/design/concurrency.md) — mục "Hạ tầng test concurrency (dùng chung)".
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-29, C-30.
- [`docs/learning/open-questions.md`](../../../docs/learning/open-questions.md) — câu hỏi về email.

## Test
Ticket `ai-implements`: không có test đỏ. Cách kiểm do người học chạy.

| Kiểm | Khẳng định | Đỏ nếu… |
|---|---|---|
| Build với analyzer | `dotnet build` xanh với `AnalysisLevel` + `EnforceCodeStyleInBuild` | Cảnh báo bị tắt thay vì sửa |
| `dotnet format --verify-no-changes` cục bộ | Xanh trên nhánh; cố ý làm bẩn một file ⇒ đỏ | Bước format không thất bại khi code sai style |
| CI trên GitHub | Có artifact trx + coverage ở một lần chạy | Thiếu artifact |
| `scripts/` env up (cả hai shell) | Compose lên đủ Postgres, Redis, pgAdmin, Mailpit | Thiếu dịch vụ, hoặc script chỉ chạy một shell |
| `scripts/` reset DB | DB về trạng thái sau migration | Còn dữ liệu cũ |
| `scripts/` chạy test concurrency | Chạy đúng bộ `Category=Concurrency` | Chạy cả bộ test |
| Làm theo `backend/README.md` từ máy sạch | Chạy được API + test không cần hỏi thêm | Thiếu bước (user-secrets, migrate) |
| `.http` mẫu | Gọi được endpoint hiện có khi API chạy | Tên / đường dẫn lệch API |

## Câu hỏi review
1. Vì sao coverage không phải cổng chặn (C-30)? Điều gì đo được khi coverage cao mà test sai?
2. `AnalysisLevel` mặc định bỏ sót gì mà latest-recommended bắt?
3. Script reset DB khác Respawn của M0-08 ở chỗ nào, dùng khi nào?

## Done khi
- [ ] CI xanh với format check; artifact trx + coverage tải được
- [ ] Mailpit trong compose; câu hỏi mở về email được đóng
- [ ] `backend/README.md` + `scripts/` + `backend/http/` có và được người học chạy thử
- [ ] Không secret trong diff
