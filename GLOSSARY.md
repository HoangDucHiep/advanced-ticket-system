# Advanced Ticket System — Glossary

Hệ thống đặt vé xem phim theo ghế: người xem chọn ghế của một suất chiếu, giữ có thời hạn,
thanh toán, rồi mang vé (mã QR) tới cổng để vào rạp.

Một file cho cả repo (single-context về mặt triển khai), chia mục theo **bounded context**. Tên
tiếng Anh là tên chuẩn trong code; tên tiếng Việt dùng để trao đổi. Từ trùng tên ở hai context
được ghi kèm context trong ngoặc (hiện chỉ còn `Cancelled`).

Nguồn: [`docs/domain/ubiquitous-language.md`](docs/domain/ubiquitous-language.md) (bản làm việc),
[`docs/domain/context-map.md`](docs/domain/context-map.md),
[`docs/domain/state-machines.md`](docs/domain/state-machines.md). Chốt ở cuối Gate B (2026-11-25).

## Scheduling — Lịch chiếu

**Movie** (Phim):
Một tác phẩm trong danh mục chung do quản trị viên quản lý. Một phim có nhiều suất chiếu.
_Avoid_: film, Title

**Room** (Phòng chiếu):
Một phòng chiếu vật lý, có sơ đồ ghế cố định, thuộc về quản lý rạp đã tạo nó.
_Avoid_: hall, screen

**Room seat** (Ghế của phòng):
Một vị trí ngồi vật lý trong phòng — hàng, số, loại ghế. Không có trạng thái, không bao giờ được bán.
_Avoid_: Seat (khi nói về phòng), chỗ ngồi, ghế chỗ

**Seat type** (Loại ghế):
Nhóm ghế trong một phòng theo vị trí và giá: `Standard` / `Couple` / `VIP`. Mang giá, không mang số lượng.
_Avoid_: tier, hạng vé, cap, SeatClass

**Couple seat type** (Ghế đôi):
Loại ghế cho hai người: tính là 2 ghế khi áp giới hạn số lượng, nhưng sinh ra 1 vé.
_Avoid_: double seat, loveseat, DoubleSeat

**Showtime** (Suất chiếu):
Một phim, một phòng, một khoảng giờ; mỗi suất có trạng thái ghế riêng dù dùng chung sơ đồ phòng.
_Avoid_: screening, buổi chiếu, Schedule (lịch chiếu là danh sách suất)

**Ticket price** (Giá vé):
Giá của một cặp (loại ghế, suất chiếu) — không thuộc về ghế cụ thể, không thuộc về phòng.
_Avoid_: SeatPrice, giá ghế, biểu giá

### Trạng thái của `Showtime`

**NotOnSale** (Chưa mở bán):
Suất đã tạo, ghế đã có, người xem chưa thấy; quản lý rạp vẫn chặn ghế được.

**OnSale** (Đang bán):
Người xem thấy suất và giữ ghế được, cho tới giờ bắt đầu chiếu.

**Cancelled (Showtime)** (Suất đã hủy):
Suất bị quản lý rạp hoặc quản trị viên hủy; kéo theo hủy các đơn đang chờ và hoàn tiền các đơn đã trả.
_Avoid_: nhầm với `Cancelled (Booking)` — khác đơn vị, khác hậu quả

## Inventory — Tồn kho ghế

**Showtime seat** (Ghế suất chiếu):
Một ghế vật lý ở một suất chiếu cụ thể — đơn vị bán thực sự, có trạng thái riêng.
_Avoid_: Seat (khi nói về suất chiếu), chỗ ngồi, Ticket

**Seat hold** (Thông tin giữ):
Thông tin trên một ghế suất chiếu đang giữ: thuộc lần giữ nào và giữ tới lúc nào. Không phải một thực thể riêng.
_Avoid_: reservation, HoldRecord, SeatHold (như tên thực thể)

**Available** (Trống):
Ghế chưa ai dùng; người xem giữ được.
_Avoid_: free, empty

**Held** (Đang giữ):
Ghế đang được giữ tạm cho một đơn, kèm thời điểm hết hạn. Chưa bán, chưa có vé.
_Avoid_: Reserved, Pending, Locked

**Sold** (Đã bán):
Ghế đã được thanh toán và đã có vé. Không bao giờ trở lại `Available`.
_Avoid_: Booked, Confirmed, Purchased

**Blocked** (Bị chặn):
Ghế không bán online (vệ sinh, dành riêng) do quản lý rạp chặn; vẫn tính vào tổng số ghế phòng.
_Avoid_: Reserved, Disabled, Unavailable

### Cách đếm

**Room seat count** (Tổng số ghế phòng):
Số ghế trong sơ đồ phòng, không phụ thuộc trạng thái.
_Avoid_: capacity, sức chứa, TotalSeats

**Available seat count** (Số ghế còn trống):
Số ghế ở trạng thái `Available` — con số người xem thấy ("còn N ghế").
_Avoid_: FreeSeats, Remaining, chỗ trống

**Sellable seat count** (Số ghế bán online):
Tổng số ghế phòng trừ ghế `Blocked` — kích thước sơ đồ người xem thấy, không phải số ghế còn đặt được.
_Avoid_: Capacity, còn chỗ

## Booking — Đặt vé

**Booking** (Đơn đặt vé):
Một lần đặt của một người xem, thuộc đúng một suất chiếu, chứa một hoặc nhiều ghế.
_Avoid_: Order, Purchase, Transaction, đơn hàng, hóa đơn

**Booking line** (Dòng đơn):
Một ghế trong đơn, kèm loại ghế và giá đã chốt tại lúc giữ.
_Avoid_: OrderItem, chi tiết đơn

**Booking total** (Tổng tiền của đơn):
Tổng giá của từng ghế trong đơn.
_Avoid_: TotalPrice, order total

**Hold seats** (Giữ ghế):
Hành động giữ tạm một nhóm ghế cho một người xem: đủ cả nhóm hoặc không ghế nào, hết hạn sau 15 phút.
_Avoid_: reserve, lock, cart, Hold

**Pay** (Thanh toán):
Hành động biến một đơn đang chờ thành đã trả tiền: ghế thành đã bán và vé được cấp cùng lúc.

**Expires at** (Thời điểm hết hạn):
Mốc 15 phút kể từ lúc giữ ghế; quá mốc này đơn không thể được thanh toán, dù đã kịp chuyển sang `Expired` hay chưa.
_Avoid_: deadline, TTL

**Idempotency key** (Định danh yêu cầu):
Định danh do client sinh cho một lần bấm "giữ ghế"; gửi lại cùng định danh cho cùng kết quả với lần đầu, kể cả khi lần đầu bị từ chối.
_Avoid_: request_id, nonce

### Trạng thái của `Booking`

**AwaitingPayment (Booking)** (Chờ thanh toán):
Đơn đã giữ ghế, chưa thanh toán, chưa quá thời điểm hết hạn.
_Avoid_: Pending, Open

**Paid** (Thanh toán thành công):
Đơn đã trả tiền trước thời điểm hết hạn; luôn đã có đủ vé.
_Avoid_: Confirmed, Completed, TicketsIssued, PaymentSucceeded

**Expired** (Hết hạn):
Đơn qua thời điểm hết hạn mà chưa thanh toán; ghế trả về, không có vé, không hồi sinh.
_Avoid_: Timeout, Voided, Cancelled

**Cancelled (Booking)** (Đơn đã hủy):
Đơn bị hủy khi chưa thanh toán — do người xem hủy hoặc do suất chiếu bị hủy.
_Avoid_: Voided, Aborted, Expired

## Payment — Thanh toán

**Payment intent** (Yêu cầu thanh toán):
Yêu cầu thu tiền cho đúng một đơn; thanh toán thất bại thì thử lại bằng chính yêu cầu đó.
_Avoid_: payment, charge, transaction; nhầm với `payment_intent` của cổng thanh toán

**Payment event** (Thông báo thanh toán):
Một thông báo kết quả từ cổng thanh toán (thành công, thất bại, đã hoàn), có định danh riêng và chỉ được xử lý một lần.
_Avoid_: webhook, callback

**Refund** (Hoàn tiền):
Một lần hoàn toàn bộ số đã thu; mỗi yêu cầu thanh toán có nhiều nhất một.
_Avoid_: partial refund

### Trạng thái của `Payment intent`

**Unpaid** (Chưa thu):
Đã tạo yêu cầu thanh toán, chưa có kết quả thành công; thanh toán thất bại vẫn ở đây.
_Avoid_: AwaitingPayment (của `Booking`), Pending, Failed

**Closed** (Đã đóng):
Đơn đã kết thúc (`Expired` / `Cancelled`) khi chưa thu tiền; nếu tiền vẫn về muộn thì phải hoàn.
_Avoid_: Cancelled, Voided

**Captured** (Đã thu):
Tiền đã về và đơn đã `Paid`.
_Avoid_: Paid, Succeeded

**RefundPending** (Chờ hoàn):
Tiền đã thu nhưng phải hoàn; yêu cầu hoàn đã ghi, chưa có xác nhận từ cổng thanh toán.
_Avoid_: Refunding, PendingRefund

**Refunded** (Đã hoàn):
Đã hoàn toàn bộ.
_Avoid_: Reversed, Chargeback

## Ticketing — Vé và check-in

**Ticket** (Vé):
Một mã QR gắn với đúng một ghế suất chiếu; chỉ tồn tại sau khi đơn đã thanh toán.
_Avoid_: coupon, vé điện tử, Seat

**Check-in**:
Ghi nhận một vé đã được quét tại cổng; mỗi vé chỉ check-in một lần.
_Avoid_: scan, redeem, validate

**Gate** (Cổng):
Nơi quét vé, được ghi lại trong lần check-in. Không phải đối tượng được phân công.
_Avoid_: Door, Entrance

**Gate assignment** (Phân công cổng):
Một nhân viên cổng được phân công trực một suất chiếu; quyết định ai được check-in vé của suất nào.
_Avoid_: Shift, Roster

### Trạng thái của `Ticket`

**Issued** (Đã cấp):
Vé đã cấp, chưa check-in.
_Avoid_: Active, Valid

**CheckedIn** (Đã check-in):
Vé đã được quét thành công tại cổng.
_Avoid_: Used, Redeemed

## Identity & Access — Danh tính và phân quyền

**User** (Người dùng):
Một tài khoản đăng nhập, mang đúng một vai trò.
_Avoid_: account

**Customer** (Người xem):
Người xem phim đã đăng nhập để đặt vé và quản lý đơn, vé của mình.
_Avoid_: user, khách, Viewer

**Theater manager** (Quản lý rạp):
Người quản lý phòng chiếu, lịch chiếu và sơ đồ ghế; chỉ sửa được lịch chiếu do chính mình tạo.
_Avoid_: Manager, owner, admin, staff

**Gate staff** (Nhân viên cổng):
Người trực cổng kiểm tra vé; chỉ ghi nhận check-in.
_Avoid_: Staff, employee, doorman

**Administrator** (Quản trị viên):
Người quản lý toàn hệ thống, cấp và thu quyền; không phải quản lý của rạp cụ thể nào.
_Avoid_: admin, superadmin, root

## Từ không dùng ở bất kỳ context nào

- `Order`, `Purchase`, `đơn hàng` — dùng `Booking`.
- `tier`, `hạng vé`, `cap` — mô hình tier-based đã bị thay (ADR-0003); dùng `Seat type`.
- `capacity`, `sức chứa` — một suất không có sức chứa cấu hình; dùng `Room seat count`.
- `Seat` đứng một mình — luôn nói rõ `Room seat` hay `Showtime seat`.
