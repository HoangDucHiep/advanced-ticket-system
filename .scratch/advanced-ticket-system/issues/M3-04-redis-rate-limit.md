# M3-04: Rate limit Redis dùng chung nhiều instance

Mode: you-implement-i-review
Status: ready
Blocked by: M3-02
Traces to: FR-30
Concurrency: #12
Branch: m3-04-redis-rate-limit

## Mục tiêu
Một hạn mức dùng chung cho cả cụm: 3 instance không được cho dò mật khẩu nhanh gấp 3
([`concurrency.md`](../../../docs/design/concurrency.md) #12). Đồng thời đóng Q07: token bucket Lua chịu được bao nhiêu req/s.

## Phạm vi
**Trong:**
- Token bucket trên Redis bằng **một script Lua nguyên tử** mỗi lần kiểm; thời gian lấy từ Redis, không từ
  đồng hồ instance (#12).
- Áp cho các policy trong bảng #12: `login` (IP), `refresh` (IP), `scan` (`staff_id`, check-in) và **`hold`**
  (`customer_id`) — `hold` có trong bảng #12 và `api-contract.md` (`POST …/bookings`), nên đi cùng ticket này.
- Khi vượt: `429 RATE_LIMITED` + `Retry-After`, **sinh qua `IProblemDetailsService`** (building-blocks B4) để ra
  cùng shape lỗi, có `traceId`/`code`/`action`.
- Redis không phản hồi ⇒ **cho qua (fail-open)** + metric `ratelimit_redis_errors_total` (#12); `/health/ready`
  vẫn là nơi báo Redis chết.
- Lấy IP thật sau load balancer (`X-Forwarded-For` — cấu hình forwarded headers chỉ tin proxy đã biết).
- Cờ `UnsafeVariant=InMemoryRateLimit` (chỉ build test).
- Đo throughput script Lua (req/s, độ trễ) bằng k6 và ghi dòng vào `perf/baseline.csv` để trả lời Q07.
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

**Ngoài:**
- Admission control `503` (không phải rate limit) → M3-02.
- Cache Redis sơ đồ ghế → M3-03.
- Chọn lại số hạn mức từ số đo / báo cáo → M3-05.
- Rate limit theo endpoint khác bảng #12: không làm.

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — Bài toán #12 (Lua, bảng policy, fail-open, test) và phần 1 (vì sao Redis chỉ cho tối ưu / bảo vệ).
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — B4: `429`/`503` do middleware sinh, `OnRejected` gọi `IProblemDetailsService`.
- [`api-contract.md`](../../../docs/design/api-contract.md) — `POST /api/auth/login`, `/refresh`, `POST /api/check-in/scan`, bảng mã lỗi toàn cục (`429`).
- [`open-questions.md`](../../../docs/learning/open-questions.md) — Q07.

## Test
Gợi ý — người học viết:

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Login_ThreeInstancesSharedQuota_ExactlyTenAllowed` | 3 host dùng chung Redis; 30 lần đăng nhập sai cùng IP (`X-Forwarded-For`) luân phiên host ⇒ đúng 10 response khác `429` | limiter trong bộ nhớ (`InMemoryRateLimit`) ⇒ 30 response khác `429` |
| `RateLimited_Response_IsProblemJsonWithRetryAfter` | `429` có `code = RATE_LIMITED`, `Retry-After`, `traceId` | thiếu header hoặc không phải problem+json |
| `RateLimit_RedisDown_FailsOpenAndCountsError` | dừng container Redis ⇒ request vẫn qua, `ratelimit_redis_errors_total` tăng | request bị chặn / `500` |
| `RateLimit_BucketRefills_AfterWindow` | sau khoảng refill, lại được phép | bucket không hồi |
| `RateLimit_TwoSubjects_IndependentBuckets` | IP A hết hạn mức không ảnh hưởng IP B | dùng chung một key |

## Câu hỏi review
- Vì sao fixed window `INCR + EXPIRE` cho phép gấp đôi hạn mức ở ranh giới cửa sổ?
- Vì sao `now_ms` lấy từ Redis chứ không từ instance, và `TIME` trong script Lua ảnh hưởng gì đến replication / script?
- Vì sao fail-open là đúng cho rate limit nhưng sẽ sai cho idempotency hay quota ghế?
- `X-Forwarded-For` giả mạo bởi client thì chuyện gì xảy ra nếu tin vô điều kiện?

## Done khi
- [ ] Test 3 instance xanh, đã thấy đỏ với `InMemoryRateLimit` (ghi trong PR)
- [ ] `429` ra qua `IProblemDetailsService`, cùng shape với các lỗi khác
- [ ] Fail-open có test + metric
- [ ] Dòng đo throughput trong `perf/baseline.csv`; Q07 chuyển đáp án vào `concurrency.md` #12 và xoá hàng khỏi `open-questions.md` (hoặc ghi lý do vì sao còn mở, chốt ở M3-05)
