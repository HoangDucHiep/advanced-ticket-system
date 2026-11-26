# M1-01: Identity — đăng ký, đăng nhập (phát JWT), `/me`, contract vai trò, seed

Mode: you-implement-i-review
Status: ready
Blocked by: M0-09
Traces to: FR-33, FR-10
Concurrency: —
Branch: m1-01-identity-register-login

## Mục tiêu
Có người dùng thật với bốn vai trò để các ticket sau gắn quyền. Đây là **module thật đầu tiên dùng nền móng M0**
— `AggregateRoot` / `ValueObject`, handler theo interface, transaction tường minh, repository dịch lỗi `23505`.
Năm module còn lại sao chép khuôn này, nên làm đúng ở đây quan trọng hơn làm nhanh.
Xác thực JWT, policy vai trò và `ICurrentUser` đã dựng ở M0-06; ticket này chỉ **phát** token và hiện thực
contract vai trò mà M0-06 đang dùng bản giả.

## Phạm vi
**Trong:**
- `POST /api/auth/register` (luôn vai trò `Customer`), `POST /api/auth/login`, `GET /api/auth/me`.
- Phát JWT access token (15 phút) lúc login, ký bằng khoá từ `JwtOptions` của M0-06; token chỉ chứa **ai**
  (`sub`) — claim `role` (nếu có) chỉ để client hiển thị.
- Hiện thực interface contract vai trò trong `IdentityAccess.Contracts` (định nghĩa ở M0-06) bằng cách đọc
  `identity.users` theo `role_version` (BR-26, R8); thay bản giả của test host bằng bản thật.
- Policy và `ICurrentUser` **không** làm ở đây — dùng từ M0-06 cho nhóm đường dẫn `/api/manager/...`,
  `/api/admin/...`, `/api/check-in/...`.
- Test đặt ở `TicketSystem.IdentityAccess.Tests`, tạo từ template M0-08, dùng `TicketSystem.Testing`.
- Schema `identity`: `users`, `role_changes` (tạo bảng + `REVOKE UPDATE, DELETE` cho `role_changes`); migration `AddUsers`.
- Domain: aggregate `User` (`sealed`, factory trả `Result<T>`), value object `Email` (chữ thường, kiểm định dạng),
  `Role`, `UserErrors`; `IUserRepository` dịch `23505` của **đúng một** constraint email thành `EMAIL_TAKEN`.
- Seed tài khoản mỗi vai trò (để các ticket sau và E2E dùng): người học chọn cơ chế seed và ghi cách chạy.

**Ngoài:**
- Refresh token, `/auth/refresh`, `/auth/logout`, bảng `refresh_tokens` (M2-03) — nên `login` ở ticket này chưa
  trả `refreshToken`.
- `GET /api/admin/users`, `PUT /api/admin/users/{userId}/role`, ghi `role_changes` khi đổi quyền (M2-04) — bảng
  `role_changes` chỉ cần tồn tại.
- Rate limit `login` (M3-04).

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B1 — Result pattern", "B2 — Khi nào exception,
  khi nào `Result`" (mẫu "kiểm trước + constraint là chốt chặn"), "B5 — Validation hai tầng", "B6 — Use case: handler
  theo interface", "B7 — Ai mở và commit transaction", "B8 — Repository", "B9 — Domain primitives", "B16 — Auth
  foundation"; mục "Cấu trúc thư mục bên trong một module" và "Ví dụ xuyên suốt — POST /api/auth/register".
- Ticket nền: M0-02, M0-03, M0-04, M0-06, M0-08.
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Identity" (register, login, me).
- [`data-model.md`](../../../docs/design/data-model.md) — mục "Schema identity" (`users`, `role_changes`).
- [`cross-context-contracts.md`](../../../docs/domain/cross-context-contracts.md) — mục "Hợp đồng: Identity & Access → các context (R8)".
- [`data-model.md`](../../../docs/design/data-model.md) — mục "Cache vai trò" (vì sao không tin claim `role`), BR-26 ở
  [`04-business-rules.md`](../../../docs/prd/04-business-rules.md).
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-10…C-14, C-22…C-25.

## Test
Gợi ý — người học viết. Integration qua HTTP + Postgres thật (C-29); domain test ở `TicketSystem.IdentityAccess.Tests`.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Register_valid_returns_201_customer` | `201`, `role = Customer`, body không có `password_hash` | Lộ hash, hoặc cho chọn vai trò |
| `Register_email_taken_returns_409` | `409 EMAIL_TAKEN`, có `action` | Trả 500 |
| `Register_same_email_concurrently_one_wins` | 20 request cùng email song song: đúng 1 `201`, còn lại `409 EMAIL_TAKEN` (không có 500) | Bỏ bắt `23505` ở repository (chỉ dựa vào kiểm trước) |
| `Register_short_password_returns_400` | `400 VALIDATION_FAILED` | Validate ở sai tầng |
| `Login_wrong_password_and_unknown_email_look_same` | Cả hai `401 INVALID_CREDENTIALS`, body giống nhau | Lộ việc email có tồn tại |
| `Me_reads_role_from_db_not_token` | Đổi `role` của user trong DB (tăng `role_version`) ⇒ cùng một token, `/me` và policy thấy vai trò mới ngay | Đọc `role` từ claim |
| `Admin_route_rejects_customer_with_403` | `Customer` gọi route nhóm `/api/admin/...` ⇒ `403`; không token ⇒ `401` | Contract vai trò thật không nối vào `ICurrentUser` |
| `Login_token_is_accepted_by_auth_foundation` | Token phát lúc login qua được xác thực của M0-06 và gọi được `/me` | Ký sai khoá / issuer / audience so với `JwtOptions` |

## Câu hỏi review
1. Vì sao `role` không nằm trong JWT? Contract đọc vai trò từ DB mỗi request; cache Redis theo `role_version` (data-model "Cache vai trò") đến ở M2-04 khi có đổi vai trò — vì sao làm theo thứ tự đó?
2. Hai request đăng ký cùng email chạy song song: điều gì thật sự ngăn trùng — kiểm `EmailExistsAsync` hay constraint? Vì sao cần cả hai?
3. `Email` là value object: ai chịu trách nhiệm chuẩn hoá chữ thường, và `CHECK (email = lower(email))` ở DB bảo vệ cái gì?

## Done khi
- [ ] Test trên xanh; migration `AddUsers` chạy được từ DB trống
- [ ] Cây thư mục khớp mẫu của B (Domain / Application / Infrastructure / Api); chỉ `IdentityAccessModule` + controller là `public`
- [ ] Seed đủ bốn vai trò, có hướng dẫn chạy; không secret/mật khẩu thật trong diff
