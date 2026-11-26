# Inventory — Aggregate và invariant

Bounded context `Inventory` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`GLOSSARY.md`](../../../GLOSSARY.md). Quyết định ranh giới:
[ADR-0005](../../adr/0005-showtime-seat-aggregate.md); so sánh các phương án:
[`README.md`](README.md#một-suất-chiếu-có-phải-là-một-aggregate-không).

## Trách nhiệm

Quản lý trạng thái của từng ghế của từng suất chiếu, kể cả **thông tin giữ** (ai đang giữ, giữ
tới lúc nào) và trạng thái **bị chặn**.

`Inventory` **không** biết phim là gì, phòng là gì, đơn đặt vé là gì. Nó chỉ biết "sinh ghế cho
suất chiếu này theo cấu hình này", "ghế này còn giữ được không", và "ghế này đang được giữ bởi tham
chiếu giữ nào, tới lúc nào".

---

## Aggregate: `Showtime seat`

**Bản chất**: entity, **aggregate root**. Một ghế của một suất chiếu — một dòng dữ liệu. Sinh ra
khi suất chiếu được tạo, sống suốt đời suất chiếu, đi qua nhiều lần giữ. Vòng đời:
[`../state-machines.md`](../state-machines.md#state-machine-showtime-seat).

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| Trạng thái | thuộc tính | Bốn trạng thái, xem state machine |
| Tham chiếu suất chiếu (`Showtime id`) | tham chiếu | Ghế này của suất nào |
| Tham chiếu ghế phòng (`Room seat id`) | tham chiếu | Ghế vật lý nào |
| `Seat type` | value object (bản sao lúc sinh) | Dùng để đếm ghế (ghế đôi tính 2) và tra giá |
| Thông tin giữ: `hold_id`, `held_until` | value object | Có khi và chỉ khi trạng thái là `Held` |

**Boundary**: nằm **ngoài** aggregate này —

- `Showtime` (thuộc `Scheduling`) — giá, trạng thái mở bán, chủ sở hữu;
- `Booking` (thuộc `Booking`) — đơn đặt vé, `expires_at` gốc, lịch sử, giới hạn theo người;
- `Ticket` (thuộc `Ticketing`) — vé đã cấp.

Không có aggregate "giữ ghế" riêng: ghế tồn tại **trước** mọi lần giữ và sống **qua nhiều** lần giữ, nên
"chỗ giữ" là thông tin nằm trên chính dòng ghế (ADR-0005).

### Invariants

- **INV-01** — Mỗi `Showtime seat` thuộc về **đúng một** chủ tại một thời điểm; bốn trạng thái
  loại trừ nhau và đầy đủ.
  - *Cơ chế bảo vệ*: **điều kiện nằm trong lệnh** — lệnh chuyển trạng thái chỉ thành công khi ghế
    đang ở trạng thái nguồn được kỳ vọng (`Available` → `Held`). Không đọc trước rồi quyết định.
    Cộng một ràng buộc trạng thái ở tầng lưu trữ.
  - *Ai có thể vi phạm*: code ghi trạng thái ghế mà không kèm điều kiện. Luật bắt buộc cho **mọi**
    lệnh ghi trạng thái ghế, không chỉ lệnh giữ.
  - *Traces to*: `BR-01`, `FR-01`, `FR-15`.

- **INV-02** — Ghế `Held` **luôn** có `hold_id` và `held_until`; ghế không `Held` **không** có.
  - *Cơ chế bảo vệ*: cả hai cùng nằm trên **một dòng**, ghi trong **cùng một lệnh** với trạng thái,
    và có ràng buộc ở tầng lưu trữ.
  - *Ai có thể vi phạm*: lệnh trả ghế quên xoá thông tin giữ. Ràng buộc chặn.
  - *Traces to*: `BR-01`, `BR-03`, `FR-04`, `FR-14`.

- **INV-03** — Lệnh trả ghế (hết hạn, huỷ) chỉ trả **ghế của đúng lần giữ đó**.
  - *Cơ chế bảo vệ*: điều kiện trong lệnh theo `hold_id`. Ghế đã sang tay lần giữ khác thì lệnh
    không ảnh hưởng gì.
  - *Ai có thể vi phạm*: lệnh trả ghế chỉ lọc theo suất + danh sách ghế mà không lọc `hold_id` — trả
    nhầm ghế người khác vừa giữ.
  - *Traces to*: `BR-03`, `BR-21`, `FR-04`, `FR-18`.

- **INV-04** — Ghế `Blocked` **không** được bán online, và **vẫn tính** vào tổng số ghế của phòng.
  - *Cơ chế bảo vệ*: lệnh giữ ghế không nhận ghế `Blocked`. "Vẫn tính vào tổng" là thuộc tính của
    **phép đếm**: bốn con số cộng lại bằng tổng số ghế phòng.
  - *Ai có thể vi phạm*: lệnh giữ ghế nới điều kiện thành "mọi trạng thái trừ `Sold`".
  - *Traces to*: `BR-11`, `FR-20`, `FR-23`.

- **INV-05** — Chỉ chuyển `Available ↔ Blocked`. Ghế đang `Held` hoặc `Sold` không bị chặn được.
  - *Cơ chế bảo vệ*: điều kiện theo trạng thái nguồn. Quyền chặn (quản lý rạp chủ suất chiếu) được
    kiểm tra ở lệnh, dựa trên thông tin chủ sở hữu đọc từ `Scheduling` (R9).
  - *Ai có thể vi phạm*: lệnh chặn ghế không có điều kiện trạng thái nguồn — chặn mất ghế đã bán.
  - *Traces to*: `BR-11`.

### Ranh giới atomic

Ranh giới aggregate là **một ghế**. Nhưng thao tác **giữ n ghế** là **một lần ghi atomic** chạm **n**
aggregate `Showtime seat` + **1** aggregate `Booking` — ngoại lệ có chủ ý (ranh giới bị cắt #1,
[`README.md`](README.md#ranh-giới-bị-cắt--năm-chỗ)).

- **Thành công**: tất cả n ghế `Held` với cùng `hold_id` và `held_until`; `Booking` tương ứng đã được
  tạo ở `AwaitingPayment` (xem [`03-booking.md`](03-booking.md)).
- **Thất bại**: **không** ghế nào đổi trạng thái, **không** `Booking` nào được tạo, và hệ thống nói
  rõ **ghế nào** đã có người.

All-or-nothing (`BR-02`) do atomic bảo đảm, không do ranh giới aggregate.

### Thứ tự lấy ghế để tránh deadlock (`BR-22`)

Hai người chọn ghế chồng nhau theo thứ tự ngược nhau sẽ **kẹt** nếu lấy khoá theo thứ tự ngẫu
nhiên; điều kiện trong lệnh không chống được điều này. Quy tắc: trong một lần giữ ghế, khoá luôn theo
thứ tự cố định — quota (người xem, suất) trước ([ADR-0007](../../adr/0007-per-customer-showtime-quota.md)),
rồi các ghế theo thứ tự tăng dần của định danh ghế. Cơ chế và test:
[`../../design/concurrency.md`](../../design/concurrency.md) #3.

### Hệ quả

| Hệ quả | Chi tiết |
|---|---|
| `Showtime` **không** phải aggregate của `Inventory` | Nó thuộc `Scheduling` — xem [`01-scheduling.md`](01-scheduling.md) |
| Số ghế của một suất không được aggregate nào bảo vệ | Là phép đếm; bốn con số tình trạng bán là **số đo đọc** |
| `held_until` là bản sao của `Booking.expires_at` | Ghi trong cùng lần giữ ghế; không lệnh nào khác sửa một bên |
| Ghế `Held` đã quá `held_until` mà tác vụ nền chưa trả | **Lệnh giữ mới được lấy luôn** (ADR-0015): đơn cũ không thể `Paid` sau `expires_at` (ADR-0006) và `INV-03` bảo đảm lệnh trả muộn không đụng ghế đã sang tay. Bốn con số tình trạng bán tính ghế này là `Available` |

---

## Invariant không bảo vệ được bằng một aggregate

Trong `Inventory` **không có**. Mọi bất biến về ghế là bất biến của **một dòng**; mọi con số tổng là
**phép đếm** trên dữ liệu đã commit.

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Trạng thái của một ghế suất chiếu (kể cả `Blocked`) | `Inventory` | `Booking` (interface); người xem (sơ đồ ghế) |
| Thông tin giữ trên ghế (`hold_id`, `held_until`) | `Inventory` | `Booking` |
| Bốn con số tình trạng bán của một suất | `Inventory` (đếm trên dữ liệu đã commit) | quản lý rạp chủ suất, qua endpoint của `Inventory` (R2) |

Nguồn sự thật gốc (không phải ở đây): sơ đồ ghế của phòng, loại ghế, giá vé, lịch chiếu — thuộc
`Scheduling`.
