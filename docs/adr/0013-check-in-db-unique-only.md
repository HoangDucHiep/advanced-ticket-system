---
id: 0013
title: Check-in đúng một lần chỉ bằng khoá chính ở DB — bỏ Redis SETNX của kế hoạch ban đầu
status: accepted
date: 2026-11-25
---

# Check-in đúng một lần chỉ bằng khoá chính ở DB — bỏ Redis `SETNX` của kế hoạch ban đầu

Kế hoạch T24 ban đầu dùng Redis `SETNX checkin:{ticketId}` để "chặn nhanh" trước khi chèn
`ticketing.check_ins`. Quyết định: **chỉ** dùng `PRIMARY KEY (ticket_id)` + `INSERT … ON CONFLICT DO
NOTHING`, lượt thua đọc dòng có sẵn để trả thời điểm và cổng gốc. Lý do: ràng buộc DB đã là bảo đảm
duy nhất có giá trị; Redis chỉ thêm một chỗ có thể **lệch** (key Redis đã đặt mà transaction DB
rollback ⇒ vé hợp lệ bị báo "đã dùng" — lỗi tệ nhất ở cổng), và tải ở cổng chỉ vài lượt/giây nên
"chặn nhanh" không có gì để tối ưu. Người đọc code sẽ thắc mắc vì sao không có lớp Redis như nhiều bài
viết — ADR này là câu trả lời. Đổi ý được rẻ nếu số đo ở T23 cho thấy cần.
