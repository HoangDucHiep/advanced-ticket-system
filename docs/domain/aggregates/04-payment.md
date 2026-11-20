# Payment — Aggregate và invariant

Bounded context `Payment` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

## Trách nhiệm

Quản lý tiền: yêu cầu thanh toán, kết quả thanh toán, hoàn tiền — và dịch sang Stripe.

`Payment` **không** biết vé, ghế, suất chiếu là gì. Nó biết "đơn này cần thu bao nhiêu" và
"tiền đã về chưa, đã hoàn chưa".

---

## Aggregate: `Payment intent`

**Bản chất**: entity. Một yêu cầu thanh toán cho một đơn — có định danh, có vòng đời.

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| Số tiền, đơn vị tiền tệ | value object | Số tiền cần thu |
| Tham chiếu đơn (`Booking id`) | tham chiếu | Tiền của đơn nào |
| Mã định danh phía Stripe (`payment_intent` id) | value object | Khóa ngoại sang hệ thống bên ngoài |
| Lịch sử `Payment event` đã xử lý | append-only | Các thông báo kết quả thanh toán đã xử lý, kèm `event.id` |
| `Refund` | entity | Khoản hoàn tiền (nhiều nhất một) |

**Boundary**: nằm **ngoài** aggregate này —

- `Booking` (thuộc `Booking`) — trạng thái đơn, vòng đời đơn;
- `Stripe` — hệ thống bên ngoài, được bảo vệ bởi **Anti-Corruption Layer** (xem R5 trong
  [`../context-map.md`](../context-map.md)).

### Invariants

- **INV-01** — Cùng một thông báo kết quả thanh toán, chạy bao nhiêu lần thì **kết quả như
  nhau**. Vé không bị cấp hai lần, trạng thái đơn không bị chuyển hai lần.
  - *Cơ chế bảo vệ*: **ràng buộc duy nhất** trên `event.id` của lịch sử `Payment event` đã
    xử lý. Lần xử lý đầu ghi dòng lịch sử; các lần sau vi phạm ràng buộc → bị coi là đã xử lý
    và **bỏ qua**, không phải là lỗi.
  - *Ai có thể vi phạm*: bất kỳ đường xử lý webhook nào không ghi `event.id`. Ràng buộc ở tầng
    lưu trữ chặn được cả trường hợp **hai webhook đến cùng lúc** — mà kiểm tra "đã xử lý chưa"
    trong bộ nhớ ứng dụng thì không.
  - *Traces to*: `BR-17`, `FR-38`.

- **INV-02** — Mỗi khoản tiền đã trừ được hoàn **đúng một lần**, và việc hoàn tiền **tự
  động**, không cần quản trị viên can thiệp.
  - *Cơ chế bảo vệ*: **ràng buộc duy nhất** — mỗi `Payment intent` có **nhiều nhất một**
    `Refund`. Lần hoàn thứ hai vi phạm ràng buộc → bị từ chối. Việc "tự động" nằm ở chỗ lệnh
    hoàn tiền được gọi từ luồng xử lý kết quả thanh toán, không phải từ thao tác thủ công.
  - *Ai có thể vi phạm*: webhook đến lặp mà logic không kiểm tra (đã bị chặn bởi `INV-01`), hoặc
    một tác vụ nền chạy lại (retry) và hoàn lần hai — ràng buộc duy nhất chặn cả hai.
  - *Traces to*: `BR-05`, `FR-39`, `FR-24`.

- **INV-03** — Số tiền hoàn **bằng đúng** số tiền đã thu. Trong PRD, hoàn tiền luôn là **toàn
  bộ** cho cả đơn.
  - *Cơ chế bảo vệ*: `Refund` lưu số tiền hoàn, và aggregate kiểm tra số tiền hoàn **bằng**
    số tiền đã thu. Không có khái niệm "hoàn một phần" trong mô hình.
  - *Ai có thể vi phạm*: code gọi Stripe hoàn một khoản nhỏ hơn. Vì "hoàn một phần" không tồn
    tại trong mô hình này, aggregate từ chối — và điều đó là có chủ ý: Stripe cho phép hoàn
    một phần, nhưng nghiệp vụ của ta không có khái niệm đó (đó là việc của lớp dịch ở R5).
  - *Traces to*: `BR-05`, `FR-39`.

- **INV-04** — Không có trạng thái tương đương với `requires_payment_method` của Stripe. Ta chỉ
  có **một** trạng thái "chờ thanh toán" cho tới khi có kết quả.
  - *Cơ chế bảo vệ*: **hình dạng mô hình** — `Payment intent` có các trạng thái theo nghiệp vụ của
    ta, không theo trạng thái của Stripe. Bảng ánh xạ nằm ở lớp dịch R5, không thấm vào bên
    trong.
  - *Ai có thể vi phạm*: mô hình của Stripe lấn át vào bên trong — dấu hiệu là code trong nghiệp
    vụ có nhánh theo trạng thái của Stripe (ví dụ `if (intent.Status == "requires_action")`).
    Vì vậy ranh giới module chặn truy cập trực tiếp vào kiểu dữ liệu của Stripe.
  - *Traces to*: `BR-05`, `FR-39` (lý do tách `Payment` khỏi `Booking` — xem R4).

- **INV-05** — Mỗi `Payment intent` gắn với **đúng một** đơn.
  - *Cơ chế bảo vệ*: mô hình — `Payment intent` mang **một** tham chiếu đơn, và ràng buộc duy
    nhất trên `booking_id` để một đơn không có hai yêu cầu thanh toán song song (tránh trả tiền
    hai lần cho một đơn).
  - *Ai có thể vi phạm*: người dùng bấm "thanh toán" hai lần và tạo hai yêu cầu — ràng buộc duy
    nhất chặn, và lần thứ hai trả về yêu cầu cũ.
  - *Traces to*: `BR-18`, `FR-37`.

### Transaction boundary

Một transaction duy nhất cho **mỗi lần xử lý một thông báo kết quả thanh toán**:

1. Ghi dòng lịch sử `Payment event` với `event.id` (nếu đã có thì dừng — xem `INV-01`);
2. Cập nhật trạng thái `Payment intent`;
3. Nếu thành công: đánh dấu đơn `Paid` và **cấp vé** — cùng transaction.

Ranh giới transaction = **ranh giới aggregate**.

> **Bất biến bị cắt ngang ranh giới.** "Đơn `Paid`" (`Booking`) và "đã cấp vé" (`Ticketing`)
> phải cùng transaction (R6). "Đã thu tiền" (`Payment`) và "đơn `Paid`" (`Booking`) cũng
> phải cùng transaction (R4). Trong modular monolith, tất cả **dùng chung một transaction**.
> Nếu tách thành service: phải có outbox + consumer idempotent, và phải xử lý riêng trường hợp
> **tiền đã thu nhưng đơn đã hết hạn** — khi đó không cấp vé, mà hoàn tiền (`BR-05`).

---

## Hoàn tiền khi đơn đã hết hạn — điểm giao giữa hai context

Tình huống: người xem trả tiền, nhưng thông báo đến **sau** khi đơn đã hết hạn và ghế đã trả về
cho người khác.

| Bước | Context | Kết quả |
|---|---|---|
| Nhận thông báo thành công | `Payment` | `Payment intent` → đã thu |
| Thấy đơn đã `Expired` | `Booking` | Không hồi sinh (`INV-05` ở Booking) |
| Quyết định | `Booking` + `Payment` | **Không cấp vé** |
| Hành động | `Payment` | **Hoàn toàn bộ**, tự động (`INV-02`, `INV-03`) |

Đây là lý do `Payment` là context riêng: quyết định "có hoàn tiền không" dựa trên **cả** tiền
đã thu **và** trạng thái đơn — và hai thứ đó nằm ở hai context khác nhau.

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Trạng thái tiền, yêu cầu thanh toán, hoàn tiền | `Payment` | `Booking` |
| Sự thật về tiền ở thế giới thật | **Stripe** | `Payment` (qua lớp dịch) |
| Trạng thái đơn | `Booking` | `Payment` |
| Vé đã cấp | `Ticketing` | `Payment` (qua đơn) |
