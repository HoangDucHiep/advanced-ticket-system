# Lớp 1 — PRD

Thư mục này chứa **tài liệu yêu cầu**. Nó nói hệ thống dành cho ai, phải làm gì, và cố tình
không làm gì.

**Đây là lớp 1 — chưa được nói gì về công nghệ.** Không có từ khoá database, API, framework,
cache, hay queue trong các file ở đây. Những thứ đó là Lớp 3.

## File

| File | Nội dung | Ticket |
|---|---|---|
| `00-overview.md` | Vấn đề, actors, jobs, success criteria | T01 |
| `01-requirements.md` | Requirements đánh số `FR-nn` | T02 |
| `02-non-requirements.md` | Những gì cố tình không làm, kèm lý do (`NR-01..NR-15`) | T03 |
| `03-user-journeys.md` | Đặt vé, mở bán phim hot, check-in — từng bước (`J-01..J-07`) | T04 |
| `04-business-rules.md` | Luật nghiệp vụ `BR-01..BR-30` | T04 |

## Quy tắc

- Requirement phải **kiểm chứng được**. "Kiểm chứng" nghĩa là có một test hoặc một số đo chứng
  minh nó đạt.
- Requirement viết bằng hành vi quan sát được từ bên ngoài, không chứa giải pháp.
- Non-requirements là chốt chặn scope creep. Muốn thêm tính năng thì đối chiếu file này
  trước.
- Chưa được người học nói thì ghi `(chưa xác định)` — đừng điền vào bằng suy đoán.

## Trạng thái

**Gate A đóng** (2026-11-20). Lớp 2 — Domain được mở. Xem exit checklist tại
[`../README.md`](../README.md).
