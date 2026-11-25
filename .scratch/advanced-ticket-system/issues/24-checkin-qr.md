# 24: Check-in — QR ký HMAC, phân công cổng, chống quét lại

Mode: pair-tdd
Gate: D
Layer: code
Blocked by: 20
Traces to: FR-07, FR-08, FR-10, FR-17, FR-25, FR-27, FR-33, FR-35, FR-40
Concurrency problem: #8

> **Đồng bộ với design (2026-11-25, Gate C đóng).** Khi ticket này mâu thuẫn với `docs/design/`,
> **design thắng**. **Check-in offline đã bị loại** (`NR-15`, `FR-26`) — bỏ mọi phần offline bên dưới. **Không dùng
> Redis `SETNX`** (ADR-0013): bảo đảm duy nhất là `PRIMARY KEY (ticket_id)` ở `ticketing.check_ins` +
> `ON CONFLICT`. Bảng: `ticketing.tickets` (không lưu mã QR, không cột trạng thái), `ticketing.check_ins`,
> `ticketing.gate_assignments`. Định dạng QR `v1.<kid>.<payload>.<signature>`, **không có hạn dùng**
> (không `410`): `api-contract.md` mục Ticketing. Năm kết quả quét (`FR-25`): hợp lệ `201`, đã dùng `409`,
> không hợp lệ `422`, suất khác `422`, suất đã hủy `409`; không được phân công `403`. Vai trò:
> `GateStaff`, `Administrator`. Thêm: phân công cổng (`FR-40`), đếm lượt vào (`FR-27`).

## Vì sao có ticket này

Ở cổng rạp, hai nhân viên có thể quét **cùng một vé cùng lúc**, hoặc khách đưa lại vé đã dùng.
Vé phải được **ký** để không ai tự tạo được, và mỗi vé chỉ được ghi nhận check-in **một lần**.

Câu bạn nên nói được trong phỏng vấn: *"Ràng buộc ở DB là bảo đảm. Tôi đã cân nhắc thêm Redis để
chặn sớm và bỏ, vì nó chỉ thêm một chỗ có thể lệch mà không thêm bảo đảm nào"* (ADR-0013).

Với mô hình seat-based, vé là **một mã QR cho một ghế cụ thể**. Nên payload phải mang đủ
thông tin để nhân viên cổng biết người đó ngồi ở **hàng nào, số ghế nào**, thuộc **suất
chiếu nào** — và đó cũng là lý do vé phải là **một mã riêng cho mỗi ghế**, không phải một
mã chung cho cả đơn.

## Bạn tạo ra

- Schema `ticketing.tickets`, `ticketing.check_ins`, `ticketing.gate_assignments` (`data-model.md`).
- **Định dạng mã QR** (`api-contract.md` mục Ticketing):
  ```
  v1.<kid>.<base64url(ticketId|showtimeId|seatLabel|issuedAtUnix)>.<base64url(HMAC-SHA256)>
  ```
  `kid` cho phép xoay khoá ký. `seatLabel` (ví dụ `D7`) để nhân viên cổng đọc được ngay. Mã **không**
  lưu trong DB — tính lại từ `ticketing.tickets`.
- **Cấp vé**: handler của event `BookingPaid`, trong transaction thanh toán (T20).
- **Quét** `POST /api/check-in/scan` `{ showtimeId, qr, gate }`:
  1. Quyền: vai trò `GateStaff` có `Gate assignment` cho suất, hoặc `Administrator` — sai ⇒ `403`.
  2. Tính lại chữ ký, so sánh **constant time**; sai, hoặc không có vé ⇒ `422 TICKET_INVALID`.
  3. Vé khác suất đang quét ⇒ `422 TICKET_WRONG_SHOWTIME`; suất đã hủy ⇒ `409 SHOWTIME_CANCELLED`.
  4. `INSERT INTO ticketing.check_ins … ON CONFLICT (ticket_id) DO NOTHING RETURNING` — PK là bảo đảm.
  5. Không chèn được ⇒ đọc dòng có sẵn, trả `409 TICKET_ALREADY_CHECKED_IN` kèm thời điểm + cổng gốc.
- `GET /api/tickets`, `GET /api/bookings/{id}/tickets` — **mỗi vé một mã riêng**, kèm ghế.
- Phân công cổng `POST/DELETE /api/manager/showtimes/{id}/gate-assignments` (`FR-40`) và đếm lượt vào
  `GET …/check-ins/count` (`FR-27`).
- Rate limit endpoint quét theo staff (cổng là điểm nóng thật).
- Test.

## AI làm ghi

AI viết **test đỏ** theo thứ tự:

1. QR hợp lệ → xác thực được, tạo đúng 1 check-in.
2. Payload bị sửa bất kỳ field nào → bị từ chối.
3. Vé của suất **đã hủy** → `409 SHOWTIME_CANCELLED`, không ghi check-in.
4. Quét cùng vé 10 lần → 1 bản ghi; các lần sau trả thời điểm gốc.
5. Vé của suất khác suất đang quét → `422 TICKET_WRONG_SHOWTIME`.
6. QR ký bằng secret khác → bị từ chối.
7. Vé của suất chiếu **không được phân công** trực → bị từ chối.
8. Mã ký đúng cho một ghế **chưa có vé** (đơn chưa thanh toán) → `422 TICKET_INVALID`, không ghi check-in.

AI dừng lại sau mỗi test đỏ.

Khi review code của bạn, AI hỏi:

- "So sánh chữ ký có constant-time không, hay dùng `==` trên chuỗi thô?"
- "Secret ký lấy từ đâu — có nằm trong DB không? Có được rotate không?"
- "Payload có chứa dữ liệu cá nhân không? Tại sao?"
- "Hai lượt quét đồng thời: điều gì bảo đảm chỉ một dòng `check_ins`? Lượt thua lấy thời điểm gốc ở
  đâu?"

## Tiêu chí hoàn thành

- [ ] QR hợp lệ xác thực và tạo đúng 1 check-in
- [ ] Payload bị sửa bị từ chối
- [ ] Vé của suất đã hủy bị từ chối với `409`
- [ ] Quét 10 lần → **1** bản ghi; lần sau trả `checked_in_at` gốc
- [ ] Vé của suất khác bị từ chối với `422`
- [ ] QR ký bằng secret khác bị từ chối
- [ ] So sánh chữ ký constant-time
- [ ] Chỉ staff được quét; endpoint bị rate limit
- [ ] Payload không chứa PII, và có `showtimeId` + `seatLabel` để cổng đọc được ghế
- [ ] Vé của suất chiếu không được phân công trực → bị từ chối
- [ ] Mã cho ghế chưa có vé → `422 TICKET_INVALID`, không ghi check-in
- [ ] Phân công / gỡ phân công cổng hoạt động; chỉ chủ suất / quản trị viên làm được (`FR-40`)
- [ ] Đếm lượt check-in của suất ≤ số vé đã cấp (`FR-27`)
