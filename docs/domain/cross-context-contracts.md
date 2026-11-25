# Cross-Context Contracts

Lớp 2 — Domain. Hợp đồng giữa các bounded context: dữ liệu gì đi qua, dưới hình thức nào, từ bên nào, và những gì các bên có thể giả định về nhau.

**Nguồn:** ticket `.scratch/advanced-ticket-system/issues/09-cross-context-contracts.md`.
**Bối cảnh:** [`context-map.md`](context-map.md) — 6 context, 8 quan hệ R1–R8.
**Thuật ngữ:** [`ubiquitous-language.md`](ubiquitous-language.md).

## Cách đọc

Mỗi hợp đồng ghi rõ:

1. **Dữ liệu đi theo hướng nào** — một chiều hay hai chiều.
2. **Hình thức** — gọi trực tiếp trong process qua interface, event, truy vấn đọc, hay ghi chung transaction.
3. **Bảng thuật ngữ** — cặp tên ở hai bên, và có phải cùng một khái niệm không. Nếu không, có cần Anti-Corruption Layer.
4. **Định nghĩa hợp đồng** — dữ liệu nào, ở mức khái niệm (không phải JSON schema).
5. **Điều khoản** — B được phép giả định gì về A, và A được phép thay đổi mà không phá B.

---

## Hợp đồng: `Scheduling` → `Inventory` (R1)

**Dữ liệu đi theo hướng**: `Scheduling` → `Inventory`.

**Hình thức**: Ghi chung transaction (cùng database, cùng transaction khi quản lý rạp mở bán).

### Dữ liệu

| Thuật ngữ ở Scheduling | Thuật ngữ ở Inventory | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Room seat` | `Showtime seat` | **KHÔNG** | `Room seat` = vị trí vật lý có hàng, số, loại. `Showtime seat` = bản ghi trạng thái bán. Khác aggregate, khác vòng đời |
| `Seat type` | (cùng tên) | Có | Tiêu chuẩn được định nghĩa ở `Scheduling`, dùng nguyên ở `Inventory` |
| `Ticket price` | (không lưu, chỉ dùng để tính toán) | Có | Giá gốc ở `Scheduling` được sinh thành `Showtime seat` với giá đó. Tùy từng suất chiếu × loại ghế |
| `Showtime` | (xác định suất chiếu tạo `Showtime seat`) | Có | Xác định đơn vị — suất chiếu nào sinh ra những `Showtime seat` nào |

### Hợp đồng

Khi quản lý rạp **mở bán** một suất chiếu:

- `Scheduling` cung cấp:
  - Danh sách `Room seat` của phòng (vị trí: hàng, số, loại)
  - Danh sách `Seat type` được sử dụng ở suất này
  - `Ticket price` cho mỗi cặp (`Seat type`, `Showtime`)
  - Danh sách ghế bị chặn (nếu có)

- `Inventory` nhận và sinh ra:
  - Một `Showtime seat` cho mỗi `Room seat` **không bị chặn**
  - Mỗi `Showtime seat` có trạng thái ban đầu `Available` và giá tương ứng

### Anti-Corruption Layer

**Phía `Inventory`** dịch `Room seat` → `Showtime seat`:
- `Inventory` không biết "phòng", "lịch chiếu", "suất chiếu" nghĩa là gì.
- Nó chỉ nhận: "Sinh ghế cho suất chiếu này theo cấu hình (vị trí, loại, bị chặn?)".
- Tất cả logic **vị trí trong phòng** nằm ở `Scheduling`. `Inventory` chỉ biết thứ tự (một số) và loại.

### Điều khoản

- **`Inventory` được giả định**: Danh sách `Seat type` không đổi trong suốt vòng đời suất chiếu. Giá `Ticket price` ở mỗi suất không thay đổi sau khi mở bán (đã chốt).
- **`Scheduling` được phép thay đổi mà không phá `Inventory`**: Thêm/sửa phim khác, lịch chiếu khác. Miễn là chưa mở bán suất chiếu đó — nếu đã mở, hành động đó không được phép (kiểm tra ở tầng `Scheduling`).
- **Không được phép đọc trực tiếp**: `Inventory` không được truy cập bảng `Room` hoặc `Movie` trực tiếp. Mọi thông tin phải qua hợp đồng này.

---

## Hợp đồng: `Inventory` → `Scheduling` (R2)

**Dữ liệu đi theo hướng**: `Inventory` → `Scheduling` (một chiều, chỉ đọc).

**Hình thức**: Gọi trực tiếp trong process qua interface của `Inventory` (ví dụ `IInventoryQueries.GetSeatAvailabilitySummary(showtimeId)`).

### Dữ liệu

| Thuật ngữ ở Inventory | Thuật ngữ ở Scheduling | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Available` / `Held` / `Sold` / `Blocked` | (các con số đếm) | Có | Trạng thái của `Showtime seat` được tích lũy thành bốn con số (`FR-23`) |

### Hợp đồng

`Scheduling` gọi `Inventory` để lấy **bốn con số tình trạng bán** của một suất chiếu:

```
{
  available_count: int,
  held_count: int,
  sold_count: int,
  blocked_count: int
}
```

Dữ liệu này được hiển thị ở **bộ điều khiển suất chiếu** cho quản lý rạp xem.

### Anti-Corruption Layer

**Không cần** — cả hai bên dùng chung số đếm, không có dịch nào.

### Điều khoản

- **`Scheduling` được giả định**: Bốn con số luôn cộng lại bằng tổng số ghế không bị chặn của phòng (công thức xác định ở `ubiquitous-language.md`).
- **`Inventory` được phép thay đổi mà không phá `Scheduling`**: Thay đổi logic tính toán bốn con số (ví dụ: include/exclude ghế bị chặn) miễn là công thức vẫn đúng.
- **Không được phép đọc trực tiếp**: `Scheduling` không được truy cập bảng `Showtime seat` trực tiếp. Mọi truy vấn phải qua interface `Inventory`.

---

## Hợp đồng: `Inventory` ↔ `Booking` (R3)

**Dữ liệu đi theo hướng**: Hai chiều, trong cùng một transaction (ghi chung).

**Hình thức**: Ghi chung transaction (cùng database, cùng atomic transaction).

### Dữ liệu

| Thuật ngữ ở Inventory | Thuật ngữ ở Booking | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Showtime seat` (trạng thái) | (đọc; không ghi lại) | Có | `Booking` đọc trạng thái của từng ghế được chọn |
| `Seat hold` (bản ghi + thời điểm hết hạn) | (đọc; không tạo riêng) | **KHÔNG** | `Seat hold` = bản ghi timeout, nằm ở `Inventory`. `Booking` = đơn, nằm ở `Booking`. Khác aggregate |
| (giữ ghế) | `Booking` (tạo đơn ở trạng thái `AwaitingPayment`) | (sự kiện) | Lệnh giữ n ghế phải ghi vào cả hai context cùng lúc |

### Hợp đồng

**Lệnh giữ n ghế** (`BR-02`, `INV-02`, `INV-05`):

```
Yêu cầu (từ Booking):
  booking_id: UUID
  showtime_id: UUID
  seat_selections: [
    { seat_id: UUID, seat_type: "Standard" | "Couple" | "VIP" },
    ...
  ]
  hold_expires_at: Timestamp
```

**Hành động cung cấp** (cả hai context):

1. `Inventory`: Cho mỗi `seat_id` trong yêu cầu:
   - Kiểm tra `Showtime seat` có trạng thái `Available` không → từ chối nếu không
   - Chuyển trạng thái sang `Held`
   - Tạo bản ghi `Seat hold` ghi thời điểm hết hạn

2. `Booking`: Tạo `Booking` với:
   - Trạng thái `AwaitingPayment`
   - Danh sách `Booking line` — mỗi ghế được chọn, cùng **giá đã chốt tại thời điểm này** (sao từ `Ticket price` của suất)
   - Tổng tiền = tổng `Booking line`

**Điều kiện tiên quyết:**
- Suất chiếu ở trạng thái `OnSale`
- Người dùng được phép đặt vé (qua `Identity & Access` — R8)
- Số ghế ≤ 6
- Số ghế toàn cộng người dùng **giữ** ở suất này (cộng thêm yêu cầu này) ≤ 10 (`BR-08` — kiểm tra tầng ứng dụng)

### Anti-Corruption Layer

**Phía `Booking`** dịch dữ liệu từ `Inventory`:
- `Booking` không biết "bản ghi `Seat hold`" của `Inventory` là gì, cấu trúc như nào.
- Nó chỉ biết: "Giữ những ghế này tới thời điểm này". Tất cả logic tạo bản ghi hold nằm ở `Inventory`.
- `Booking` chỉ lưu: "Những ghế mà đơn của tôi đã giữ" (dưới dạng `Booking line`), không lưu "thời điểm hết hạn" (nằm ở `Seat hold` của `Inventory`).

### Điều khoản

**Ranh giới bị cắt thứ 1** — lệnh giữ ghế phải atomic:
- Nếu `Inventory` ghi thành công nhưng `Booking` ghi thất bại → **hệ thống sai**: ghế bị khóa mà không có đơn.
- Nếu `Booking` ghi thành công nhưng `Inventory` ghi thất bại → **hệ thống sai**: đơn tồn tại nhưng ghế không bị khóa, người khác đặt được.
- Vì vậy: **một transaction**, không chia nhỏ.

**Ranh giới bị cắt thứ 3** — tác vụ giải phóng hết hạn phải atomic:
- Lệnh nền quét lô: với mỗi `Seat hold` đã quá hạn:
  - `Inventory`: Chuyển `Showtime seat` về `Available`, xóa bản ghi `Seat hold`
  - `Booking`: Chuyển `Booking` tương ứng sang `Expired`
- Nếu chỉ một phía thành công → **hệ thống sai**.
- **Một transaction** cho cả cặp.

**`Inventory` được giả định:**
- Giá `Ticket price` không thay đổi sau khi mở bán (đã chốt ở R1).
- `Booking line` = một ghế một dòng; không có nhóm ghế hoặc ưu đãi phức tạp.

**`Booking` được giả định:**
- Trạng thái `Showtime seat` được đọc lại từng lần giữ; không được lưu bộ đệm.
- Nếu trạng thái `Showtime seat` đổi (bị khóa bởi người khác) → từ chối ngay.

**Không được phép đọc trực tiếp:**
- `Booking` không được truy cập bảng `Showtime seat` hoặc `Seat hold` của `Inventory` trực tiếp.
- `Inventory` không được truy cập bảng `Booking` trực tiếp.

---

## Hợp đồng: `Booking` ↔ `Payment` (R4)

**Dữ liệu đi theo hướng**: Hai chiều (hỏi/đáp + thông báo).

**Hình thức**: 
- **Lệnh tạo intent**: Ghi chung transaction (cùng lúc).
- **Nhận kết quả thanh toán**: Ghi chung transaction (webhook đến → `Payment` xử lý → ghi vào `Booking` cùng transaction).

### Dữ liệu

| Thuật ngữ ở Booking | Thuật ngữ ở Payment | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Booking` (đơn) | `Payment intent` (yêu cầu thanh toán) | **KHÔNG** | `Booking` = đơn đặt vé + ghế được chọn + giá. `Payment intent` = yêu cầu thanh toán, chờ kết quả. Khác aggregate, khác vòng đời |
| `AwaitingPayment` (trạng thái Booking) | `AwaitingPayment` (trạng thái Payment intent) | **KHÔNG** | Cùng tên nhưng khác aggregate. `Booking.AwaitingPayment` = chờ tiền để cấp vé. `Payment intent.AwaitingPayment` = chờ Stripe phản hồi. **Ghi chú Q28** — xem mục "Câu hỏi còn mở" |
| `Paid` (trạng thái Booking) | `Captured` (trạng thái Payment intent) | Có | Khi thanh toán thành công, cả hai chuyển sang trạng thái "đã thu tiền" |
| (không có) | `Refund` (hoàn tiền) | (không lưu ở Booking) | Hoàn tiền được ghi ở `Payment` dưới dạng `Payment intent.Refunded`. `Booking` **không** có trạng thái `Refunded` (xem Q27 decision) |

### Hợp đồng

**Lệnh tạo intent** (khi người xem bấm "thanh toán"):

```
Yêu cầu (từ Booking):
  booking_id: UUID
  amount: Money (tổng tiền của Booking)
  currency: "VND"
```

**Hành động**:
1. `Payment`: Tạo `Payment intent` ở trạng thái `AwaitingPayment`
2. `Booking`: Nhận xác nhận intent tạo thành công (không đổi trạng thái Booking lúc này)

**Lệnh xử lý kết quả thanh toán** (khi webhook từ Stripe đến):

```
Thông báo (từ Payment):
  booking_id: UUID
  event_id: UUID (định danh webhook từ Stripe — idempotency key)
  result: "succeeded" | "failed"
  ...
```

**Hành động nếu `result = "succeeded"`**:
- `Payment`: Chuyển `Payment intent` sang `Captured` (đã thu tiền)
- `Booking`: Chuyển `Booking` sang `Paid` ⇒ **kích hoạt R6** (cấp vé)

**Hành động nếu `result = "failed"`**:
- `Payment`: Hủy `Payment intent` (không có trạng thái `Failed` — tác vụ dừng)
- `Booking`: Vẫn ở `AwaitingPayment` (người xem có thể thử lại)

### Anti-Corruption Layer

**Phía `Payment`** dịch dữ liệu từ Stripe (`payment_intent` → `Payment intent`):
- Xem R5 bên dưới.

**Phía `Booking`** dịch dữ liệu từ `Payment`:
- `Booking` không biết "bản ghi intent", "event webhook" của `Payment` là gì.
- Nó chỉ biết: "Thanh toán cho đơn này". Tất cả logic xử lý Stripe nằm ở `Payment`.

### Điều khoản

**Ranh giới bị cắt thứ 2** — cấp vé phải atomic:
- Xem R6 bên dưới (đây là nơi thực sự xảy ra).
- R4 tới R6 cùng một transaction.

**`Booking` được giả định:**
- `Payment intent.state` luôn trùng hoặc tiến bộ so với `Booking.state` — `Booking` không bao giờ chuyển sang `Paid` nếu `Payment intent` chưa `Captured`.

**`Payment` được giả định:**
- `Booking.amount` = tổng tiền chốt tại lúc tạo booking, không đổi.
- Một `Booking` tối đa gắn với **một** `Payment intent` chính (không có retry intent khác).

**Không được phép đọc trực tiếp:**
- `Booking` không được truy cập bảng `Payment intent` hoặc `Payment event` trực tiếp.
- `Payment` không được truy cập bảng `Booking` trực tiếp.

**Ghi chú Q27 — Trạng thái hoàn tiền của Booking:**
- Khi quản lý rạp hủy suất chiếu, `Booking.Paid` được hoàn tiền tự động (xem Ranh giới thứ 4).
- `Booking` **không** đổi sang `Refunded` — nó giữ trạng thái `Paid`.
- Dấu vết hoàn tiền nằm ở `Payment intent.Refunded` (R4 → R5).
- Lý do: `Booking` đã kết thúc chu kỳ chính (giữ → thanh toán → cấp vé). Hoàn tiền là sự kiện ngoại lai do quản lý rạp hành động.

---

## Hợp đồng: `Payment` → `Stripe` (R5)

**Dữ liệu đi theo hướng**: Hai chiều (gọi API + webhook callback).

**Hình thức**: Gọi API bên ngoài + webhook từ Stripe.

### Dữ liệu

| Thuật ngữ ở Payment | Thuật ngữ ở Stripe | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Payment intent` | `payment_intent` | **KHÔNG** | `Payment intent` = abstraction của ta. `payment_intent` = Stripe API object. Khác mô hình |
| `Captured` | `payment_intent.status = "succeeded"` | Có | Thanh toán thành công |
| `AwaitingPayment` | `payment_intent.status` ∈ `{"requires_payment_method", "requires_confirmation", "processing"}` | Có (gần đúng) | Chờ kết quả, nhưng Stripe có nhiều trạng thái hơn ta cần |
| `Refunded` | `charge.refunded = true` | Có | Hoàn tiền — Stripe cho phép partial refund, ta chỉ làm full refund |
| `amount`, `currency` | `amount`, `currency` | Có | Trực tiếp dùng |
| `Refund` | `Charge.refunds[]` | Có | Mỗi hoàn tiền là một dòng |

### Hợp đồng

**Lệnh khởi tạo thanh toán**:

```
API call: POST /v1/payment_intents
Dữ liệu:
  amount: int (cent)
  currency: "vnd"
  metadata: { booking_id: UUID }
```

**Hành động `Payment`**:
- Lưu `payment_intent.id` (Stripe ID) vào `Payment intent.external_id`
- Trạng thái = `AwaitingPayment`

**Webhook từ Stripe** (charge.succeeded / charge.refunded / ...):

```
POST /webhooks/stripe
Dữ liệu:
  event_id: UUID (định danh Stripe — xử lý đúng một lần)
  event_type: "charge.succeeded" | "charge.refunded" | ...
  data: { charge, payment_intent, ... }
```

**Hành động `Payment`**:
- Nếu `event_type = "charge.succeeded"` ⇒ `Payment intent.Captured` ⇒ webhook đến `Booking` (R4)
- Nếu `event_type = "charge.refunded"` ⇒ `Payment intent.Refunded` (không làm gì thêm trong context này)

### Anti-Corruption Layer

**Phía `Payment`** dịch Stripe → Internal:

| Stripe nói | Ta nói | Cách dịch |
|---|---|---|
| `payment_intent` | `Payment intent` | Lưu `payment_intent.id` như `external_id`; abstract lại trạng thái phức tạp thành `AwaitingPayment`, `Captured`, `Refunded` |
| `amount`, `currency` | `Money(amount, currency)` | Trực tiếp |
| `charge.succeeded` | `Captured` | Lắng nghe webhook |
| `charge.refunded` (có thể partial) | `Refunded` (luôn full) | Nếu `refunded_amount ≠ amount_captured` → lỗi logic (theo `FR-39` luôn full refund) |
| không có | `Expired` | Ta có trạng thái "hết hạn" do hold timeout; Stripe không biết |
| không có | "Hủy do quản lý rạp" | Ta quyết định hoàn tiền; Stripe chỉ nhận lệnh hoàn, không biết lý do |

Lớp dịch **nằm ở `Payment`**. `Booking` không trực tiếp gọi Stripe.

### Điều khoản

**Idempotency**:
- Mỗi webhook từ Stripe được định danh bằng `event_id` duy nhất → xử lý đúng một lần (`FR-38`).
- Nếu webhook đến lặp → lần thứ hai tìm thấy `Payment event` với `event_id` đó rồi → trả về kết quả cũ, không xử lý lại.

**`Payment` được giả định:**
- Stripe API sẽ gửi webhook ít nhất một lần (not guaranteed immediately).
- `payment_intent.id` là định danh không đổi.

**Không được phép:**
- `Booking` không được call Stripe API trực tiếp.
- `Booking` không được parse webhook từ Stripe — `Payment` parse rồi dịch sang `PaymentEvent` gửi về.

---

## Hợp đồng: `Booking` → `Ticketing` (R6)

**Dữ liệu đi theo hướng**: Hai chiều (yêu cầu cấp vé + xác nhận).

**Hình thức**: Ghi chung transaction (cùng lúc khi `Booking` chuyển sang `Paid`).

### Dữ liệu

| Thuật ngữ ở Booking | Thuật ngữ ở Ticketing | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Booking line` (ghế được chọn) | `Showtime seat` (xác định vé nào) | (sự kiện) | `Booking line` cập nhật danh sách `Showtime seat` cần cấp vé |
| `Paid` (trạng thái Booking) | (không có trạng thái tương ứng) | (sự kiện) | Lệnh cấp vé được kích hoạt khi `Booking` chuyển `Paid` |
| `Ticket` (mã QR) | `Ticket` | Có | Mã QR được tạo ở `Ticketing`, gắn với từng `Showtime seat` |
| **"Đã cấp vé"** (cờ + số lượng vé, thuộc `Booking`) | `Ticket` (đã tồn tại) | **KHÔNG** | "Đã cấp vé" là **trạng thái của đơn**; `Ticket` là **sản phẩm đã sinh ra**. Đơn `Paid` mà chưa cấp vé ≠ đơn `Paid` đã cấp vé. Không phải cùng một thứ — xem Q26 |

### Hợp đồng

**Lệnh cấp vé** (được kích hoạt khi `Booking.AwaitingPayment → Booking.Paid`):

```
Yêu cầu (từ Booking):
  booking_id: UUID
  showtime_id: UUID
  seats: [
    { showtime_seat_id: UUID, seat_type: "Standard" | "Couple" | "VIP" },
    ...
  ]
```

**Hành động `Ticketing`**:
- Cho mỗi `showtime_seat_id`:
  - Kiểm tra có `Ticket` tồn tại chưa → từ chối nếu có (ràng buộc duy nhất trên `showtime_seat_id`)
  - Tạo `Ticket` mới (mã QR, trạng thái `Issued`)
- Trả về danh sách `Ticket` đã tạo

**Hành động `Booking`**:
- Nhận danh sách `Ticket` → ghi nhận "đơn đã cấp vé"
- Nếu tất cả `Ticket` cấp thành công → kết thúc (không đổi trạng thái Booking nữa, vẫn `Paid`)
- Nếu một số thất bại → từ chối cấp vé (rollback lệnh tạo ticket được tạo)

### Anti-Corruption Layer

**Không cần** — cả hai bên dùng chung khái niệm vé.

### Điều khoản

**Ranh giới bị cắt thứ 2 (tiếp theo R4)** — cấp vé phải atomic:
- Lệnh tạo `Payment intent` → webhook thành công → cấp vé → đánh dấu "đơn đã cấp vé" — **một transaction**.
- Nếu cấp vé thành công nhưng không đánh dấu → webhook đến lại → cấp vé lần hai (**vi phạm `FR-38`**, `SC-04`).
- Nếu không cấp được nhưng vẫn đánh dấu → người xem thấy "đã cấp" nhưng không có vé.
- **Một transaction** cho cả cặp.

**Idempotency**:
- Ràng buộc duy nhất trên `showtime_seat_id` — một ghế không thể có hai vé.
- Lần thứ hai cấp vé cho cùng ghế → thất bại (ràng buộc vi phạm).

**`Booking` được giả định:**
- `Showtime seat` tồn tại và trạng thái hợp lệ (chưa bị hoàn tiền, chưa được khóa).
- Giá vé của `Showtime seat` không đổi sau khi tạo booking (đã chốt ở R1).

**`Ticketing` được giả định:**
- Mỗi `Showtime seat` chỉ được cấp vé **một lần** (ràng buộc duy nhất).
- Webhook lặp lại từ `Payment` không được phép lừa được tạo vé lần hai.

**Ghi chú Q26 — "đã cấp vé" là cờ hay là trạng thái riêng:**
- Hiện tại: cờ + số lượng vé **bên trong** trạng thái `Paid` (`INV-04` của `Booking`). Không có
  trạng thái `TicketsIssued` riêng.
- Vì sao điều này quan trọng cho hợp đồng: nếu `Booking` có trạng thái riêng, thì R6 sẽ có
  **hai** transition đi từ `AwaitingPayment` (`→ Paid` và `→ TicketsIssued`), và ranh giới bị cắt
  #2 sẽ phải phát biểu lại. Với cờ, ranh giới này là **một** transition duy nhất.
- Chốt ở T11 (đặt tên trạng thái + data model). Xem `docs/domain/state-machines.md`.

**Không được phép đọc trực tiếp:**
- `Booking` không được truy cập bảng `Ticket` trực tiếp.
- `Ticketing` không được truy cập bảng `Booking line` trực tiếp.

---

## Hợp đồng: `Scheduling` → `Ticketing` (R7)

**Dữ liệu đi theo hướng**: `Scheduling` → `Ticketing` (một chiều, chỉ đọc).

**Hình thức**: Gọi trực tiếp trong process qua interface của `Scheduling` (ví dụ `ISchedulingQueries.GetShowtime(showtimeId)` và `ISchedulingQueries.GetGateAssignment(showtimeId)`).

### Dữ liệu

| Thuật ngữ ở Scheduling | Thuật ngữ ở Ticketing | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Showtime` (lịch chiếu) | (xác định vé nào thuộc suất nào) | Có | Vé gắn với `Showtime` để kiểm tra (`FR-25`) |
| `Gate assignment` (phân công cổng) | (thông tin cổng để hiển thị vé) | Có | Suất chiếu được phân công cho cổng nào — hiển thị trên vé (`FR-35`) |

### Hợp đồng

`Ticketing` gọi `Scheduling` để:

1. **Kiểm tra vé thuộc suất chiếu**: Cho trước `ticket_id` → tìm `showtime_seat_id` → tìm suất chiếu → kiểm tra suất có hợp lệ không (không bị hủy, v.v. — `FR-25`).

2. **Lấy thông tin hiển thị vé**: Cho trước `showtime_id` → lấy:
   - Thông tin phim (tên, rating)
   - Thời gian chiếu
   - Phòng chiếu
   - Cổng (gate) gán cho suất này (`FR-35`)

### Anti-Corruption Layer

**Không cần** — cả hai bên dùng chung khái niệm suất chiếu.

### Điều khoản

**`Ticketing` được giả định:**
- Suất chiếu không đổi sau khi vé đã cấp (không được sửa thời gian, phòng, cổng).
- Trạng thái suất chiếu (`OnSale`, `Cancelled`) có thể đổi — `Ticketing` phải kiểm tra lại khi check-in.

**`Scheduling` được phép thay đổi mà không phá `Ticketing`:**
- Thêm/xóa phim khác.
- Thêm/xóa suất chiếu chưa cấp vé.
- **Không được phép**: Đổi thời gian/phòng suất chiếu đã cấp vé (kiểm tra ở `Scheduling` trước khi cho phép).

**Không được phép đọc trực tiếp:**
- `Ticketing` không được truy cập bảng `Movie`, `Room`, `Showtime` của `Scheduling` trực tiếp.
- Mọi thông tin phải qua interface `Scheduling`.

---

## Hợp đồng: `Identity & Access` → `Scheduling` / `Booking` / `Ticketing` (R8)

**Dữ liệu đi theo hướng**: `Identity & Access` → các context (một chiều, chỉ đọc).

**Hình thức**: Gọi trực tiếp trong process qua interface của `Identity & Access` (ví dụ `IIdentityQueries.GetUserRoles(userId)`, `IIdentityQueries.CanManageShowtime(userId, showtimeId)`).

### Dữ liệu

| Thuật ngữ ở Identity & Access | Thuật ngữ ở Scheduling / Booking / Ticketing | Cùng nghĩa? | Ghi chú |
|---|---|---|---|
| `Customer` | (người xem) | Có | Người dùng có vai trò `Customer` |
| `Theater manager` | (quản lý rạp) | Có | Người dùng có vai trò `Theater manager` cho rạp cụ thể |
| `Gate staff` | (nhân viên cổng) | Có | Người dùng có vai trò `Gate staff` |
| `Administrator` | (quản trị viên) | Có | Người dùng có vai trò `Administrator` |

### Hợp đồng

Các context gọi `Identity & Access` để **kiểm tra quyền**:

1. **`Booking` kiểm tra**: Người xem có được phép đặt vé không?
   - Mặc định: `Customer` được phép.

2. **`Scheduling` kiểm tra**: Quản lý rạp có được phép sửa lịch chiếu không?
   - `Theater manager` được phép sửa lịch chiếu của **rạp của chính họ**.
   - `Administrator` được phép sửa lịch chiếu **bất kỳ rạp nào**.

3. **`Ticketing` kiểm tra**: Nhân viên cổng có được phép check-in vé không?
   - `Gate staff` được phép check-in.
   - `Administrator` được phép check-in.

### Anti-Corruption Layer

**Không cần** — vai trò là khái niệm chung.

### Điều khoản

**Quyết định thiết kế**:
- Kiểm tra quyền **không** nằm trong hợp đồng dữ liệu (không phải anti-corruption).
- Nó là **kiểm tra logic**: mỗi action yêu cầu role nhất định.
- Không có state machine hay idempotency phức tạp.

**`Scheduling` / `Booking` / `Ticketing` được giả định:**
- Nếu kiểm tra quyền trả về `false` → từ chối hành động ngay, không thực hiện.
- Danh sách vai trò không đổi (tức là vai trò được định nghĩa cứng: `Customer`, `Theater manager`, `Gate staff`, `Administrator`).

**`Identity & Access` được phép thay đổi mà không phá các context khác:**
- Thêm người dùng mới, gán vai trò.
- Lưu ý: Nếu xóa vai trò hiện tại hoặc đổi định nghĩa vai trò → có thể phá các context (ví dụ: xóa vai trò `Customer` → không ai được phép đặt vé). Nên tránh.

**Không được phép:**
- Các context không được lưu bộ đệm kết quả kiểm tra quyền (phải gọi lại mỗi request).
- Các context không được tự quyết định vai trò của người dùng; phải hỏi `Identity & Access`.

---

## Ranh giới bị cắt — bốn chỗ cần lưu ý

Bốn nơi một bất biến **bị cắt ngang ranh giới context**, phải ghi chung transaction trong modular monolith:

| # | Bất biến | Contracts liên quan | Transition / Hành động | Hậu quả nếu tách service |
|---|---|---|---|---|
| **1** | "Giữ n ghế: đủ n hoặc không ghế nào" + "ghế `Held` luôn có thời điểm hết hạn" | R1, R3 | Lệnh "giữ n ghế" phải ghi cả `Inventory` (chuyển `Showtime seat` → `Held`) và `Booking` (tạo đơn) cùng lúc | Lệnh giữ ghế phải trở thành saga: giữ ở `Inventory` → xác nhận ở `Booking`. Có cửa sổ race nếu giữ xong nhưng booking thất bại |
| **2** | "Cấp vé đúng một lần" | R4, R6 | Lệnh "cấp vé" phải atomic với "đánh dấu đơn đã cấp vé" (nằm trong `Booking.Paid` state → không có trạng thái `TicketsIssued` riêng) | Cần outbox ở `Ticketing` + consumer idempotent ở `Booking`. Nếu tạo ticket thành công nhưng consumer thất bại → ticket mồ côi không có booking |
| **3** | "Hết hạn: giải phóng ghế + chuyển đơn sang `Expired` cùng lúc" | R3 (reverse) | Tác vụ nền quét lô: với mỗi `Seat hold` đã quá hạn, chuyển `Showtime seat` → `Available` **và** `Booking` → `Expired` | Phải compensating: giải phóng ghế xong nhưng đơn chưa chuyển → người xem vẫn thấy giữ được ghế |
| **4** | "Hủy suất chiếu: vô hiệu vé + hoàn tiền mọi đơn liên quan cùng lúc, đúng một lần" | R1, R6, R4 (cascade) | Lệnh "hủy suất chiếu" kích hoạt: vô hiệu tất cả `Ticket` của suất (thực tế: `Showtime seat` chuyển trạng thái) + hoàn tiền mọi `Booking.Paid` của suất | Phải outbox ở `Scheduling` + consumer chạy N iteration (một per `Booking.Paid`), mỗi iteration hoàn tiền một đơn. Nếu hoàn được 50% rồi hệ thống down → phải xử lý tiếp. Rủi ro lớn nhất |

**Quyết định thiết kế T09:**
- Tất cả bốn ranh giới được ghi **rõ ràng tường minh** ở đây và ở `state-machines.md`.
- Modular monolith hiện tại: **một transaction** mỗi ranh giới.
- T10 (kiến trúc) và T12 (concurrency) phải nhắc lại: nếu tách service, **không được "tối ưu" bằng cách chia transaction** mà không thay đổi logic.

---

## Các cặp thuật ngữ khác nghĩa (cần Anti-Corruption Layer)

| Cặp | Bên A | Bên B | Nơi dịch | Giải thích |
|---|---|---|---|---|
| 1 | `Room seat` (Scheduling) | `Showtime seat` (Inventory) | Phía `Inventory` | `Room seat` = vị trí vật lý (hàng, số, loại). `Showtime seat` = bản ghi trạng thái bán. Khác aggregate, khác vòng đời. R1 |
| 2 | `Cancelled` (Booking — user-initiated) | `Cancelled` (Showtime — manager-initiated) | Không dịch; ghi rõ trong hợp đồng | Khác nguyên nhân, khác hậu quả. R3, R1 (hủy suất là ranh giới bị cắt thứ 4) |
| 3 | `Ticket price` gốc (mutable, Scheduling) | `Ticket price` frozen (immutable, Booking line) | Phía `Booking` | Giá gốc ở `Scheduling` được "chụp" khi giữ ghế, lưu ở `Booking line`. Giá gốc có thể đổi sau, không ảnh hưởng đơn cũ. R1, R3 |
| 4 | `Seat hold` (Inventory — record) | `Booking` (Booking — order) | Không dịch; ghi rõ trong hợp đồng | Khác aggregate. `Seat hold` = timeout record. `Booking` = business entity (đơn). Cùng transaction, không thể tách. R3 |
| 5 | `Payment intent` (Payment — abstraction) | `payment_intent` (Stripe — API object) | Phía `Payment` | Stripe API phức tạp hơn (nhiều trạng thái, partial refund, v.v.). `Payment` abstract lại thành trạng thái đơn giản. R5 |
| 6 | `Sold` (Inventory — seat state) | `Ticket` (Ticketing — revenue record) | Không dịch; ghi rõ trong hợp đồng | "Sold" = điều kiện tiên quyết. "Ticket" = sản phẩm. Không phải cùng nghĩa. R6 |
| 7 | `AwaitingPayment` (Booking) | `AwaitingPayment` (Payment intent) | Không dịch; **ghi chú Q28** | Cùng tên nhưng khác aggregate. Có thể gây nhầm khi đọc log. Giải pháp: thay đổi tên một bên ở T11 |
| 8 | "Đã cấp vé" (cờ trong `Booking`) | `Ticket` (sản phẩm ở `Ticketing`) | Không dịch; **ghi chú Q26** | "Đã cấp vé" là **trạng thái của đơn**; `Ticket` là **sản phẩm đã sinh ra**. Đơn `Paid` chưa cấp vé ≠ đơn `Paid` đã cấp vé. R6 |

---

## Ứng viên tách service (defer to T10)

Phân tích **độ sẵn sàng** của mỗi hợp đồng để tách thành service riêng. Tiêu chí:

- **Một chiều?** (đơn chiều tốt hơn hai chiều)
- **Dòng dữ liệu rõ ràng?** (cầu gọi rõ, không lộn xộn)
- **Có cắt ranh giới transaction không?** (cắt → phức tạp, cần saga/outbox)
- **Idempotency rõ ràng?** (nếu rõ → an toàn hơn)

### Điểm số sơ bộ

| Hợp đồng | Một chiều? | Dòng rõ? | Cắt tx? | Idempotent? | Ghi chú | Ứng viên? |
|---|---|---|---|---|---|---|
| **R1** `Scheduling → Inventory` | Có | Rõ | Có (R1 + R3 cắt chung) | — | Mở bán ⇒ sinh `Showtime seat`. Nhưng R3 ràng buộc R1 (nếu tách R1 thành service → R3 vỡ) | ❌ Không (phụ thuộc R3) |
| **R2** `Inventory → Scheduling` | Có | Rõ (4 con số) | Không | — | Đọc tình trạng bán. Không ghi, không cắt tx. **Ứng viên hàng đầu** | ✅ **Cao** |
| **R3** `Inventory ↔ Booking` | Hai chiều | Rõ | **Có (ranh giới bị cắt #1)** | — | Giữ n ghế atomic. Nếu tách → saga phức tạp, có cửa sổ race | ❌ Không (cắt tx) |
| **R4** `Booking ↔ Payment` | Hai chiều | Rõ | **Có (ranh giới bị cắt #2 kèm R6)** | — | Tạo intent + nhận webhook. Nếu tách → outbox complex, cập nhật `Booking` từ webhook | ⚠️ Trung bình |
| **R5** `Payment → Stripe` | Một chiều (ra ngoài) | Rõ | Không | Có (webhook event idempotent) | Tách riêng API Stripe. ACL rõ ràng. Ngoài hệ thống nên coi như đã "tách" | ✅ **Cao** (đã tách conceptually) |
| **R6** `Booking → Ticketing` | Hai chiều | Rõ | **Có (ranh giới bị cắt #2 kèm R4)** | Có (ràng buộc duy nhất) | Cấp vé atomic với "đơn đã cấp vé". Nếu tách → outbox + idempotent consumer | ⚠️ Trung bình |
| **R7** `Scheduling → Ticketing` | Một chiều | Rõ (kiểm tra vé, lấy gate) | Không | — | Đọc lịch chiếu + phân công cổng. Không ghi. | ✅ **Cao** |
| **R8** `Identity & Access → tất cả` | Một chiều | Rõ (kiểm tra quyền) | Không | — | Mỗi context gọi kiểm tra quyền. Có thể là service riêng từ đầu (ví dụ OAuth provider) | ✅ **Cao** |

### Ứng viên hàng đầu (trình bày chi tiết ở T10)

1. **R2** — Đọc-duy-nhất, không cắt transaction, dòng dữ liệu đơn giản (4 con số). Có thể tách trước khi tách bất cứ cái nào khác.
2. **R7** — Đọc-duy-nhất, không cắt transaction. Lấy metadata suất chiếu + gate assignment.
3. **R8** — Kiểm tra quyền; đã được thiết kế như service (có thể là OAuth / external auth service).
4. **R5** — Đã là external API (Stripe); conceptually tách rồi.

### Những cái **không thể** tách mà không redesign

- **R1** ← phụ thuộc vào R3; nếu tách riêng → R3 vỡ
- **R3** ← **ranh giới bị cắt #1**; tách = saga + race condition
- **R4** + **R6** ← **ranh giới bị cắt #2 + #4**; tách = outbox + saga phức tạp

---

## Câu hỏi còn mở

| # | Câu hỏi | Liên quan | Trạng thái |
|---|---|---|---|
| **Q27** | Khi hủy suất chiếu, `Booking.Paid` có cần chuyển sang trạng thái `Refunded` không, hay thuộc tính hoàn tiền ở `Payment` là đủ? | R1, R4 (ranh giới bị cắt #4) | ✅ **Đã quyết**: `Booking` giữ `Paid`, không có `Refunded`. Dấu vết hoàn tiền ở `Payment intent.Refunded` |
| **Q28** | `AwaitingPayment` được dùng ở cả `Booking` và `Payment intent` — cùng tên nhưng khác aggregate. Có nên đổi tên? | R4 | ⏳ Defer to T11 (chốt tên trạng thái) |
| **Q26** | `Booking` có cần trạng thái **"đã cấp vé"** riêng, hay cờ + số lượng vé bên trong trạng thái `Paid`? | R6 (ranh giới bị cắt #2) | ⏳ Defer to T11. Hiện là cờ bên trong `Paid` (`INV-04`) — xem ghi chú trong hợp đồng R6 |
| **Q20** | Bốn "ranh giới bị cắt" có cần chuyển sang saga/outbox khi tách service không? | R1, R3, R4, R6 | ⏳ Chốt ở T10 (kiến trúc) |

---

## Trạng thái

Bản làm việc. T09 vừa hoàn thành. Tiếp theo: T10 — kiến trúc module.