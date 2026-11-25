# Inventory — Aggregate và invariant

Bounded context `Inventory` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

Quyết định ranh giới: [ADR-0005](../../adr/0005-showtime-seat-aggregate.md) (thay
[ADR-0004](../../adr/0004-seat-hold-aggregate-boundary.md)).

## Trách nhiệm

Quản lý trạng thái của từng ghế của từng suất chiếu, kể cả **thông tin giữ** (ai đang giữ, giữ
tới lúc nào) và trạng thái **bị chặn**.

`Inventory` **không** biết phim là gì, phòng là gì, đơn đặt vé là gì. Nó chỉ biết "sinh ghế cho
suất chiếu này theo cấu hình này", "ghế này còn trống không", và "ghế này đang được giữ bởi tham
chiếu giữ nào, tới lúc nào".

---

## Aggregate: `Showtime seat`

**Bản chất**: entity, **aggregate root**. Một ghế của một suất chiếu — một dòng dữ liệu. Sinh ra
khi suất chiếu được tạo, sống suốt đời suất chiếu, đi qua nhiều lần giữ.

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| Trạng thái | thuộc tính | `Available` / `Held` / `Sold` / `Blocked` |
| Tham chiếu suất chiếu (`Showtime id`) | tham chiếu | Ghế này của suất nào |
| Tham chiếu ghế phòng (`Room seat id`) | tham chiếu | Ghế vật lý nào |
| `Seat type` | value object (bản sao lúc sinh) | Dùng để đếm ghế (ghế đôi tính 2) và tra giá |
| Thông tin giữ: `hold_id`, `held_until` | value object | Có khi và chỉ khi trạng thái là `Held` |

**Boundary**: nằm **ngoài** aggregate này —

- `Showtime` (thuộc `Scheduling`) — giá, trạng thái mở bán, chủ sở hữu;
- `Booking` (thuộc `Booking`) — đơn đặt vé, `expires_at` gốc, lịch sử, giới hạn theo người;
- `Ticket` (thuộc `Ticketing`) — vé đã cấp.

> **Vì sao không có aggregate `Seat hold` nữa.** ADR-0004 từng đặt `Showtime seat` làm entity bên
> trong `Seat hold`. Không được: ghế tồn tại **trước** mọi lần giữ và sống **qua nhiều** lần giữ —
> entity phải sinh/chết cùng aggregate chứa nó. Và `Seat hold` trùng 1:1 với `Booking` (cùng chủ,
> cùng ≤ 6 ghế, cùng hạn). "Chỗ giữ ghế" giờ là **thông tin giữ** nằm trên chính dòng ghế.

### Invariants

- **INV-01** — Mỗi `Showtime seat` thuộc về **đúng một** chủ tại một thời điểm; bốn trạng thái
  `Available` / `Held` / `Sold` / `Blocked` là loại trừ nhau và đầy đủ.
  - *Cơ chế bảo vệ*: **điều kiện lệnh** — lệnh chuyển trạng thái chỉ thành công khi ghế đang ở
    trạng thái nguồn được kỳ vọng (`Available` → `Held`). Không đọc trước rồi quyết định; điều kiện
    nằm **trong** lệnh. Thêm `CHECK` trên cột trạng thái.
  - *Ai có thể vi phạm*: code ghi trạng thái ghế mà không kèm điều kiện. Luật bắt buộc cho **mọi**
    lệnh ghi trạng thái ghế, không chỉ lệnh giữ.
  - *Traces to*: `BR-01`, `FR-01`, `FR-15`.

- **INV-02** — Ghế `Held` **luôn** có `hold_id` và `held_until`; ghế không `Held` **không** có.
  - *Cơ chế bảo vệ*: cả hai cùng nằm trên **một dòng** và được ghi trong **cùng một lệnh** với
    trạng thái. Thêm `CHECK ((status = 'Held') = (hold_id IS NOT NULL AND held_until IS NOT NULL))`.
  - *Ai có thể vi phạm*: lệnh trả ghế quên xoá thông tin giữ. Ràng buộc chặn.
  - *Traces to*: `BR-01`, `BR-03`, `FR-04`, `FR-14`.

- **INV-03** — Lệnh trả ghế (hết hạn, huỷ) chỉ trả **ghế của đúng lần giữ đó**.
  - *Cơ chế bảo vệ*: điều kiện lệnh `WHERE hold_id = @hold_id AND status = 'Held'`. Nếu ghế đã
    sang tay lần giữ khác thì lệnh ảnh hưởng 0 dòng.
  - *Ai có thể vi phạm*: lệnh trả ghế chỉ lọc theo `showtime_id` + danh sách ghế mà không lọc
    `hold_id` — trả nhầm ghế người khác vừa giữ.
  - *Traces to*: `BR-03`, `BR-21`, `FR-04`, `FR-18`.

- **INV-04** — Ghế `Blocked` **không** được bán online, và **vẫn tính** vào tổng số ghế của phòng.
  - *Cơ chế bảo vệ*: điều kiện lệnh — lệnh giữ ghế chỉ nhận `Available`. "Vẫn tính vào tổng" là
    thuộc tính của **phép đếm**: bốn con số cộng lại bằng tổng số ghế phòng.
  - *Ai có thể vi phạm*: lệnh giữ ghế nới điều kiện thành `status <> 'Sold'`.
  - *Traces to*: `BR-11`, `FR-20`, `FR-23`.

- **INV-05** — Chỉ chuyển `Available ↔ Blocked`. Ghế đang `Held` hoặc `Sold` không bị chặn được.
  - *Cơ chế bảo vệ*: điều kiện lệnh theo trạng thái nguồn. Quyền chặn (quản lý rạp chủ suất chiếu)
    được kiểm tra ở lệnh, dựa trên thông tin chủ sở hữu đọc từ `Scheduling` (R9).
  - *Ai có thể vi phạm*: lệnh chặn ghế không có điều kiện trạng thái nguồn — chặn mất ghế đã bán.
  - *Traces to*: `BR-11`.

### Transaction boundary

Ranh giới aggregate là **một ghế**. Nhưng thao tác **giữ n ghế** là **một DB transaction** chạm
**n** aggregate `Showtime seat` + **1** aggregate `Booking` — ngoại lệ có chủ ý so với "một
transaction sửa một aggregate" (ADR-0005, ranh giới bị cắt R3).

Kết quả của transaction giữ ghế:

- **Thành công**: tất cả n ghế `Held` với cùng `hold_id` và `held_until`; `Booking` tương ứng đã
  được tạo ở `AwaitingPayment` (xem [`03-booking.md`](03-booking.md)).
- **Thất bại**: **không** ghế nào đổi trạng thái, **không** `Booking` nào được tạo, và hệ thống nói
  rõ **ghế nào** đã có người.

> **All-or-nothing (`BR-02`) do DB transaction bảo đảm, không do ranh giới aggregate.** Một
> transaction cập nhật n dòng là atomic. Nếu số dòng ảnh hưởng < n ⇒ rollback toàn bộ.

### Thứ tự khoá để tránh deadlock (`BR-22`)

Hai người chọn ghế chồng nhau theo thứ tự ngược nhau sẽ **kẹt** nếu lấy khoá theo thứ tự ngẫu
nhiên. Điều kiện lệnh **không** chống được điều này — `UPDATE` có điều kiện vẫn lấy row lock.

> **Trong một transaction giữ ghế, khoá luôn theo thứ tự cố định: khoá quota (người xem, suất)
> trước — xem [ADR-0007](../../adr/0007-per-customer-showtime-quota.md) — rồi các dòng ghế theo thứ
> tự tăng dần của định danh ghế.**

Chi tiết kỹ thuật chốt ở T12. Test phải chứng minh **không có deadlock nào xảy ra** — không chỉ
"cả hai đều có phản hồi", vì cơ sở dữ liệu tự phá deadlock bằng cách huỷ một bên (xem ADR-0002).

---

## Phương án ranh giới đã cân nhắc

| | Ranh giới | All-or-nothing do đâu | Kích thước | Kết luận |
|---|---|---|---|---|
| (a) | `Showtime` chứa toàn bộ `Showtime seat` | Aggregate | ~200 dòng | Contention dồn một chỗ khi mở bán hot. Không chọn |
| **(b)** | **`Showtime seat` là aggregate root riêng** | **DB transaction** | **1 dòng** | **Chọn (ADR-0005)** |
| (c) | `Seat hold` chứa ≤ 6 ghế (ADR-0004) | Aggregate | ≤ 6 dòng | Vòng đời entity không khớp; trùng `Booking`. Bị thay thế |

### Hệ quả

| Hệ quả | Chi tiết |
|---|---|
| `Showtime` **không** phải aggregate của `Inventory` | Nó thuộc `Scheduling` — xem [`01-scheduling.md`](01-scheduling.md) |
| Số ghế của một suất không được aggregate nào bảo vệ | Đếm bằng `COUNT`; bốn con số tình trạng bán là **số đo đọc** |
| `held_until` là bản sao của `Booking.expires_at` | Ghi trong cùng transaction giữ ghế; không lệnh nào khác sửa một bên |
| Ghế `Held` đã quá `held_until` mà worker chưa trả | Q25 — chốt ở T12. Vì `Booking` không thể sang `Paid` sau `expires_at` (ADR-0006), ghế này **an toàn để lấy lại**; INV-03 bảo đảm lệnh trả muộn không đụng nhầm ghế đã sang tay |

---

## Invariant không bảo vệ được bằng một transaction đơn lẻ

Trong `Inventory` **không có**. Mọi bất biến về ghế là bất biến của **một dòng**; mọi con số tổng
là **phép đếm** trên dữ liệu đã commit.

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Trạng thái của một ghế suất chiếu (kể cả `Blocked`) | `Inventory` | `Booking` (interface); người xem (sơ đồ ghế) |
| Thông tin giữ trên ghế (`hold_id`, `held_until`) | `Inventory` | `Booking` |
| Bốn con số tình trạng bán của một suất | `Inventory` (đếm trên dữ liệu đã commit) | quản lý rạp chủ suất, qua endpoint của `Inventory` (R2) |

Nguồn sự thật gốc (không phải ở đây): sơ đồ ghế của phòng, loại ghế, giá vé, lịch chiếu —
thuộc `Scheduling`.
