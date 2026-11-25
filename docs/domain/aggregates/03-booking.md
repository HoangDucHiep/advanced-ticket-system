# Booking — Aggregate và invariant

Bounded context `Booking` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

## Trách nhiệm

Quản lý đơn đặt vé của người xem: ghế đã chọn, giá đã chốt, thời điểm hết hạn, giới hạn số ghế,
vòng đời đơn.

`Booking` **không** biết ghế có trạng thái gì — nó chỉ biết "đơn này giữ những ghế nào (theo id),
tới lúc nào", và "đơn này đang ở trạng thái nào".

---

## Aggregate: `Booking`

**Bản chất**: entity, aggregate root. Một lần đặt vé — tồn tại từ lúc người xem bấm xác nhận giữ
ghế đến lúc đơn kết thúc (`Paid` / `Expired` / `Cancelled`). Là **nơi duy nhất** đại diện cho "một
lần đặt" (ADR-0005 — không còn aggregate `Seat hold` song song).

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| `Booking line` (1–6 theo `BR-06`) | entity | Một ghế trong đơn (tham chiếu `Showtime seat id`), kèm loại ghế và giá đã chốt tại lúc giữ |
| `Ticket price` đã chốt | value object | Giá của từng ghế, **đóng băng** tại lúc giữ |
| Thời điểm hết hạn (`expires_at`) | value object | **Nguồn sự thật** về hạn của lần giữ (15 phút kể từ lúc giữ) |
| Danh tính người đặt (`Customer id`) | tham chiếu | Ai đặt, để áp dụng giới hạn theo người |
| Suất chiếu (`Showtime id`) | tham chiếu | Đơn thuộc đúng một suất chiếu |
| Tổng tiền của đơn | value object | Tổng giá của các dòng |
| Lịch sử trạng thái | append-only | Mọi lần thay đổi trạng thái, kèm nguyên nhân, theo đúng thứ tự thời gian |

**Boundary**: nằm **ngoài** aggregate này —

- `Showtime seat` (thuộc `Inventory`) — trạng thái thật của ghế và thông tin giữ trên ghế;
- `Ticket price` gốc (thuộc `Scheduling`) — giá trước khi được chốt;
- `Ticket` (thuộc `Ticketing`) — vé đã cấp;
- `Payment intent` (thuộc `Payment`) — trạng thái tiền.

### Invariants

- **INV-01** — Mọi ghế trong một đơn thuộc **cùng một suất chiếu**.
  - *Cơ chế bảo vệ*: `Booking` lưu **một** tham chiếu suất chiếu; `Booking line` **không** có
    trường suất chiếu ⇒ trộn suất **không thể biểu diễn được** (hình dạng dữ liệu).
  - *Ai có thể vi phạm*: không ai, miễn là không ai thêm trường suất chiếu vào dòng.
  - *Traces to*: `BR-12`, `FR-16`, `FR-21`.

- **INV-02** — Giá của đơn là **tổng giá từng ghế**; giá mỗi ghế là **giá đã chốt** của loại ghế
  đó trong suất chiếu này **tại lúc giữ**. Loại ghế và giá lấy từ dữ liệu **phía server**, không
  bao giờ từ yêu cầu của client.
  - *Cơ chế bảo vệ*: `Booking line` lưu giá đã chốt (value object bất biến); `Booking` tính tổng từ
    các dòng. Loại ghế đọc từ `Inventory` (R3), giá đọc từ `Scheduling` (R9).
  - *Ai có thể vi phạm*: (1) code tính lại giá từ giá hiện hành của `Scheduling`; (2) code tin
    loại ghế client gửi lên — client gửi ghế VIP là "Standard" để trả giá rẻ.
  - *Traces to*: `BR-12`, `FR-16`.

- **INV-03** — Một đơn chứa **tối đa 6 ghế** tính theo quy tắc đếm ghế (ghế đôi tính 2).
  - *Cơ chế bảo vệ*: kiểm tra trong aggregate trước khi tạo.
  - *Ai có thể vi phạm*: code tạo đơn với 7 ghế. Aggregate từ chối ở biên.
  - *Traces to*: `BR-06`, `BR-07`, `FR-09`.

- **INV-04** — Mỗi ghế trong đơn sinh ra **đúng một vé**, và vé **chỉ tồn tại sau khi** đơn đã
  `Paid`. Đơn `Paid` **luôn** đã có đủ vé.
  - *Cơ chế bảo vệ*: `AwaitingPayment → Paid` và cấp vé (`Ticketing`) nằm trong **cùng một** DB
    transaction (R6). Vì vậy "đơn `Paid` mà chưa cấp vé" là trạng thái **không thể xảy ra** — không
    cần cờ "đã cấp vé" riêng (đóng Q26).
  - *Ai có thể vi phạm*: code tách cấp vé ra transaction khác — xem [`05-ticketing.md`](05-ticketing.md).
  - *Traces to*: `BR-09`, `FR-05`, `FR-08`, `FR-16`.

- **INV-05** — Trạng thái đơn chỉ đi theo **một chiều**. Đơn quá `expires_at` **không bao giờ**
  sang `Paid` — kể cả khi worker chưa kịp chuyển nó sang `Expired`.
  - *Cơ chế bảo vệ*: state machine ([`../state-machines.md`](../state-machines.md)) + điều kiện lệnh
    của transition `→ Paid`: `WHERE status = 'AwaitingPayment' AND expires_at > now()` (ADR-0006).
    Hết hạn được định nghĩa bằng **thời gian**, không bằng việc worker đã chạy hay chưa.
  - *Ai có thể vi phạm*: code xử lý thanh toán chỉ kiểm `status = 'AwaitingPayment'` — đơn quá hạn
    nhưng worker chưa quét vẫn được `Paid`. `BR-05` yêu cầu hoàn tiền thay vì cấp vé.
  - *Traces to*: `BR-04`, `FR-06`.

- **INV-06** — Huỷ đơn chỉ có tác dụng khi đơn **chưa thanh toán**; huỷ **trả ghế về ngay**. Huỷ
  xảy ra theo hai nguyên nhân: người xem chủ động huỷ, hoặc suất chiếu bị huỷ. Nguyên nhân được
  ghi trong lịch sử đơn.
  - *Cơ chế bảo vệ*: state machine chặn huỷ đơn `Paid`. Trả ghế là lệnh ở `Inventory` lọc theo
    `hold_id` của đơn (INV-03 ở [`02-inventory.md`](02-inventory.md)), **cùng transaction** (R3).
  - *Ai có thể vi phạm*: code huỷ đơn mà quên trả ghế — ghế mất vĩnh viễn.
  - *Traces to*: `BR-21`, `FR-18`, `FR-24`, `SC-07`.

- **INV-07** — Lịch sử của đơn là **append-only**: mỗi lần thay đổi trạng thái ghi đúng một lần,
  theo thứ tự thời gian, không sửa, không xoá.
  - *Cơ chế bảo vệ*: aggregate chỉ **thêm**; quyền ghi bảng lịch sử thuộc module `Booking`.
  - *Traces to*: `BR-27`, `FR-31`.

- **INV-08** — Cùng một **định danh yêu cầu** của cùng một người chỉ tạo ra **một** đơn, và trả về
  **cùng kết quả** cho mọi lần gửi — **kể cả khi lần đầu bị từ chối**.
  - *Cơ chế bảo vệ*: một **bản ghi idempotency** riêng, ràng buộc duy nhất trên
    `(customer_id, định danh yêu cầu)`, lưu **kết quả của lần đầu** (thành công *hoặc* từ chối) và
    dấu vân tay của nội dung yêu cầu. Cùng định danh mà khác nội dung ⇒ từ chối. Chi tiết ở T19.
  - *Vì sao không đặt unique trên chính `Booking`*: lần đầu bị từ chối (ghế đã có người) thì không
    có `Booking` nào được tạo — gửi lại cùng định danh có thể thành công ⇒ "cùng kết quả" bị vi phạm.
  - *Ai có thể vi phạm*: định danh do server sinh (timestamp) thay vì do client cung cấp.
  - *Traces to*: `BR-18`, `FR-37`.

- **INV-09** — Người xem chỉ xem, tải, huỷ được **đơn và vé của chính mình**.
  - *Cơ chế bảo vệ*: `Booking` lưu chủ sở hữu; mọi thao tác kiểm tra chủ sở hữu ở biên aggregate.
  - *Ai có thể vi phạm*: endpoint đọc đơn bằng id mà không kiểm tra chủ (IDOR).
  - *Traces to*: `BR-14`, `FR-10`.

- **INV-10** — Một người xem có tối đa **10 ghế** (theo quy tắc đếm ghế) cho **một suất chiếu**,
  tính trên các đơn `AwaitingPayment` **còn hạn** và `Paid` của người đó cho suất đó.
  - *Vì sao không nằm trong một aggregate*: nó đếm theo **người** qua **nhiều đơn**; ranh giới
    `Booking` chỉ bảo vệ một đơn.
  - *Cơ chế bảo vệ*: trong transaction giữ ghế, **trước khi đếm**, lấy một **khoá theo cặp
    (`customer_id`, `showtime_id`)** giữ tới hết transaction ⇒ các yêu cầu của cùng người cho cùng
    suất xếp hàng, "đếm rồi giữ" không có cửa sổ race (ADR-0007).
  - *Vì sao không phải điểm tranh chấp toàn cục*: khoá chỉ tranh giữa các yêu cầu **của chính người
    đó** cho **chính suất đó**.
  - *Ai có thể vi phạm*: (1) đếm chỉ ghế đang giữ, quên ghế đã mua; (2) đếm trước khi lấy khoá.
  - *Traces to*: `BR-08`, `FR-09`.

> **Đã sửa (2026-11-25).** Ở T07, INV-10 được coi là "không bảo vệ được" và chấp nhận cửa sổ race,
> với lý do "chỉ người dùng tự hại" và "khoá sẽ là điểm tranh chấp toàn cục". Cả hai lý do đều sai:
> luật tồn tại để chống phe vé (người bị hại là người khác), và khoá theo cặp (người, suất) không
> toàn cục. Xem ADR-0007.

### Transaction boundary

**Giữ ghế** — một DB transaction (ranh giới bị cắt R3, ADR-0005):

1. Khoá quota `(customer_id, showtime_id)`, đếm, so với 10 (INV-10);
2. Chuyển n `Showtime seat` `Available → Held` với `hold_id` = id đơn, `held_until` = `expires_at`;
3. Tạo `Booking` ở `AwaitingPayment` kèm các `Booking line`, giá đã chốt, `expires_at`;
4. Ghi lịch sử (dòng đầu tiên).

**Mỗi thao tác đổi trạng thái đơn** — một DB transaction:

- **Thanh toán thành công** (khi `expires_at > now()`): đơn → `Paid`, ghế → `Sold`, cấp vé, ghi
  lịch sử (R4 + R6).
- **Hết hạn**: đơn → `Expired`, ghế của đơn → `Available`, ghi lịch sử.
- **Huỷ** (người xem, hoặc do hủy suất chiếu): đơn → `Cancelled`, ghế của đơn → `Available`, ghi
  lịch sử kèm nguyên nhân.

---

## Invariant không bảo vệ được bằng một aggregate

`INV-10` (`BR-08`) — bảo vệ bằng khoá **ngoài** ranh giới aggregate, nhưng **trong** một DB
transaction. Không còn cửa sổ race.

### Hệ quả đối với `FR-09`

`FR-09` gộp hai luật: `BR-06` (≤6 ghế mỗi đơn — `INV-03`) và `BR-08` (≤10 ghế mỗi người mỗi suất —
`INV-10`). Viết **hai** test khác nhau. Test của `BR-08` phải là test concurrency thật: nhiều yêu
cầu song song của **cùng** một người ⇒ tổng ≤ 10, và test phải **fail** nếu bỏ khoá.

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Đơn đặt vé, ghế đã chọn, giá đã chốt, `expires_at` | `Booking` | `Payment`, `Ticketing` |
| Lịch sử trạng thái đơn | `Booking` | quản trị viên (`FR-31`) |
| Trạng thái tiền của đơn | `Payment` | `Booking` |
| Trạng thái thật của ghế | `Inventory` | `Booking` |
| Vé đã cấp | `Ticketing` | `Booking` (qua đơn) |
