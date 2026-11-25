# 08: State machines

Mode: co-design
Gate: B
Layer: domain
Blocked by: 07
Traces to: —

## Vì sao có ticket này

Mọi hệ thống có trạng thái đều có cùng một lỗi kinh điển: **chuyển trạng thái chạy hai lần**.
Webhook Stripe đến lặp. Người dùng bấm "hủy" hai lần. Worker chạy lại sau một sự cố.

Giải pháp không phải là "cẩn thận hơn" — mà là **chuyển trạng thái là một hành vi idempotent**:
đã chuyển rồi thì chuyển lại không làm gì, và việc đó được bảo đảm bởi cơ chế, không bởi kỷ
luật code.

State machine là nơi bạn vẽ ra "hành vi đúng" một lần, rồi mọi nơi khác tuân theo. Đó là
lý do nó là artifact của domain chứ không phải của design.

## Bạn tạo ra

`docs/domain/state-machines.md`, mỗi máy trạng thái một mục:

```
## State machine: <tên>

<Đơn vị nào có vòng đời này>

### States

- `<tên trạng thái>` — <mô tả>

### Transitions

| Từ | Sự kiện | Đến | Tác dụng phụ |
|---|---|---|---|
| | | | |

### Transitions bị từ chối

| Từ | Sự kiện | Kết quả mong đợi |
|---|---|---|

### Điều kiện tiên quyết

- <điều gì phải đúng trước khi chuyển>
```

Bắt buộc có bảng **"Transitions bị từ chối"** — đó là phần hay nhất và cũng là phần hay bị
bỏ sót. Nó chính là nơi định nghĩa "idempotent" và "không hồi sinh đơn đã hết hạn".

Máy trạng thái cần vẽ (ít nhất): **ghế của suất chiếu** (seat state), **đơn đặt vé**
(booking), **thanh toán** (payment), **vé** (ticket / check-in).

Câu hỏi gợi ý:

1. Mỗi trạng thái bắt đầu từ đâu, kết thúc ở đâu? Trạng thái đầu và trạng thái cuối có
   "chết" không, hay luôn có đường quay lại?
2. Sự kiện nào có thể đến **trễ**? (Webhook đến sau khi đơn đã hết hạn — chuyển trạng thái
   nào phải bị từ chối?)
3. Sự kiện nào có thể đến **lặp**? Chuyển trạng thái đó có idempotent không?
4. Sự kiện nào có thể đến **sai thứ tự**? (Webhook thanh toán đến trước cả lần tạo đơn?)
5. Tác dụng phụ của mỗi transition là gì — có thay đổi trạng thái ghế không, có phát sinh vé
   không, có ghi outbox không?
6. **Ghế của suất chiếu** và **đơn đặt vé** có chuyển trạng thái cùng lúc không? Nếu một
   transition của đơn kéo theo nhiều transition của ghế, thì chúng phải atomic.
7. Khi một đơn bị huỷ hoặc hết hạn, ghế đã giữ trở lại trạng thái nào — và điều đó là
   idempotent không (chạy hai lần có hại không)?

## AI làm gì

AI hỏi từng máy trạng thái một. Với mỗi transition AI hỏi:

- "Chuyển này có thể xảy ra lần hai không? Nếu có thì kết quả mong đợi là gì?"
- "Chuyển này có tác dụng phụ lên aggregate khác không? Cái đó nằm trong cùng transaction
  không?"
- "Nếu transition này chạy nửa chừng rồi hỏng, trạng thái còn nhất quán không?"

Nếu một trạng thái hoặc transition còn mơ hồ, AI hỏi lại cho tới khi rõ. AI ghi vào
`docs/domain/state-machines.md`.

**Lưu ý:** nếu có câu hỏi mà trên giấy không trả lời được (ví dụ: "nếu giữ ghế hết hạn đúng
lúc webhook thanh toán đến thì kết quả mong đợi là gì?"), dừng lại và dùng skill `/prototype`
để dựng một mô hình nhỏ chạy được, rồi mới quay lại chốt. Đó là đúng công dụng của prototype.

## Tiêu chí hoàn thành

- [x] Có ít nhất 4 máy trạng thái: ghế của suất chiếu, đơn đặt vé, thanh toán, vé
      — **5 máy**: thêm `Showtime` (chưa mở bán / đang bán / đã hủy) cho `BR-13`
- [x] Mỗi máy có bảng transitions và bảng **transitions bị từ chối**
- [x] Mọi transition có tác dụng phụ được ghi rõ
- [x] Có nêu rõ transition nào phải idempotent, và idempotent bằng cơ chế nào
- [x] Có nêu rõ điều kiện tiên quyết của transition
- [x] Có chỉ rõ transition nào kéo theo thay đổi trạng thái ghế, và chúng atomic với nhau
- [x] Tổng các transition đã ghi đủ chưa — mỗi từ `FR-nn` liên quan tới vòng đời đều có
      một transition tương ứng

## Ghi chú khi hoàn thành (2026-11-24)

Artifact: [`docs/domain/state-machines.md`](../../../docs/domain/state-machines.md).

- **5 máy trạng thái**, mỗi máy có đủ 4 phần (States / Transitions / Transitions bị từ chối /
  Điều kiện tiên quyết) và mỗi máy có bảng **Idempotency** riêng ghi rõ transition nào phải
  idempotent và idempotent **bằng cơ chế nào**.
- Máy `Showtime` được thêm ngoài 4 máy ticket yêu cầu, vì `BR-13` (chỉ đặt vé được suất chiếu
  **đang bán**) và `FR-22`/`FR-34` là hành vi vòng đời của `Showtime` — không thuộc máy nào
  khác.
- **Vé bị vô hiệu do hủy suất chiếu** (`FR-24`) được xử lý như **tác dụng phụ** của transition
  `OnSale → Cancelled`, không phải một trạng thái thứ ba của `Ticket`. Máy `Ticket` chỉ có 2
  trạng thái: `Issued` → `CheckedIn`.
- **`Payment intent` không có trạng thái "thất bại"** — kết quả thất bại làm intent bị **bỏ**,
  đơn đi theo đường `Expired`/`Cancelled` bình thường. Bám theo `INV-04` của `Payment`.
- Tên trạng thái chốt: `AwaitingPayment` (đơn chờ thanh toán — **không** dùng `Pending` vì
  glossary cấm), `Paid`, `Expired`, `Cancelled` (`Booking`); `AwaitingPayment`, `Captured`,
  `Refunded` (`Payment intent` — dùng `Captured` thay vì `Paid` để không trùng tên với
  `Booking`); `Issued`, `CheckedIn` (`Ticket`); `NotOnSale`, `OnSale`, `Cancelled` (`Showtime`).
- **Race "hết hạn đúng lúc webhook đến" chốt được trên giấy**, không cần `/prototype`: cả hai
  luồng ghi vào **cùng một trạng thái nguồn** (`AwaitingPayment`) của cùng một aggregate bằng
  **điều kiện lệnh** ⇒ chỉ một thắng, nhánh thua được định nghĩa sẵn (webhook đến sau ⇒ không
  cấp vé, hoàn toàn bộ tiền).
- Phát hiện **ranh giới bị cắt thứ tư**: hủy suất chiếu cắt qua ba context (`Scheduling` →
  `Ticketing` + `Payment`). Khác ba chỗ đã biết ở T06 ở chỗ nó chạy **nhiều đơn**, không phải
  một aggregate.
- Bảng truy vết phủ **22 `FR-nn`** (20 có transition riêng + 2 là hệ quả) — bảng "FR không có
  transition riêng" giải thích `FR-08` và `FR-19`.
- Câu hỏi mới: Q25–Q29 trong [`docs/learning/open-questions.md`](../../../docs/learning/open-questions.md).
