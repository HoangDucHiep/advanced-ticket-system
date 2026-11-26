# Ticketing — Aggregate và invariant

Bounded context `Ticketing` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`GLOSSARY.md`](../../../GLOSSARY.md). Vòng đời: [`../state-machines.md`](../state-machines.md#state-machine-ticket).

## Trách nhiệm

Quản lý vé (mã QR), việc check-in tại cổng, và **phân công nhân viên cổng** cho suất chiếu.

`Ticketing` **không** biết giá, không biết đơn đặt vé, không biết trạng thái ghế. Nó chỉ biết
"vé này của ghế này, thuộc suất chiếu này", "vé này đã được quét hay chưa", và "nhân viên cổng
này được phân công trực suất nào".

---

## Aggregate: `Ticket`

**Bản chất**: entity. Một vé — có định danh (mã QR), có vòng đời, thuộc **đúng một** ghế của
**đúng một** suất chiếu.

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| Mã vé (QR) | value object / định danh | Mã có chữ ký; chữ ký được xác minh **tại server** lúc quét (check-in offline đã bị loại — `NR-15`) |
| Tham chiếu ghế (`Showtime seat id`) | tham chiếu | Vé gắn với **một** ghế |
| Tham chiếu suất chiếu | tham chiếu | Vé thuộc **một** suất chiếu |
| Tham chiếu đơn (`Booking id`) | tham chiếu | Vé đến từ đơn nào |
| Chủ sở hữu (`Customer id`) | tham chiếu (bản sao lúc cấp) | Chép từ event `BookingPaid` để tự kiểm tra chủ sở hữu (`FR-10`) |
| Bản ghi check-in | entity | Vé đã quét lúc nào, ở cổng nào |

**Boundary**: nằm **ngoài** aggregate này —

- `Booking` (thuộc `Booking`) — đơn đặt vé, trạng thái đơn, lịch sử;
- `Showtime` (thuộc `Scheduling`) — suất chiếu, giờ chiếu, trạng thái hủy (đọc qua R7);
- `Gate assignment` — aggregate riêng **trong** `Ticketing` (xem bên dưới);
- `Payment` (thuộc `Payment`) — trạng thái tiền;
- `Identity & Access` — người quét (quyền, không phải dữ liệu vé).

### Invariants

- **INV-01** — Mỗi vé gắn với **đúng một** ghế của **đúng một** suất chiếu, và mỗi ghế sinh
  ra **đúng một** vé.
  - *Cơ chế bảo vệ*: ràng buộc **duy nhất** theo ghế ở tầng lưu trữ — một ghế không thể có hai vé. Và
    mỗi vé mang **một** tham chiếu suất chiếu, nên vé không thể thuộc hai suất.
  - *Ai có thể vi phạm*: code cấp vé lại cho cùng một ghế (ví dụ thông báo thanh toán đến lặp và
    cấp vé lần hai). Ràng buộc chặn bất kể logic ứng dụng có kiểm tra hay không.
  - *Traces to*: `BR-09`, `FR-05`, `FR-08`, `FR-16`.

- **INV-02** — Vé **chỉ tồn tại sau khi** đơn đã thanh toán thành công. Trước đó, đơn không có
  vé nào.
  - *Cơ chế bảo vệ*: cấp vé nằm trong **cùng lần ghi atomic** với `Booking: AwaitingPayment → Paid`
    (xem [`03-booking.md`](03-booking.md) `INV-04`). Không có đường nào tạo vé cho một đơn chưa
    thanh toán — lệnh cấp vé chỉ được gọi từ đúng lần ghi đó.
  - *Ai có thể vi phạm*: một endpoint cấp vé riêng, không nằm trong luồng thanh toán. Đây là
    lý do quyền tạo vé phải thuộc `Ticketing` **và** chỉ được gọi từ đúng một chỗ.
  - *Traces to*: `BR-09`, `BR-17`, `FR-05`, `FR-38`.

- **INV-03** — Một vé chỉ ghi nhận check-in **một lần duy nhất**, trong suất chiếu của nó, ở một
  cổng.
  - *Cơ chế bảo vệ*: **ràng buộc duy nhất** theo vé ở bản ghi check-in. Hai nhân viên cổng quét cùng
    một vé cùng lúc: một lượt thành công, lượt còn lại bị từ chối. Chặn được cả **hai lượt quét đồng
    thời**, mà kiểm tra "đã check-in chưa" trong ứng dụng thì không. Cơ chế:
    [`../../design/concurrency.md`](../../design/concurrency.md) #8 (ADR-0013).
  - *Ai có thể vi phạm*: không ai, miễn là mọi lượt quét thành công đều ghi bản ghi check-in (lượt quét
    thất bại thì không được ghi — xem `INV-05`).
  - *Traces to*: `BR-10`, `FR-07`, `FR-25`.

- **INV-04** — Mỗi lượt check-in thành công được **đếm** vào suất chiếu, và tổng số lượt ghi
  nhận của một suất chiếu **không vượt quá** số vé đã bán của suất chiếu đó.
  - *Cơ chế bảo vệ*: đây là **hệ quả trực tiếp** của `INV-03` — mỗi vé một lượt, và mỗi vé
    chỉ sinh ra khi đơn đã thanh toán. Số vé đã bán = số vé tồn tại; số lượt check-in = số dòng
    ghi nhận. Vì mỗi vé có nhiều nhất một dòng ghi nhận, số lượt ≤ số vé.
  - *Ai có thể vi phạm*: không ai, nếu `INV-03` giữ. Ghi lại để thấy rõ đây là invariant **dẫn
    xuất**, không phải invariant độc lập — và để trả lời câu hỏi "số check-in có vượt số vé
    không" bằng phép đếm chứ không bằng niềm tin.
  - *Traces to*: `BR-25`, `FR-27`.

- **INV-05** — Một lượt quét lại vé đã check-in phải bị **từ chối**, kèm **thời điểm** và
  **cổng** đã check-in lần đầu. Không ghi thêm một dòng ghi nhận nào.
  - *Cơ chế bảo vệ*: khi ràng buộc duy nhất (`INV-03`) bị vi phạm, hệ thống **đọc** dòng ghi
    nhận đã có để trả lời. Dòng ghi nhận chỉ có **một** và không bao giờ bị sửa.
  - *Ai có thể vi phạm*: code ghi đè dòng ghi nhận cũ thay vì từ chối — đây là lỗi làm mất
    dấu vết, và làm hỏng chính thứ mà `BR-25` cần để đếm.
  - *Traces to*: `BR-10`, `FR-07`.

- **INV-06** — Vé mang **suất chiếu** và **ghế** của nó, kèm mã dùng được tại cổng. Danh sách vé
  chỉ chứa vé **đã thanh toán**.
  - *Cơ chế bảo vệ*: `Ticket` mang các tham chiếu này như **dữ liệu bắt buộc** — không có
    vé mà thiếu suất chiếu hoặc thiếu ghế. Phần "chỉ chứa vé đã thanh toán" được bảo đảm bởi
    `INV-02`.
  - *Ai có thể vi phạm*: code tạo vé với tham chiếu null — bị chặn bởi ràng buộc bắt buộc ở
    tầng lưu trữ.
  - *Vé của suất đã hủy*: vẫn nằm trong danh sách (người xem đã trả tiền), nhưng hiển thị rõ "suất
    đã hủy — đã/đang hoàn tiền" và **không** dùng được để check-in. Trạng thái hủy đọc từ
    `Scheduling` (R7), trạng thái hoàn tiền từ `Payment`.
  - *Traces to*: `BR-30`, `FR-17`, `FR-24`.

- **INV-07** — Mỗi vé thuộc về **một người xem** (chủ sở hữu của đơn), và người xem đó chỉ
  thấy vé của chính mình.
  - *Cơ chế bảo vệ*: vé mang **bản sao** `Customer id` (chép lúc cấp vé — chủ đơn không đổi), nên
    `Ticketing` tự kiểm tra chủ sở hữu ở biên aggregate, không phải hỏi `Booking`. Với quyền theo vai trò (quản lý rạp xem vé của rạp mình)
    thì điều kiện là **sở hữu lịch chiếu**.
  - *Ai có thể vi phạm*: endpoint tải vé bằng mã vé mà không kiểm tra chủ sở hữu — mã QR
    chính là thứ mang tính bí mật, nên lộ mã là lộ vé.
  - *Traces to*: `BR-14`, `FR-10`, `FR-17`.

### Ranh giới atomic

Một lần ghi atomic cho **mỗi lượt quét vé**:

1. Kiểm tra quyền người quét (vai trò + `Gate assignment`); vé tồn tại, đúng chữ ký, thuộc đúng
   suất đang quét, và suất **chưa bị hủy**;
2. Ghi **một** dòng ghi nhận check-in (chứa thời điểm, cổng, người quét).

Nếu bản ghi đó đã tồn tại → lượt quét bị **từ chối** và hệ thống trả lại thời điểm + cổng của lượt
quét đầu tiên. Ranh giới atomic = **ranh giới aggregate**: một lượt quét = một lần ghi.

> **Bất biến bị cắt ngang ranh giới** #2: "đơn đã thanh toán" (`Booking`) và "đã cấp vé" (`Ticketing`)
> xảy ra cùng lúc (R6) — xem [`README.md`](README.md#ranh-giới-bị-cắt--năm-chỗ). Làm sai thì **cấp vé
> hai lần**, vi phạm `BR-17`.

---

## Aggregate: `Gate assignment`

**Bản chất**: entity, aggregate root. Một lần phân công: **một nhân viên cổng** trực **một suất
chiếu**.

**Chứa**: tham chiếu `Showtime id`, tham chiếu người dùng (`Gate staff`), người phân công, thời
điểm phân công.

> **Vì sao ở `Ticketing`, không ở `Scheduling`.** Phân công cổng chỉ được dùng cho đúng một việc:
> kiểm tra quyền check-in (`FR-35`). Đặt nó cạnh nơi dùng giúp lệnh check-in không phải gọi sang
> context khác để kiểm tra quyền. Nghĩa của nó là **nhân viên ↔ suất**. "Cổng" trong bản ghi check-in là **nơi quét** (nhãn thiết bị/vị trí), không phải đối
> tượng được phân công.

### Invariants

- **INV-08** — Nhân viên cổng chỉ ghi nhận check-in được vé thuộc suất chiếu họ được phân công;
  quản trị viên được check-in mọi suất.
  - *Cơ chế bảo vệ*: lệnh check-in kiểm tra tồn tại `Gate assignment (showtime, người quét)`. Ràng
    buộc duy nhất theo (suất, nhân viên) để không phân công trùng.
  - *Ai có thể vi phạm*: chỉ kiểm tra vai trò mà bỏ qua phân công.
  - *Traces to*: `BR-15`, `FR-35`, `FR-40`.

- **INV-09** — Chỉ **quản lý rạp sở hữu suất chiếu** (hoặc quản trị viên) được tạo/gỡ phân công,
  và chỉ phân công được người dùng có vai trò `Gate staff`.
  - *Cơ chế bảo vệ*: kiểm tra chủ sở hữu suất qua `Scheduling` (R7) và vai trò qua
    `Identity & Access` (R8) ở lệnh phân công.
  - *Traces to*: `BR-16`, `FR-40`.

---

## Invariant không bảo vệ được bằng một aggregate

Mọi invariant của `Ticketing` đều thuộc phạm vi một aggregate (`Ticket` hoặc `Gate assignment`)
hoặc là hệ quả trực tiếp của nó. Điểm đáng chú ý: `BR-10` — "mỗi vé chỉ check-in một lần" — nghe thì
cần một lần ghi atomic **xuyên nhiều** vé, nhưng thực ra **không**: nó là invariant về **một
vé**, và hai lượt quét đồng thời là hai lệnh cùng tranh **một dòng**. Ràng buộc duy nhất
là đủ.

Một invariant trông "toàn cục" có thể hoá "cục bộ" nếu đặt ranh giới aggregate đúng chỗ; ngược lại `BR-08`
trông cục bộ nhưng hoá toàn cục — xem [`README.md`](README.md#invariant-không-bảo-vệ-được-bằng-một-aggregate).

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Vé và trạng thái check-in | `Ticketing` | người xem (vé của mình), quản lý rạp (`FR-27`) |
| Phân công nhân viên cổng cho suất chiếu (`Gate assignment`) | `Ticketing` | quản lý rạp |
| Suất chiếu, giờ chiếu, trạng thái hủy | `Scheduling` | `Ticketing` (R7) |
| Đơn và chủ sở hữu đơn | `Booking` | `Ticketing` — chỉ qua event `BookingPaid` (bản sao lên vé) |
| Quyền của người quét | `Identity & Access` | `Ticketing` |
