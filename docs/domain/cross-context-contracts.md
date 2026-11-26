# Cross-Context Contracts

Lớp 2 — Domain. Hợp đồng giữa các bounded context: dữ liệu gì đi qua, dưới hình thức nào, từ bên
nào, và những gì các bên có thể giả định về nhau.

**Bối cảnh:** [`context-map.md`](context-map.md) — 6 context, 9 quan hệ R1–R9.
**Thuật ngữ:** [`GLOSSARY.md`](../../GLOSSARY.md).

Mỗi hợp đồng ghi: **hướng** dữ liệu; **hình thức** (gọi trong process qua interface, truy vấn đọc,
hay ghi chung atomic); **bảng thuật ngữ** (cặp tên ở hai bên, cùng nghĩa không, có cần
Anti-Corruption Layer không); **hợp đồng** (dữ liệu nào, ở mức khái niệm); **điều khoản** (bên này
được giả định gì về bên kia, bên kia được đổi gì mà không phá bên này).

**Quy tắc chung cho mọi hợp đồng:**

- **Không context nào đọc hoặc ghi trực tiếp dữ liệu của context khác.** Mọi trao đổi đi qua interface
  của context sở hữu — kể cả khi hai bên ghi chung atomic. Cách hiện thực:
  [`../design/architecture.md`](../design/architecture.md) D3, D4 (ADR-0008, ADR-0009).
- **Không lời gọi ra ngoài (Stripe, email) nào nằm trong lần ghi atomic.** Gọi ra ngoài chạy bất đồng bộ.

---

## Hợp đồng: `Scheduling` → `Inventory` (R1)

**Hướng**: `Scheduling` → `Inventory`.

**Hình thức**: ghi chung atomic, lúc **tạo suất chiếu**.

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
thái `Available`. **All-or-nothing**, atomic với tạo `Showtime` (ranh giới bị cắt #5).

Ghế bị chặn **không** đi qua R1: chặn ghế là lệnh **sau** khi ghế đã được sinh, trên `Inventory`
(`Available → Blocked`). `Blocked` chỉ có **một** nguồn sự thật: `Inventory`.

### Anti-Corruption Layer

Phía `Inventory`: nó không biết "phòng", "phim" là gì; chỉ nhận "sinh ghế cho suất này theo danh
sách (id ghế phòng, loại)". Mọi logic **vị trí trong phòng** nằm ở `Scheduling`.

### Điều khoản

- **`Inventory` được giả định**: sơ đồ phòng không đổi khi phòng còn suất chưa kết thúc và chưa hủy
  (`INV-13` ở `Scheduling`).
- **`Scheduling` được đổi mà không phá `Inventory`**: phim, suất chiếu khác, giá của suất `NotOnSale`.
- **Không được**: `Inventory` đọc dữ liệu `Room`, `Movie`.

---

## Hợp đồng: `Inventory` → `Scheduling` (R2)

**Hướng**: `Inventory` → quản lý rạp, chỉ đọc. (Ở Lớp 2 đây là `Inventory` cung cấp cho nhu cầu
"tình trạng bán" của `Scheduling`.)

**Hình thức**: truy vấn đọc do **chính `Inventory` phục vụ** (một endpoint của module `Inventory`).
`Scheduling` **không** gọi `Inventory` — nếu gọi thì `Scheduling` phụ thuộc `Inventory` trong khi
`Inventory` đã phụ thuộc `Scheduling` (R1, R9) ⇒ vòng. Quyền xem (chủ suất) `Inventory` tự kiểm qua
R9. Xem [`../design/architecture.md`](../design/architecture.md) D9.

### Hợp đồng

| Trường | Ý nghĩa |
|---|---|
| `available_count` | Số ghế còn trống |
| `held_count` | Số ghế đang giữ |
| `sold_count` | Số ghế đã bán |
| `blocked_count` | Số ghế bị chặn |

Hiển thị cho quản lý rạp (`FR-23`).

### Điều khoản

- **Người đọc được giả định**: bốn con số cộng lại bằng **tổng số ghế của phòng** — gồm cả ghế bị chặn
  (`FR-23`, `BR-11`). Ghế `Held` đã quá hạn được tính là `available` (ADR-0015).
- **`Inventory` được đổi**: cách tính, miễn công thức vẫn đúng.
- **Không được**: `Scheduling` đọc dữ liệu `Showtime seat`.

ACL: **không cần**.

---

## Hợp đồng: `Inventory` ↔ `Booking` (R3)

**Hướng**: hai chiều, trong **cùng một lần ghi atomic**.

**Hình thức**: ghi chung atomic, qua interface của từng bên.

### Dữ liệu

| Thuật ngữ ở Inventory | Thuật ngữ ở Booking | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Showtime seat` (trạng thái, loại ghế) | `Booking line` (tham chiếu ghế) | **KHÔNG** | `Booking line` = một ghế **trong đơn**, kèm giá đã chốt. `Showtime seat` = ghế của suất, có trạng thái |
| `hold_id` | `Booking id` | **KHÔNG** (với `Inventory`) | `Inventory` coi `hold_id` là tham chiếu **không diễn giải**; `Booking` dùng id đơn làm giá trị |
| `held_until` | `expires_at` | Có — **bản sao** | Nguồn sự thật là `Booking.expires_at`; `held_until` ghi cùng lúc để lệnh trên ghế đọc được |

### Hợp đồng

**Lệnh giữ n ghế** (`BR-02`, `BR-06`, `BR-08`):

Yêu cầu từ client (qua `Booking`):

| Trường | Ý nghĩa |
|---|---|
| `idempotency_key` | Khoá idempotency của yêu cầu |
| `showtime_id` | Suất chiếu |
| `showtime_seat_ids` | Danh sách ghế — **CHỈ id ghế**. Không nhận loại ghế hay giá từ client |

Trong **một** lần ghi atomic, theo thứ tự:

1. `Booking`: kiểm tra suất `OnSale` và chưa tới giờ chiếu (R9).
2. `Inventory`: trả loại ghế của các id ghế (dữ liệu server) ⇒ `Booking` kiểm tra ≤ 6 theo quy tắc đếm.
3. `Booking`: khoá quota (người, suất), đếm ghế hiện có (đơn `AwaitingPayment` còn hạn + `Paid`),
   kiểm tra ≤ 10 (ADR-0007).
4. `Inventory`: chuyển các ghế sang `Held` theo **thứ tự cố định**, ghi `hold_id`, `held_until`. Thiếu
   một ghế ⇒ báo **ghế nào** không giữ được ⇒ rollback toàn bộ.
5. `Booking`: tạo `Booking` (`AwaitingPayment`), `Booking line` với giá chốt (R9), `expires_at`, dòng
   lịch sử đầu tiên.

**Lệnh trả ghế** (hết hạn, huỷ): `Booking` gọi `Inventory` "trả các ghế có `hold_id` = X". Chỉ ghế
`Held` với đúng `hold_id` đó được trả.

### Điều khoản

**Ranh giới bị cắt #1** (giữ ghế) và **#3** (hết hạn / huỷ): chỉ một bên ghi được ⇒ hệ thống sai
(ghế giữ không có đơn, hoặc đơn không có ghế) ⇒ **một lần ghi atomic**. Danh sách chuẩn:
[`aggregates/README.md`](aggregates/README.md#ranh-giới-bị-cắt--năm-chỗ).

**`Inventory` được giả định:** `hold_id` và `held_until` do bên gọi cung cấp đúng; nó không kiểm tra
nghĩa của chúng.

**`Booking` được giả định:**

- Trạng thái ghế được kiểm tra **trong lệnh** mỗi lần giữ, không dùng dữ liệu đã hiển thị/đã cache.
- Lệnh trả ghế theo `hold_id` không bao giờ trả ghế của lần giữ khác.

**Không được:** `Booking` đọc dữ liệu ghế; `Inventory` đọc dữ liệu đơn.

---

## Hợp đồng: `Booking` ↔ `Payment` (R4)

**Hướng**: hai chiều về dữ liệu. **Hướng phụ thuộc** (ai biết ai) chỉ một chiều: `Booking` gọi
interface của `Payment`; `Payment` **không biết** `Booking` — nó báo kết quả bằng **event** mà
`Booking` nghe (ADR-0009).

**Hình thức**: ghi chung atomic (tạo intent; xử lý thông báo).

### Dữ liệu

| Thuật ngữ ở Booking | Thuật ngữ ở Payment | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Booking` | `Payment intent` | **KHÔNG** | Đơn vs. yêu cầu thanh toán. Khác aggregate, khác vòng đời |
| `AwaitingPayment` | `Unpaid` | **KHÔNG** | Đơn chờ thanh toán vs. intent chưa thu. Hai tên khác nhau có chủ ý (ADR-0010) |
| `Expired` / `Cancelled` | `Closed` | Gần | Đơn kết thúc khi chưa thu ⇒ intent `Closed` cùng lần ghi atomic |
| `Paid` | `Captured` | Gần — xảy ra **cùng lần ghi atomic** | `Booking.Paid` ⇔ `Payment intent.Captured` (trừ khi sau đó suất bị hủy: `Captured → RefundPending`) |
| (không có) | `RefundPending`, `Refunded`, `Refund` | — | `Booking` **không** có trạng thái hoàn tiền |

### Hợp đồng

**Tạo intent** (người xem bấm thanh toán — endpoint thuộc `Booking`): `Booking` kiểm tra đơn của người
gọi, `AwaitingPayment`, còn hạn; rồi gọi `Payment` với id đơn, tổng tiền (VND). `Payment` tạo intent
(`Unpaid`) hoặc trả intent cũ nếu đã có. Gọi Stripe tạo PaymentIntent **ngoài** lần ghi atomic.

**Xử lý thông báo thành công** — một lần ghi atomic:

1. `Payment` ghi `Payment event` (`event.id` đã có ⇒ dừng).
2. `Payment` phát event `PaymentSucceeded(booking_id, intent_id, số tiền)` (in-process).
3. `Booking` nghe event, thử `AwaitingPayment → Paid` với điều kiện `expires_at > now`.
4. Thành công ⇒ `Booking` gọi `Payment`: intent → `Captured`; gọi `Inventory`: ghế → `Sold`; phát
   `BookingPaid` ⇒ `Ticketing` cấp vé (R6).
5. Không thành công (đơn `Expired`/`Cancelled`/quá hạn) ⇒ `Booking` gọi `Payment`: intent →
   `RefundPending`. **Không** cấp vé.

**Xử lý thông báo thất bại**: `Payment` chỉ ghi `Payment event`. Intent vẫn `Unpaid`, đơn vẫn
`AwaitingPayment` — người xem thử lại bằng **cùng** intent trong thời gian còn lại (J-04.4).

**Đơn kết thúc mà chưa trả** (`Expired` / `Cancelled`): trong cùng lần ghi atomic, `Booking` gọi
`Payment` ⇒ intent `Unpaid → Closed` + yêu cầu huỷ PaymentIntent phía Stripe (best-effort, bất đồng
bộ). Nếu tiền vẫn về sau đó ⇒ `Closed → RefundPending`.

### Điều khoản

- **`Booking` được giả định**: không bao giờ chuyển `Paid` khi tiền chưa thu.
- **`Payment` được giả định**: tổng tiền đơn không đổi sau khi tạo; một đơn đúng một intent.
- Hủy suất chiếu **không** đổi `Booking.Paid`. Dấu vết hoàn tiền nằm ở `Payment`
  (`RefundPending` / `Refunded`). Hệ quả: lịch sử đơn cho `FR-31` được ghép từ lịch sử `Booking` **và**
  lịch sử `Payment event` theo đơn (chỉ đọc); danh sách vé hiển thị trạng thái "suất đã hủy —
  đã/đang hoàn tiền" (`BR-30`).
- **Không được**: hai bên đọc dữ liệu của nhau.

---

## Hợp đồng: `Payment` → `Stripe` (R5)

**Hướng**: hai chiều (gọi API + nhận thông báo).

**Hình thức**: gọi API bên ngoài (bất đồng bộ với mọi lời gọi có tác dụng phụ) + nhận thông báo từ Stripe.

### Dữ liệu

| Thuật ngữ ở Payment | Thuật ngữ ở Stripe | Ghi chú |
|---|---|---|
| `Payment intent` | `payment_intent` | Abstraction của ta vs. API object. Lưu `payment_intent.id` làm `external_id` |
| `Unpaid` | `requires_payment_method`, `requires_confirmation`, `requires_action`, `processing` | Gộp mọi trạng thái trung gian — kể cả sau thất bại |
| `Closed` | `canceled` (sau lời gọi huỷ) | Huỷ là best-effort; tiền vẫn về được nếu Stripe đã xử lý trước |
| `Captured` | `payment_intent.succeeded` | |
| `RefundPending` | (không có) | Ta đã ghi yêu cầu hoàn, chờ Stripe xác nhận |
| `Refunded` | `charge.refunded` với số tiền hoàn = số đã thu | Ta chỉ hoàn toàn bộ |
| `Money(amount, VND)` | `amount`, `currency = "vnd"` | **VND là tiền không có phần lẻ ở Stripe**: `amount` là số đồng, không nhân 100 |
| `event.id` | `evt_…` (chuỗi) | **Không phải UUID** |

### Sự kiện Stripe được lắng nghe

Chỉ ba loại — mỗi kết quả nghiệp vụ ứng với **đúng một** loại sự kiện, để không xử lý cùng một kết quả
hai lần qua hai `event.id` khác nhau (chi tiết: [`../design/api-contract.md`](../design/api-contract.md)):

| Sự kiện | Ý nghĩa với ta |
|---|---|
| `payment_intent.succeeded` | Thông báo thành công |
| `payment_intent.payment_failed` | Thông báo thất bại (không đổi trạng thái intent) |
| `charge.refunded` | Xác nhận đã hoàn |

**Không** lắng nghe `charge.succeeded` (trùng nghĩa với `payment_intent.succeeded`).

### Lời gọi ra Stripe

| Lời gọi | Khi nào | Idempotency |
|---|---|---|
| Tạo PaymentIntent | Tạo intent | Khoá idempotency = id intent của ta |
| Hoàn tiền | Tác vụ nền, intent `RefundPending` | Khoá idempotency = id `Refund` |
| Huỷ PaymentIntent | Tác vụ nền, đơn `Expired`/`Cancelled` khi chưa thu | Khoá idempotency = id intent + "cancel" |

### Điều khoản

- **Stripe gửi thông báo ít nhất một lần, có thể trễ, có thể sai thứ tự.** Mọi thứ tự đến đều phải
  cho kết quả đúng — xem bảng "transitions bị từ chối" của máy `Payment intent`
  ([`state-machines.md`](state-machines.md#state-machine-payment-intent)).
- `Payment` xác minh **chữ ký** thông báo trước khi xử lý.
- **Không được**: `Booking` gọi Stripe hoặc đọc nội dung thông báo của Stripe.

---

## Hợp đồng: `Booking` → `Ticketing` (R6)

**Hướng**: `Booking` → `Ticketing` về dữ liệu. **Hướng phụ thuộc**: `Ticketing` nghe event
`BookingPaid` mà `Booking` phát — `Booking` **không biết** `Ticketing` (ADR-0009).

**Hình thức**: ghi chung atomic — event in-process được xử lý **trước khi chốt** lần ghi
`Booking → Paid`.

### Dữ liệu

| Thuật ngữ ở Booking | Thuật ngữ ở Ticketing | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Booking line` | `Ticket` | **KHÔNG** | Một dòng của đơn sinh **một** vé |
| `Paid` | (điều kiện để cấp vé) | — | `Paid` ⇔ đã có đủ vé (cùng lần ghi) |

### Hợp đồng

Event `BookingPaid` (`Booking` phát, trong lần ghi → `Paid`):

| Trường | Ý nghĩa |
|---|---|
| `booking_id` | Đơn |
| `customer_id` | Chủ đơn |
| `showtime_id` | Suất chiếu |
| `showtime_seat_ids` | Danh sách ghế của đơn |

`Ticketing` tạo một `Ticket` (`Issued`, mã có chữ ký) cho mỗi ghế, **chép** `customer_id` lên vé để tự
kiểm tra chủ sở hữu (`FR-10`) mà không phải hỏi `Booking`. Mỗi ghế đúng một vé. Danh sách vé của người
xem / của một đơn do **`Ticketing`** phục vụ.

### Điều khoản

- **Không có cờ "đã cấp vé".** Vì `Paid` và cấp vé cùng một lần ghi atomic, "đơn `Paid` chưa có vé"
  **không thể xảy ra**.
- **Lỗi không mong đợi** (ví dụ vi phạm "một ghế một vé") ⇒ rollback **cả** lần ghi, kể cả dòng
  `Payment event` ⇒ Stripe gửi lại. Quá số lần thử ⇒ danh sách xử lý tay + cảnh báo quản trị viên
  (xem [`aggregates/04-payment.md`](aggregates/04-payment.md)). Không được im lặng nuốt lỗi khi tiền đã thu.
- **Không được**: `Ticketing` đọc dữ liệu `Booking line`; cấp vé từ bất kỳ đâu khác ngoài lần ghi
  `→ Paid`.

ACL: **không cần**.

---

## Hợp đồng: `Scheduling` → `Ticketing` (R7)

**Hướng**: `Scheduling` → `Ticketing`, chỉ đọc.

**Hình thức**: gọi trong process qua interface của `Scheduling`.

### Hợp đồng

`Ticketing` đọc của một suất chiếu: trạng thái (đặc biệt `Cancelled`), giờ bắt đầu/kết thúc, phòng,
phim (tên), chủ sở hữu (để kiểm tra quyền tạo `Gate assignment`).

**Phân công nhân viên cổng không đi qua R7** — nó là aggregate `Gate assignment` của `Ticketing`.

### Điều khoản

- **`Ticketing` được giả định**: giờ và phòng của suất `OnSale` không đổi.
- Trạng thái suất **có thể** đổi sang `Cancelled` bất kỳ lúc nào ⇒ `Ticketing` đọc lại lúc check-in.
- **Không được**: `Ticketing` đọc dữ liệu `Movie`, `Room`, `Showtime`.

---

## Hợp đồng: `Identity & Access` → các context (R8)

**Hướng**: `Identity & Access` → `Scheduling`, `Inventory`, `Booking`, `Ticketing`, chỉ đọc.

**Hình thức**: gọi trong process qua interface.

### Hợp đồng

- Token đăng nhập chỉ xác định **ai** đang gọi.
- **Vai trò hiện hành** luôn lấy từ `Identity & Access`, không lấy từ token — vì thu quyền phải có
  hiệu lực **ngay** (`BR-26`).
- Kiểm tra **chủ sở hữu** (đơn, vé, suất chiếu) **không** thuộc `Identity & Access` — mỗi context tự
  kiểm tra trên dữ liệu của mình.

### Điều khoản

- Được **nhớ tạm** vai trò, với điều kiện bản nhớ bị **vô hiệu ngay** khi vai trò đổi (theo số phiên
  bản vai trò). Cơ chế: [`../design/api-contract.md`](../design/api-contract.md).
- Danh sách vai trò cố định: `Customer`, `Theater manager`, `Gate staff`, `Administrator`.

ACL: **không cần**.

---

## Hợp đồng: `Scheduling` → `Booking`, `Inventory` (R9)

**Hướng**: `Scheduling` → `Booking` và `Inventory`, chỉ đọc.

**Hình thức**: gọi trong process qua interface của `Scheduling`, có thể bên trong lần ghi atomic của
bên gọi.

### Hợp đồng

- `Booking` đọc: trạng thái suất, giờ bắt đầu (`BR-13`, `BR-31`), `Ticket price` theo `Seat type`
  của suất (để chốt vào `Booking line`).
- `Inventory` đọc: chủ sở hữu suất (để kiểm tra quyền chặn ghế).

### Điều khoản

- Giá của suất `OnSale` **không đổi** — giá đã chốt trong đơn vẫn là bản sao, nên kể cả nếu sau này cho
  đổi giá, đơn cũ không bị ảnh hưởng (`INV-02` của `Booking`).
- **Không được**: đọc dữ liệu `Showtime` trực tiếp.

ACL: **không cần**.

---

## Ranh giới bị cắt

Năm chỗ một lần ghi atomic chạm aggregate ở nhiều context — danh sách chuẩn, kèm hậu quả nếu tách
service: [`aggregates/README.md`](aggregates/README.md#ranh-giới-bị-cắt--năm-chỗ). Theo hợp đồng: #1 và
#3 ở R3; #2 ở R4 + R6; #4 ở R3, R4, R9; #5 ở R1.

---

## Các cặp thuật ngữ khác nghĩa

| # | Bên A | Bên B | Nơi dịch | Giải thích |
|---|---|---|---|---|
| 1 | `Room seat` (Scheduling) | `Showtime seat` (Inventory) | Phía `Inventory` | Vị trí vật lý vs. ghế của suất có trạng thái. R1 |
| 2 | `Cancelled` (Booking) | `Cancelled` (Showtime) | Không dịch; ghi rõ | Khác đơn vị, khác hậu quả. Huỷ suất **kéo theo** huỷ các đơn chờ |
| 3 | `Ticket price` gốc (Scheduling) | Giá đã chốt (Booking line) | Phía `Booking` | Chụp lúc giữ ghế, không đổi sau đó. R9 |
| 4 | `hold_id` (Inventory) | `Booking id` (Booking) | Không dịch | `Inventory` không diễn giải `hold_id`. R3 |
| 5 | `Payment intent` (Payment) | `payment_intent` (Stripe) | Phía `Payment` | Stripe nhiều trạng thái hơn, có hoàn một phần. R5 |
| 6 | `Sold` (Inventory) | `Ticket` (Ticketing) | Không dịch; ghi rõ | Trạng thái ghế vs. sản phẩm cấp ra. Cùng lần ghi atomic. R6 |
| 7 | `AwaitingPayment` (Booking) | `Unpaid` (Payment intent) | Không dịch; ghi rõ | Hai tên khác nhau có chủ ý (ADR-0010) |

---

## Ứng viên tách service

Thứ tự ứng viên và điều kiện tách: [`../design/architecture.md`](../design/architecture.md). Các hợp đồng chỉ đọc
và không cắt ranh giới atomic (R2, R7, R8) tách dễ nhất; R1, R3 không tách được mà không đổi logic.
