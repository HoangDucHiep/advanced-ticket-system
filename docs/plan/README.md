# Plan

| File | Nội dung | Khi nào tạo |
|---|---|---|
| `00-roadmap.md` | Plan tổng: mục tiêu, nguyên tắc, 4 gate, ma trận truy vết, danh sách 26 ticket, log quyết định | Đã có, cập nhật dần |
| `gate-a-prd.md` | Plan chi tiết Gate A | Khi bắt đầu Gate A |
| `gate-b-domain.md` | Plan chi tiết Gate B | Khi bắt đầu Gate B |
| `gate-c-design.md` | Plan chi tiết Gate C | Khi bắt đầu Gate C |
| `gate-d-implementation.md` | Plan chi tiết Gate D | Khi bắt đầu Gate D |

## Vì sao plan chi tiết tạo dần

Thiết kế chi tiết của Gate B vô nghĩa nếu Gate A chưa chốt. Viết trước sẽ biến lại thành
một đặc tả phẳng — đúng cái mà project này sinh ra để tránh.

Nên quy tắc là: **tạo `gate-<X>-<tên>.md` đúng lúc bắt đầu giai đoạn X**, không sớm hơn.

Mỗi file plan giai đoạn gồm:

- **Entry criteria** — gate trước đã pass chưa, và bằng chứng.
- **Các bước chi tiết** của giai đoạn.
- **Câu hỏi cần trả lời** — những gì phải biết trước khi đi tiếp.
- **Artifact sẽ tạo ra** — file nào, nội dung gì.
- **Exit checklist** — điều kiện đóng gate.
- **Điều chưa biết lúc mở giai đoạn** — ghi lại để biết sau này cần quay lại.

## Plan tổng là bản sống

`00-roadmap.md` tồn tại từ đầu và được cập nhật liên tục. Đặc biệt là **ma trận truy vết**:
mỗi `FR-nn` phải có ít nhất một ticket đáp ứng trước khi mở Gate D. Requirement nào không
map được thì hoặc bỏ khỏi requirements, hoặc thêm ticket.
