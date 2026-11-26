# Payment — Aggregate và invariant

Bounded context `Payment` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`GLOSSARY.md`](../../../GLOSSARY.md). Vòng đời: [`../state-machines.md`](../state-machines.md#state-machine-payment-intent).
Quyết định luồng tiền: [ADR-0006](../../adr/0006-payment-expiry-refund-flow.md).

## Trách nhiệm

Quản lý tiền: yêu cầu thanh toán, kết quả thanh toán, hoàn tiền — và dịch sang Stripe.

`Payment` **không** biết vé, ghế, suất chiếu là gì. Nó biết "đơn này cần thu bao nhiêu" và
"tiền đã về chưa, đã hoàn chưa".

---

## Aggregate: `Payment intent`

**Bản chất**: entity, aggregate root. Một yêu cầu thanh toán cho một đơn.

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| Trạng thái | thuộc tính | Năm trạng thái, xem state machine (ADR-0010) |
| Số tiền, đơn vị tiền tệ | value object | Số tiền cần thu (VND — đơn vị không có phần lẻ) |
| Tham chiếu đơn (`Booking id`) | tham chiếu | Tiền của đơn nào |
| Mã định danh phía Stripe (`payment_intent` id) | value object | Khoá sang hệ thống bên ngoài |
| Lịch sử `Payment event` đã nhận | append-only | Mọi thông báo đã nhận, kèm `event.id` (chuỗi `evt_…` của Stripe) |
| `Refund` | entity | Yêu cầu hoàn tiền (nhiều nhất một) |

**Boundary**: nằm **ngoài** aggregate này —

- `Booking` — trạng thái đơn, `expires_at`;
- `Stripe` — hệ thống bên ngoài, sau **Anti-Corruption Layer** (R5).

### Invariants

- **INV-01** — Cùng một thông báo kết quả thanh toán, nhận bao nhiêu lần thì **kết quả như nhau**.
  - *Cơ chế bảo vệ*: **ràng buộc duy nhất** theo `event.id` trong lịch sử `Payment event`. Lần đầu ghi
    lịch sử; các lần sau coi là đã xử lý và **bỏ qua**. Cơ chế:
    [`../../design/concurrency.md`](../../design/concurrency.md) #7.
  - *Ai có thể vi phạm*: đường xử lý thông báo không ghi `event.id`. Ràng buộc ở tầng lưu trữ chặn
    được cả hai thông báo đến cùng lúc.
  - *Traces to*: `BR-17`, `FR-38`.

- **INV-02** — Mỗi khoản tiền đã thu được hoàn **đúng một lần**, **tự động**.
  - *Cơ chế bảo vệ*: hai tầng.
    1. Phía ta: mỗi `Payment intent` có **nhiều nhất một** `Refund`.
    2. Phía Stripe: lời gọi hoàn tiền mang khoá idempotency gắn với `Refund` — gọi lại sau timeout
       thì Stripe vẫn chỉ hoàn một lần.

    Yêu cầu hoàn được **ghi** trong lần ghi nghiệp vụ (trạng thái `RefundPending`); việc **gọi**
    Stripe chạy bất đồng bộ (ADR-0006).
  - *Ai có thể vi phạm*: (1) gọi Stripe hoàn tiền ngay trong lần ghi atomic; (2) gọi Stripe không
    kèm khoá idempotency. Ràng buộc "≤ 1 `Refund`" **không** chặn được (2).
  - *Traces to*: `BR-05`, `FR-39`, `FR-24`.

- **INV-03** — Số tiền hoàn **bằng đúng** số tiền đã thu. Không có "hoàn một phần".
  - *Cơ chế bảo vệ*: `Refund` lưu số tiền; aggregate kiểm tra bằng số đã thu.
  - *Traces to*: `BR-05`, `FR-39`.

- **INV-04** — Không có trạng thái tương đương các trạng thái trung gian của Stripe. Thanh toán
  **thất bại** không tạo trạng thái mới và **không** kết thúc intent: intent vẫn `Unpaid`,
  người xem thử lại bằng **cùng** intent. Intent chỉ sang `Closed` khi **đơn** kết thúc mà chưa thu
  tiền (`Expired` / `Cancelled`).
  - *Cơ chế bảo vệ*: hình dạng mô hình; bảng ánh xạ nằm ở lớp dịch R5 ([`../cross-context-contracts.md`](../cross-context-contracts.md)).
  - *Ai có thể vi phạm*: code nghiệp vụ rẽ nhánh theo trạng thái Stripe
    (`if (intent.Status == "requires_action")`), hoặc "bỏ" intent khi thất bại — khi đó người xem
    không thử lại được vì `INV-05`.
  - *Traces to*: `BR-05`, `FR-39`, J-04.4.

- **INV-05** — Mỗi đơn có **đúng một** `Payment intent`.
  - *Cơ chế bảo vệ*: ràng buộc duy nhất theo đơn. Bấm "thanh toán" lần hai trả về intent cũ.
  - *Traces to*: `BR-18`, `FR-37`.

- **INV-06** — Thông báo thành công chỉ dẫn tới cấp vé khi đơn **còn `AwaitingPayment` và còn hạn**.
  Mọi trường hợp khác (đơn `Expired`, `Cancelled`, hoặc quá `expires_at`) ⇒ **không cấp vé, hoàn
  tiền**.
  - *Cơ chế bảo vệ*: điều kiện của `Booking → Paid` (`expires_at > now`, ADR-0006). Điều kiện không
    thoả ⇒ nhánh hoàn tiền. Race "thanh toán sát hạn":
    [`../state-machines.md`](../state-machines.md#race-thông-báo-thanh-toán-đến-sát-thời-điểm-hết-hạn).
  - *Traces to*: `BR-04`, `BR-05`, `FR-06`, `FR-39`.

### Ranh giới atomic

Một lần ghi atomic cho **mỗi thông báo kết quả thanh toán**: ghi `Payment event` (đã có `event.id` ⇒
dừng), rồi theo loại thông báo — các nhánh nằm trong bảng transitions của
[`../state-machines.md`](../state-machines.md#state-machine-payment-intent). **Không** có lời gọi
Stripe nào trong lần ghi này; gọi Stripe (hoàn tiền, huỷ intent) chạy bất đồng bộ.

**Lỗi không mong đợi** ⇒ rollback **cả** dòng `Payment event` để Stripe gửi lại; quá số lần thử ⇒ đưa
vào danh sách xử lý tay và cảnh báo quản trị viên — tiền đã thu nên **không được im lặng bỏ qua**
(cơ chế: [`../../design/data-model.md`](../../design/data-model.md)).

> **Bất biến bị cắt ngang ranh giới** #2: "đã thu tiền" (`Payment`), "đơn `Paid`" (`Booking`), "ghế
> `Sold`" (`Inventory`) và "đã cấp vé" (`Ticketing`) cùng một lần ghi atomic (R4, R6) — xem
> [`README.md`](README.md#ranh-giới-bị-cắt--năm-chỗ).

---

## Tiền đến muộn

Thông báo thành công đến khi đơn đã `Expired`/`Cancelled` hoặc đã quá `expires_at` ⇒ `Booking → Paid`
bị từ chối ⇒ `Payment` **không cấp vé**, intent → `RefundPending`, hoàn **toàn bộ**. Đây là lý do
`Payment` là context riêng: quyết định "có hoàn tiền không" dựa trên **cả** tiền đã thu **và** trạng
thái đơn. Diễn biến theo thời gian: [`../state-machines.md`](../state-machines.md#race-thông-báo-thanh-toán-đến-sát-thời-điểm-hết-hạn).

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Trạng thái tiền, yêu cầu thanh toán, hoàn tiền | `Payment` | `Booking`, quản trị viên (`FR-31`) |
| Sự thật về tiền ở thế giới thật | **Stripe** | `Payment` (qua lớp dịch) |
| Trạng thái đơn, `expires_at` | `Booking` | `Payment` |
