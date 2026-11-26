# Concurrency design

Lớp 3 — Design. Bảng và cột: [`data-model.md`](data-model.md).
Module và transaction: [`architecture.md`](architecture.md), ADR-0008, ADR-0009.

Đây là trái tim của dự án. Mỗi bài toán có: kịch bản, hậu quả nếu sai, **cơ chế tới mức viết được
SQL**, phương án bị loại, và **test chứng minh** — kèm dòng "test này đỏ nếu…" (`SC-12`).

---

## Phần 1 — Giảng: bộ công cụ

Đọc phần này trước; các bài toán bên dưới chỉ việc chọn công cụ.

| Công cụ | Nó bảo đảm gì | Cái giá | Dùng ở |
|---|---|---|---|
| **Điều kiện trong lệnh** `UPDATE … WHERE status = 'X'` | Postgres kiểm `WHERE` trên **phiên bản mới nhất** của dòng sau khi chờ khoá ⇒ không có cửa sổ đọc-rồi-ghi (TOCTOU). Số dòng ảnh hưởng 0/1 = thua/thắng | Không cho biết **vì sao** thua; vẫn lấy row lock ⇒ **không** chống deadlock | #1, #4, #6, #7 |
| **`SELECT … FOR UPDATE`** | Khoá dòng tới hết transaction; đọc được trạng thái để báo lý do | Giữ khoá lâu hơn; nhiều dòng thì phải tự lo **thứ tự** | #2, #3 |
| **Thứ tự khoá cố định** | Hai transaction khoá cùng tập tài nguyên theo cùng thứ tự ⇒ không có vòng chờ ⇒ không deadlock | Phải ép ở code; Postgres **không** bảo đảm thứ tự khoá của `UPDATE … WHERE id = ANY(...)` | #3, mọi luồng |
| **Ràng buộc `UNIQUE` + `ON CONFLICT`** | Hai lệnh chèn đồng thời cùng khoá: lệnh sau **chờ** lệnh trước commit/rollback rồi mới quyết | Nhánh conflict phải xử lý riêng | #5, #7, #8 |
| **Advisory lock** `pg_advisory_xact_lock(key)` | Khoá theo một số 64-bit bất kỳ, tự nhả khi transaction kết thúc | Không thấy trong schema; trùng băm ⇒ hai khoá thành một (chỉ chậm hơn, không sai) | #13 |
| **Isolation `SERIALIZABLE`** | Mọi anomaly bị phát hiện | Lỗi `40001` phải retry — trên đường nóng nhất khi mở bán, retry = tải nhân lên | **Không dùng** |
| **Outbox** | Việc phụ xảy ra **khi và chỉ khi** commit; at-least-once | Trễ; handler phải idempotent | #7, #10, hoàn tiền |
| **`FOR UPDATE SKIP LOCKED`** | Nhiều worker chia việc không đè nhau, không chờ nhau | Việc bị khoá thì bị bỏ qua tạm | worker hết hạn, outbox |
| **Single-flight** | N request cùng miss cache ⇒ 1 lần đọc DB | Request chờ dùng chung kết quả; lỗi lan cho cả nhóm | #9 |
| **Cache theo phiên bản** | Ghi cache chỉ khi phiên bản mới hơn ⇒ bản cũ không đè bản mới | Mỗi dòng cần cột `version` | #10 |
| **Redis token bucket (Lua)** | Một hạn mức dùng chung cho nhiều instance, nguyên tử | Thêm một hop mạng | #12 |
| **Admission control** | Từ chối sớm khi quá tải thay vì xếp hàng chờ connection | Một phần request bị `503` | #11 |

**Vì sao không Redlock (khoá phân tán trên nhiều Redis):** (1) mọi bất biến của dự án đã được DB
bảo vệ — thêm một khoá ngoài không cứu được logic DB sai, chỉ thêm một hop mạng; (2) khoá có thời hạn
không an toàn khi tiến trình bị dừng (GC pause) quá thời hạn mà không có fencing token — phân tích
nổi tiếng của Martin Kleppmann. Redis ở đây chỉ dùng cho **tối ưu** (single-flight, rate limit), không
bao giờ cho **tính đúng đắn**. **Không cần distributed lock**.

**Isolation:** toàn hệ thống `READ COMMITTED`. **Không** dùng `SERIALIZABLE` ở đâu.

### Thứ tự khoá toàn cục

Mọi transaction lấy khoá theo **cùng một** thứ tự (bỏ qua bước không cần):

```
1. scheduling.showtimes   (một dòng)
2. khoá quota (advisory, người xem × suất)
3. booking.bookings       (tăng dần theo id nếu nhiều)
4. inventory.showtime_seats (tăng dần theo id)
5. payment.payment_intents
6. ticketing.tickets / check_ins (chèn)
```

| Luồng | Thứ tự thực tế |
|---|---|
| Giữ ghế | 1 (`FOR KEY SHARE`) → 2 → 4 → chèn đơn mới |
| Thanh toán thành công | chèn sự kiện → 3 → 4 → 5 → 6 |
| Hết hạn / huỷ đơn | 3 → 4 → 5 |
| Hủy suất | 1 (`FOR UPDATE`) → 3 → 4 → 5 |

Chèn một dòng **mới** (đơn mới, vé mới) không tranh khoá với ai nên không ảnh hưởng thứ tự. Chính
bảng này là lý do mỗi bài toán bên dưới không tự sinh deadlock với bài toán khác.

### Hạ tầng test concurrency (dùng chung)

- **Postgres + Redis thật** qua Testcontainers — không InMemory (InMemory không có khoá, không có
  unique ⇒ test xanh khi logic sai).
- **Chồng lấn thật:** N task cùng chờ một `Barrier(N)` rồi mới gửi request; với test DB cấp thấp,
  thêm `pg_sleep` trong transaction thứ nhất (qua hook test) để **ép** transaction thứ hai đến trong
  lúc thứ nhất còn giữ khoá. Test không được dựa vào may mắn của lịch chạy.
- **Lặp lại:** test race chạy ≥ 50 vòng trong một lần chạy; một vòng sai là đỏ.
- **Đối chứng âm (negative control):** mỗi test có một biến thể bật cờ cấu hình
  `Concurrency:UnsafeVariant=<tên>` (chỉ có trong build test) thay cơ chế đúng bằng cơ chế sai, và
  khẳng định test **đỏ**. Đây là cách biến dòng "test này đỏ nếu…" thành thứ chạy được, không phải
  lời hứa.
- **Đếm số lần đọc DB:** `DiagnosticListener` của Npgsql (hoặc wrapper đếm lệnh theo tag) — cho #9.

---

## Bài toán #1 — Gán trùng ghế

**Kịch bản**: 100 người xem cùng giữ ghế D7 của suất 19h.
**Hậu quả nếu sai**: hai người cùng có ghế D7 — bán trùng, khách đến rạp cãi nhau (`FR-01`, `SC-01`).

### Cơ chế

Ghế được khoá rồi đổi trạng thái **trong cùng transaction**, và lệnh đổi trạng thái mang điều kiện
nguồn. Xem SQL đầy đủ ở #2. Với một ghế, cốt lõi là:

```sql
UPDATE inventory.showtime_seats
SET status = 'Held', hold_id = @hold_id, held_until = @expires_at, version = version + 1
WHERE id = @seat_id
  AND (status = 'Available' OR (status = 'Held' AND held_until <= now()))
RETURNING id;
-- 1 dòng ⇒ thắng; 0 dòng ⇒ 409 SEATS_UNAVAILABLE
```

`(status = 'Held' AND held_until <= now())`: ghế đã quá hạn giữ được lấy luôn, không chờ worker —
xem #4 và ADR-0015.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| `SELECT status` rồi `UPDATE` không điều kiện | Cửa sổ TOCTOU: cả 100 đọc `Available`, cả 100 ghi ⇒ bán trùng |
| `SERIALIZABLE` | Đúng, nhưng 99 request nhận `40001` và phải retry — tải nhân lên đúng lúc mở bán |
| Redis `SETNX seat:{id}` làm nguồn sự thật | Redis và DB lệch nhau là mất ghế/bán trùng; không có transaction chung với việc tạo đơn |

### Test chứng minh

- `HoldSeat_100ConcurrentRequestsSameSeat_ExactlyOneWins` (integration).
- Khẳng định: đúng 1 response `201`, 99 response `409` với `code = SEATS_UNAVAILABLE`; trong DB ghế
  có đúng một `hold_id`; đúng **1** đơn `AwaitingPayment` chứa ghế đó.
- **Đỏ nếu** bỏ điều kiện `status` trong `UPDATE` (biến thể `UnsafeVariant=ReadThenWrite`: đọc rồi
  ghi không điều kiện) ⇒ > 1 response `201`.

---

## Bài toán #2 — Giữ n ghế phải all-or-nothing

**Kịch bản**: người xem giữ A, B, C, D; người khác vừa giữ C.
**Hậu quả nếu sai**: giữ được A, B, D — người xem trả tiền 4 ghế, nhận 3, hoặc ba ghế bị giữ "mồ
côi" (`FR-02`, `SC-02`, `BR-02`).

### Cơ chế — luồng giữ ghế đầy đủ (một transaction)

Đây là **thứ tự lệnh trong một transaction**, không phải một class. Mỗi module chạy phần SQL trên bảng của
chính nó: lệnh trên `inventory.*` nằm trong `Inventory`, `Booking` gọi qua interface ở `Inventory.Contracts`
(architecture D4, D5); tất cả dùng chung connection + transaction của Unit of Work (ADR-0008).

```sql
BEGIN;  -- READ COMMITTED; UoW của request (ADR-0008)

-- (0) Idempotency: chiếm key — xem #5
INSERT INTO booking.idempotency_records (...) VALUES (...) ON CONFLICT DO NOTHING RETURNING 1;
SAVEPOINT business;

-- (1) Suất: đang bán, chưa bắt đầu; khoá KEY SHARE để hủy suất phải chờ (#14)   [Scheduling]
SELECT status, starts_at FROM scheduling.showtimes WHERE id = @showtime_id FOR KEY SHARE;

-- (2) Loại ghế của các ghế được yêu cầu — KHÔNG khoá (seat_type bất biến)       [Inventory]
SELECT id, seat_type, row_label || seat_number AS seat_label
FROM inventory.showtime_seats WHERE showtime_id = @showtime_id AND id = ANY(@seat_ids);
-- thiếu id ⇒ 422 INVALID_SEATS; SUM(weight) > 6 ⇒ 422 BOOKING_TOO_LARGE              [Booking]

-- (3) Khoá quota rồi mới đếm — #13                                                [Booking]
SELECT pg_advisory_xact_lock(hashtextextended(@customer_id::text || '/' || @showtime_id::text, 0));
SELECT COALESCE(SUM(l.seat_weight), 0) FROM booking.bookings b
JOIN booking.booking_lines l ON l.booking_id = b.id
WHERE b.customer_id = @customer_id AND b.showtime_id = @showtime_id
  AND (b.status = 'Paid' OR (b.status = 'AwaitingPayment' AND b.expires_at > now()));
-- hiện có + yêu cầu > 10 ⇒ 422 CUSTOMER_SEAT_LIMIT

-- (4) Khoá các ghế THEO THỨ TỰ id, đọc trạng thái — #3                            [Inventory]
SELECT id, status, held_until FROM inventory.showtime_seats
WHERE id = ANY(@seat_ids) ORDER BY id FOR UPDATE;
-- ghế nào không (Available hoặc Held quá hạn) ⇒ danh sách ghế đã có người

-- (5a) Có ghế không giữ được:
ROLLBACK TO SAVEPOINT business;   -- nhả mọi thay đổi nghiệp vụ, giữ dòng idempotency
UPDATE booking.idempotency_records SET response_status = 409, response_body = @body WHERE ...;
COMMIT;  -- 409 SEATS_UNAVAILABLE + unavailableSeatIds

-- (5b) Tất cả giữ được:
UPDATE inventory.showtime_seats
SET status = 'Held', hold_id = @booking_id, held_until = @expires_at, version = version + 1
WHERE id = ANY(@seat_ids) AND (status = 'Available' OR (status = 'Held' AND held_until <= now()))
RETURNING id, status, held_until, version;     -- phải đúng n dòng (đã khoá ở (4) nên chắc chắn)
INSERT INTO inventory.outbox_messages (type, payload, ...) VALUES ('SeatStatesChanged', ...);  -- #10
INSERT INTO booking.bookings (...) VALUES (@booking_id, ..., 'AwaitingPayment', @expires_at, ...);
INSERT INTO booking.booking_lines (...) VALUES ...;          -- giá đọc ở (1) qua R9
INSERT INTO booking.booking_history (...) VALUES (NULL → 'AwaitingPayment');
INSERT INTO booking.outbox_messages (type, available_at, ...)
  VALUES ('ExpiryReminderDue', @expires_at - interval '5 minutes', ...);       -- FR-19
UPDATE booking.idempotency_records SET response_status = 201, response_body = @body WHERE ...;
COMMIT;
```

`@expires_at` = `now() + 15 phút` tính **một lần** ở ứng dụng từ đồng hồ DB (`SELECT now()` đầu
transaction), dùng cho cả `held_until` và `bookings.expires_at` ⇒ hai bản luôn bằng nhau.

**All-or-nothing đến từ đâu:** không từ aggregate, mà từ **một DB transaction** (ADR-0005): hoặc mọi
lệnh ở (5b) commit, hoặc không lệnh nào. Bước (4) quyết định **trước** khi ghi, nên nhánh thất bại
không cần "trả lại" ghế nào — chưa ghế nào bị đổi.

**Không có partial success:** giữ được 3/4 là **lỗi** `409`, không phải kết quả một phần
(`api-contract.md`).

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Một câu `UPDATE … WHERE id = ANY(...)` rồi so số dòng với n (biến thể A của ADR-0002) | Đúng về all-or-nothing, nhưng (a) không cho biết **ghế nào** thất bại nếu không đọc thêm, (b) thứ tự khoá không được bảo đảm — xem #3 |
| Giữ từng ghế ở transaction riêng, lỗi thì "trả lại" | Có lúc ghế đã giữ mà đơn chưa có; tiến trình chết giữa chừng ⇒ ghế mồ côi |
| Khoá cả suất (`FOR UPDATE` dòng `showtimes`) | Mọi người mua của suất xếp một hàng — mất ý nghĩa seat-based (ADR-0003) |

### Test chứng minh

- `HoldSeats_OneSeatAlreadyTaken_NoSeatIsHeld` (integration).
- Chuẩn bị: người X giữ C. Người Y giữ [A, B, C, D].
- Khẳng định: Y nhận `409`, `unavailableSeatIds = [C]`; **trong DB** A, B, D vẫn `Available` với
  `hold_id IS NULL` (đọc DB trực tiếp, không tin response); C vẫn của X; không có đơn nào của Y.
- Test song song: 20 người, mỗi người 4 ghế, các tập chồng nhau, chạy 50 vòng ⇒ mỗi ghế thuộc ≤ 1
  đơn, và mỗi đơn có **đúng** số ghế đã yêu cầu (không đơn nào 3/4).
- **Đỏ nếu** giữ từng ghế ở transaction riêng (`UnsafeVariant=PerSeatTransaction`) ⇒ xuất hiện ghế
  `Held` với `hold_id` không ứng với đơn nào, hoặc đơn thiếu ghế.

---

## Bài toán #3 — Deadlock khi tranh ghế chồng nhau

**Kịch bản**: X giữ [3, 7], Y giữ [7, 3] cùng lúc.
**Hậu quả nếu sai**: X khoá 3 chờ 7, Y khoá 7 chờ 3 ⇒ vòng chờ. Postgres phát hiện sau
`deadlock_timeout` (1s) và **huỷ một bên** với `40P01` ⇒ một người nhận lỗi 500 vô lý, và trong lúc
chờ, mọi request khác của hai ghế đó treo theo (`FR-03`, `SC-03`, `BR-22`).

### Cơ chế

Bước (4) của #2: `SELECT … WHERE id = ANY(@seat_ids) ORDER BY id FOR UPDATE`. Postgres khoá dòng
**theo thứ tự dòng đi ra khỏi node sắp xếp** ⇒ mọi transaction khoá ghế theo id tăng dần ⇒ không có
vòng chờ. Khoá quota (bước 3) **luôn trước** khoá ghế, và mọi luồng khác tuân thủ thứ tự khoá toàn
cục (Phần 1).

**Chi tiết phải biết:** `UPDATE … WHERE id = ANY(...)` **không có** `ORDER BY`; thứ tự khoá của nó là
thứ tự quét — có thể **tình cờ** tăng dần (quét index PK) và tình cờ đúng trong test. Không dựa vào
tình cờ: bước (4) khoá tường minh, bước (5b) cập nhật các dòng **đã** khoá.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Chỉ một câu `UPDATE … = ANY(...)` | Thứ tự khoá phụ thuộc kế hoạch thực thi — đúng hôm nay, sai khi planner đổi (ví dụ chuyển sang seq scan khi bảng nhỏ) |
| Bắt `40P01` rồi retry | Chữa triệu chứng: vẫn mất `deadlock_timeout` mỗi lần, và `BR-22` cấm từ chối vì "kẹt" |
| `NOWAIT` / `SKIP LOCKED` khi khoá ghế | Ghế đang bị transaction khác khoá (có thể sắp rollback) bị báo "đã có người" oan |
| Advisory lock cho cả suất | Tuần tự hoá toàn bộ suất |

### Test chứng minh

- `HoldSeats_ReverseOrderContention_NoDeadlock` (integration).
- Mỗi vòng: hai khách, hai ghế mới (S1 < S2). X gửi `[S1, S2]`, Y gửi `[S2, S1]`, qua `Barrier(2)`;
  hook test chèn `pg_sleep(0.05)` **giữa** việc khoá ghế đầu và ghế sau để ép vòng chờ nếu thứ tự sai.
  Lặp 200 vòng.
- Khẳng định: **0** exception có `SqlState = 40P01`; mỗi response là `201` hoặc `409
  SEATS_UNAVAILABLE`; mỗi vòng đúng 1 người thắng; mỗi response < 2 giây (dưới `deadlock_timeout` +
  biên — nếu có vòng chờ, request phải đợi ít nhất 1s).
- **Đỏ nếu** khoá theo thứ tự client gửi (`UnsafeVariant=ClientOrderLocking`: từng `UPDATE` một ghế,
  theo thứ tự trong request) ⇒ xuất hiện `40P01`.
- Vì sao không chỉ kiểm "cả hai có phản hồi": Postgres tự gỡ deadlock nên câu đó **vẫn đúng** khi
  không có cơ chế (ADR-0002) — test vô giá trị.

---

## Bài toán #4 — Hết hạn đua với webhook và với người mua mới

**Kịch bản**: đơn của X hết hạn 19:15:00. Ba thứ đến cùng lúc: worker hết hạn, webhook "thanh toán
thành công" của X lúc 19:15:01, và Y giữ đúng những ghế đó.
**Hậu quả nếu sai**: cấp vé cho đơn đã hết hạn **và** cho Y cùng ghế (bán trùng); hoặc mất ghế; hoặc
X mất tiền không có vé (`FR-04`, `FR-06`, `FR-39`, `SC-07`, `BR-04`, `BR-05`).

### Cơ chế

**Hết hạn theo thời gian** (ADR-0006) — ba lệnh, điều kiện loại trừ nhau:

```sql
-- Webhook thành công (#7) — chỉ thắng khi CÒN hạn:
UPDATE booking.bookings SET status = 'Paid', closed_at = now()
WHERE id = @booking_id AND status = 'AwaitingPayment' AND expires_at > now()
RETURNING id;            -- 0 dòng ⇒ nhánh hoàn tiền

-- Worker (lô, SKIP LOCKED để nhiều instance không giẫm nhau):
SELECT id FROM booking.bookings
WHERE status = 'AwaitingPayment' AND expires_at <= now()
ORDER BY id LIMIT 100 FOR UPDATE SKIP LOCKED;
-- với mỗi id, cùng transaction:
UPDATE booking.bookings SET status = 'Expired', closed_at = now()
WHERE id = @id AND status = 'AwaitingPayment' AND expires_at <= now();
UPDATE inventory.showtime_seats
SET status = 'Available', hold_id = NULL, held_until = NULL, version = version + 1
WHERE hold_id = @id AND status = 'Held'           -- chỉ ghế còn thuộc lần giữ này (INV-03)
RETURNING id, version;                             -- + outbox SeatStatesChanged (#10)
-- Payment: intent Unpaid → Closed + outbox huỷ phía Stripe (nếu có intent)
-- history: AwaitingPayment → Expired

-- Người mua mới (#1/#2): lấy được ghế Held đã quá held_until mà không chờ worker.
```

Hệ quả, theo từng thứ tự đến:

| Diễn biến | Kết quả |
|---|---|
| Webhook đến trước `expires_at` | `Paid`, ghế `Sold`, vé. Worker sau đó không thấy đơn (không còn `AwaitingPayment`) |
| Webhook đến sau `expires_at`, worker chưa chạy | `UPDATE … expires_at > now()` 0 dòng ⇒ intent `RefundPending`, không vé. Worker chạy sau ⇒ `Expired`, trả ghế |
| Y giữ ghế trước khi worker chạy | Y lấy ghế (điều kiện `held_until <= now()`), `hold_id` = đơn Y. Worker chạy sau: trả ghế `WHERE hold_id = X` ⇒ 0 dòng — không đụng ghế của Y |
| Webhook của X đến sau khi Y đã giữ | X quá hạn ⇒ hoàn tiền. Lệnh `Held → Sold` của X (nếu có) lọc `hold_id = X` ⇒ không thể lấy ghế của Y |

**Tiến trình chết giữa chừng:** đổi trạng thái đơn và trả ghế **cùng một transaction** ⇒ chết là
rollback cả hai, lượt sau làm lại. Không có cửa sổ "đơn `Expired` mà ghế còn `Held`". Lệnh
giữ **được** lấy ghế `Held` quá hạn (ADR-0015).

Thứ tự khoá: worker khoá đơn (3) rồi ghế (4) — khớp thứ tự toàn cục. Người mua mới khoá ghế (4) và
**không** khoá đơn cũ ⇒ không vòng chờ với worker.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Hết hạn theo worker ("ai chuyển trước thắng") | Kết quả nghiệp vụ phụ thuộc lịch chạy worker; trái `BR-04` (đã bỏ — ADR-0006) |
| Trả ghế bằng outbox sau khi đổi đơn | Có cửa sổ ghế mồ côi; thêm một bước có thể thất bại |
| Trả ghế theo `showtime_id` + danh sách ghế | Trả nhầm ghế người khác vừa giữ |
| Không cho lấy ghế `Held` quá hạn (chờ worker) | Ghế "đã hết hạn" vẫn không đặt được tới lượt quét sau — trái `SC-07` ("lập tức người khác đặt được") |

### Test chứng minh

- `PaymentSucceeded_JustAfterExpiry_NoTicketsAndRefund` — đơn có `expires_at = now() - 1ms`, worker
  **chưa** chạy; gửi webhook thành công ⇒ đơn **không** `Paid`, 0 vé, intent `RefundPending`, có 1
  dòng outbox `RefundRequested`. **Đỏ nếu** bỏ `expires_at > now()` khỏi lệnh `→ Paid`.
- `ExpiredHold_NewBuyerTakesSeat_WorkerDoesNotReleaseIt` — đơn X quá hạn; Y giữ cùng ghế (thành
  công); rồi worker chạy ⇒ ghế vẫn `Held` với `hold_id = Y`, đơn X `Expired`. **Đỏ nếu** lệnh trả ghế
  không lọc `hold_id`.
- `ExpiryWorker_TwoInstances_ReleaseOnce` — 2 worker song song trên 200 đơn quá hạn ⇒ mỗi đơn đúng 1
  dòng lịch sử `→ Expired`, mỗi ghế `Available`, tổng `version` tăng đúng 1 mỗi ghế. **Đỏ nếu** bỏ
  `SKIP LOCKED` và điều kiện `status = 'AwaitingPayment'`.
- `ExpiryWorker_CrashBetweenStatements_Recovers` — hook ném exception sau khi đổi trạng thái đơn,
  trước khi trả ghế ⇒ rollback; lượt sau trả đủ ghế.

---

## Bài toán #5 — Idempotency key race

**Kịch bản**: client gửi "giữ ghế" với cùng `Idempotency-Key` 50 lần đồng thời (bấm đúp, mạng chập
chờn, retry tự động).
**Hậu quả nếu sai**: 2 đơn giữ 2 bộ ghế cho một ý định mua; hoặc lần đầu `409`, lần sau `201`
(`FR-37`, `SC-06`, `BR-18`).

### Cơ chế — chiếm key trong chính transaction nghiệp vụ, kết quả lỗi giữ bằng savepoint (ADR-0011)

1. Lệnh đầu tiên của transaction:
   `INSERT INTO booking.idempotency_records (customer_id, idempotency_key, request_hash, …) VALUES (…)
   ON CONFLICT DO NOTHING RETURNING 1`.
   - Request thứ hai cùng key: lệnh `INSERT` của nó **chờ** trên unique index cho tới khi request
     đầu commit hoặc rollback — đây là "đang xử lý", không cần cột trạng thái.
   - Request đầu commit ⇒ request sau thấy conflict ⇒ đọc bản ghi: `request_hash` khác ⇒ `422
     IDEMPOTENCY_KEY_REUSED`; giống ⇒ trả **nguyên văn** status + body đã lưu.
   - Request đầu rollback (chết, lỗi hạ tầng) ⇒ request sau chèn được ⇒ tự xử lý — đúng, vì lần đầu
     không để lại gì.
2. `SAVEPOINT business` ngay sau đó. Thất bại **nghiệp vụ** (`409`, `422`) ⇒ `ROLLBACK TO SAVEPOINT`
   (nhả ghế, đơn), ghi response lỗi vào bản ghi, **commit** ⇒ lời từ chối được lưu, gửi lại vẫn nhận
   đúng lời từ chối đó dù ghế đã trống trở lại.
3. Thành công ⇒ ghi response `201` vào bản ghi trong cùng transaction với đơn.
4. Chờ có giới hạn: `SET LOCAL lock_timeout = '3s'` trước `INSERT`. Quá 3s ⇒ `409
   IDEMPOTENCY_IN_PROGRESS` + `Retry-After: 1`.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| `UNIQUE` trên `bookings.request_id` | Chỉ lưu lần **thành công** — lần đầu `409` thì lần sau có thể `201` (đã bỏ ở review) |
| Bản ghi `InProgress` commit ở transaction riêng, rồi xử lý ở transaction khác | Tiến trình chết sau khi commit `InProgress` ⇒ key kẹt tới khi hết lease; cần thêm cột lease, job gỡ kẹt; và ghi đơn + ghi kết quả ở hai transaction thì có thể lệch |
| Đọc bản ghi trước, chưa có thì xử lý rồi chèn | Đọc-rồi-ghi: 50 request cùng đọc "chưa có" ⇒ 50 lần xử lý |
| Redis `SETNX` cho key | Key và đơn nằm ở hai hệ thống ⇒ không atomic |

### Test chứng minh

- `HoldSeats_50ConcurrentSameKey_OneBookingSameResponse` — `Barrier(50)`; khẳng định đúng **1** đơn
  trong DB, **50** response cùng status và **cùng body byte-by-byte**.
- `HoldSeats_FirstRejected_RetryAfterSeatFreed_StillRejected` — lần 1 `409` (ghế của người khác);
  người kia huỷ; gửi lại cùng key ⇒ vẫn `409` với body cũ.
- `HoldSeats_SameKeyDifferentBody_422`.
- **Đỏ nếu** bỏ `PRIMARY KEY (customer_id, idempotency_key)` (migration test xoá ràng buộc) ⇒ > 1 đơn.

---

## Bài toán #6 — Refresh token bị dùng lại

**Kịch bản**: token làm mới bị đánh cắp; kẻ trộm và chủ cùng dùng nó. Hoặc hai request refresh đồng
thời với cùng token.
**Hậu quả nếu sai**: hai chuỗi token song song — kẻ trộm giữ phiên vô thời hạn.

### Cơ chế (ADR-0014)

```sql
-- Rotation: chỉ MỘT request thắng nhờ điều kiện trong lệnh
UPDATE identity.refresh_tokens
SET revoked_at = now(), replaced_by = @new_id
WHERE token_hash = @hash AND revoked_at IS NULL AND expires_at > now()
RETURNING user_id, family_id;
-- 1 dòng ⇒ chèn token mới (cùng family_id), trả cặp token mới
-- 0 dòng ⇒ tra token: không tồn tại/hết hạn ⇒ 401; đã bị revoke ⇒ DÙNG LẠI:
UPDATE identity.refresh_tokens SET revoked_at = now()
WHERE family_id = @family_id AND revoked_at IS NULL;      -- revoke cả chuỗi ⇒ 401
```

Hai request đồng thời cùng token: một thắng, request kia thấy token đã bị revoke ⇒ coi là dùng lại ⇒
revoke cả chuỗi, kể cả token vừa cấp cho bên thắng. Đây là **chủ ý** (ADR-0014): server không phân
biệt được "client gửi đúp" với "kẻ trộm"; client phải tuần tự hoá refresh.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Đọc token, kiểm `revoked_at`, rồi revoke | Hai request cùng đọc "chưa revoke" ⇒ hai chuỗi |
| Ân hạn vài giây cho token vừa bị thay | Mở lại đúng cửa sổ kẻ trộm cần; thêm trạng thái |
| Không rotation (refresh token dài hạn) | Không phát hiện được đánh cắp |

### Test chứng minh

- `Refresh_TwoConcurrentSameToken_OneWinsThenFamilyRevoked` — `Barrier(2)` ⇒ đúng 1 response `200`,
  1 response `401`; sau đó mọi token của family đều bị revoke (refresh bằng token mới cũng `401`).
- `Refresh_ReuseOldToken_RevokesFamily`.
- **Đỏ nếu** đọc-rồi-ghi (`UnsafeVariant=ReadThenRevoke`) ⇒ 2 response `200`.

---

## Bài toán #7 — Webhook Stripe trùng, sai thứ tự; hoàn tiền đúng một lần

**Kịch bản**: Stripe gửi `payment_intent.succeeded` 3 lần (có thể đồng thời); `payment_failed` đến
**sau** `succeeded`; worker hoàn tiền bị kill sau khi gọi Stripe, trước khi đánh dấu xong.
**Hậu quả nếu sai**: cấp vé hai lần, đơn chuyển trạng thái hai lần, hoàn tiền hai lần (`FR-38`,
`FR-39`, `SC-04`, `BR-17`, `BR-05`).

### Cơ chế

**Nhận** (một transaction, ADR-0009 luồng #2):

```sql
-- 1. Chữ ký Stripe-Signature hợp lệ (ngoài DB). Sai ⇒ 400.
-- 2. Chống lặp:
INSERT INTO payment.payment_events (stripe_event_id, intent_id, type, amount, payload, received_at)
VALUES (...) ON CONFLICT (stripe_event_id) DO NOTHING RETURNING id;
-- 0 dòng ⇒ đã xử lý ⇒ COMMIT, 200.
-- 3. succeeded: phát PaymentSucceeded (in-process) ⇒ Booking:
UPDATE booking.bookings SET status='Paid', closed_at=now()
WHERE id=@booking AND status='AwaitingPayment' AND expires_at > now() RETURNING id;
--   1 dòng: ghế Held→Sold theo hold_id (+version, outbox cache);
--           intent Unpaid→Captured (WHERE status='Unpaid');
--           BookingPaid ⇒ Ticketing chèn vé (UNIQUE showtime_seat_id); history.
--   0 dòng: intent → RefundPending (WHERE status IN ('Unpaid','Closed')),
--           INSERT refunds (UNIQUE intent_id), outbox RefundRequested.
-- 4. payment_failed: chỉ ghi sự kiện. charge.refunded: RefundPending → Refunded.
COMMIT;  -- 200
```

Hai webhook cùng `event.id` đồng thời: `INSERT` thứ hai **chờ** unique index, thấy conflict sau khi
thứ nhất commit ⇒ bỏ qua. Hai `event.id` **khác nhau** cùng nghĩa "thành công": lệnh `→ Paid` và
`Unpaid → Captured` có điều kiện trạng thái ⇒ lần hai 0 dòng ⇒ bỏ qua (`state-machines.md`).

Số tiền trong sự kiện ≠ `payment_intents.amount` ⇒ không cấp vé, `RefundPending` + log `Critical`.

**Gửi hoàn tiền** (outbox, ngoài transaction): `POST /v1/refunds` với `Idempotency-Key = refunds.id`.
Worker chết sau khi Stripe đã hoàn ⇒ lease hết ⇒ worker khác gọi lại cùng key ⇒ Stripe trả **cùng**
refund, không hoàn lần hai. Kết thúc khi `charge.refunded` đến (`RefundPending → Refunded`).

**Lỗi không mong đợi** (ví dụ vi phạm unique vé): rollback **cả** dòng sự kiện ⇒ trả `500` ⇒ Stripe
gửi lại; ghi `payment.webhook_failures` ở transaction riêng; ≥ 5 lần ⇒ cảnh báo (`data-model.md`).

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Kiểm "đơn đã `Paid` chưa" trong code | Hai webhook đồng thời cùng thấy "chưa" |
| Dùng lại cơ chế `idempotency_records` cho webhook | Không cần lưu response để phát lại (Stripe chỉ cần `2xx`); chống lặp phải nằm **trong** transaction nghiệp vụ, và `event.id` là khoá tự nhiên sẵn có |
| Gọi Stripe hoàn tiền trong transaction | Hoàn rồi mà rollback, hoặc commit rồi mà gọi lỗi (ADR-0006) |
| Hoàn tiền không `Idempotency-Key` | Worker retry sau timeout ⇒ hoàn hai lần; `UNIQUE (intent_id)` không chặn được lời gọi thứ hai |

### Test chứng minh

Dùng **sự kiện thật** từ Stripe test mode (`NR` "không giả lập"): test tạo PaymentIntent thật, xác
nhận bằng thẻ test `pm_card_visa`, lấy sự kiện thật qua `GET /v1/events`, rồi gửi lại nó vào webhook
của ta (ký bằng webhook secret của ta — như Stripe gửi lại). Test chạy khi có biến môi trường
`STRIPE_TEST_SECRET_KEY`; CI có secret này.

- `Webhook_SameEventThreeTimesConcurrently_OneTransitionOneTicketSet` ⇒ đúng 1 dòng `payment_events`,
  1 dòng lịch sử `→ Paid`, số vé = số ghế. **Đỏ nếu** bỏ `UNIQUE (stripe_event_id)` **và** điều kiện
  trạng thái (`UnsafeVariant=NoDedup`) ⇒ vi phạm unique vé ⇒ `500`, hoặc 2 dòng lịch sử.
- `Webhook_FailedAfterSucceeded_Ignored` ⇒ đơn vẫn `Paid`, intent vẫn `Captured`.
- `RefundWorker_KilledAfterStripeCall_RefundsOnce` — hook dừng tiến trình worker ngay sau lời gọi
  Stripe, trước `processed_at`; chạy lại ⇒ `GET /v1/refunds?payment_intent=…` trả **1** refund. **Đỏ
  nếu** bỏ `Idempotency-Key`.

---

## Bài toán #8 — Quét lại mã QR

**Kịch bản**: hai nhân viên ở hai cổng quét cùng một vé cùng lúc; hoặc quét 10 lần liền.
**Hậu quả nếu sai**: một vé vào hai người (`FR-07`, `SC-05`, `BR-10`).

### Cơ chế

```sql
INSERT INTO ticketing.check_ins (ticket_id, showtime_id, gate, scanned_by, checked_in_at)
VALUES (...) ON CONFLICT (ticket_id) DO NOTHING RETURNING checked_in_at;
-- 1 dòng ⇒ 201 CheckedIn
-- 0 dòng ⇒ SELECT checked_in_at, gate FROM ticketing.check_ins WHERE ticket_id = @id
--         ⇒ 409 TICKET_ALREADY_CHECKED_IN kèm thời điểm + cổng GỐC
```

Các kiểm tra trước đó (chữ ký, đúng suất, suất chưa hủy, phân công) là **đọc** — không cần khoá.
Suất bị hủy **đúng lúc** đang quét: chấp nhận được — check-in ghi nhận trước thời điểm hủy commit.

**Không dùng Redis `SETNX`** (ADR-0013): ràng buộc DB đã là bảo đảm;
Redis chỉ thêm một chỗ có thể lệch (key Redis còn mà transaction DB rollback ⇒ vé hợp lệ bị báo "đã
dùng"). Tải ở cổng là vài lượt/giây — không cần chặn sớm.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| `SELECT` xem đã check-in chưa rồi `INSERT` | Hai lượt cùng thấy "chưa" |
| Redis `SETNX` trước DB | Thêm điểm lệch, không thêm bảo đảm (ADR-0013) |
| Cột trạng thái `CheckedIn` trên `tickets` + `UPDATE … WHERE` | Đúng, nhưng phải ghi hai chỗ (trạng thái + bản ghi thời điểm/cổng) |

### Test chứng minh

- `Scan_SameTicket10Concurrently_OneCheckIn` — `Barrier(10)` ⇒ 1 dòng `check_ins`; 1 response `201`,
  9 response `409` cùng `checkedInAt` và `gate` gốc.
- **Đỏ nếu** kiểm-rồi-chèn và bỏ PK (`UnsafeVariant=CheckThenInsert` + migration test bỏ ràng buộc)
  ⇒ > 1 dòng.

---

## Bài toán #9 — Cache stampede trên sơ đồ ghế

**Kịch bản**: mở bán phim hot, cache sơ đồ ghế của suất trống (vừa mở, hoặc vừa hết TTL); 500 người
cùng mở sơ đồ.
**Hậu quả nếu sai**: 500 truy vấn cùng lúc vào DB đúng lúc DB đang chịu tải giữ ghế (`SC-11`, `SC-10`).

### Cơ chế

Cache Redis: hash `seatmap:{showtimeId}` — field `meta` (bố cục + giá, JSON; không đổi khi suất đang
bán) và mỗi ghế một field `s:{seatId}` = `status|heldUntil|version`. TTL 10 phút (lưới an toàn — độ
mới đến từ #10, không từ TTL).

Khi miss, **hai tầng single-flight**:

1. **Trong tiến trình:** `ConcurrentDictionary<showtimeId, Lazy<Task<SeatMap>>>` — mọi request trên
   cùng instance chờ **một** Task dựng lại.
2. **Giữa các instance:** Task đó trước khi đọc DB lấy `SET lock:seatmap:{id} {token} NX PX 3000`.
   Không lấy được ⇒ đợi cache có dữ liệu (thử lại mỗi 50ms, tối đa 500ms), hết hạn chờ ⇒ tự đọc DB
   (chấp nhận đọc thêm thay vì lỗi). Khoá Redis ở đây chỉ là **tối ưu**: mất khoá không làm sai dữ
   liệu (ghi cache theo phiên bản — #10).

Khi đọc: ghế `Held` có `heldUntil <= now` hiển thị là `Available` (khớp #1).

**Sơ đồ ghế là ảnh chụp** (`BR-23`): sai một nhịp không gây bán trùng — lệnh giữ luôn kiểm lại DB
(`FR-15`).

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Không cache | DB nhận toàn bộ tải đọc khi mở bán — `SC-11` không đạt |
| Chỉ khoá Redis, không single-flight trong tiến trình | 500 request trên một instance đều hỏi Redis lock — dồn tải sang Redis |
| Làm mới cache trước khi hết hạn (refresh-ahead) | Không giải quyết lần miss đầu tiên khi mở bán; thêm job |
| TTL ngẫu nhiên (jitter) | Chống nhiều key hết hạn cùng lúc, không chống một key nóng |

### Test chứng minh

- `SeatMap_100ConcurrentColdRequests_OneDbRead` — xoá cache, `Barrier(100)` gọi
  `GET /api/showtimes/{id}/seats` trên một instance ⇒ bộ đếm lệnh SQL có tag `seatmap.load` = **1**;
  100 response giống nhau.
- **Đỏ nếu** tắt single-flight (`UnsafeVariant=NoSingleFlight`) ⇒ bộ đếm ≈ 100.
- Đo: tỉ lệ hit cache ở bước load test (metric `seatmap_cache_requests_total{result}`).

---

## Bài toán #10 — Cập nhật cache theo từng ghế

**Kịch bản**: instance A giữ 4 ghế; người xem đang được instance B phục vụ mở sơ đồ ghế.
**Hậu quả nếu sai**: B hiển thị 4 ghế còn trống tới khi hết TTL; hoặc một lần dựng lại cache từ dữ
liệu cũ **đè** lên trạng thái mới (ghế vừa bán hiện lại "trống").

### Cơ chế — ghi đè từng ghế theo phiên bản, qua outbox

1. Mọi lệnh đổi trạng thái ghế tăng `showtime_seats.version` và `RETURNING id, status, held_until,
   version`; cùng transaction ghi `inventory.outbox_messages` loại `SeatStatesChanged` mang các bộ đó.
2. Dispatcher (sau commit; được "đánh thức" ngay khi commit trên cùng instance, và quét định kỳ 1s)
   chạy script Lua cho mỗi suất:

```lua
-- KEYS[1] = seatmap:{showtimeId}; ARGV = seatId, status|heldUntil|version, ...
if redis.call('EXISTS', KEYS[1]) == 0 then return 0 end   -- chưa có cache: lần đọc sau tự dựng
for i = 1, #ARGV, 2 do
  local cur = redis.call('HGET', KEYS[1], 's:' .. ARGV[i])
  if (not cur) or tonumber(string.match(cur, '[^|]+$')) < tonumber(string.match(ARGV[i+1], '[^|]+$')) then
    redis.call('HSET', KEYS[1], 's:' .. ARGV[i], ARGV[i+1])
  end
end
return 1
```

3. Dựng lại cache từ DB (#9) dùng **cùng** quy tắc "chỉ ghi khi version mới hơn" ⇒ một lần dựng đọc
   snapshot cũ không thể đè bản cập nhật mới hơn đã ghi trước nó.

Không xoá cả sơ đồ: xoá = lần đọc kế tiếp miss ⇒ mỗi lần giữ ghế gây một lần dựng lại khi mở bán.
Không có dòng "phiên bản của suất": mọi lần giữ ghế sẽ cùng cập nhật **một** dòng đó ⇒ điểm tranh
chấp mới giữa mọi người mua.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Xoá key khi có thay đổi | Đúng nhưng biến mỗi lần giữ ghế thành một lần miss; ghép với #9 thì vẫn ổn, nhưng tốn tải DB đúng lúc nóng nhất |
| Ghi cache ngay sau commit trong request, không outbox | Tiến trình chết sau commit ⇒ cache không bao giờ được cập nhật (tới TTL) |
| Version theo suất (một số đếm) | Hot row mới trên đường giữ ghế |
| Redis Pub/Sub để báo các instance xoá cache cục bộ | Không có cache cục bộ; Redis là cache chung |

### Test chứng minh

- `SeatMap_HoldOnInstanceA_InstanceBSeesHeld` — hai host `WebApplicationFactory` dùng chung Postgres +
  Redis. B đọc sơ đồ (làm nóng cache); A giữ 4 ghế; chờ outbox của A xử lý xong (poll
  `processed_at`); B đọc lại ⇒ đúng 4 ghế `Held`, **không** có lần đọc DB mới ở B. **Đỏ nếu** tắt ghi
  outbox `SeatStatesChanged` ⇒ B còn thấy `Available`.
- `SeatMap_StaleRebuild_DoesNotOverwriteNewerState` — hook làm chậm lần dựng lại (đọc DB xong, ngủ,
  rồi mới ghi); giữa lúc đó giữ ghế và chạy dispatcher ⇒ sau cùng cache có trạng thái mới. **Đỏ nếu**
  ghi cache không so version (`UnsafeVariant=BlindCacheWrite`).

---

## Bài toán #11 — Mở bán phim hot cạn connection pool

**Kịch bản**: 500 request/giây vào một suất; mỗi request giữ ghế giữ một connection trong suốt
transaction.
**Hậu quả nếu sai**: request xếp hàng chờ connection tới `Timeout` của pool (mặc định 15s) rồi lỗi
`The connection pool has been exhausted` ⇒ người dùng chờ 15s để nhận lỗi 500; request đọc cũng chết
theo (`SC-10`, `FR-30`).

### Cơ chế — admission control trước pool

- Pool Npgsql mỗi instance: `Maximum Pool Size = 40`, `Timeout = 3s`. Postgres `max_connections = 200`
  (đủ cho 3 instance × 40 + worker + công cụ).
- Endpoint **ghi** (giữ ghế, huỷ, thanh toán) đi qua `ConcurrencyLimiter` của ASP.NET Core
  (`Microsoft.AspNetCore.RateLimiting`): `PermitLimit = 30` (< pool size, chừa chỗ cho đọc và worker),
  `QueueLimit = 100`, hàng đợi FIFO. Quá ⇒ **`503 OVERLOADED` + `Retry-After: 2`** ngay lập tức.
- Endpoint đọc sơ đồ ghế được cache phục vụ (#9) nên gần như không cần connection.
- `statement_timeout = 5s` cho role ứng dụng: không câu nào giữ connection vô hạn.

Con số `30 / 100` là **khởi điểm**; đo baseline ở bước load test rồi chỉnh — ghi số đo vào `perf/baseline.csv`.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Tăng pool size thật lớn | Postgres chậm đi khi quá nhiều connection hoạt động; chỉ dời nút cổ chai |
| Hàng đợi ảo (waiting room) trước khi mở bán | Đúng hướng cho hệ thống thật, nhưng là một dự án riêng — ghi làm hướng mở rộng |
| PgBouncer transaction pooling | Không giải quyết quá tải; advisory lock theo transaction vẫn dùng được, nhưng thêm một thành phần vận hành |

### Test chứng minh

- **Load test** (k6): kịch bản mở bán 500 req/s trong 60s vào một suất 100 ghế.
- Khẳng định: **0** lỗi pool exhausted trong log; mọi response thuộc `{201, 409, 503}`; p99 < 2s; sau
  test, kiểm invariant #1 trên DB (không ghế nào có hai đơn).
- Đo bằng **`dotnet-counters`** (không chỉ latency): `Npgsql` — `busy-connections`, `idle-connections`,
  `pending-connection-requests`; `Microsoft.AspNetCore.Hosting` — `current-requests`;
  `Microsoft.AspNetCore.RateLimiting` — `current-queued-requests`, `requests-rejected`.
- **Đỏ nếu** tắt limiter (`UnsafeVariant=NoAdmissionControl`) ⇒ `pending-connection-requests` tăng vọt
  và xuất hiện lỗi pool exhausted. Chạy cả hai biến thể, ghi cả hai dòng vào `perf/baseline.csv`.

---

## Bài toán #12 — Rate limit trên nhiều instance

**Kịch bản**: 3 instance sau load balancer; hạn mức đăng nhập 10 lần/phút/IP.
**Hậu quả nếu sai**: limiter trong bộ nhớ mỗi instance ⇒ thực tế 30 lần/phút — dò mật khẩu nhanh gấp
3.

### Cơ chế

Token bucket trên Redis, một script Lua nguyên tử cho mỗi lần kiểm:

```lua
-- KEYS[1] = rl:{policy}:{subject}; ARGV = capacity, refill_per_ms
local t = redis.call('TIME')
local now = tonumber(t[1]) * 1000 + math.floor(tonumber(t[2]) / 1000)
local b = redis.call('HMGET', KEYS[1], 'tokens', 'ts')
local tokens = tonumber(b[1]) or tonumber(ARGV[1])
local ts = tonumber(b[2]) or now
tokens = math.min(tonumber(ARGV[1]), tokens + (now - ts) * tonumber(ARGV[2]))
local allowed = tokens >= 1
if allowed then tokens = tokens - 1 end
redis.call('HSET', KEYS[1], 'tokens', tokens, 'ts', now)
redis.call('PEXPIRE', KEYS[1], math.ceil(tonumber(ARGV[1]) / tonumber(ARGV[2])))
return allowed and 1 or 0
```

Thời gian lấy bằng `TIME` **ngay trong script**: một đồng hồ chung cho mọi instance (đồng hồ từng máy
lệch nhau), và đọc giờ + cập nhật bucket nằm trong cùng một lần chạy nguyên tử. Không truyền giờ từ
ứng dụng vào, và không gọi `TIME` riêng trước đó — hai lệnh tách rời thì không còn nguyên tử. Redis 7
mặc định nhân bản script theo hiệu ứng nên `TIME` trong script là hợp lệ.

| Policy | Subject | Hạn mức |
|---|---|---|
| `login` | IP | 10 / phút |
| `refresh` | IP | 30 / phút |
| `hold` | `customer_id` | 20 / phút |
| `scan` | `staff_id` | 120 / phút |

Redis không phản hồi ⇒ **cho qua** (fail-open) + metric `ratelimit_redis_errors_total`: rate limit là
bảo vệ, không phải tính đúng đắn; chặn toàn bộ đăng nhập vì Redis lỗi tệ hơn.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| `Microsoft.AspNetCore.RateLimiting` trong bộ nhớ | Mỗi instance một bộ đếm |
| `INCR` + `EXPIRE` (fixed window) | Cho phép gấp đôi hạn mức ở ranh giới hai cửa sổ |
| Sliding log (sorted set) | Chính xác hơn nhưng tốn bộ nhớ theo số request |

### Test chứng minh

- `Login_ThreeInstancesSharedQuota_ExactlyTenAllowed` — 3 host chung Redis; 30 lần đăng nhập sai từ
  cùng IP (header `X-Forwarded-For` trong test), luân phiên host ⇒ đúng 10 response khác `429`.
- **Đỏ nếu** dùng limiter trong bộ nhớ (`UnsafeVariant=InMemoryRateLimit`) ⇒ 30 response khác `429`.

---

## Bài toán #13 — Vượt 10 ghế/người/suất bằng yêu cầu song song

**Kịch bản**: phe vé gửi 20 yêu cầu song song, mỗi yêu cầu 2 ghế khác nhau, cho cùng suất.
**Hậu quả nếu sai**: cả 20 cùng đếm "đang có 0" ⇒ giữ 40 ghế (`FR-09`, `BR-08`).

### Cơ chế (ADR-0007)

Bước (3) của #2:

```sql
SELECT pg_advisory_xact_lock(hashtextextended(@customer_id::text || '/' || @showtime_id::text, 0));
-- rồi mới đếm SUM(seat_weight) của đơn Paid + AwaitingPayment còn hạn
```

Khoá tự nhả khi transaction kết thúc (commit hay rollback). Trùng băm giữa hai cặp khác nhau chỉ làm
hai cặp đó xếp chung hàng — chậm hơn chút, không sai. Khoá này chỉ tranh giữa các yêu cầu của **cùng
một người** cho **cùng một suất** ⇒ không ảnh hưởng người mua khác.

Chốt câu hỏi của ADR-0007: **advisory lock**, không phải dòng quota.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| Bảng `customer_showtime_quotas` + `SELECT … FOR UPDATE` | Phải `INSERT … ON CONFLICT` dòng trước khi khoá; thêm một bảng tăng theo (người × suất) mà không ai đọc; phải dọn |
| Bộ đếm có `CHECK (count <= 10)` | Phải cập nhật bộ đếm ở mọi đường hết hạn / huỷ / thanh toán — nhiều chỗ ghi dễ lệch |
| `pg_advisory_lock` (mức session) | Quên nhả ⇒ khoá sống theo connection trong pool |

### Test chứng minh

- `HoldSeats_20ParallelSameCustomer_TotalAtMostTen` — `Barrier(20)`, mỗi yêu cầu 2 ghế khác nhau ⇒
  tổng `SUM(seat_weight)` của khách cho suất = 10 (đúng 5 yêu cầu thắng), 15 yêu cầu `422
  CUSTOMER_SEAT_LIMIT`.
- **Đỏ nếu** bỏ khoá (`UnsafeVariant=NoQuotaLock`) ⇒ tổng > 10.

---

## Bài toán #14 — Giữ ghế đua với hủy suất

**Kịch bản**: quản lý rạp hủy suất đúng lúc một người đang giữ ghế.
**Hậu quả nếu sai**: lệnh giữ đọc `OnSale`, hủy suất commit và huỷ "mọi đơn đang chờ" (chưa thấy đơn
mới), rồi lệnh giữ commit ⇒ một đơn `AwaitingPayment` sống trên suất đã hủy ⇒ thanh toán thành công
⇒ vé cho suất đã hủy (`FR-24`).

### Cơ chế

- Giữ ghế: `SELECT … FROM scheduling.showtimes WHERE id = @id FOR KEY SHARE` (bước (1) của #2).
- Hủy suất: `SELECT … FOR UPDATE` trên cùng dòng **trước** mọi việc khác.

`FOR KEY SHARE` chỉ xung đột với `FOR UPDATE` ⇒ hàng trăm lệnh giữ ghế đồng thời **không** chặn
nhau, nhưng hủy suất phải chờ mọi lệnh giữ đang chạy commit, và lệnh giữ đến sau phải chờ hủy suất
commit rồi đọc thấy `Cancelled` ⇒ `409 SHOWTIME_CANCELLED`.

Trong transaction hủy suất, thứ tự lệnh: (a) `showtimes` → `Cancelled`; (b) `UPDATE bookings …
'Cancelled' WHERE showtime_id = @id AND status = 'AwaitingPayment' RETURNING id` (+ trả ghế, intent
`Closed`); (c) **sau đó** mới chọn đơn `Paid` để yêu cầu hoàn. Một webhook đang làm `→ Paid` cho một
đơn của suất: nếu nó commit trước (b), (b) bỏ qua đơn đó (đã `Paid`) và (c) — lệnh mới, snapshot mới —
thấy nó và hoàn tiền; nếu (b) đến trước, webhook thấy `Cancelled` ⇒ hoàn tiền. Không nhánh nào cấp vé
mà không hoàn.

### Phương án đã cân nhắc

| Phương án | Vì sao không chọn |
|---|---|
| `FOR SHARE` khi giữ ghế | Xung đột cả với `UPDATE` thường (mở bán) và tạo multixact nặng hơn khi nhiều người cùng khoá |
| Không khoá, kiểm lại trạng thái suất lúc thanh toán | Vẫn có đơn sống trên suất hủy tới khi hết hạn; thanh toán phải đọc thêm `Scheduling` |
| Khoá `FOR UPDATE` suất khi giữ ghế | Tuần tự hoá mọi người mua của suất |

### Test chứng minh

- `CancelShowtime_ConcurrentHolds_NoBookingSurvives` — `Barrier`: 20 lệnh giữ ghế + 1 lệnh hủy suất,
  hook làm chậm lệnh giữ sau bước (1) ⇒ sau cùng: 0 đơn `AwaitingPayment` của suất; mọi ghế
  `Available`; mỗi lệnh giữ là `201` (rồi bị hủy theo suất) hoặc `409 SHOWTIME_CANCELLED`.
- **Đỏ nếu** bỏ `FOR KEY SHARE` (`UnsafeVariant=NoShowtimeLock`) ⇒ còn đơn `AwaitingPayment` trên suất
  đã hủy.

---

## Tổng hợp

| # | Bài toán | Tầng giải | Cơ chế | Test | Đo bằng |
|---|---|---|---|---|---|
| 1 | Gán trùng ghế | DB | Khoá dòng + điều kiện trạng thái | integration | — |
| 2 | All-or-nothing | DB | Một transaction; quyết định trước khi ghi | integration | — |
| 3 | Deadlock | DB | `ORDER BY id FOR UPDATE`; thứ tự khoá toàn cục | integration | số `40P01` |
| 4 | Hết hạn ↔ webhook ↔ người mới | DB | `expires_at > now()`; trả ghế theo `hold_id`; `SKIP LOCKED` | integration | — |
| 5 | Idempotency key | DB | PK + chờ trên unique + savepoint | integration | — |
| 6 | Refresh reuse | DB | `UPDATE … WHERE revoked_at IS NULL` + revoke family | integration | — |
| 7 | Webhook trùng / sai thứ tự / hoàn tiền | DB + Stripe | `UNIQUE event_id`, điều kiện trạng thái, outbox + `Idempotency-Key` | integration (Stripe test mode) | — |
| 8 | Quét lại QR | DB | PK `ticket_id` + `ON CONFLICT` | integration | — |
| 9 | Cache stampede | **Redis** + tiến trình | Single-flight hai tầng | integration | hit rate |
| 10 | Cache theo ghế | **Redis** | Ghi đè theo `version`, qua outbox | integration (2 host) | — |
| 11 | Cạn pool | Ứng dụng | Admission control trước pool | **load (k6)** | **`dotnet-counters`** |
| 12 | Rate limit nhiều instance | **Redis** | Token bucket Lua | integration (3 host) | — |
| 13 | Quota 10 ghế | DB | Advisory lock (người, suất) | integration | p99 giữ ghế |
| 14 | Giữ ghế ↔ hủy suất | DB | `FOR KEY SHARE` vs `FOR UPDATE` | integration | — |

**Bài toán nào chỉ tồn tại vì domain model này:** #2, #3 (seat-based, n ghế — ADR-0003); #4 dạng "lấy
ghế quá hạn" (hết hạn theo thời gian — ADR-0006); #10 (trạng thái theo từng ghế); #13 (luật chống phe
vé `BR-08`); #14 (hủy suất huỷ đơn chờ — ADR-0006). Với mô hình tier-based cũ, #2/#3/#10 không có.

## Vì sao chọn cơ chế này, cách kia thua ở đâu — năm câu trả lời ngắn

1. **#1** — điều kiện trong lệnh thắng `SERIALIZABLE` vì đúng như nhau mà không bắt 99/100 request
   retry.
2. **#3** — khoá tường minh `ORDER BY id` thắng "một câu `UPDATE ANY`" vì thứ tự của câu sau là chi
   tiết của planner, không phải hợp đồng.
3. **#5** — chờ trên unique index trong cùng transaction thắng "bản ghi `InProgress` riêng" vì không
   bao giờ kẹt key khi tiến trình chết.
4. **#8** — PK ở DB thắng Redis `SETNX` vì nó là bảo đảm, còn Redis chỉ thêm khả năng lệch.
5. **#10** — ghi đè theo `version` thắng xoá key vì không biến mỗi lần giữ ghế thành một lần miss, và
   thắng "version theo suất" vì không tạo hot row.
