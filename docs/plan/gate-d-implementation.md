# Plan Gate D — Implementation

Gate D biến design thành **code chạy được và test chứng minh được**. Người học viết production code
(`AGENTS.md`); AI làm skeleton (T15), viết test đỏ ở ticket `pair-tdd`, và review.

**Quy tắc vàng:** khi ticket mâu thuẫn với `docs/design/`, **design thắng**. Mỗi ticket T15–T26 có khối
"Đồng bộ với design" ở đầu.

## Entry criteria

- [x] Gate C đóng 2026-11-25 — exit checklist trong `docs/README.md` tick đủ
- [x] `docs/design/` đủ bốn file; ADR-0001..0015
- [x] Ma trận truy vết: mọi `FR` trong scope có ticket + quyết định design
- [x] Ticket T15–T26 đã đồng bộ với design

## Đọc gì trước khi code

1. [`../design/architecture.md`](../design/architecture.md) — module, project, quy tắc phụ thuộc.
2. [`../design/data-model.md`](../design/data-model.md) — bảng của module đang làm.
3. [`../design/concurrency.md`](../design/concurrency.md) — bài toán của ticket (trường `Concurrency
   problem:`), **gồm test và dòng "đỏ nếu…"**.
4. [`../design/api-contract.md`](../design/api-contract.md) — endpoint và mã lỗi.
5. [`../../GLOSSARY.md`](../../GLOSSARY.md) — tên class, bảng, trạng thái.

## Thứ tự

| Bước | Ticket | Mode | Ghi chú |
|---|---|---|---|
| 1 | T15 Skeleton + Compose + CI | `ai-implements` | UoW (ADR-0008), architecture test, `btree_gist` |
| 2 | T16 Identity | `you-implement-i-review` | #6, #12 |
| 3 | T17 Phòng, suất, sơ đồ ghế, cache | `you-implement-i-review` | #9, #10 |
| 4 | T18 Giữ n ghế | `pair-tdd` | #1, #2, #3, #13, #14 — **lõi** |
| 5 | T19 Idempotency | `you-implement-i-review` | #5 |
| 6 | T20 Stripe | `pair-tdd` | #7 |
| 7 | T21 Worker hết hạn + outbox | `you-implement-i-review` | #4 |
| 8 | T22 Bộ test concurrency | `you-implement-i-review` | gom + phép thử âm tính |
| 9 | T24 Check-in | `pair-tdd` | #8 — cắt đầu tiên nếu trễ |
| 10 | T23 Load test + baseline | `pair-tdd` | #11; số đo cho ADR-0002, ADR-0007 |
| 11 | T25 Observability | `you-implement-i-review` | |
| 12 | T26 README + CV | `co-design` | |

Lõi bắt buộc: T15–T22 + T26. Cắt khi trễ: T24 → T25 → mở rộng T23.

## Câu hỏi còn mở trong gate này

| # | Câu hỏi | Ticket |
|---|---|---|
| Q07 | Token bucket Redis chịu được bao nhiêu request/giây? | T23 |
| — | Ngưỡng `PermitLimit` / `QueueLimit` của admission control (khởi điểm 30 / 100) | T23 |
| — | Ngưỡng tách service (architecture D10) | sau T23 |
| — | Biến thể khoá ghế nào nhanh nhất (ADR-0002, số đo) | T23 |

## Exit checklist

Xem `docs/README.md` mục Gate D.
