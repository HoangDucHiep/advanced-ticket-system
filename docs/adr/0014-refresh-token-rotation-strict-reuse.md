---
id: 0014
title: Refresh token rotation, phát hiện dùng lại nghiêm ngặt — không có ân hạn
status: accepted
date: 2026-11-25
---

# Refresh token rotation, phát hiện dùng lại nghiêm ngặt — không có ân hạn

Mỗi lần làm mới, refresh token cũ bị revoke và thay bằng token mới cùng `family_id`; việc này dùng một
lệnh `UPDATE … WHERE token_hash = @h AND revoked_at IS NULL RETURNING` nên hai request đồng thời chỉ
một thắng. Dùng lại một token đã bị revoke ⇒ revoke **cả chuỗi** và trả `401`. Quyết định **không có
ân hạn**: request thứ hai đồng thời cùng token (client gửi đúp) cũng bị coi là dùng lại và đăng xuất
người dùng. Lý do: server không phân biệt được "client gửi đúp" với "kẻ trộm dùng token vừa bị thay";
ân hạn vài giây mở lại đúng cửa sổ kẻ trộm cần và thêm trạng thái. Không chọn refresh token dài hạn
không rotation (không phát hiện được đánh cắp). Hệ quả: client phải tuần tự hoá việc refresh (một
promise dùng chung); token lưu dạng SHA-256 (token 256-bit ngẫu nhiên không cần hàm băm chậm, và cần
tra theo giá trị băm). Access token 15 phút; vai trò không đọc từ token (`BR-26`).
