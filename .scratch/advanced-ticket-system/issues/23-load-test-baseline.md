# 23: Load test và baseline

Mode: pair-tdd
Gate: D
Layer: code
Blocked by: 22
Traces to: FR-11, FR-12, FR-13, FR-30
Concurrency problem: #11 #12

> **Đồng bộ với design (2026-11-25, Gate C đóng).** Khi ticket này mâu thuẫn với `docs/design/`,
> **design thắng**. Kịch bản đo và tiêu chí: `concurrency.md` #11 (admission control, `dotnet-counters`), #9 (tỉ lệ
> hit cache), #13 (p99 giữ ghế có khoá quota), ADR-0002 (ba biến thể khoá ghế). Mỗi lần chạy ghi một dòng
> `perf/baseline.csv`, có cả biến thể **không** admission control để so.

## Vì sao có ticket này

Đo baseline **trước khi tối ưu**. Đây là nguyên tắc không thể bàn cãi trong dự án CV:
nếu bạn tối ưu xong rồi mới đo, bạn không có "trước" để so, và con số của bạn là một
phát biểu không kiểm chứng được.

Đồng thời, ticket này biến các quyết định ở T12 từ **lý thuyết** thành **số đo**. Câu
"conditional UPDATE đa dòng tốt hơn `FOR UPDATE`" chỉ có giá trị nếu bạn có số liệu.

Và đây là lý do `dotnet-counters` quan trọng: nó đo **lock contention** ở tầng runtime, thứ
mà latency p99 không cho bạn thấy.

## Bạn tạo ra

- Script k6 trong `tests/Load/`, chạy qua Docker image `grafana/k6` (không cần cài k6):
  - `browse.js` — đọc nhiều: danh sách phim, đọc lịch chiếu, đọc sơ đồ ghế.
  - `hot-release.js` — N VUs đánh `POST /bookings` cùng lúc khi mở bán phim hot, tranh một suất
    chiếu còn khoảng 100 ghế. Kịch bản phải có **nhiều người chọn ghế chồng nhau** để kích
    hoạt cả bài toán #2 và #3.
  - `checkin.js` — lưu lượng quét vé ở cổng (`POST /api/check-in/scan`).
- **Lệnh seed** tạo dữ liệu thực tế (ví dụ 50 phim, 20 phòng, 100 suất chiếu, 10 000 đơn)
  để phép đọc không vô nghĩa.
- **Admission control**: giới hạn số request đồng thời vào đường tạo đơn, để khi quá tải
  thì bị từ chối một cách êm thay vì cạn connection pool. So sánh giới hạn này với
  `max_connections` của Postgres.
- **Biến thể đối chiếu**: một đường tạo đơn thay thế dùng `SELECT ... FOR UPDATE` trong
  transaction `REPEATABLE READ`, bật qua feature flag, để đo cùng điều kiện.
- **Đo runtime counter**: dùng `dotnet-counters` để ghi `monitor-lock-contentions` và số liệu
  thread pool trong cả hai lần chạy.
- `perf/baseline.csv` — mỗi lần chạy một dòng: ngày, scenario, biến thể, VU, RPS, p50,
  p95, p99, tỉ lệ lỗi, số lock contention, ghi chú.

## AI làm gì

AI viết **script k6 và lệnh seed** (đây là code kiểm thử, không phải production code, nên
mode `pair-tdd` cho phép AI viết phần này).

AI hỗ trợ đo:

- Giải thích cách chạy k6 trong Docker Compose, cách truyền biến môi trường, cách xuất
  summary ra file.
- Giải thích `dotnet-counters`: cách attach, cách chọn đúng counter, cách đọc chỉ số.
  `monitor-lock-contentions` là counter quan trọng nhất ở đây.
- Khi bạn chạy xong: giúp đọc kết quả và **đặt câu hỏi** — "biến thể nào nhanh hơn, chênh
  lệch bao nhiêu, và con số này có đủ tin cậy không (số lần chạy, độ lệch)?"

AI **không** tự quyết kết luận thay bạn — bạn là người viết phần "kết luận" của báo cáo.

## Tiêu chí hoàn thành

- [ ] Một lệnh duy nhất chạy được script k6
- [ ] `perf/baseline.csv` có ít nhất một dòng **cho mỗi scenario, trước khi** tối ưu gì
- [ ] Test mở bán phim hot báo cáo **đúng số ghế** thành công và **không** bán trùng ở mọi
      mức RPS
- [ ] Không có `TimeoutException` từ connection pool ở bất kỳ mức tải nào
- [ ] Có số liệu p95/p99 của hai biến thể, kèm số `dotnet-counters` đi kèm
- [ ] Số lần chạy đủ nhiều để kết luận có độ tin cậy, và điều đó được ghi lại
- [ ] Bảng so sánh cuối cùng nằm trong README
