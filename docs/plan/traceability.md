# Ma trận truy vết

Mỗi `FR-nn` một dòng. Đây là **nơi duy nhất** nối requirement (PRD) với luật nghiệp vụ, journey, domain, design và ticket; các tài liệu khác không lặp lại phép nối này mà chỉ trỏ về đây. Requirement nào chưa có ticket thì chưa đáp ứng xong.

| FR | Tóm tắt | BR | J | Domain | Design | Ticket |
|---|---|---|---|---|---|---|
| FR-01 | Một ghế không thuộc hai người | BR-01 | J-02, J-03, J-04 | `Showtime seat` INV-01 | concurrency.md #1, ADR-0002 | |
| FR-02 | Giữ đủ n ghế hoặc không giữ | BR-02 | J-03 | `Hold seats`; R3, ADR-0005 | concurrency.md #2 | |
| FR-03 | Không kẹt khi tranh chồng | BR-22 | J-03 | Thứ tự khoá: quota → ghế tăng dần (BR-22) | concurrency.md #3 | |
| FR-04 | Giữ ghế có hạn, tự giải phóng | BR-03 | J-04 | `Booking.expires_at`; `Booking → Expired` | concurrency.md #4, ADR-0015 | |
| FR-05 | Vé chỉ cấp khi đã thanh toán | BR-09 | J-02, J-04 | `Ticket` INV-02 | concurrency.md #7 | |
| FR-06 | Đơn hết hạn không hồi sinh | BR-04, BR-05 | J-04 | `Booking` INV-05, ADR-0006 | concurrency.md #4, ADR-0006 | |
| FR-07 | Vé check-in đúng một lần | BR-10 | J-05 | `Ticket` `Issued → CheckedIn`; `Check-in` | concurrency.md #8, ADR-0013 | |
| FR-08 | Vé chưa trả tiền không check-in | BR-09 | J-05 | `Ticket` INV-02 | api-contract.md (Ticketing) | |
| FR-09 | Giới hạn 6/đơn, 10/người/suất | BR-06, BR-07, BR-08 | J-04 | `Booking` INV-03, INV-10; ADR-0007 | concurrency.md #13 | |
| FR-10 | Không thao tác đơn/vé người khác | BR-14 | J-02 | `Booking` INV-09; `Ticket` INV-07 | api-contract.md | |
| FR-11 | Xem phim đang chiếu | BR-29 | J-01 | `Movie` INV-11 | api-contract.md; cache 30s | |
| FR-12 | Xem lịch chiếu của phim | BR-29 | J-01 | `Showtime`; `Ticket price` | api-contract.md | |
| FR-13 | Xem sơ đồ ghế | BR-23 | J-01, J-02, J-03 | `Showtime seat`; `Available seat count` | concurrency.md #9, #10, ADR-0012 | |
| FR-14 | Giữ ghế + đếm ngược | BR-03 | J-02 | `Hold seats`; `Booking.expires_at` | concurrency.md #2 | |
| FR-15 | Xác nhận lại ghế lúc giữ | BR-01, BR-23 | J-02, J-04 | `Showtime seat` INV-01 | concurrency.md #2 | |
| FR-16 | Thanh toán và nhận vé | BR-09, BR-12 | J-02 | `Booking → Paid`; `Booking line`; `Ticket` | api-contract.md (payment, webhook) | |
| FR-17 | Xem vé đã mua, lấy mã | BR-30 | J-02 | `Ticket` INV-06 | api-contract.md (Ticketing) | |
| FR-18 | Huỷ đơn chờ, trả ghế | BR-21 | J-04 | `Booking → Cancelled` | api-contract.md; concurrency.md #4 | |
| FR-19 | Nhắc sắp hết hạn giữ ghế | BR-24 | J-02 | `Booking.expires_at` | concurrency.md (outbox) | |
| FR-20 | Quản lý phòng và sơ đồ ghế | BR-11, BR-32 | J-02, J-06 | `Room`; `Room seat`; `Seat type` | data-model.md (scheduling) | |
| FR-21 | Lên lịch chiếu, đặt giá | BR-12, BR-20, BR-32 | J-03, J-06 | `Showtime`; `Ticket price`; BR-20 | data-model.md (exclusion constraint) | |
| FR-22 | Mở bán suất chiếu | BR-13 | J-03, J-06 | `Showtime` `NotOnSale → OnSale` | api-contract.md | |
| FR-23 | Xem tình trạng bán | BR-11 | J-06 | R2 — bốn con số tình trạng bán | api-contract.md (Inventory) | |
| FR-24 | Hủy suất, huỷ đơn, hoàn tiền | BR-05, BR-30 | J-04, J-05, J-06 | `Showtime` `OnSale → Cancelled`; `Payment intent → RefundPending` | concurrency.md #14, ADR-0006 | |
| FR-25 | Quét vé ra kết quả xác định | BR-10 | J-05 | Điều kiện tiên quyết check-in | api-contract.md (Ticketing) | |
| FR-26 | đã loại — NR-15 | — | — | — | — | — |
| FR-27 | Đếm người đã vào | BR-25 | J-05 | `Check-in`; `Ticket` INV-04 | api-contract.md (Ticketing) | |
| FR-28 | Quản lý phim và lịch mọi rạp | BR-16, BR-32 | J-07 | `Administrator`; `Movie`; `Showtime` INV-04; BR-32 | api-contract.md (admin) | |
| FR-29 | Quản lý người dùng và quyền | BR-26 | J-07 | `User` INV-01..03 | ADR-0014 | |
| FR-30 | Giám sát sức khỏe hệ thống | BR-28 | J-07 | (không có khái niệm domain) | concurrency.md #11; observability | |
| FR-31 | Truy vết lịch sử giao dịch | BR-27 | J-07 | `Booking` INV-07; `Payment event` | api-contract.md (admin history) | |
| FR-32 | Chỉ chủ suất được sửa/hủy | BR-16 | J-06 | `Showtime` INV-04 | api-contract.md | |
| FR-33 | Chỉ nhân viên cổng check-in | BR-15 | J-05 | `Identity` INV-06 | api-contract.md (role policy) | |
| FR-34 | Suất chưa mở bán ẩn, không đặt | BR-13 | J-01, J-03 | `Showtime` INV-02 | api-contract.md | |
| FR-35 | Chỉ quét vé được phân công | BR-15 | J-05 | `Gate assignment` INV-08 | api-contract.md (Ticketing) | |
| FR-36 | Từ chối kèm lý do, bước tiếp | BR-19 | J-03, J-04 | Bảng transitions bị từ chối | ADR-0016; api-contract.md (problem+json) | |
| FR-37 | Yêu cầu đặt lặp tạo một đơn | BR-18 | J-04 | `Booking` INV-08; `Idempotency key` | concurrency.md #5, ADR-0011 | |
| FR-38 | Thông báo thanh toán lặp, một vé | BR-17 | J-02, J-07 | `Payment intent` INV-01 | concurrency.md #7 | |
| FR-39 | Hoàn tiền khi trả mà không có vé | BR-05 | J-04, J-06 | `Payment intent`; `Refund` INV-02 | concurrency.md #7 | |
| FR-40 | Phân công nhân viên cổng | BR-15 | J-06 | `Gate assignment` INV-09 | api-contract.md (Ticketing) | |
| FR-41 | Không đặt suất đã bắt đầu | BR-31 | J-04 | `Showtime` INV-02; BR-31 | concurrency.md #2 | |
