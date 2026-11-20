# 12: Concurrency design — 12 bài toán → cơ chế → test chứng minh

Mode: explain + co-design
Gate: C
Layer: design
Blocked by: 07, 08
Traces to: —

## Vì sao có ticket này

Đây là **trái tim** của dự án. 12 bài toán ở `docs/plan/00-roadmap.md` là giả định từ trước khi
có domain model. Giờ domain đã rõ, bạn phải chốt lại: bài nào còn đúng, và **cơ chế cụ thể
là gì**.

Khác biệt giữa một người biết và một người giỏi không nằm ở việc dùng `SELECT ... FOR UPDATE`.
Nó nằm ở việc **trả lời được**: "Tại sao chọn cách này, và cách kia thua ở đâu?" — và câu trả
lời phải là **số đo**, không phải lý thuyết.

Ticket này có hai phần: `explain` (AI dạy bạn cơ chế) rồi `co-design` (bạn chốt cơ chế).

## Bạn tạo ra

`docs/design/concurrency.md`, mỗi bài toán một mục:

```
## Bài toán #N — <tên>

**Kịch bản**: <ai làm gì đồng thời với ai>
**Hậu quả nếu làm sai**: <bán trùng ghế, mất tiền, vé trùng, kẹt...>

### Cơ chế

<Cơ chế cụ thể, tới mức có thể viết SQL>

### Phương án thay thế đã cân nhắc

| Phương án | Ưu | Nhược |

### Test chứng minh

- <tên test>
- <khẳng định cụ thể>
- <cách test này fail nếu cơ chế bị thay bằng cái sai>
```

Bắt buộc có:

- **Test chứng minh** cho **mọi** bài toán — không có "sẽ test sau".
- Dòng **"cách test này fail nếu cơ chế bị thay bằng cái sai"** — đây là điều phân biệt
  một test có giá trị với một test rỗng.

### 12 bài toán

| # | Bài toán |
|---|---|
| 1 | Gán trùng ghế (double booking) |
| 2 | **Đặt N ghế phải all-or-nothing** |
| 3 | **Deadlock khi tranh ghế chồng nhau** |
| 4 | Hết hạn chỗ giữ đua với người mua mới |
| 5 | Idempotency key race |
| 6 | Refresh token reuse |
| 7 | Webhook Stripe trùng / sai thứ tự |
| 8 | Quét lại QR |
| 9 | Cache stampede trên sơ đồ ghế |
| 10 | **Invalidate cache theo từng ghế** |
| 11 | Mở bán phim hot cạn connection pool |
| 12 | Rate limit nhiều instance |

Câu hỏi gợi ý:

1. Với từng bài toán: bạn sẽ **chứng minh** điều gì? Bằng quan sát nào từ bên ngoài?
2. Test concurrency cần làm gì để **thật sự** chồng lấn, chứ không chỉ "gọi song song"? (gây
   barrier trước khi gọi? dùng task có delay ngẫu nhiên?)
3. Với **bài toán #2**: nếu giữ 4 ghế mà 1 ghế đã có người, làm sao test biết chắc **cả 4 ghế**
   đã được trả lại — chứ không chỉ biết yêu cầu bị từ chối?
4. Với **bài toán #3**: test deadlock cần làm gì để *thật sự* kích hoạt vòng chờ? Nếu test
   luôn xanh, làm sao biết là vì cơ chế đúng hay vì test không chồng lấn đủ?
5. Bài toán nào có **nhiều cách giải quyết ngang nhau**? Bạn sẽ đo để chọn, hay sẽ chọn theo
   lý thuyết rồi đo để xác nhận?
6. Bài toán nào **không thể** giải quyết chỉ bằng database? (Đó là bài cần Redis, hoặc cần
   thừa nhận giới hạn.)
7. Bài toán nào sẽ **không xuất hiện** nếu domain model khác? (Câu hỏi này bảo đảm bạn không
   đang giải bài toán của một hệ thống khác.)

## AI làm gì

### Phần `explain` — AI dạy

AI trình bày, cho mỗi bài toán, các cơ chế khả dĩ với đánh đổi thật:

- **Atomic conditional UPDATE** — một câu, một round trip, đánh giá `WHERE` trên giá trị mới
  nhất. Đánh đổi: predicate phải viết đúng, không trả về *lý do* thất bại.
- **`SELECT ... FOR UPDATE`** — khóa dòng, đọc, kiểm tra trong code, cập nhật. Đánh đổi: giữ
  lock qua application code, lock convoy, cần quản lý transaction thủ công.
- **Khoá theo thứ tự cố định** — cách chống deadlock kinh điển: luôn khoá ghế theo `seat_id`
  tăng dần. Đánh đổi: cần sắp xếp ở tầng code, và **Postgres không tự bảo đảm thứ tự `UPDATE`**
  với `WHERE ... = ANY(...)` — đây là chi tiết phải kiểm chứng bằng test, không được coi là
  hiển nhiên.
- **Postgres advisory lock** — khóa theo key ở tầng DB. Đánh đổi: thêm một round trip, và
  khóa sống lâu hơn cả transaction nếu không cẩn thận.
- **Redis `SETNX` / token bucket** — cho rate limit và fast-reject. Đánh đổi: thêm network hop
  vào hot path, và **không** thay thế được guarantee ở DB.
- **Unique constraint + `ON CONFLICT`** — cho idempotency và chống trùng. Đánh đổi: cần xử lý
  riêng nhánh conflict.
- **Isolation level** — `READ COMMITTED` vs `REPEATABLE READ` vs `SERIALIZABLE`, và cái giá
  của `SERIALIZABLE` (retry khi serialization failure).
- **Outbox pattern** — cho side effect sau commit.
- **`FOR UPDATE SKIP LOCKED`** — cho hàng đợi worker.
- **Single-flight lock** — chặn stampede khi cache miss, cho bài toán #9.
- **Version-based cache** — ghi đè trạng thái ghế vào cache và đọc theo phiên bản, cho bài toán
  #10.

AI giải thích **tại sao** Redlock là câu trả lời sai (bổ sung network hop, không cứu được
logic DB sai, và có vấn đề lý thuyết đã biết) — để bạn không dùng nó một cách máy móc.

Rồi AI hỏi lại bạn bằng câu hỏi cụ thể cho từng bài toán.

### Phần `co-design` — bạn chốt

Với mỗi bài toán AI hỏi: chọn cơ chế nào, và **cách nào sẽ được đo**. AI ghi vào
`docs/design/concurrency.md`.

## Tiêu chí hoàn thành

- [ ] Cả 12 bài toán trong roadmap đều có mục trong file, hoặc ghi rõ là đã bị loại vì lý do
      gì (domain không có nó nữa)
- [ ] Mọi bài toán đều có test chứng minh, và test nằm ở đúng tầng (unit / integration /
      load)
- [ ] Mọi bài toán đều có dòng "cách test này fail nếu cơ chế bị thay bằng cái sai"
- [ ] Mỗi bài toán liệt kê ít nhất một phương án thay thế đã cân nhắc
- [ ] **Bài toán #2 và #3 có cơ chế cụ thể tới mức viết được SQL**, kèm phần trình bày thứ
      tự khoá
- [ ] Có phân biệt rõ bài toán nào giải được ở tầng DB và bài nào cần Redis
- [ ] Có ghi rõ bài toán nào được đo bằng `dotnet-counters` thay vì chỉ bằng latency
- [ ] Bạn trả lời được: "vì sao chọn cơ chế này, cách kia thua ở đâu" cho ít nhất 5 bài toán
