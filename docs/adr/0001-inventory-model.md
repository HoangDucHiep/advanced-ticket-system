---
id: 0001
title: Mô hình tồn kho theo tier
status: superseded by ADR-0003
date: 2026-11-18
Superseded at: 2026-11-19
---

# Mô hình tồn kho theo tier

> **ĐÃ BỊ THAY THẾ (2026-11-19) bởi [ADR-0003](0003-seat-based-inventory.md).**
> Nghiệp vụ đổi từ "bán vé sự kiện theo hạng" sang "đặt vé xem phim theo ghế". Mô hình
> tier-based không biểu diễn được chỗ ngồi cụ thể, và bài toán concurrency của nó ít giá trị
> hơn bài toán giữ nhiều ghế all-or-nothing.
>
> File này được giữ lại để có lịch sử. **Đừng dùng nó làm căn cứ thiết kế.**

## Context

Hệ thống bán vé cho sự kiện có nhiều hạng giá (VIP, GA, …). Cần một mô hình tồn kho biểu
diễn được số lượng vé còn bán được cho từng hạng, và phải bảo vệ được invariant
"số vé đã bán cộng số vé đang giữ chỗ không vượt capacity của hạng đó".

## Decision

Dùng **mô hình tier-based**: mỗi event có nhiều `ticket_tier`, mỗi tier có một `capacity` cố
định. Số lượng vé còn bán được của một tier là `capacity - sold - reserved`, trong đó
`sold` là số vé đã thanh toán và `reserved` là số vé đang được giữ chỗ.

Chưa dùng mô hình seat-based (mỗi vé gắn với một chỗ ngồi cụ thể).

## Why

- Tier-based là mô hình phổ biến của nghiệp vụ concert/bóng đá, và nó làm invariant
  "tổng vé của một hạng không vượt capacity" trở thành một phép so sánh số đơn giản — có thể
  ép bằng `CHECK` constraint ở tầng database.
- Seat-based đòi hỏi thêm một bảng ghế và thêm bài toán "ghế đã bán ở hạng khác", làm phình
  scope mà project này chưa cần.
- Mô hình này cho phép chốt cơ chế chống oversell ở tầng SQL (xem
  [ADR-0002](0002-oversell-prevention.md)) mà không cần lock nhiều bảng.

## Considered Options

| Phương án | Ưu | Nhược |
|---|---|---|
| **Tier-based** (chọn) | Đơn giản, invariant ép được bằng `CHECK`, ít bảng | Không biểu diễn được chỗ ngồi cụ thể |
| Seat-based | Biểu diễn được chỗ ngồi, chặt chẽ hơn về mặt nghiệp vụ | Thêm bảng, thêm bài toán tranh chấp ghế, phình scope |
| Không giới hạn capacity (chỉ đếm sold) | Đơn giản nhất | Không có đường kiểm soát trước — từ chối ở mức thanh toán, tệ hơn nhiều cho người dùng |

## Consequences

- `ticket_tier` cần ba cột: `capacity`, `sold`, `reserved`, và một `CHECK (sold + reserved <= capacity)`.
- Trạng thái `reserved` tồn tại trong cùng bảng với `sold`. Việc giữ chỗ và việc bán đều là
  cập nhật cột trên cùng một dòng — điều kiện cần cho cơ chế chống oversell.
- ~~Nếu nghiệp vụ sau này cần chỗ ngồi cụ thể, đây là ADR phải được mở lại.~~ **(Đã xảy ra —
  xem ADR-0003.)**

## Revalidate at Gate B

> **Không còn hiệu lực.** ADR này đã bị thay thế bởi ADR-0003 trước khi tới Gate B. Phần
> "Revalidate" dưới đây được giữ lại để thấy câu hỏi đã được trả lời thế nào.

Quyết định này được chốt **tạm** để có một mô hình đủ dùng. Ở Gate B, khi aggregate và
invariant được xác định chính thức, phải trả lời:

- Invariant tồn kho có thực sự nằm trong **một** aggregate, hay bị chia ra? → **Không còn
  câu hỏi**: mô hình tier-based đã bị bỏ.
- Có cần seat-based cho bài toán check-in không? → **Có.** Đã chốt ở ADR-0003.
- `reserved` và `sold` có cần tách thành hai bảng (để giảm contention) không? → **Không còn
  câu hỏi**: trạng thái ghế thay cho cả hai.

