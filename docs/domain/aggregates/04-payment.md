# Payment — Aggregate và invariant

Bounded context `Payment` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

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
| Trạng thái | thuộc tính | `Unpaid` / `Closed` / `Captured` / `RefundPending` / `Refunded` (ADR-0010) |
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
  - *Cơ chế bảo vệ*: **ràng buộc duy nhất** trên `event.id` của lịch sử `Payment event`. Lần đầu ghi
    dòng lịch sử; các lần sau vi phạm ràng buộc ⇒ coi là đã xử lý, **bỏ qua**, trả thành công cho
    Stripe.
  - *Ai có thể vi phạm*: đường xử lý webhook không ghi `event.id`. Ràng buộc ở tầng lưu trữ chặn
    được cả hai webhook đến cùng lúc.
  - *Traces to*: `BR-17`, `FR-38`.

- **INV-02** — Mỗi khoản tiền đã thu được hoàn **đúng một lần**, **tự động**.
  - *Cơ chế bảo vệ*: hai tầng.
    1. Phía ta: ràng buộc duy nhất — mỗi `Payment intent` có **nhiều nhất một** `Refund`.
    2. Phía Stripe: lời gọi hoàn tiền mang **`Idempotency-Key` = id của `Refund`** — worker gọi
       lại sau timeout thì Stripe vẫn chỉ hoàn một lần.

    Yêu cầu hoàn được **ghi** trong transaction nghiệp vụ (trạng thái `RefundPending` + dòng
    outbox); việc **gọi** Stripe chạy bất đồng bộ (ADR-0006).
  - *Ai có thể vi phạm*: (1) gọi Stripe hoàn tiền ngay trong DB transaction; (2) worker gọi Stripe
    không kèm idempotency key. Ràng buộc "≤ 1 `Refund`" **không** chặn được (2).
  - *Traces to*: `BR-05`, `FR-39`, `FR-24`.

- **INV-03** — Số tiền hoàn **bằng đúng** số tiền đã thu. Không có "hoàn một phần".
  - *Cơ chế bảo vệ*: `Refund` lưu số tiền; aggregate kiểm tra bằng số đã thu.
  - *Traces to*: `BR-05`, `FR-39`.

- **INV-04** — Không có trạng thái tương đương các trạng thái trung gian của Stripe. Thanh toán
  **thất bại** không tạo trạng thái mới và **không** kết thúc intent: intent vẫn `Unpaid`,
  người xem thử lại bằng **cùng** intent. Intent chỉ sang `Closed` khi **đơn** kết thúc mà chưa thu
  tiền (`Expired` / `Cancelled`).
  - *Cơ chế bảo vệ*: hình dạng mô hình; bảng ánh xạ nằm ở lớp dịch R5.
  - *Ai có thể vi phạm*: code nghiệp vụ rẽ nhánh theo trạng thái Stripe
    (`if (intent.Status == "requires_action")`), hoặc "bỏ" intent khi thất bại — khi đó người xem
    không thử lại được vì `INV-05`.
  - *Traces to*: `BR-05`, `FR-39`, J-04.4.

- **INV-05** — Mỗi đơn có **đúng một** `Payment intent`.
  - *Cơ chế bảo vệ*: ràng buộc duy nhất trên `booking_id`. Bấm "thanh toán" lần hai trả về intent
    cũ.
  - *Traces to*: `BR-18`, `FR-37`.

- **INV-06** — Thông báo thành công chỉ dẫn tới cấp vé khi đơn **còn `AwaitingPayment` và còn hạn**.
  Mọi trường hợp khác (đơn `Expired`, `Cancelled`, hoặc quá `expires_at`) ⇒ **không cấp vé, hoàn
  tiền**.
  - *Cơ chế bảo vệ*: điều kiện lệnh của `Booking → Paid` (`expires_at > now()`, ADR-0006). Nếu lệnh
    ảnh hưởng 0 dòng ⇒ nhánh hoàn tiền.
  - *Traces to*: `BR-04`, `BR-05`, `FR-06`, `FR-39`.

### Transaction boundary

Một DB transaction cho **mỗi thông báo kết quả thanh toán**:

1. Ghi `Payment event` với `event.id` (đã có ⇒ dừng, trả thành công);
2. Theo loại thông báo:
   - **thành công** + đơn còn `AwaitingPayment` và còn hạn ⇒ intent → `Captured`, đơn → `Paid`, ghế
     → `Sold`, cấp vé (R4 + R6);
   - **thành công** + đơn không còn nhận thanh toán (intent `Unpaid` của đơn quá hạn, hoặc intent
     `Closed`) ⇒ intent → `RefundPending`, tạo `Refund` + dòng outbox;
   - **thất bại** ⇒ chỉ ghi lịch sử; intent giữ `Unpaid`;
   - **hoàn tiền xong** ⇒ `RefundPending` → `Refunded`.

**Không** có lời gọi Stripe nào trong transaction. Gọi Stripe (hoàn tiền, huỷ intent) đi qua outbox.

**Lỗi không mong đợi** trong transaction (ví dụ ràng buộc vé bị vi phạm dù không thể xảy ra) ⇒
rollback **cả** dòng `Payment event` ⇒ Stripe gửi lại. Quá số lần thử ⇒ đưa vào danh sách xử lý tay
và cảnh báo quản trị viên — tiền đã thu nên **không được im lặng bỏ qua**.

> **Bất biến bị cắt ngang ranh giới.** "Đã thu tiền" (`Payment`), "đơn `Paid`" (`Booking`), "ghế
> `Sold`" (`Inventory`) và "đã cấp vé" (`Ticketing`) cùng một DB transaction (R4, R6). Nếu tách
> service: outbox + consumer idempotent.

---

## Tiền đến muộn — điểm giao giữa hai context

| Bước | Context | Kết quả |
|---|---|---|
| Nhận thông báo thành công | `Payment` | Ghi `Payment event` |
| Thử `Booking → Paid` | `Booking` | 0 dòng ảnh hưởng: đơn đã `Expired`/`Cancelled` **hoặc** đã quá `expires_at` |
| Quyết định | `Payment` | **Không cấp vé** |
| Hành động | `Payment` | Intent → `RefundPending`; worker hoàn **toàn bộ** qua Stripe với idempotency key |

Đây là lý do `Payment` là context riêng: quyết định "có hoàn tiền không" dựa trên **cả** tiền đã
thu **và** trạng thái đơn.

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Trạng thái tiền, yêu cầu thanh toán, hoàn tiền | `Payment` | `Booking`, quản trị viên (`FR-31`) |
| Sự thật về tiền ở thế giới thật | **Stripe** | `Payment` (qua lớp dịch) |
| Trạng thái đơn, `expires_at` | `Booking` | `Payment` |
