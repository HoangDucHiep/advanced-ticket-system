# 07: Aggregates và invariants

Mode: co-design
Gate: B
Layer: domain
Blocked by: 06
Traces to: —

## Vì sao có ticket này

Đây là ticket quan trọng nhất của Lớp 2. Aggregate là ranh giới nhất quán: **một transaction
duy nhất bảo vệ được những invariant nào?**

Câu trả lời quyết định trực tiếp cơ chế concurrency. Nếu invariant "một ghế của một suất
chiếu thuộc về đúng một chủ sở hữu tại một thời điểm" nằm trong một aggregate, thì việc thực
thi nó là **một lệnh nguyên tử**. Nếu nó nằm rải rác ở ba aggregate, thì bạn cần một cơ chế
khác, phức tạp hơn nhiều.

Và đây là lý do DDD không phải là lý thuyết cho vui: aggregate boundary **là** ranh giới
transaction **là** đơn vị nhất quán **là** phạm vi của một lệnh SQL atomic.

## Bạn tạo ra

`docs/domain/aggregates.md`, mỗi aggregate một mục:

```
## Aggregate: <Tên>

**Bản chất**: <identity / value>
**Chứa**: <các entity/value object bên trong>
**Boundary**: <điều gì nằm ngoài aggregate này>

### Invariants

- INV-01: <luật bất biến> — <vì sao phải đúng>
- INV-02: ...

### Transaction boundary

<Hành động nào nằm trong một transaction, và transaction đó bảo vệ invariant nào>
```

Câu hỏi gợi ý:

1. Với mỗi context ở T06: aggregate chính của nó là gì? (Một context có thể có nhiều aggregate.)
2. **Aggregate nào chứa ghế?** Một suất chiếu có hàng trăm ghế — đó là hàng trăm dòng trong
   **một** aggregate. Câu hỏi lớn nhất của ticket này: một suất chiếu có phải là một aggregate
   không, hay phải chia? Và nếu chia, invariant "một ghế thuộc đúng một chủ" bảo vệ thế nào?
3. Invariant nào phải luôn đúng, kể cả **giữa chừng một transaction**? Ví dụ: giữ 4 ghế xong
   mới tạo đơn — nếu transaction rollback ở giữa chừng, có ghế nào bị giữ lại không?
4. Invariant nào **không thể** bảo vệ bằng một transaction đơn lẻ? (Ví dụ: "mỗi người chỉ
   được mua tối đa 4 vé trên 1 suất chiếu" — có cần unique constraint ở DB chứ không phải logic?)
5. Aggregate nào có thể lớn? Nếu một aggregate phải chứa hàng nghìn row, đó là dấu hiệu
   boundary sai.
6. Điều gì là **value object** ở đây? (Ví dụ: một khoảng tiền, một vị trí ghế, một `BookingId`.)

## AI làm gì

AI giảng phần khó trước khi hỏi:

- Aggregate, entity, value object khác nhau thế nào, và vì sao ranh giới aggregate quyết định
  ranh giới transaction.
- Vì sao một aggregate lớn là mùi code (anemic domain model, nhiều query).
- Vì sao invariant phải được **bảo vệ bằng cơ chế**, không bằng ý thức của lập trình viên.
- **Vì sao "giữ n ghế all-or-nothing" buộc ranh giới aggregate phải bao trọn n ghế đó** — và
  điều đó có thể mâu thuẫn với nguyên tắc "aggregate không được quá lớn". Đây là cuộc đánh đổi
  trung tâm của ticket này.

Rồi AI hỏi từng invariant một, và với mỗi invariant hỏi:

- "Cơ chế nào bảo vệ nó?" (constraint DB / check constraint / conditional update / lock / không
  thể bảo vệ được?)
- "Ai có thể vi phạm nó?" (chỉ aggregate đó hay code ở nơi khác?)

AI ghi vào `docs/domain/aggregates.md`. AI **không tự quyết boundary** — đây là quyết định của
bạn, AI chỉ phản biện.

## Tiêu chí hoàn thành

- [x] Có ít nhất 4 aggregate, mỗi cái ghi rõ boundary — **8 aggregate**: `Seat hold`,
  `Booking`, `Payment intent`, `Ticket`, `Showtime`, `Room`, `Movie`, `User`
  (mỗi file ghi rõ mục **Boundary**: thứ nằm ngoài aggregate)
- [x] Mỗi aggregate có ít nhất 2 invariant, mỗi invariant ghi rõ cơ chế bảo vệ — **47 invariant**,
  mỗi cái ghi: cơ chế bảo vệ + **ai có thể vi phạm** + `Traces to` về `BR-nn` / `FR-nn`
- [x] Có ít nhất một invariant được ghi rõ là **không thể** bảo vệ bằng transaction đơn lẻ,
      kèm hệ quả — **3 cái**, tách thành 3 dạng khác nhau:
  1. `BR-08` (tích luỹ xuyên nhiều aggregate) → quy tắc tầng ứng dụng, chấp nhận cửa sổ race
     vì hậu quả là *người dùng tự gian lận* chứ không phải *bán quá số ghế*
  2. `BR-20` (quan hệ giữa hai aggregate) → exclusion constraint ở tầng lưu trữ
  3. `BR-17` (cần điều phối nhiều bước qua 3 context) → unique constraint trên `event.id` +
     transaction chung trong modular monolith
- [x] Transaction boundary của mỗi aggregate được mô tả bằng hành vi, không phải bằng SQL —
  mỗi mục mô tả "một transaction duy nhất cho hành vi X", kèm kết quả thành công / thất bại
- [x] Có phân biệt rõ entity và value object — mục "Entity và Value Object" trong
  `aggregates/README.md`, bảng 14 dòng
- [x] Không aggregate nào bị coi là chứa thứ vốn thuộc về aggregate khác — mục "Không aggregate
  nào chứa thứ thuộc về aggregate khác", đối chiếu với bảng nguồn sự thật ở T06 (7 dòng, tất cả
  đều "Không"). Nêu rõ ngoại lệ `Gate assignment` và lý do
- [x] **Câu hỏi về kích thước aggregate (một suất chiếu có phải một aggregate không) đã được
      trả lời rõ ràng**, kèm lý do — **KHÔNG**. `Seat hold` là aggregate root (chứa ≤ 6
      `Showtime seat`). Lý do: `BR-02` không được phép bị cắt; ở phương án "mỗi ghế là một
      aggregate" thì all-or-nothing phải dùng compensating transaction ⇒ có thời điểm hệ thống
      vi phạm `BR-02`. Xem `aggregates/README.md` và `02-inventory.md`.

> **Các quyết định chính đã chốt trong phiên:**
>
> 1. **Ranh giới tồn kho ghế** → `Seat hold` là aggregate root (≤6 ghế); `Showtime seat` là entity
>    được bảo vệ bằng **điều kiện lệnh** (`UPDATE ... WHERE status = 'Available'`), không phải
>    bằng khoá đọc-sửa-ghi.
> 2. **`BR-01` được ép bằng điều kiện lệnh** — không có cửa sổ TOCTOU; hai lệnh đồng thời trên
>    cùng một ghế thì chỉ một thắng, lệnh còn lại ảnh hưởng 0 dòng và phải coi là thất bại.
> 3. **`Booking line` là entity bên trong** aggregate `Booking` — tổng tiền của đơn là hành vi của
>    aggregate, không phải phép cộng ở tầng khác.
> 4. **`BR-08` không nằm trong aggregate nào** — chấp nhận cửa sổ race, vì hậu quả là *người
>    dùng tự gian lận*, không phải *bán quá số ghế có thật*.
> 5. **`Ticket` là aggregate root** (1 vé = 1 aggregate); `BR-10` ép bằng **ràng buộc duy nhất**
>    trên `ticket_id` ở bảng check-in.
> 6. **`Payment intent` là aggregate root**, chứa lịch sử `Payment event` và ≤1 `Refund`;
>    "hoàn đúng một lần" ép bằng ràng buộc duy nhất 1↔≤1.
> 7. **`Showtime` là aggregate root** của `Scheduling` (chứa `Ticket price` theo `Seat type`);
>    `Room` và `Movie` là aggregate riêng. `Room seat` nằm trong `Room` dù phòng có hàng trăm
>    ghế — vì ghế vật lý **tĩnh**, và contention chỉ sinh ra khi có **ghi**.
> 8. **`User` là aggregate root**; đây là context duy nhất mà aggregate của nó **không** tham
>    chiếu aggregate nào khác — hệ quả của việc chọn `Customer/Supplier` thay vì
>    `Shared Kernel` ở T06.
>
> **Bài học ghi nhận:** aggregate boundary **không phải lúc nào cũng đủ** để bảo vệ invariant.
> Ba dạng "không bảo vệ được" — *tích luỹ xuyên nhiều aggregate* (`BR-08`), *quan hệ giữa hai
> aggregate* (`BR-20`), *cần điều phối nhiều bước qua nhiều context* (`BR-17`) — đều có cùng một
> nguyên nhân gốc: **ranh giới aggregate không trùng ranh giới transaction**. Ngược lại, một
> invariant trông như "toàn cục" (`BR-10`) có thể hoá thành "cục bộ" nếu đặt ranh giới đúng chỗ.

## Tài liệu tạo ra

`docs/domain/aggregates/` — 1 file index + 6 file theo context:

| File | Nội dung |
|---|---|
| `README.md` | Bảng tổng hợp aggregate → invariant → cơ chế, bảng 3 invariant không bảo vệ được, sơ đồ mermaid, phân biệt entity/value object, kiểm tra "không aggregate nào chứa thứ của aggregate khác" |
| `01-scheduling.md` | `Showtime`, `Room`, `Movie` |
| `02-inventory.md` | `Seat hold` — kèm lý do chi tiết vì sao không phải `Showtime` |
| `03-booking.md` | `Booking` — kèm phân tích `BR-08` |
| `04-payment.md` | `Payment intent` |
| `05-ticketing.md` | `Ticket` |
| `06-identity-access.md` | `User` |

> **Ghi chú (2026-11-19):** nghiệp vụ đổi sang đặt vé xem phim. Điểm cần chú ý khi vẽ context
> map: **"ghế của phòng"** và **"ghế của suất chiếu"** là hai thuật ngữ khác nhau và thuộc
> hai vùng ngôn ngữ có thể khác nhau — đó là ứng viên mạnh nhất cho một `Anti-Corruption Layer`.
