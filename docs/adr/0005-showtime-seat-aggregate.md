---
id: 0005
title: Showtime seat là aggregate root riêng; giữ n ghế là một DB transaction qua nhiều aggregate
status: accepted
date: 2026-11-25
Supersedes: ADR-0004
Related: ADR-0002, ADR-0003
---

# `Showtime seat` là aggregate root riêng; giữ n ghế là một DB transaction qua nhiều aggregate

## Context

ADR-0004 chốt `Seat hold` là aggregate root của `Inventory`, chứa tối đa 6 `Showtime seat` như
entity bên trong. Khi review lại trước khi đóng Gate B (2026-11-25), quyết định này có ba chỗ
không đứng vững:

1. **Vòng đời không khớp.** Một entity phải sinh ra và chết đi cùng aggregate chứa nó. Nhưng
   `Showtime seat`:
   - tồn tại **trước** mọi lần giữ (trạng thái `Available`, sinh ra lúc mở bán);
   - sống **qua nhiều** lần giữ (giữ → hết hạn → người khác giữ → bán);
   - có transition `Available ↔ Blocked` không dính tới lần giữ nào.

   Không thể có entity thuộc nhiều aggregate instance theo thời gian, hay tồn tại khi không có
   aggregate nào. Chính `docs/domain/aggregates/02-inventory.md` (bảng "Hệ quả") đã ghi "vòng
   đời `Showtime seat` không thuộc aggregate nào" — mâu thuẫn với quyết định #1 của ADR-0004.

2. **Lý do bác phương án (b) không đúng.** ADR-0004 bác "`Showtime seat` là aggregate root riêng"
   vì "all-or-nothing qua N aggregate thì phải dùng compensating transaction". Trong modular
   monolith dùng một database, **một DB transaction cập nhật N dòng vẫn atomic** — không cần lệnh
   bù. Và tài liệu domain đã chấp nhận nhiều aggregate trong một transaction ở mọi chỗ khác (R3,
   R4, R6 — các "ranh giới bị cắt"). Lập luận đang dùng hai chuẩn.

3. **`Seat hold` trùng với `Booking`.** Hai thứ quan hệ 1:1, cùng chủ, cùng ≤ 6 ghế, cùng thời
   điểm hết hạn, sinh và kết thúc cùng lúc (`INV-05` của `Inventory` ≡ `INV-03` của `Booking`).
   Giữ cả hai làm aggregate nghĩa là có **hai** `expires_at` phải luôn bằng nhau — một invariant
   thừa mà không luật nghiệp vụ nào cần.

Thêm nữa, SQL trong ADR-0002 (`SET status='Held', held_until=…, …`) vốn không có bảng
`seat_hold` nào — tức là cơ chế đã chọn khớp với mô hình dưới đây hơn là với ADR-0004.

## Decision

1. **`Showtime seat` là aggregate root**, mỗi aggregate là **một** ghế của **một** suất chiếu
   (một dòng). Nó bảo vệ `BR-01` bằng **điều kiện lệnh** (`UPDATE … WHERE status = 'Available'`)
   như ADR-0002.
2. **Thông tin giữ nằm trên chính dòng ghế**: một tham chiếu giữ (`hold_id`) và thời điểm hết
   hạn giữ (`held_until`). `hold_id` do bên gọi cung cấp (hiện là định danh của `Booking`);
   `Inventory` **không diễn giải** nó — chỉ dùng để biết "ghế này đang được giữ bởi lần giữ nào".
3. **Không còn aggregate `Seat hold`.** "Chỗ giữ ghế" trở thành **thông tin giữ trên ghế**, không
   phải một aggregate hay một bản ghi riêng.
4. **`Booking` là aggregate duy nhất đại diện cho "một lần đặt"**: chủ đơn, các `Booking line`
   (tham chiếu `Showtime seat` bằng id), giá đã chốt, và **`expires_at`**.
5. **Giữ n ghế là một DB transaction chạm N aggregate `Showtime seat` + 1 aggregate `Booking`.**
   Đây là **ngoại lệ có chủ ý** so với quy tắc "một transaction sửa một aggregate", được ghi
   tường minh như các ranh giới bị cắt khác (R3).
6. **Thứ tự khoá không đổi**: các dòng ghế trong một lần giữ được khoá theo thứ tự tăng dần của
   định danh ghế (`BR-22`). Cơ chế cụ thể vẫn là việc của ADR-0002 / T12.

## Why

- **Aggregate nhỏ nhất đủ chứa invariant.** `BR-01` là invariant về **một** ghế ⇒ một ghế là đủ.
  Không có invariant nào cần nhiều ghế nằm trong cùng một aggregate.
- **Vòng đời khớp.** `Showtime seat` sinh lúc mở bán và sống suốt đời suất chiếu — đúng với một
  aggregate root, sai với một entity con của một lần giữ 15 phút.
- **Tham chiếu giữa aggregate bằng id.** `Booking` biết "tôi giữ những ghế nào" qua id, không
  chứa bản thân các ghế.
- **"Một aggregate mỗi transaction" là hướng dẫn, không phải định luật.** Mục đích của nó là giữ
  khả năng tách service. Trong một database, vi phạm có chủ ý là hợp lệ **khi được ghi rõ** — và
  dự án đã có cơ chế ghi rõ: bảng "ranh giới bị cắt". `BR-02` (all-or-nothing) được bảo đảm bởi
  **DB transaction**, không phải bởi ranh giới aggregate.
- **Bỏ một khái niệm trùng lặp.** Một `expires_at` duy nhất (ở `Booking`, bản sao `held_until` trên
  ghế để điều kiện lệnh đọc được) thay vì hai aggregate phải đồng bộ.

## Considered Options

| | Ranh giới | All-or-nothing do đâu | Kích thước | Ghi chú |
|---|---|---|---|---|
| (a) | `Showtime` chứa toàn bộ `Showtime seat` | Aggregate | ~200 dòng | Contention dồn vào một aggregate khi mở bán phim hot. Không chọn |
| **(b)** | **`Showtime seat` là aggregate root riêng** | **Một DB transaction** | **1 dòng** | **Chọn** — ADR-0004 bác phương án này vì lý do sai (xem Context #2) |
| (c) | `Seat hold` là aggregate root chứa ≤ 6 ghế (ADR-0004) | Aggregate | ≤ 6 dòng | Vòng đời entity không khớp; trùng `Booking`. Bị thay thế |
| (c') | `Seat hold` là aggregate chỉ chứa **tham chiếu** ghế + `expires_at` | Một DB transaction | 1 dòng + tham chiếu | Sửa được lỗi vòng đời, nhưng vẫn trùng `Booking` và vẫn có hai `expires_at`. Không chọn |

## Consequences

### Tốt

- Mô hình khớp với vòng đời thật của ghế; không còn mâu thuẫn trong `02-inventory.md`.
- Một nguồn duy nhất cho "lần đặt" và thời điểm hết hạn (`Booking`).
- `held_until` nằm trên dòng ghế ⇒ điều kiện lệnh có thể đọc trực tiếp thời điểm hết hạn. Đây là
  **một hướng** để trả lời Q25 (ghế đã quá hạn nhưng worker chưa quét) — chưa chốt, để T12.
- SQL của ADR-0002 khớp với mô hình mà không cần bảng phụ.

### Âm

- **Một transaction chạm nhiều aggregate.** Phải được ghi trong bảng "ranh giới bị cắt" (R3) và
  nhắc lại ở T10/T12 — đừng ai "tối ưu" bằng cách chia nhỏ transaction.
- **`held_until` trên ghế là bản sao của `Booking.expires_at`.** Hai giá trị được ghi trong cùng
  transaction giữ ghế nên luôn bằng nhau lúc tạo; không có thao tác nào khác được sửa một bên.
- **Khi tách `Inventory` thành service, thao tác giữ ghế vẫn vỡ** (như đã ghi ở context map) —
  quyết định này không làm tốt hơn hay tệ hơn điểm đó.

### Tài liệu cần cập nhật theo quyết định này

- `docs/adr/0004-seat-hold-aggregate-boundary.md` → `superseded by ADR-0005`.
- `docs/domain/aggregates/02-inventory.md` — aggregate là `Showtime seat`; bỏ `Seat hold`.
- `docs/domain/aggregates/03-booking.md` — `Booking` mang `expires_at`.
- `docs/domain/aggregates/README.md` — bảng tổng hợp, sơ đồ, mục "câu hỏi trọng tâm"; bảng
  "invariant không bảo vệ được bằng transaction đơn lẻ" phân biệt rõ **một aggregate** vs **một
  DB transaction**.
- `docs/domain/state-machines.md` — dòng "Aggregate:" của máy `Showtime seat`; tác dụng phụ "tạo
  bản ghi `Seat hold`".
- `docs/domain/cross-context-contracts.md` — R3 (`Booking` lưu `expires_at`; bỏ cặp thuật ngữ
  `Seat hold` / `Booking`).
- `docs/domain/ubiquitous-language.md` — `Seat hold` định nghĩa lại là thông tin giữ trên ghế.
- `docs/plan/00-roadmap.md` — log quyết định.
