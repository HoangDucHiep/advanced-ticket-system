# M2-03: Refresh token rotation + reuse detection, logout

Mode: you-implement-i-review
Status: ready
Blocked by: M1-01
Traces to: FR-29
Concurrency: #6
Branch: m2-03-refresh-token-rotation

## Mục tiêu
Phiên đăng nhập sống dài bằng refresh token nhưng đánh cắp token thì bị phát hiện: mỗi lần làm mới token
cũ bị thay, dùng lại token đã thay thì cả chuỗi bị thu hồi. Không có ân hạn (ADR-0014).

## Phạm vi
**Trong:**
- Login (M1-01) phát refresh token: 32 byte ngẫu nhiên từ `RandomNumberGenerator` (C-24), lưu **SHA-256**
  trong `identity.refresh_tokens`, hạn 14 ngày, `family_id` mới cho mỗi lần đăng nhập.
- `POST /api/auth/refresh`: rotation bằng **một** câu `UPDATE … WHERE revoked_at IS NULL AND expires_at >
  now() RETURNING`; 1 dòng ⇒ chèn token mới cùng `family_id`; 0 dòng ⇒ không tồn tại / hết hạn ⇒
  `401 REFRESH_TOKEN_INVALID`; đã bị revoke ⇒ revoke cả family ⇒ `401 REFRESH_TOKEN_REUSED`.
- `POST /api/auth/logout`: revoke cả `family_id` của token, `204`.
- So sánh giá trị bí mật dùng `CryptographicOperations.FixedTimeEquals` (C-23) ở chỗ có so sánh trong
  code; chỗ tra bằng index theo hash thì ghi trong PR vì sao không cần so sánh thêm.

**Ngoài:**
- Rate limit `refresh` / `login` dùng chung nhiều instance → M3-04.
- Đổi vai trò làm mất hiệu lực token/cache vai trò → M2-04 (access token không chứa vai trò tin cậy).
- Client tuần tự hoá refresh → M4-01.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — mục "Bài toán #6".
- [`ADR-0014`](../../../docs/adr/0014-refresh-token-rotation-strict-reuse.md).
- [`data-model.md`](../../../docs/design/data-model.md) — mục `identity.refresh_tokens`.
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Identity" (refresh, logout).
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-22, C-23, C-24.

## Test
Gợi ý — người học viết.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Refresh_TwoConcurrentSameToken_OneWinsThenFamilyRevoked` | `Barrier(2)`: đúng 1 `200`, 1 `401`; sau đó refresh bằng token mới của bên thắng cũng `401` | đọc-rồi-revoke (`UnsafeVariant=ReadThenRevoke`) ⇒ 2 response `200` |
| `Refresh_ReuseOldToken_RevokesFamily` | dùng token cũ sau khi đã rotate ⇒ `401 REFRESH_TOKEN_REUSED`, token hiện hành của family cũng bị revoke | không revoke family |
| `Refresh_ExpiredToken_401Invalid` | `401 REFRESH_TOKEN_INVALID`, không revoke family nào | trả `REUSED` cho token hết hạn |
| `Refresh_UnknownToken_401Invalid` | không lộ khác biệt với hết hạn | — |
| `Logout_RevokesWholeFamily` | sau logout mọi token của family `401` | chỉ revoke token gửi lên |
| `RefreshTokens_StoredHashedOnly` | DB không chứa token thô (so với giá trị trả về) | lưu thô |

## Câu hỏi review
- Vì sao hai request đồng thời cùng token đều bị coi là "dùng lại" và đăng xuất người dùng, và đó là
  chủ ý chứ không phải lỗi?
- Vì sao token lưu SHA-256 chứ không phải PBKDF2 như mật khẩu?
- `CHECK (replaced_by IS NULL OR revoked_at IS NOT NULL)` bảo vệ khỏi lỗi code nào?

## Done khi
- [ ] Test race đã thấy đỏ với biến thể `ReadThenRevoke`
- [ ] Không log token / hash (C-22); không `Random`, không `==` so giá trị bí mật
- [ ] Migration chạy từ DB trống, không sửa migration cũ
