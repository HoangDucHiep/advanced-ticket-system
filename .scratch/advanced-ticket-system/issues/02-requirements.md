# 02: Requirements

Mode: co-design
Gate: A
Layer: prd
Blocked by: 01
Traces to: — (ticket này **sinh ra** FR-01..FR-38, không truy vết tới FR nào)

## Vì sao có ticket này

Requirements là hợp đồng giữa bạn và chính bạn 6 tháng sau. Chúng là thứ quyết định
"feature này đã xong chưa" và "feature này có cần làm không".

Vì vậy requirement phải **đánh số** và **kiểm chứng được**. Một requirement không đánh số
không thể truy vết, và một requirement không kiểm chứng được sẽ trở thành một lời hứa rỗng.

## Bạn tạo ra

`docs/prd/01-requirements.md` gồm danh sách requirement, mỗi cái:

```
### FR-01 — <tên ngắn>

<Yêu cầu, viết như một hành vi quan sát được từ bên ngoài.>

- Kiểm chứng: <test hoặc số đo chứng minh đạt>
```

Quy tắc viết một requirement:

- Viết ở góc nhìn người dùng, mô tả **hành vi quan sát được**, không mô tả cách làm.
- Không nhúng giải pháp. "Hệ thống dùng atomic UPDATE" là **quyết định design**, không phải
  requirement. Requirement là "không bao giờ bán vượt số vé có sẵn".
- Một requirement = một hành vi. Nếu cần "và" để nối hai hành vi, tách thành hai cái.
- Mỗi requirement gắn với ít nhất một actor và một job từ `docs/prd/00-overview.md`.

Câu hỏi gợi ý:

1. Từ mỗi job ở T01, hệ thống phải làm gì để job đó thành hiện thực?
2. Hành vi nào **không** được phép xảy ra? (Đây thường là requirement quan trọng nhất —
   ví dụ "một ghế không được thuộc về hai người", "đặt 4 ghế phải giữ đủ 4 hoặc không giữ
   ghế nào", "một vé không được check-in hai lần".)
3. Hệ thống phải hành xử ra sao khi thất bại? (hết ghế, hết thời gian giữ ghế, thanh toán lỗi)
4. Ranh giới của "đủ" là gì — đủ cho một người dùng thật dùng được?

## AI làm gì

AI hỏi từng nhóm hành vi một. Với mỗi requirement:

- Hỏi bạn muốn hành vi gì, đưa lựa chọn kèm khuyến nghị nếu có.
- **Phản biện**: nếu một requirement không kiểm chứng được, nói thẳng và đề xuất cách viết lại.
- Phát hiện requirement trùng lặp hoặc mâu thuẫn với nhau và chỉ ra.
- Ghi vào `docs/prd/01-requirements.md`, đánh số `FR-01`, `FR-02`…

AI không tự thêm requirement bạn chưa nói. Nếu nghĩ ra một requirement quan trọng mà bạn chưa
nói, **đề xuất** chứ không tự thêm.

## Các quyết định đã chốt trong ticket này

| Quyết định | Nội dung | Ảnh hưởng |
|---|---|---|
| Đăng nhập | Bắt buộc đăng nhập trước khi đặt vé | Đơn đặt vé luôn có chủ sở hữu → FR-10, FR-33 |
| Giới hạn vé mỗi người | Do rạp quyết định khi lên lịch chiếu | **Invariant tách khỏi tồn kho ghế** → ảnh hưởng T07 |
| Cấu trúc đơn | 1 đơn = **1 suất chiếu, N ghế** | Một đơn không đi qua nhiều suất → không có transaction phân tán; vẫn giữ bài toán all-or-nothing trên N ghế |
| Ánh xạ ghế → vé | 1 ghế = 1 vé = 1 mã QR | Giữ được invariant "mỗi vé chỉ check-in một lần" ở mức từng ghế → FR-07, FR-25 |
| Chọn ghế | Chọn bất kỳ, không cần liền kề, có thể trộn nhiều loại ghế | Giá tính theo từng ghế → FR-16 |
| FR trùng lặp | "Không xem đơn của người khác" (hành vi bị cấm) và "người xem chỉ xem vé của mình" (phân quyền) là cùng một hành vi | Đã gộp thành FR-10 |
| Thứ tự thực hiện | FR-24 (hủy suất chiếu và hoàn tiền) đầy đủ nhưng thực hiện ở Gate D **sau T22** | Hoàn tiền là bài toán lặp lại phức tạp → cần nền tảng vững |

> **Ghi chú (2026-11-19):** ticket này ban đầu sinh ra 35 FR cho nghiệp vụ "bán vé sự kiện theo
> hạng". Sau khi nghiệp vụ đổi sang đặt vé xem phim (mô hình seat-based), toàn bộ file
> `docs/prd/01-requirements.md` đã được viết lại thành **38 FR** (`FR-01..FR-38`).

## Tiêu chí hoàn thành

- [x] Mọi requirement có số `FR-nn` và tên ngắn
- [x] Mọi requirement có dòng "Kiểm chứng" cho biết bằng gì chứng minh là đạt
- [x] Mọi requirement truy vết được tới một actor và một job ở T01
- [x] Không requirement nào chứa giải pháp kỹ thu
- [x] Có ít nhất vài requirement về **hành vi bị cấm** (không bán trùng ghế, giữ n ghế phải all-or-nothing, không check-in hai lần, …)
- [x] Ma trận truy vết trong `docs/plan/00-roadmap.md` đã được cập nhật với các `FR-nn` này
