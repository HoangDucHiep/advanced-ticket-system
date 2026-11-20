# 24: Check-in — QR ký HMAC, offline, chống replay

Mode: pair-tdd
Gate: D
Layer: code
Blocked by: 20
Traces to: FR-07, FR-08, FR-10, FR-17, FR-25, FR-26, FR-27, FR-33, FR-35
Concurrency problem: #8

## Vì sao có ticket này

Ở cổng rạp, máy quét có thể **mất mạng**. Nếu mỗi lần quét phải gọi API, cổng sập
mạng là cổng sập. Vậy nên vé phải **tự xác thực được** — chứa trong nó đủ thông tin để
kiểm tra chữ ký mà không cần hỏi ai.

Nhưng "tự xác thực" mở ra câu hỏi hay: **nếu không gọi DB thì làm sao biết vé đã quét
chưa?** Đó là bài toán exactly-once với một nguồn sự thật ở DB và một lớp fast-reject ở
Redis.

Câu trả lời đúng, và câu bạn nên nói được trong phỏng vấn: *"Redis là tối ưu, unique
constraint là bảo đảm."*

Với mô hình seat-based, vé là **một mã QR cho một ghế cụ thể**. Nên payload phải mang đủ
thông tin để nhân viên cổng biết người đó ngồi ở **hàng nào, số ghế nào**, thuộc **suất
chiếu nào** — và đó cũng là lý do vé phải là **một mã riêng cho mỗi ghế**, không phải một
mã chung cho cả đơn.

## Bạn tạo ra

- Schema `checkin.ticket_tokens`, `checkin.check_ins`.
- **Định dạng payload QR** — chuỗi ngắn, không phải JSON:
  ```
  v1|ticketId|showtimeId|seatLabel|issuedAtUnix|nonce
  ```
  kèm chữ ký `HMAC-SHA256` trên payload, mã hoá base64url. Toàn bộ là nội dung render thành
  QR. `seatLabel` là nhãn ghế hiển thị (ví dụ `D7`) để nhân viên cổng đọc được ngay.
- **Ký** khi cấp vé (sau khi thanh toán).
- **Quét trực tuyến** `POST /api/check-in/scan`:
  1. Tính lại chữ ký, so sánh **constant time**. Sai → `400`.
  2. Kiểm tra hạn dùng → `410`.
  3. Redis `SETNX checkin:{ticketId}` — chặn nhanh vé đã quét.
  4. Insert vào `checkin.check_ins`; `UNIQUE(ticket_token_id)` là bảo đảm.
  5. Khi insert đụng unique → đọc bản ghi cũ và **trả lại**, kèm `checked_in_at` gốc.
- **Xác thực offline** `POST /api/check-in/offline/verify`: chỉ kiểm chữ ký và hạn dùng,
  **không ghi DB**. Tài liệu nói rõ offline **không** phát hiện được replay nếu không có
    trạng thái cục bộ, và mô tả cách thiết bị đồng bộ xử lý sau đó.
- `GET /api/bookings/{id}/tickets` trả payload QR của các vé trong đơn đã thanh toán — **mỗi
  vé một payload riêng**, kèm ghế của nó.
- Phân quyền: chỉ `CheckInStaff` và `Admin` được quét; và chỉ quét được vé thuộc suất chiếu
  được phân công trực (FR-35).
- Rate limit endpoint quét theo staff (cổng là điểm nóng thật).
- Test.

## AI làm ghi

AI viết **test đỏ** theo thứ tự:

1. QR hợp lệ → xác thực được, tạo đúng 1 check-in.
2. Payload bị sửa bất kỳ field nào → bị từ chối.
3. QR hết hạn → `410`.
4. Quét cùng vé 10 lần → 1 bản ghi; các lần sau trả thời điểm gốc.
5. Offline verify: QR hợp lệ → hợp lệ, **không** ghi DB; QR bị sửa → bị từ chối.
6. QR ký bằng secret khác → bị từ chối.
7. Vé của suất chiếu **không được phân công** trực → bị từ chối.
8. Vé **chưa thanh toán** → bị từ chối, không ghi check-in.

AI dừng lại sau mỗi test đỏ.

Khi review code của bạn, AI hỏi:

- "So sánh chữ ký có constant-time không, hay dùng `==` trên chuỗi thô?"
- "Secret ký lấy từ đâu — có nằm trong DB không? Có được rotate không?"
- "Payload có chứa dữ liệu cá nhân không? Tại sao?"
- "Nếu Redis không có sẵn (chưa đồng bộ, bị flush), hệ thống còn check-in được không?" — đây
  là câu hỏi về **degradation**, và câu trả lời phải là "Redis chỉ là fast-reject, DB vẫn
  đúng".
- "Endpoint offline ghi gì vào log, và log đó có đủ để đồng bộ sau không?"

## Tiêu chí hoàn thành

- [ ] QR hợp lệ xác thực và tạo đúng 1 check-in
- [ ] Payload bị sửa bị từ chối
- [ ] QR hết hạn bị từ chối với `410`
- [ ] Quét 10 lần → **1** bản ghi; lần sau trả `checked_in_at` gốc
- [ ] Endpoint offline xác thực được mà **không ghi DB**, và từ chối được QR bị sửa
- [ ] QR ký bằng secret khác bị từ chối
- [ ] So sánh chữ ký constant-time
- [ ] Chỉ staff được quét; endpoint bị rate limit
- [ ] Payload không chứa PII, và có `showtimeId` + `seatLabel` để cổng đọc được ghế
- [ ] Vé của suất chiếu không được phân công trực → bị từ chối
- [ ] Vé chưa thanh toán → bị từ chối, không ghi check-in
- [ ] Tài liệu nói rõ giới hạn của chế độ offline và cách đồng bộ
