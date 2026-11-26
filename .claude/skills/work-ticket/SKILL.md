---
name: work-ticket
description: Làm việc một ticket trong project học tập, theo đúng vai trò mà ticket khai báo, có kiểm tra gate trước khi bắt đầu. User-invoked.
disable-model-invocation: true
argument-hint: "Số hoặc slug của ticket cần làm, ví dụ: 12 hoặc order-hold-atomic-update"
---

Làm việc **một** ticket của project Advanced Ticket System.

Project này là project **học tập**: người học viết production code, AI là trợ lý. Skill này
thực thi đúng vai trò mà ticket khai báo qua trường `Mode:` — không vượt quá vai trò đó.

Xem `AGENTS.md` để biết hợp đồng đầy đủ. Tóm tắt các bước:

## 1. Xác định ticket

Nếu người dùng không truyền đối số, hỏi xem muốn làm ticket nào — hoặc đề xuất ticket đầu tiên
trong frontier (ticket mở được ngay).

Đọc file ticket tại `.scratch/advanced-ticket-system/issues/<NN>-<slug>.md`.

## 2. Kiểm tra gate — dừng nếu chưa mở

Đọc `docs/README.md` để biết exit checklist của từng gate.

Ba điều kiện, **thiếu một là dừng**:

- **Gate của ticket đã mở.** Exit checklist của gate trước đó đã tick đủ chưa.
- **Blocker đã xong.** Mọi ticket trong trường `Blocked by:` đã hoàn thành chưa.
- **Tài liệu lớp trước tồn tại.** Ví dụ ticket `Layer: design` cần `docs/domain/` đã có nội dung.

Nếu chưa đạt: **dừng và nói rõ** cái gì đang chặn, cần làm gì để mở, và đề xuất ticket
tiếp theo hợp lý. Đừng tự nhảy gate và đừng làm ticket khác.

## 3. Thực thi theo `Mode:`

Đọc trường `Mode:` trong ticket và làm đúng vai trò đó.

### `explain`

Giảng khái niệm mà ticket đang xử lý. Sau đó hỏi lại bằng câu hỏi cụ thể để kiểm tra người học
thực sự hiểu — không phải hỏi lại để người học lặp lại. Nếu câu trả lời sai hoặc thiếu, giảng
lại phần đó bằng cách khác.

Không viết file tài liệu nào ở mode này trừ khi ticket nói rõ.

### `co-design`

Hỏi **một quyết định một lần**. Dùng `AskQuestion` để đưa lựa chọn kèm khuyến nghị.

Người học là người quyết. AI:

- Đưa lựa chọn và khuyến nghị của mình, nêu rõ đánh đổi của từng lựa chọn.
- Chỉ ghi lại **lời người học nói**, không tự bịa nội dung.
- Nếu người học chưa biết đáp án, dạy trước rồi hỏi lại — đừng chỉ đưa lựa chọn.
- Khi người học dùng một từ mơ hồ hoặc một từ đã có nghĩa khác trong dự án, gọi ra ngay.

Ghi kết quả vào file đích mà ticket nêu (`docs/prd/…`, `docs/domain/…`).

### `you-implement-i-review`

Người học viết code và test. AI chờ.

Khi người học nói xong, review:

- Đối chiếu **tiêu chí hoàn thành** trong ticket — cái nào thiếu, cái nào có nhưng sai.
- So với các file `docs/design/` liên quan (nếu ticket thuộc `Layer: code`).
- So với `docs/agents/domain.md` về cách dùng glossary và tôn trọng ADR.
- Áp dụng smell baseline của skill `code-review`: Mysterious Name, Duplicated Code, Feature Envy,
  Data Clumps, Primitive Obsession, Repeated Switches, Shotgun Surgery, Divergent Change,
  Speculative Generality, Message Chains, Middle Man, Refused Bequest. Mỗi cái là **heuristic**,
  không phải lỗi — nêu như nhận xét, không như phán quyết.

Không viết lại code thay người học. Nếu cần sửa, đưa đề xuất cụ thể để họ tự sửa.

### `pair-tdd`

Gọi skill `tdd`.

Vai trò của AI ở đây: **viết test đỏ**. Người học viết code cho xanh.

- Một test đỏ tại một thời điểm, không viết trước cả loạt.
- Test phải kiểm tra **hành vi quan sát được qua interface**, không kiểm tra chi tiết lõi.
- Giá trị kỳ vọng phải đến từ nguồn độc lập — không tính lại bằng đúng công thức mà code dùng.
- Dừng lại sau khi test đỏ thành công và chờ người học viết code.

Test concurrency phải **thật sự chồng lấn**: dùng `Barrier` hoặc điểm đồng bộ để các request
được phát ra cùng lúc, đừng trông chờ may mắn.

### `ai-implements`

AI làm phần được giao (thường là boilerplate, hạ tầng, cấu hình). Người học review.

Làm đúng phạm vi ticket, không làm thêm. Sau đó dừng lại để người học review.

## 4. Ghi lại

Sau khi xong phần việc của mình:

- Tick những tiêu chí hoàn thành nào đạt trong file ticket.
- Thêm mục mới vào `docs/learning/log.md` theo ngày: đã học gì, đã làm gì, chỗ nào vấp,
  câu hỏi nào còn mở. Mục này là để session sau đọc là hiểu ngay bối cảnh.
- Nếu ticket làm lộ ra một quyết định khó đảo ngược chưa có ADR, **đề xuất** tạo ADR
  (dùng format ở `docs/agents/domain.md`) — đừng tự quyết và đừng tự tạo.
- Cập nhật ma trận truy vết trong `docs/plan/00-roadmap.md` nếu ticket có trường `Traces to:`.

## 5. Báo cáo

Nói ngắn gọn: ticket nào, làm được gì, còn thiếu gì, bước tiếp theo đề xuất.

## Nguyên tắc

- **Không tự bịa nội dung PRD và domain.** Chưa được người học nói thì ghi `(chưa xác định)`.
- **Không viết production code** ở bốn mode đầu trừ khi ticket ghi rõ cho phép.
- **Không nhảy gate.** Bị chặn thì nói lý do.
- **Không làm thêm ticket nào ngoài ticket đang mở.**
