# M1-03: Đọc công khai — phim, lịch chiếu, sơ đồ ghế, availability, chặn ghế

Mode: you-implement-i-review
Status: ready
Blocked by: M1-02
Traces to: FR-11, FR-12, FR-13, FR-20, FR-23, FR-34
Concurrency: —
Branch: m1-03-public-read

## Mục tiêu
Người xem thấy được phim nào đang chiếu, suất nào, ghế nào còn; quản lý rạp thấy bốn con số và chặn được ghế.
Sơ đồ ghế **đọc thẳng DB (Dapper), chưa cache**: ở M3-02 ta cần số đo "trước" không có cache thì M3-03 mới
có câu chuyện "sau". Đừng thêm Redis ở đây.

## Phạm vi
**Trong** (endpoint ở [`api-contract.md`](../../../docs/design/api-contract.md) mục "Scheduling" các `GET` và mục "Inventory"):
- `GET /api/movies` (phim có ≥ 1 suất `OnSale` chưa bắt đầu, phân trang), `GET /api/movies/{movieId}/showtimes`
  (`404 MOVIE_NOT_FOUND`). Module `Scheduling`.
- `GET /api/manager/showtimes` (suất của người gọi, lọc `from/to/status`). Module `Scheduling`.
- `GET /api/showtimes/{showtimeId}/seats`: suất không `OnSale` hoặc đã bắt đầu ⇒ `404` (FR-34). Phân biệt `Held` với
  `Sold`; ghế `Blocked` không xuất hiện; ghế `Held` quá hạn hiển thị `Available`; `availableSeatCount`. Module
  `Inventory` ghép bố cục + giá lấy từ `Scheduling.Contracts` với trạng thái ghế của chính nó (D9).
- `GET /api/manager/showtimes/{showtimeId}/availability`: bốn con số `available / held / sold / blocked`
  cộng lại = số ghế phòng; chỉ chủ suất / `Administrator`, khác ⇒ `404`.
- `POST /api/manager/showtimes/{showtimeId}/seats/block` và `/unblock`: all-or-nothing (`Available ↔ Blocked`),
  ghế đang giữ / đã bán ⇒ `409 SEATS_NOT_BLOCKABLE`; mỗi lần đổi trạng thái tăng `version`.
- Test nằm ở project test của module (tạo từ template M0-08 nếu chưa có) và dùng `TicketSystem.Testing`.

**Ngoài:**
- Cache Redis cho sơ đồ ghế và danh sách phim, single-flight, cập nhật theo ghế qua outbox (M3-03, bài toán #9 #10).
- Rate limit, admission control (M3-02, M3-04).
- Giữ ghế (M1-04); cơ chế lấy ghế giữ quá hạn khi giữ (M2-01) — ở đây chỉ **hiển thị** ghế `Held` quá hạn là `Available`.

## Đọc trước
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Inventory" (đoạn giải thích `Held` vs `Sold`, `asOf`) và "Scheduling".
- [`architecture.md`](../../../docs/design/architecture.md) — mục "D9" (đoạn "Sơ đồ ghế của suất (FR-13) cũng là endpoint của Inventory"), "D7" (Dapper cho đọc nóng).
- [`cross-context-contracts.md`](../../../docs/domain/cross-context-contracts.md) — "R2" (bốn con số), "R9".
- [`data-model.md`](../../../docs/design/data-model.md) — `inventory.showtime_seats` (index, `version`), `scheduling.showtimes` (index `OnSale`).
- [`building-blocks.md`](../../../docs/design/building-blocks.md) — B8 (đọc = query handler, chiếu thẳng DTO).

## Test
Gợi ý — người học viết.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Movies_lists_only_movies_with_onsale_future_showtime` | Phim chỉ có suất `NotOnSale`/đã qua/`Cancelled` không có trong danh sách | Lọc sai điều kiện |
| `SeatMap_not_on_sale_returns_404` | Suất `NotOnSale` ⇒ `404`, cả khi biết id | Lộ suất chưa mở bán |
| `SeatMap_distinguishes_held_and_sold` | Ghế `Held` và `Sold` có `status` khác nhau; không có `heldUntil` | Gộp hai trạng thái |
| `SeatMap_expired_hold_shows_available` | Ghế `Held` có `held_until` đã qua ⇒ `Available`; `availableSeatCount` tính cả ghế đó | Hiển thị `Held` mãi |
| `SeatMap_hides_blocked_seats` | Ghế `Blocked` không có trong `seats` | Lộ ghế chặn |
| `Availability_sums_to_room_seat_count` | `available + held + sold + blocked = roomSeatCount`; ghế `Held` quá hạn tính vào `available` | Đếm lệch |
| `Availability_of_other_owner_returns_404` | Quản lý khác ⇒ `404` | Trả `403` |
| `Block_seats_all_or_nothing` | Một ghế đang `Held` trong danh sách ⇒ `409 SEATS_NOT_BLOCKABLE` kèm `seatIds`, không ghế nào bị chặn | Chặn được một phần |
| `Unblock_returns_seats_to_available_and_bumps_version` | `Blocked → Available`, `version` tăng | Không tăng `version` |

## Câu hỏi review
1. Vì sao `GET .../seats` do `Inventory` phục vụ chứ không phải `Scheduling`? Nếu `Scheduling` làm sẽ tạo vòng phụ thuộc nào?
2. `Held` và `Sold` khác nhau có chủ ý — người xem được lợi gì, và vì sao không trả `heldUntil`?
3. Câu SQL đọc sơ đồ ghế chạy bao nhiêu lần mỗi request, và con số đó gợi ý gì cho M3?

## Done khi
- [ ] Test trên xanh; không có code Redis/cache nào trong ticket này
- [ ] Mỗi câu Dapper có comment vì sao không dùng EF (C-20); `block`/`unblock` đi qua `Inventory` và tăng `version`
