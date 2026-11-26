# Học tập

| File | Nội dung |
|---|---|
| [`log.md`](log.md) | Bài học đã gom theo chủ đề + "Nhật ký ticket" (mục mới thêm ở cuối) |
| [`open-questions.md`](open-questions.md) | Câu hỏi chưa có đáp án, cần số đo hoặc tra cứu |
| [`concepts.md`](concepts.md) | Khái niệm giảng giải, tra cứu khi cần |

## Template một mục nhật ký ticket

Thêm vào cuối `log.md`, dưới "Nhật ký ticket":

```markdown
## <YYYY-MM-DD> — <mã ticket> <tên>
**Làm gì**: 1–2 câu.
**Vì sao làm thế này**: cơ chế chính và lý do, bằng lời của người học.
**Phương án khác và vì sao không**: ít nhất một.
**Test chứng minh, đỏ khi nào**: tên test + phép thử âm tính đã chạy.
**Vấp ở đâu**: nguyên nhân thật.
**Còn mở**: câu hỏi chưa chắc (đưa vào open-questions.md nếu cần tra cứu).
```

Nguyên tắc:

- **Học được** là khái niệm, không phải hoạt động. "Học cách dùng `Barrier`" là hoạt động; "test
  song song mà không đồng bộ thì chỉ chứng minh được điều may mắn" là khái niệm.
- **Vấp ở đâu** ghi nguyên nhân thật, không ghi "không làm được".
- **Còn mở** tồn tại quá 2 tuần là tín hiệu cần xử lý hoặc bỏ.
- Khi bài học đủ chín, gom vào chủ đề phù hợp ở phần đầu `log.md` và bỏ phần kể lể theo ngày.
