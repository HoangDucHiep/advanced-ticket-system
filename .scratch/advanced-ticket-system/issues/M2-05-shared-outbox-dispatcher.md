# M2-05: Outbox dùng chung + dispatcher

Mode: you-implement-i-review
Status: ready
Blocked by: M1-05
Traces to: —
Concurrency: —
Branch: m2-05-shared-outbox-dispatcher

## Mục tiêu
Có một cơ chế duy nhất để làm việc "khi và chỉ khi transaction commit" mà nằm ngoài transaction (gọi Stripe,
gửi email, vô hiệu cache). M2-06, M2-07, M2-08 và M3-03 chỉ việc ghi một dòng outbox và viết handler.

## Phạm vi
**Trong:**
- Một bảng `outbox_messages` **mỗi module** cần nó (`inventory`, `booking`, `payment`), cùng hình dạng; dùng
  **cấu hình bảng dùng chung** đã khai báo ở M0-04 (không bảng chung ở `shared` — D5), mỗi module thêm migration.
- **Hiện thực** `IOutboxWriter` — interface đã có từ M0-03 — ghi qua connection / transaction của `IUnitOfWork`,
  cùng transaction với thay đổi nghiệp vụ; cộng abstraction handler theo `type`.
- Dispatcher là hosted service ở host: claim bằng lease (`locked_until`, tăng `attempts`) với
  `FOR UPDATE SKIP LOCKED`, **commit ngay rồi mới gọi handler**; thành công ⇒ `processed_at`; lỗi ⇒
  `last_error` + backoff qua `available_at`; quá 10 lần ⇒ `dead_at` + log mức `Critical`.
- Hỗ trợ hẹn giờ (`available_at` ở tương lai) — M2-06 cần cho email nhắc.
- Handler chỉ cần idempotent (at-least-once); ticket này chỉ có **handler thử** trong test, không có
  handler nghiệp vụ.
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

**Ngoài:**
- Handler nghiệp vụ: huỷ intent Stripe, email nhắc → M2-06; hoàn tiền → M2-07; cache sơ đồ ghế
  (`SeatStatesChanged`) → M3-03.
- Metric / alert số message dead → M3-01, M3-06.

## Đọc trước
- [`architecture.md`](../../../docs/design/architecture.md) — mục "D8 — Outbox".
- [`data-model.md`](../../../docs/design/data-model.md) — mục "Outbox — mỗi module một bảng" (cột, index, câu claim).
- [`concurrency.md`](../../../docs/design/concurrency.md) — "Bài toán #7" (phần gửi hoàn tiền: lease hết ⇒ worker khác làm lại).
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — mục "B7 — Ai mở và commit transaction", "B11 — Cấu hình: Options pattern + kiểm khi khởi động", "B13 — Domain event và bus in-process" (dòng `IOutboxWriter`), "B14 — Persistence foundation" (dòng Outbox).

## Test
Gợi ý — người học viết. Integration trên Postgres thật.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Dispatcher_TwoInstances_NeverProcessSameMessage` | 2 dispatcher chạy song song trên 500 message: mỗi message được handler gọi đúng 1 lần | bỏ `SKIP LOCKED` hoặc điều kiện lease |
| `Dispatcher_HandlerFailsTenTimes_MessageDeadNotLost` | handler luôn ném lỗi ⇒ sau 10 lần `dead_at` có giá trị, dòng vẫn còn, `last_error` ghi lỗi, không chạy lần 11 | xoá dòng khi lỗi, hoặc retry vô hạn |
| `Dispatcher_ProcessCrashAfterClaim_LeaseExpiresThenRetried` | lease hết hạn ⇒ message được claim lại và xử lý | `locked_until` không bao giờ hết |
| `Dispatcher_FutureAvailableAt_NotDispatchedEarly` | message hẹn giờ chưa tới hạn thì không được gọi | bỏ điều kiện `available_at` |
| `Outbox_RolledBackTransaction_LeavesNoMessage` | rollback nghiệp vụ ⇒ không có dòng outbox | ghi outbox ở connection riêng |
| `Dispatcher_HandlerRunsOutsideTransaction` | trong lúc handler chạy, không có transaction/connection giữ dòng đó | giữ transaction mở suốt lúc gọi ngoài |

## Câu hỏi review
- Vì sao claim bằng lease và commit trước khi gọi ngoài, thay vì giữ `FOR UPDATE` suốt lúc gọi Stripe?
- At-least-once nghĩa là handler phải thoả điều gì? Cho một ví dụ handler **không** an toàn.
- Vì sao mỗi module một bảng chứ không một bảng chung?

## Done khi
- [ ] Sáu test xanh; test đầu và test dead đã thấy đỏ khi phá cơ chế
- [ ] Số lần thử tối đa, độ dài lease, kích thước lô, backoff nằm trong Options có `ValidateOnStart`
- [ ] Arch test vẫn xanh (module chỉ ghi outbox của schema mình)
