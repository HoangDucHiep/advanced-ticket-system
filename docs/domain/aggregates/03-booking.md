# Booking — Aggregate và invariant

Bounded context `Booking` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

## Trách nhiệm

Quản lý đơn đặt vé của người xem: ghế đã chọn, giới hạn số ghế, vòng đời đơn.

`Booking` **không** biết ghế có trạng thái gì — nó chỉ biết "tôi đã giữ ghế này tới lúc này",
"tôi giữ được ghế nào", và "đơn này đang ở trạng thái nào".

---

## Aggregate: `Booking`

**Bản chất**: entity. Một lần đặt vé — có định danh, có vòng đời, tồn tại từ lúc người xem bấm xác
nhận giữ ghế đến lúc đơn kết thúc (`Paid` / `Expired` / `Cancelled`).

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| `Booking line` (1–6 theo `BR-06`) | entity | Một ghế trong đơn, kèm giá đã chốt tại lúc giữ và loại ghế lúc đó |
| `Ticket price` đã chốt | value object | Giá của từng ghế, **đóng băng** tại lúc giữ |
| Danh tính người đặt (`Customer id`) | tham chiếu | Ai đặt, để áp dụng giới hạn theo người |
| Suất chiếu (`Showtime id`) | tham chiếu | Đơn thuộc đúng một suất chiếu |
| Tổng tiền của đơn | value object | Tổng giá của các dòng |
| Lịch sử trạng thái | append-only | Mọi lần thay đổi trạng thái, theo đúng thứ tự thời gian |

**Boundary**: nằm **ngoài** aggregate này —

- `Showtime seat` và `Seat hold` (thuộc `Inventory`) — trạng thái thật của ghế;
- `Ticket price` gốc (thuộc `Scheduling`) — giá trước khi được chốt;
- `Ticket` (thuộc `Ticketing`) — vé đã cấp;
- `Payment intent` (thuộc `Payment`) — trạng thái tiền.

### Invariants

- **INV-01** — Mọi ghế trong một đơn thuộc **cùng một suất chiếu**. Không có đơn ghé nhiều suất.
  - *Cơ chế bảo vệ*: `Booking` lưu **một** tham chiếu suất chiếu, không phải một tham chiếu
    trên mỗi dòng. Vì vậy việc trộn suất chiếu **không thể biểu diễn được** trong mô hình —
    đây là invariant được ép bằng **hình dạng dữ liệu**, mạnh hơn là kiểm tra lúc ghi.
  - *Ai có thể vi phạm*: không ai, miễn là mọi dòng đều lấy suất chiếu từ `Booking` chứ không
    tự mang suất chiếu riêng. Luật bổ sung cho lập trình viên: `Booking line` **không** có
    trường suất chiếu.
  - *Traces to*: `BR-12`, `FR-16`, `FR-21`.

- **INV-02** — Giá của đơn là **tổng giá từng ghế**, và giá mỗi ghế là **giá đã chốt** của
  loại ghế đó **trong suất chiếu này tại lúc giữ**.
  - *Cơ chế bảo vệ*: `Booking line` lưu giá đã chốt (value object, bất biến) và `Booking`
    tính tổng từ các dòng. Giá gốc thuộc `Scheduling` và có thể đổi sau đó — **điều đó không
    được phép làm thay đổi giá của đơn đã tạo**.
  - *Ai có thể vi phạm*: code tính lại giá đơn từ giá hiện hành của `Scheduling` thay vì đọc
    giá đã chốt. Đây là lỗi nguy hiểm vì nó chỉ lộ ra khi giá thay đổi giữa lúc giữ và lúc
    thanh toán.
  - *Traces to*: `BR-12`, `FR-16`.

- **INV-03** — Một đơn chứa **tối đa 6 ghế** tính theo quy tắc đếm ghế (ghế đôi tính 2).
  - *Cơ chế bảo vệ*: kiểm tra trong aggregate trước khi tạo. Đồng thời `BR-06` còn giới hạn
    **kích thước aggregate `Booking`** (≤6 dòng).
  - *Ai có thể vi phạm*: code gọi thao tác tạo đơn với danh sách 7 ghế. Aggregate từ chối ở biên.
  - *Traces to*: `BR-06`, `BR-07`, `FR-09`.

- **INV-04** — Mỗi ghế trong đơn sinh ra **đúng một vé**, và vé **chỉ tồn tại sau khi** đơn đã
  thanh toán thành công.
  - *Cơ chế bảo vệ*: phần "chỉ tồn tại sau khi thanh toán" thuộc về `Ticketing` (xem
    [`05-ticketing.md`](05-ticketing.md)) — ở đây `Booking` chỉ giữ **cờ** "đơn đã cấp vé" và
    **số lượng vé đã cấp**, để cấp vé là **hành vi nguyên tử** của đơn.
  - *Ai có thể vi phạm*: code cấp vé mà không đánh dấu đơn (hoặc ngược lại) — đó là `BR-17`,
    xem [`05-ticketing.md`](05-ticketing.md).
  - *Traces to*: `BR-09`, `FR-05`, `FR-08`, `FR-16`.

- **INV-05** — Trạng thái đơn chỉ đi theo **một chiều**. Một đơn đã hết hạn là **hết hạn vĩnh
  viễn** — không hồi sinh.
  - *Cơ chế bảo vệ*: state machine của `Booking` (chốt ở T08 —
    [`08-state-machines.md`](../../../.scratch/advanced-ticket-system/issues/08-state-machines.md)).
    Aggregate **từ chối** mọi lệnh chuyển trạng thái không hợp lệ; không có đường nào đưa một
    đơn `Expired` về trạng thái chờ.
  - *Ai có thể vi phạm*: code xử lý kết quả thanh toán đến muộn — bật cờ "đã thanh toán" cho
    một đơn đã hết hạn. Aggregate phải từ chối; và `BR-05` yêu cầu hoàn tiền thay vì cấp vé.
  - *Traces to*: `BR-04`, `FR-06`.

- **INV-06** — Huỷ một đơn chỉ có tác dụng khi đơn **chưa thanh toán**; và việc huỷ **trả ghế
  về ngay** (trạng thái `Trống`).
  - *Cơ chế bảo vệ*: state machine của `Booking` chặn lệnh huỷ trên đơn đã `Paid`. Phần "trả
    ghế về" là thao tác ở `Inventory` — xem [`02-inventory.md`](02-inventory.md) — và được thực
    hiện **cùng transaction** (ranh giới R3).
  - *Ai có thể vi phạm*: code huỷ đơn mà quên gọi thao tác trả ghế — ghế mất vĩnh viễn mà
    không ai mua. Đây là lý do luật tồn tại.
  - *Traces to*: `BR-21`, `FR-18`, `SC-07`.

- **INV-07** — Lịch sử của đơn là **append-only**: mỗi lần thay đổi trạng thái được ghi lại đúng
  một lần, theo thứ tự thời gian, và không bao giờ sửa hay xoá.
  - *Cơ chế bảo vệ*: lịch sử là **dữ liệu chỉ-đọc** — không có hành vi nào trong aggregate
    sửa hoặc xoá một dòng lịch sử. Aggregate chỉ **thêm**.
  - *Ai có thể vi phạm*: mã nguồn có quyền ghi trực tiếp bảng lịch sử (ngoài lĩnh vực của
    aggregate). Vì vậy quyền ghi bảng này phải thuộc `Booking` module.
  - *Traces to*: `BR-27`, `FR-31`.

- **INV-08** — Cùng một **định danh yêu cầu** chỉ tạo ra **một** đơn, và trả về cùng kết quả
  cho mọi lần gửi.
  - *Cơ chế bảo vệ*: ràng buộc **duy nhất** (unique) trên định danh yêu cầu ở tầng lưu trữ —
    đây là cơ chế **mạnh hơn** kiểm tra trong ứng dụng, vì nó chống được cả hai yêu cầu đến
    **đồng thời** (race) chứ không chỉ hai lần gửi tuần tự.
  - *Ai có thể vi phạm*: không ai, nếu định danh yêu cầu có ràng buộc duy nhất. Lỗi có thể nằm
    ở chỗ sinh định danh (ví dụ dùng timestamp thay vì định danh do client cung cấp) — khi đó
    ràng buộc vẫn đúng nhưng vô nghĩa. Vì vậy luật bổ sung: định danh yêu cầu **do người
    dùng cung cấp**.
  - *Traces to*: `BR-18`, `FR-37`.

- **INV-09** — Người xem chỉ xem, tải, huỷ được **đơn và vé của chính mình**.
  - *Cơ chế bảo vệ*: `Booking` lưu chủ sở hữu; mọi thao tác đọc/ghi đều kiểm tra chủ sở hữu
    ở **biên aggregate** (đọc là hành vi của aggregate, không phải của tầng trên).
  - *Ai có thể vi phạm*: endpoint đọc đơn bằng định danh mà không kiểm tra chủ sở hữu. Đây là
    lỗ hổng rò rỉ thông tin (IDOR) — và cũng là lý do luật tồn tại.
  - *Traces to*: `BR-14`, `FR-10`.

### Transaction boundary

Một transaction duy nhất bao trọn **một thao tác giữ ghế** về phía `Booking`:

1. Tạo `Booking` ở trạng thái chờ thanh toán, kèm các `Booking line` và giá đã chốt;
2. Ghi lịch sử trạng thái (dòng đầu tiên).

Và một transaction duy nhất cho **mỗi thao tác đổi trạng thái đơn**:

- **Thanh toán thành công**: đơn → `Paid`, ghi lịch sử, đánh dấu "đã cấp vé".
- **Hết hạn**: đơn → `Expired`, ghi lịch sử.
- **Huỷ**: đơn → `Cancelled`, ghi lịch sử, **và trả ghế về `Trống`** (ghi ở `Inventory`).

Ranh giới transaction = **ranh giới aggregate**. Một đơn là một transaction; không thể tách
"tạo đơn" và "giữ ghế" thành hai transaction.

> **Bất biến bị cắt ngang ranh giới.** Tạo `Booking` (`Booking`) và tạo `Seat hold`
> (`Inventory`) phải xảy ra cùng lúc — đó là ranh giới **R3**. Trong modular monolith, hai bên
> **dùng chung một transaction**. Nếu tách thành service, thao tác giữ ghế **vỡ**.
>
> Tương tự, "đơn `Paid`" (`Booking`) và "đã cấp vé" (`Ticketing`) — ranh giới **R6** — cũng
> phải cùng transaction, nếu không thì cấp vé hai lần.

---

## Invariant không bảo vệ được bằng một transaction đơn lẻ

- **INV-10 — `BR-08`: một người xem được mua tối đa 10 ghế cho một suất chiếu, tính cộng dồn
  qua tất cả đơn của người đó cho suất chiếu đó.**
  - *Vì sao không bảo vệ được*: giới hạn này đếm theo **người**, qua **nhiều đơn**. Ranh giới
    aggregate `Booking` bảo vệ **một** đơn. Để bảo vệ `INV-10` bằng transaction của `Booking` ta
    phải truy cập tới các đơn **khác** của cùng người — tức là đọc ngoài ranh giới aggregate,
    hoặc phải gộp tất cả đơn của một người vào một aggregate (vô lý: một người có thể có đơn ở
    nhiều suất chiếu khác nhau).
  - *Cơ chế đã chọn*: **quy tắc ở tầng ứng dụng** — trước khi tạo đơn, đọc tổng số ghế đang giữ
    của người đó cho suất chiếu đó (theo quy tắc đếm ghế) rồi so với 10.
  - *Hệ quả được chấp nhận*: **có một cửa sổ race**. Hai yêu cầu của cùng một người đến gần
    như đồng thời có thể cùng đọc được "đang giữ 8 ghế" và cùng tạo đơn → tổng 12 ghế vượt
    giới hạn 10.
  - *Vì sao chấp nhận được*: đây là ranh giới **"người dùng tự gian lận"**, không phải "hệ
    thống bán vượt số ghế". Một người vượt giới hạn thì chỉ tự mình bị hại — hậu quả là mua
    được nhiều ghế hơn dự định, chứ **không** phải bán quá số ghế có thật. Ngược lại, nếu ép
    bằng khóa ở tầng DB thì mọi lần giữ ghế của một người phải tranh một khoá theo
    (người, suất) — tức là thêm một điểm tranh chấp **toàn cục** vào đúng đường nóng nhất.
  - *Điều kiện để hệ quả này được chấp nhận*: số ghế vượt phải nhỏ và hậu quả phải lành
    nhẹ. Nếu sau này `BR-08` trở thành quy tắc **tài chính** (ví dụ giới hạn số lượng để chống
    gian lận thuế, hoặc mỗi đơn tối đa 1 vé cho khách hàng thân thiết) thì phải nâng lên
    ràng buộc ở tầng lưu trữ.
  - *Traces to*: `BR-08`, `FR-09`.

> **Bài học của `INV-10`.** Đây là ví dụ rõ nhất về việc **aggregate boundary không phải lúc nào
> cũng đủ**. Có những invariant mà ranh giới aggregate **về nguyên tắc không thể** bảo vệ, vì
> chúng mang tính chất **tích lũy xuyên nhiều aggregate**. Việc "hết hạn 10 ghế mỗi người mỗi
> suất" không phải lúc nào cũng đúng với hệ thống thực — và sự thật đó phải được **ghi ra
> tường minh**, kèm hậu quả, thay vì được che bằng một lời hứa suông rằng "logic sẽ kiểm
> tra".

### Hệ quả đối với `FR-09`

`FR-09` gộp hai luật: `BR-06` (≤6 ghế mỗi đơn — **có** bảo vệ được, `INV-03`) và `BR-08` (≤10
ghế mỗi người mỗi suất — **không** bảo vệ được, `INV-10`). Khi viết test, phải tách hai luật
này thành hai test khác nhau, và **không được** viết một test khẳng định giới hạn 10 ghế là
"tuyệt đối".

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Đơn đặt vé, ghế đã chọn, giá đã chốt | `Booking` | `Payment`, `Ticketing` |
| Lịch sử trạng thái đơn | `Booking` | `Payment`, `Ticketing`, quản trị viên (`FR-31`) |
| Trạng thái tiền của đơn | `Payment` | `Booking` |
| Trạng thái thật của ghế | `Inventory` | `Booking` |
| Vé đã cấp | `Ticketing` | `Booking` (qua đơn) |
