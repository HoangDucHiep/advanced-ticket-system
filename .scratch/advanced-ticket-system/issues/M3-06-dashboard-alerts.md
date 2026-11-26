# M3-06: Dashboard + alert

Mode: you-implement-i-review
Status: ready
Blocked by: M3-05
Traces to: FR-30
Concurrency: —
Branch: m3-06-dashboard-alerts

## Mục tiêu
Quản trị viên nhìn một màn hình là biết đang mở bán phim hot có ổn không (`FR-30`, `BR-28`): chỉ số phản ánh
lưu lượng **đang diễn ra**, đo được, không phải con số viết tay. Dashboard này là thứ chụp màn hình cho README (M4-02).

## Phạm vi
**Trong:**
- Một dashboard Grafana trả lời bốn câu hỏi, mỗi câu một hàng panel:
  1. Có đang bị **thử bán trùng ghế** và có bị chặn không (số `409 SEATS_UNAVAILABLE`, tỉ lệ so với giữ thành công; không bao giờ có dấu hiệu ghế hai chủ).
  2. Có **tranh chấp khoá** không (lock contention, deadlock `40P01` = 0).
  3. **Connection pool** có bão hoà không (busy / idle / pending connection requests, request đang xếp hàng ở limiter).
  4. **Rate limiting** có đang kích hoạt không (`429`, `503 OVERLOADED`, `ratelimit_redis_errors_total`).
  Thêm hàng nền: RPS, tỉ lệ lỗi, p50/p95/p99, cache hit ratio (`seatmap_cache_requests_total{result}`), outbox backlog / dead.
- Tối thiểu **2 alert rule có nghĩa**, mỗi rule có ngưỡng lấy từ số đo ở `perf/report.md` (không đoán): ví dụ tỉ lệ lỗi `5xx` và p99 vượt ngưỡng
  trong một khoảng thời gian; thêm rule cho `payment_webhook_poison_total` hoặc outbox `dead` nếu người học thấy đáng. Mỗi rule ghi: kích hoạt khi nào, người trực làm gì.
- **Provision bằng file** trong repo (dashboard JSON + datasource + alert rules) qua Docker Compose của M3-01; `docker compose up` là có dashboard, không cấu hình tay.
- Dashboard dùng đúng tên metric của M3-01 / M3-03 / M3-04; không label không giới hạn.

**Ngoài:**
- Phát metric, trace, Prometheus/Grafana hạ tầng → M3-01. Cache, rate limit → M3-03, M3-04.
- Số liệu và `report.md` → M3-05. README tổng → M4-02.
- Kênh gửi thông báo thật (email, Slack): alert chỉ cần hiện trạng thái trong Grafana.

## Đọc trước
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Vận hành" (`/metrics` nội bộ).
- [`concurrency.md`](../../../docs/design/concurrency.md) — #11 (counter pool / limiter), #9 (hit ratio), #12 (`ratelimit_redis_errors_total`).
- [`04-business-rules.md`](../../../docs/prd/04-business-rules.md) BR-28; [`00-overview.md`](../../../docs/prd/00-overview.md) SC-08..SC-11.
- `perf/report.md` (M3-05) và bảng tên metric trong PR của M3-01.

## Test
Gợi ý — người học viết (phần dashboard là cấu hình, test là kiểm tra tự động + xác nhận bằng tải thật):

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Dashboard_Json_ValidAndProvisioned` (CI hoặc script) | JSON dashboard / alert rule parse được; mọi metric được tham chiếu có trong danh sách metric của M3-01 | panel trỏ tới metric không tồn tại |
| Chạy k6 `hot-release` với Compose lên | panel 409 / pool / limiter / 429-503 có số khác 0 và khớp counter đo bằng `dotnet-counters` cùng lúc | panel phẳng hoặc lệch số |
| Bơm lỗi có chủ ý (dừng Redis / hạ limiter) | alert tương ứng chuyển sang trạng thái firing trong thời gian chờ đã định | alert không bắn |
| Không có panel nào group theo id không giới hạn | review truy vấn PromQL | `by (showtimeId)` v.v. |

## Câu hỏi review
- Với mỗi panel: nếu hệ thống đang sập, panel này có đổi không? (BR-28: tránh "nhìn ổn mà đang sập".)
- Ngưỡng alert lấy từ đâu trong `report.md`? Cái gì làm nó không bắn nhầm khi tải bình thường?
- Vì sao "tỉ lệ `409` cao" không nhất thiết là sự cố, còn "deadlock > 0" thì có?
- Panel nào trả lời "có đang bán trùng ghế không" — và bằng chứng nào ở DB mà dashboard không thay thế được?

## Done khi
- [ ] `docker compose up` có dashboard + alert rules, không bước tay
- [ ] Dashboard trả lời đủ bốn câu hỏi, đã xác nhận bằng một lần chạy `hot-release`
- [ ] ≥ 2 alert rule, ngưỡng có nguồn số đo, đã thấy bắn bằng lỗi bơm có chủ ý
- [ ] Tất cả nằm trong repo dưới dạng file (không export tay không kiểm soát được)
