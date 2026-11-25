# State machines

Lớp 2 — Domain. Vòng đời của các thứ có trạng thái, và những gì **không** được phép xảy ra.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/08-state-machines.md`.
Thuật ngữ: [`ubiquitous-language.md`](ubiquitous-language.md).
Ranh giới aggregate và invariant: [`aggregates/`](aggregates/).

## Cách đọc

Một state machine ở đây là **hành vi đúng** được vẽ ra **một lần**. Mọi nơi khác trong hệ
thống tuân theo bản vẽ này — đó là lý do nó là artifact của domain chứ không phải của design.

Mỗi máy trạng thái có bốn phần:

| Phần | Trả lời câu hỏi |
|---|---|
| **States** | Đơn vị này có những trạng thái nào? |
| **Transitions** | Sự kiện nào đưa nó từ trạng thái này sang trạng thái kia, và **tác dụng phụ** là gì? |
| **Transitions bị từ chối** | Sự kiện nào đến sai lúc, đến lặp, đến sai thứ tự — và **kết quả mong đợi** là gì? |
| **Điều kiện tiên quyết** | Trước khi chuyển, cái gì phải đúng? |

> **Bảng "Transitions bị từ chối" là phần quan trọng nhất.** Nó là nơi định nghĩa
> **idempotency** và **không hồi sinh** — hai thứ mà bảng "Transitions" không nói được.
> Một transition bị từ chối **không phải** là lỗi: nó là một kết quả hợp lệ, có kết quả
> mong đợi riêng, và thường phải trả về cho người dùng một thông điệp cụ thể (`FR-36`).

### Quy ước về tên trạng thái

- Tên trạng thái dùng **đúng** tên tiếng Anh trong
  [`ubiquitous-language.md`](ubiquitous-language.md) — đây là tên sẽ dùng trong code
  (tên cột, tên hằng số).
- Từ nào bị glossary ghi là **nên tránh** thì không dùng. Đáng chú ý: `Pending` bị cấm
  (dễ nhầm với `Held` của ghế), `Reserved` bị cấm (dễ nhầm với `Blocked`).
- Trạng thái **chết** (không có đường đi ra) được đánh dấu rõ.

### Ba loại transition

| Loại | Nghĩa | Ví dụ |
|---|---|---|
| **Chuyển trạng thái thuần** | Chỉ đổi trạng thái của đơn vị đó | `Blocked → Available` |
| **Chuyển kèm tác dụng phụ** | Đổi trạng thái **và** ghi ra ngoài ranh giới aggregate | `AwaitingPayment → Paid` (cấp vé, ghi lịch sử) |
| **Chuyển cắt qua ranh giới context** | Đổi trạng thái ở **nhiều context**, phải cùng transaction | Giữ ghế (`Inventory` + `Booking`) |

> **Ba ranh giới bị cắt.** `context-map.md` đã liệt kê ba chỗ một bất biến bị cắt ngang ranh giới
> context: giữ n ghế (R3), cấp vé (R6), hết hạn (R3). T08 phát hiện **chỗ thứ tư**: hủy suất
> chiếu (`Scheduling`) kéo theo vô hiệu vé (`Ticketing`) và hoàn tiền (`Payment`). Xem mục
> "Ranh giới bị cắt" ở cuối file.

---

## State machine: `Showtime seat`

**Đơn vị**: một ghế của một suất chiếu cụ thể — đơn vị bán thực sự.
Context: `Inventory`. Aggregate: `Seat hold` (chứa tối đa 6 ghế).

Thuật ngữ: `Room seat` (ghế vật lý của phòng) là thứ **khác** — nó tĩnh và không có trạng
thái. Xem mục "Các cặp thuật ngữ dễ nhầm" ở
[`ubiquitous-language.md`](ubiquitous-language.md).

### States

| Trạng thái | Nghĩa |
|---|---|
| `Available` | Chưa ai dùng. Người xem có thể đặt. |
| `Held` | Đang được giữ tạm cho một người xem, kèm thời điểm hết hạn. Chưa bán, chưa có vé. |
| `Sold` | Đã bán. Đã thanh toán, đã có vé. |
| `Blocked` | Không bán online (vệ sinh, dành riêng). Vẫn tính vào tổng số ghế phòng. |

Bốn trạng thái này là **loại trừ nhau và đầy đủ** — không có trạng thái thứ năm.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | `Hold seats` | `Held` | Tạo bản ghi `Seat hold` với thời điểm hết hạn (+15 phút). **Cùng transaction** với: tạo `Booking` ở trạng thái `AwaitingPayment` và ghi lịch sử đơn. Tất cả n ghế chuyển trong **cùng một** transaction — all-or-nothing. |
| `Held` | Kết quả thanh toán thành công | `Sold` | **Một transition duy nhất, cắt qua ba context**: đơn → `Paid`, ghế → `Sold`, và **cấp vé** (mỗi ghế sinh đúng một vé). Tất cả cùng transaction. Ghi lịch sử đơn. |
| `Held` | `Expired` (hết hạn giữ) | `Available` | Chuyển đơn → `Expired`. **Cùng transaction**. Tác vụ nền quét lô. |
| `Held` | `Cancelled` (người xem huỷ) | `Available` | Chuyển đơn → `Cancelled`. **Cùng transaction**. |
| `Available` | Quản lý rạp chặn ghế | `Blocked` | Ghi nhận ghế bị chặn theo lịch chiếu (`Scheduling`). Không ai đặt được ghế này nữa. |
| `Blocked` | Quản lý rạp gỡ chặn | `Available` | Bỏ ghế khỏi danh sách bị chặn. Ghế trở lại bán online. |

**Không có transition nào khác.** Đặc biệt: **không có** `Sold → Available` (ghế đã bán
không trở lại), và **không có** `Held → Blocked` / `Sold → Blocked` (xem bảng bị từ chối).

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `Held` hoặc `Sold` | Quản lý rạp chặn ghế | **Từ chối**, kèm lý do: ghế đang được giữ hoặc đã bán. Ghế phải về `Available` trước. |
| `Sold` | Bất kỳ sự kiện giải phóng nào | **Từ chối** — ghế đã bán không trở lại `Available`. |
| Bất kỳ trạng thái nào khác `Available` | `Hold seats` | **Từ chối**, và nói rõ **ghế nào** đã có người (`FR-01`, `FR-15`). Nếu chỉ một ghế trong n ghế không đạt, **toàn bộ** thao tác thất bại — không ghế nào được giữ (`FR-02`). |
| `Available` | `Hold seats` với suất chiếu **chưa mở bán** | Từ chối ở tầng lệnh đặt vé (`FR-34`). |

### Idempotency

Transition nào **phải** idempotent, và idempotent **bằng cơ chế nào**:

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| `Held → Sold` | **Có** — chạy lại là **no-op** | Điều kiện lệnh `UPDATE ... WHERE status = 'Held'`. Lần thứ hai ảnh hưởng **0 dòng** ⇒ coi như đã xong, không lỗi. Cần thiết vì webhook Stripe đến lặp (`BR-17`) và worker chạy lại. |
| `Held → Available` | **Có** — chạy lại là **no-op** | Cùng cơ chế: điều kiện lệnh theo trạng thái nguồn. Lần thứ sau ảnh hưởng 0 dòng. Cần thiết vì tác vụ giải phóng hết hạn có thể retry, và huỷ đơn có thể được gọi lại. |
| `Available → Blocked` | Không cần idempotent riêng | Ghi theo danh sách ghế bị chặn theo lịch chiếu — đây là **thuộc tính của lịch chiếu**, không phải lần ghi trạng thái ghế. Ghi lại danh sách là idempotent theo bản chất. |
| `Blocked → Available` | Không cần idempotent riêng | Như trên — bỏ khỏi danh sách bị chặn. |
| `Hold seats` | **Có** ở tầng yêu cầu | Ràng buộc duy nhất trên **định danh yêu cầu** (`FR-37`, `BR-18`) — xem máy `Booking`. Ở tầng ghế, lỗi là do ghế không còn `Available`, không phải do lặp yêu cầu. |

> **Vì sao điều kiện lệnh đủ.** Với `Held → Sold`, hai webhook đến cùng lúc ⇒ chỉ một lệnh
> ảnh hưởng 1 dòng, lệnh còn lại ảnh hưởng 0 dòng. Không cần khoá đọc-sửa-ghi, không có cửa
> sổ TOCTOU. Xem `INV-01` ở [`aggregates/02-inventory.md`](aggregates/02-inventory.md).

### Điều kiện tiên quyết

**`Hold seats`** — điều kiện ở **biên aggregate** `Seat hold`:

- Tất cả ghế được chọn đều đang `Available` — kiểm tra bằng điều kiện lệnh, không đọc trước
  rồi quyết định.
- Số ghế tính theo quy tắc đếm ghế (ghế đôi tính 2) **≤ 6** (`BR-06`, `BR-07`) — cũng là giới
  hạn kích thước aggregate.
- Mỗi ghế thuộc **đúng một** suất chiếu, và suất đó **đang bán** (`BR-13`, `FR-34`) — điều
  kiện **xuyên ranh giới**, xem [`aggregates/01-scheduling.md`](aggregates/01-scheduling.md)
  `INV-02`.

Điều kiện ở **tầng ứng dụng** (không nằm trong aggregate — vì vượt ranh giới):

- Tổng số ghế người đó đang giữ cho suất chiếu đó **chưa vượt 10** (`BR-08`, `FR-09`) — đếm
  theo **người** qua **nhiều đơn**, về nguyên tắc không thể nằm trong một aggregate. Chấp
  nhận cửa sổ race. Xem `INV-10` ở [`aggregates/03-booking.md`](aggregates/03-booking.md).

**Kết quả thanh toán thành công**:

- Ghế đang `Held` và thuộc đúng `Seat hold` đó.
- Đơn đang `AwaitingPayment`.
- Tất cả cùng transaction — không được tách.

**Giải phóng hết hạn**:

- Ghế đang `Held` và **thời điểm hết hạn đã qua**.
- Đơn vẫn `AwaitingPayment` — nếu đơn đã `Paid`, tác vụ này **không làm gì**.

### Ghi chú về bản ghi `Seat hold`

Bản ghi `Seat hold` **được giữ lại** khi kết thúc — không xoá. Nó là dấu vết để truy vết
(`FR-31`), và việc trạng thái ghế là nguồn sự thật cho `Sold`/`Available`/`Blocked` thì nằm ở
chính dòng ghế.

---

## State machine: `Booking`

**Đơn vị**: một lần đặt vé của một người xem.
Context: `Booking`. Aggregate: `Booking`.

### States

| Trạng thái | Nghĩa |
|---|---|
| `AwaitingPayment` | Đã giữ ghế, đang chờ thanh toán. Có thời điểm hết hạn. |
| `Paid` | Đã thanh toán thành công. **Chỉ lúc này** vé mới được cấp. |
| `Expired` | Quá thời điểm giữ mà chưa thanh toán. Coi như đã huỷ, ghế trả về, không có vé, và **không hồi sinh**. |
| `Cancelled` | Người xem chủ động huỷ (chưa thanh toán). Ghế trả về, không có vé. |

> **Tên `AwaitingPayment`, không phải `Pending`.** `Pending` bị glossary ghi là nên tránh
> (dễ nhầm với `Held` của ghế). `AwaitingPayment` là tên sẽ dùng trong code.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | `Hold seats` | `AwaitingPayment` | Tạo `Booking` với các `Booking line` và **giá đã chốt** tại lúc giữ. Tạo bản ghi `Seat hold` và chuyển n ghế → `Held`. **Cùng transaction**. Ghi dòng lịch sử đầu tiên. |
| `AwaitingPayment` | Kết quả thanh toán thành công | `Paid` | Ghế → `Sold`, **cấp vé** (mỗi ghế một vé), đánh dấu "đã cấp vé" + số lượng vé. **Cùng transaction**. Ghi lịch sử. |
| `AwaitingPayment` | Hết thời gian giữ | `Expired` | Ghế → `Available`. **Cùng transaction**. Ghi lịch sử. |
| `AwaitingPayment` | Người xem huỷ | `Cancelled` | Ghế → `Available`. **Cùng transaction**. Ghi lịch sử. |

**Cả ba trạng thái cuối (`Paid`, `Expired`, `Cancelled`) là trạng thái chết** — không có đường
đi ra. Trạng thái đơn đi theo **một chiều**.

> **`Expired` và `Cancelled` là hai trạng thái khác nhau** dù hậu quả giống nhau (đều trả ghế
> về, đều không có vé). Khác nhau ở **nguyên nhân** và ở một tính chất: `Expired` **không hồi
> sinh** (`BR-04`, `FR-06`) — thông báo kết quả thanh toán đến sau không đưa được đơn về lại
> trạng thái đang chờ. `Cancelled` do người xem chủ động, và cũng không hồi sinh.

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `Expired` | Kết quả thanh toán thành công đến **sau** | **Từ chối** — đơn vẫn `Expired`, **không cấp vé**, ghế đã trả về. Tiền đã thu ⇒ **hoàn toàn bộ** (`BR-05`, `FR-06`, `FR-39`). Đây là hành vi được yêu cầu rõ ràng, không phải lỗi. |
| `Cancelled` | Người xem huỷ lần nữa | **Từ chối**, kèm lý do "đơn đã huỷ" (`FR-36`). Idempotent ở tầng lệnh: trả về đơn cũ. |
| `Paid` | Người xem huỷ | **Từ chối** — đơn đã thanh toán không huỷ được bằng thao tác này (`BR-21`). Việc này thuộc quyền của quản lý rạp khi **hủy suất chiếu** (`FR-24`). |
| `Paid` | Hết thời gian giữ | **Từ chối** — tác vụ giải phóng hết hạn **không làm gì** với đơn đã trả tiền. |
| Bất kỳ trạng thái nào khác `AwaitingPayment` | Tạo đơn mới với cùng **định danh yêu cầu** | Không tạo đơn thứ hai. Trả về **cùng kết quả** với lần đầu (`FR-37`, `BR-18`). |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Tạo đơn (`Hold seats`) | **Có** | Ràng buộc **duy nhất** trên `request_id` ở tầng lưu trữ. Yêu cầu lặp tuần tự ⇒ trả về đơn cũ. Yêu cầu lặp **đồng thời** ⇒ ràng buộc chặn, chỉ một đơn tồn tại. Đây là cơ chế **mạnh hơn** kiểm tra trong ứng dụng. Xem `INV-08` ở [`aggregates/03-booking.md`](aggregates/03-booking.md). |
| `AwaitingPayment → Paid` | **Có** | Cùng cơ chế với ghế: điều kiện lệnh theo trạng thái nguồn. Lần thứ hai ảnh hưởng 0 dòng. Bảo vệ thêm bằng ràng buộc duy nhất trên `event.id` (`Payment`) và ràng buộc duy nhất trên `showtime_seat_id` (`Ticketing`). |
| `AwaitingPayment → Expired` | **Có** | Điều kiện lệnh. Lần thứ hai ảnh hưởng 0 dòng. Tác vụ nền quét lô có thể chạy lại vô hạn. |
| `AwaitingPayment → Cancelled` | **Có** | Điều kiện lệnh. Lần thứ hai ảnh hưởng 0 dòng — và lần thứ hai **không** trả ghế về lần nữa (ghế đã `Available` nên lệnh ảnh hưởng 0 dòng). |

### Điều kiện tiên quyết

**Tạo đơn** — xem "Điều kiện tiên quyết" của máy `Showtime seat`: đó là cùng một thao tác.

**Chuyển sang `Paid`**:

- Đơn đang `AwaitingPayment`.
- Ghế tương ứng đang `Held` và thuộc đúng `Seat hold` của đơn.
- Thông báo thanh toán **chưa** được xử lý trước đó (ràng buộc duy nhất trên `event.id`).

**Chuyển sang `Expired`**:

- Đơn đang `AwaitingPayment`.
- Thời điểm hết hạn **đã qua**.

**Chuyển sang `Cancelled`**:

- Đơn đang `AwaitingPayment`.
- Người gọi là **chủ sở hữu** của đơn (`BR-14`, `FR-10`) — kiểm tra ở biên aggregate.

---

## State machine: `Payment intent`

**Đơn vị**: một yêu cầu thanh toán cho một đơn.
Context: `Payment`. Aggregate: `Payment intent`.

> **`Payment intent` không có trạng thái "thất bại".** `INV-04` chốt rằng ta không có trạng
> thái tương đương `requires_payment_method` của Stripe — chỉ có **một** trạng thái "chờ
> thanh toán" cho tới khi có kết quả. Kết quả thất bại (thẻ hết hạn, người dùng hủy giữa
> chừng) **không** tạo ra một trạng thái mới: intent bị **bỏ**, đơn đi theo đường hết hạn
> (`Expired`) hoặc huỷ (`Cancelled`) như bình thường.

### States

| Trạng thái | Nghĩa |
|---|---|
| `AwaitingPayment` | Đã tạo yêu cầu thanh toán, chờ kết quả từ Stripe. |
| `Captured` | Tiền **đã thu**. |
| `Refunded` | Đã hoàn **toàn bộ**. |

> **Tên `Captured`, không phải `Paid`.** `Paid` là trạng thái của `Booking`. Dùng lại ở
> `Payment` sẽ gây nhầm lẫn khi đọc log: "booking `Paid`" và "payment `Paid`" là hai sự
> kiện ở hai context khác nhau, và chúng **không** xảy ra cùng lúc.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Tạo yêu cầu thanh toán | `AwaitingPayment` | Tạo `Payment intent` gắn với **đúng một** đơn (ràng buộc duy nhất trên `booking_id` — một đơn không có hai yêu cầu song song). Ghi mã định danh phía Stripe. |
| `AwaitingPayment` | Thông báo thanh toán thành công | `Captured` | Ghi dòng lịch sử `Payment event` với `event.id`. **Nếu đơn còn `AwaitingPayment`** ⇒ đơn → `Paid`, ghế → `Sold`, cấp vé — cùng transaction. **Nếu đơn đã `Expired`/`Cancelled`** ⇒ **không cấp vé**, chuyển thẳng sang `Refunded`. |
| `AwaitingPayment` | Kết quả thanh toán thất bại | — (bị bỏ) | Intent không còn hiệu lực. Đơn đi theo đường `Expired`/`Cancelled` bình thường. **Không** có trạng thái "thất bại" (`INV-04`). |
| `Captured` | Hoàn tiền | `Refunded` | Hoàn **toàn bộ** số tiền đã thu (`BR-05`, `FR-39`). Mỗi khoản đã thu được hoàn **đúng một lần** (ràng buộc duy nhất: mỗi intent có nhiều nhất một `Refund`). |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| Bất kỳ | Thông báo với `event.id` **đã được xử lý** | **Bỏ qua** — không phải lỗi. Không cấp vé lần hai, không chuyển trạng thái đơn lần hai, không hoàn lần hai (`BR-17`, `FR-38`, `FR-39`). |
| `Captured` | Hoàn tiền lần thứ hai | **Từ chối** — ràng buộc duy nhất chặn. Mỗi khoản đã thu được hoàn **đúng một lần** (`BR-05`). |
| `Refunded` | Hoàn tiền | **Từ chối** — đã hoàn rồi. |
| `Refunded` | Thông báo thanh toán thành công đến lại | **Bỏ qua** — cùng cơ chế `event.id`. Không hoàn thêm lần nữa. |
| `AwaitingPayment` | Hoàn tiền | **Từ chối** — chưa thu được gì thì không có gì để hoàn. |

### Idempotency

Đây là máy trạng thái **quan trọng nhất** về idempotency — vì webhook Stripe đến lặp là
hiện thực, không phải giả định.

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Xử lý thông báo thanh toán (mọi nhánh) | **Có** — cơ chế **mạnh nhất** trong dự án | Ràng buộc **duy nhất** trên `event.id` ở bảng lịch sử `Payment event`. Lần xử lý đầu ghi dòng lịch sử; các lần sau vi phạm ràng buộc ⇒ bị coi là **đã xử lý và bỏ qua**, không phải lỗi. Cơ chế này chặn được cả **hai webhook đến cùng lúc** — mà kiểm tra "đã xử lý chưa" trong bộ nhớ ứng dụng thì không. Xem `INV-01` ở [`aggregates/04-payment.md`](aggregates/04-payment.md). |
| `Captured → Refunded` | **Có** | Ràng buộc duy nhất: mỗi `Payment intent` có **nhiều nhất một** `Refund`. Lần hoàn thứ hai vi phạm ràng buộc ⇒ bị từ chối. Chặn được cả webhook đến lặp lẫn tác vụ nền retry. |
| Tạo intent | **Có** | Ràng buộc duy nhất trên `booking_id` — người dùng bấm "thanh toán" hai lần thì lần thứ hai trả về intent cũ, không tạo intent mới. |

### Điều kiện tiên quyết

**Xử lý thông báo thành công**:

- `event.id` **chưa** từng được xử lý — kiểm tra bằng ràng buộc duy nhất, không bằng đọc rồi
  quyết định.
- Intent đang `AwaitingPayment`.
- Số tiền khớp với tổng tiền của đơn.

**Hoàn tiền**:

- Intent đang `Captured`.
- Chưa có `Refund` nào cho intent này.
- Số tiền hoàn **bằng** số tiền đã thu — không có khái niệm "hoàn một phần" trong mô hình này
  (`INV-03`).

### Điểm giao giữa hai context

Tình huống: người xem trả tiền, nhưng thông báo đến **sau** khi đơn đã hết hạn và ghế đã
trả về cho người khác.

| Bước | Context | Kết quả |
|---|---|---|
| Nhận thông báo thành công | `Payment` | Intent → `Captured` |
| Thấy đơn đã `Expired` | `Booking` | Không hồi sinh — đơn vẫn `Expired` |
| Quyết định | `Booking` + `Payment` | **Không cấp vé** |
| Hành động | `Payment` | **Hoàn toàn bộ**, tự động |

Đây là lý do `Payment` là context riêng: quyết định "có hoàn tiền không" dựa trên **cả** tiền
đã thu **và** trạng thái đơn — và hai thứ đó nằm ở hai context khác nhau.

---

## State machine: `Ticket`

**Đơn vị**: một vé — mã QR gắn với **một** ghế của **một** suất chiếu.
Context: `Ticketing`. Aggregate: `Ticket`.

### States

| Trạng thái | Nghĩa |
|---|---|
| `Issued` | Vé đã được cấp (đơn đã `Paid`). Chưa check-in. |
| `CheckedIn` | Vé đã được quét tại cổng. |

> **Chỉ hai trạng thái.** Vé bị vô hiệu do quản lý rạp hủy suất chiếu (`FR-24`) **không** là
> một trạng thái thứ ba — nó là **tác dụng phụ** của transition hủy suất chiếu. Điều kiện
> "vé này còn dùng được không" được kiểm tra ở **điều kiện tiên quyết** của transition
> check-in, không phải bằng một trạng thái.

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Cấp vé (khi đơn `Paid`) | `Issued` | **Cùng transaction** với đánh dấu đơn "đã cấp vé" + số lượng vé. Mỗi ghế sinh **đúng một** vé (ràng buộc duy nhất trên `showtime_seat_id`). Tạo mã vé ký HMAC. |
| `Issued` | Check-in lần đầu | `CheckedIn` | Ghi **một** dòng ghi nhận check-in: thời điểm, cổng, người quét. Ràng buộc duy nhất trên `ticket_id` — hai nhân viên quét cùng lúc thì chỉ một lượt thành công. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `CheckedIn` | Quét lại (kể cả ở cổng khác) | **Từ chối**, kèm **thời điểm** và **cổng** của lượt check-in đầu tiên (`FR-07`, `BR-10`, `INV-05`). Không ghi thêm dòng ghi nhận nào. |
| `CheckedIn` | Hai nhân viên quét **cùng lúc** | Lượt thắng ghi nhận; lượt còn lại bị từ chối như trên. Ràng buộc duy nhất ở tầng lưu trữ chặn được cả hai tiến trình đồng thời. |
| — | Quét vé của đơn **chưa thanh toán** | **Từ chối**, không có bản ghi check-in (`FR-08`). Thực ra trạng thái này **không tồn tại** — vé chỉ được cấp sau khi đơn `Paid` (`INV-02`). Điều kiện này là **hệ quả**, không phải kiểm tra. |
| — | Quét vé thuộc **suất chiếu khác** | **Từ chối** (`FR-25`). |
| — | Quét vé của suất chiếu người quét **không được phân công** | **Từ chối** (`FR-35`, `BR-15`). |
| `Issued` | Check-in khi suất chiếu **đã bị hủy** | **Từ chối** — vé đã hoàn tiền không còn dùng được (`FR-24`). |
| — | Quét vé không tồn tại / mã không hợp lệ | **Từ chối** — kết quả xác định, không có trường hợp nào là "không rõ" (`FR-25`). |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Check-in | **Không idempotent theo nghĩa "cùng kết quả"** — và đây là **đúng ý** | Lượt quét lại bị **từ chối** kèm thời điểm + cổng lần đầu. Hệ thống **không** ghi dòng thứ hai. Đây là hành vi được yêu cầu (`FR-07`), không phải no-op. Ràng buộc duy nhất trên `ticket_id` ở bảng ghi nhận là cơ chế ép — nó chặn được cả hai tiến trình quét đồng thời. |
| Cấp vé | **Có** | Ràng buộc duy nhất trên `showtime_seat_id` — một ghế không thể có hai vé. Webhook đến lặp không cấp vé lần hai (`BR-17`). |

### Điều kiện tiên quyết

**Cấp vé**:

- Đơn đang `Paid` **và** đã đánh dấu "đã cấp vé" — cả hai lệnh nằm trong **cùng một**
  transaction.
- Ghế chưa có vé nào (ràng buộc duy nhất).

**Check-in**:

- Vé tồn tại và đang `Issued`.
- Suất chiếu của vé **đang diễn ra** — không phải suất khác (`FR-25`).
- Người quét có vai trò `Gate staff` hoặc `Administrator` (`BR-15`, `FR-33`).
- Người quét được **phân công** trực suất chiếu đó (`FR-35`).
- Suất chiếu **chưa bị hủy** — vé đã hoàn tiền không dùng được (`FR-24`).
- Dòng ghi nhận check-in cho vé này **chưa tồn tại** (ràng buộc duy nhất).

---

## State machine: `Showtime`

**Đơn vị**: một phim, một phòng, một giờ.
Context: `Scheduling`. Aggregate: `Showtime`.

### States

| Trạng thái | Nghĩa |
|---|---|
| `NotOnSale` | Chưa mở bán. Người xem không thấy, không đặt được. |
| `OnSale` | Đang bán. Người xem thấy và đặt được. |
| `Cancelled` | Đã hủy. Vé đã bán được hoàn tiền và không còn dùng được. |

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| — | Mở bán | `OnSale` | **Sinh `Showtime seat` cho `Inventory`** theo sơ đồ ghế của phòng. Sinh **all-or-nothing**: tạo hết số ghế phòng, hoặc không tạo ghế nào. **Cùng transaction** với chuyển trạng thái suất chiếu — suất không bao giờ "đang bán" mà thiếu ghế (`INV-02` ở [`aggregates/01-scheduling.md`](aggregates/01-scheduling.md)). |
| `OnSale` | Quản lý rạp hủy suất chiếu | `Cancelled` | **Tác dụng phụ cắt qua ba context** (xem bên dưới): vô hiệu các vé đã bán + **hoàn tiền** từng đơn liên quan. |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|
| `NotOnSale` | Người xem đặt vé | **Từ chối** — suất chưa mở bán không xuất hiện trong danh sách, không có trang chi tiết, và yêu cầu đặt vé bị từ chối (`BR-13`, `FR-34`). |
| `NotOnSale` | Quản lý rạp hủy suất chiếu | **Từ chối** — suất chưa mở bán thì **xoá** được, không cần đi qua transition hủy (không có vé nào, không có tiền nào phải hoàn). |
| `Cancelled` | Mở bán | **Từ chối** — suất đã hủy không mở bán lại được. |
| `Cancelled` | Hủy lần nữa | **Từ chối** — idempotent ở tầng lệnh: trả về suất đã hủy. |
| `OnSale` | Hủy khi **không** phải chủ sở hữu lịch chiếu | **Từ chối** — quản lý rạp chỉ hủy được lịch chiếu của chính họ (`BR-16`, `FR-32`). |

### Idempotency

| Transition | Idempotent? | Cơ chế |
|---|---|---|
| Mở bán | Không cần idempotent riêng | Mở bán một suất đã mở bán là lỗi nghiệp vụ rõ ràng. Nhưng vì sinh ghế là all-or-nothing trong một transaction, chạy lại **không** tạo ra ghế trùng — transaction thứ hai thất bại ở ràng buộc duy nhất và rollback trọn vẹn. |
| Hủy suất chiếu | **Có** | Chuyển trạng thái là điều kiện lệnh (`WHERE status = 'OnSale'`); lần thứ hai ảnh hưởng 0 dòng. Phần **hoàn tiền** idempotent bằng ràng buộc duy nhất "mỗi intent có nhiều nhất một `Refund`" — xem máy `Payment intent`. |

### Điều kiện tiên quyết

**Mở bán**:

- Suất chiếu thuộc **đúng một** phòng và **đúng một** phim (`INV-03`).
- Phòng **không** có suất chiếu chồng nhau (`BR-20`) — điều kiện nằm **ngoài** ranh giới
  aggregate, ép bằng exclusion constraint ở tầng lưu trữ.
- Người thao tác là **chủ sở hữu** lịch chiếu, hoặc là quản trị viên (`BR-16`, `FR-32`).
- Sơ đồ ghế của phòng đã tồn tại.

**Hủy suất chiếu**:

- Suất đang `OnSale`.
- Người thao tác là **chủ sở hữu** lịch chiếu, hoặc là quản trị viên (`BR-16`, `FR-32`).

### Tác dụng phụ của "hủy suất chiếu" — ranh giới bị cắt thứ tư

Hủy suất chiếu kéo theo hai việc ở **hai context khác**:

| Việc | Context | Nội dung |
|---|---|---|
| Vô hiệu vé đã bán | `Ticketing` | Vé của suất này **không còn dùng được** để check-in (`FR-24`). Vì `Ticket` chỉ có hai trạng thái, đây là **điều kiện tiên quyết** của transition check-in, không phải một trạng thái thứ ba. |
| Hoàn tiền | `Payment` | Mỗi đơn đã `Paid` của suất này được hoàn **toàn bộ**, **tự động**, **đúng một lần** (`BR-05`, `FR-39`). |

> **Đây là ranh giới bị cắt thứ tư.** `context-map.md` liệt kê ba chỗ: giữ n ghế (R3), cấp
> vé (R6), hết hạn (R3). Hủy suất chiếu là chỗ thứ tư — và nó **khác** ba chỗ kia ở chỗ nó
> chạy **nhiều đơn** chứ không phải một đơn: một suất chiếu có thể có hàng trăm đơn đã trả
> tiền.
>
> Trong modular monolith, tất cả nằm trong **một** transaction. Nếu tách thành service, việc
> hủy suất chiếu trở thành một thao tác **nhiều bước** — cần outbox + consumer idempotent,
> và phải xử lý riêng trường hợp **đã hoàn một phần thì giữa chừng suất bị hủy**.

---

## Race: giữ ghế hết hạn đúng lúc webhook thanh toán đến

Hai luồng tranh nhau trên **cùng một đơn** và **cùng một ghế**:

- **Luồng A** — tác vụ nền quét lô thấy hold đã quá hạn → chuyển đơn `Expired`, ghế
  `Available`.
- **Luồng B** — webhook Stripe báo thành công → chuyển đơn `Paid`, ghế `Sold`, cấp vé.

### Kết quả mong đợi

**Ai chuyển trạng thái trước thì thắng** — nhưng đơn chỉ đi theo **một chiều**, nên không
bao giờ có hai kết quả mâu thuẫn:

| Thứ tự | Kết quả |
|---|---|
| **Webhook đến trước khi hết hạn** | Đơn → `Paid`, ghế → `Sold`, **cấp vé**. Tác vụ giải phóng hết hạn chạy sau thấy đơn **không còn** `AwaitingPayment` → **không làm gì**. |
| **Hết hạn trước** | Đơn → `Expired`, ghế → `Available`. Webhook đến sau thấy đơn `Expired` → **không cấp vé**, **hoàn toàn bộ** tiền (`BR-05`, `FR-06`, `FR-39`). |

### Cơ chế

Cả hai transition đều là **điều kiện lệnh** theo trạng thái nguồn:

- Luồng A: `UPDATE ... WHERE booking.status = 'AwaitingPayment'`
- Luồng B: `UPDATE ... WHERE booking.status = 'AwaitingPayment'`

⇒ **Chỉ một lệnh ảnh hưởng 1 dòng**, lệnh còn lại ảnh hưởng 0 dòng và phải được coi là
thất bại (rồi xử lý theo nhánh tương ứng). Không cần khoá đọc-sửa-ghi, không có cửa sổ
TOCTOU.

### Vì sao không cần prototype

Câu hỏi này **trả lời được trên giấy** — vì nó không phải bài toán "hệ thống biết gì", mà là
bài toán "ai đến trước". Cả hai luồng đều ghi vào **cùng một trạng thái nguồn** của cùng
một aggregate, nên chỉ một lệnh thắng, và nhánh "thua" được định nghĩa sẵn. Không có tình
huống nào mà kết quả phụ thuộc vào thứ tự **đọc**.

Cơ chế tương tự đã được dùng ở T07 cho `BR-01` (điều kiện lệnh thay khoá đọc-sửa-ghi), nên
đây không phải một quyết định mới.

> **Điều kiện để hệ thống trả lời được:** cả hai luồng phải dùng **cùng một** điều kiện
> lệnh trên **cùng một** cột trạng thái. Nếu một luồng đọc trạng thái rồi mới quyết định
> (kiểu "đọc-sửa-ghi"), cửa sổ TOCTOU mở ra và bài toán này trở nên không chốt được trên
> giấy. Chi tiết cơ chế ở Lớp 3 (T12).

---

## Bảng truy vết: `FR-nn` → transition

Mỗi requirement liên quan tới vòng đời phải có **ít nhất một** transition tương ứng.

| FR | Transition | Máy trạng thái | Traces to |
|---|---|---|---|
| `FR-01` | `Available → Held` (điều kiện lệnh trạng thái nguồn) | `Showtime seat` | `BR-01` |
| `FR-02` | `Hold seats` — all-or-nothing trong một transaction | `Showtime seat` | `BR-02` |
| `FR-03` | `Hold seats` — điều kiện lệnh, không khoá đọc-sửa-ghi | `Showtime seat` | `BR-22` |
| `FR-04` | `Held → Available` (tác vụ nền quét lô) | `Showtime seat` | `BR-03` |
| `FR-05` | Cấp vé — chỉ sau khi đơn `Paid` | `Ticket` | `BR-09` |
| `FR-06` | `Expired` — không hồi sinh, kết quả thanh toán đến sau bị từ chối | `Booking` | `BR-04` |
| `FR-07` | `CheckedIn` — quét lại bị từ chối kèm thời điểm + cổng | `Ticket` | `BR-10` |
| `FR-14` | `Hold seats` — tạo bản ghi `Seat hold` với thời điểm hết hạn | `Showtime seat` | `BR-03` |
| `FR-15` | `Available → Held` — kiểm tra lại tại lúc giữ, nói rõ ghế nào đã có người | `Showtime seat` | `BR-01` |
| `FR-16` | `AwaitingPayment → Paid` — cấp vé, mỗi ghế một vé, giá tính theo từng ghế | `Booking` | `BR-09`, `BR-12` |
| `FR-18` | `AwaitingPayment → Cancelled` — trả ghế về ngay | `Booking` | `BR-21` |
| `FR-20` | `Available → Blocked` — quản lý rạp chặn ghế | `Showtime seat` | `BR-11` |
| `FR-22` | `NotOnSale → OnSale` — sinh ghế all-or-nothing | `Showtime` | `BR-13` |
| `FR-24` | `OnSale → Cancelled` — vô hiệu vé + hoàn tiền | `Showtime` | `BR-05`, `BR-16` |
| `FR-25` | `Issued → CheckedIn` — 5 kết quả xác định, không có "không rõ" | `Ticket` | `BR-10`, `BR-15` |
| `FR-27` | `Issued → CheckedIn` — mỗi lượt thành công được đếm | `Ticket` | `BR-25` |
| `FR-34` | `NotOnSale` — yêu cầu đặt vé bị từ chối | `Showtime` | `BR-13` |
| `FR-37` | Tạo đơn — ràng buộc duy nhất trên định danh yêu cầu | `Booking` | `BR-18` |
| `FR-38` | Xử lý thông báo thanh toán — ràng buộc duy nhất trên `event.id` | `Payment intent` | `BR-17` |
| `FR-39` | `Captured → Refunded` — hoàn toàn bộ, đúng một lần | `Payment intent` | `BR-05` |

### `FR` không có transition riêng

Một số requirement là **hệ quả** của các transition đã có, không cần một transition riêng.
Việc "không có transition" cũng là một câu trả lời cần viết ra — không phải một chỗ trống.

| FR | Vì sao |
|---|---|
| `FR-08` | Quét vé của đơn chưa thanh toán — nhưng **không tồn tại** vé nào cho đơn chưa thanh toán, nên không có transition nào để thực hiện. Đây là hệ quả của `FR-05`, không phải một quy tắc riêng cần ép. |
| `FR-19` | Thông báo nhắc hết hạn là **tác dụng phụ** của việc có `expires_at`, không phải transition. |

---

## Ranh giới bị cắt — bốn chỗ

`context-map.md` liệt kê ba chỗ một bất biến bị cắt ngang ranh giới context. T08 bổ sung
chỗ thứ tư.

| # | Bất biến bị cắt | Cắt ở đâu | Transition liên quan | Hậu quả nếu tách service |
|---|---|---|---|---|
| 1 | "Giữ n ghế: đủ n hoặc không ghế nào" + "ghế `Held` luôn có thời điểm hết hạn" | R3 — lệnh giữ ghế ghi cả `Inventory` và `Booking` | `Hold seats` | Lệnh giữ ghế trở thành nhiều lượt gọi có điều phối |
| 2 | "Cấp vé đúng một lần" | R6 — cấp vé atomic với đánh dấu "đơn đã cấp vé" | `AwaitingPayment → Paid` | Phải có outbox + idempotent consumer |
| 3 | "Hết hạn: giải phóng ghế + chuyển đơn sang `Expired` cùng lúc" | R3 — tác vụ nền giải phóng hết hạn | `AwaitingPayment → Expired` | Phải có compensating: giải phóng ghế xong nhưng đơn chưa chuyển |
| **4** | **"Hủy suất chiếu: vô hiệu vé + hoàn tiền mọi đơn liên quan"** | **R1 + R6 + R4 — cắt qua ba context** | **`OnSale → Cancelled`** | **Phải có outbox + consumer idempotent; và phải xử lý riêng trường hợp đã hoàn một phần thì giữa chừng suất bị hủy** |

> **Chỗ thứ tư khác ba chỗ kia ở chỗ nó chạy *nhiều đơn*.** Chỗ 1–3 đều là hành vi trên **một**
> aggregate. Hủy suất chiếu là hành vi trên **tập** các đơn `Paid` của suất đó — một suất
> chiếu có thể có hàng trăm đơn. Khi tách service, đây không còn là "một lệnh phải chia
> thành nhiều bước", mà là **một lệnh sinh ra N lượt**, mỗi lượt phải idempotent.

---

## Câu hỏi còn mở

| # | Câu hỏi | Chốt ở |
|---|---|---|
| Q25 | `Held` của ghế có cần **trạng thái riêng** cho "đang giữ nhưng đã quá hạn chưa xử lý" không? Hiện tại `Held` được giải phóng bằng tác vụ nền quét lô, nên có một khoảng thời gian ghế `Held` đã quá hạn nhưng chưa được giải phóng. | T12, T21 |
| Q26 | Một `Booking` có cần trạng thái **"đã cấp vé"** riêng, hay cờ đó chỉ là thuộc tính bên trong trạng thái `Paid`? Hiện tại là cờ + số lượng vé, theo `INV-04` của `Booking`. | T11 |
| Q27 | Khi hủy suất chiếu, đơn `Paid` của suất đó có cần **trạng thái riêng** (ví dư `Refunded`) để phản ánh "đã hoàn tiền", hay thuộc tính hoàn tiền của `Payment` là đủ? | T09, T11 |
| Q28 | Trạng thái "chờ kết quả" của `Payment intent` dùng chung tên `AwaitingPayment` với `Booking` — hai thứ khác nhau nhưng cùng tên. Có nên đổi tên để tránh nhầm khi đọc log? | T11 |
| Q29 | Hủy suất chiếu chạy **N đơn** — gộp tất cả vào **một** transaction, hay chia thành N transaction riêng để tránh transaction quá dài? | T10, T12 |

## Trạng thái

Bản làm việc. Sau T08, `docs/domain/state-machines.md` đã có. Tiếp theo: T09 — cross-context
contracts.
