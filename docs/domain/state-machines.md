# State machines

Lớp 2 — Domain. Vòng đời của các thứ có trạng thái, và những gì **không** được phép xảy ra.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/08-state-machines.md`.
Thuật ngữ: [`ubiquitous-language.md`](ubiquitous-language.md).
Ranh giới aggregate và invariant: [`aggregates/`](aggregates/).
Sửa lớn 2026-11-25 sau review: [ADR-0005](../adr/0005-showtime-seat-aggregate.md),
[ADR-0006](../adr/0006-payment-expiry-refund-flow.md),
[ADR-0007](../adr/0007-per-customer-showtime-quota.md).

## Cách đọc

Một state machine ở đây là **hành vi đúng** được vẽ ra **một lần**. Mọi nơi khác trong hệ
thống tuân theo bản vẽ này.

| Phần | Trả lời câu hỏi |
|---|---|
| **States** | Đơn vị này có những trạng thái nào? |
| **Transitions** | Sự kiện nào đưa nó từ trạng thái này sang trạng thái kia, và **tác dụng phụ** là gì? |
| **Transitions bị từ chối** | Sự kiện nào đến sai lúc, đến lặp, đến sai thứ tự — và **kết quả mong đợi** là gì? |
| **Điều kiện tiên quyết** | Trước khi chuyển, cái gì phải đúng? |

> **Bảng "Transitions bị từ chối" là phần quan trọng nhất.** Nó định nghĩa **idempotency** và
> **không hồi sinh**. Một transition bị từ chối **không phải** là lỗi: nó là một kết quả hợp lệ,
> và thường phải trả về cho người dùng một thông điệp cụ thể (`FR-36`).

### Quy ước

- Tên trạng thái dùng **đúng** tên tiếng Anh trong [`ubiquitous-language.md`](ubiquitous-language.md).
  `Pending` và `Reserved` bị cấm.
- Trạng thái **chết** (không có đường ra) được đánh dấu rõ.
- **"Cùng transaction" chỉ áp dụng cho ghi trong database.** Không transition nào gọi hệ thống
  bên ngoài (Stripe, email) bên trong DB transaction — những việc đó đi qua **outbox** (ADR-0006).

### Ba loại transition

| Loại | Nghĩa | Ví dụ |
|---|---|---|
| **Chuyển trạng thái thuần** | Chỉ đổi trạng thái của đơn vị đó | `Blocked → Available` |
| **Chuyển kèm tác dụng phụ** | Đổi trạng thái **và** ghi ra ngoài ranh giới aggregate | `AwaitingPayment → Paid` (cấp vé, ghi lịch sử) |
| **Chuyển cắt qua ranh giới context** | Đổi trạng thái ở **nhiều context**, cùng một DB transaction | Giữ ghế (`Inventory` + `Booking`) |

> **Ranh giới bị cắt** — năm chỗ, xem bảng ở cuối file.

---

## State machine: `Showtime seat`

**Đơn vị**: một ghế của một suất chiếu — đơn vị bán thực sự.
Context: `Inventory`. Aggregate: **`Showtime seat`** (aggregate root riêng — ADR-0005).

### States

| Trạng thái | Nghĩa |
|---|---|
| `Available` | Chưa ai dùng. Người xem có thể giữ. |
| `Held` | Đang được giữ, kèm `hold_id` và `held_until`. Chưa bán, chưa có vé. |
| `Sold` | Đã bán. Đã thanh toán, đã có vé. **Trạng thái chết.** |
| `Blocked` | Không bán online (vệ sinh, dành riêng). Vẫn tính vào tổng số ghế phòng. |

Bốn trạng thái **loại trừ nhau và đầy đủ**.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Tạo suất chiếu | `Available` | Sinh **toàn bộ** ghế của suất theo sơ đồ phòng, all-or-nothing, **cùng transaction** với tạo `Showtime` (R1). |
| `Available` | `Hold seats` | `Held` | Ghi `hold_id` (= id đơn) và `held_until` (= `expires_at` của đơn). **Cùng transaction** với tạo `Booking` ở `AwaitingPayment`. Tất cả n ghế trong **một** transaction — all-or-nothing. |
| `Held` | Thanh toán thành công (đơn còn hạn) | `Sold` | Xoá thông tin giữ. **Cùng transaction** với: đơn → `Paid`, intent → `Captured`, **cấp vé** (mỗi ghế một vé). |
| `Held` | Đơn hết hạn | `Available` | Xoá thông tin giữ. Chỉ trả ghế có `hold_id` = id đơn đó. **Cùng transaction** với đơn → `Expired`. |
| `Held` | Đơn bị huỷ (người xem huỷ, hoặc do hủy suất chiếu) | `Available` | Như trên. **Cùng transaction** với đơn → `Cancelled`. |
| `Available` | Quản lý rạp (chủ suất) chặn ghế | `Blocked` | Không. Trạng thái `Blocked` chỉ sống ở `Inventory`. |
| `Blocked` | Quản lý rạp (chủ suất) gỡ chặn | `Available` | Không. |

**Không có transition nào khác.** Đặc biệt: **không có** `Sold → Available`. Khi suất chiếu bị hủy,
ghế `Sold` **giữ nguyên** `Sold` — suất đã chết, không ai đặt lại được; vé bị vô hiệu bằng điều
kiện tiên quyết của check-in (xem máy `Ticket`), không bằng đổi trạng thái ghế.

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `Held` hoặc `Sold` | Chặn ghế | **Từ chối**, kèm lý do: ghế đang được giữ hoặc đã bán. |
| `Sold` | Bất kỳ sự kiện trả ghế nào | **Từ chối** — ghế đã bán không trở lại `Available`. |
| Khác `Available` | `Hold seats` | **Từ chối**, nói rõ **ghế nào** đã có người (`FR-01`, `FR-15`). Một ghế không đạt ⇒ **toàn bộ** thất bại (`FR-02`). |
| `Held` bởi lần giữ **khác** | Trả ghế theo `hold_id` cũ | **Không làm gì** (0 dòng ảnh hưởng) — ghế đã sang tay người khác. |
| `Available` | `Hold seats` khi suất **chưa mở bán** hoặc **đã tới giờ chiếu** | Từ chối ở lệnh giữ ghế (`FR-34`, `BR-13`, `BR-31`). |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| `Held → Sold` | **Có** — no-op | Điều kiện lệnh `WHERE hold_id = @id AND status = 'Held'`. Lần hai ảnh hưởng 0 dòng. |
| `Held → Available` | **Có** — no-op | Cùng điều kiện theo `hold_id`. Worker retry hay huỷ gọi lại đều an toàn, và **không** trả nhầm ghế đã sang tay. |
| `Available ↔ Blocked` | **Có** | Điều kiện lệnh theo trạng thái nguồn. |
| `Hold seats` | **Có** ở tầng yêu cầu | Bản ghi idempotency của `Booking` (`INV-08`). Ở tầng ghế, thất bại là do ghế không còn `Available`, không phải do lặp. |

> **Điều kiện lệnh đủ cho tính đúng đắn, nhưng không đủ để chống deadlock.** Hai webhook cùng lúc
> ⇒ chỉ một lệnh ảnh hưởng 1 dòng — không có cửa sổ TOCTOU. Nhưng lệnh `UPDATE` vẫn lấy row lock;
> thứ chống deadlock là **thứ tự khoá** (bên dưới).

### Điều kiện tiên quyết

**`Hold seats`** — tất cả trong **một** DB transaction, theo thứ tự cố định:

1. Suất chiếu `OnSale` **và** chưa tới giờ bắt đầu (`BR-13`, `BR-31`) — đọc từ `Scheduling` (R9).
2. Số ghế theo quy tắc đếm (ghế đôi tính 2) **≤ 6** (`BR-06`, `BR-07`). Loại ghế lấy từ dữ liệu
   ghế phía server, **không** từ yêu cầu client.
3. **Khoá quota `(customer, showtime)`**, rồi đếm ghế của người đó cho suất đó (đơn
   `AwaitingPayment` còn hạn + `Paid`) — tổng sau khi giữ **≤ 10** (`BR-08`, ADR-0007).
4. Khoá và chuyển các dòng ghế theo **thứ tự tăng dần của id ghế**, điều kiện `status = 'Available'`.
   Số dòng ảnh hưởng < n ⇒ rollback toàn bộ (`BR-02`, `BR-22`).

**Thanh toán thành công → `Sold`**: ghế `Held` với `hold_id` = id đơn; đơn `AwaitingPayment` và
`expires_at > now()`.

**Trả ghế (hết hạn / huỷ)**: ghế `Held` với `hold_id` = id đơn.

---

## State machine: `Booking`

**Đơn vị**: một lần đặt vé của một người xem.
Context: `Booking`. Aggregate: `Booking`.

### States

| Trạng thái | Nghĩa |
|---|---|
| `AwaitingPayment` | Đã giữ ghế, chờ thanh toán. Có `expires_at` (15 phút kể từ lúc giữ). |
| `Paid` | Đã thanh toán thành công **trước** `expires_at`. Vé đã được cấp. **Trạng thái chết.** |
| `Expired` | Quá `expires_at` mà chưa thanh toán. Ghế trả về, không có vé, **không hồi sinh**. **Trạng thái chết.** |
| `Cancelled` | Bị huỷ khi chưa thanh toán — do người xem, hoặc do suất chiếu bị hủy. Ghế trả về, không có vé. **Trạng thái chết.** |

> **Hết hạn được định nghĩa bằng thời gian (ADR-0006).** Một đơn mà `now() ≥ expires_at` thì về
> nghiệp vụ **đã hết hạn**, dù worker chưa kịp chuyển nó sang `Expired`. Transition `→ Paid` có
> điều kiện `expires_at > now()` để không bao giờ thắng được trong khoảng đó.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | `Hold seats` | `AwaitingPayment` | Tạo `Booking` với `Booking line`, giá đã chốt, `expires_at`. n ghế → `Held`. **Cùng transaction**. Ghi lịch sử. |
| `AwaitingPayment` | Thanh toán thành công, `expires_at > now()` | `Paid` | Ghế → `Sold`, intent → `Captured`, **cấp vé** (mỗi ghế một vé). **Cùng transaction**. Ghi lịch sử. |
| `AwaitingPayment` | `expires_at` đã qua (worker) | `Expired` | Ghế → `Available`. **Cùng transaction**. Ghi lịch sử. Yêu cầu huỷ intent phía Stripe qua outbox (nếu có intent). |
| `AwaitingPayment` | Người xem huỷ | `Cancelled` | Ghế → `Available`. **Cùng transaction**. Ghi lịch sử (nguyên nhân: người xem). Huỷ intent phía Stripe qua outbox. |
| `AwaitingPayment` | Suất chiếu bị hủy | `Cancelled` | Như trên, nguyên nhân: hủy suất chiếu. **Cùng transaction** với `Showtime → Cancelled`. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `AwaitingPayment` nhưng `expires_at` đã qua | Thanh toán thành công | **Từ chối** — không cấp vé. Tiền đã thu ⇒ **hoàn toàn bộ** (`BR-04`, `BR-05`, `FR-06`, `FR-39`). |
| `Expired` | Thanh toán thành công đến sau | **Từ chối** — vẫn `Expired`, không cấp vé, **hoàn toàn bộ**. |
| `Cancelled` | Thanh toán thành công đến sau | **Từ chối** — không cấp vé, **hoàn toàn bộ**. |
| `Cancelled` | Người xem huỷ lần nữa | **Từ chối**, lý do "đơn đã huỷ" (`FR-36`). |
| `Paid` | Người xem huỷ | **Từ chối** — đơn đã thanh toán chỉ bị ảnh hưởng khi **hủy suất chiếu** (`BR-21`, `FR-24`). |
| `Paid` | Worker hết hạn | **Không làm gì**. |
| Bất kỳ | Gửi lại yêu cầu giữ ghế với cùng **định danh yêu cầu** | Không tạo đơn thứ hai. Trả về **cùng kết quả với lần đầu** — kể cả khi lần đầu bị từ chối (`FR-37`, `BR-18`). Cùng định danh mà khác nội dung ⇒ từ chối. |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Tạo đơn (`Hold seats`) | **Có** | Bản ghi idempotency, unique `(customer_id, định danh yêu cầu)`, lưu kết quả lần đầu (`INV-08`). Chặn cả yêu cầu lặp **đồng thời**. |
| `AwaitingPayment → Paid` | **Có** | Điều kiện lệnh theo trạng thái nguồn + `expires_at > now()`. Lần hai ảnh hưởng 0 dòng. Thêm: unique `event.id` (`Payment`), unique `showtime_seat_id` (`Ticketing`). |
| `AwaitingPayment → Expired` | **Có** | Điều kiện lệnh. Worker chạy lại vô hạn được. |
| `AwaitingPayment → Cancelled` | **Có** | Điều kiện lệnh. Lần hai không trả ghế lần nữa (lọc theo `hold_id` + `Held`). |

### Điều kiện tiên quyết

- **Tạo đơn** — xem điều kiện `Hold seats` ở máy `Showtime seat`: cùng một thao tác.
- **`→ Paid`**: đơn `AwaitingPayment`; `expires_at > now()`; ghế `Held` với `hold_id` = id đơn;
  `event.id` chưa xử lý.
- **`→ Expired`**: đơn `AwaitingPayment`; `expires_at ≤ now()`.
- **`→ Cancelled` (người xem)**: đơn `AwaitingPayment`; người gọi là **chủ** đơn (`BR-14`).
- **`→ Cancelled` (hủy suất)**: đơn `AwaitingPayment`; suất đang chuyển `OnSale → Cancelled`.

---

## State machine: `Payment intent`

**Đơn vị**: một yêu cầu thanh toán cho một đơn.
Context: `Payment`. Aggregate: `Payment intent`.

> **Không có trạng thái "thất bại".** Thất bại (thẻ bị từ chối, xác thực không xong) chỉ được
> **ghi vào lịch sử**; intent vẫn `AwaitingPayment` và người xem **thử lại bằng cùng intent** —
> đúng mô hình Stripe, và đúng J-04.4 (ADR-0006).

### States

| Trạng thái | Nghĩa |
|---|---|
| `AwaitingPayment` | Đã tạo yêu cầu thanh toán, chờ kết quả. |
| `Captured` | Tiền **đã thu**, đơn đã `Paid`. |
| `RefundPending` | Tiền đã thu nhưng **phải hoàn**; yêu cầu hoàn đã ghi vào outbox, chưa có xác nhận từ Stripe. |
| `Refunded` | Đã hoàn **toàn bộ**. **Trạng thái chết.** |

> **Tên `Captured`, không phải `Paid`** — `Paid` là của `Booking`. Q28 (cùng tên
> `AwaitingPayment` ở hai máy) vẫn mở, chốt ở T11.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Người xem bấm thanh toán | `AwaitingPayment` | Tạo intent gắn **đúng một** đơn (unique `booking_id`; bấm lại trả intent cũ). Gọi Stripe tạo PaymentIntent — **ngoài** DB transaction. |
| `AwaitingPayment` | Thông báo thất bại | `AwaitingPayment` | Chỉ ghi `Payment event`. Người xem thử lại được. |
| `AwaitingPayment` | Thông báo thành công, đơn còn hạn | `Captured` | Ghi `Payment event`. Đơn → `Paid`, ghế → `Sold`, cấp vé — **cùng transaction**. |
| `AwaitingPayment` | Thông báo thành công, đơn **không** còn nhận thanh toán (`Expired` / `Cancelled` / quá `expires_at`) | `RefundPending` | Ghi `Payment event`. Tạo `Refund` + dòng outbox. **Không** cấp vé. |
| `Captured` | Suất chiếu bị hủy | `RefundPending` | Tạo `Refund` + dòng outbox. **Cùng transaction** với `Showtime → Cancelled`. |
| `RefundPending` | Stripe xác nhận đã hoàn | `Refunded` | Ghi `Payment event`. |

Worker outbox gọi Stripe hoàn tiền với **`Idempotency-Key` = id `Refund`** — không phải transition,
mà là việc làm **giữa** `RefundPending` và `Refunded`.

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| Bất kỳ | Thông báo có `event.id` **đã xử lý** | **Bỏ qua**, trả thành công cho Stripe (`BR-17`, `FR-38`). |
| `Captured`, `RefundPending`, `Refunded` | Thông báo **thất bại** đến sau (sai thứ tự) | **Bỏ qua** — chỉ ghi lịch sử. Tiền đã thu là sự thật cuối cùng. |
| `Captured`, `RefundPending`, `Refunded` | Thông báo **thành công** khác (`event.id` khác, cùng intent) | **Bỏ qua** — trạng thái đã vượt qua bước đó. |
| `RefundPending`, `Refunded` | Yêu cầu hoàn lần nữa | **Từ chối** — ràng buộc ≤ 1 `Refund`. |
| `AwaitingPayment` | Yêu cầu hoàn / xác nhận đã hoàn | **Từ chối** + cảnh báo — chưa thu thì không có gì để hoàn; nếu Stripe báo đã hoàn thì dữ liệu lệch, cần người xem xét. |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Xử lý mọi thông báo | **Có** | Unique `event.id` ở lịch sử `Payment event`. Chặn cả hai webhook cùng lúc. |
| Tạo `Refund` | **Có** | Unique: ≤ 1 `Refund` mỗi intent. |
| Gọi Stripe hoàn tiền | **Có** | `Idempotency-Key` = id `Refund`. Worker bị kill giữa chừng rồi chạy lại ⇒ Stripe chỉ hoàn một lần. |
| Tạo intent | **Có** | Unique `booking_id`. |

### Điều kiện tiên quyết

- **Thông báo thành công**: `event.id` chưa xử lý; số tiền khớp tổng tiền đơn — **không khớp** ⇒
  coi như không nhận thanh toán: `RefundPending` + cảnh báo.
- **Hoàn tiền**: intent `Captured` hoặc vừa thu ở nhánh tiền đến muộn; chưa có `Refund`; số tiền
  hoàn **bằng** số đã thu (`INV-03`).

---

## State machine: `Ticket`

**Đơn vị**: một vé — mã QR gắn với **một** ghế của **một** suất chiếu.
Context: `Ticketing`. Aggregate: `Ticket`.

### States

| Trạng thái | Nghĩa |
|---|---|
| `Issued` | Vé đã được cấp (đơn đã `Paid`). Chưa check-in. |
| `CheckedIn` | Vé đã được quét tại cổng. **Trạng thái chết.** |

> **Chỉ hai trạng thái.** Vé của suất đã hủy **không** có trạng thái thứ ba — "suất chưa bị hủy" là
> **điều kiện tiên quyết** của check-in.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Cấp vé (đơn → `Paid`) | `Issued` | **Cùng transaction** với `Booking → Paid` và ghế → `Sold`. Mỗi ghế đúng một vé (unique `showtime_seat_id`). Tạo mã vé ký HMAC. |
| `Issued` | Check-in lần đầu | `CheckedIn` | Ghi **một** dòng check-in: thời điểm, cổng (nơi quét), người quét. Unique `ticket_id`. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `CheckedIn` | Quét lại (kể cả cổng khác) | **Từ chối**, kèm **thời điểm** và **cổng** lần đầu (`FR-07`, `BR-10`). |
| `CheckedIn` | Hai nhân viên quét **cùng lúc** | Một lượt thắng; lượt còn lại bị từ chối như trên. |
| — | Mã không hợp lệ / chữ ký sai / vé không tồn tại | **Từ chối** — kết quả xác định (`FR-25`). |
| — | Vé của đơn **chưa thanh toán** | Không xảy ra được — vé chỉ tồn tại khi đơn `Paid` (`FR-08`). |
| — | Vé thuộc **suất chiếu khác** suất đang quét | **Từ chối** (`FR-25`). |
| — | Người quét **không được phân công** suất đó | **Từ chối** (`FR-35`, `BR-15`). |
| `Issued` | Suất chiếu **đã bị hủy** | **Từ chối** — vé đã/đang được hoàn tiền (`FR-24`). |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Check-in | **Không** theo nghĩa "cùng kết quả" — **đúng ý** | Lượt quét lại bị **từ chối** kèm thời điểm + cổng lần đầu (`FR-07`). Unique `ticket_id` chặn hai lượt đồng thời. |
| Cấp vé | **Có** | Unique `showtime_seat_id`. |

### Điều kiện tiên quyết

**Cấp vé**: chỉ được gọi từ transaction `Booking → Paid`; ghế chưa có vé.

**Check-in**:

- Chữ ký mã vé hợp lệ (xác minh tại server); vé tồn tại và `Issued`.
- Vé thuộc **đúng** suất chiếu đang quét (`FR-25`).
- Người quét là `Gate staff` hoặc `Administrator` (`BR-15`, `FR-33`); nếu là `Gate staff` thì có
  `Gate assignment` cho suất đó (`FR-35`).
- Suất chiếu **chưa bị hủy** (đọc từ `Scheduling`, R7).
- Chưa có dòng check-in cho vé này (unique).

---

## State machine: `Showtime`

**Đơn vị**: một phim, một phòng, một khoảng giờ.
Context: `Scheduling`. Aggregate: `Showtime`.

### States

| Trạng thái | Nghĩa |
|---|---|
| `NotOnSale` | Đã tạo, ghế đã sinh, chưa mở bán. Người xem không thấy. Quản lý rạp chặn ghế được. |
| `OnSale` | Đang bán. Người xem thấy và giữ ghế được **tới giờ bắt đầu chiếu** (`BR-31`). |
| `Cancelled` | Đã hủy. **Trạng thái chết.** |

> "Đã chiếu xong" **không** là trạng thái — nó **suy ra** từ giờ kết thúc. Hạn chót bán vé là điều
> kiện của lệnh giữ ghế, không phải transition.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Tạo suất chiếu | `NotOnSale` | Sinh **toàn bộ** `Showtime seat` (`Available`) theo sơ đồ phòng, all-or-nothing, **cùng transaction** (R1). Exclusion constraint chặn suất chồng nhau (`BR-20`). |
| `NotOnSale` | Mở bán | `OnSale` | Chỉ đổi trạng thái. |
| `OnSale` | Quản lý rạp (chủ) / quản trị viên hủy | `Cancelled` | **Một transaction** (ADR-0006): mọi đơn `AwaitingPayment` → `Cancelled` + trả ghế; mọi intent `Captured` của đơn `Paid` → `RefundPending` + outbox. Gọi Stripe **sau**, bất đồng bộ. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `NotOnSale` | Người xem giữ ghế | **Từ chối** (`BR-13`, `FR-34`). |
| `OnSale`, đã tới giờ bắt đầu | Người xem giữ ghế | **Từ chối**, lý do "suất đã bắt đầu" (`BR-31`). |
| `NotOnSale` | Hủy | Không đi qua transition hủy — suất chưa mở bán thì **xoá** được (xoá cả ghế đã sinh, cùng transaction): không có đơn, không có tiền. |
| `OnSale` | Sửa giờ / phòng | **Từ chối** — vé có thể đã bán (R7). |
| `Cancelled` | Mở bán / hủy lần nữa | **Từ chối**; hủy lần nữa trả về suất đã hủy (idempotent ở tầng lệnh). |
| `OnSale` | Hủy bởi người **không** sở hữu | **Từ chối** (`BR-16`, `FR-32`). |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Tạo suất | Ở tầng yêu cầu (T19) | Ghế sinh trong cùng transaction; unique `(showtime_id, room_seat_id)` chặn ghế trùng. |
| Mở bán | **Có** | Điều kiện lệnh `WHERE status = 'NotOnSale'`. |
| Hủy suất | **Có** | Điều kiện lệnh `WHERE status = 'OnSale'`; hoàn tiền idempotent bằng ≤ 1 `Refund` + `Idempotency-Key`. |

### Điều kiện tiên quyết

- **Tạo suất**: người tạo là quản lý rạp (hoặc quản trị viên); phòng có sơ đồ ghế; không chồng
  suất khác của phòng (`BR-20`).
- **Mở bán**: `NotOnSale`; người thao tác là chủ hoặc quản trị viên; chưa tới giờ bắt đầu.
- **Hủy**: `OnSale`; người thao tác là chủ hoặc quản trị viên (`BR-16`).

---

## Race: thông báo thanh toán đến sát thời điểm hết hạn

Hai luồng tranh **cùng một đơn**:

- **Luồng A** — worker thấy `expires_at ≤ now()` → đơn `Expired`, ghế `Available`.
- **Luồng B** — webhook thành công → thử đơn `Paid`, ghế `Sold`, cấp vé.

### Kết quả mong đợi

Quyết định bằng **thời gian**, không bằng ai chạy trước (ADR-0006):

| Thời điểm xử lý webhook | Kết quả |
|---|---|
| **Trước** `expires_at` | Luồng B thắng: đơn `Paid`, cấp vé. Worker sau đó thấy đơn không còn `AwaitingPayment` → không làm gì. |
| **Sau** `expires_at` (dù worker đã chạy hay chưa) | Luồng B **không thể** thắng (điều kiện `expires_at > now()`). Không cấp vé, intent → `RefundPending`, hoàn toàn bộ. |

### Cơ chế

- Luồng A: `UPDATE … WHERE status = 'AwaitingPayment' AND expires_at <= now()`
- Luồng B: `UPDATE … WHERE status = 'AwaitingPayment' AND expires_at > now()`

Hai điều kiện **loại trừ nhau** theo thời gian, và cùng đọc **một** cột trạng thái ⇒ chỉ một lệnh
có thể ảnh hưởng 1 dòng. Không có cửa sổ TOCTOU.

> **Vì sao sửa (2026-11-25).** Bản cũ là "ai chuyển trạng thái trước thì thắng" — nên webhook đến
> lúc 15:30, sau hạn nhưng trước khi worker quét, vẫn `Paid`. Trái `BR-04`. Kết quả nghiệp vụ không
> được phụ thuộc vào lịch chạy của worker.

---

## Bảng truy vết: `FR-nn` → transition

| FR | Transition | Máy trạng thái | Traces to |
|---|---|---|---|
| `FR-01` | `Available → Held` (điều kiện lệnh) | `Showtime seat` | `BR-01` |
| `FR-02` | `Hold seats` — n ghế trong một DB transaction | `Showtime seat` | `BR-02` |
| `FR-03` | `Hold seats` — **thứ tự khoá cố định** (quota, rồi ghế tăng dần) | `Showtime seat` | `BR-22` |
| `FR-04` | `Held → Available` khi đơn `Expired` | `Showtime seat`, `Booking` | `BR-03` |
| `FR-05` | Cấp vé chỉ trong transaction `→ Paid` | `Ticket` | `BR-09` |
| `FR-06` | `→ Paid` bị từ chối sau `expires_at` | `Booking` | `BR-04` |
| `FR-07` | `CheckedIn` — quét lại bị từ chối kèm thời điểm + cổng | `Ticket` | `BR-10` |
| `FR-09` | Điều kiện `Hold seats` (≤ 6; khoá quota rồi ≤ 10) | `Showtime seat`, `Booking` | `BR-06`, `BR-07`, `BR-08` |
| `FR-14` | `Hold seats` — `expires_at` +15 phút | `Booking` | `BR-03` |
| `FR-15` | `Available → Held` — nói rõ ghế nào đã có người | `Showtime seat` | `BR-01` |
| `FR-16` | `AwaitingPayment → Paid` — cấp vé, giá từng ghế | `Booking` | `BR-09`, `BR-12` |
| `FR-18` | `AwaitingPayment → Cancelled` — trả ghế ngay | `Booking` | `BR-21` |
| `FR-20` | `Available ↔ Blocked` | `Showtime seat` | `BR-11` |
| `FR-22` | `NotOnSale → OnSale` | `Showtime` | `BR-13` |
| `FR-24` | `OnSale → Cancelled` — huỷ đơn chờ, yêu cầu hoàn đơn đã trả | `Showtime`, `Booking`, `Payment intent` | `BR-05`, `BR-16` |
| `FR-25` | Check-in — 5 kết quả xác định | `Ticket` | `BR-10`, `BR-15` |
| `FR-27` | `Issued → CheckedIn` — mỗi lượt thành công được đếm | `Ticket` | `BR-25` |
| `FR-34` | `Hold seats` bị từ chối khi `NotOnSale` hoặc đã tới giờ chiếu | `Showtime` | `BR-13`, `BR-31` |
| `FR-37` | Tạo đơn — bản ghi idempotency | `Booking` | `BR-18` |
| `FR-38` | Xử lý thông báo — unique `event.id` | `Payment intent` | `BR-17` |
| `FR-39` | `→ RefundPending → Refunded` — hoàn toàn bộ, đúng một lần | `Payment intent` | `BR-05` |
| `FR-40` | Tạo `Gate assignment` (không có máy trạng thái — tạo/gỡ) | — | `BR-15` |

### `FR` không có transition riêng

| FR | Vì sao |
|---|---|
| `FR-08` | Không tồn tại vé cho đơn chưa thanh toán — hệ quả của `FR-05`. |
| `FR-19` | Nhắc hết hạn là **tác dụng phụ** của `expires_at` (gửi qua outbox), không phải transition. |

---

## Ranh giới bị cắt — năm chỗ

Nơi **một DB transaction** chạm aggregate ở **nhiều context**. Trong modular monolith: một
transaction, **chỉ gồm ghi database**. Gọi ra ngoài (Stripe, email) luôn qua outbox.

| # | Bất biến bị cắt | Cắt ở đâu | Transition | Hậu quả nếu tách service |
|---|---|---|---|---|
| 1 | "Giữ n ghế: đủ n hoặc không ghế nào" + "đơn và ghế giữ cùng hạn" | R3 — `Inventory` + `Booking` | `Hold seats` | Saga giữ ghế; có lúc ghế giữ mà chưa có đơn |
| 2 | "Thu tiền ⇒ đơn `Paid` ⇒ ghế `Sold` ⇒ cấp vé, đúng một lần" | R4 + R6 + R3 — `Payment`, `Booking`, `Inventory`, `Ticketing` | `AwaitingPayment → Paid` | Outbox + consumer idempotent |
| 3 | "Hết hạn / huỷ: trả ghế + chuyển đơn cùng lúc" | R3 | `→ Expired`, `→ Cancelled` | Compensating; lệnh trả ghế theo `hold_id` giúp không trả nhầm |
| 4 | "Hủy suất: huỷ đơn chờ + yêu cầu hoàn mọi đơn đã trả" | R9 + R3 + R4 — `Scheduling`, `Booking`, `Inventory`, `Payment` | `OnSale → Cancelled` | Một sự kiện sinh N lượt xử lý, mỗi lượt idempotent. Phần gọi Stripe **đã** bất đồng bộ ngay trong monolith |
| 5 | "Suất tồn tại ⇔ ghế của suất tồn tại đủ" | R1 — `Scheduling` + `Inventory` | Tạo suất | Saga tạo suất; có lúc suất tồn tại mà chưa có ghế |

---

## Câu hỏi còn mở

| # | Câu hỏi | Chốt ở |
|---|---|---|
| Q25 | Ghế `Held` đã quá `held_until` mà worker chưa trả — lệnh giữ mới có được lấy luôn không? Nay **an toàn về tính đúng đắn** (đơn cũ không thể `Paid` sau hạn; lệnh trả ghế lọc `hold_id`) — còn lại là câu hỏi cơ chế. | T12 |
| Q28 | `AwaitingPayment` dùng chung tên ở `Booking` và `Payment intent` — đổi tên một bên? | T11 |

**Đã đóng 2026-11-25:** Q26 (bỏ cờ "đã cấp vé" — `Paid` ⇒ đã có vé), Q27 (`Booking` giữ `Paid` khi
hủy suất), Q29 (hủy suất = một transaction ngắn + N yêu cầu hoàn qua outbox — ADR-0006).

## Trạng thái

Bản làm việc, sửa lớn sau review 2026-11-25.
