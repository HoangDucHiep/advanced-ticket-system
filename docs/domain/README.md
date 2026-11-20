# Lớp 2 — Domain Model

Thư mục này chứa **mô hình miền**. Nó nói các khái niệm nghiệp vụ là gì, chúng thuộc về
bounded context nào, và những điều phải luôn đúng.

Đây là lớp 2 — **không được chứa SQL, không được chứa tên bảng, không được chứa tên service**.
Những thứ đó là Lớp 3.

## File

| File | Nội dung | Ticket |
|---|---|---|
| `ubiquitous-language.md` | Thuật ngữ đã chốt, từ nào nên tránh dùng | T05 |
| `context-map.md` | Bounded context và quan hệ giữa chúng | T06 |
| `aggregates/` | Aggregate, boundary, invariant, cơ chế bảo vệ (1 file index + 6 file theo context) | T07 |
| `state-machines.md` | Vòng đời booking, payment, ticket, seat | T08 |
| `cross-context-contracts.md` | Hợp đồng giữa các context | T09 |

## Vì sao lớp này tồn tại

Aggregate boundary **là** transaction boundary **là** phạm vi của một lệnh SQL atomic. Chốt
được boundary ở đây thì Lớp 3 gần như chỉ còn là hệ quả kỹ thu.

Tương tự, state machine chốt ở đây là thứ biến "idempotent" từ một từ khóa mơ hồ thành một
hành vi cụ thể.

## Trạng thái

**Gate B được mở** (2026-11-20) sau khi Gate A đóng. Đã xong: T05 (ubiquitous language),
T06 (context map), T07 (aggregates và invariants — 8 aggregate, 47 invariant). Xem exit checklist
tại [`../README.md`](../README.md).

Tiếp theo: T08 — state machines.
