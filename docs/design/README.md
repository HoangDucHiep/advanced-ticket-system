# Lớp 3 — Thiết kế kỹ thuật

Thư mục này chứa **thiết kế kỹ thuật**: kiến trúc, mô hình dữ liệu, cơ chế concurrency,
hợp đồng API.

**Lớp này chỉ được mở sau khi Gate B đóng.** Mọi thứ ở đây phải truy ngược được về một
invariant ở [`../domain/`](../domain/) hoặc một requirement ở [`../prd/`](../prd/). Thiết kế
mà không có nguồn gốc là bịa đặt.

## File

| File | Nội dung | Ticket |
|---|---|---|
| `architecture.md` | Cấu trúc module, quy tắc phụ thuộc | T10 |
| `data-model.md` | Schema, index, constraint | T11 |
| `concurrency.md` | Mỗi bài toán: cơ chế + test chứng minh | T12 |
| `api-contract.md` | Endpoint, error code, idempotency contract | T13 |
| `building-blocks.md` | Khối nền dùng chung: Result, problem+json, handler, transaction, repository, domain tối thiểu (B1–B12) | T28 (thiết kế 2026-11-26) |
| `coding-conventions.md` | Checklist viết và review code (C-01…C-32, smell) | T28 |

## Quy tắc

- Mỗi `CHECK` / `UNIQUE` constraint truy ngược được tới một `INV-nn` hoặc `BR-nn`.
- Mỗi câu SQL tay ghi rõ **vì sao ORM không biểu đạt được**.
- Mỗi bài toán concurrency có **test chứng minh**, và có dòng ghi rõ test đó fail nếu cơ chế
  bị thay bằng cái sai.
- Mọi claim về hiệu năng cần số đo đi kèm.

## Trạng thái

Gate C **đã đóng** 2026-11-25 — bốn file đầy đủ. Đọc theo thứ tự: `architecture.md` → `data-model.md`
→ `concurrency.md` → `api-contract.md`. Bổ sung 2026-11-26 (Gate D, trước T16): `building-blocks.md` +
`coding-conventions.md` — khối nền mà bốn file trên ngầm giả định nhưng chưa thiết kế. Khi ticket Gate D mâu thuẫn với các file này, **design thắng**.
