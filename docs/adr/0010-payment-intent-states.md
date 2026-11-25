---
id: 0010
title: Payment intent dùng Unpaid và Closed — không trùng tên với Booking, không treo intent của đơn đã kết thúc
status: accepted
date: 2026-11-25
Related: ADR-0006
---

# `Payment intent` dùng `Unpaid` và `Closed`

Máy `Payment intent` (ADR-0006) có hai vấn đề: trạng thái chờ tên `AwaitingPayment` trùng với
trạng thái của `Booking` (Q28 — log ghi "booking `AwaitingPayment`" và "intent `AwaitingPayment`"
là hai sự kiện khác nhau), và khi đơn `Expired` / `Cancelled` mà chưa thu tiền thì intent **không có
trạng thái nào để đi** — nó nằm mãi ở trạng thái chờ dù đã yêu cầu huỷ phía Stripe. Quyết định:
trạng thái chờ đổi tên thành **`Unpaid`**; thêm **`Closed`** (đơn kết thúc khi chưa thu, cùng
transaction với chuyển trạng thái đơn, kèm outbox huỷ phía Stripe). `Closed` vẫn có đường ra
`→ RefundPending` vì lời huỷ phía Stripe là best-effort — tiền có thể vẫn về. Máy đầy đủ:
`Unpaid → Captured | RefundPending | Closed`, `Closed → RefundPending`, `Captured → RefundPending`,
`RefundPending → Refunded`. Đóng Q28.

Không chọn đổi tên phía `Booking`: `AwaitingPayment` là từ người xem thấy và đã dùng khắp PRD/domain;
intent là khái niệm nội bộ của `Payment`, đổi ở đây rẻ hơn.
