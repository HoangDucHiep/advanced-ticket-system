# 22: Bộ test concurrency

Mode: you-implement-i-review
Gate: D
Layer: code
Blocked by: 18, 19, 20, 21
Traces to: FR-01, FR-02, FR-03, FR-04, FR-07, FR-08, FR-37, FR-38
Concurrency problem: #1 #2 #3 #4 #5 #7 #8

## Vì sao có ticket này

Đây là ticket **có giá trị CV cao nhất** của cả dự án. Vì sao:

Câu "tôi đã xử lý concurrency" trong CV nghĩa là **không có gì cả**. Câu "tôi viết test mà
fail nếu bỏ `SELECT FOR UPDATE`" là **bằng chứng** — không ai trả lời được, và không ai
phản bác được.

Và đây cũng là chỗ bạn học cách **chứng minh** thay vì **khẳng định**. Sau ticket này, mọi
tuyên bố về tính đúng đắn trong repo đều có test đứng sau.

## Bạn tạo ra

Bộ integration test chạy trên **Postgres và Redis thật** qua Testcontainers.

> Không dùng EF Core InMemory provider cho bất kỳ test nào ở ticket này. InMemory không có
> transaction, không có row lock, không enforce unique constraint — một test viết trên đó sẽ
> **pass dù logic concurrency của bạn sai**. Đây là cái bẫy phổ biến nhất của người mới và
> interviewer sẽ bắt được.

| Test | Bố trí | Khẳng định |
|---|---|---|
| `DoubleBooking_100Buyers_1Seat` | 1 ghế, 100 request song song | đúng 1 thành công, 99 `409` |
| `MultiSeat_AllOrNothing` | đặt 4 ghế, 1 ghế đã có người | `409`, và **cả 4 ghế** còn trống |
| `MultiSeat_PartialHeld_RolledBack` | đặt 4 ghế, 1 ghế thất bại giữa chừng | **không** ghế nào bị giữ lại |
| `Deadlock_OverlappingSeats` | 2 người chọn ghế chồng nhau **ngược thứ tự** | cả hai đều có phản hồi trong thời gian giới hạn; không treo |
| `HoldExpiry_RacesNewPurchase` | worker expire và người mua chạy cùng lúc trên ghế cuối | đúng một bên thắng; không mất ghế, không giữ trùng |
| `Idempotency_50ConcurrentSameKey` | 50 request cùng key | 1 đơn, 49 response giống hệt |
| `Idempotency_SameKeyDifferentBody` | cùng key, body khác | `422` |
| `Webhook_DeliveredThreeTimes` | cùng event 3 lần | đúng 1 transition, **1 bộ vé** đúng số ghế |
| `Webhook_AfterBookingExpired` | webhook sau khi đơn hết hạn | đơn vẫn `Expired`, không cấp vé |
| `CheckIn_SameQrScannedTenTimes` | 1 vé, 10 lần quét | đúng 1 bản ghi check-in; lần sau trả thời điểm gốc |
| `RefreshToken_Reuse` | dùng lại token đã xoay vòng | `401`, family bị revoke |
| `Cache_Stampede` | 100 request đọc 1 suất nóng | đúng 1 lần đọc DB |
| `Cache_InvalidatePerSeat` | đặt 4 ghế, instance khác đọc cache | thấy **đúng 4 ghế** đã bán |

Yêu cầu chất lượng:

- Test concurrency **thật sự chồng lấn**: dùng `Barrier` hoặc điểm đồng bộ để các request
  được phát ra cùng lúc. Không trông chờ may mắn.
- Khẳng định trên **trạng thái quan sát được** (dòng DB, mã HTTP), không trên chuỗi lời
  gọi nội bộ.
- Mỗi test class dùng **một** container, dữ liệu được xoá giữa các test để test độc lập.

## AI làm gì

AI **không viết code**. AI:

1. Nêu lại từng test cần viết, kèm invariant mà nó kiểm chứng.
2. Chờ bạn viết.
3. Review, tập trung vào:
   - Test có **thật sự** đồng thời không? Hỏi: "request có được phát ra cùng lúc không, hay
     chỉ chạy tuần tự rồi khẳng định?"
   - Khẳng định có **tautological** không? (Giá trị kỳ vọng tính lại bằng đúng công thức mà
     code dùng thì test pass theo cấu trúc.)
   - Test có kiểm tra **trạng thái** hay chỉ mã HTTP?
   - Với test deadlock: "test này luôn xanh. Làm sao bạn biết là vì cơ chế đúng hay vì test
     không chồng lấn đủ? Có cách nào chứng minh test này **có thể** bắt được deadlock
     không?"
4. **Đề xuất phép thử âm tính**: với mỗi test quan trọng, hỏi "nếu tôi thay cơ chế bằng cái
   sai, test này có fail không?" — và đề xuất một cách kiểm chứng điều đó.

## Tiêu chí hoàn thành

- [ ] Tất cả 13 test trong bảng tồn tại và pass
- [ ] Test chạy trên Postgres và Redis **thật** qua Testcontainers
- [ ] **Bằng chứng phủ định**: test bán trùng **fail** khi thay cơ chế bằng đọc-rồi-ghi; test
      idempotency **fail** khi bỏ unique constraint; test all-or-nothing **fail** khi giữ ghế
      từng cái một. Ghi lại cách bạn đã kiểm chứng điều này.
- [ ] Test concurrency dùng cơ chế đồng bộ để request thật sự chồng lấn
- [ ] Có test **chứng minh được** rằng test deadlock thật sự có khả năng bắt deadlock
- [ ] Khẳng định dựa trên trạng thái quan sát được
- [ ] Test chạy được trong CI
- [ ] Thời gian chạy cả bộ không quá chậm để lùa CI
