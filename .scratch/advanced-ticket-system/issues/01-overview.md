# 01: Overview — vấn đề, actors, jobs, success criteria

Mode: co-design
Gate: A
Layer: prd
Blocked by: —
Traces to: —

## Vì sao có ticket này

Mọi tài liệu sau này — domain model, kiến trúc, schema, cơ chế concurrency — đều phải trả lời
được "vì sao". File này là câu trả lời gốc. Nếu chưa có nó, các quyết định kỹ thu về sau sẽ
trôi trong không có điểm tựa.

Ticket này **không** nói gì về công nghệ, gì về database, gì về API. Chỉ nói: hệ thống này
làm gì, cho ai, và thành công trông như thế nào.

## Bạn tạo ra

`docs/prd/00-overview.md` gồm:

- **Vấn đề** — hệ thống giải quyết vấn đề gì, và vì sao vấn đề đó đáng giải.
- **Actors** — những vai trò dùng hệ thống. Mỗi actor là một người hoặc một hệ thống bên ngoài.
- **Jobs** — mỗi actor cần đạt được điều gì từ hệ thống. Job là thứ actor *muốn*, không phải
  thứ hệ thống *làm* — hai cái này khác nhau và hay bị lẫn.
- **Success criteria** — điều kiện để gọi là xong. Viết sao cho **đo được**, không viết kiểu
  "hệ thống nhanh và ổn định".

Câu hỏi gợi ý để bắt đầu (trả lời hoặc bỏ qua, không phải câu nào cũng cần):

1. Hệ thống này giải quyết tình huống nào trong đời thực mà bạn đã thấy hoặc nghe kể?
2. Ai là người dùng? Có bao nhiêu kiểu người dùng khác nhau không?
3. Vì sao chọn nghiệp vụ đặt vé xem phim? Có nghiệp vụ nào khác mà bạn cân nhắc rồi bỏ?
4. Nếu 6 tháng sau nhìn lại và thấy dự án này đã thành công, những gì sẽ khác so với hiện tại?
5. Bạn muốn học được gì từ dự án này? (Đây là success criterion quan trọng nhất.)

## AI làm gì

AI hỏi từng câu một, đưa lựa chọn kèm khuyến nghị khi có lựa chọn rõ ràng, rồi ghi lại lời
bạn vào `docs/prd/00-overview.md`.

AI không tự điền vào những chỗ bạn chưa nói. Chỗ chưa rõ thì ghi `(chưa xác định)`.

## Tiêu chí hoàn thành

- [x] Có phần Vấn đề, Actors, Jobs, Success criteria
- [x] Mỗi actor có ít nhất một job
- [x] Success criteria viết sao cho đo được — có số hoặc có điều kiện kiểm chứng được
- [x] Không có từ kỹ thu nào lọt vào (database, API, framework) — file này nói về nghiệp vụ
- [x] Tài liệu phía sau có thể trích dẫn file này bằng đường dẫn
