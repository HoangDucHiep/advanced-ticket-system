# Ubiquitous language

Lớp 2 — Domain. Bản làm việc của thuật ngữ.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/05-ubiquitous-language.md`.

## Cách đọc

Mỗi thuật ngữ có **tên tiếng Việt** và **tên tiếng Anh**. Tên tiếng Anh là **tên chuẩn dùng trong
code** — tên class, biến, bảng, trạng thái. Tên tiếng Việt chỉ dùng để trao đổi trong tài liệu
và thảo luận.

Mỗi thuật ngữ có định nghĩa và danh sách từ **nên tránh**. Những thuật ngữ ở đây được dùng
xuyên suốt các tài liệu về sau, và sẽ được gom vào `GLOSSARY.md` ở cuối Gate B.

### Quy tắc đặt tên trong code

- Dùng **đúng** tên tiếng Anh trong glossary này cho tên class, tên biến, tên bảng, tên trạng
  thái.
- **Không** dùng tên tiếng Việt trong code, kể cả bản transliterate không dấu (`Ghe`, `DonDatVe`).
- Khi một từ tiếng Anh trùng nghĩa với thuật ngữ khác, dùng tên trong glossary và ghi từ đó
  vào `_Avoid_`.

## Nhóm 1 — Bốn thứ dễ nhầm

**Ghế (của phòng) / Room seat**:
Một vị trí ngồi trong phòng chiếu. Thuộc về phòng, không đổi, không bao giờ được bán.
_Avoid_: `ghế chỗ`, `chỗ ngồi`, `Seat` khi nói về phòng

**Ghế suất chiếu / Showtime seat**:
Cùng một cái ghế vật lý, nhưng ở một suất chiếu cụ thể. Đây là đơn vị bán thực sự.
_Avoid_: `chỗ ngồi`, `Seat` khi nói về suất chiếu, `Ticket` (vé là mã QR, không phải ghế)

**Vé / Ticket**:
Một mã QR gắn với **một ghế suất chiếu**. Chỉ tồn tại sau khi đơn đã được thanh toán.
_Avoid_: `coupon`, `mã QR`, `vé điện tử`, `Seat` (vé không phải ghế)

**Đơn đặt vé / Booking**:
Một lần đặt của một người xem, thuộc **một suất chiếu**, chứa **nhiều ghế**.
_Avoid_: `đơn hàng`, `hóa đơn`, `Order`, `Purchase`, `Transaction`
## Nhóm 2 — Trạng thái và cách đếm

Một ghế suất chiếu có **một** trong bốn trạng thái. Đây là tất cả các trạng thái — không có
trạng thái thứ năm.

**Trống / Available**:
Ghế chưa ai dùng. Người xem có thể đặt.
_Avoid_: `free`, `empty`, `SeatAvailable`

**Đang giữ / Held**:
Ghế đang được giữ tạm cho một người xem, kèm thời điểm hết hạn. Chưa bán, chưa có vé.
_Avoid_: `Reserved` (dễ nhầm với `Blocked`), `Pending`, `Locked`

**Đã bán / Sold**:
Ghế đã được bán. Đã thanh toán, đã có vé.
_Avoid_: `Booked`, `Confirmed`, `Purchased`

**Bị chặn / Blocked**:
Ghế không bán online (vệ sinh, dành riêng). Vẫn tính vào tổng số ghế của phòng.
_Avoid_: `Reserved` (dễ nhầm với `Held`), `Disabled`, `Unavailable`

### Ba cách đếm

Cùng một suất chiếu, có **ba** con số khác nhau. Nhầm ba con số này là nguồn của bug.

**Tổng số ghế phòng / Room seat count**:
Số ghế có trong sơ đồ của phòng. Không phụ thuộc trạng thái.
_Avoid_: `sức chứa`, `capacity`, `số chỗ`, `TotalSeats` (nghĩa là cấu hình, không phải đếm)

**Số ghế còn trống / Available seat count**:
Chỉ đếm ghế ở trạng thái `Available`. Đây là con số **người xem** thấy ("còn N ghế") **và** là một
trong bốn con số của quản lý rạp.
_Avoid_: `FreeSeats`, `chỗ trống`, `Remaining`

**Số ghế bán online / Sellable seat count**:
Tổng số ghế phòng **trừ** ghế `Blocked` (= `Available` + `Held` + `Sold`). Là **kích thước sơ đồ**
người xem thấy (ghế bị chặn không hiện), **không** phải số ghế còn đặt được — con số này gần như
không đổi trong suốt đợt bán.
_Avoid_: `còn chỗ`, `Capacity`

> **Đã sửa (2026-11-25):** bản trước ghi `Sellable seat count` là "con số người xem thấy". Nhưng
> `Available + Held + Sold` là hằng số trong suốt đợt bán (ghế chỉ chuyển qua lại giữa ba nhóm này),
> nên không cho người xem biết gì. Người xem cần `Available seat count`.

## Nhóm 3 — Loại ghế, giá, suất chiếu, phim

**Loại ghế / Seat type**:
Nhóm ghế trong một phòng, phân biệt theo vị trí và giá: `Standard` / `Couple` / `VIP`. Loại ghế
mang **giá**, không mang **số lượng**.
_Avoid_: `hạng vé`, `tier`, `hạng`, `cap`, `dung lượng`, `loại giá`, `SeatClass`

**Giá vé / Ticket price**:
Thuộc về cặp **(loại ghế, suất chiếu)** — không thuộc về ghế cụ thể, không thuộc về phòng. Hai
suất của cùng một phim có thể có giá khác nhau.
_Avoid_: `giá ghế`, `biểu giá`, `tarif`, `SeatPrice` (giá thuộc về cặp, không thuộc về ghế)

**Suất chiếu / Showtime**:
Một phim, một phòng, một khoảng giờ. Mỗi suất chiếu có **sơ đồ ghế riêng** về mặt trạng thái, dù
dùng chung sơ đồ vật lý của phòng. Trạng thái: `NotOnSale` (chưa mở bán) / `OnSale` (đang bán) /
`Cancelled` (đã hủy). Bán vé tới **giờ bắt đầu chiếu** thì dừng.
_Avoid_: `screening`, `lịch chiếu`, `buổi chiếu`, `Schedule` (lịch chiếu là danh sách suất)

**Phim / Movie**:
Một tác phẩm. Một phim có nhiều suất chiếu.
_Avoid_: `film`, `Title`

## Nhóm 4 — Vòng đời đơn đặt vé

**Giữ ghế / Hold seats**:
Hành động giữ tạm một nhóm ghế cho một người xem, kèm thời điểm hết hạn. Nếu không thanh toán
trong 15 phút, ghế được giải phóng tự động.
_Avoid_: `reserve`, `lock`, `cart`, `Hold` (đó là bản ghi, không phải hành động)

**Thông tin giữ / Seat hold**:
Thông tin nằm **trên** một `Showtime seat` đang `Held`: tham chiếu giữ (`hold_id` — hiện là id
đơn) và thời điểm hết hạn giữ (`held_until` — bản sao `expires_at` của đơn). **Không** phải một
aggregate hay một bản ghi riêng (ADR-0005).
_Avoid_: `reservation`, `HoldRecord`, `SeatHold` (như tên aggregate/bảng)

**Chờ thanh toán / AwaitingPayment**:
Trạng thái của đơn đã giữ ghế, chưa thanh toán, **chưa quá** `expires_at`.
_Avoid_: `Pending` (dễ nhầm với `Held`), `Open`

**Thời điểm hết hạn / Expires at**:
Mốc 15 phút kể từ lúc giữ ghế, thuộc `Booking`. Quá mốc này đơn **không thể** được thanh toán nữa —
bất kể hệ thống đã kịp chuyển nó sang `Expired` hay chưa.
_Avoid_: `deadline`, `TTL` (khái niệm kỹ thuật)

**Hết hạn / Expired**:
Trạng thái của một đơn đã qua `expires_at` mà chưa thanh toán. Ghế trả về, **không có vé**, và
**không hồi sinh** dù thông báo kết quả thanh toán đến sau (khi đó tiền được hoàn).
_Avoid_: `timeout`, `Cancelled` (khác nguyên nhân), `Voided`

**Đã hủy / Cancelled** (của `Booking`):
Trạng thái của đơn bị huỷ khi chưa thanh toán — do **người xem** chủ động huỷ, hoặc do **suất chiếu
bị hủy**. Nguyên nhân ghi trong lịch sử đơn.
_Avoid_: `voided`, `aborted`, `Expired` (khác nguyên nhân)

**Thanh toán thành công / Paid**:
Trạng thái của đơn đã được trả tiền **trước** `expires_at`. Đơn `Paid` **luôn** đã có đủ vé — cấp vé
nằm trong cùng thay đổi.
_Avoid_: `đã thanh toán`, `confirmed`, `completed`, `TicketsIssued`, `PaymentSucceeded` (là sự kiện)

## Nhóm 5 — Người, vai trò, và hành động tại chỗ

**Người xem / Customer**:
Khách xem phim. Đã đăng nhập để quản lý đơn và vé của mình.
_Avoid_: `người dùng`, `user`, `khách`, `Viewer`

**Quản lý rạp / Theater manager**:
Bên quản lý phòng chiếu, lịch chiếu và sơ đồ ghế. Chỉ sửa được lịch chiếu **do chính họ tạo**.
_Avoid_: `quản lý`, `owner`, `staff`, `admin`, `Manager` (quá chung)

**Nhân viên cổng / Gate staff**:
Người trực cổng kiểm tra vé khi khách vào rạp. Chỉ ghi nhận check-in được.
_Avoid_: `employee`, `doorman`, `Staff`

**Quản trị viên / Administrator**:
Người quản lý toàn hệ thống. Không phải quản lý rạp cụ thể nào.
_Avoid_: `admin`, `superadmin`, `root`

**Check-in / Check-in**:
Ghi nhận một vé đã được quét tại cổng. Một vé chỉ check-in **một lần**.
_Avoid_: `scan`, `quét vé`, `validate`, `redeem`, `Scan` (là hành động quét, không phải trạng thái)

## Nhóm 6 — Nhóm ba con số và trạng thái liên quan

**Ghế đôi / Couple seat type**:
Loại ghế cho hai người. Khi đếm giới hạn số lượng, tính là **2 ghế**; nhưng sinh ra **1 vé**.
_Avoid_: `double seat`, `loveseat`, `DoubleSeat`

**Tổng tiền của đơn / Booking total**:
Tổng giá của từng ghế trong đơn. Không phải một giá chung cho cả đơn.
_Avoid_: `order total`, `tổng giá đơn`, `TotalPrice` (giá của một ghế, không phải của cả đơn)

**Dòng đơn / Booking line**:
Một ghế trong đơn, kèm loại ghế và **giá đã chốt** tại lúc giữ.
_Avoid_: `OrderItem`, `chi tiết đơn`

**Định danh yêu cầu / Idempotency key**:
Định danh do **client** sinh cho một lần bấm "giữ ghế". Gửi lại cùng định danh ⇒ cùng kết quả với
lần đầu, kể cả khi lần đầu bị từ chối.
_Avoid_: `request_id` (dễ nhầm với id truy vết), `nonce`

## Nhóm 7 — Tiền

**Yêu cầu thanh toán / Payment intent**:
Yêu cầu thu tiền cho **đúng một** đơn. Một đơn có đúng một yêu cầu; thanh toán thất bại thì thử
lại bằng **cùng** yêu cầu.
_Avoid_: `payment`, `charge`, `transaction`

**Thông báo thanh toán / Payment event**:
Một thông báo kết quả từ cổng thanh toán (thành công, thất bại, đã hoàn), có định danh riêng. Mỗi
thông báo chỉ được xử lý **một lần**.
_Avoid_: `webhook` (là cách gửi, không phải khái niệm), `callback`

**Đã thu / Captured**:
Trạng thái của `Payment intent` khi tiền đã về và đơn đã `Paid`.
_Avoid_: `Paid` (đó là của `Booking`), `Succeeded`

**Chờ hoàn / RefundPending**:
Tiền đã thu nhưng **phải hoàn** (đơn hết hạn/huỷ trước khi tiền về, hoặc suất bị hủy); yêu cầu hoàn
đã được ghi, chưa có xác nhận từ cổng thanh toán.
_Avoid_: `Refunding`, `PendingRefund`

**Đã hoàn / Refunded**:
Trạng thái của `Payment intent` khi đã hoàn **toàn bộ**.
_Avoid_: `Reversed`, `Chargeback` (khác nghĩa)

**Hoàn tiền / Refund**:
Một lần hoàn **toàn bộ** số đã thu. Mỗi `Payment intent` có nhiều nhất một.
_Avoid_: `partial refund` (không tồn tại trong mô hình)

## Nhóm 8 — Vé, cổng

**Đã cấp / Issued**:
Trạng thái của vé vừa được cấp, chưa check-in.
_Avoid_: `Active`, `Valid`

**Đã check-in / CheckedIn**:
Trạng thái của vé đã được quét thành công tại cổng.
_Avoid_: `Used`, `Redeemed`

**Cổng / Gate**:
**Nơi quét** vé (nhãn thiết bị/vị trí), ghi lại trong bản ghi check-in. **Không** phải đối tượng
được phân công.
_Avoid_: `Door`, `Entrance`

**Phân công cổng / Gate assignment**:
Một **nhân viên cổng** được phân công trực một **suất chiếu**. Thuộc `Ticketing`. Quyết định ai được
check-in vé của suất nào.
_Avoid_: `Shift`, `Roster`

## Danh từ và động từ của cùng một thứ

Hai thứ được gọi cùng một tên khi chúng **cùng một ngữ nghĩa** — một cái là danh tính, cái
khác là hành động. Dùng nhầm hai cái là nguồn bug:

| Hành động (EN) | Kết quả (EN) |
|---|---|
| `Hold seats` | tạo một `Booking` ở `AwaitingPayment` và gắn **thông tin giữ** lên n `Showtime seat` |
| `Pay` | đổi `Booking` thành `Paid`, ghế thành `Sold`, và tạo ra các `Ticket` |

## Các cặp thuật ngữ dễ nhầm

### `Ghế (của phòng)` / `Room seat` vs `Ghế suất chiếu` / `Showtime seat`

Dùng chung một từ "ghế" cho cả hai là **nguồn nhầm lớn nhất** của dự án.

`Room seat` thuộc về phòng, tĩnh, và dùng chung cho mọi suất chiếu. `Showtime seat` thuộc về
một suất chiếu cụ thể, có trạng thái riêng, và là đơn vị bán.

Hai câu hỏi phải luôn trả lời **khác nhau**:

| Câu hỏi | Trả lời bằng |
|---|---|
| "Phòng này có bao nhiêu ghế?" | đếm `Room seat` của phòng |
| "Suất chiếu này còn ghế nào trống?" | xem `Showtime seat` của suất đó |

Hậu quả khi nhầm: một người mua `Showtime seat` D7 ở suất 19h sẽ **không** chặn người
khác mua D7 ở suất 21h. Nếu dùng một từ "ghế" cho cả hai, người đọc sẽ tự hiểu là một người
đã mua D7 thì D7 hết ở mọi suất — và hệ thống sẽ báo lỗi sai.

### `Trống` / `Available` vs `Còn bán online` / `Sellable`

Một ghế `Held` **không** phải `Available`, và người xem khác **không** đặt được.

`Available seat count` là số ghế **còn đặt được** — người xem và quản lý rạp đều thấy. `Sellable
seat count` là **kích thước sơ đồ** (tổng trừ ghế bị chặn) — ghế đang giữ **thuộc** con số này,
nhưng không thuộc `Available seat count`.

### `Hết hạn` / `Expired` vs `Đã hủy` / `Cancelled`

Hai trạng thái khác nhau của `Booking`, và khác nhau ở **hậu quả**:

- `Expired`: do thời điểm hết hạn đi qua.
- `Cancelled`: do người xem chủ động hủy, hoặc do suất chiếu bị hủy.

Cả hai đều không có vé, đều trả ghế về. Nhưng `Expired` có thêm một tính chất: **không hồi
sinh** — thông báo kết quả thanh toán đến sau không đưa được đơn về lại trạng thái đang chờ.

### `Vé` / `Ticket` vs `Thông tin giữ` / `Seat hold`

`Seat hold` là **tiền chưa thu**, `Ticket` là **tiền đã thu**. `Ticket` không tồn tại trước khi
đơn đã thanh toán. Một người xem giữ 4 ghế mà không thanh toán có **0 vé**, không phải 4 vé.

## Ranh giới aggregate: phần giảng trong buổi nay

> **Đã chuyển (2026-11-21).** Phần giảng này được ghi lại ở
> [`aggregates/README.md`](aggregates/README.md) kèm quyết định chính thức. Mục dưới đây giữ
> lại như **ghi chú buổi học** — nó giải thích *tại sao* câu hỏi ranh giới được đặt ra, không
> phải kết quả chốt.

Ranh giới aggregate **là** transaction boundary **là** phạm vi của một lệnh SQL atomic. Chốt
được ranh giới ở T07 thì Lớp 3 gần như chỉ còn là hệ quả kỹ thu.

### Ba dạng thứ DDD phân biệt

| | Có định danh? | Có vòng đời? | Ví dụ ở đây |
|---|---|---|---|
| **Entity** | Có | Có | `Showtime seat` D7 của suất 19h — là "cái ghế đó" qua mọi thay đổi trạng thái, dù đã được giữ rồi bán rồi check-in |
| **Value Object** | Không, chỉ có **giá trị** | Không | `Seat type Standard giá 90k trong suất 19h` — đổi giá là tạo VO mới, không phải sửa. `Ticket price` cũng là VO |
| **Aggregate** | — | — | Entity gốc + những thứ bảo vệ **cùng nhau** |

Value Object hữu ích vì **bất biến** và **so sánh bằng giá trị**: hai `Seat type Standard giá
90k` là bằng nhau, không phải "cùng một cái".

### Câu hỏi quyết định ranh giới

> **"Có thay đổi nào mà nếu chỉ một phần được thay đổi thì hệ thống thành sai không?"**

Áp dụng vào thao tác **giữ ghế** (`Hold seats`) — một thao tác làm hai việc cùng lúc:

1. `Showtime seat` D7 chuyển `Available` → `Held`
2. `Booking` chuyển sang trạng thái đang chờ thanh toán, kèm 4 ghế trong danh sách

- Chỉ (1) xảy ra: một ghế bị giữ mà không ai chịu trách nhiệm, không ai trả tiền, không ai có
  vé. Treo tới hết đời.
- Chỉ (2) xảy ra: một đơn chờ thanh toán cho 4 ghế mà không ai giữ. Người khác đặt được 4 ghế
  đó, rồi người xem trả tiền và nhận vé cho ghế **đã bán cho người khác**.

Cả hai đều sai. Vậy nên: **ghế và đơn phải thay đổi cùng lúc, không thể tách** → chung một ranh
giới.

### Vậy ranh giới nằm ở giữa "giữ ghế" và "thanh toán"

`Hold seats` (lúc người xem bấm xác nhận) và `Pay` (sau đó, qua cổng thanh toán bên ngoài)
**không phải một việc**. Thanh toán có thể đến vài giây, vài phút, thậm chí vài giờ sau. Nếu gộp
chúng làm một aggregate, một transaction sẽ phải mở suốt thời gian chờ người xem bấm "thanh
toán" — không thể.

Ranh giới aggregate nằm ở giữa hai điểm đó. Phần trước (giữ ghế + trạng thái ghế + đơn) là
một khối. Phần sau (thanh toán) là khối khác.

### Câu hỏi đã trả lời ở T07

> **Một suất chiếu có hàng trăm ghế — có nên là một aggregate không?**

Nếu tất cả ghế của suất chiếu nằm trong cùng aggregate với đơn, đó là hàng trăm dòng bị khoá
trong một transaction. Đây là câu hỏi lớn nhất của Gate B. Xem
`docs/adr/0003-seat-based-inventory.md`.

**Đã trả lời: KHÔNG.** Ban đầu (2026-11-21) chọn `Seat hold` làm aggregate root chứa ≤ 6 ghế;
sửa lại 2026-11-25: **mỗi `Showtime seat` là một aggregate root**, all-or-nothing do một DB
transaction bảo đảm (ADR-0005). Lý do đầy đủ: [`aggregates/README.md`](aggregates/README.md).

> Lưu ý câu ở đầu mục này — "ranh giới aggregate **là** transaction boundary" — là cách nói của buổi
> học đầu, và **không chính xác**: aggregate là đơn vị nhất quán nhỏ nhất; một DB transaction có thể
> chạm nhiều aggregate khi được ghi rõ.

## Thuật ngữ mà không dùng

| Từ | Vì sao không dùng |
|---|---|
| `đơn hàng`, `hóa đơn`, `Order`, `Purchase` | `Booking` không phải đơn hàng — nó không có hàng hoá, không có giao hàng, không có thuế |
| `hạng vé`, `tier`, `hạng`, `cap`, `SeatClass` | Đã bị thay thế bởi ADR-0003. `Seat type` không mang số lượng, chỉ mang giá |
| `sức chứa`, `capacity`, `TotalSeats` (nghĩa cấu hình) | Một suất chiếu không có "sức chứa" được cấu hình. Số vé của nó = đếm số dòng ghế |
| `Ticket` (nghĩa là chỗ ngồi) | Đã dùng `Ticket` = mã QR. Chỗ ngồi là `Showtime seat` |
| `transaction` | Là khái niệm kỹ thu (Lớp 3), không phải khái niệm nghiệp vụ (Lớp 2) |

## Bảng tra nhanh: tiếng Việt → tiếng Anh (dùng trong code)

| Tiếng Việt | Tiếng Anh (chuẩn trong code) | Ghi chú |
|---|---|---|
| Ghế (của phòng) | `Room seat` | Sơ đồ vật lý của phòng |
| Ghế suất chiếu | `Showtime seat` | Đơn vị bán |
| Vé | `Ticket` | Mã QR |
| Đơn đặt vé | `Booking` | |
| Trống | `Available` | Trạng thái ghế |
| Đang giữ | `Held` | Trạng thái ghế |
| Đã bán | `Sold` | Trạng thái ghế |
| Bị chặn | `Blocked` | Trạng thái ghế |
| Tổng số ghế phòng | `Room seat count` | |
| Số ghế còn trống | `Available seat count` | Người xem và quản lý rạp |
| Số ghế bán online | `Sellable seat count` | Kích thước sơ đồ người xem thấy |
| Loại ghế | `Seat type` | `Standard` / `Couple` / `VIP` |
| Giá vé | `Ticket price` | Thuộc (seat type, showtime) |
| Suất chiếu | `Showtime` | |
| Phim | `Movie` | |
| Giữ ghế | `Hold seats` | Hành động |
| Thông tin giữ | `Seat hold` | `hold_id` + `held_until` trên ghế |
| Chờ thanh toán | `AwaitingPayment` | Trạng thái booking |
| Thời điểm hết hạn | `Expires at` | Thuộc booking |
| Hết hạn | `Expired` | Trạng thái booking |
| Đã hủy | `Cancelled` | Trạng thái booking (và showtime) |
| Thanh toán thành công | `Paid` | Trạng thái booking |
| Dòng đơn | `Booking line` | |
| Định danh yêu cầu | `Idempotency key` | |
| Yêu cầu thanh toán | `Payment intent` | |
| Thông báo thanh toán | `Payment event` | |
| Đã thu / Chờ hoàn / Đã hoàn | `Captured` / `RefundPending` / `Refunded` | Trạng thái payment intent |
| Hoàn tiền | `Refund` | |
| Chưa mở bán / Đang bán | `NotOnSale` / `OnSale` | Trạng thái showtime |
| Đã cấp / Đã check-in | `Issued` / `CheckedIn` | Trạng thái ticket |
| Cổng | `Gate` | Nơi quét |
| Phân công cổng | `Gate assignment` | Nhân viên ↔ suất chiếu |
| Người xem | `Customer` | |
| Quản lý rạp | `Theater manager` | |
| Nhân viên cổng | `Gate staff` | |
| Quản trị viên | `Administrator` | |
| Check-in | `Check-in` | |
| Ghế đôi | `Couple seat type` | |
| Tổng tiền của đơn | `Booking total` | |

### Gợi ý đặt tên (chưa chốt — sẽ chốt ở T07/T11)

| Khái niệm | Gợi ý tên | |
|---|---|---|
| Ghế của một suất chiếu | `ShowtimeSeat` | Aggregate root (ADR-0005) |
| Ghế của phòng | `RoomSeat` | Entity trong aggregate `Room` |
| Đơn đặt vé | `Booking` | Aggregate root |
| Vé | `Ticket` | Aggregate root |
| Phân công cổng | `GateAssignment` | Aggregate root |

T11 (data model) chốt tên bảng.

## Mâu thuẫn đã phát hiện và sửa

### FR-23 vs BR-11 — số ghế bị chặn

`FR-23` nói ba con số (trống / đang giữ / đã bán) cộng lại bằng tổng số ghế phòng. `BR-11` nói
ghế bị chặn vẫn tính vào tổng số ghế phòng. Hai câu không cùng đúng — **đã sửa `FR-23` thành
bốn con số**, kèm ghi chú phân biệt con số người xem thấy (`FR-13`).

Bài học: khi chốt một quyết định ở ticket sau, **quay lại kiểm tra các ticket trước** có
còn đúng không. Một quyết định được ghi thêm ở một nơi mà không sửa ở nơi khác tạo ra một
mâu thuẫn chỉ lộ ra khi ai đó đọc cả hai.

## Trạng thái

Bản làm việc. Cập nhật 2026-11-25 sau review: thêm nhóm 7 (tiền), nhóm 8 (vé, cổng), các trạng
thái từ T08; sửa `Seat hold`, `Sellable seat count`, `Cancelled`, `Paid`. Sẽ gom vào `GLOSSARY.md`
khi đóng Gate B.
