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
Chỉ đếm ghế ở trạng thái `Available`.
_Avoid_: `FreeSeats`, `chỗ trống`

**Số ghế còn bán online / Sellable seat count**:
Đếm `Available` + `Held` + `Sold`, **không** tính `Blocked`. Đây là con số **người xem** thấy
trên sơ đồ.
_Avoid_: `Remaining`, `còn chỗ`, `AvailableSeatCount` (đó là con số của quản lý rạp)

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
Một phim, một phòng, một giờ. Mỗi suất chiếu có **sơ đồ ghế riêng** về mặt trạng thái, dù
dùng chung sơ đồ vật lý của phòng.
_Avoid_: `screening`, `lịch chiếu`, `buổi chiếu`, `Schedule` (lịch chiếu là danh sách suất)

**Phim / Movie**:
Một tác phẩm. Một phim có nhiều suất chiếu.
_Avoid_: `film`, `Title`

## Nhóm 4 — Vòng đời đơn đặt vé

**Giữ ghế / Hold seats**:
Hành động giữ tạm một nhóm ghế cho một người xem, kèm thời điểm hết hạn. Nếu không thanh toán
trong 15 phút, ghế được giải phóng tự động.
_Avoid_: `reserve`, `lock`, `cart`, `Hold` (đó là bản ghi, không phải hành động)

**Chỗ giữ ghế / Seat hold**:
Một bản ghi đang giữ: ghế nào, của đơn nào, hết hạn lúc nào. Có **thời điểm hết hạn**.
_Avoid_: `reservation`, `HoldRecord`

**Hết hạn / Expired**:
Trạng thái của một đơn khi qua thời điểm hết hạn mà chưa thanh toán. Coi như đã hủy, ghế trả về,
**không có vé**, và **không hồi sinh** dù thông báo kết quả thanh toán đến sau.
_Avoid_: `timeout`, `Cancelled` (khác nguyên nhân), `Voided`

**Đã hủy / Cancelled**:
Trạng thái của đơn bị người xem hủy ngay (chưa thanh toán).
_Avoid_: `voided`, `aborted`, `Expired` (khác nguyên nhân)

**Thanh toán thành công / Paid**:
Trạng thái của đơn đã được trả tiền. Chỉ lúc này **vé** mới được cấp.
_Avoid_: `đã thanh toán`, `confirmed`, `completed`, `PaymentSucceeded` (là sự kiện, không phải trạng thái)

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

**Loại ghế Couple / Couple seat type**:
Loại ghế cho hai người. Khi đếm giới hạn số lượng, tính là **2 ghế**; nhưng sinh ra **1 vé**.
_Avoid_: `ghế đôi`, `double seat`, `loveseat`, `DoubleSeat`

**Tổng tiền của đơn / Booking total**:
Tổng giá của từng ghế trong đơn. Không phải một giá chung cho cả đơn.
_Avoid_: `order total`, `tổng giá đơn`, `TotalPrice` (giá của một ghế, không phải của cả đơn)

## Danh từ và động từ của cùng một thứ

Hai thứ được gọi cùng một tên khi chúng **cùng một ngữ nghĩa** — một cái là danh tính, cái
khác là hành động. Dùng nhầm hai cái là nguồn bug:

| Hành động (EN) | Kết quả (EN) |
|---|---|
| `Hold seats` | tạo ra một `Seat hold` |
| `Pay` | đổi trạng thái `Booking` thành `Paid` và tạo ra các `Ticket` |

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

Một ghế `Held` **không** phải `Available`, nhưng người xem khác **không** đặt được.

`Available seat count` là con số của quản lý rạp. `Sellable seat count` là con số người xem thấy.
Một ghế đang giữ không thuộc con số nào.

### `Hết hạn` / `Expired` vs `Đã hủy` / `Cancelled`

Hai trạng thái khác nhau của `Booking`, và khác nhau ở **hậu quả**:

- `Expired`: tự động xuất hiện, do thời điểm hết hạn đi qua.
- `Cancelled`: do người xem chủ động hủy.

Cả hai đều không có vé, đều trả ghế về. Nhưng `Expired` có thêm một tính chất: **không hồi
sinh** — thông báo kết quả thanh toán đến sau không đưa được đơn về lại trạng thái đang chờ.

### `Vé` / `Ticket` vs `Chỗ giữ ghế` / `Seat hold`

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

**Đã trả lời (2026-11-21): KHÔNG.** `Seat hold` là aggregate root, chứa ≤ 6 `Showtime seat`.
Lý do đầy đủ: [`aggregates/README.md`](aggregates/README.md).

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
| Số ghế còn trống | `Available seat count` | Cho quản lý rạp |
| Số ghế còn bán online | `Sellable seat count` | Cho người xem |
| Loại ghế | `Seat type` | `Standard` / `Couple` / `VIP` |
| Giá vé | `Ticket price` | Thuộc (seat type, showtime) |
| Suất chiếu | `Showtime` | |
| Phim | `Movie` | |
| Giữ ghế | `Hold seats` | Hành động |
| Chỗ giữ ghế | `Seat hold` | Bản ghi đang giữ |
| Hết hạn | `Expired` | Trạng thái booking |
| Đã hủy | `Cancelled` | Trạng thái booking |
| Thanh toán thành công | `Paid` | Trạng thái booking |
| Người xem | `Customer` | |
| Quản lý rạp | `Theater manager` | |
| Nhân viên cổng | `Gate staff` | |
| Quản trị viên | `Administrator` | |
| Check-in | `Check-in` | |
| Loại ghế Couple | `Couple seat type` | |
| Tổng tiền của đơn | `Booking total` | |

### Gợi ý đặt tên (chưa chốt — sẽ chốt ở T07/T11)

| Khái niệm | Gợi ý tên | |
|---|---|---|
| Ghế của một suất chiếu | `ShowtimeSeat` | Có khả năng là entity, xem T07 |
| Ghế của phòng | `RoomSeat` | Nhiều khả năng chỉ là dữ liệu tĩnh |
| Chỗ giữ ghế | `SeatHold` | |
| Đơn đặt vé | `Booking` | Aggregate gốc — xem T07 |
| Vé | `Ticket` | |

Các tên này **chưa chốt**. T07 (aggregates) và T11 (data model) sẽ quyết định tên entity thật sự
và tên bảng.

## Mâu thuẫn đã phát hiện và sửa

### FR-23 vs BR-11 — số ghế bị chặn

`FR-23` nói ba con số (trống / đang giữ / đã bán) cộng lại bằng tổng số ghế phòng. `BR-11` nói
ghế bị chặn vẫn tính vào tổng số ghế phòng. Hai câu không cùng đúng — **đã sửa `FR-23` thành
bốn con số**, kèm ghi chú phân biệt con số người xem thấy (`FR-13`).

Bài học: khi chốt một quyết định ở ticket sau, **quay lại kiểm tra các ticket trước** có
còn đúng không. Một quyết định được ghi thêm ở một nơi mà không sửa ở nơi khác tạo ra một
mâu thuẫn chỉ lộ ra khi ai đó đọc cả hai.

## Trạng thái

Bản làm việc. Các thuật ngữ trên đã được người học xác nhận. Tiếp theo: T06 — context map.
