---
id: 0004
title: Ranh giới aggregate của tồn kho ghế — Seat hold là aggregate root
status: superseded by ADR-0005
date: 2026-11-21
Superseded at: 2026-11-25
Related: ADR-0002, ADR-0003
---

# Ranh giới aggregate của tồn kho ghế — `Seat hold` là aggregate root

> **ĐÃ BỊ THAY THẾ (2026-11-25) bởi [ADR-0005](0005-showtime-seat-aggregate.md).**
> `Showtime seat` không thể là entity bên trong `Seat hold` vì vòng đời không khớp (ghế tồn tại
> trước và sống qua nhiều lần giữ), và lý do bác phương án (b) không đúng trong monolith — một DB
> transaction cập nhật N dòng vẫn atomic. File này được giữ lại để có lịch sử. **Đừng dùng nó làm
> căn cứ thiết kế.**

## Context

ADR-0003 chốt mô hình **seat-based**: mỗi ghế của một suất chiếu là một dòng dữ liệu riêng,
không có khái niệm `capacity`. ADR-0002 chốt cơ chế chống bán trùng bằng **conditional UPDATE**.

Còn một câu hỏi chưa có câu trả lời, và nó là câu hỏi trọng tâm của Gate B:

> **Một suất chiếu có hàng trăm ghế — có nên là một aggregate không?**

Câu hỏi này không phải lý thuyết. Nó quyết định trực tiếp: thao tác "giữ 4 ghế" có còn là
**một lệnh nguyên tử** không, và contention khi mở bán phim hot dồn vào đâu.

Hai ràng buộc kéo ngược nhau:

- **`BR-02`** — giữ n ghế phải là **all-or-nothing**. Nếu mỗi ghế là một aggregate riêng, mọi cơ
  chế đảm bảo "đủ 4 hoặc không ghế nào" đều là *compensating transaction* (lùi 3 ghế đã giữ),
  tức là có thời điểm hệ thống ở trạng thái trung gian **vi phạm `BR-02`**.
- **Contention** — nếu `Showtime` là aggregate chứa toàn bộ ghế, mỗi lần giữ ghế là một
  transaction trên tập dòng rất lớn; khi mở bán phim hot, hàng nghìn người tranh cùng một
  suất ⇒ tranh chấp dồn vào **một** aggregate.

## Decision

**`Seat hold` là aggregate root của context `Inventory`.** Nó chứa tối đa **6** `Showtime seat`
(theo `BR-06`).

Ba điều được chốt kèm theo:

1. **`Showtime seat` là entity bên trong ranh giới `Seat hold`**, không phải aggregate root.
2. **`BR-01` (một ghế thuộc đúng một chủ tại một thời điểm) được ép bằng ĐIỀU KIỆN LỆNH** —
   `UPDATE ... WHERE status = 'Available'` — chứ không phải bằng khoá đọc-sửa-ghi.
3. **Thứ tự khoá dòng ghế trong một lần giữ ghế là tăng dần theo định danh ghế** — quy tắc
   toàn cục, xác định trước, để tránh deadlock (`BR-22`).

## Alternatives considered

| | Ranh giới | All-or-nothing | Kích thước aggregate | Ai ép `BR-01` |
|---|---|---|---|---|
| (a) | `Showtime` chứa toàn bộ `Showtime seat` | Tự nhiên | 200 dòng | Aggregate |
| (b) | `Showtime seat` là aggregate root riêng | Phải qua N aggregate | 1 dòng | Constraint + điều kiện lệnh |
| **(c)** | **`Seat hold` là aggregate root, chứa ≤ 6 ghế** | **Gói trong 1 aggregate** | **≤ 6 dòng** | **Constraint + điều kiện lệnh** |

### (a) `Showtime` chứa toàn bộ ghế — không chọn

Với phòng 200 ghế, một lần giữ ghế là một transaction trên một tập dòng rất lớn. Khi mở bán phim
hot, hàng nghìn người tranh cùng một suất ⇒ contention dồn vào một aggregate. Thêm vào đó, `BR-02`
sẽ kéo `Booking` vào cùng aggregate đó — một aggregate vừa chứa hàng trăm ghế, vừa chứa đơn, vừa
chứa lịch sử. Đây là mùi code kinh điển của **anemic domain model**.

### (b) Mỗi `Showtime seat` là aggregate root — không chọn

All-or-nothing phải đi qua N aggregate. Mọi cơ chế bảo đảm "đủ 4 hoặc không ghế nào" đều là
*compensating transaction* ⇒ có thời điểm hệ thống ở trạng thái trung gian **vi phạm `BR-02`**.
`BR-02` là business rule, không phải mô hình tạm thời — không thể chấp nhận trạng thái trung gian
đó.

### (c) `Seat hold` là aggregate root — **đã chọn**

`BR-02` chỉ cần bao trọn n ghế của **một lần giữ**, không cần bao trọn cả suất chiếu. Với `BR-06`
giới hạn 6 ghế, ranh giới này vừa đủ nhỏ để gói trong một transaction, vừa đủ nhỏ để không kẹt.

## Consequences

### Tốt

- **Aggregate nhỏ, nhưng đủ chứa bất biến.** Một lần giữ ghế = **một transaction**. `BR-02` được
  bảo vệ bằng cơ chế của cơ sở dữ liệu, không bằng lệnh bù.
- **`BR-01` phải được bảo vệ bởi constraint + điều kiện lệnh, không phải bởi aggregate.** Đây là
  điểm quan trọng nhất của ADR này: **ranh giới aggregate không phải là cơ chế bảo vệ invariant
  duy nhất**. Với seat-based, điều kiện lệnh còn mạnh hơn khoá đọc-sửa-ghi: không có cửa sổ
  TOCTOU, và hai lệnh đồng thời trên cùng một ghế thì **chỉ một** thắng — lệnh còn lại ảnh
  hưởng 0 dòng và phải được coi là thất bại.
- **Ranh giới transaction = ranh giới aggregate.** Một `Seat hold` = một transaction. Các thao
  tác khác trên ghế (giải phóng khi hết hạn, bán, huỷ) cũng nằm trong ranh giới này.
- **Thứ tự khoá là một quy tắc, không phải một thứ biên dịch.** `BR-02` đòi hỏi một transaction
  trên nhiều dòng ghế ⇒ cần quy tắc thứ tự khoá để không kẹt. Ranh giới aggregate trả lời "một
  transaction bao trọn cái gì", không trả lời "transaction đó khoá theo thứ tự nào".
- **Sau khi tách thành service, thao tác giữ ghế vỡ.** `Seat hold` và `Booking` hiện dùng chung
  một transaction (ranh giới R3). Khi tách, phải dùng outbox + saga.

### Âm

- **Một invariant được bảo vệ ở bên ngoài ranh giới aggregate.** Với `BR-01`, phải có ràng buộc
  `CHECK` trên cột trạng thái và quy tắc "mọi lệnh ghi trạng thái ghế đều phải kèm điều kiện".
  Ràng buộc đó không nằm trong aggregate ⇒ cần convention và cần review.
- **Kích thước aggregate giới hạn ở 6 là hệ quả từ `BR-06`, không phải từ lý do kỹ thu.**
  Nếu `BR-06` đổi, ranh giới aggregate đổi theo. Cần ghi nhớ điều này khi đọc lại.
- **Không thể suy ra số ghế tương lai của một suất chiếu từ aggregate.** Không tồn tại aggregate
  "của suất chiếu" ⇒ số ghế phải **đếm** trên dữ liệu đã commit. Bốn con số tình trạng bán là
  **số đo đọc**, không phải invariant.

### Hành động tiếp theo

- T11 chốt tên bảng, cột và constraint cụ thể.
- T12 chốt cụ thể điều kiện lệnh, thứ tự khoá, và cấp độ isolation.
- T13 chốt TTL và cơ chế giải phóng ghế hết hạn.

## Revalidate at Gate C

Xem lại khi T11 (data model) và T12 (concurrency design) chốt xong:

- Có còn phải tách nhỏ hơn nữa không? (ví dụ: có nên tách `Seat hold` và `Seat release`?)
- Cấu trúc bảng có thể đảm bảo `BR-01` bằng ràng buộc DB không, hay phải dựa vào convention?
- Thiếu khoảng đệm giữa các suất chiếu cùng phòng — đáp ứng `BR-20` — có làm ảnh hưởng ranh giới
  này không?
