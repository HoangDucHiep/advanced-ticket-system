# State machines

Lớp 2 — Domain. Vòng đời của các thứ có trạng thái, và những gì **không** được phép xảy ra.

Thuật ngữ: [`GLOSSARY.md`](../../GLOSSARY.md). Ranh giới aggregate, invariant và "ranh giới bị
cắt": [`aggregates/`](aggregates/README.md). Cơ chế chống đua: [`../design/concurrency.md`](../design/concurrency.md).

Một state machine ở đây là **hành vi đúng** được vẽ ra **một lần**; mọi nơi khác tuân theo bản vẽ này.
Mỗi máy có bốn phần: **States**, **Transitions** (kèm tác dụng phụ), **Transitions bị từ chối** (đến sai
lúc, đến lặp, đến sai thứ tự — kèm kết quả mong đợi), **Điều kiện tiên quyết**.

> **Bảng "Transitions bị từ chối" là phần quan trọng nhất.** Nó định nghĩa **idempotency** và
> **không hồi sinh**. Một transition bị từ chối **không phải** lỗi: nó là kết quả hợp lệ và thường
> phải trả cho người dùng một thông điệp cụ thể (`FR-36`).

## Quy ước

- Tên trạng thái dùng đúng tên trong [`GLOSSARY.md`](../../GLOSSARY.md).
- Trạng thái **chết** (không có đường ra) được đánh dấu rõ.
- **"Cùng nhau" / "atomic"** nghĩa là các thay đổi xảy ra trong một lần ghi atomic của hệ thống; việc
  gọi hệ thống bên ngoài (Stripe, email) **không** nằm trong lần ghi đó mà chạy bất đồng bộ
  (ADR-0006).
- **Ba loại transition**: *thuần* (chỉ đổi trạng thái của đơn vị đó, ví dụ `Blocked → Available`);
  *kèm tác dụng phụ* (đổi trạng thái **và** ghi ra ngoài aggregate, ví dụ `AwaitingPayment → Paid` cấp
  vé); *cắt qua ranh giới context* (đổi trạng thái ở nhiều context cùng lúc, ví dụ giữ ghế) — danh sách
  các chỗ cắt: [`aggregates/README.md`](aggregates/README.md#ranh-giới-bị-cắt--năm-chỗ).

---

## State machine: `Showtime seat`

**Đơn vị**: một ghế của một suất chiếu — đơn vị bán thực sự.
Context: `Inventory`. Aggregate: `Showtime seat` (aggregate root riêng — ADR-0005).

### States

| Trạng thái | Nghĩa |
|---|---|
| `Available` | Chưa ai dùng. Người xem có thể giữ. |
| `Held` | Đang được giữ, kèm `hold_id` và `held_until`. Chưa bán, chưa có vé. |
| `Sold` | Đã bán, đã có vé. **Trạng thái chết.** |
| `Blocked` | Không bán online (vệ sinh, dành riêng). Vẫn tính vào tổng số ghế phòng. |

Bốn trạng thái loại trừ nhau và đầy đủ.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Tạo suất chiếu | `Available` | Sinh **toàn bộ** ghế của suất theo sơ đồ phòng, all-or-nothing, atomic với tạo `Showtime` (R1). |
| `Available` | `Hold seats` | `Held` | Ghi `hold_id` (= id đơn) và `held_until` (= `expires_at` của đơn). Atomic với tạo `Booking` ở `AwaitingPayment`. Cả n ghế cùng lúc — all-or-nothing. |
| `Held` đã quá `held_until` | `Hold seats` của người khác | `Held` (lần giữ mới) | Lấy luôn ghế mà không chờ tác vụ hết hạn chạy (ADR-0015). An toàn vì đơn cũ không thể `Paid` sau `expires_at` và lệnh trả ghế chỉ trả đúng lần giữ của nó. |
| `Held` | Thanh toán thành công (đơn còn hạn) | `Sold` | Xoá thông tin giữ. Atomic với: đơn → `Paid`, intent → `Captured`, **cấp vé** (mỗi ghế một vé). |
| `Held` | Đơn hết hạn | `Available` | Xoá thông tin giữ. Chỉ trả ghế có `hold_id` = id đơn đó. Atomic với đơn → `Expired`. |
| `Held` | Đơn bị huỷ (người xem huỷ, hoặc do hủy suất chiếu) | `Available` | Như trên. Atomic với đơn → `Cancelled`. |
| `Available` | Quản lý rạp (chủ suất) chặn ghế | `Blocked` | Không. `Blocked` chỉ sống ở `Inventory`. |
| `Blocked` | Quản lý rạp (chủ suất) gỡ chặn | `Available` | Không. |

**Không có transition nào khác.** Đặc biệt **không có** `Sold → Available`: khi suất chiếu bị hủy, ghế
`Sold` **giữ nguyên** — suất đã chết, không ai đặt lại được; vé bị vô hiệu bằng điều kiện tiên quyết
của check-in (máy `Ticket`), không bằng đổi trạng thái ghế.

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `Held` (còn hạn) hoặc `Sold` | Chặn ghế | **Từ chối**, kèm lý do: ghế đang được giữ hoặc đã bán. |
| `Sold` | Bất kỳ sự kiện trả ghế nào | **Từ chối** — ghế đã bán không trở lại `Available`. |
| `Held` (còn hạn), `Sold` hoặc `Blocked` | `Hold seats` | **Từ chối**, nói rõ **ghế nào** không giữ được (`FR-01`, `FR-15`). Một ghế không đạt ⇒ **toàn bộ** thất bại (`FR-02`). |
| `Held` bởi lần giữ **khác** | Trả ghế theo `hold_id` cũ | **Không làm gì** — ghế đã sang tay người khác. |
| bất kỳ | `Hold seats` khi suất **chưa mở bán** hoặc **đã tới giờ chiếu** | Từ chối ở lệnh giữ ghế (`FR-34`, `BR-13`, `BR-31`). |

### Idempotency

| Transition | Idempotent? | Ý nghĩa |
|---|---|---|
| `Held → Sold` | **Có** | Lệnh lặp là no-op: chỉ ghế đang `Held` đúng `hold_id` mới đổi. |
| `Held → Available` | **Có** | Lặp (tác vụ hết hạn chạy lại, huỷ gọi lại) vẫn an toàn và **không** trả nhầm ghế đã sang tay. |
| `Available ↔ Blocked` | **Có** | Chỉ đổi khi đang ở trạng thái nguồn. |
| `Hold seats` | **Có** ở tầng yêu cầu | Nhờ `INV-08` của `Booking`. Ở tầng ghế, thất bại là do ghế không còn giữ được, không phải do lặp. |

Cơ chế ghi lệnh và chống deadlock: [`concurrency.md`](../design/concurrency.md) #1–#3.

### Điều kiện tiên quyết

**`Hold seats`** — tất cả atomic, theo thứ tự cố định:

1. Suất chiếu `OnSale` **và** chưa tới giờ bắt đầu (`BR-13`, `BR-31`) — đọc từ `Scheduling` (R9).
2. Số ghế theo quy tắc đếm (ghế đôi tính 2) **≤ 6** (`BR-06`, `BR-07`). Loại ghế lấy từ dữ liệu phía
   server, **không** từ yêu cầu client.
3. Tổng ghế của người đó cho suất đó (đơn `AwaitingPayment` còn hạn + `Paid`) sau khi giữ **≤ 10**
   (`BR-08`, ADR-0007).
4. Mỗi ghế đang giữ được (`Available`, hoặc `Held` đã quá hạn); thứ tự lấy ghế cố định để chống deadlock
   (`BR-22`). Thiếu một ghế ⇒ rollback toàn bộ (`BR-02`).

**Thanh toán thành công → `Sold`**: ghế `Held` với `hold_id` = id đơn; đơn `AwaitingPayment` và còn hạn.

**Trả ghế (hết hạn / huỷ)**: ghế `Held` với `hold_id` = id đơn.

---

## State machine: `Booking`

**Đơn vị**: một lần đặt vé của một người xem.
Context: `Booking`. Aggregate: `Booking`.

### States

| Trạng thái | Nghĩa |
|---|---|
| `AwaitingPayment` | Đã giữ ghế, chờ thanh toán. Có `expires_at` (15 phút kể từ lúc giữ). |
| `Paid` | Đã thanh toán **trước** `expires_at`. Vé đã được cấp. **Trạng thái chết.** |
| `Expired` | Quá `expires_at` mà chưa thanh toán. Ghế trả về, không có vé, **không hồi sinh**. **Trạng thái chết.** |
| `Cancelled` | Bị huỷ khi chưa thanh toán — do người xem, hoặc do suất chiếu bị hủy. Ghế trả về, không có vé. **Trạng thái chết.** |

> **Hết hạn được định nghĩa bằng thời gian (ADR-0006).** Đơn mà `now ≥ expires_at` thì về nghiệp vụ
> **đã hết hạn**, dù tác vụ nền chưa kịp chuyển sang `Expired`. Transition `→ Paid` yêu cầu
> `expires_at > now` nên không bao giờ thắng được trong khoảng đó.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | `Hold seats` | `AwaitingPayment` | Tạo `Booking` với `Booking line`, giá đã chốt, `expires_at`. n ghế → `Held`, atomic. Ghi lịch sử. |
| `AwaitingPayment` | Thanh toán thành công, `expires_at > now` | `Paid` | Ghế → `Sold`, intent → `Captured`, **cấp vé** (mỗi ghế một vé), atomic. Ghi lịch sử. |
| `AwaitingPayment` | `expires_at` đã qua (tác vụ nền) | `Expired` | Ghế → `Available`, atomic. Ghi lịch sử. Nếu có intent `Unpaid` ⇒ intent → `Closed` + yêu cầu huỷ phía Stripe. |
| `AwaitingPayment` | Người xem huỷ | `Cancelled` | Ghế → `Available`, atomic. Ghi lịch sử (nguyên nhân: người xem). Intent `Unpaid` ⇒ `Closed` như trên. |
| `AwaitingPayment` | Suất chiếu bị hủy | `Cancelled` | Như trên, nguyên nhân: hủy suất chiếu. Atomic với `Showtime → Cancelled`. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `AwaitingPayment` nhưng `expires_at` đã qua | Thanh toán thành công | **Từ chối** — không cấp vé. Tiền đã thu ⇒ **hoàn toàn bộ** (`BR-04`, `BR-05`, `FR-06`, `FR-39`). |
| `Expired` | Thanh toán thành công đến sau | **Từ chối** — vẫn `Expired`, không cấp vé, **hoàn toàn bộ**. |
| `Cancelled` | Thanh toán thành công đến sau | **Từ chối** — không cấp vé, **hoàn toàn bộ**. |
| `Cancelled` | Người xem huỷ lần nữa | **Từ chối**, lý do "đơn đã huỷ" (`FR-36`). |
| `Paid` | Người xem huỷ | **Từ chối** — đơn đã thanh toán chỉ bị ảnh hưởng khi **hủy suất chiếu** (`BR-21`, `FR-24`). |
| `Paid` | Tác vụ hết hạn | **Không làm gì**. |
| Bất kỳ | Gửi lại yêu cầu giữ ghế với cùng **định danh yêu cầu** | Không tạo đơn thứ hai; trả **cùng kết quả với lần đầu** — kể cả khi lần đầu bị từ chối (`FR-37`, `BR-18`). Cùng định danh mà khác nội dung ⇒ từ chối. |

### Idempotency

| Transition | Idempotent? | Ý nghĩa |
|---|---|---|
| Tạo đơn (`Hold seats`) | **Có** | Theo định danh yêu cầu, kể cả yêu cầu lặp **đồng thời** (`INV-08`). |
| `AwaitingPayment → Paid` | **Có** | Chỉ đơn đang `AwaitingPayment` còn hạn mới đổi; lần hai không làm gì. Thêm: thông báo thanh toán xử lý một lần (`Payment`), một ghế một vé (`Ticketing`). |
| `AwaitingPayment → Expired` | **Có** | Tác vụ nền chạy lại vô hạn được. |
| `AwaitingPayment → Cancelled` | **Có** | Lần hai không trả ghế lần nữa. |

### Điều kiện tiên quyết

- **Tạo đơn** — cùng thao tác `Hold seats` ở máy `Showtime seat`.
- **`→ Paid`**: đơn `AwaitingPayment`; `expires_at > now`; ghế `Held` với `hold_id` = id đơn; thông báo
  thanh toán chưa xử lý.
- **`→ Expired`**: đơn `AwaitingPayment`; `expires_at ≤ now`.
- **`→ Cancelled` (người xem)**: đơn `AwaitingPayment`; người gọi là **chủ** đơn (`BR-14`).
- **`→ Cancelled` (hủy suất)**: đơn `AwaitingPayment`; suất đang chuyển `OnSale → Cancelled`.

---

## State machine: `Payment intent`

**Đơn vị**: một yêu cầu thanh toán cho một đơn.
Context: `Payment`. Aggregate: `Payment intent`.

> **Không có trạng thái "thất bại".** Thất bại (thẻ bị từ chối, xác thực không xong) chỉ được **ghi
> vào lịch sử**; intent vẫn `Unpaid` và người xem **thử lại bằng cùng intent** — đúng mô hình Stripe,
> và đúng J-04.4 (ADR-0006). Tên trạng thái: ADR-0010.

### States

| Trạng thái | Nghĩa |
|---|---|
| `Unpaid` | Đã tạo yêu cầu thanh toán, chưa có kết quả thành công. |
| `Closed` | Đơn đã `Expired` / `Cancelled` khi chưa thu tiền; đã yêu cầu huỷ intent phía Stripe. Chỉ còn đường ra nếu tiền **vẫn** về muộn. |
| `Captured` | Tiền **đã thu**, đơn đã `Paid`. |
| `RefundPending` | Tiền đã thu nhưng **phải hoàn**; yêu cầu hoàn đã ghi, chưa có xác nhận từ Stripe. |
| `Refunded` | Đã hoàn **toàn bộ**. **Trạng thái chết.** |

> **`Captured`, không phải `Paid`** (`Paid` của `Booking`); **`Unpaid`, không phải `AwaitingPayment`**
> (của `Booking`) — ADR-0010.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Người xem bấm thanh toán (đơn `AwaitingPayment`, còn hạn) | `Unpaid` | Tạo intent gắn **đúng một** đơn (bấm lại trả intent cũ). Tạo PaymentIntent phía Stripe — **ngoài** lần ghi atomic. |
| `Unpaid` | Thông báo thất bại | `Unpaid` | Chỉ ghi `Payment event`. Người xem thử lại được. |
| `Unpaid` | Thông báo thành công, đơn còn hạn | `Captured` | Ghi `Payment event`. Đơn → `Paid`, ghế → `Sold`, cấp vé — atomic. |
| `Unpaid` | Thông báo thành công, đơn **không** còn nhận thanh toán (quá `expires_at`, `Expired`, `Cancelled`) | `RefundPending` | Ghi `Payment event`. Tạo `Refund` + yêu cầu hoàn. **Không** cấp vé. |
| `Unpaid` | Đơn → `Expired` / `Cancelled` | `Closed` | Yêu cầu huỷ PaymentIntent phía Stripe. Atomic với chuyển trạng thái đơn. |
| `Closed` | Thông báo thành công (tiền vẫn về muộn — huỷ phía Stripe chưa kịp) | `RefundPending` | Ghi `Payment event`. Tạo `Refund` + yêu cầu hoàn. **Không** cấp vé. |
| `Captured` | Suất chiếu bị hủy | `RefundPending` | Tạo `Refund` + yêu cầu hoàn. Atomic với `Showtime → Cancelled`. |
| `RefundPending` | Stripe xác nhận đã hoàn | `Refunded` | Ghi `Payment event`. |

Việc gọi Stripe hoàn tiền (kèm khoá idempotency theo `Refund`) không phải transition: nó là việc làm
**giữa** `RefundPending` và `Refunded`.

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| Bất kỳ | Thông báo **đã xử lý** | **Bỏ qua**, vẫn báo nhận thành công cho Stripe (`BR-17`, `FR-38`). |
| `Captured`, `RefundPending`, `Refunded` | Thông báo **thất bại** đến sau (sai thứ tự) | **Bỏ qua** — chỉ ghi lịch sử. Tiền đã thu là sự thật cuối cùng. |
| `Captured`, `RefundPending`, `Refunded` | Thông báo **thành công** khác (cùng intent) | **Bỏ qua** — trạng thái đã vượt qua bước đó. |
| `RefundPending`, `Refunded` | Yêu cầu hoàn lần nữa | **Từ chối** — tối đa một `Refund` mỗi intent. |
| `Unpaid`, `Closed` | Yêu cầu hoàn / xác nhận đã hoàn | **Từ chối** + cảnh báo — chưa thu thì không có gì để hoàn; nếu Stripe báo đã hoàn thì dữ liệu lệch, cần người xem xét. |
| `Closed` | Thông báo thất bại | **Bỏ qua** — chỉ ghi lịch sử. |
| `Closed` | Người xem bấm thanh toán lần nữa | **Từ chối** — đơn đã kết thúc (`FR-36`: "đơn đã hết hạn / đã huỷ — hãy đặt lại"). |

### Idempotency

| Transition | Idempotent? | Ý nghĩa |
|---|---|---|
| Xử lý mọi thông báo | **Có** | Mỗi thông báo (theo `event.id`) chỉ xử lý một lần, kể cả hai thông báo đến cùng lúc. |
| Tạo `Refund` | **Có** | Tối đa một `Refund` mỗi intent. |
| Gọi Stripe hoàn tiền | **Có** | Tiến trình bị kill giữa chừng rồi chạy lại ⇒ Stripe chỉ hoàn một lần. |
| Tạo intent | **Có** | Một đơn một intent. |

Cơ chế: [`concurrency.md`](../design/concurrency.md) #7.

### Điều kiện tiên quyết

- **Thông báo thành công**: chưa xử lý; số tiền khớp tổng tiền đơn — **không khớp** ⇒ coi như không
  nhận thanh toán: `RefundPending` + cảnh báo.
- **Hoàn tiền**: intent `Captured` hoặc vừa thu ở nhánh tiền đến muộn; chưa có `Refund`; số tiền hoàn
  **bằng** số đã thu (`INV-03`).

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
| — | Cấp vé (đơn → `Paid`) | `Issued` | Atomic với `Booking → Paid` và ghế → `Sold`. Mỗi ghế đúng một vé. Tạo mã vé có chữ ký. |
| `Issued` | Check-in lần đầu | `CheckedIn` | Ghi **một** bản ghi check-in: thời điểm, cổng (nơi quét), người quét. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `CheckedIn` | Quét lại (kể cả cổng khác) | **Từ chối**, kèm **thời điểm** và **cổng** lần đầu (`FR-07`, `BR-10`). |
| `CheckedIn` | Hai nhân viên quét **cùng lúc** | Một lượt thắng; lượt còn lại bị từ chối như trên. |
| — | Mã không hợp lệ / chữ ký sai / vé không tồn tại | **Từ chối** — kết quả xác định (`FR-25`). |
| — | Mã dựng cho ghế của đơn **chưa thanh toán** | Không có vé nào ứng với mã ⇒ **không hợp lệ**. Vé chỉ tồn tại khi đơn `Paid` (`FR-08`). |
| — | Vé thuộc **suất chiếu khác** suất đang quét | **Từ chối** (`FR-25`). |
| — | Người quét **không được phân công** suất đó | **Từ chối về quyền**, trước khi xét vé (`FR-35`, `BR-15`). Không phải một trong 5 kết quả của vé. |
| `Issued` | Suất chiếu **đã bị hủy** | **Từ chối** — vé đã/đang được hoàn tiền (`FR-24`). |

### Idempotency

| Transition | Idempotent? | Ý nghĩa |
|---|---|---|
| Check-in | **Không** theo nghĩa "cùng kết quả" — **đúng ý** | Lượt quét lại bị **từ chối** kèm thời điểm + cổng lần đầu (`FR-07`); hai lượt đồng thời chỉ một lượt thắng. |
| Cấp vé | **Có** | Một ghế một vé. |

Cơ chế: [`concurrency.md`](../design/concurrency.md) #8.

### Điều kiện tiên quyết

**Cấp vé**: chỉ được gọi từ lần ghi `Booking → Paid`; ghế chưa có vé.

**Check-in**:

- Chữ ký mã vé hợp lệ (xác minh tại server); vé tồn tại và `Issued`.
- Vé thuộc **đúng** suất chiếu đang quét (`FR-25`).
- Người quét là `Gate staff` hoặc `Administrator` (`BR-15`, `FR-33`); nếu là `Gate staff` thì có
  `Gate assignment` cho suất đó (`FR-35`).
- Suất chiếu **chưa bị hủy** (đọc từ `Scheduling`, R7).
- Vé chưa có bản ghi check-in.

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
| — | Tạo suất chiếu | `NotOnSale` | Sinh **toàn bộ** `Showtime seat` (`Available`) theo sơ đồ phòng, all-or-nothing, atomic (R1). Suất không được chồng giờ với suất khác của phòng (`BR-20`). |
| `NotOnSale` | Mở bán | `OnSale` | Chỉ đổi trạng thái. |
| `OnSale` | Quản lý rạp (chủ) / quản trị viên hủy | `Cancelled` | **Một lần ghi atomic** (ADR-0006): mọi đơn `AwaitingPayment` → `Cancelled` + trả ghế; mọi intent `Captured` của đơn `Paid` → `RefundPending`. Gọi Stripe **sau**, bất đồng bộ. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `NotOnSale` | Người xem giữ ghế | **Từ chối** (`BR-13`, `FR-34`). |
| `OnSale`, đã tới giờ bắt đầu | Người xem giữ ghế | **Từ chối**, lý do "suất đã bắt đầu" (`BR-31`). |
| `NotOnSale` | Hủy | Không đi qua transition hủy — suất chưa mở bán thì **xoá** được (xoá cả ghế đã sinh, atomic): không có đơn, không có tiền. |
| `OnSale` | Sửa giờ / phòng | **Từ chối** — vé có thể đã bán (R7). |
| `Cancelled` | Mở bán / hủy lần nữa | **Từ chối**; hủy lần nữa trả về suất đã hủy (idempotent ở tầng lệnh). |
| `OnSale` | Hủy bởi người **không** sở hữu | **Từ chối** (`BR-16`, `FR-32`). |

### Idempotency

| Transition | Idempotent? | Ý nghĩa |
|---|---|---|
| Tạo suất | Ở tầng yêu cầu | Ghế sinh atomic với suất; không thể có ghế trùng. |
| Mở bán | **Có** | Chỉ đổi khi đang `NotOnSale`. |
| Hủy suất | **Có** | Chỉ đổi khi đang `OnSale`; hoàn tiền đúng một lần mỗi đơn (`INV-02` của `Payment`). |

### Điều kiện tiên quyết

- **Tạo suất**: người tạo là quản lý rạp sở hữu phòng (hoặc quản trị viên); phòng có sơ đồ ghế; không
  chồng suất khác của phòng (`BR-20`).
- **Mở bán**: `NotOnSale`; người thao tác là chủ hoặc quản trị viên; chưa tới giờ bắt đầu.
- **Hủy**: `OnSale`; người thao tác là chủ hoặc quản trị viên (`BR-16`).

---

## Race: thông báo thanh toán đến sát thời điểm hết hạn

Hai luồng tranh **cùng một đơn**:

- **Luồng A** — tác vụ nền thấy `expires_at ≤ now` → đơn `Expired`, ghế `Available`.
- **Luồng B** — thông báo thanh toán thành công → thử đơn `Paid`, ghế `Sold`, cấp vé.

Kết quả quyết định bằng **thời gian**, không bằng ai chạy trước (ADR-0006):

| Thời điểm xử lý thông báo | Kết quả |
|---|---|
| **Trước** `expires_at` | Luồng B thắng: đơn `Paid`, cấp vé. Tác vụ nền sau đó thấy đơn không còn `AwaitingPayment` → không làm gì. |
| **Sau** `expires_at` (dù tác vụ nền đã chạy hay chưa) | Luồng B **không thể** thắng. Không cấp vé, intent → `RefundPending`, hoàn toàn bộ. |

Hai điều kiện `expires_at ≤ now` và `expires_at > now` **loại trừ nhau** theo thời gian nên không có
cửa sổ để cả hai cùng thắng. Kết quả nghiệp vụ không phụ thuộc lịch chạy của tác vụ nền. Cơ chế:
[`concurrency.md`](../design/concurrency.md) #4.
