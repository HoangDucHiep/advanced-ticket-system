# M0-06: Auth foundation — JWT bearer, policy vai trò, `ICurrentUser`, contract vai trò

Mode: you-implement-i-review
Status: ready
Blocked by: M0-03, M0-05
Traces to: FR-33
Concurrency: —
Branch: m0-06-auth-foundation

## Mục tiêu
Xác thực và phân quyền dựng một lần trước mọi module: ticket sau chỉ gắn policy lên controller. Vai trò hiện
hành lấy qua contract, **không** từ claim, để thu quyền có hiệu lực ngay (BR-26). Phát token thuộc Identity.

## Phạm vi
**Trong:**
- JWT bearer: validate issuer, audience, khoá ký từ Options + `ValidateOnStart` (cấu hình sai ⇒ không khởi động).
- Policy vai trò đặt tên theo GLOSSARY: `Customer`, `TheaterManager`, `GateStaff`, `Administrator` (hằng số, C-14).
- `ICurrentUser` (interface từ M0-03) hiện thực từ `HttpContext`: id từ `sub`, vai trò hỏi qua contract.
- Interface contract vai trò trong `IdentityAccess.Contracts` (hỏi vai trò hiện hành theo user id). Test host
  dùng **bản giả** cho tới khi M1-01 hiện thực thật.
- Claim `role` không bao giờ được dùng để phân quyền.

**Ngoài:**
- Phát token (login, refresh), `/me`, seed tài khoản (M1-01); refresh token (M2-03).
- Cache Redis theo `role_version` + đổi vai trò (M2-04); ở M0-06 bản giả / bản đơn giản là đủ.
- Gắn policy lên endpoint cụ thể (ticket có endpoint).

## Đọc trước
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B16 — Auth foundation", "B11 — Cấu hình:
  Options pattern + kiểm khi khởi động".
- [`data-model.md`](../../../docs/design/data-model.md) — mục "Cache vai trò" (BR-26; vì sao không tin claim).
- [`cross-context-contracts.md`](../../../docs/domain/cross-context-contracts.md) — mục "Hợp đồng: Identity & Access → các context (R8)".
- [`GLOSSARY.md`](../../../GLOSSARY.md) — tên vai trò; [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-14, C-22.

## Test
Gợi ý — người học viết. Dùng `WebApplicationFactory` + controller chỉ có trong test + bản giả contract vai trò;
token do helper trong test tự ký (helper dùng chung ở M0-08).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `No_token_returns_401_problem_json` | Endpoint cần policy, không token ⇒ `401` problem+json | Policy không áp, hoặc trả body rỗng |
| `Wrong_role_returns_403` | Token hợp lệ, vai trò (theo contract) không đủ ⇒ `403` | Đọc vai trò từ claim, hoặc không kiểm |
| `Right_role_returns_200` | Vai trò đúng ⇒ `200` | Policy chặn nhầm |
| `Role_change_visible_on_next_request` | Cùng một token; bản giả contract đổi vai trò ⇒ request kế tiếp đổi kết quả 403 ⇄ 200 | Vai trò bị giữ trong token / cache cục bộ |
| `Role_claim_in_token_is_ignored` | Token mang claim `role = Administrator` nhưng contract trả `Customer` ⇒ `403` | Claim `role` được dùng để phân quyền |
| `Expired_or_wrong_signature_token_returns_401` | Token hết hạn / sai khoá / sai issuer / sai audience ⇒ `401` | Bỏ validate một trong các tham số |
| `CurrentUser_exposes_id_and_role` | `ICurrentUser` trả đúng id (từ `sub`) và vai trò (từ contract) | Đọc nhầm nguồn |
| `Invalid_jwt_options_fail_at_startup` | Khoá ký ngắn / thiếu issuer ⇒ host không khởi động | Thiếu `ValidateOnStart` |

## Câu hỏi review
1. Vì sao `role` không nằm trong JWT dù đỡ một lần đọc? Điều gì xảy ra khi thu quyền một quản trị viên?
2. Contract vai trò đặt ở `IdentityAccess.Contracts` thay vì để host gọi thẳng DB của Identity — lợi gì (R8, D9)?
3. Redis lỗi thì `ICurrentUser` nên làm gì (data-model "Cache vai trò")? Chọn fail-open hay fail-closed và vì sao?

## Done khi
- [ ] Các test trên xanh; không secret thật trong diff
- [ ] Không chỗ nào trong repo đọc claim `role` để phân quyền
- [ ] Tên policy là hằng số, khớp GLOSSARY
