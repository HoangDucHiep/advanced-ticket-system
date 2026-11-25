---
id: 0009
title: Module gọi nhau qua interface, event trong transaction, hoặc outbox — theo hướng phụ thuộc không vòng
status: accepted
date: 2026-11-25
Related: ADR-0006, ADR-0008
---

# Module gọi nhau qua interface, event trong transaction, hoặc outbox — theo hướng phụ thuộc không vòng

## Context

Sáu module phải phối hợp trong năm ranh giới bị cắt (ADR-0008), và gọi Stripe / Redis / email sau
commit. Context map có hai quan hệ **hai chiều về dữ liệu** (R4 `Booking ↔ Payment`, R6
`Booking → Ticketing` với vé thuộc `Ticketing`) và một cặp ngược chiều (R1 `Scheduling → Inventory`,
R2 `Inventory → Scheduling`) — nếu mỗi chiều đều là tham chiếu code thì đồ thị module có vòng.

## Decision

**Cách gọi chọn theo use case** (`docs/design/architecture.md` D4):

| Bên gọi cần kết quả? | Phải atomic? | Dùng |
|---|---|---|
| Có | Có | Interface trong `<Upstream>.Contracts`, gọi đồng bộ trong transaction |
| Không | Có | Event in-process định nghĩa trong `.Contracts` của bên phát; handler chạy **trước commit**; exception ⇒ rollback |
| Không | Không / chạm hệ thống ngoài | Event qua outbox (sau commit, at-least-once, handler idempotent) |

**Hướng phụ thuộc compile-time** (D9) — `A → B` nghĩa là A reference `B.Contracts`:

- `Inventory → Scheduling`; `Booking → Scheduling, Inventory, Payment`; `Ticketing → Booking,
  Scheduling`; mọi module → `IdentityAccess`. `Scheduling` và `Payment` không reference module domain
  nào.
- R4: `Booking` gọi `Payment`; `Payment` báo kết quả bằng event `PaymentSucceeded` / `PaymentFailed`.
- R6: `Booking` phát `BookingPaid`; `Ticketing` nghe và cấp vé (chép `customer_id` lên vé).
- R2: bốn con số do endpoint của `Inventory` phục vụ — `Scheduling` không gọi `Inventory`.
- Không dùng mediator **giữa** module.

## Considered Options

| Phương án | Vì sao không chọn |
|---|---|
| Chỉ interface | Upstream phải reference downstream ở #2, #4, #5 ⇒ vòng; thêm module phản ứng phải sửa upstream |
| Chỉ event in-process | Giữ ghế cần **kết quả** (ghế nào thất bại — `FR-15`); event không trả về |
| Mediator (MediatR) giữa module | Không thêm gì so với interface; che hướng phụ thuộc khỏi `.csproj` |
| Mọi phối hợp qua outbox | Mất atomic ở ranh giới bị cắt (vd. suất đã hủy mà đơn vẫn chờ ⇒ cấp vé cho suất đã hủy) |

## Consequences

- Đồ thị module là DAG; architecture test (T15) kiểm tra từng cạnh.
- Event in-process **không** phải cơ chế bất đồng bộ: nó chạy trong cùng transaction. Ai đọc code cần
  biết điều này — đặt tên interface rõ (`IInProcessEvent`), tách hẳn khỏi outbox.
- Luồng thanh toán (#2) có một vòng gọi trong cùng transaction: `Payment` phát event → `Booking` xử lý
  → `Booking` gọi lại interface của `Payment`. Hợp lệ vì không có tham chiếu compile-time ngược.
