# M2-10: Quét vé check-in

Mode: pair-tdd
Status: ready
Blocked by: M2-09
Traces to: FR-07, FR-08, FR-25, FR-27, FR-35
Concurrency: #8
Branch: m2-10-check-in-scan

## Mục tiêu
Một vé chỉ cho một người vào, dù hai cổng quét cùng lúc hay quét mười lần liền; mỗi lần quét cho kết quả
xác định, đúng một trong năm loại, để nhân viên biết phải làm gì.

## Phạm vi
**Trong:**
- `POST /api/check-in/scan` (`GateStaff` có phân công cho suất, hoặc `Administrator`), body `showtimeId`,
  `qr`, `gate`. Thứ tự kiểm: quyền (vai trò + phân công) → chữ ký → vé tồn tại → đúng suất → suất chưa hủy →
  ghi. Năm kết quả của `FR-25` với status và `code` đúng như api-contract (`201` hợp lệ,
  `409 TICKET_ALREADY_CHECKED_IN` kèm `firstCheckedInAt` + `firstGate`, `422 TICKET_INVALID`,
  `422 TICKET_WRONG_SHOWTIME` kèm `ticketShowtimeStartsAt`, `409 SHOWTIME_CANCELLED`); thiếu quyền ⇒
  `403 NOT_ASSIGNED` / `FORBIDDEN`.
- Ghi bằng `INSERT … ON CONFLICT (ticket_id) DO NOTHING RETURNING`; 0 dòng ⇒ đọc bản ghi gốc để trả thời điểm +
  cổng **gốc**. Chỉ dựa vào PK của DB, **không** Redis (ADR-0013).
- Mã cho ghế chưa thanh toán không có vé ⇒ `TICKET_INVALID` (`FR-08`).
- `GET /api/manager/showtimes/{showtimeId}/check-ins/count` (chủ suất / `Administrator`):
  `{ showtimeId, checkedIn, ticketsIssued }`.

**Ngoài:**
- Rate limit `scan` dùng chung nhiều instance → M3-04.
- Sinh / xác minh mã QR và phân công cổng → M2-09 (ở đây chỉ dùng).
- Hủy suất → M2-08 (ở đây chỉ trả `SHOWTIME_CANCELLED`).

## Đọc trước
- [`concurrency.md`](../../../docs/design/concurrency.md) — "Bài toán #8".
- [`ADR-0013`](../../../docs/adr/0013-check-in-db-unique-only.md).
- [`api-contract.md`](../../../docs/design/api-contract.md) — "Ticketing" (`scan`, `check-ins/count`).
- [`data-model.md`](../../../docs/design/data-model.md) — `ticketing.check_ins` (PK `ticket_id`, append-only).
- [`business rules`](../../../docs/prd/04-business-rules.md) — BR-10, BR-15, BR-25.

## Test
AI viết các test này trước.

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Scan_SameTicket10Concurrently_OneCheckIn` | `Barrier(10)`: 1 dòng `check_ins`; 1 response `201`, 9 response `409` cùng `firstCheckedInAt` và `firstGate` gốc | kiểm-rồi-chèn và bỏ PK (`UnsafeVariant=CheckThenInsert` + migration test bỏ ràng buộc) ⇒ > 1 dòng |
| `Scan_Valid_201WithSeatLabel` | `201 { ticketId, seatLabel, checkedInAt }` | — |
| `Scan_SecondScan_409WithOriginalTimeAndGate` | thời điểm / cổng của lần đầu, không phải lần hai | trả `201` lần hai |
| `Scan_TamperedSignature_422TicketInvalid` | không chạm DB | không xác minh chữ ký |
| `Scan_TicketOfOtherShowtime_422WrongShowtime` | kèm `ticketShowtimeStartsAt` | bỏ kiểm suất |
| `Scan_CancelledShowtime_409` | `SHOWTIME_CANCELLED`, không ghi `check_ins` | — |
| `Scan_UnpaidSeatCode_422TicketInvalid` | mã hợp lệ chữ ký nhưng không có vé ⇒ `TICKET_INVALID` | — |
| `Scan_GateStaffNotAssigned_403` / `Scan_Customer_403` | `NOT_ASSIGNED` / `FORBIDDEN` | kiểm phân công sau khi ghi |
| `CheckInCount_NeverExceedsTicketsIssued` | `checkedIn <= ticketsIssued`; vai trò / chủ suất đúng | — |

## Câu hỏi review
- Vì sao lần quét thứ hai **phải** cho kết quả khác lần đầu, nên endpoint này không dùng `Idempotency-Key`?
- Vì sao không thêm Redis `SETNX` phía trước, và nó gây lỗi gì khi transaction rollback?
- Suất bị hủy đúng lúc đang quét: vì sao chấp nhận được mà không cần khoá?

## Done khi
- [ ] Test race đã thấy đỏ với biến thể `CheckThenInsert`
- [ ] Mỗi trong năm kết quả `FR-25` có test và đúng `code` của api-contract
- [ ] `check_ins` append-only (không `UPDATE`/`DELETE` cho `app_user`)
