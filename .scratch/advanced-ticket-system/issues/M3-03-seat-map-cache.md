# M3-03: Cache sơ đồ ghế theo từng ghế + version

Mode: you-implement-i-review
Status: ready
Blocked by: M3-02
Traces to: FR-13
Concurrency: #9 #10
Branch: m3-03-seat-map-cache

## Mục tiêu
Phần lớn lượt xem sơ đồ ghế được phục vụ không chạm DB (`SC-11`), mà không để cache cũ đè trạng thái mới
(ADR-0012). Đây là tối ưu mà số "trước" ở M3-02 đang chờ.

## Phạm vi
**Trong:**
- Cache Redis cho `GET /api/showtimes/{showtimeId}/seats`: hash theo suất, mỗi ghế một field, TTL là lưới an toàn
  ([`concurrency.md`](../../../docs/design/concurrency.md) #9).
- **Single-flight hai tầng khi miss** (#9): trong tiến trình (`Lazy<Task>` theo suất) + khoá Redis `SET NX PX` giữa
  các instance (chỉ là tối ưu, không phải tính đúng đắn). 100 request lạnh đồng thời ⇒ **đúng 1** lần đọc DB.
- **Ghi đè từng ghế theo `version`** (#10, ADR-0012): mỗi lần đổi trạng thái ghế tăng `showtime_seats.version`
  (cột đã có trong [`data-model.md`](../../../docs/design/data-model.md)) và ghi `SeatStatesChanged` vào
  `inventory.outbox_messages` cùng transaction; dispatcher chạy script Lua "chỉ ghi nếu version mới hơn".
  Lần dựng lại từ DB dùng cùng quy tắc.
- **Dùng lại outbox + dispatcher của M2-05** — không viết cơ chế outbox thứ hai. Ticket này chỉ thêm loại
  message `SeatStatesChanged`, handler của nó, và cơ chế "đánh thức" dispatcher khi commit nếu M2-05 chưa có.
- Phát metric `seatmap_cache_requests_total{result}` (quy ước đặt tên từ M3-01) để đo hit ratio.
- Ghế `Held` quá hạn hiển thị `Available` (khớp #1/#4); `asOf` là ảnh chụp (`BR-23`).
- Cờ `UnsafeVariant=NoSingleFlight` và `UnsafeVariant=BlindCacheWrite` (chỉ build test).
- Test trên **Redis và Postgres thật** (Testcontainers), hai host `WebApplicationFactory` dùng chung.
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

- Cache-aside Redis TTL 30s cho `GET /api/movies` và `GET /api/movies/{id}/showtimes` (`api-contract.md`
  "Endpoint có cache"); đo cùng lúc với cache sơ đồ ghế ở M3-05.

**Ngoài:**
- Rate limit Redis → M3-04. Đo số "sau" → M3-05.
- Redlock / khoá phân tán cho tính đúng đắn: **không dùng** (concurrency.md phần 1).

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — Bài toán #9 và #10 (cơ chế, Lua, phương án loại, test).
- [`ADR-0012`](../../../docs/adr/0012-seat-map-cache-per-seat-versioned.md).
- [`data-model.md`](../../../docs/design/data-model.md) — `inventory.showtime_seats` (cột `version`), mục "Outbox".
- [`architecture.md`](../../../docs/design/architecture.md) — D8 (outbox).
- [`api-contract.md`](../../../docs/design/api-contract.md) — `GET /api/showtimes/{showtimeId}/seats`.

## Test
Gợi ý — người học viết (tên lấy từ design):

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `SeatMap_100ConcurrentColdRequests_OneDbRead` | xoá cache, `Barrier(100)` trên một instance ⇒ bộ đếm lệnh có tag `seatmap.load` = 1, 100 response giống nhau | tắt single-flight (`NoSingleFlight`) ⇒ ≈ 100 |
| `SeatMap_HoldOnInstanceA_InstanceBSeesHeld` | B làm nóng cache, A giữ 4 ghế, chờ outbox A xử lý ⇒ B thấy đúng 4 ghế `Held`, **không** đọc DB mới | tắt ghi outbox `SeatStatesChanged` ⇒ B còn thấy `Available` |
| `SeatMap_StaleRebuild_DoesNotOverwriteNewerState` | rebuild chậm đọc snapshot cũ, giữa lúc đó giữ ghế + chạy dispatcher ⇒ cuối cùng cache có trạng thái mới | ghi cache không so version (`BlindCacheWrite`) |
| `SeatMap_HeldPastExpiry_ShowsAvailable` | ghế `Held` có `heldUntil <= now` hiển thị `Available` | hiển thị `Held` |
| `SeatMap_RedisDown_FallsBackToDb` | Redis không phản hồi ⇒ vẫn trả sơ đồ đúng từ DB | `500` |

## Câu hỏi review
- Vì sao khoá `SET NX PX` ở tầng thứ hai chỉ là tối ưu và mất nó không làm sai dữ liệu?
- Vì sao `version` theo từng ghế, không theo suất? Hot row nào sẽ xuất hiện nếu theo suất?
- Message `SeatStatesChanged` nằm trong transaction giữ ghế thì ghi gì, và nếu tiến trình chết ngay sau commit thì cache được cập nhật nhờ đâu?
- Script Lua trả 0 khi key chưa tồn tại — vì sao không tự tạo hash từ message?

## Done khi
- [ ] Bốn test đầu xanh, mỗi test đã thấy đỏ với biến thể tương ứng (ghi trong PR)
- [ ] Không cơ chế outbox mới: dùng đúng dispatcher của M2-05 (nêu trong PR)
- [ ] Hit ratio xem được qua `seatmap_cache_requests_total{result}` trong Prometheus
- [ ] `GET …/seats` dưới cache trả cùng shape với bản đọc DB (test hợp đồng của M1-03 vẫn xanh)
