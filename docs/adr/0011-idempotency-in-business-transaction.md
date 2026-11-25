---
id: 0011
title: Idempotency key được chiếm trong chính transaction nghiệp vụ; kết quả lỗi giữ bằng savepoint
status: accepted
date: 2026-11-25
Related: ADR-0008
---

# Idempotency key được chiếm trong chính transaction nghiệp vụ; kết quả lỗi giữ bằng savepoint

## Context

`FR-37` đòi cùng `Idempotency-Key` ⇒ cùng kết quả, **kể cả khi lần đầu bị từ chối**, và 50 request
đồng thời cùng key ⇒ một đơn. Thiết kế phổ biến (và kế hoạch gốc của T19) là bản ghi có trạng thái
`InProgress` commit ở một transaction riêng trước khi xử lý.

## Decision

- Lệnh **đầu tiên** của transaction nghiệp vụ: `INSERT INTO booking.idempotency_records … ON CONFLICT
  DO NOTHING`. Request cùng key đến sau **chờ trên unique index** cho tới khi request đầu kết thúc —
  "đang xử lý" chính là "dòng chưa commit"; không có cột trạng thái. Chờ tối đa `lock_timeout = 3s`,
  quá ⇒ `409 IDEMPOTENCY_IN_PROGRESS` + `Retry-After`.
- Ngay sau đó `SAVEPOINT`. Lỗi **nghiệp vụ** ⇒ `ROLLBACK TO SAVEPOINT`, ghi response lỗi vào bản ghi,
  **commit**. Thành công ⇒ ghi response cùng transaction với đơn.
- Lỗi **hạ tầng** / tiến trình chết ⇒ rollback toàn bộ, không còn bản ghi ⇒ gửi lại được xử lý như mới.
- Request hash trên method + đường dẫn + body chuẩn hoá (seat id sắp tăng). Khác hash ⇒ `422`.
- Hạn 24 giờ, job dọn mỗi giờ.

## Considered Options

| Phương án | Vì sao không chọn |
|---|---|
| Bản ghi `InProgress` ở transaction riêng | Tiến trình chết sau khi commit `InProgress` ⇒ key kẹt tới khi hết lease; cần cột lease + logic chiếm lại; kết quả và đơn ghi ở hai transaction có thể lệch |
| `UNIQUE` trên `bookings.request_id` | Chỉ nhớ lần thành công — lần đầu `409`, lần sau có thể `201` |
| Redis `SETNX` | Key và đơn ở hai hệ thống, không atomic |

## Consequences

- Request trùng giữ một connection trong lúc chờ (≤ 3s) — chấp nhận được vì trùng là hiếm; admission
  control (#11) chặn trường hợp xấu.
- Không có trạng thái `InProgress` để quan sát; "đang xử lý" chỉ thấy qua `pg_locks`.
- Kế hoạch T19 (`409` cho mọi request trùng đang chạy) đổi thành "chờ rồi trả cùng kết quả" — đúng
  `FR-37` hơn.
