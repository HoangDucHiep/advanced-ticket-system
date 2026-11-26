# M2-09: Vé QR ký HMAC + phân công cổng

Mode: pair-tdd
Status: ready
Blocked by: M1-05
Traces to: FR-17, FR-40
Concurrency: —
Branch: m2-09-qr-tickets-gate-assignments

## Mục tiêu
Người xem lấy được vé có mã QR không giả mạo được; quản lý rạp phân công ai được quét vé của suất nào.
Hai thứ này là đầu vào để M2-10 quét vé.

## Phạm vi
**Trong:**
- Định dạng mã QR `v1.<kid>.<payload>.<signature>`, HMAC-SHA256, khoá cấu hình qua Options + `ValidateOnStart`
  (không trong DB, không trong repo); `kid` cho xoay khoá — khoá cũ giữ ở chế độ chỉ-xác-minh. Mã **tính lại**
  từ `ticketing.tickets`, không lưu. Component tạo + xác minh mã nằm trong `Ticketing`, so chữ ký bằng
  `CryptographicOperations.FixedTimeEquals`.
- `GET /api/tickets` (vé của người gọi, phân trang, kèm `showtime.status`, `qr`, `checkedInAt`) và
  `GET /api/bookings/{bookingId}/tickets` (chủ đơn; không có vé / không phải chủ ⇒ `404`).
- `POST /api/manager/showtimes/{showtimeId}/gate-assignments` và `DELETE …/gate-assignments/{staffId}` (chủ
  suất / `Administrator`): `201` / `204`; `404 SHOWTIME_NOT_FOUND` (kể cả không phải chủ),
  `409 ALREADY_ASSIGNED`, `422 NOT_GATE_STAFF` (người được phân công phải có vai trò `GateStaff`).
- `checkedInAt` đọc từ `ticketing.check_ins` — migration tạo bảng thuộc ticket này (ở đây chỉ đọc; ghi ở
  M2-10); chưa có dòng ⇒ `null`.

**Ngoài:**
- Quét vé, các kết quả `FR-25`, `GET …/check-ins/count` → M2-10.
- Cấp vé khi `Paid` (đã có ở M1-05); rate limit `scan` → M3-04.
- Hiển thị "suất đã hủy — tiền được hoàn" là việc client (M4-01); API chỉ trả `showtime.status`.

## Đọc trước
- [`api-contract.md`](../../../docs/design/api-contract.md) — mục "Ticketing" (định dạng mã QR, `GET /api/tickets`, gate assignments).
- [`data-model.md`](../../../docs/design/data-model.md) — `ticketing.tickets` (không có cột trạng thái, không lưu mã), `ticketing.gate_assignments`.
- [`coding-conventions.md`](../../../docs/design/coding-conventions.md) — C-23, C-25.
- [`aggregates`](../../../docs/domain/aggregates) — `Ticket`, `Gate assignment` (INV-07..INV-09).

## Test
AI viết các test này trước.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Qr_RoundTrip_VerifiesAndCarriesTicketFields` | mã sinh ra xác minh được và giải ra `ticketId`, `showtimeId`, `seatLabel`, `issuedAt` | — |
| `Qr_TamperedPayloadOrSignature_Rejected` | sửa 1 ký tự payload / chữ ký ⇒ không hợp lệ | không xác minh chữ ký |
| `Qr_RotatedKey_OldKidStillVerifies_NewTicketsUseNewKid` | tài liệu xoay khoá: vé cũ quét được, vé mới ký bằng `kid` mới | khoá cũ bị bỏ ngay |
| `Qr_UnknownKidOrBadFormat_RejectedWithoutThrowing` | `v2.…`, thiếu phần, base64 hỏng ⇒ kết quả "không hợp lệ", không exception | parse ném lỗi |
| `GetTickets_OnlyOwnTickets` / `GetBookingTickets_OtherCustomer_404` | `FR-10`, `BR-30` | thiếu lọc `customer_id` |
| `GetTickets_CancelledShowtime_ShowsStatusCancelled` | `showtime.status = "Cancelled"` | — |
| `GateAssignment_Create_Duplicate_NotGateStaff_NotOwner` | `201`, `409 ALREADY_ASSIGNED`, `422 NOT_GATE_STAFF`, `404` | — |
| `GateAssignment_Delete_RemovesRow` | `204`, quét sau đó (M2-10) bị `403 NOT_ASSIGNED` | — |

## Câu hỏi review
- Vì sao không lưu mã QR trong DB và không đặt hạn dùng trong mã?
- Vì sao dùng `kid` thay vì một khoá duy nhất, và xoay khoá an toàn theo thứ tự nào?
- Vì sao so chữ ký phải constant-time dù kẻ tấn công chỉ thấy `422`?

## Done khi
- [ ] Khoá ký chỉ đến từ cấu hình bí mật; không có trong diff (C-25)
- [ ] Không có chuỗi QR nào xuất hiện trong log (C-22)
- [ ] Mọi test trên xanh
