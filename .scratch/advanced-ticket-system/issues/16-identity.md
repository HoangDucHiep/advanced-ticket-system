# 16: Identity — register, login, JWT, refresh rotation

Mode: you-implement-i-review
Gate: D
Layer: code
Blocked by: 15
Traces to: FR-10, FR-29, FR-33
Concurrency problem: #6

## Vì sao có ticket này

Bạn cần có **user thật** thì các bài toán sau mới diễn ra được: giới hạn mỗi người mua bao
nhiêu vé trên một suất chiếu, rate limit theo user, ai được check-in, ai sở hữu đơn đặt vé.

Auth cũng chứa sẵn một bài concurrency hay: **refresh token rotation với reuse detection**.
Hai request refresh cùng lúc với cùng một token — chỉ một cái được chấp nhận, cái còn lại bị
coi là hành vi đánh cắp và phải revoke toàn bộ chain.

Đây là ticket đầu tiên bạn viết production code, nên nó là nơi đo xem cách làm việc chung
có hợp hay không.

## Bạn tạo ra

- Schema `identity.users` và `identity.refresh_tokens` (theo T11).
- `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/refresh`,
  `POST /api/auth/logout`, `GET /api/auth/me`.
- Access token: JWT, ngắn hạn, có `sub`, `role`, `jti`.
- Refresh token: giá trị ngẫu nhiên, lưu **bản hash** trong DB, kèm `family_id`,
  `expires_at`, `revoked_at`, `replaced_by`.
- Rotation: refresh token mới thay thế token cũ, token cũ bị revoke.
- **Reuse detection**: refresh token đã bị revoke mà vẫn được dùng → revoke toàn bộ
  `family_id`, trả `401`.
- Rate limit login và refresh theo IP.
- Test.

## AI làm gì

AI **không viết code**. AI:

1. Đọc `docs/design/api-contract.md` và `docs/design/data-model.md` để nêu lại yêu cầu cụ thể
   cần đạt, kèm các `FR-nn` liên quan.
2. Chờ bạn viết.
3. Review khi bạn nói xong:
   - Đối chiếu tiêu chí hoàn thành.
   - Với concurrency của refresh rotation: **câu hỏi sắc nhất** — "hai request refresh đồng
     thời thì điều gì bảo đảm chỉ một cái thắng? Nếu bạn đọc rồi mới ghi, cửa sổ race ở
     đâu?"
   - Với bảo mật: refresh token có được lưu dạng hash không? So sánh hợp thức có
     constant-time không? `family_id` có thật sự nối được chuỗi token không?
   - Smell baseline.
4. Gợi ý test cần có, đặc biệt test concurrency cho rotation.

## Tiêu chí hoàn thành

- [ ] Đăng ký, đăng nhập, xem `/me`, refresh, logout đều hoạt động
- [ ] Dùng lại refresh token đã bị revoke → `401` **và** toàn bộ `family_id` bị revoke
- [ ] Hai request refresh đồng thời với cùng token → chỉ một cái thành công
- [ ] DB **không** chứa refresh token dạng thô
- [ ] Login bị rate limit theo IP, vượt ngưỡng trả `429`
- [ ] Unit test cho phép so sánh token và quy tắc revoke family
- [ ] Password lưu dạng hash, không phải plaintext
- [ ] Endpoint nào cần phân quyền theo vai trò đã được chốt trong T13
