# Lớp 3 — Thiết kế kỹ thuật

Thư mục này chứa **thiết kế kỹ thuật**: kiến trúc, mô hình dữ liệu, cơ chế concurrency, hợp đồng API,
khối nền dùng chung. Mọi thứ ở đây truy ngược được về một invariant ở [`../domain/`](../domain/) hoặc một
requirement ở [`../prd/`](../prd/). Thiết kế không có nguồn gốc là bịa đặt.

## Index — file nào là nguồn chuẩn cho cái gì

| File | Nguồn chuẩn cho |
|---|---|
| [`architecture.md`](architecture.md) | Stack, module và quy tắc phụ thuộc (D1–D10), transaction xuyên module, outbox, điều kiện tách service |
| [`data-model.md`](data-model.md) | Schema, cột, index, constraint; đối chiếu invariant → cơ chế ở DB |
| [`concurrency.md`](concurrency.md) | 14 bài toán concurrency: cơ chế, SQL, test chứng minh, "đỏ nếu…" |
| [`api-contract.md`](api-contract.md) | Endpoint, mã lỗi, ý nghĩa status, idempotency contract |
| [`building-blocks.md`](building-blocks.md) | Nền móng dùng chung (B1–B19): Result / problem+json, handler interface + decorator, transaction, repository, domain primitives (base class), domain event + bus in-process, persistence / web / auth / observability / test foundation, tooling |
| [`coding-conventions.md`](coding-conventions.md) | Checklist viết và review code (C-01…C-41, smell) |

Ánh xạ FR → endpoint / bài toán: [`../plan/traceability.md`](../plan/traceability.md).

## Quy tắc

- Mỗi `CHECK` / `UNIQUE` constraint truy ngược được tới một `INV-nn` hoặc `BR-nn`.
- Mỗi câu SQL tay ghi rõ **vì sao ORM không biểu đạt được**.
- Mỗi bài toán concurrency có **test chứng minh**, và có dòng ghi rõ test đó fail nếu cơ chế bị thay
  bằng cái sai.
- Mọi claim về hiệu năng cần số đo đi kèm.
- Mỗi sự kiện chỉ có một nơi định nghĩa: nơi khác dẫn link, không chép lại.
- Khi code mâu thuẫn với các file này, **design thắng**.
