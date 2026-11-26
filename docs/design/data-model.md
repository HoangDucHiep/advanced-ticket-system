# Data model

Lớp 3 — Design. Mỗi quyết định có lý do và phương án bị loại.

Nguồn: [`architecture.md`](architecture.md) (module, sở hữu bảng), [`../domain/aggregates/`](../domain/aggregates/)
(`INV-nn`), [`../domain/state-machines.md`](../domain/state-machines.md), [`../prd/04-business-rules.md`](../prd/04-business-rules.md)
(`BR-nn`). Cơ chế concurrency dùng các bảng này: [`concurrency.md`](concurrency.md).

---

## Quy ước chung

| Quy ước | Quyết định | Vì sao / không chọn |
|---|---|---|
| Schema | Một schema mỗi module: `scheduling`, `inventory`, `booking`, `payment`, `ticketing`, `identity` | Ranh giới sở hữu thấy được ngay trong SQL (ADR-0008) |
| Khoá chính | `uuid`, sinh ở ứng dụng theo **UUIDv7** | v7 tăng dần theo thời gian ⇒ chèn vào B-tree không phân mảnh như v4; id sinh trước khi ghi nên dùng làm `hold_id` / `Idempotency-Key` phía Stripe được. Không chọn `bigserial`: lộ số lượng, và phải ghi xong mới có id |
| Bảng append-only | `bigint GENERATED ALWAYS AS IDENTITY` | Lịch sử, sự kiện: không ai tham chiếu từ ngoài, cần thứ tự chèn |
| Trạng thái | `text` + `CHECK (status IN (...))` | Thêm giá trị = sửa một `CHECK` trong migration. `enum` của Postgres không xoá/đổi tên giá trị được và khó sửa trong transaction. Tên giá trị = tên trong `GLOSSARY.md` |
| Tiền | `bigint`, đơn vị **đồng** (VND) | VND không có phần lẻ (cũng là quy ước của Stripe). Không dùng `numeric`/`decimal`: không cần, và tránh lẫn với "cent" |
| Thời gian | `timestamptz`, luôn UTC | |
| `now()` | Trong Postgres là **thời điểm bắt đầu transaction** | Mọi so sánh `expires_at` / `held_until` trong một transaction dùng cùng một mốc ⇒ nhất quán. Transaction ngắn nên sai lệch không đáng kể |
| Isolation | `READ COMMITTED` (mặc định) | Mọi bất biến được ép bằng điều kiện lệnh, khoá dòng, ràng buộc. Không cần `SERIALIZABLE` (xem `concurrency.md`) |
| Khoá ngoại | Chỉ **trong** một schema | Khoá ngoại xuyên schema = module A phụ thuộc bảng của module B; khi tách service sẽ vỡ. Tham chiếu chéo module là cột `uuid` không có FK, nhất quán do ranh giới bị cắt (một transaction) |
| Append-only | Role ứng dụng **không** có quyền `UPDATE`, `DELETE` trên bảng lịch sử | Ép `INV-07` (Booking), `INV-03` (Identity) ở tầng DB, không phụ thuộc code |

Migrations: EF Core, mỗi module một lịch sử (`<schema>.__ef_migrations_history`). Câu SQL đặc thù
(exclusion constraint, partial index, `GRANT`) viết bằng `migrationBuilder.Sql(...)`.

---

## Schema `scheduling`

### `scheduling.movies`

**Invariant**: danh mục phim chung, chỉ quản trị viên ghi (`BR-32`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK |
| `title` | `text` | không | |
| `description` | `text` | có | |
| `duration_minutes` | `int` | không | Gợi ý giờ kết thúc khi tạo suất |
| `created_at` | `timestamptz` | không | |

Constraint: `CHECK (duration_minutes > 0)`.
Index: chỉ PK — danh sách phim đang chiếu đi từ `showtimes` (`Movie` `INV-11`: không lưu cờ "đang
chiếu").

### `scheduling.rooms`

**Invariant**: phòng thuộc quản lý rạp tạo ra nó (`Room` `INV-14`, `BR-32`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK |
| `owner_id` | `uuid` | không | `identity.users.id` (không FK — khác schema) |
| `name` | `text` | không | |
| `created_at` | `timestamptz` | không | |

Constraint: `UNIQUE (owner_id, name)` — một quản lý rạp không có hai phòng trùng tên.
Index: `UNIQUE` trên phục vụ luôn "phòng của tôi".

**Không có cột "sức chứa"** (`Room` `INV-09`): số ghế = `COUNT(*)` trên `room_seats`.

### `scheduling.room_seats`

**Invariant**: vị trí ghế duy nhất trong phòng, mỗi ghế một loại (`Room` `INV-08`, `INV-10`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK |
| `room_id` | `uuid` | không | FK → `rooms` `ON DELETE CASCADE` |
| `row_label` | `text` | không | `A`, `B`, … |
| `seat_number` | `int` | không | |
| `seat_type` | `text` | không | `Standard` / `Couple` / `VIP` |

Constraint:
- `UNIQUE (room_id, row_label, seat_number)` — `INV-08`.
- `CHECK (seat_type IN ('Standard','Couple','VIP'))` — `INV-10`.
- `CHECK (seat_number > 0)`.

Index: `UNIQUE` trên (tiền tố `room_id`) phục vụ "sơ đồ của phòng".

**Không có cột giá** (`INV-10`): giá thuộc suất chiếu.

**Sửa sơ đồ (`INV-13`)** — lệnh sửa sơ đồ:

```sql
SELECT 1 FROM scheduling.rooms WHERE id = @room_id FOR UPDATE;
SELECT EXISTS (SELECT 1 FROM scheduling.showtimes
               WHERE room_id = @room_id AND status <> 'Cancelled' AND ends_at > now());
-- có ⇒ 409 ROOM_HAS_ACTIVE_SHOWTIMES; không ⇒ thay room_seats
```

`FOR UPDATE` trên dòng phòng **xung đột** với khoá `FOR KEY SHARE` mà Postgres tự lấy khi chèn một
`showtimes` có FK tới phòng đó ⇒ không thể đồng thời "sửa sơ đồ" và "tạo suất" trên cùng phòng.
Không cần bảng khoá riêng.

### `scheduling.showtimes`

**Invariant**: một phòng không có hai suất chồng nhau, tính cả đệm 15 phút (`INV-07`, `BR-20`);
một phim, một phòng (`INV-03`); chủ sở hữu (`INV-04`); trạng thái (`state-machines.md`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK |
| `movie_id` | `uuid` | không | FK → `movies` |
| `room_id` | `uuid` | không | FK → `rooms` |
| `owner_id` | `uuid` | không | = `rooms.owner_id` lúc tạo (`INV-14`); dùng cho kiểm tra quyền `BR-16` mà không join |
| `starts_at` | `timestamptz` | không | |
| `ends_at` | `timestamptz` | không | |
| `status` | `text` | không | `NotOnSale` / `OnSale` / `Cancelled` |
| `created_at` | `timestamptz` | không | |
| `cancelled_at` | `timestamptz` | có | |

Constraint:
- `CHECK (status IN ('NotOnSale','OnSale','Cancelled'))`.
- `CHECK (ends_at > starts_at)`.
- `CHECK ((status = 'Cancelled') = (cancelled_at IS NOT NULL))`.
- **Exclusion constraint** — `BR-20`:

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;
ALTER TABLE scheduling.showtimes ADD CONSTRAINT ex_showtimes_room_no_overlap
  EXCLUDE USING gist (
    room_id WITH =,
    tstzrange(starts_at, ends_at + interval '15 minutes', '[)') WITH &&
  ) WHERE (status <> 'Cancelled');
```

**Vì sao SQL tay:** EF Core không biểu diễn được exclusion constraint. **Vì sao ở DB, không ở ứng
dụng:** hai lệnh tạo suất chạy đồng thời đều "kiểm tra không thấy chồng" rồi cùng chèn — chỉ ràng
buộc ở DB chặn được (`aggregates/01-scheduling.md`). `WHERE status <> 'Cancelled'`: suất đã hủy
không giữ khung giờ.

Index:
- Exclusion constraint tự tạo index GiST `(room_id, khoảng)`.
- `(status, starts_at) WHERE status = 'OnSale'` — "phim đang chiếu" / "lịch chiếu của phim" (`FR-11`,
  `FR-12`, `BR-29`): chỉ suất đang bán, chưa bắt đầu.
- `(movie_id, starts_at)` — lịch chiếu của một phim.
- `(owner_id, starts_at)` — "suất của tôi" (quản lý rạp).

### `scheduling.ticket_prices`

**Invariant**: giá thuộc cặp (suất, loại ghế), mỗi loại một giá (`Showtime` `INV-01`, `BR-12`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `showtime_id` | `uuid` | không | FK → `showtimes` `ON DELETE CASCADE` |
| `seat_type` | `text` | không | |
| `amount` | `bigint` | không | VND |

Constraint: `PRIMARY KEY (showtime_id, seat_type)` — `INV-01`; `CHECK (amount >= 0)` (`FR-21`);
`CHECK (seat_type IN ('Standard','Couple','VIP'))`.

"Mỗi loại ghế **có trong phòng** đều có giá" kiểm ở lệnh tạo suất (cùng transaction) — ràng buộc
xuyên hai bảng, không biểu diễn bằng `CHECK`.

> **Giá lưu ở đâu:** theo **(suất, loại ghế)**, không phải "giá loại ghế của
> phòng × hệ số". Nếu giá nằm ở phòng, đổi giá một loại ghế sẽ đổi giá mọi suất đang bán của phòng
> đó. Giá đã chốt của đơn vẫn là bản sao ở `booking.booking_lines` (`Booking` `INV-02`).

---

## Schema `inventory`

### `inventory.showtime_seats`

**Invariant**: `Showtime seat` `INV-01..INV-05` — một chủ, bốn trạng thái loại trừ, `Held` ⇔ có
thông tin giữ, trả ghế theo `hold_id`, `Blocked` không bán.

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK. **Thứ tự khoá** khi giữ nhiều ghế là thứ tự cột này (`BR-22`) |
| `showtime_id` | `uuid` | không | Không FK (bảng ở `scheduling`) |
| `room_seat_id` | `uuid` | không | Không FK |
| `row_label` | `text` | không | **Bản sao** lúc sinh ghế |
| `seat_number` | `int` | không | **Bản sao** |
| `seat_type` | `text` | không | **Bản sao** — đếm ghế đôi, tra giá |
| `status` | `text` | không | `Available` / `Held` / `Sold` / `Blocked` |
| `hold_id` | `uuid` | có | = id `Booking` (không diễn giải — cặp thuật ngữ #4) |
| `held_until` | `timestamptz` | có | = `Booking.expires_at` |
| `version` | `bigint` | không | Tăng 1 ở **mỗi** lần đổi trạng thái — dùng cho cache theo ghế (#10) |

Constraint:
- `UNIQUE (showtime_id, room_seat_id)` — ranh giới bị cắt #5: một ghế phòng sinh đúng một ghế suất.
- `CHECK (status IN ('Available','Held','Sold','Blocked'))` — `INV-01`.
- `CHECK ((status = 'Held' AND hold_id IS NOT NULL AND held_until IS NOT NULL) OR (status <> 'Held'
  AND hold_id IS NULL AND held_until IS NULL))` — `INV-02`.
- `CHECK (seat_type IN ('Standard','Couple','VIP'))`.

Index:
- `UNIQUE (showtime_id, room_seat_id)` — tiền tố `showtime_id` phục vụ sơ đồ ghế và bốn con số.
- `(hold_id) WHERE hold_id IS NOT NULL` — trả ghế / `→ Sold` theo `hold_id` (`INV-03`). Partial: chỉ
  ghế đang giữ có mặt trong index ⇒ index nhỏ.
- **Không** index `status`: cột ghi nóng nhất hệ thống; mọi truy vấn theo trạng thái đều đã lọc
  theo `showtime_id` (≤ vài trăm dòng).

> **Có cần bảng riêng không:** có. Mỗi ghế của suất là **một dòng** (ADR-0003,
> ADR-0005) — khoá dòng là đơn vị tranh chấp. Giữ "danh sách id ghế" trong dòng suất chiếu thì mọi
> người tranh **một** dòng.
>
> **Vì sao chép `row_label`, `seat_number`, `seat_type`:** sơ đồ ghế và nhãn ghế trên vé cần chúng.
> Sơ đồ phòng không đổi khi còn suất chưa kết thúc (`INV-13`) nên bản sao không lệch. Nhờ vậy đọc
> sơ đồ ghế chỉ cần thêm **giá** từ `Scheduling`.

**Bảng lớn nhất hệ thống.** Ước lượng: 10 phòng × 200 ghế × 5 suất/ngày × 365 ≈ **3,6 triệu dòng/năm**.
Tác động: mỗi lần giữ ghế cập nhật `hold_id` (cột có index) ⇒ **không** phải HOT update, mỗi lần ghi
cập nhật 2 index (PK không đổi nên thực chất là index `hold_id` + heap). Giữ số index ở mức tối
thiểu là vì vậy. Dòng của suất đã kết thúc không bao giờ ghi lại; nếu cần, partition theo tháng của
suất là phương án sau bước load test — chưa làm vì chưa có số đo.

Câu SQL tay của bảng này (giữ n ghế, trả ghế, `→ Sold`, chặn ghế, sinh ghế) nằm ở
[`concurrency.md`](concurrency.md) #1–#4. Ghi chú vì sao không dùng ORM ở mục "Câu SQL tay" cuối file.

### `inventory.outbox_messages`

Xem mục [Outbox](#outbox-mỗi-module-một-bảng).

---

## Schema `booking`

### `booking.bookings`

**Invariant**: `Booking` `INV-01` (một suất), `INV-05` (một chiều, quá hạn không `Paid`), `INV-06`
(huỷ chỉ khi chưa trả, có nguyên nhân), `INV-09` (chủ sở hữu).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK; cũng là `hold_id` trên ghế |
| `customer_id` | `uuid` | không | |
| `showtime_id` | `uuid` | không | **Một** cột ⇒ không trộn suất (`INV-01`) |
| `status` | `text` | không | `AwaitingPayment` / `Paid` / `Expired` / `Cancelled` |
| `expires_at` | `timestamptz` | không | Nguồn sự thật của hạn giữ |
| `total_amount` | `bigint` | không | Tổng giá đã chốt của các dòng |
| `cancel_reason` | `text` | có | `Customer` / `ShowtimeCancelled` |
| `created_at` | `timestamptz` | không | |
| `closed_at` | `timestamptz` | có | Lúc rời `AwaitingPayment` |

Constraint:
- `CHECK (status IN ('AwaitingPayment','Paid','Expired','Cancelled'))`.
- `CHECK ((status = 'Cancelled') = (cancel_reason IS NOT NULL))` — `INV-06`.
- `CHECK (cancel_reason IN ('Customer','ShowtimeCancelled'))`.
- `CHECK ((status = 'AwaitingPayment') = (closed_at IS NULL))`.
- `CHECK (total_amount >= 0)`.

Index:
- `(expires_at) WHERE status = 'AwaitingPayment'` — worker hết hạn (`FR-04`): chỉ đơn đang chờ nằm
  trong index ⇒ nhỏ dù bảng lớn.
- `(customer_id, showtime_id)` — đếm quota (`INV-10`).
- `(customer_id, created_at DESC)` — "đơn của tôi".
- `(showtime_id, status)` — hủy suất: tìm đơn chờ và đơn đã trả của suất (`FR-24`).

### `booking.booking_lines`

**Invariant**: ≤ 6 ghế theo quy tắc đếm (`INV-03`), giá đã chốt (`INV-02`), mỗi dòng một vé
(`INV-04`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `booking_id` | `uuid` | không | FK → `bookings` |
| `showtime_seat_id` | `uuid` | không | |
| `seat_label` | `text` | không | Bản sao (`D7`) — để event `BookingPaid` mang sang vé |
| `seat_type` | `text` | không | Từ `Inventory`, **không** từ client |
| `seat_weight` | `smallint` | không | Couple = 2, còn lại = 1 (`BR-07`) |
| `price` | `bigint` | không | Giá đã chốt |

Constraint:
- `PRIMARY KEY (booking_id, showtime_seat_id)`.
- `CHECK (seat_weight = CASE seat_type WHEN 'Couple' THEN 2 ELSE 1 END)` — quy tắc đếm ghế nằm ở
  **một** chỗ trong DB; tổng quota là `SUM(seat_weight)`.
- `CHECK (price >= 0)`.

**Không** `UNIQUE (showtime_seat_id)`: một ghế xuất hiện ở đơn `Expired` rồi ở đơn khác là hợp lệ.
"Một ghế một chủ" do `inventory.showtime_seats` ép (`INV-01`).

`≤ 6` (`INV-03`) kiểm trong aggregate trước khi chèn — tổng theo nhiều dòng, không biểu diễn bằng
`CHECK`.

### `booking.booking_history`

**Invariant**: append-only, đúng thứ tự (`INV-07`, `BR-27`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `bigint` identity | không | PK — thứ tự chèn |
| `booking_id` | `uuid` | không | FK |
| `from_status` | `text` | có | `NULL` ở dòng đầu |
| `to_status` | `text` | không | |
| `reason` | `text` | có | `Customer`, `ShowtimeCancelled`, `Expired`, `PaymentSucceeded`, … |
| `occurred_at` | `timestamptz` | không | |

Index: `(booking_id, id)` — lịch sử một đơn theo thứ tự (`FR-31`).
Append-only: `REVOKE UPDATE, DELETE ON booking.booking_history FROM app_user`.

### `booking.idempotency_records`

**Invariant**: một `(customer, key)` = một kết quả, kể cả kết quả từ chối (`INV-08`, `BR-18`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `customer_id` | `uuid` | không | |
| `idempotency_key` | `text` | không | 1–64 ký tự `[A-Za-z0-9_-]` |
| `request_hash` | `bytea` | không | SHA-256 (32 byte) của yêu cầu đã chuẩn hoá — `api-contract.md` |
| `response_status` | `int` | không | |
| `response_body` | `jsonb` | không | Trả lại **nguyên văn** |
| `created_at` | `timestamptz` | không | |

Constraint: `PRIMARY KEY (customer_id, idempotency_key)` — đây là cơ chế, không phải trang trí:
yêu cầu thứ hai cùng key **chờ** trên index này cho tới khi yêu cầu đầu commit (ADR-0011).

Index: `(created_at)` — job dọn.

**Hết hạn:** 24 giờ. Job dọn (module `Booking`, mỗi giờ) xoá theo lô 1000 dòng
`WHERE created_at < now() - interval '24 hours'`. Không có trạng thái `InProgress` được **lưu**:
"đang xử lý" là một dòng **chưa commit** — người khác không thấy nó, chỉ chờ nó (ADR-0011). Vì vậy
job dọn không thể xoá nhầm bản ghi đang xử lý.

> **Vì sao bảng nằm ở `booking`, không ở `shared`:** mỗi bảng một chủ (architecture D5). Cơ chế là
> code dùng chung (`Shared.Infrastructure`), bảng thì mỗi module có endpoint cần idempotency tự tạo
> trong schema của mình. Hiện chỉ `Booking` cần.

### `booking.outbox_messages`

Xem [Outbox](#outbox-mỗi-module-một-bảng). Dùng cho email nhắc hết hạn (`FR-19`).

**Khoá quota (`INV-10`)** không có bảng: dùng advisory lock theo transaction (`concurrency.md` #13,
ADR-0007).

---

## Schema `payment`

### `payment.payment_intents`

**Invariant**: `Payment intent` `INV-03` (hoàn = đã thu), `INV-05` (một đơn một intent), máy trạng thái
`Unpaid` / `Closed` / `Captured` / `RefundPending` / `Refunded` (ADR-0010).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK; gửi sang Stripe trong `metadata.intent_id` và làm `Idempotency-Key` khi tạo |
| `booking_id` | `uuid` | không | |
| `amount` | `bigint` | không | VND |
| `currency` | `text` | không | `VND` |
| `status` | `text` | không | |
| `stripe_payment_intent_id` | `text` | có | `pi_…`; `NULL` tới khi Stripe trả về |
| `stripe_client_secret` | `text` | có | Trả cho chủ đơn để client xác nhận thanh toán |
| `created_at` | `timestamptz` | không | |
| `captured_at` | `timestamptz` | có | |

Constraint:
- `UNIQUE (booking_id)` — `INV-05`; bấm "thanh toán" lần hai trả intent cũ (`ON CONFLICT`).
- `UNIQUE (stripe_payment_intent_id)`.
- `CHECK (status IN ('Unpaid','Closed','Captured','RefundPending','Refunded'))`.
- `CHECK (amount > 0)`, `CHECK (currency = 'VND')`.
- `CHECK ((status IN ('Captured','RefundPending','Refunded')) <= (captured_at IS NOT NULL))` — đã
  thu thì có thời điểm thu (`<=` trên boolean = "kéo theo").

Index: hai `UNIQUE` trên đủ cho mọi truy vấn (theo đơn, theo id Stripe).

**Khớp webhook với intent bằng `metadata.intent_id`**, không bằng `stripe_payment_intent_id`: webhook
có thể đến **trước** khi ta kịp lưu id Stripe (lời gọi tạo nằm ngoài transaction).

### `payment.payment_events`

**Invariant**: mỗi thông báo xử lý đúng một lần (`INV-01`, `BR-17`, `FR-38`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `bigint` identity | không | PK |
| `stripe_event_id` | `text` | không | `evt_…` — **không** phải UUID |
| `intent_id` | `uuid` | có | `NULL` nếu không khớp intent nào (cảnh báo) |
| `type` | `text` | không | `payment_intent.succeeded` / `payment_intent.payment_failed` / `charge.refunded` |
| `amount` | `bigint` | có | Số tiền trong thông báo |
| `payload` | `jsonb` | không | Nguyên văn, để truy vết (`FR-31`) |
| `received_at` | `timestamptz` | không | |

Constraint: `UNIQUE (stripe_event_id)` — **cơ chế** chống xử lý lặp: `INSERT … ON CONFLICT DO
NOTHING RETURNING id` không trả dòng ⇒ đã xử lý, trả `200` cho Stripe.
Index: `(intent_id, id)` — lịch sử thông báo của một đơn (`FR-31`).
Append-only: `REVOKE UPDATE, DELETE`.

### `payment.refunds`

**Invariant**: ≤ 1 `Refund` mỗi intent, hoàn toàn bộ (`INV-02`, `INV-03`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK; **`Idempotency-Key`** khi gọi Stripe hoàn tiền |
| `intent_id` | `uuid` | không | FK |
| `amount` | `bigint` | không | = `payment_intents.amount` (kiểm khi tạo, cùng transaction) |
| `stripe_refund_id` | `text` | có | `re_…` |
| `created_at` | `timestamptz` | không | |

Constraint: `UNIQUE (intent_id)` — `INV-02` tầng thứ nhất; `CHECK (amount > 0)`.

### `payment.webhook_failures`

**Invariant**: tiền đã thu không bao giờ bị **im lặng** bỏ qua (`aggregates/04-payment.md` — lỗi
không mong đợi).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `stripe_event_id` | `text` | không | PK |
| `attempts` | `int` | không | |
| `last_error` | `text` | không | |
| `first_seen_at`, `last_seen_at` | `timestamptz` | không | |

Ghi bằng **transaction riêng** khi xử lý webhook ném lỗi không mong đợi (transaction chính đã
rollback ⇒ Stripe sẽ gửi lại). `attempts >= 5` ⇒ log mức `Critical` + metric
`payment_webhook_poison_total` để cảnh báo. Đây là ngoại lệ có chủ ý của ADR-0008 "một transaction
mỗi request": ghi nhận lỗi không được rollback cùng lỗi.

### `payment.outbox_messages`

Xem [Outbox](#outbox-mỗi-module-một-bảng). Dùng cho: hoàn tiền, huỷ PaymentIntent phía Stripe.

---

## Schema `ticketing`

### `ticketing.tickets`

**Invariant**: một ghế một vé (`Ticket` `INV-01`), vé chỉ tồn tại sau `Paid` (`INV-02` — do chỉ chèn
trong transaction `→ Paid`), chủ sở hữu (`INV-07`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK; nằm trong mã QR |
| `booking_id` | `uuid` | không | |
| `customer_id` | `uuid` | không | **Bản sao** từ `BookingPaid` |
| `showtime_id` | `uuid` | không | |
| `showtime_seat_id` | `uuid` | không | |
| `seat_label` | `text` | không | Bản sao — in trên vé, nhân viên cổng đọc |
| `issued_at` | `timestamptz` | không | Nằm trong mã QR |

Constraint: `UNIQUE (showtime_seat_id)` — `INV-01`; webhook lặp mà lọt qua mọi lớp khác thì vẫn chết
ở đây (rollback cả transaction thanh toán).

Index: `(customer_id, issued_at DESC)` — "vé của tôi" (`FR-17`); `(booking_id)` — vé của một đơn.

**Không có cột trạng thái** (`Issued` / `CheckedIn`): trạng thái **suy ra** từ việc có dòng ở
`check_ins` hay không. Lưu thêm một cột nghĩa là check-in phải ghi hai chỗ và hai chỗ có thể lệch;
máy trạng thái ở `state-machines.md` vẫn đúng về nghĩa.

**Không lưu mã QR:** mã được **tính lại** từ các cột trên + khoá ký (`api-contract.md` — định dạng
QR). Lưu mã = lưu một bí mật không cần thiết.

### `ticketing.check_ins`

**Invariant**: một vé một check-in (`INV-03`, `BR-10`); quét lại trả thời điểm + cổng gốc (`INV-05`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `ticket_id` | `uuid` | không | **PK** — FK → `tickets` |
| `showtime_id` | `uuid` | không | Bản sao — để đếm theo suất không cần join |
| `gate` | `text` | không | Nhãn nơi quét |
| `scanned_by` | `uuid` | không | |
| `checked_in_at` | `timestamptz` | không | |

Constraint: `PRIMARY KEY (ticket_id)` — **cơ chế** của `BR-10`.
Index: `(showtime_id)` — đếm lượt vào (`FR-27`).
Append-only: `REVOKE UPDATE, DELETE`.

### `ticketing.gate_assignments`

**Invariant**: `Gate assignment` `INV-08`, `INV-09`.

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `showtime_id` | `uuid` | không | |
| `staff_id` | `uuid` | không | |
| `assigned_by` | `uuid` | không | |
| `assigned_at` | `timestamptz` | không | |

Constraint: `PRIMARY KEY (showtime_id, staff_id)` — không phân công trùng. Gỡ phân công = `DELETE`
(không có yêu cầu lưu lịch sử phân công).

---

## Schema `identity`

### `identity.users`

**Invariant**: `User` `INV-04` (đúng một vai trò), `INV-02` (thu quyền có hiệu lực ngay — qua
`role_version`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK; `sub` trong JWT |
| `email` | `text` | không | Lưu dạng chữ thường |
| `password_hash` | `text` | không | ASP.NET Core `PasswordHasher` (PBKDF2) — không plaintext |
| `role` | `text` | không | `Customer` / `TheaterManager` / `GateStaff` / `Administrator` |
| `role_version` | `int` | không | Tăng mỗi lần đổi vai trò — khoá cache vai trò |
| `created_at` | `timestamptz` | không | |

Constraint: `UNIQUE (email)`; `CHECK (role IN (...))`; `CHECK (email = lower(email))`.

Tên giá trị vai trò trong code/DB bỏ dấu cách (`TheaterManager`, `GateStaff`) — là tên định danh của
thuật ngữ `Theater manager`, `Gate staff` trong `GLOSSARY.md`.

### `identity.role_changes`

**Invariant**: lịch sử đổi quyền append-only (`INV-03`).

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `bigint` identity | không | PK |
| `user_id` | `uuid` | không | FK |
| `from_role`, `to_role` | `text` | không | |
| `changed_by` | `uuid` | không | |
| `changed_at` | `timestamptz` | không | |

Index: `(user_id, id)`. Append-only: `REVOKE UPDATE, DELETE`.

### `identity.refresh_tokens`

**Invariant**: rotation + phát hiện dùng lại (bài toán #6, ADR-0014); không lưu token thô.

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `uuid` | không | PK |
| `user_id` | `uuid` | không | FK |
| `family_id` | `uuid` | không | Chuỗi rotation bắt đầu từ một lần đăng nhập |
| `token_hash` | `bytea` | không | SHA-256 của token (token là 32 byte ngẫu nhiên) |
| `expires_at` | `timestamptz` | không | 14 ngày |
| `created_at` | `timestamptz` | không | |
| `revoked_at` | `timestamptz` | có | |
| `replaced_by` | `uuid` | có | Token kế tiếp trong chuỗi |

Constraint: `UNIQUE (token_hash)`; `CHECK (replaced_by IS NULL OR revoked_at IS NOT NULL)`.
Index: `UNIQUE (token_hash)` — tra token; `(family_id) WHERE revoked_at IS NULL` — revoke cả chuỗi.

**Vì sao SHA-256 mà không phải bcrypt/PBKDF2 như mật khẩu:** token là 256 bit ngẫu nhiên — không
đoán được, nên không cần hàm băm chậm; và cần tra theo **giá trị băm** (index), điều mà hàm băm có
salt không cho làm.

---

## Outbox — mỗi module một bảng

Có ở `inventory`, `booking`, `payment` (architecture D8). Cùng một hình dạng, cấu hình từ
`Shared.Infrastructure`.

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|
| `id` | `bigint` identity | không | PK — thứ tự xử lý |
| `type` | `text` | không | Ví dụ `RefundRequested`, `SeatStatesChanged`, `ExpiryReminderDue` |
| `payload` | `jsonb` | không | |
| `available_at` | `timestamptz` | không | Hẹn giờ (email nhắc) và backoff khi thử lại |
| `attempts` | `int` | không | |
| `locked_until` | `timestamptz` | có | Lease của dispatcher |
| `last_error` | `text` | có | |
| `processed_at` | `timestamptz` | có | |
| `dead_at` | `timestamptz` | có | Quá 10 lần thử ⇒ dừng + cảnh báo |

Index: `(available_at, id) WHERE processed_at IS NULL AND dead_at IS NULL` — dispatcher chỉ quét
việc còn lại.

**Claim bằng lease, không giữ transaction trong lúc gọi ra ngoài:**

```sql
UPDATE payment.outbox_messages SET locked_until = now() + interval '30 seconds', attempts = attempts + 1
WHERE id IN (
  SELECT id FROM payment.outbox_messages
  WHERE processed_at IS NULL AND dead_at IS NULL
    AND available_at <= now() AND (locked_until IS NULL OR locked_until < now())
  ORDER BY id
  LIMIT 20
  FOR UPDATE SKIP LOCKED)
RETURNING id, type, payload, attempts;
-- commit ngay; gọi Stripe ngoài transaction; rồi:
UPDATE payment.outbox_messages SET processed_at = now() WHERE id = @id;
```

**Vì sao SQL tay:** `FOR UPDATE SKIP LOCKED` và `UPDATE … WHERE id IN (subquery) RETURNING` không
biểu diễn được bằng LINQ. **Vì sao lease, không giữ khoá:** giữ transaction mở trong lúc gọi Stripe
(vài trăm ms tới vài giây) giữ luôn một connection của pool. Lease chết thì việc tự quay lại hàng
đợi; handler idempotent nên xử lý lại an toàn (`Idempotency-Key` phía Stripe).

---

## Câu SQL tay — tổng hợp và vì sao ORM không làm được

| # | Câu | Ở đâu | Vì sao không dùng EF Core |
|---|---|---|---|
| 1 | **Giữ n ghế**: `SELECT … ORDER BY id FOR UPDATE` rồi `UPDATE … WHERE status … RETURNING id` | `concurrency.md` #1–#3 | Cần khoá theo **thứ tự** chỉ định, cần biết **ghế nào** thất bại (`FR-15`), cần điều kiện trên trạng thái trong chính lệnh. `ExecuteUpdate` của EF chỉ trả **số dòng**, không khoá theo thứ tự, không `RETURNING` |
| 2 | Trả ghế / `→ Sold` theo `hold_id` | #3, #4 | Cập nhật hàng loạt có điều kiện + tăng `version` + trả về các dòng đã đổi (để ghi outbox cache) |
| 3 | `→ Paid` với `expires_at > now()` | #4 | Điều kiện thời gian **trong** lệnh; số dòng 0/1 quyết định nhánh |
| 4 | Khoá quota `pg_advisory_xact_lock` | #13 | Hàm của Postgres, không có trong LINQ |
| 5 | `INSERT … ON CONFLICT DO NOTHING RETURNING` (sự kiện thanh toán, check-in, idempotency) | #5, #7, #8 | EF ném exception khi vi phạm unique ⇒ phải bắt exception để điều khiển luồng, và exception làm hỏng transaction đang mở |
| 6 | Exclusion constraint, partial index, `GRANT` | file này | Migration builder không có API |
| 7 | Outbox claim `FOR UPDATE SKIP LOCKED` | mục Outbox | Như trên |
| 8 | Rotation refresh token `UPDATE … WHERE revoked_at IS NULL RETURNING` | #6 | Đọc-rồi-ghi qua change tracking có cửa sổ race |

Phần còn lại (tạo phim, phòng, suất, phân công, người dùng) dùng EF Core.

---

## Đối chiếu invariant → cơ chế ở DB

| Invariant | Ràng buộc / cơ chế ở DB | Cơ chế ở ứng dụng (nếu có) |
|---|---|---|
| `Showtime seat` `INV-01` | `CHECK status` + điều kiện lệnh + khoá dòng | — |
| `Showtime seat` `INV-02` | `CHECK` thông tin giữ ⇔ `Held` | — |
| `Showtime seat` `INV-03` | Điều kiện `hold_id = @id` | — |
| `Showtime seat` `INV-04`, `INV-05` | Điều kiện trạng thái nguồn trong lệnh | Kiểm quyền chủ suất (R9) |
| Ranh giới bị cắt #5 | `UNIQUE (showtime_id, room_seat_id)` + cùng transaction | Sinh đủ ghế |
| `Booking` `INV-01` | Hình dạng: một cột `showtime_id` | — |
| `Booking` `INV-02` | Giá chép vào `booking_lines.price` | Loại ghế/giá đọc từ server |
| `Booking` `INV-03` (≤ 6) | — | Kiểm trong aggregate |
| `Booking` `INV-05` | Điều kiện `expires_at > now()` + `CHECK status` | — |
| `Booking` `INV-06` | `CHECK cancel_reason` | Máy trạng thái |
| `Booking` `INV-07` | `REVOKE UPDATE, DELETE` | Chỉ thêm |
| `Booking` `INV-08` | `PRIMARY KEY (customer_id, idempotency_key)` | Savepoint (ADR-0011) |
| `Booking` `INV-10` (≤ 10) | Advisory lock (người, suất) | Đếm `SUM(seat_weight)` sau khoá |
| `BR-07` (ghế đôi = 2) | `CHECK seat_weight` | — |
| `Showtime` `INV-01` | `PRIMARY KEY (showtime_id, seat_type)` | Đủ giá mọi loại ghế |
| `Showtime` `INV-07` / `BR-20` | Exclusion constraint | — |
| `Room` `INV-08` | `UNIQUE (room_id, row_label, seat_number)` | — |
| `Room` `INV-13` | `FOR UPDATE` trên `rooms` xung đột với FK khi tạo suất | Kiểm suất chưa kết thúc |
| `Payment intent` `INV-01` | `UNIQUE (stripe_event_id)` | — |
| `Payment intent` `INV-02` | `UNIQUE (intent_id)` ở `refunds` | `Idempotency-Key` = `refunds.id` |
| `Payment intent` `INV-05` | `UNIQUE (booking_id)` | — |
| `Ticket` `INV-01` | `UNIQUE (showtime_seat_id)` | — |
| `Ticket` `INV-03` | `PRIMARY KEY (ticket_id)` ở `check_ins` | — |
| `Gate assignment` `INV-08` | `PRIMARY KEY (showtime_id, staff_id)` | Kiểm phân công khi quét |
| `User` `INV-03` | `REVOKE UPDATE, DELETE` ở `role_changes` | — |
| `User` `INV-04` | Một cột `role` | — |
