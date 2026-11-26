# M2-02: Idempotency trong transaction nghiệp vụ

Mode: you-implement-i-review
Status: ready
Blocked by: M2-01
Traces to: FR-37
Concurrency: #5
Branch: m2-02-idempotency-in-transaction

## Mục tiêu
Gửi lại "giữ ghế" cùng `Idempotency-Key` (bấm đúp, retry) chỉ tạo một đơn và luôn nhận cùng một kết quả,
kể cả khi kết quả đó là lời từ chối. Đây là bảo đảm duy nhất mà client dựa vào khi mất response.

## Phạm vi
**Trong:**
- Chiếm key bằng `INSERT … ON CONFLICT` làm lệnh **đầu tiên** của transaction nghiệp vụ; request thứ hai
  chờ trên unique index (không có trạng thái `InProgress` được lưu). Chờ tối đa 3s rồi
  `409 IDEMPOTENCY_IN_PROGRESS` + `Retry-After: 1`.
- Savepoint ngay sau khi chiếm key: thất bại nghiệp vụ (`409`/`422`) ⇒ rollback về savepoint, ghi
  response lỗi vào bản ghi, **commit**; thành công ⇒ ghi `201` cùng transaction với đơn. Lỗi hạ tầng ⇒
  rollback hết, không lưu gì.
- Request hash chuẩn hoá (body sắp key, UUID thường, `seatIds` sắp tăng) ⇒ cùng key khác nội dung:
  `422 IDEMPOTENCY_KEY_REUSED`; lặp lại hợp lệ trả nguyên văn status + body + header
  `Idempotent-Replayed: true`.
- Cơ chế đặt ở `Shared.Infrastructure` (dùng chung), bảng `booking.idempotency_records` thuộc `Booking`.
- Job dọn mỗi giờ, xoá lô 1000 dòng quá 24 giờ.

**Ngoài:**
- Thiếu header ⇒ `400 IDEMPOTENCY_KEY_REQUIRED` đã có từ M1-04; ở đây chỉ giữ nguyên.
- Idempotency cho webhook / check-in / các endpoint khác: không dùng cơ chế này (xem bảng "không cần
  `Idempotency-Key`" trong api-contract). Webhook → M1-05 / M2-07.
- Rate limit và admission control → M3-02, M3-04.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — mục "Bài toán #5" và "Bài toán #2" (bước (0), (5a), (5b)).
- [`ADR-0011`](../../../docs/adr/0011-idempotency-in-business-transaction.md).
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Idempotency contract".
- [`data-model.md`](../../../docs/design/data-model.md) — mục `booking.idempotency_records`.
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — B7 (handler tự commit, vì sao không filter).

## Test
Gợi ý — người học viết.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `HoldSeats_50ConcurrentSameKey_OneBookingSameResponse` | `Barrier(50)`: đúng 1 đơn trong DB; 50 response cùng status và cùng body byte-by-byte | bỏ `PRIMARY KEY (customer_id, idempotency_key)` (migration test xoá ràng buộc) |
| `HoldSeats_FirstRejected_RetryAfterSeatFreed_StillRejected` | lần 1 `409`; ghế được trả; gửi lại cùng key ⇒ vẫn `409` body cũ | không giữ lời từ chối (rollback hết thay vì savepoint) |
| `HoldSeats_SameKeyDifferentBody_422` | `422 IDEMPOTENCY_KEY_REUSED` | không so `request_hash` |
| `HoldSeats_SeatIdsDifferentOrder_SameIntent` | cùng tập ghế khác thứ tự = cùng hash ⇒ replay | hash theo thứ tự gốc |
| `HoldSeats_FirstRequestSlowerThan3s_SecondGets409InProgress` | `409 IDEMPOTENCY_IN_PROGRESS` + `Retry-After` | không đặt `lock_timeout` |
| `HoldSeats_InfrastructureFailureMidway_SameKeyProcessedAsNew` | rollback ⇒ không còn dòng idempotency | ghi key ở transaction riêng |
| `IdempotencyCleanup_DeletesOnlyOlderThan24h` | dòng 25h bị xoá, dòng 1h còn | cleanup xoá theo điều kiện sai |

## Câu hỏi review
- Vì sao "đang xử lý" không cần cột trạng thái trong thiết kế này, và điều gì mất đi so với phương án
  `InProgress` commit riêng?
- Vì sao phải `ROLLBACK TO SAVEPOINT` rồi vẫn `COMMIT`, thay vì rollback toàn bộ transaction khi `409`?
- Hai người dùng gửi cùng key có ảnh hưởng nhau không? Test nào chứng minh?

## Done khi
- [ ] Mọi test trên xanh; test đầu đã thấy đỏ khi bỏ PK
- [ ] Replay trả `Idempotent-Replayed: true`; lần đầu không có header này
- [ ] Job dọn chạy bằng hosted service, không xoá được dòng chưa commit (giải thích trong PR)
