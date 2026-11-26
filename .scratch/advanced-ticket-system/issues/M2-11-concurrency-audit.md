# M2-11: Kiểm toán bộ test concurrency

Mode: you-implement-i-review
Status: ready
Blocked by: M2-02, M2-04, M2-08, M2-10
Traces to: —
Concurrency: #1 #2 #3 #4 #5 #6 #7 #8 #13 #14
Branch: m2-11-concurrency-audit

## Mục tiêu
Chứng minh được câu "không bán trùng ghế dưới tranh chấp": mỗi bài toán DB-level có test xanh **và** một phép
thử âm tính đã chạy ra đỏ, ổn định qua nhiều lần chạy. Đây là demo của M2. Test được viết ở ticket tạo ra
tính năng; ticket này chỉ kiểm toán và hợp nhất hạ tầng.

## Phạm vi
**Trong:**
- Bảng kiểm toán (đặt trong PR / `docs/learning/log.md`): mỗi test liệt kê ở mục "Test chứng minh" của
  #1–#8, #13, #14 trong `concurrency.md` — tồn tại, đúng tên, xanh, thuộc `Category=Concurrency`.
- Cơ chế **thống nhất** cho `Concurrency:UnsafeVariant=<tên>` (chỉ có trong build test): một điểm đọc cấu
  hình, mỗi biến thể một `switch` rõ ràng ở đúng chỗ cần thay cơ chế đúng bằng cơ chế sai; gom các biến thể đã
  rải ở M1-04, M2-01, M2-03, M2-08, M2-10 về cơ chế này. Với các bài toán đỏ bằng cách bỏ ràng buộc / điều
  kiện (#4, #5, #7), ghi rõ cách phá (migration test hoặc biến thể) trong bảng kiểm toán.
- Mỗi test concurrency chạy lặp 20 lần liên tiếp không flaky (một lần đỏ ⇒ điều tra gốc rễ, không chạy lại
  cho qua).
- Mỗi phép thử âm tính chạy lại một lần cuối, kết quả (đỏ như dự kiến) ghi vào bảng kiểm toán.
- Trait `Category=Concurrency` trên mọi test race; `dotnet test --filter Category=Concurrency` chạy được
  riêng; CI chạy bộ này trong thời gian chấp nhận được (ghi số đo; nếu quá chậm thì tách job, không giảm số
  vòng lặp mà design yêu cầu).

**Ngoài:**
- Test cho #9–#12 (cache, admission control, rate limit) → M3-02, M3-03, M3-04.
- Viết thêm tính năng; sửa lỗi phát hiện được thì mở sửa trong ticket gốc hoặc ticket `fix` riêng.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — mục "Hạ tầng test concurrency (dùng chung)", từng "Test chứng minh", "Tổng hợp".
- [`gate-d-implementation.md`](../../../docs/plan/gate-d-implementation.md) — demo M2 và Exit checklist Gate D.
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-29 (Testcontainers, không InMemory).

## Test
Gợi ý — người học viết (các test là *của ticket này*, không phải test race).

| Test / kiểm tra | Khẳng định | Đỏ nếu… |
|---|---|---|
| `ConcurrencyTests_AllListedInDesignExist` (arch/reflection test) | mọi tên test trong bảng kiểm toán tìm thấy trong assembly và có trait `Concurrency` | một test bị đổi tên / xoá / quên gắn trait |
| `UnsafeVariant_OnlyInTestBuild` | build Release không có đường nào đọc `Concurrency:UnsafeVariant` | cờ rò vào build production |
| `UnsafeVariant_EachVariant_MakesItsTestFail` (kịch bản chạy tay, ghi kết quả) | mỗi biến thể trong `concurrency.md` (`ReadThenWrite`, `PerSeatTransaction`, `ClientOrderLocking`, `NoQuotaLock`, `ReadThenRevoke`, `NoDedup`, `CheckThenInsert`, `NoShowtimeLock`) làm đúng test của nó đỏ | test vẫn xanh khi cơ chế bị thay ⇒ test vô giá trị |
| Lặp 20 lần | 20/20 xanh cho từng test race | flaky ⇒ test dựa vào may rủi lịch chạy |

## Câu hỏi review
- Nêu một test race mà **không** có phép thử âm tính thì còn giá trị gì? Có test nào trong bộ này như vậy không?
- Vì sao `Barrier` + `pg_sleep` trong transaction thứ nhất tốt hơn chạy "nhiều lần cho chắc"?
- Test #3 xanh chỉ vì Postgres tự gỡ deadlock thì sai ở đâu, và test của ta chống điều đó bằng cách nào?

## Done khi
- [ ] Bảng kiểm toán đủ #1–#8, #13, #14; không dòng nào trống
- [ ] 20 lần liên tiếp xanh cho mọi test `Category=Concurrency`
- [ ] Mọi biến thể `UnsafeVariant` đã được chạy lại và ghi kết quả đỏ
- [ ] `dotnet test --filter Category=Concurrency` xanh cả local lẫn CI; thời gian CI được ghi lại
