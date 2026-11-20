# Triage Labels

Các skill nói bằng năm vai trò triage chuẩn. File này ánh xạ chúng sang chuỗi label thật
của repo.

| Label trong mattpocock/skills | Label trong tracker này | Ý nghĩa |
|---|---|---|
| `needs-triage` | `needs-triage` | Maintainer cần đánh giá issue này |
| `needs-info` | `needs-info` | Đang chờ người báo cáo bổ sung thông tin |
| `ready-for-agent` | `ready-for-agent` | Đã đặc tả đủ, sẵn sàng cho agent |
| `ready-for-human` | `ready-for-human` | Cần con người implement |
| `wontfix` | `wontfix` | Sẽ không xử lý |

Khi skill nhắc tới một vai trò (ví dụ "apply the AFK-ready triage label"), dùng đúng chuỗi
label trong bảng này.

## Lưu ý cho project học tập này

`ready-for-agent` ở đây **không** có nghĩa là "AI tự làm hết". Nghĩa là ticket đủ rõ để
đưa vào `/work-ticket`. Việc thực hiện vẫn theo trường `Mode:` của ticket — xem
`issue-tracker.md`.

Sửa cột "Label trong tracker này" nếu bạn dùng bộ từ khác.
