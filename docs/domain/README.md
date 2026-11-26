# Lớp 2 — Domain Model

Thư mục này chứa **mô hình miền**: các khái niệm nghiệp vụ, bounded context của chúng, và những điều
phải luôn đúng. Lớp này **không chứa SQL, tên bảng, tên service** — những thứ đó là Lớp 3
([`../design/`](../design/)).

Vì sao lớp này tồn tại: ranh giới aggregate quyết định phạm vi của một lần ghi atomic, nên chốt được
ở đây thì Lớp 3 gần như chỉ còn là hệ quả kỹ thuật. Tương tự, state machine biến "idempotent" từ một
từ khóa mơ hồ thành hành vi cụ thể. Giảng khái niệm DDD:
[`../learning/concepts.md`](../learning/concepts.md#ddd-trong-project-này).

## File và nguồn chuẩn

| File | Canonical cho |
|---|---|
| [`../../GLOSSARY.md`](../../GLOSSARY.md) | Thuật ngữ, từ nên tránh, tên Việt ↔ Anh, cặp dễ nhầm |
| [`context-map.md`](context-map.md) | Sáu bounded context, quan hệ R1–R9, nguồn sự thật của từng loại dữ liệu |
| [`aggregates/`](aggregates/README.md) | Aggregate, invariant (`INV-nn`), cơ chế bảo vệ, năm "ranh giới bị cắt" |
| [`state-machines.md`](state-machines.md) | Vòng đời, transition, transition bị từ chối, idempotency của transition |
| [`cross-context-contracts.md`](cross-context-contracts.md) | Hợp đồng giữa các context, cặp thuật ngữ khác nghĩa, Anti-Corruption Layer |

Quy tắc nghiệp vụ gốc (`BR-nn`) nằm ở [`../prd/04-business-rules.md`](../prd/04-business-rules.md);
cơ chế chống đua nằm ở [`../design/concurrency.md`](../design/concurrency.md). Domain chỉ trích dẫn.
