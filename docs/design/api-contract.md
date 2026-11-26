# API contract

Lớp 3 — Design. Cơ chế đằng sau: [`concurrency.md`](concurrency.md);
bảng: [`data-model.md`](data-model.md); module sở hữu endpoint: [`architecture.md`](architecture.md) D9.

---

## Quy ước chung

| Quy ước | Quyết định | Vì sao |
|---|---|---|
| Base path | `/api` | |
| Định dạng | JSON, `camelCase`; thời gian ISO-8601 UTC (`2026-12-01T12:00:00Z`); tiền là số nguyên **đồng** (`"amount": 90000`) | VND không có phần lẻ |
| Id | UUID dạng chuỗi | |
| Xác thực | `Authorization: Bearer <access token>` (JWT, 15 phút) | |
| Vai trò | **Đọc từ `IdentityAccess`**, không tin `role` trong JWT (cache có `role_version`) | `BR-26` — thu quyền có hiệu lực ngay |
| Tài nguyên của người khác | **`404`**, không `403` | `FR-10`: không rò rỉ việc tài nguyên tồn tại |
| Nhóm đường dẫn | Công khai / người xem: `/api/...`; quản lý rạp: `/api/manager/...`; quản trị: `/api/admin/...`; cổng: `/api/check-in/...` | Đọc đường dẫn là biết cần vai trò gì; chính sách phân quyền gắn theo nhóm |
| Phân trang | `?page=1&pageSize=20` (tối đa 100); response `{ items, page, pageSize, totalCount }` | Danh sách ở đây nhỏ (đơn của một người, người dùng cho quản trị) — offset đủ và dễ hiểu. Keyset pagination để dành khi có số đo cần nó |
| Sắp xếp mặc định | Mới nhất trước (`createdAt` giảm dần); lịch chiếu: `startsAt` tăng dần | |
| Thời gian giữ ghế | Cố định 15 phút (`BR-03`), không cấu hình theo suất | Quy tắc nghiệp vụ một giá trị; giá trị nằm trong cấu hình ứng dụng để test rút ngắn được |
| Phiên bản API | Không đặt version trong đường dẫn | Một client, một repo; thêm `/v2` khi có thay đổi phá vỡ |

### Định dạng lỗi — `application/problem+json` (RFC 9457)

```json
{
  "type": "https://errors.ticket.local/seats-unavailable",
  "title": "Một số ghế đã có người",
  "status": 409,
  "code": "SEATS_UNAVAILABLE",
  "detail": "Ghế D7 vừa được người khác giữ.",
  "action": "Chọn ghế khác trên sơ đồ rồi thử lại.",
  "traceId": "00-4bf9…",
  "unavailableSeatIds": ["…"]
}
```

`code` là chuỗi ổn định cho máy; `action` là **hành động tiếp theo** cho người — mọi lỗi đều có
(`FR-36`, `BR-19`). Trường thêm (ví dụ `unavailableSeatIds`) tuỳ `code`.

### Bảng mã lỗi toàn cục

| Status | Khi nào | Client nên làm gì |
|---|---|---|
| `400` | Request sai cú pháp / sai kiểu / thiếu trường bắt buộc / chữ ký webhook sai | Sửa request; **không** retry nguyên văn |
| `401` | Thiếu / hết hạn access token; refresh token không hợp lệ hoặc bị dùng lại | Gọi `/auth/refresh`; nếu refresh cũng `401` ⇒ đăng nhập lại |
| `403` | Đã đăng nhập nhưng **vai trò** không đủ, hoặc nhân viên cổng không được phân công suất | Không retry; báo người dùng không có quyền |
| `404` | Không tồn tại, **hoặc** thuộc người khác, **hoặc** suất chưa mở bán (với người xem) | Quay lại danh sách |
| `409` | **Xung đột trạng thái**: ghế đã có người, suất đã bắt đầu / đã hủy, đơn không còn ở trạng thái cho phép, vé đã check-in, request cùng key đang xử lý | Đọc `code` + `action`; thường là làm mới dữ liệu rồi chọn lại. Với `IDEMPOTENCY_IN_PROGRESS`: chờ `Retry-After` rồi gửi lại **cùng** key |
| `422` | Request hợp lệ về cú pháp nhưng **vi phạm luật nghiệp vụ** của chính nó: quá 6 ghế, vượt 10 ghế/suất, ghế không thuộc suất, giá âm, dùng lại `Idempotency-Key` với nội dung khác, vé không hợp lệ | Sửa nội dung yêu cầu; với `IDEMPOTENCY_KEY_REUSED`: sinh key **mới** |
| `429` | Vượt rate limit (`login`, `refresh`, `hold`, `scan`) | Chờ `Retry-After` |
| `503` | Quá tải — admission control từ chối (bài toán #11) | Chờ `Retry-After` rồi gửi lại **cùng** `Idempotency-Key` |
| `500` | Lỗi không mong đợi (`INTERNAL_ERROR`, không lộ chi tiết) | Retry với cùng `Idempotency-Key` (an toàn) |

**Phân biệt `409` và `422`:** `409` = yêu cầu đúng, nhưng **thế giới** đã đổi (ghế vừa bị giữ). `422` =
bản thân **yêu cầu** sai luật (7 ghế). Client xử lý khác nhau: `409` thì làm mới rồi chọn lại, `422`
thì sửa yêu cầu.

**Không có `200` cho lỗi nghiệp vụ.** Ngoại lệ duy nhất có chủ ý: webhook Stripe trả `200` cho sự
kiện **trùng** — với Stripe, "đã nhận" là kết quả đúng, và trả lỗi làm Stripe gửi lại mãi.

---

## Idempotency contract

**Cơ chế:** [`concurrency.md`](concurrency.md) #5, ADR-0011. Bảng: `booking.idempotency_records`.

| Mục | Hợp đồng |
|---|---|
| Header | `Idempotency-Key` |
| Giá trị | Client tự sinh, **UUID** (v4 hoặc v7) khuyến nghị; chấp nhận 1–64 ký tự `[A-Za-z0-9_-]` |
| Sinh khi nào | **Một lần cho một ý định** — khi người xem bấm "giữ ghế". Retry (mạng lỗi, `5xx`, `503`, timeout) dùng **lại** key đó. Đổi ghế = ý định mới = key mới |
| Phạm vi | Theo **(người xem, key)**. Hai người dùng cùng key không ảnh hưởng nhau |
| Bắt buộc ở | `POST /api/showtimes/{showtimeId}/bookings` — thiếu ⇒ `400 IDEMPOTENCY_KEY_REQUIRED` |
| Request hash | SHA-256 trên `METHOD` + đường dẫn đã điền tham số + **body chuẩn hoá**: JSON key sắp theo thứ tự chữ cái, không khoảng trắng, UUID viết thường, và `seatIds` **sắp tăng dần** (cùng tập ghế khác thứ tự = cùng ý định) |
| Lần đầu | Xử lý; lưu status + body (kể cả **lỗi nghiệp vụ** `409`/`422`) |
| Lặp lại, hash giống, đã xong | Trả **nguyên văn** status + body đã lưu, thêm header `Idempotent-Replayed: true` |
| Lặp lại, hash khác | `422 IDEMPOTENCY_KEY_REUSED` |
| Lặp lại trong lúc lần đầu **đang chạy** | Request sau **chờ** (tối đa 3s) rồi trả kết quả của lần đầu; quá 3s ⇒ `409 IDEMPOTENCY_IN_PROGRESS` + `Retry-After: 1` |
| Lần đầu thất bại vì **hạ tầng** (`500`, tiến trình chết) | Không lưu gì (transaction rollback) ⇒ gửi lại cùng key được xử lý như mới |
| Hạn dùng | 24 giờ kể từ lần đầu. Sau đó cùng key được coi là mới |
| Dọn dẹp | Job mỗi giờ ở module `Booking`, xoá theo lô — không thể xoá nhầm bản ghi "đang chạy" vì bản ghi đó chưa commit |

**Endpoint không cần `Idempotency-Key` và vì sao:**

| Endpoint | Vì sao tự nhiên idempotent |
|---|---|
| `POST /api/bookings/{id}/payment` | `UNIQUE (booking_id)` — gọi lại trả intent cũ |
| `POST /api/bookings/{id}/cancel` | Điều kiện trạng thái; gọi lại ⇒ `409 BOOKING_ALREADY_CANCELLED` (không đổi gì) |
| `POST /api/manager/showtimes/{id}/open-sale`, `/cancel` | Điều kiện trạng thái |
| `POST /api/check-in/scan` | PK `ticket_id` — lần hai trả "đã check-in" kèm thời điểm gốc (**đúng ý**: lần hai **không** được trả cùng kết quả "mời vào") |
| Tạo phòng, phim, suất (quản lý) | Tải thấp, người thao tác nhìn thấy kết quả; trùng thì sửa tay. Thêm key là chi phí không đáng |

**Webhook có dùng lại cơ chế này không?** **Không.** Webhook chống lặp bằng `UNIQUE (stripe_event_id)`
**trong** transaction nghiệp vụ (#7): khoá tự nhiên có sẵn, và Stripe không cần response được phát
lại — chỉ cần `2xx`.

---

## Không có partial success

`POST /api/showtimes/{showtimeId}/bookings` là **all-or-nothing** (`FR-02`, `BR-02`): giữ được 3/4 ghế
**là lỗi** `409 SEATS_UNAVAILABLE` kèm `unavailableSeatIds` — không có ghế nào bị giữ. Client đánh dấu
các ghế đó trên sơ đồ và để người xem chọn lại. Không endpoint nào trong API trả kết quả một phần.

---

## Identity

### `POST /api/auth/register`

**Auth**: không. **Idempotency**: n/a (email unique). **Rate limit**: `login`.

| Trường | Kiểu | Ràng buộc |
|---|---|---|
| `email` | string | Email hợp lệ; lưu chữ thường |
| `password` | string | ≥ 10 ký tự |

`201` — `{ "id", "email", "role": "Customer" }`. Luôn tạo vai trò `Customer`.

| Status | `code` |
|---|---|
| `400` | `VALIDATION_FAILED` |
| `409` | `EMAIL_TAKEN` |

### `POST /api/auth/login`

**Auth**: không. **Rate limit**: `login` (10/phút/IP).
Body `{ email, password }`. `200` — `{ "accessToken", "accessTokenExpiresAt", "refreshToken" }`.

| Status | `code` |
|---|---|
| `401` | `INVALID_CREDENTIALS` (không nói email có tồn tại hay không) |
| `429` | `RATE_LIMITED` |

### `POST /api/auth/refresh`

**Auth**: không (refresh token trong body). **Rate limit**: `refresh`.
Body `{ refreshToken }`. `200` — cặp token mới (rotation, bài toán #6).

| Status | `code` |
|---|---|
| `401` | `REFRESH_TOKEN_INVALID` (không tồn tại / hết hạn) |
| `401` | `REFRESH_TOKEN_REUSED` — cả chuỗi đã bị revoke; đăng nhập lại |

### `POST /api/auth/logout`

**Auth**: Bearer. Body `{ refreshToken }`. `204` — revoke cả `family_id` của token.

### `GET /api/auth/me`

**Auth**: Bearer. `200` — `{ id, email, role }` (vai trò **hiện hành**, đọc từ DB/cache).

### `GET /api/admin/users` — `FR-29`

**Auth**: `Administrator`. Phân trang; lọc `?role=`. `200` — `{ items: [{ id, email, role, createdAt }], … }`.

### `PUT /api/admin/users/{userId}/role` — `FR-29`

**Auth**: `Administrator`. Body `{ role }` (`Customer` / `TheaterManager` / `GateStaff` /
`Administrator`). `200` — `{ id, role, roleVersion }`. Ghi `role_changes`, tăng `role_version`
(cache vai trò bị vô hiệu ngay).

| Status | `code` |
|---|---|
| `404` | `USER_NOT_FOUND` |
| `422` | `CANNOT_CHANGE_OWN_ROLE` — quản trị viên tự hạ quyền mình có thể khoá hệ thống |

---

## Scheduling

### `GET /api/movies` — `FR-11`

**Auth**: không. Phim có **ít nhất một suất `OnSale` chưa bắt đầu** (`BR-29`). Phân trang.
`200` — `{ items: [{ id, title, durationMinutes, nextShowtimeAt }], … }`. Cache Redis 30s.

### `GET /api/movies/{movieId}/showtimes` — `FR-12`, `FR-34`

**Auth**: không. Chỉ suất `OnSale` chưa bắt đầu, sắp theo giờ.
`200` — `{ movie: {…}, showtimes: [{ id, startsAt, endsAt, roomName, prices: [{ seatType, amount }] }] }`.

| Status | `code` |
|---|---|
| `404` | `MOVIE_NOT_FOUND` |

### `POST /api/admin/movies`, `PUT /api/admin/movies/{movieId}` — `FR-28`

**Auth**: `Administrator`. Body `{ title, description, durationMinutes }`. `201` / `200`.
`400 VALIDATION_FAILED` khi `durationMinutes <= 0` (sai hình dạng đầu vào, kiểm bằng `[Range]` — [`building-blocks.md`](building-blocks.md) B5).

### `POST /api/manager/rooms` — `FR-20`

**Auth**: `TheaterManager` (chủ = người gọi) hoặc `Administrator` (truyền `ownerId`).

| Trường | Kiểu | Ràng buộc |
|---|---|---|
| `name` | string | Duy nhất trong phòng của chủ |
| `seats` | array | `[{ rowLabel, seatNumber, seatType }]`, ≥ 1, vị trí không trùng |

`201` — `{ id, name, seatCount }`.

| Status | `code` |
|---|---|
| `409` | `ROOM_NAME_TAKEN` |
| `422` | `DUPLICATE_SEAT_POSITION`, `INVALID_SEAT_TYPE` |

### `GET /api/manager/rooms`

Phòng của người gọi (quản trị: mọi phòng). `200` — danh sách `{ id, name, seatCount }`.

### `PUT /api/manager/rooms/{roomId}/seats` — `FR-20`, `INV-13`

Thay toàn bộ sơ đồ. `200`. `409 ROOM_HAS_ACTIVE_SHOWTIMES` khi còn suất chưa kết thúc, chưa hủy.
`404` khi phòng không thuộc người gọi.

### `POST /api/manager/showtimes` — `FR-21`, `BR-20`, `BR-32`

**Auth**: `TheaterManager` / `Administrator`.

| Trường | Kiểu | Ràng buộc |
|---|---|---|
| `movieId` | uuid | Có trong danh mục |
| `roomId` | uuid | Phòng **của người gọi** |
| `startsAt` | datetime | Trong tương lai |
| `endsAt` | datetime | Tuỳ chọn; mặc định `startsAt + durationMinutes` |
| `prices` | array | `[{ seatType, amount }]` — đủ **mọi** loại ghế có trong phòng, `amount >= 0` |

`201` — `{ id, status: "NotOnSale", seatCount }`. Cùng transaction: sinh mọi `Showtime seat` (ranh giới
bị cắt #5).

| Status | `code` |
|---|---|
| `404` | `ROOM_NOT_FOUND` (kể cả phòng của người khác), `MOVIE_NOT_FOUND` |
| `409` | `SHOWTIME_OVERLAP` — vi phạm exclusion constraint (gồm đệm 15 phút) |
| `422` | `MISSING_PRICE`, `NEGATIVE_PRICE`, `STARTS_IN_PAST` |

### `PUT /api/manager/showtimes/{showtimeId}` · `DELETE /api/manager/showtimes/{showtimeId}`

Chỉ khi `NotOnSale` (`state-machines.md`). Sửa giờ / giá; xoá cả ghế đã sinh. `409 SHOWTIME_NOT_EDITABLE`
khi đã mở bán / đã hủy. `404` khi không phải chủ (`FR-32`).

### `POST /api/manager/showtimes/{showtimeId}/open-sale` — `FR-22`

`200` — `{ id, status: "OnSale" }`. `409 SHOWTIME_NOT_EDITABLE` (không còn `NotOnSale`),
`409 SHOWTIME_STARTED`.

### `POST /api/manager/showtimes/{showtimeId}/cancel` — `FR-24`, `FR-28`, `FR-32`

`200` — `{ id, status: "Cancelled", cancelledBookings, refundsRequested }`. Một transaction: huỷ đơn chờ,
yêu cầu hoàn đơn đã trả (bài toán #14, ADR-0006). Gọi lại khi đã hủy ⇒ `200` cùng thân (idempotent ở
tầng lệnh — `state-machines.md`). `409 SHOWTIME_NOT_ON_SALE` khi `NotOnSale` (dùng `DELETE`).

### `GET /api/manager/showtimes`

Suất của người gọi, lọc `?from=&to=&status=`. Phân trang.

---

## Inventory

### `GET /api/showtimes/{showtimeId}/seats` — `FR-13`, `BR-23`, `SC-11`

**Auth**: không. **Cache**: Redis (bài toán #9, #10). Chỉ suất `OnSale` chưa bắt đầu; khác ⇒ `404`.

`200`:

```json
{
  "showtimeId": "…", "startsAt": "…",
  "availableSeatCount": 87,
  "seats": [
    { "id": "…", "row": "D", "number": 7, "label": "D7", "seatType": "VIP", "price": 120000,
      "status": "Held" }
  ],
  "asOf": "2026-12-01T11:59:58Z"
}
```

- `status` ∈ `Available` / `Held` / `Sold`. Ghế `Blocked` **không** có trong danh sách (`BR-11`). Ghế
  `Held` đã quá hạn hiển thị `Available` (bài toán #4).
- **`Held` và `Sold` khác nhau có chủ ý:** `Held` có thể trống lại trong ≤ 15 phút — người xem có lý do
  quay lại; `Sold` thì không. Không trả `heldUntil` (thời điểm giữ của người khác không phải việc của
  người xem).
- `asOf`: đây là **ảnh chụp** (`BR-23`); lệnh giữ ghế luôn kiểm lại.

### `GET /api/manager/showtimes/{showtimeId}/availability` — `FR-23`

**Auth**: chủ suất / `Administrator`; khác ⇒ `404`.
`200` — `{ roomSeatCount, available, held, sold, blocked }` với `available + held + sold + blocked =
roomSeatCount` (ghế `Held` quá hạn tính vào `available`).

### `POST /api/manager/showtimes/{showtimeId}/seats/block` · `/seats/unblock` — `FR-20`, `BR-11`

**Auth**: chủ suất / `Administrator`. Body `{ seatIds: [...] }`. All-or-nothing như giữ ghế.
`200` — `{ blocked: [...] }`.

| Status | `code` |
|---|---|
| `409` | `SEATS_NOT_BLOCKABLE` + `seatIds` — ghế đang giữ / đã bán (`INV-05`) |
| `409` | `SHOWTIME_CANCELLED` |
| `422` | `INVALID_SEATS` |

---

## Booking

### `POST /api/showtimes/{showtimeId}/bookings` — giữ ghế

`FR-01`, `FR-02`, `FR-03`, `FR-09`, `FR-14`, `FR-15`, `FR-37`, `FR-41`.

**Auth**: `Customer`. **Idempotency**: **bắt buộc** — vì đây là lệnh duy nhất mà client không biết
được kết quả nếu mất response (mạng chập chờn đúng lúc mở bán), và gửi lại mà không có key thì giữ
hai bộ ghế (`FR-37`). **Rate limit**: `hold`. **Admission control**: có (#11).

| Trường | Kiểu | Ràng buộc |
|---|---|---|
| `seatIds` | uuid[] | 1–6 phần tử, không trùng; tổng theo quy tắc đếm ≤ 6 |

**Chỉ nhận id ghế.** Loại ghế và giá lấy từ server (`Booking` `INV-02`).

`201`:

```json
{
  "bookingId": "…", "status": "AwaitingPayment",
  "expiresAt": "2026-12-01T12:15:00Z",
  "lines": [{ "seatId": "…", "label": "D7", "seatType": "VIP", "price": 120000 }],
  "totalAmount": 240000
}
```

| Status | `code` | Thêm | `action` |
|---|---|---|---|
| `400` | `IDEMPOTENCY_KEY_REQUIRED` | | Gửi kèm header |
| `404` | `SHOWTIME_NOT_FOUND` | | Suất không tồn tại / chưa mở bán (`FR-34`) |
| `409` | `SEATS_UNAVAILABLE` | `unavailableSeatIds` | Chọn ghế khác |
| `409` | `SHOWTIME_STARTED` | | Chọn suất khác (`FR-41`) |
| `409` | `SHOWTIME_CANCELLED` | | Chọn suất khác |
| `409` | `IDEMPOTENCY_IN_PROGRESS` | `Retry-After` | Chờ rồi gửi lại cùng key |
| `422` | `BOOKING_TOO_LARGE` | `maxSeats: 6`, `requestedWeight` | Bớt ghế; ghế đôi tính 2 |
| `422` | `CUSTOMER_SEAT_LIMIT` | `limit: 10`, `currentWeight` | Bạn đã có `currentWeight` ghế cho suất này |
| `422` | `INVALID_SEATS` | `seatIds` | Ghế không thuộc suất / bị chặn — làm mới sơ đồ |
| `422` | `IDEMPOTENCY_KEY_REUSED` | | Sinh key mới |
| `429` | `RATE_LIMITED` | `Retry-After` | |
| `503` | `OVERLOADED` | `Retry-After` | Gửi lại cùng key |

### `GET /api/bookings` — `FR-10`

**Auth**: `Customer`. Đơn **của người gọi**, mới nhất trước. `200` — `{ items: [{ bookingId, showtime:
{ id, movieTitle, startsAt }, status, expiresAt, totalAmount, seatCount }], … }`.

### `GET /api/bookings/{bookingId}` — `FR-10`, `BR-30`

**Auth**: chủ đơn; khác ⇒ `404 BOOKING_NOT_FOUND`.
`200` — đơn + `lines` + `payment: { status, refundStatus }` (đọc qua `Payment.Contracts`) — đây là
nơi người xem thấy "đang hoàn / đã hoàn tiền" khi suất bị hủy.

### `POST /api/bookings/{bookingId}/cancel` — `FR-18`, `BR-21`

**Auth**: chủ đơn. `200` — `{ bookingId, status: "Cancelled" }`; ghế trả về ngay.

| Status | `code` |
|---|---|
| `404` | `BOOKING_NOT_FOUND` |
| `409` | `BOOKING_ALREADY_CANCELLED` |
| `409` | `BOOKING_NOT_CANCELLABLE` — `Paid` (chỉ hủy suất mới ảnh hưởng đơn đã trả) hoặc `Expired` |

### `POST /api/bookings/{bookingId}/payment` — `FR-16`, J-02.4

**Auth**: chủ đơn. Tạo hoặc trả lại `Payment intent` (`UNIQUE booking_id`).
`200` — `{ intentId, clientSecret, amount, currency: "VND", status: "Unpaid" }`. Client dùng
`clientSecret` với Stripe.js để xác nhận thanh toán; kết quả đến qua webhook. Client **poll**
`GET /api/bookings/{id}` (mỗi 2s, tối đa tới `expiresAt`) để thấy `Paid`.

| Status | `code` |
|---|---|
| `404` | `BOOKING_NOT_FOUND` |
| `409` | `BOOKING_EXPIRED` (`expiresAt` đã qua — dù chưa sang `Expired`) / `BOOKING_ALREADY_PAID` / `BOOKING_NOT_PAYABLE` (`Cancelled`) |
| `502` | `PAYMENT_PROVIDER_UNAVAILABLE` — Stripe lỗi khi tạo intent; gọi lại an toàn |

### `GET /api/admin/bookings/{bookingId}/history` — `FR-31`, J-07.5

**Auth**: `Administrator`. `200` — dòng thời gian **ghép** lịch sử đơn (`booking_history`) và thông báo
thanh toán (`payment_events`, qua `Payment.Contracts`) theo thời gian:
`{ booking: {…}, timeline: [{ at, source: "Booking"|"Payment", event, detail }] }`. Thông báo trùng hiện
**một** lần (đã chặn ở `UNIQUE`); lần gửi lặp xem được qua log có `stripe_event_id`.

---

## Payment

### `POST /api/webhooks/stripe` — `FR-38`, `FR-39`

**Auth**: header `Stripe-Signature` (HMAC của Stripe, xác minh bằng webhook secret; dung sai 5 phút).
**Idempotency**: `UNIQUE (stripe_event_id)` (#7).
Sự kiện xử lý: `payment_intent.succeeded`, `payment_intent.payment_failed`, `charge.refunded`. Loại
khác ⇒ `200` bỏ qua.

| Status | Khi nào |
|---|---|
| `200` | Đã xử lý, **hoặc** trùng (đã xử lý trước đó), hoặc loại sự kiện không quan tâm |
| `400` | Chữ ký sai / payload không đọc được — Stripe sẽ không thể thành công khi gửi lại, nên báo rõ |
| `500` | Lỗi không mong đợi — transaction rollback, Stripe gửi lại; ghi `webhook_failures` |

---

## Ticketing

### Định dạng mã QR

```
v1.<kid>.<payload>.<signature>
payload   = base64url("<ticketId>|<showtimeId>|<seatLabel>|<issuedAtUnix>")
signature = base64url(HMAC-SHA256(key[kid], "v1." + kid + "." + payload))
```

- `kid`: id khoá ký, để **xoay khoá** mà vé cũ vẫn quét được (giữ khoá cũ ở chế độ chỉ-xác-minh). Khoá
  trong cấu hình bí mật (user-secrets / biến môi trường), **không** trong DB.
- So sánh chữ ký **constant-time** (`CryptographicOperations.FixedTimeEquals`).
- Không có hạn dùng riêng trong mã: vé dùng được cho tới khi check-in, trừ khi suất bị hủy.
- Không lưu mã — tính lại từ `ticketing.tickets` (`data-model.md`).

### `GET /api/tickets` — `FR-17`, `BR-30`

**Auth**: `Customer`. Vé **của người gọi** (`tickets.customer_id`). Phân trang.
`200` — `{ items: [{ ticketId, bookingId, showtime: { id, movieTitle, startsAt, roomName, status },
seatLabel, qr, checkedInAt }] }`. Suất `Cancelled` ⇒ `showtime.status = "Cancelled"` và client hiển thị
"suất đã hủy — tiền được hoàn tự động" (chi tiết hoàn tiền: `GET /api/bookings/{id}`).

### `GET /api/bookings/{bookingId}/tickets` — `FR-17`

**Auth**: chủ đơn (kiểm qua `tickets.customer_id`); không có vé / không phải chủ ⇒ `404`.
`200` — vé của đơn, mỗi vé một `qr`.

### `POST /api/check-in/scan` — `FR-07`, `FR-08`, `FR-25`, `FR-33`, `FR-35`

**Auth**: `GateStaff` (có `Gate assignment` cho suất) hoặc `Administrator`. **Rate limit**: `scan`.
**Idempotency**: n/a — lần quét thứ hai **phải** cho kết quả khác.

| Trường | Kiểu | Ràng buộc |
|---|---|---|
| `showtimeId` | uuid | Suất nhân viên đang trực |
| `qr` | string | Nội dung mã |
| `gate` | string | Nhãn nơi quét, 1–32 ký tự |

Thứ tự kiểm: quyền (vai trò + phân công) → chữ ký → vé tồn tại → đúng suất → suất chưa hủy → ghi.

| Kết quả (`FR-25`) | Status | `code` | Thêm |
|---|---|---|---|
| Hợp lệ | `201` | — | `{ ticketId, seatLabel, checkedInAt }` |
| Đã dùng | `409` | `TICKET_ALREADY_CHECKED_IN` | `firstCheckedInAt`, `firstGate` |
| Không hợp lệ (sai chữ ký, không có vé — gồm mã cho ghế chưa thanh toán) | `422` | `TICKET_INVALID` | |
| Suất khác | `422` | `TICKET_WRONG_SHOWTIME` | `ticketShowtimeStartsAt` |
| Suất đã hủy | `409` | `SHOWTIME_CANCELLED` | |
| *(quyền)* không được phân công / sai vai trò | `403` | `NOT_ASSIGNED` / `FORBIDDEN` | |

### `GET /api/manager/showtimes/{showtimeId}/check-ins/count` — `FR-27`

**Auth**: chủ suất / `Administrator`. `200` — `{ showtimeId, checkedIn, ticketsIssued }`
(`checkedIn <= ticketsIssued` — `Ticket` `INV-04`).

### `POST /api/manager/showtimes/{showtimeId}/gate-assignments` · `DELETE …/gate-assignments/{staffId}` — `FR-40`

**Auth**: chủ suất / `Administrator`. Body `{ staffId }`. `201` / `204`.

| Status | `code` |
|---|---|
| `404` | `SHOWTIME_NOT_FOUND` (kể cả không phải chủ) |
| `409` | `ALREADY_ASSIGNED` |
| `422` | `NOT_GATE_STAFF` — người được phân công không có vai trò `GateStaff` |

---

## Vận hành

| Endpoint | Auth | Dùng cho |
|---|---|---|
| `GET /health/live` | không | Tiến trình còn sống |
| `GET /health/ready` | không | Postgres + Redis kết nối được |
| `GET /metrics` | mạng nội bộ (không public qua reverse proxy) | Prometheus scrape; Grafana dashboard là nơi quản trị viên xem chỉ số (`FR-30`) |

---

## Truy vết user journey → endpoint

| Journey | Endpoint |
|---|---|
| J-01 | `GET /api/movies` → `GET /api/movies/{id}/showtimes` → `GET /api/showtimes/{id}/seats` |
| J-02 | `POST /api/showtimes/{id}/bookings` → `POST /api/bookings/{id}/payment` → (Stripe) → webhook → `GET /api/bookings/{id}` (poll) → `GET /api/bookings/{id}/tickets` |
| J-03 | như J-02 dưới tải; `503`/`429` khi quá tải |
| J-04 | các mã lỗi của `POST …/bookings`, `…/payment`, `…/cancel` |
| J-05 | `POST /api/check-in/scan`, `GET …/check-ins/count` |
| J-06 | `/api/manager/rooms` → `/api/manager/showtimes` → `…/seats/block` → `…/open-sale` → `…/gate-assignments` → `…/availability` → `…/cancel` |
| J-07 | `/metrics` (Grafana), `GET /api/admin/bookings/{id}/history`, `/api/admin/movies`, `/api/admin/users` |

## Endpoint có cache

| Endpoint | Cache | Độ mới |
|---|---|---|
| `GET /api/showtimes/{id}/seats` | Redis hash theo suất (#9, #10) | Cập nhật theo ghế qua outbox, ≈ < 1s |
| `GET /api/movies`, `GET /api/movies/{id}/showtimes` | Redis, TTL 30s | Mở bán / hủy suất thấy sau ≤ 30s — chấp nhận được; giữ ghế vẫn kiểm trạng thái suất |
| Còn lại | Không | |
