# Plan Gate C — Design

Gate C biến domain model thành **thiết kế kỹ thuật**: module nào biết gì về module nào, dữ liệu
nằm ở đâu, mỗi bài toán concurrency được giải bằng cơ chế gì và test nào chứng minh, API trả gì.
Mục tiêu: **đủ để viết code** ở Gate D, và không hơn thế.

Mọi thứ ở Lớp 3 phải truy ngược được về một `INV-nn` / `BR-nn` ở `docs/domain/` hoặc một
`FR-nn` ở `docs/prd/`. Thiết kế không có nguồn gốc là bịa đặt.

## Entry criteria

- [x] Gate B đóng 2026-11-25 — exit checklist trong `docs/README.md` tick đủ 8/8
- [x] `GLOSSARY.md` ở root — tên chuẩn dùng trong code
- [x] ADR-0003, ADR-0005, ADR-0006, ADR-0007 `accepted`; ADR-0002 còn `provisional` (chốt ở T12)
- [x] 5 "ranh giới bị cắt" đã liệt kê ở `docs/domain/cross-context-contracts.md`
- [x] 5 ticket Gate C (T10–T14) đã có trong `.scratch/advanced-ticket-system/issues/`

## Bước chi tiết

Thứ tự cứng: **T10 trước** (mọi ticket khác cần biết module và quyền sở hữu bảng). T11 và T12
có thể đan xen vì cơ chế concurrency ảnh hưởng schema và ngược lại. T13 cần T12. T14 cuối cùng.

### Bước 1 — T10: Architecture và module boundary (`co-design`)

Artifact: `docs/design/architecture.md`. Trả lời Q20: module tham gia chung một DB transaction
thế nào khi không đọc bảng của nhau.

### Bước 2 — T11: Data model (`co-design`)

Artifact: `docs/design/data-model.md`. Mỗi `CHECK` / `UNIQUE` / exclusion constraint truy ngược
được tới một invariant. (Q28 đã đóng — ADR-0010.)

### Bước 3 — T12: Concurrency design (`explain` + `co-design`)

Artifact: `docs/design/concurrency.md`. 13 bài toán ở roadmap → cơ chế → test, kèm dòng "test
này đỏ nếu bỏ cơ chế". Chốt ADR-0002 (`provisional`).

### Bước 4 — T13: API contract và idempotency contract (`co-design`)

Artifact: `docs/design/api-contract.md`. Endpoint, error code, idempotency contract.

### Bước 5 — T14: Bộ ADR (`co-design`)

Rà mọi quyết định khó đảo ngược phát sinh ở T10–T13; mỗi cái một ADR.

## Câu hỏi cần trả lời trong gate này

Từ `docs/learning/open-questions.md` và roadmap "Câu hỏi còn mở":

| # | Câu hỏi | Ticket |
|---|---|---|
| ~~Q20~~ | Đã đóng ở T10 (ADR-0008) | T10 |
| Q01 | Chống bán trùng: conditional `UPDATE` nhiều dòng, từng ghế theo thứ tự, hay `SELECT FOR UPDATE`? | T12 |
| Q02 | `SERIALIZABLE` có cần ở đâu không? | T12 |
| Q06 | Có cần distributed lock ở đâu không? | T12 |
| Q25 | Ghế `Held` đã quá hạn mà worker chưa trả — lệnh giữ mới có lấy luôn không? | T12 |
| Q04 | Hold TTL cố định hay cấu hình theo suất? | T13 |
| Q08 | Admission control ở tầng nào? | T12 |
| Q11 | Có dùng `teach` cho lý thuyết concurrency không? | T12 |

## Artifact sẽ tạo ra

| File | Nội dung | Ticket |
|---|---|---|
| `docs/design/architecture.md` | Module, quy tắc phụ thuộc, sở hữu bảng, đường đi request, outbox, điều kiện tách service | T10 |
| `docs/design/data-model.md` | Schema, index, constraint | T11 |
| `docs/design/concurrency.md` | Bài toán → cơ chế → test chứng minh | T12 |
| `docs/design/api-contract.md` | Endpoint, error code, idempotency contract | T13 |
| `docs/adr/00NN-*.md` | ADR cho quyết định khó đảo ngược | T10–T14 |

## Exit checklist — đóng Gate C

Xem `docs/README.md` mục Gate C (nguồn sự thật duy nhất của checklist).

> **Gate C đóng ngày 2026-11-25.** T10–T14 xong; 4 file design; ADR-0001..0015; ma trận truy vết có
> quyết định design cho mọi `FR`. Mọi câu hỏi trong bảng trên đã đóng trừ Q07 (đo ở T23). Bước tiếp:
> Gate D — `docs/plan/gate-d-implementation.md`.

## Điều chưa biết lúc mở Gate C

- Roadmap đã chốt "1 `DbContext` + `HasDefaultSchema`" từ trước khi có domain. Với ranh giới
  module + "không đọc bảng của nhau", quyết định này cần được xem lại ở T10.
- Bảng 13 bài toán concurrency vẫn là **giả định** cho tới khi T12 chốt cơ chế và Gate D có test.
- ~~Các ADR đề xuất còn treo~~ — đã tạo ADR-0008, ADR-0009 (T10), ADR-0010 (Q28).
- **Đã đóng sớm khi rà docs 2026-11-25:** Q13 (giá theo suất), Q22 (đệm 15 phút), Q28 (`Unpaid`/`Closed`).
- **Đổi hợp đồng 2026-11-25:** AI tự quyết mọi quyết định spec của T11–T14 (xem `AGENTS.md`).
