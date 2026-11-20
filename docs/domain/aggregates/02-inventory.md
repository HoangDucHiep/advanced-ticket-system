# Inventory — Aggregate và invariant

Bounded context `Inventory` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

## Trách nhiệm

Quản lý trạng thái của từng ghế của từng suất chiếu, và các chỗ giữ ghế kèm thời điểm hết
hạn.

`Inventory` **không** biết phim là gì, phòng là gì, lịch chiếu là gì. Nó chỉ biết "sinh ghế
cho suất chiếu này theo cấu hình này" và "ghế này còn trống không".

---

## Aggregate: `Seat hold`

**Bản chất**: entity. Một lần giữ ghế — có định danh, có vòng đời, tồn tại từ lúc ghế được giữ
đến lúc ghế được trả về (`Trống`) hoặc được bán (`Sold`).

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| `Showtime seat` (tối đa 6 theo `BR-06`) | entity | Các ghế đang bị giữ bởi lần giữ này |
| Thời điểm hết hạn | value object | Mốc quyết định ghế được giải phóng |
| Chủ sở hữu (`Customer id`) | tham chiếu | Ai đang giữ |

**Boundary**: nằm **ngoài** aggregate này —

- `Showtime` (thuộc `Scheduling`) — cấu hình ghế, giá, trạng thái mở bán;
- `Booking` (thuộc `Booking`) — đơn đặt vé, lịch sử, giới hạn theo người;
- `Ticket` (thuộc `Ticketing`) — vé đã cấp.

### Invariants

- **INV-01** — Mỗi `Showtime seat` thuộc về **đúng một** chủ sở hữu tại một thời điểm; bốn
  trạng thái `Available` / `Held` / `Sold` / `Blocked` là loại trừ nhau và đầy đủ.
  - *Cơ chế bảo vệ*: **điều kiện lệnh** — lệnh chuyển trạng thái chỉ thành công khi ghế đang ở
    trạng thái nguồn được kỳ vọng (`Available` → `Held`). Không đọc trạng thái trước rồi quyết
    định; điều kiện nằm **trong** lệnh. Cộng thêm: constraint `CHECK` trên cột trạng thái để
    không có giá trị ngoài bốn trạng thái.
  - *Ai có thể vi phạm*: bất kỳ code nào ghi trạng thái ghế mà không kèm điều kiện. Vì vậy
    luật này được viết thành **quy tắc bắt buộc cho mọi lệnh ghi trạng thái ghế**, không phải
    chỉ cho lệnh giữ ghế.
  - *Traces to*: `BR-01`, `FR-01`, `FR-15`.

- **INV-02** — Giữ n ghế là **all-or-nothing**: đủ n ghế, hoặc không giữ ghế nào. Không tồn tại
  trạng thái "giữ được một phần".
  - *Cơ chế bảo vệ*: **một transaction duy nhất** bao trọn các lệnh chuyển trạng thái của cả n
    ghế. Vì `Seat hold` là aggregate root và chứa cả n ghế, toàn bộ nằm trong một aggregate ⇒
    một transaction. Nếu một ghế không đạt điều kiện (`Available`), transaction rollback, các
    ghế đã đổi trong thao tác đó trở lại `Available`.
  - *Ai có thể vi phạm*: code ghi trạng thái ghế ngoài thao tác giữ ghế (ví dụ một endpoint
    debug sửa thẳng trạng thái). Luật: **mọi lệnh chuyển trạng thái ghế đều phải đi qua thao tác
    giữ/ghi phóng của aggregate**.
  - *Traces to*: `BR-02`, `FR-02`.

- **INV-03** — Mỗi lần giữ ghế có **một thời điểm hết hạn xác định** (15 phút kể từ lúc giữ).
  Không có lần giữ nào tồn tại vô thời hạn.
  - *Cơ chế bảo vệ*: trường `expires_at` **bắt buộc có** (NOT NULL) trên `Seat hold`, và được
    thiết lập ngay khi aggregate được tạo. Không có đường tạo `Seat hold` mà không có mốc
    hết hạn.
  - *Ai có thể vi phạm*: code tạo `Seat hold` với `expires_at` null hoặc bằng 0. Constraint NOT
    NULL chặn trường hợp đầu; trường hợp thứ hai là lỗi logic, chặn bằng invariant của
    aggregate (constructor bắt buộc nhận mốc hết hạn hợp lệ).
  - *Traces to*: `BR-03`, `FR-04`, `FR-14`.

- **INV-04** — Ghế ở trạng thái `Held` **luôn** thuộc về một `Seat hold` đang hiệu lực, và
  `Seat hold` đó **luôn** có thời điểm hết hạn.
  - *Cơ chế bảo vệ*: `Showtime seat` ở trạng thái `Held` phải trỏ tới một `Seat hold` còn hiệu
    lực. Đây là quan hệ **bên trong** aggregate nên không cần cơ chế bên ngoài: trạng thái `Held`
    và bản ghi giữ là hai mặt của cùng một thay đổi.
  - *Ai có thể vi phạm*: mã nguồn khác context ghi trực tiếp cột trạng thái ghế mà không tạo
    bản ghi giữ. Vì vậy ranh giới module chặn truy cập trực tiếp bảng này từ ngoài `Inventory`.
  - *Traces to*: `BR-01`, `BR-03`.

- **INV-05** — Một lần giữ ghế **không vượt quá 6 ghế** tính theo quy tắc đếm ghế (ghế đôi tính
  2).
  - *Cơ chế bảo vệ*: kiểm tra trong aggregate trước khi tạo — và `Seat hold` chứa tối đa 6
    `Showtime seat`, nên đây cũng là **giới hạn kích thước aggregate**.
  - *Ai có thể vi phạm*: code gọi thao tác giữ ghế với danh sách 7 ghế. Aggregate từ chối ở
    biên.
  - *Traces to*: `BR-06`, `BR-07`, `FR-09`.

- **INV-06** — Một `Showtime seat` **không thể** vừa `Held` vừa thuộc hai `Seat hold` khác nhau.
  - *Cơ chế bảo vệ*: hệ quả trực tiếp của `INV-01` — ghế chỉ chuyển sang `Held` khi đang
    `Available`, và mỗi lần chuyển sang `Held` gắn với đúng một `Seat hold`. Không cần cơ chế
    riêng.
  - *Ai có thể vi phạm*: không ai, nếu `INV-01` được giữ. Ghi lại để thấy rõ đây là invariant
    **dẫn xuất**, không phải invariant độc lập.
  - *Traces to*: `BR-01`, `BR-02`.

- **INV-07** — Ghế `Blocked` **không** được bán online, và **vẫn tính** vào tổng số ghế của
  phòng.
  - *Cơ chế bảo vệ*: điều kiện lệnh — lệnh giữ ghế chỉ nhận ghế `Available`, nên ghế `Blocked`
    không bao giờ được giữ. Phần "vẫn tính vào tổng" là **thuộc tính của phép đếm**, không
    phải trạng thái: bốn con số cộng lại bằng tổng số ghế phòng.
  - *Ai có thể vi phạm*: code chặn ghế ở `Scheduling` (quyền của quản lý rạp) nhưng lại ghi
    thẳng trạng thái `Blocked` ở bảng của `Inventory` mà không đi qua lớp dịch.
  - *Traces to*: `BR-11`, `FR-20`, `FR-23`.

### Transaction boundary

Một transaction duy nhất bao trọn **một lần giữ ghế**: tạo `Seat hold` với mốc hết hạn, và
chuyển trạng thái của **tất cả** n ghế từ `Available` sang `Held`.

Ranh giới transaction = **ranh giới aggregate**. Một thao tác giữ ghế là **một** transaction;
không thể tách nó thành "giữ từng ghế một".

Kết quả transaction, theo hành vi:

- **Thành công**: tất cả n ghế ở `Held`, có đúng một `Seat hold` đang hiệu lực, đơn đặt vé tương
  ứng đã được tạo (xem [`03-booking.md`](03-booking.md)).
- **Thất bại**: **không** ghế nào thay đổi trạng thái, **không** có `Seat hold` nào được tạo,
  và hệ thống nói rõ **ghế nào** đã có người.

> **Bất biến bị cắt ngang ranh giới.** Tạo `Seat hold` (`Inventory`) và tạo `Booking`
> (`Booking`) phải xảy ra cùng lúc — đó là ranh giới **R3** trong context map. Trong modular
> monolith hiện tại, hai bên **dùng chung một transaction**. Nếu sau này tách `Inventory` thành
> service riêng, thao tác giữ ghế **vỡ** và phải được thiết kế lại bằng saga/outbox.

---

## Ranh giới aggregate: vì sao không phải `Showtime`

Đây là câu hỏi trọng tâm của Gate B (`ADR-0003` — "Revalidate at Gate B"). Ba phương án đã
cân nhắc:

| | Ranh giới | All-or-nothing | Kích thước aggregate | Ai ép `INV-01` |
|---|---|---|---|---|
| (a) | `Showtime` chứa toàn bộ `Showtime seat` | Tự nhiên | 200 dòng | Aggregate |
| (b) | `Showtime seat` là aggregate root riêng | Phải qua N aggregate | 1 dòng | Constraint + điều kiện lệnh |
| **(c)** | **`Seat hold` là aggregate root, chứa ≤6 ghế** | **Gói trong 1 aggregate** | **≤6 dòng** | **Constraint + điều kiện lệnh** |

**Đã chọn (c).** Lý do:

1. **`BR-02` không được bị cắt.** Ở (b), thao tác giữ 4 ghế phải đi qua 4 aggregate — mọi cơ
   chế đảm bảo "đủ 4 hoặc không ghế nào" đều là *compensating transaction* (lùi 3 ghế đã giữ),
   tức là có thời điểm hệ thống ở trạng thái trung gian vi phạm `BR-02`. Ở (c), toàn bộ n ghế
   nằm trong **một** aggregate ⇒ một transaction ⇒ all-or-nothing là điều DB bảo đảm.
2. **Aggregate vừa đủ lớn để chứa bất biến, vừa đủ nhỏ để không kẹt.** Ở (a), một phòng 200
   ghế ⇒ mỗi lần giữ ghế là một transaction trên một tập dòng rất lớn, và khi mở bán phim hot,
   hàng nghìn người tranh cùng một suất ⇒ contention dồn vào một aggregate. Ở (c), aggregate
   nhỏ (≤6 ghế theo `BR-06`) và các aggregate khác nhau tranh nhau **ở từng dòng ghế**, nên
   phân tán tự nhiên.
3. **`INV-01` vẫn được ép — chỉ là bằng cơ chế khác.** Aggregate không phải lúc nào cũng là
   thứ duy nhất bảo vệ invariant. Với seat-based, `INV-01` là invariant **về một dòng**, và
   điều kiện lệnh (`UPDATE ... WHERE status = 'Available'`) ép nó **mạnh hơn** khoá đọc-sửa-ghi:
   không cần đọc trước, không có cửa sổ TOCTOU, và hai lệnh cùng lúc trên cùng một ghế thì
   **chỉ một** thắng — lệnh còn lại ảnh hưởng 0 dòng và phải được coi là thất bại.

### Hệ quả của lựa chọn này

| Hệ quả | Chi tiết |
|---|---|
| `Showtime` **không** phải aggregate của `Inventory` | Nó thuộc `Scheduling` — xem [`01-scheduling.md`](01-scheduling.md) |
| Số ghế của một suất không phải thứ được aggregate bảo vệ | Đếm bằng `COUNT`; bốn con số tình trạng bán là **số đo đọc**, không phải invariant |
| `Seat hold` bị giới hạn 6 ghế | Không phải hệ quả kỹ thu mà là hệ quả nghiệp vụ từ `BR-06` — và nó giữ aggregate nhỏ |
| `Booking` không nằm trong aggregate `Seat hold` | Nó là aggregate riêng ở context `Booking` — xem [`03-booking.md`](03-booking.md) |
| Vòng đời `Showtime seat` không thuộc aggregate nào | Trạng thái của nó được bảo vệ bằng **điều kiện lệnh**, không bằng ranh giới aggregate |

### Thứ tự khoá để tránh deadlock (`BR-22`)

`BR-02` đòi hỏi một transaction trên nhiều dòng ghế. Hai người chọn ghế chồng nhau theo thứ tự
ngược nhau sẽ **kẹt** nếu lấy khoá theo thứ tự ngẫu nhiên.

Quy tắc (chi tiết kỹ thu chốt ở T12 — [`12-concurrency-design.md`](../../../.scratch/advanced-ticket-system/issues/12-concurrency-design.md)):

> **Các dòng ghế trong một thao tác giữ ghế luôn được khoá theo thứ tự tăng dần của định danh
> ghế.**

Đây là thứ tự **toàn cục** và xác định trước, nên không bao giờ có hai transaction cùng giữ hai
dòng theo hai thứ tự ngược nhau — đó chính là điều kiện gây deadlock.

> **Đây là một ví dụ rất rõ về việc invariant không được bảo vệ bằng aggregate vẫn cần một quy
> tắc.** `INV-01` được ép bằng điều kiện lệnh, nhưng *tính all-or-nothing* của `INV-02` vẫn
> cần quy tắc thứ tự khoá để không kẹt. Ranh giới aggregate trả lời "một transaction bao trọn
> cái gì" — nó không trả lời "transaction đó khoá theo thứ tự nào".

---

## Invariant không bảo vệ được bằng một transaction đơn lẻ

Trong context `Inventory` **không có** invariant nào thuộc loại này. Đó là kết quả trực tiếp
của lựa chọn (c): mọi bất biến về ghế đều nằm trong phạm vi một aggregate, và mọi bất biến về
tổng số ghế là **phép đếm** trên dữ liệu đã commit, không phải trạng thái cần bảo vệ.

Hai invariant **không** bảo vệ được nằm ở context khác:

- `BR-08` (giới hạn ghế mỗi người mỗi suất) — ở [`03-booking.md`](03-booking.md).
- `BR-20` (không có hai suất chiếu chồng nhau ở cùng phòng) — ở
  [`01-scheduling.md`](01-scheduling.md).

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Trạng thái của một ghế suất chiếu | `Inventory` | `Booking`, `Ticketing`, `Scheduling` |
| Chỗ giữ ghế và thời điểm hết hạn | `Inventory` | `Booking` |
| Bốn con số tình trạng bán của một suất | `Inventory` (đếm trên dữ liệu đã commit) | `Scheduling` |

Nguồn sự thật gốc (không phải ở đây): sơ đồ ghế của phòng, loại ghế, giá vé, lịch chiếu —
thuộc `Scheduling`.
