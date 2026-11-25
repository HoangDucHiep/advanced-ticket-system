# Cross-Context Contracts

Lớp 2 — Domain. Hợp đồng giữa các bounded context: dữ liệu gì đi qua, dưới hình thức nào, từ bên
nào, và những gì các bên có thể giả định về nhau.

**Nguồn:** ticket `.scratch/advanced-ticket-system/issues/09-cross-context-contracts.md`.
**Bối cảnh:** [`context-map.md`](context-map.md) — 6 context, 9 quan hệ R1–R9.
**Thuật ngữ:** [`ubiquitous-language.md`](ubiquitous-language.md).
**Sửa lớn 2026-11-25** sau review: [ADR-0005](../adr/0005-showtime-seat-aggregate.md),
[ADR-0006](../adr/0006-payment-expiry-refund-flow.md),
[ADR-0007](../adr/0007-per-customer-showtime-quota.md).

## Cách đọc

Mỗi hợp đồng ghi rõ:

1. **Dữ liệu đi theo hướng nào** — một chiều hay hai chiều.
2. **Hình thức** — gọi trong process qua interface, truy vấn đọc, hay ghi chung DB transaction.
3. **Bảng thuật ngữ** — cặp tên ở hai bên, có cùng nghĩa không, có cần Anti-Corruption Layer không.
4. **Định nghĩa hợp đồng** — dữ liệu nào, ở mức khái niệm (không phải JSON schema).
5. **Điều khoản** — B được giả định gì về A, A được đổi gì mà không phá B.

**Quy tắc chung cho mọi hợp đồng:**

- **Không context nào đọc hoặc ghi trực tiếp bảng của context khác.** Mọi trao đổi đi qua interface
  của context sở hữu — kể cả khi hai bên ghi chung một DB transaction (khi đó interface tham gia vào
  transaction đang mở). Cách hiện thực chốt ở T10.
- **Không lời gọi ra ngoài (Stripe, email) nào nằm trong DB transaction.** Ra ngoài đi qua outbox.

---

## Hợp đồng: `Scheduling` → `Inventory` (R1)

**Hướng**: `Scheduling` → `Inventory`.

**Hình thức**: ghi chung DB transaction, lúc **tạo suất chiếu**.

### Dữ liệu

| Thuật ngữ ở Scheduling | Thuật ngữ ở Inventory | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Room seat` | `Showtime seat` | **KHÔNG** | `Room seat` = vị trí vật lý (hàng, số, loại). `Showtime seat` = ghế của một suất, có trạng thái. Khác aggregate, khác vòng đời |
| `Seat type` | `Seat type` (bản sao trên ghế) | Có | Chép lúc sinh ghế để đếm ghế đôi và tra giá |
| `Showtime` | `Showtime id` | Có | Suất nào sở hữu những ghế nào |

`Ticket price` **không** đi qua R1 và **không** được lưu trên `Showtime seat`. Giá thuộc cặp
(suất chiếu, loại ghế) ở `Scheduling`; `Booking` đọc và chốt giá lúc giữ ghế (R9).

### Hợp đồng

Khi quản lý rạp **tạo** một suất chiếu, `Scheduling` cung cấp danh sách `Room seat` của phòng
(id ghế phòng, loại ghế). `Inventory` sinh **một** `Showtime seat` cho **mỗi** `Room seat`, trạng
thái `Available`. **All-or-nothing**, cùng transaction với tạo `Showtime` (ranh giới bị cắt #5).

Ghế bị chặn **không** đi qua R1: chặn ghế là lệnh **sau** khi ghế đã được sinh, trên `Inventory`
(`Available → Blocked`). `Blocked` chỉ có **một** nguồn sự thật: `Inventory`.

### Anti-Corruption Layer

Phía `Inventory`: nó không biết "phòng", "phim" là gì; chỉ nhận "sinh ghế cho suất này theo danh
sách (id ghế phòng, loại)". Mọi logic **vị trí trong phòng** nằm ở `Scheduling`.

### Điều khoản

- **`Inventory` được giả định**: sơ đồ phòng không đổi khi phòng còn suất chưa kết thúc và chưa hủy
  (`INV-13` ở `Scheduling`).
- **`Scheduling` được đổi mà không phá `Inventory`**: phim, suất chiếu khác, giá của suất
  `NotOnSale`.
- **Không được**: `Inventory` đọc bảng `Room`, `Movie`.

---

## Hợp đồng: `Inventory` → `Scheduling` (R2)

**Hướng**: `Inventory` → `Scheduling`, chỉ đọc.

**Hình thức**: gọi trong process qua interface của `Inventory` (ví dụ
`IInventoryQueries.GetSeatAvailabilitySummary(showtimeId)`).

### Hợp đồng

```
{ available_count, held_count, sold_count, blocked_count }
```

Hiển thị cho quản lý rạp (`FR-23`).

### Điều khoản

- **`Scheduling` được giả định**: bốn con số cộng lại bằng **tổng số ghế của phòng** — gồm cả ghế
  bị chặn (`FR-23`, `BR-11`).
- **`Inventory` được đổi**: cách tính (đếm trực tiếp, bảng tổng hợp, cache) miễn công thức vẫn đúng.
- **Không được**: `Scheduling` đọc bảng `Showtime seat`.

ACL: **không cần**.

---

## Hợp đồng: `Inventory` ↔ `Booking` (R3)

**Hướng**: hai chiều, trong **cùng một DB transaction**.

**Hình thức**: ghi chung DB transaction, qua interface của từng bên.

### Dữ liệu

| Thuật ngữ ở Inventory | Thuật ngữ ở Booking | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Showtime seat` (trạng thái, loại ghế) | `Booking line` (tham chiếu ghế) | **KHÔNG** | `Booking line` = một ghế **trong đơn**, kèm giá đã chốt. `Showtime seat` = ghế của suất, có trạng thái |
| `hold_id` | `Booking id` | **KHÔNG** (với `Inventory`) | `Inventory` coi `hold_id` là tham chiếu **không diễn giải**; `Booking` dùng id đơn làm giá trị |
| `held_until` | `expires_at` | Có — **bản sao** | Nguồn sự thật là `Booking.expires_at`; `held_until` ghi cùng lúc để điều kiện lệnh trên ghế đọc được |

### Hợp đồng

**Lệnh giữ n ghế** (`BR-02`, `BR-06`, `BR-08`):

```
Yêu cầu từ client (qua Booking):
  idempotency_key
  showtime_id
  showtime_seat_ids: [...]          ← CHỈ id ghế. Không nhận loại ghế hay giá từ client.
```

Trong **một** DB transaction, theo thứ tự:

1. `Booking`: kiểm tra suất `OnSale` và chưa tới giờ chiếu (R9).
2. `Inventory`: trả loại ghế của các id ghế (dữ liệu server) ⇒ `Booking` kiểm tra ≤ 6 theo quy tắc
   đếm.
3. `Booking`: khoá quota `(customer, showtime)`, đếm ghế hiện có (đơn `AwaitingPayment` còn hạn +
   `Paid`), kiểm tra ≤ 10 (ADR-0007).
4. `Inventory`: chuyển các ghế `Available → Held` theo **thứ tự tăng dần id**, ghi `hold_id`,
   `held_until`. Số ghế chuyển được < n ⇒ báo **ghế nào** không giữ được ⇒ rollback toàn bộ.
5. `Booking`: tạo `Booking` (`AwaitingPayment`), `Booking line` với giá chốt (R9), `expires_at`,
   dòng lịch sử đầu tiên.

**Lệnh trả ghế** (hết hạn, huỷ): `Booking` gọi `Inventory` "trả các ghế có `hold_id` = X". Chỉ ghế
`Held` với đúng `hold_id` đó được trả.

### Điều khoản

**Ranh giới bị cắt #1** — giữ ghế: chỉ một bên ghi được ⇒ hệ thống sai (ghế giữ không có đơn, hoặc
đơn không có ghế) ⇒ **một DB transaction**.

**Ranh giới bị cắt #3** — hết hạn / huỷ: trả ghế + đổi trạng thái đơn ⇒ **một DB transaction**.

**`Inventory` được giả định:**
- `hold_id` và `held_until` do bên gọi cung cấp đúng; nó không kiểm tra nghĩa của chúng.

**`Booking` được giả định:**
- Trạng thái ghế được kiểm tra **trong lệnh** mỗi lần giữ, không dùng dữ liệu đã hiển thị/đã cache.
- Lệnh trả ghế theo `hold_id` không bao giờ trả ghế của lần giữ khác.

**Không được:** `Booking` đọc bảng ghế; `Inventory` đọc bảng đơn.

---

## Hợp đồng: `Booking` ↔ `Payment` (R4)

**Hướng**: hai chiều.

**Hình thức**: ghi chung DB transaction (tạo intent; xử lý thông báo).

### Dữ liệu

| Thuật ngữ ở Booking | Thuật ngữ ở Payment | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Booking` | `Payment intent` | **KHÔNG** | Đơn vs. yêu cầu thanh toán. Khác aggregate, khác vòng đời |
| `AwaitingPayment` | `AwaitingPayment` | **KHÔNG** | Cùng tên, khác máy trạng thái. Q28 — chốt ở T11 |
| `Paid` | `Captured` | Gần — xảy ra **cùng transaction** | `Booking.Paid` ⇔ `Payment intent.Captured` (trừ khi sau đó suất bị hủy: `Captured → RefundPending`) |
| (không có) | `RefundPending`, `Refunded`, `Refund` | — | `Booking` **không** có trạng thái hoàn tiền (Q27) |

### Hợp đồng

**Tạo intent** (người xem bấm thanh toán): `Booking` cung cấp `booking_id`, tổng tiền (VND). `Payment`
tạo intent (`AwaitingPayment`) hoặc trả intent cũ nếu đã có. Gọi Stripe tạo PaymentIntent **ngoài**
DB transaction.

**Xử lý thông báo thành công** — một DB transaction:

1. `Payment` ghi `Payment event` (`event.id` đã có ⇒ dừng).
2. `Payment` yêu cầu `Booking` chuyển `AwaitingPayment → Paid` với điều kiện `expires_at > now()`.
3. Thành công ⇒ intent → `Captured`, R6 cấp vé, ghế → `Sold`.
4. Không thành công (đơn `Expired`/`Cancelled`/quá hạn) ⇒ intent → `RefundPending` + outbox. **Không**
   cấp vé.

**Xử lý thông báo thất bại**: `Payment` chỉ ghi `Payment event`. Intent vẫn `AwaitingPayment`, đơn
vẫn `AwaitingPayment` — người xem thử lại bằng **cùng** intent trong thời gian còn lại (J-04.4).

**Đơn kết thúc mà chưa trả** (`Expired` / `Cancelled`): `Booking` báo `Payment` ⇒ `Payment` ghi outbox
"huỷ PaymentIntent phía Stripe" (best-effort).

### Điều khoản

- **`Booking` được giả định**: không bao giờ chuyển `Paid` khi tiền chưa thu.
- **`Payment` được giả định**: tổng tiền đơn không đổi sau khi tạo; một đơn đúng một intent.
- **Q27**: hủy suất chiếu **không** đổi `Booking.Paid`. Dấu vết hoàn tiền nằm ở `Payment`
  (`RefundPending` / `Refunded`). Hệ quả: lịch sử đơn cho `FR-31` được ghép từ lịch sử `Booking` **và**
  lịch sử `Payment event` theo `booking_id` (chỉ đọc); danh sách vé hiển thị trạng thái "suất đã hủy —
  đã/đang hoàn tiền" (`BR-30`).
- **Không được**: hai bên đọc bảng của nhau.

---

## Hợp đồng: `Payment` → `Stripe` (R5)

**Hướng**: hai chiều (gọi API + webhook).

**Hình thức**: gọi API bên ngoài (qua outbox với mọi lời gọi có tác dụng phụ) + webhook.

### Dữ liệu

| Thuật ngữ ở Payment | Thuật ngữ ở Stripe | Ghi chú |
|---|---|---|
| `Payment intent` | `payment_intent` | Abstraction của ta vs. API object. Lưu `payment_intent.id` làm `external_id` |
| `AwaitingPayment` | `requires_payment_method`, `requires_confirmation`, `requires_action`, `processing` | Gộp mọi trạng thái trung gian — kể cả sau thất bại |
| `Captured` | `payment_intent.succeeded` | |
| `RefundPending` | (không có) | Ta đã ghi yêu cầu hoàn, chờ Stripe xác nhận |
| `Refunded` | `charge.refunded` với số tiền hoàn = số đã thu | Ta chỉ hoàn toàn bộ |
| `Money(amount, VND)` | `amount`, `currency = "vnd"` | **VND là tiền không có phần lẻ ở Stripe**: `amount` là số đồng, không nhân 100 |
| `event.id` | `evt_…` (chuỗi) | **Không phải UUID** |

### Sự kiện Stripe được lắng nghe

Chỉ ba loại — mỗi kết quả nghiệp vụ ứng với **đúng một** loại sự kiện, để không xử lý cùng một kết
quả hai lần qua hai `event.id` khác nhau:

| Sự kiện | Ý nghĩa với ta |
|---|---|
| `payment_intent.succeeded` | Thông báo thành công |
| `payment_intent.payment_failed` | Thông báo thất bại (không đổi trạng thái intent) |
| `charge.refunded` | Xác nhận đã hoàn |

**Không** lắng nghe `charge.succeeded` (trùng nghĩa với `payment_intent.succeeded`).

### Lời gọi ra Stripe

| Lời gọi | Khi nào | Idempotency |
|---|---|---|
| Tạo PaymentIntent | Tạo intent | `Idempotency-Key` = id intent của ta |
| Hoàn tiền | Worker outbox, intent `RefundPending` | `Idempotency-Key` = id `Refund` |
| Huỷ PaymentIntent | Worker outbox, đơn `Expired`/`Cancelled` khi chưa thu | `Idempotency-Key` = id intent + "cancel" |

### Điều khoản

- **Stripe gửi webhook ít nhất một lần, có thể trễ, có thể sai thứ tự.** Mọi thứ tự đến đều phải
  cho kết quả đúng — xem bảng "transitions bị từ chối" của máy `Payment intent`.
- `Payment` xác minh **chữ ký** webhook trước khi xử lý.
- **Không được**: `Booking` gọi Stripe hoặc đọc payload webhook.

---

## Hợp đồng: `Booking` → `Ticketing` (R6)

**Hướng**: `Booking` → `Ticketing`.

**Hình thức**: ghi chung DB transaction, trong transaction `Booking → Paid`.

### Dữ liệu

| Thuật ngữ ở Booking | Thuật ngữ ở Ticketing | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Booking line` | `Ticket` | **KHÔNG** | Một dòng của đơn sinh **một** vé |
| `Paid` | (điều kiện để cấp vé) | — | `Paid` ⇔ đã có đủ vé (cùng transaction) |

### Hợp đồng

```
Yêu cầu (từ Booking, trong transaction → Paid):
  booking_id, showtime_id, showtime_seat_ids: [...]
```

`Ticketing` tạo một `Ticket` (`Issued`, mã ký HMAC) cho mỗi ghế. Unique `showtime_seat_id`.

### Điều khoản

- **Không có cờ "đã cấp vé".** Vì `Paid` và cấp vé cùng một transaction, "đơn `Paid` chưa có vé"
  **không thể xảy ra** (đóng Q26).
- **Lỗi không mong đợi** (ví dụ unique `showtime_seat_id` bị vi phạm) ⇒ rollback **cả** transaction,
  kể cả dòng `Payment event` ⇒ Stripe gửi lại. Quá số lần thử ⇒ danh sách xử lý tay + cảnh báo
  quản trị viên. Không được im lặng nuốt lỗi khi tiền đã thu.
- **Không được**: `Ticketing` đọc bảng `Booking line`; cấp vé từ bất kỳ đâu khác ngoài transaction
  `→ Paid`.

ACL: **không cần**.

---

## Hợp đồng: `Scheduling` → `Ticketing` (R7)

**Hướng**: `Scheduling` → `Ticketing`, chỉ đọc.

**Hình thức**: gọi trong process qua interface của `Scheduling` (ví dụ
`ISchedulingQueries.GetShowtime(showtimeId)`).

### Hợp đồng

`Ticketing` đọc của một suất chiếu: trạng thái (đặc biệt `Cancelled`), giờ bắt đầu/kết thúc, phòng,
phim (tên), chủ sở hữu (để kiểm tra quyền tạo `Gate assignment`).

**Phân công nhân viên cổng không đi qua R7** — nó là aggregate `Gate assignment` của `Ticketing`.

### Điều khoản

- **`Ticketing` được giả định**: giờ và phòng của suất `OnSale` không đổi.
- Trạng thái suất **có thể** đổi sang `Cancelled` bất kỳ lúc nào ⇒ `Ticketing` đọc lại lúc check-in.
- **Không được**: `Ticketing` đọc bảng `Movie`, `Room`, `Showtime`.

---

## Hợp đồng: `Identity & Access` → các context (R8)

**Hướng**: `Identity & Access` → `Scheduling`, `Inventory`, `Booking`, `Ticketing`, chỉ đọc.

**Hình thức**: gọi trong process qua interface (ví dụ `IIdentityQueries.GetCurrentRole(userId)`).

### Hợp đồng

- Token đăng nhập chỉ xác định **ai** đang gọi.
- **Vai trò hiện hành** luôn lấy từ `Identity & Access`, không lấy từ token — vì thu quyền phải có
  hiệu lực **ngay** (`BR-26`).
- Kiểm tra **chủ sở hữu** (đơn, vé, suất chiếu) **không** thuộc `Identity & Access` — mỗi context tự
  kiểm tra trên dữ liệu của mình.

### Điều khoản

- Được **cache** vai trò, với điều kiện cache bị **vô hiệu ngay** khi vai trò đổi (ví dụ theo số
  phiên bản quyền). Chi tiết ở T16.
- Danh sách vai trò cố định: `Customer`, `Theater manager`, `Gate staff`, `Administrator`.

ACL: **không cần**.

---

## Hợp đồng: `Scheduling` → `Booking`, `Inventory` (R9) — *mới 2026-11-25*

**Hướng**: `Scheduling` → `Booking` và `Inventory`, chỉ đọc.

**Hình thức**: gọi trong process qua interface của `Scheduling`, có thể bên trong DB transaction của
bên gọi.

> **Vì sao thêm.** Bản T06 không có quan hệ nào cho việc `Booking` đọc **giá** (để chốt) và
> **trạng thái/giờ** của suất lúc giữ ghế, dù bảng "nguồn sự thật" đã ghi `Booking` đọc giá từ
> `Scheduling`.

### Hợp đồng

- `Booking` đọc: trạng thái suất, giờ bắt đầu (`BR-13`, `BR-31`), `Ticket price` theo `Seat type`
  của suất (để chốt vào `Booking line`).
- `Inventory` đọc: chủ sở hữu suất (để kiểm tra quyền chặn ghế).

### Điều khoản

- Giá của suất `OnSale` **không đổi** — giá đã chốt trong đơn vẫn là bản sao, nên kể cả nếu sau
  này cho đổi giá, đơn cũ không bị ảnh hưởng (`INV-02` của `Booking`).
- **Không được**: đọc bảng `Showtime` trực tiếp.

ACL: **không cần**.

---

## Ranh giới bị cắt — năm chỗ

Một DB transaction chạm aggregate ở nhiều context. Chỉ gồm **ghi database**; gọi ra ngoài qua
outbox. Chi tiết transition: [`state-machines.md`](state-machines.md).

| # | Bất biến | Contracts | Hậu quả nếu tách service |
|---|---|---|---|
| **1** | Giữ n ghế: đủ n hoặc không ghế nào; đơn và ghế cùng hạn | R3, R9 | Saga giữ ghế; có lúc ghế giữ mà chưa có đơn |
| **2** | Thu tiền ⇒ `Paid` ⇒ `Sold` ⇒ cấp vé, đúng một lần | R4, R6, R3 | Outbox + consumer idempotent; vé mồ côi nếu consumer lỗi |
| **3** | Hết hạn / huỷ: trả ghế + chuyển đơn cùng lúc | R3 | Compensating; trả ghế theo `hold_id` nên không trả nhầm |
| **4** | Hủy suất: huỷ đơn chờ + yêu cầu hoàn mọi đơn đã trả | R3, R4, R9 | Một sự kiện sinh N lượt idempotent. Gọi Stripe **đã** bất đồng bộ ngay từ bây giờ |
| **5** | Suất tồn tại ⇔ ghế của suất tồn tại đủ | R1 | Saga tạo suất |

**Quyết định:** trong modular monolith, mỗi chỗ là **một DB transaction**. T10 và T12 phải nhắc lại:
không "tối ưu" bằng cách chia transaction mà không đổi logic.

---

## Các cặp thuật ngữ khác nghĩa

| # | Bên A | Bên B | Nơi dịch | Giải thích |
|---|---|---|---|---|
| 1 | `Room seat` (Scheduling) | `Showtime seat` (Inventory) | Phía `Inventory` | Vị trí vật lý vs. ghế của suất có trạng thái. R1 |
| 2 | `Cancelled` (Booking) | `Cancelled` (Showtime) | Không dịch; ghi rõ | Khác đơn vị, khác hậu quả. Huỷ suất **kéo theo** huỷ các đơn chờ |
| 3 | `Ticket price` gốc (Scheduling) | Giá đã chốt (Booking line) | Phía `Booking` | Chụp lúc giữ ghế, không đổi sau đó. R9 |
| 4 | `hold_id` (Inventory) | `Booking id` (Booking) | Không dịch | `Inventory` không diễn giải `hold_id`. R3 |
| 5 | `Payment intent` (Payment) | `payment_intent` (Stripe) | Phía `Payment` | Stripe nhiều trạng thái hơn, có hoàn một phần. R5 |
| 6 | `Sold` (Inventory) | `Ticket` (Ticketing) | Không dịch; ghi rõ | Trạng thái ghế vs. sản phẩm cấp ra. Cùng transaction. R6 |
| 7 | `AwaitingPayment` (Booking) | `AwaitingPayment` (Payment intent) | Không dịch; **Q28** | Cùng tên, khác máy. Chốt ở T11 |

> Cặp "`Seat hold` vs `Booking`" và "cờ đã cấp vé vs `Ticket`" của bản T09 đã bị **xoá**: `Seat hold`
> không còn là aggregate (ADR-0005), cờ "đã cấp vé" không còn tồn tại (Q26).

---

## Ứng viên tách service (chuyển sang T10)

| Hợp đồng | Một chiều? | Cắt tx? | Ứng viên? |
|---|---|---|---|
| R1 `Scheduling → Inventory` | Có | **Có** (#5) | ❌ |
| R2 `Inventory → Scheduling` | Có, chỉ đọc | Không | ✅ **Cao** |
| R3 `Inventory ↔ Booking` | Hai chiều | **Có** (#1, #3) | ❌ |
| R4 `Booking ↔ Payment` | Hai chiều | **Có** (#2, #4) | ⚠️ Trung bình |
| R5 `Payment → Stripe` | — | Không (đã bất đồng bộ) | ✅ Đã tách |
| R6 `Booking → Ticketing` | Có | **Có** (#2) | ⚠️ Trung bình |
| R7 `Scheduling → Ticketing` | Có, chỉ đọc | Không | ✅ **Cao** |
| R8 `Identity & Access → *` | Có, chỉ đọc | Không | ✅ **Cao** |
| R9 `Scheduling → Booking, Inventory` | Có, chỉ đọc | Đọc **trong** tx #1, #4 | ⚠️ — tách thì phải chấp nhận đọc dữ liệu có thể cũ (giá, trạng thái) |

---

## Câu hỏi còn mở

| # | Câu hỏi | Trạng thái |
|---|---|---|
| Q20 | Module tham gia chung DB transaction thế nào khi không được đọc bảng của nhau? Khi tách service, chuyển các ranh giới bị cắt sang saga/outbox ra sao? | Chốt ở T10 |
| Q28 | Đổi tên `AwaitingPayment` ở một trong hai máy? | Chốt ở T11 |

**Đã đóng:** Q26 (không có cờ "đã cấp vé"), Q27 (`Booking` giữ `Paid` khi hủy suất).

## Trạng thái

Bản làm việc, sửa lớn sau review 2026-11-25. Tiếp theo: T10 — kiến trúc module.
