# Scheduling — Aggregate và invariant

Bounded context `Scheduling` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

## Trách nhiệm

Quản lý phim, phòng chiếu, sơ đồ ghế vật lý, lịch chiếu và giá vé.

`Scheduling` là nơi **khởi sinh** dữ liệu mà `Inventory` cần: **tạo** một suất chiếu là lúc
`Inventory` sinh ra `Showtime seat` theo sơ đồ phòng, qua lớp dịch (xem R1). Mở bán chỉ đổi trạng
thái suất chiếu.

> **Đã đổi (2026-11-25):** trước đây ghế được sinh lúc **mở bán**. Nhưng J-06 cho quản lý rạp
> **chặn ghế trước khi mở bán** — lúc đó chưa có `Showtime seat` nào, nên danh sách ghế bị chặn
> phải nằm tạm ở `Scheduling` rồi được chép sang, tức là `Blocked` có **hai** nguồn sự thật. Sinh
> ghế ngay lúc tạo suất thì `Blocked` chỉ còn **một** chủ: `Inventory`.

---

## Aggregate: `Showtime`

**Bản chất**: entity. Một lần chiếu — một phim, một phòng, một giờ.

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| `Ticket price` theo `Seat type` | value object | Giá của từng loại ghế **trong suất chiếu này** |
| Trạng thái | thuộc tính | `NotOnSale` / `OnSale` / `Cancelled` |
| Giờ bắt đầu, giờ kết thúc | value object | Khoảng thời gian chiếu (dùng cho `BR-20` và hạn chót bán vé) |
| Chủ sở hữu | tham chiếu | Quản lý rạp đã tạo suất (`BR-16`) |
| Tham chiếu phim, phòng | tham chiếu | Phim nào, phòng nào |

**Boundary**: nằm **ngoài** aggregate này —

- `Room` — sơ đồ ghế vật lý;
- `Movie` — thông tin phim;
- `Showtime seat` (thuộc `Inventory`) — trạng thái thật của ghế, kể cả `Blocked`;
- `Gate assignment` (thuộc `Ticketing`) — phân công nhân viên cổng.

### Invariants

- **INV-01** — Giá vé thuộc về **cặp (suất chiếu, loại ghế)**. Cùng một loại ghế trong cùng một
  suất có cùng giá; giá **không** thuộc về ghế cụ thể, và **không** thuộc về phim.
  - *Cơ chế bảo vệ*: `Showtime` chứa tập `Ticket price` — mỗi loại ghế **một** giá. Không có
    chỗ nào lưu giá theo ghế, nên "hai ghế cùng loại khác giá" **không thể biểu diễn được**.
  - *Ai có thể vi phạm*: code tạo `Ticket price` trùng loại ghế — cần ràng buộc duy nhất trên
    `(showtime_id, seat_type)`.
  - *Traces to*: `BR-12`, `FR-16`, `FR-12`.

- **INV-02** — Người xem chỉ nhìn thấy và chỉ giữ ghế được suất chiếu **đang bán** và **chưa tới
  giờ bắt đầu chiếu**. Suất chưa mở bán không xuất hiện trong danh sách, không có trang chi tiết,
  và yêu cầu đặt vé bị từ chối. Suất đã bắt đầu thì không giữ ghế được nữa.
  - *Cơ chế bảo vệ*: trạng thái và giờ bắt đầu là **thuộc tính của `Showtime`**; lệnh giữ ghế (ở
    `Booking`) đọc chúng qua R9 trước khi ghi. Đây là điều kiện **xuyên ranh giới**.
  - *Ai có thể vi phạm*: code giữ ghế chỉ kiểm `OnSale` mà quên giờ bắt đầu — bán vé cho suất đã
    chiếu xong.
  - *Traces to*: `BR-13`, `BR-31`, `FR-22`, `FR-34`.

- **INV-03** — Một suất chiếu thuộc **đúng một** phòng và **đúng một** phim.
  - *Cơ chế bảo vệ*: mô hình — `Showtime` mang **một** tham chiếu phòng và **một** tham chiếu
    phim. Không thể biểu diễn suất chiếu thuộc hai phòng.
  - *Ai có thể vi phạm*: không ai, nếu không tạo thêm trường. Luật bổ sung cho lập trình viên:
    không thêm trường "phòng phụ" hay "chiếu song song".
  - *Traces to*: `BR-12`, `FR-16`.

- **INV-04** — Quản lý rạp chỉ sửa hoặc hủy được lịch chiếu **do chính họ tạo**; quản trị viên
  thì được trên lịch chiếu của bất kỳ rạp nào.
  - *Cơ chế bảo vệ*: `Showtime` mang tham chiếu **chủ sở hữu** (quản lý rạp đã tạo nó), và quyền
    được kiểm tra **ở biên aggregate** cho mọi lệnh thay đổi. Việc kiểm tra nằm trong hành vi
    của aggregate, không phải ở tầng controller.
  - *Ai có thể vi phạm*: một endpoint sửa lịch chiếu mà không kiểm tra chủ sở hữu — một rạp sửa
    lịch chiếu của rạp khác là can thiệp vào vận hành của rạp khác.
  - *Traces to*: `BR-16`, `FR-28`, `FR-32`.

- **INV-05** — Chỉ **quản lý rạp sở hữu suất chiếu** mới được **chặn / gỡ chặn ghế** của suất đó.
  - *Cơ chế bảo vệ*: trạng thái `Blocked` là của `Showtime seat` (`Inventory` — xem
    [`02-inventory.md`](02-inventory.md) `INV-04`, `INV-05`). `Scheduling` chỉ cung cấp thông tin
    **chủ sở hữu** suất chiếu để lệnh chặn ghế kiểm tra quyền (R9). `Scheduling` **không** lưu danh
    sách ghế bị chặn.
  - *Ai có thể vi phạm*: lệnh chặn ghế không kiểm tra chủ sở hữu suất.
  - *Traces to*: `BR-11`, `BR-16`, `FR-20`, `FR-23`.

- **INV-06** — *(Đã chuyển, 2026-11-25.)* Phân công nhân viên cổng giờ là aggregate
  `Gate assignment` ở `Ticketing` — xem [`05-ticketing.md`](05-ticketing.md) `INV-08`, `INV-09`.

- **INV-07** — Một phòng chiếu không thể có **hai suất chiếu chồng nhau** về thời gian.
  - *Cơ chế bảo vệ*: **KHÔNG bảo vệ được bằng transaction của aggregate `Showtime`** — xem mục
    "Invariant không bảo vệ được bằng một transaction đơn lẻ" bên dưới.
  - *Traces to*: `BR-20`, `FR-21`.

### Transaction boundary

Một transaction cho **mỗi lần tạo suất chiếu**:

1. Kiểm tra quyền người gọi; tạo `Showtime` (`NotOnSale`) cùng tập `Ticket price`;
2. Sinh toàn bộ `Showtime seat` (`Available`) cho `Inventory` qua lớp dịch (R1) — all-or-nothing.

Một transaction cho **mỗi lần sửa** (chỉ khi `NotOnSale`) và **mỗi lần mở bán**
(`NotOnSale → OnSale`, chỉ đổi trạng thái).

Một transaction cho **mỗi lần hủy suất** (ADR-0006): `OnSale → Cancelled`, huỷ mọi đơn
`AwaitingPayment` của suất, ghi yêu cầu hoàn tiền (outbox) cho mọi đơn `Paid`. Việc gọi Stripe
hoàn tiền chạy **sau**, bất đồng bộ.

Tạo suất và hủy suất là **ranh giới bị cắt** (chạm `Inventory`, `Booking`, `Payment`) — xem
[`../state-machines.md`](../state-machines.md).

---

## Aggregate: `Room`

**Bản chất**: entity. Một phòng chiếu vật lý.

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| `Room seat` | entity | Sơ đồ ghế vật lý: hàng, số, loại ghế. **Tĩnh** — không có trạng thái |

**Boundary**: nằm **ngoài** aggregate này — `Showtime` (lịch chiếu của phòng), và
`Showtime seat` (thuộc `Inventory`).

> **Vì sao `Room seat` nằm trong aggregate `Room` dù phòng có hàng trăm ghế?** Vì ghế vật lý là
> **tĩnh** — nó không bao giờ đổi trạng thái, chỉ có thể được sửa sơ đồ khi quản lý rạp thiết
> lập lại phòng. Một aggregate chứa hàng trăm dòng **tĩnh** không gây contention, vì
> contention chỉ sinh ra khi có **ghi**. Ranh giới aggregate quan tâm tới **tần suất ghi**, không
> chỉ số dòng. Đây là đối lập với `Showtime seat` — cùng số dòng, nhưng ghi liên tục.

### Invariants

- **INV-08** — Mỗi `Room seat` có **vị trí duy nhất** trong phòng (hàng + số), và **một** loại
  ghế.
  - *Cơ chế bảo vệ*: ràng buộc duy nhất trên `(room_id, row, number)`.
  - *Ai có thể vi phạm*: code thêm ghế trùng vị trí.
  - *Traces to*: `BR-12`, `FR-23`.

- **INV-09** — Tổng số ghế của một phòng **không** được cấu hình riêng — nó **bằng** số dòng
  `Room seat` trong phòng đó.
  - *Cơ chế bảo vệ*: **hình dạng mô hình** — không tồn tại khái niệm "sức chứa" được cấu
    hình. Số ghế là phép đếm. Ghế `Blocked` **vẫn tính** vào tổng, vì nó là một dòng ghế, chỉ
    là không bán online.
  - *Ai có thể vi phạm*: không ai, miễn là không ai thêm trường "capacity". Luật bổ sung cho
    lập trình viên: nếu cần số ghế, hãy **đếm**.
  - *Traces to*: `BR-11`, `FR-23`, ADR-0003.

- **INV-10** — Loại ghế là của **phòng** (`Standard` / `Couple` / `VIP`), không phải của suất
  chiếu. Giá thì của **suất chiếu** × loại ghế.
  - *Cơ chế bảo vệ*: `Seat type` là thuộc tính của `Room seat`; `Ticket price` là thuộc tính
    của `Showtime`. Vì vậy cùng một loại ghế có thể có **giá khác nhau ở các suất khác nhau** —
    điều này hợp lệ và là chủ ý.
  - *Ai có thể vi phạm*: lưu giá trên `Room seat` — sai, vì giá thuộc suất chiếu.
  - *Traces to*: `BR-12`, `FR-16`.

- **INV-13** — Sơ đồ ghế của phòng **không sửa được** khi phòng còn suất chiếu chưa kết thúc và
  chưa bị hủy.
  - *Vì sao*: `Showtime seat` được sinh từ sơ đồ phòng lúc tạo suất. Sửa sơ đồ sau đó làm ghế của
    suất lệch với phòng — có thể bán một ghế không còn tồn tại.
  - *Cơ chế bảo vệ*: lệnh sửa sơ đồ kiểm tra không còn suất chiếu như vậy.
  - *Traces to*: `BR-11`, `FR-20`, ADR-0003.

---

## Aggregate: `Movie`

**Bản chất**: entity. Một tác phẩm được chiếu.

**Chứa**: tên, mô tả, thời lượng, đạo diễn, thể loại.

**Boundary**: nằm **ngoài** aggregate này — `Showtime` (các suất chiếu của phim).

### Invariants

- **INV-11** — Chỉ hiện phim **đang có suất chiếu mở bán**. Trang chi tiết phim chỉ hiện các
  suất chiếu đang mở bán, kèm giờ chiếu, phòng, và giá vé.
  - *Cơ chế bảo vệ*: đây là **thuộc tính của truy vấn**, không phải trạng thái lưu trữ. Phim
    không có cờ "đang mở bán" — nó **có** suất chiếu đang mở bán, và điều đó được **suy ra**.
  - *Ai có thể vi phạm*: code hiện phim theo một cờ được lưu thủ công trên phim — cờ đó sẽ lệch
    với thực tế ngay khi một suất chiếu đóng bán.
  - *Traces to*: `BR-29`, `FR-11`, `FR-12`.

- **INV-12** — Giá hiển thị là **giá theo loại ghế trong suất chiếu đó** — một suất có thể có
  nhiều loại ghế với nhiều mức giá.
  - *Cơ chế bảo vệ*: dữ liệu lấy từ `Ticket price` của `Showtime`, không phải một con số
    "giá chung" lưu ở đâu đó.
  - *Ai có thể vi phạm*: code hiển thị `giá từ` — giá nhỏ nhất trong các loại ghế — là chấp nhận
    được cho *hiển thị*, nhưng không được dùng làm giá của đơn (giá của đơn là tổng từng ghế).
  - *Traces to*: `BR-12`, `BR-29`, `FR-12`.

### Transaction boundary

Một transaction duy nhất cho **mỗi lần tạo hoặc sửa một phim**. Phim không có trạng thái
nghiệp vụ phức tạp, nên ranh giới aggregate là ranh giới transaction gần như tự nhiên.

---

## Invariant không bảo vệ được bằng một transaction đơn lẻ

- **INV-07 — `BR-20`: một phòng chiếu không thể có hai suất chiếu chồng nhau về thời gian.**
  - *Vì sao không bảo vệ được*: đây là quan hệ **giữa hai `Showtime` khác nhau**. Ranh giới
    aggregate `Showtime` bảo vệ **một** suất chiếu. Để transaction của aggregate này ngăn
    được hai suất chiếu chồng nhau, nó phải **truy cập tới suất chiếu khác** — tức là đọc và
    ghi ngoài ranh giới aggregate, tức là phá vỡ chính mô hình aggregate. Nếu không sửa được
    thì phải đặt ranh giới `Room` bao trọn tất cả suất chiếu của phòng — và khi đó lại quay
    về vấn đề aggregate quá lớn, cộng thêm việc một `Room` chứa hàng trăm ghế **và** hàng
    chục suất chiếu.
  - *Cơ chế đã chọn*: **ràng buộc loại trừ** (exclusion constraint) ở tầng lưu trữ trên
    `(room_id, khoảng thời gian)` — ở Postgres là `EXCLUDE USING gist (room_id WITH =,
    time_range WITH &&)`. Cơ chế này **không nằm trong ranh giới aggregate** nhưng vẫn bảo vệ
    invariant, và nó mạnh hơn kiểm tra ứng dụng vì chặn được cả **hai lệnh tạo suất chiếu chạy
    đồng thời** — mà kiểm tra "có suất nào chồng không" trong ứng dụng thì không.
  - *Hệ quả được chấp nhận*: việc bảo vệ invariant nằm **ngoài** ranh giới aggregate. Đây là
    hệ quả không tránh được của việc invariant mang tính chất **quan hệ giữa hai aggregate**.
  - *Điều kiện để chấp nhận được*: phải có **khoảng đệm giữa hai suất** (thời gian dọn dẹp) —
    khoảng thời gian trong ràng buộc là `[giờ bắt đầu, giờ kết thúc + khoảng đệm)`. Suất
    `Cancelled` **không** tính vào ràng buộc (ràng buộc có điều kiện), nếu không thì hủy một suất
    sẽ chặn vĩnh viễn khung giờ đó. Giá trị khoảng đệm chốt ở T13.
  - *Traces to*: `BR-20`, `FR-21`.

> **Bài học của `INV-07`.** Có những invariant mà **aggregate boundary không đủ** — không phải
> vì ranh giới chọn sai, mà vì bản thân invariant mang tính chất **quan hệ giữa hai aggregate**
> ("hai suất chiếu này không được chồng nhau"). Đây là dạng thứ ba của "không bảo vệ được bằng
> transaction đơn lẻ": không phải do **tích luỹ xuyên nhiều aggregate** (`BR-08`), cũng không
> phải do **cần điều phối nhiều bước** (`BR-17`), mà là do **quan hệ giữa hai aggregate**.

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Phim, suất chiếu, phòng, sơ đồ ghế vật lý | `Scheduling` | `Inventory`, `Booking`, `Ticketing` |
| Loại ghế của một phòng | `Scheduling` | `Inventory` |
| Giá vé (theo cặp loại ghế × suất chiếu) | `Scheduling` | `Inventory`, `Booking` |
| Trạng thái, giờ chiếu, chủ sở hữu suất chiếu | `Scheduling` | `Booking` (R9), `Inventory` (R9), `Ticketing` (R7) |
| Trạng thái thật của một ghế suất chiếu (kể cả `Blocked`) | `Inventory` | `Scheduling` (để báo cáo tình trạng bán) |
