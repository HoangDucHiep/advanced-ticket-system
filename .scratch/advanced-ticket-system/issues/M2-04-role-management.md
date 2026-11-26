# M2-04: Quản lý vai trò, thu quyền có hiệu lực ngay

Mode: you-implement-i-review
Status: ready
Blocked by: M2-03
Traces to: FR-29, FR-33
Concurrency: —
Branch: m2-04-role-management

## Mục tiêu
Quản trị viên xem người dùng và đổi vai trò; khi thu quyền, người dùng mất khả năng tương ứng **ngay**
dù access token (15 phút) còn hạn, và mọi lần đổi đều có dấu vết (`BR-26`).

## Phạm vi
**Trong:**
- `GET /api/admin/users` (phân trang, lọc `?role=`) và `PUT /api/admin/users/{userId}/role`, chỉ
  `Administrator`.
- Mỗi lần đổi: cập nhật `users.role`, tăng `role_version`, ghi một dòng `identity.role_changes`
  (append-only) — cùng một transaction.
- Cache vai trò trên Redis theo [`data-model.md`](../../../docs/design/data-model.md) mục "Cache vai trò":
  key `role:{userId}`, ghi đè sau commit chỉ khi `role_version` mới hơn, Redis lỗi ⇒ đọc DB. **Không** tin
  `role` trong JWT; đổi vai trò có hiệu lực ngay trên mọi instance.
- `422 CANNOT_CHANGE_OWN_ROLE`; `404 USER_NOT_FOUND`.
- Kiểm chính sách phân quyền theo nhóm đường dẫn (`/api/admin`, `/api/manager`, `/api/check-in`) bằng
  test đi qua HTTP thật: sau khi hạ `TheaterManager → Customer`, cùng access token cũ gọi
  `/api/manager/...` phải `403`.
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

**Ngoài:**
- Rotation / logout của refresh token → M2-03.
- Phân công nhân viên cổng theo suất → M2-09 (đây chỉ là vai trò `GateStaff`, `FR-33`).
- Ghi nhớ: rò rỉ thông tin (`404` thay `403` cho tài nguyên người khác) là việc từng endpoint, không phải ở đây.

## Đọc trước
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Quy ước chung" (dòng Vai trò), "Identity" (admin users).
- [`data-model.md`](../../../docs/design/data-model.md) — mục `identity.users`, `identity.role_changes`.
- [`business rules`](../../../docs/prd/04-business-rules.md) — BR-26.
- [`architecture.md`](../../../docs/design/architecture.md) — sơ đồ "Đường đi của một request" (vai trò hỏi `IdentityAccess.Contracts`).

## Test
Gợi ý — người học viết.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `RevokeRole_TokenStillValid_NextRequestForbidden` | hạ quyền xong, access token cũ gọi endpoint quản lý ⇒ `403` ngay lập tức | tin `role` trong JWT, hoặc cache không vô hiệu theo `role_version` |
| `ChangeRole_WritesRoleChangeAndBumpsVersion` | một dòng `role_changes` (from, to, changed_by), `role_version` + 1, `200` trả `roleVersion` mới | thiếu một trong ba thay đổi (kiểm cùng transaction) |
| `ChangeOwnRole_422` | `CANNOT_CHANGE_OWN_ROLE`, không ghi gì | — |
| `ChangeRole_NonAdmin_403` / `ListUsers_NonAdmin_403` | vai trò khác `Administrator` bị chặn | policy gắn sai nhóm |
| `ListUsers_FilterByRole_Paged` | `?role=` lọc đúng; `totalCount` đúng | — |
| `ChangeRole_UnknownUser_404` | `USER_NOT_FOUND` | — |

## Câu hỏi review
- Vì sao không nhét vai trò vào JWT dù đỡ một lần đọc DB, và cache theo `role_version` giải quyết gì?
- Hai quản trị viên đổi vai trò cùng một người đồng thời thì `role_changes` và `role_version` nhất quán ra sao?
- Vì sao tự đổi vai trò của mình bị cấm?

## Done khi
- [ ] Test "thu quyền có hiệu lực ngay" xanh và đã thấy đỏ khi tắt vô hiệu cache
- [ ] `role_changes` append-only (quyền `UPDATE`/`DELETE` bị thu, có test hoặc ghi chú migration)
- [ ] Không endpoint nào đọc `role` từ claim
