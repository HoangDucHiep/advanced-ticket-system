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

## Quy tắc

- Mỗi `CHECK` / `UNIQUE` constraint truy ngược được tới một `INV-nn` hoặc `BR-nn`.
- Mỗi câu SQL tay ghi rõ **vì sao ORM không biểu đạt được**.
- Mỗi bài toán concurrency có **test chứng minh**, và có dòng ghi rõ test đó fail nếu cơ chế
  bị thay bằng cái sai.
- Mọi claim về hiệu năng cần số đo đi kèm.

## Trạng thái

Gate C **đang mở** (từ 2026-11-25). Plan chi tiết:
[`../plan/gate-c-design.md`](../plan/gate-c-design.md). Exit checklist tại
[`../README.md`](../README.md).
