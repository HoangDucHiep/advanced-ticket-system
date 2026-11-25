# Context map

Lớp 2 — Domain. Bản đồ các bounded context và quan hệ giữa chúng.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/06-context-map.md`.
Thuật ngữ: [`ubiquitous-language.md`](ubiquitous-language.md).

## Cách đọc

Bốn điều cần nhớ khi đọc bản đồ này:

1. **Bounded context ≠ transaction boundary.** Bounded context là ranh giới *ngôn ngữ*; transaction
   boundary là ranh giới *atomicity*, chốt ở T07. Trong modular monolith hai thứ này **không trùng
   nhau** — và bản đồ này ghi rõ chỗ nào chúng tách.
2. **Tên tiếng Anh là tên chuẩn trong code.** Xem mục "Quy tắc đặt tên trong code" ở
   [`ubiquitous-language.md`](ubiquitous-language.md).
3. **Mỗi quan hệ có tên dạng quan hệ**, không phải mũi tên trần.
4. **Quan hệ nào đi qua API, quan hệ nào ghi chung transaction** — phải phân biệt. Đây là thông
   tin quan trọng nhất của bản đồ: nó quyết định cái gì sẽ **vỡ** nếu sau này tách service.

## Bối cảnh

Hệ thống đặt vé xem phim. Sáu bounded context, dựa trên 7 journey ở T04.

| Context | Tên tiếng Anh | Trách nhiệm (một câu) |
|---|---|---|
| **Lịch chiếu** | `Scheduling` | Quản lý phim, phòng chiếu, sơ đồ ghế vật lý, lịch chiếu và giá vé. |
| **Tồn kho** | `Inventory` | Quản lý trạng thái của từng ghế của từng suất chiếu, kể cả thông tin giữ (ai giữ, tới lúc nào) và trạng thái bị chặn. |
| **Đặt vé** | `Booking` | Quản lý đơn đặt vé của người xem: ghế đã chọn, giới hạn số ghế, vòng đời đơn. |
| **Thanh toán** | `Payment` | Quản lý tiền: yêu cầu thanh toán, kết quả thanh toán, hoàn tiền — và dịch sang Stripe. |
| **Vé & check-in** | `Ticketing` | Quản lý vé (mã QR) và việc check-in tại cổng. |
| **Danh tính & phân quyền** | `Identity & Access` | Quản lý người dùng và quyền của họ. |

### Thuật ngữ riêng của từng context

Từ nào mang **một nghĩa duy nhất** bên trong context đó, và mang nghĩa **khác** ở context khác.

| Context | Thuật ngữ riêng | Nghĩa bên trong context này |
|---|---|---|
| `Scheduling` | `Movie` | Một tác phẩm được chiếu, có nhiều `Showtime` |
| `Scheduling` | `Room` | Một phòng chiếu vật lý, có nhiều `Room seat` |
| `Scheduling` | `Room seat` | Vị trí ngồi vật lý trong phòng — hàng, số, loại. Không có trạng thái |
| `Scheduling` | `Seat type` | `Standard` / `Couple` / `VIP` — khai báo ở phòng |
| `Scheduling` | `Showtime` | Một phim, một phòng, một khoảng giờ. Trạng thái `NotOnSale` / `OnSale` / `Cancelled` |
| `Scheduling` | `Ticket price` | Giá của cặp (`Seat type`, `Showtime`) |
| `Inventory` | `Showtime seat` | Một ghế của một suất chiếu — **đơn vị bán**, aggregate root (ADR-0005). Có trạng thái riêng |
| `Inventory` | Thông tin giữ (`hold_id`, `held_until`) | "Ghế này đang được giữ bởi lần giữ nào, tới lúc nào" — nằm **trên** dòng ghế |
| `Inventory` | `Available` / `Held` / `Sold` / `Blocked` | Bốn trạng thái của `Showtime seat` |
| `Booking` | `Booking` | Một lần đặt của một người xem, thuộc đúng một suất chiếu, mang `expires_at` |
| `Booking` | `Booking line` | Một ghế trong đơn, cùng giá đã chốt tại lúc giữ |
| `Booking` | `AwaitingPayment` / `Paid` / `Expired` / `Cancelled` | Vòng đời của `Booking` |
| `Payment` | `Payment intent` | Yêu cầu thanh toán cho một đơn |
| `Payment` | `Refund` | Hoàn tiền — luôn là **hoàn toàn bộ** cho cả đơn (`FR-39`) |
| `Payment` | `Payment event` | Thông báo kết quả thanh toán từ Stripe; xử lý đúng một lần (`FR-38`) |
| `Ticketing` | `Ticket` | Mã QR gắn với **một** `Showtime seat`. Sinh ra khi đơn được thanh toán |
| `Ticketing` | `Check-in` | Ghi nhận vé đã quét tại cổng. Mỗi vé **một lần** (`FR-07`) |
| `Ticketing` | `Gate assignment` | **Nhân viên cổng** nào được phân công trực **suất chiếu** nào (`FR-35`, `FR-40`) |
| `Identity & Access` | `Customer` | Người xem đã đăng nhập |
| `Identity & Access` | `Theater manager` | Quản lý rạp — sở hữu lịch chiếu của mình |
| `Identity & Access` | `Gate staff` | Nhân viên cổng — chỉ ghi nhận check-in |
| `Identity & Access` | `Administrator` | Quản trị viên toàn hệ thống |

## Sơ đồ

```mermaid
graph LR
    Identity["Identity & Access<br/>(danh tính, quyền)"]
    Scheduling["Scheduling<br/>(lịch chiếu, sơ đồ ghế, giá)"]
    Inventory["Inventory<br/>(trạng thái ghế, chỗ giữ ghế)"]
    Booking["Booking<br/>(đơn đặt vé)"]
    Payment["Payment<br/>(tiền, hoàn tiền)"]
    Ticketing["Ticketing<br/>(vé, check-in)"]
    Stripe["Stripe<br/>(hệ thống bên ngoài)"]

    Scheduling -->|"Customer/Supplier<br/>cấu hình ghế + giá"| Inventory
    Inventory -->|"Customer/Supplier<br/>tình trạng bán"| Scheduling
    Inventory -->|"Customer/Supplier + ACL<br/>nguồn chuẩn trạng thái ghế"| Booking
    Booking -->|"Customer/Supplier<br/>trạng thái tiền"| Payment
    Payment -->|"Anti-Corruption Layer"| Stripe
    Booking -->|"Customer/Supplier<br/>cấp vé"| Ticketing
    Scheduling -->|"Customer/Supplier<br/>lịch chiếu, trạng thái hủy"| Ticketing
    Scheduling -->|"Customer/Supplier (R9)<br/>giá, trạng thái, giờ chiếu"| Booking
    Identity -->|"Customer/Supplier<br/>quyền truy cập"| Scheduling
    Identity -->|"Customer/Supplier<br/>quyền truy cập"| Booking
    Identity -->|"Customer/Supplier<br/>quyền truy cập"| Ticketing
```

Cơ chế của từng quan hệ: [`cross-context-contracts.md`](cross-context-contracts.md) (T09).

## Quan hệ giữa các context

Mỗi quan hệ ghi rõ: dạng quan hệ, ai gọi ai, hướng dữ liệu, và đi qua API hay ghi chung
transaction.

### R1 — `Scheduling` → `Inventory`

- **Dạng quan hệ:** `Customer/Supplier` — `Scheduling` là Supplier.
- **Ai gọi ai:** `Inventory` đọc từ `Scheduling`.
- **Dữ liệu đi theo hướng nào:** `Scheduling` → `Inventory`.
- **Cơ chế:** **ghi chung transaction** (cùng database, cùng transaction khi **tạo suất chiếu**).

**Dữ liệu đi qua:** sơ đồ ghế của phòng (`Room seat` + `Seat type`). **Không** có giá (giá đi qua
R9) và **không** có danh sách ghế bị chặn (`Blocked` chỉ sống ở `Inventory`).

> **Anti-Corruption Layer (phía `Inventory`).** Khi quản lý rạp tạo một suất chiếu, `Inventory`
> phải **sinh ra** `Showtime seat` từ sơ đồ ghế của phòng. Đây là chỗ mô hình của hai bên gặp
> nhau: `Scheduling` nói "phòng có 100 ghế", `Inventory` nói "mỗi ghế phải có trạng thái riêng".
> Lớp dịch giữ cho phía trong của `Inventory` không phải biết "phòng" và "lịch chiếu" nghĩa là
> gì — nó chỉ nhận "sinh ghế cho suất chiếu này theo cấu hình này".

### R2 — `Inventory` → `Scheduling` (chiều ngược)

- **Dạng quan hệ:** `Customer/Supplier` — `Inventory` là Supplier.
- **Ai gọi ai:** `Scheduling` đọc từ `Inventory`.
- **Dữ liệu đi theo hướng nào:** `Inventory` → `Scheduling`.
- **Cơ chế:** **đọc**, không ghi.

**Dữ liệu đi qua:** bốn con số tình trạng bán của một suất chiếu (`FR-23`) — còn trống / đang
giữ / đã bán / bị chặn.

### R3 — `Inventory` → `Booking`

- **Dạng quan hệ:** `Customer/Supplier` + `Anti-Corruption Layer` phía `Booking`.
- **Ai gọi ai:** `Booking` đọc trạng thái ghế từ `Inventory`; lệnh "giữ ghế" ghi vào **cả hai**.
- **Dữ liệu đi theo hướng nào:** hai chiều, trong **cùng một transaction**.
- **Cơ chế:** **ghi chung transaction** — không gọi API.

**Dữ liệu đi qua:**

- Chiều `Inventory` → `Booking`: kết quả giữ từng ghế (được / ghế nào đã có người), loại ghế.
- Chiều `Booking` → `Inventory`: yêu cầu giữ các ghế cụ thể với `hold_id` (= id đơn) và
  `held_until` (= `expires_at` của đơn); yêu cầu trả ghế theo `hold_id`.

> **Anti-Corruption Layer (phía `Booking`).** `Booking` có mô hình riêng: một đơn chứa các
> `Booking line` với **giá đã chốt tại lúc giữ** và `expires_at`. `Inventory` chỉ có ghế + thông tin
> giữ trên ghế. `Booking` không biết cấu trúc dòng ghế — nó chỉ biết "giữ những ghế này với tham
> chiếu này tới lúc này" và "trả ghế của tham chiếu này".

> **Quan trọng — bất biến bị cắt ngang ranh giới.** Lệnh "giữ ghế" phải làm **hai** việc cùng
> lúc: đặt `Showtime seat` sang `Held` và tạo `Booking` ở `AwaitingPayment`. Nếu chỉ một phía ghi
> được, hệ thống sai. Vì vậy:
>
> - Ranh giới này **không** phải ranh giới transaction. Hai context ghi chung transaction.
> - Tác vụ **giải phóng hết hạn** cũng cắt qua ranh giới: giải phóng ghế (`Inventory`) và
>   chuyển đơn sang `Expired` (`Booking`) phải xảy ra cùng lúc.
> - Nếu sau này tách `Inventory` thành service riêng, **lệnh giữ ghế vỡ** — phải thiết kế lại
>   bằng saga/outbox. Đây là rủi ro lớn nhất của bản đồ này.

### R4 — `Booking` → `Payment`

- **Dạng quan hệ:** `Customer/Supplier` — `Payment` là Supplier.
- **Ai gọi ai:** `Booking` hỏi `Payment` về trạng thái tiền của một đơn.
- **Dữ liệu đi theo hướng nào:** `Booking` → `Payment` (hỏi) và `Payment` → `Booking` (thông báo
  kết quả thanh toán).
- **Cơ chế:** **ghi chung transaction** khi ghi nhận kết quả thanh toán.

**Dữ liệu đi qua:** `Payment intent` gắn với đơn, kết quả thanh toán, trạng thái hoàn tiền.

> **Vì sao tách riêng.** `FR-38` (thông báo đến lặp chỉ xử lý một lần) và `FR-39` (đã thu tiền mà
> không cấp được vé thì phải hoàn lại) là **nghiệp vụ của ta**, không phải của Stripe. Nếu
> `Payment` nằm chung với `Booking`, ranh giới ngôn ngữ sẽ mờ — và khi tách service, phần
> xử lý tiền sẽ đi theo đường nào?

### R5 — `Payment` → `Stripe`

- **Dạng quan hệ:** `Anti-Corruption Layer`.
- **Ai gọi ai:** `Payment` gọi Stripe. Stripe **không** gọi ngược vào nghiệp vụ của ta — nó chỉ
  gửi thông báo, và `Payment` là bên duy nhất tiếp nhận.
- **Dữ liệu đi theo hướng nào:** hai chiều, nhưng chiều về là **thông báo bên ngoài**.
- **Cơ chế:** gọi API bên ngoài + webhook.

**Dữ liệu đi qua:** `payment_intent` (mã định danh phía Stripe), số tiền, đơn vị tiền tệ, và
các sự kiện thanh toán / hoàn tiền.

> **Vì sao bắt buộc phải có lớp dịch.** Mô hình của Stripe lệch mạnh với mô hình của ta:
>
> | Stripe nói | Ta nói |
> |---|---|
> | `payment_intent` | `Booking` |
> | `amount`, `currency` | `Tổng tiền của đơn` |
> | `payment_intent.succeeded` | `Paid` → cấp vé |
> | `charge.refunded` (có thể partial) | `Refund` (luôn toàn bộ theo `FR-39`) |
> | `requires_payment_method` | chưa có trạng thái tương đương — ta chỉ có "chờ thanh toán" |
>
> Stripe không biết "vé" là gì, "ghế" là gì, "hết hạn" là gì. Không có lớp dịch, mô hình của
> Stripe sẽ lấn át vào bên trong và ta sẽ có code kiểu
> `if (paymentIntent.Status == "requires_confirmation")` rải khắp nơi.

### R6 — `Booking` → `Ticketing`

- **Dạng quan hệ:** `Customer/Supplier` — `Ticketing` là Supplier (nguồn chuẩn về vé).
- **Ai gọi ai:** `Booking` gọi `Ticketing` cấp vé khi đơn đã thanh toán.
- **Dữ liệu đi theo hướng nào:** `Booking` → `Ticketing` (yêu cầu cấp vé), và
  `Ticketing` → `Booking` (trạng thái "đơn đã cấp vé").
- **Cơ chế:** **ghi chung transaction** khi cấp vé.

**Dữ liệu đi qua:** danh sách `Showtime seat` cần cấp vé.

> **Quan trọng — bất biến bị cắt ngang ranh giới.** Cấp vé phải **atomic** với `Booking → Paid`.
> Nếu cấp vé xong mà đơn chưa `Paid`, lần thông báo thanh toán đến lại sẽ cấp vé lần hai — vi phạm
> `FR-38` và `SC-04`. Vì vậy ranh giới này là **ghi chung transaction**, không gọi API. (Không cần cờ
> "đã cấp vé" riêng: `Paid` ⇔ đã có vé.)

### R7 — `Scheduling` → `Ticketing`

- **Dạng quan hệ:** `Customer/Supplier` — `Scheduling` là Supplier.
- **Ai gọi ai:** `Ticketing` đọc từ `Scheduling`.
- **Dữ liệu đi theo hướng nào:** `Scheduling` → `Ticketing`.
- **Cơ chế:** **đọc**.

**Dữ liệu đi qua:** suất chiếu của một vé, trạng thái hủy, giờ chiếu (`FR-25`, `FR-24`), chủ sở hữu
suất (để kiểm tra quyền phân công). Phân công nhân viên cổng (`FR-35`) **không** đi qua đây — nó là
aggregate `Gate assignment` của `Ticketing`.

### R9 — `Scheduling` → `Booking`, `Inventory` *(thêm 2026-11-25)*

- **Dạng quan hệ:** `Customer/Supplier` — `Scheduling` là Supplier.
- **Ai gọi ai:** `Booking` và `Inventory` đọc từ `Scheduling`.
- **Cơ chế:** **đọc**, có thể bên trong DB transaction của bên gọi.

**Dữ liệu đi qua:** cho `Booking` — trạng thái, giờ bắt đầu, `Ticket price` theo loại ghế (để chốt
giá lúc giữ). Cho `Inventory` — chủ sở hữu suất (kiểm tra quyền chặn ghế).

> Bản T06 thiếu quan hệ này dù bảng "nguồn sự thật" đã ghi `Booking` đọc giá từ `Scheduling`.

### R8 — `Identity & Access` → các context khác

- **Dạng quan hệ:** `Customer/Supplier` — `Identity & Access` là Supplier.
- **Ai gọi ai:** mọi context gọi `Identity & Access` để kiểm tra quyền.
- **Dữ liệu đi theo hướng nào:** `Identity & Access` → các context khác.
- **Cơ chế:** đọc, trong cùng hệ thống.

**Dữ liệu đi qua:** danh tính người gọi, và các quyền. `FR-10` (không thao tác được đơn và vé của
người khác) và `FR-32` (quản lý rạp chỉ sửa được lịch chiếu của mình) là hai quy tắc dùng
chung này.

> **Vì sao không phải `Shared Kernel`.** Shared Kernel nghĩa là hai context **cùng sở hữu** một
> phần mô hình dùng chung — cùng kiểu dữ liệu, cùng quy ước, đổi một bên thì cả hai đều đổi.
> Ở đây chỉ một bên sở hữu (`Identity & Access`), bên kia chỉ **hỏi**. Đó là quan hệ
> `Customer/Supplier` thuần.

## Nguồn sự thật

Mỗi loại dữ liệu có **đúng một** nơi quyết định nó. Chỗ khác chỉ được **đọc**, không được tự
suy ra.

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Phim, suất chiếu, phòng, sơ đồ ghế vật lý | `Scheduling` | `Inventory`, `Booking`, `Ticketing` |
| Loại ghế của một phòng | `Scheduling` | `Inventory` (chép lúc sinh ghế) |
| Giá vé (theo cặp loại ghế × suất chiếu) | `Scheduling` | `Booking` (R9) |
| Trạng thái của một ghế suất chiếu (kể cả `Blocked`) | `Inventory` | `Booking`, `Ticketing`, `Scheduling` |
| Thông tin giữ trên ghế (`hold_id`, `held_until`) | `Inventory` | `Booking` |
| Đơn đặt vé, ghế đã chọn, giá đã chốt, `expires_at` | `Booking` | `Payment`, `Ticketing` |
| Giới hạn số ghế mỗi người mỗi suất | `Booking` | — |
| Trạng thái tiền, yêu cầu thanh toán, hoàn tiền | `Payment` | `Booking` |
| Vé và trạng thái check-in | `Ticketing` | `Booking` (qua đơn), quản lý rạp (`FR-27`) |
| Phân công nhân viên cổng | `Ticketing` | quản lý rạp |
| Người dùng và quyền | `Identity & Access` | tất cả |
| Sự thật về tiền ở thế giới thật | **Stripe** | `Payment` |

## Đối chiếu với điểm tranh chấp ở T04

Bảng "bản đồ điểm tranh chấp" trong `docs/prd/03-user-journeys.md` là đầu vào cho T12. Mỗi
điểm tranh chấp phải nằm trong một context, và phải ghi rõ: nó cắt ranh giới context nào hay
không.

| Điểm tranh chấp | Context | Cắt ranh giới? | Ghi chú |
|---|---|---|---|
| Giữ 1 ghế | `Inventory` | **có** | Vẫn ghi cả `Inventory` + `Booking` (tạo đơn) — xem R3 |
| Giữ n ghế một lúc | `Inventory` | **có** | All-or-nothing nhiều dòng, cắt qua `Inventory`/`Booking` |
| Xác nhận lại lúc giữ | `Inventory` | không | Đọc lại trạng thái thật, không tin số đã hiển thị |
| Tranh ngược thứ tự | `Inventory` | không | Không deadlock — thứ tự khoá là ở T12 |
| Cấp vé | `Ticketing` | **có** | Phải atomic với `Booking → Paid` — xem R6 |
| Thanh toán đến lặp | `Payment` | không | Idempotency theo `event.id` |
| Thanh toán muộn sau hết hạn | `Booking` + `Payment` | **có** | Không hồi sinh đơn, phải hoàn tiền |
| Hết hạn giữ ghế | `Inventory` + `Booking` | **có** | Giải phóng ghế + chuyển đơn sang `Expired` cùng lúc |
| Hủy đơn | `Booking` + `Inventory` | **có** | Trả ghế về. Đơn chưa trả tiền nên **không** có hoàn tiền |
| Hủy suất chiếu | `Scheduling` + `Booking` + `Inventory` + `Payment` | **có** | Huỷ đơn chờ, yêu cầu hoàn mọi đơn đã trả — đúng một lần mỗi đơn |
| Quét lại vé | `Ticketing` | không | Một vé chỉ check-in một lần |
| Xem sơ đồ ghế | `Scheduling` (đọc) + `Inventory` (trạng thái) | không | Được phục vụ từ bộ đệm ngoài DB — `SC-11` |
| Giao dịch lặp lại | `Booking` | không | Idempotency key theo yêu cầu |

## Nhóm "ranh giới bị cắt" — cần lưu ý nhất

> **Cập nhật 2026-11-25:** danh sách đầy đủ hiện có **năm** chỗ (T08 thêm hủy suất chiếu; review
> thêm tạo suất chiếu) — bảng chuẩn ở [`state-machines.md`](state-machines.md#ranh-giới-bị-cắt--năm-chỗ).
> Bảng dưới đây là ba chỗ gốc của T06, giữ lại để thấy lịch sử.

Ba nơi một ranh giới context **bị cắt** bởi một bất biến (bản T06). Không nơi nào được "sửa" khi tách
thành service — nó phải được thay bằng cơ chế khác (saga, outbox, compensating transaction).

| # | Bất biến bị cắt | Cắt ở đâu | Hậu quả nếu tách service |
|---|---|---|---|
| 1 | "Giữ n ghế: đủ n hoặc không ghế nào" + "ghế `Held` luôn có thời điểm hết hạn" | R3 — lệnh giữ ghế ghi cả `Inventory` và `Booking` | Lệnh giữ ghế phải trở thành nhiều lượt gọi có điều phối |
| 2 | "Cấp vé đúng một lần" | R6 — cấp vé phải atomic với đánh dấu "đơn đã cấp vé" | Phải có outbox + idempotent consumer |
| 3 | "Hết hạn: giải phóng ghế + chuyển đơn sang `Expired` cùng lúc" | R3 — tác vụ nền giải phóng hết hạn | Phải có compensating: giải phóng ghế xong nhưng đơn chưa chuyển |

Điểm 2 là nguy hiểm nhất: nó không chỉ "chậm hơn" mà **sinh ra lỗi sai** (cấp vé hai lần).

Trong modular monolith hiện tại, cả ba đều xử lý trong **một transaction** — đúng. Nhưng khi
T10 (architecture) và T12 (concurrency design) chốt, ba nơi này phải được ghi vào tài liệu
design **nói rõ** bây giờ là một transaction, để sau này không ai "tối ưu" bằng cách tách
service mà không đi theo.

## Các điểm còn mở

| # | Câu hỏi | Chốt ở |
|---|---|---|
| Q16 | ~~Check-in thuộc context nào; `Ticketing` đọc `Scheduling` qua API hay chung database?~~ **Đã đóng**: `Ticketing`; đọc qua interface (T09) | — |
| Q18 | ~~Đặt `Seat hold` ở đâu?~~ **Đã đóng 2026-11-25**: không còn aggregate `Seat hold`; thông tin giữ nằm trên `Showtime seat` (ADR-0005) | — |
| Q19 | ~~Ai giữ lịch sử trạng thái cho `FR-31`?~~ **Đã đóng**: mỗi context giữ lịch sử của mình; `FR-31` ghép `Booking` + `Payment event` | — |
| Q20 | Các "ranh giới bị cắt": module tham gia chung DB transaction thế nào; khi tách service chuyển sang saga/outbox ra sao? | T10 |

## Trạng thái

Bản làm việc, cập nhật sau review 2026-11-25. 6 context, 9 quan hệ R1–R9.
