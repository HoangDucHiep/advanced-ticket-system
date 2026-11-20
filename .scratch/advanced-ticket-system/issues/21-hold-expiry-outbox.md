# 21: Worker hết hạn chỗ giữ ghế và dispatcher outbox

Mode: you-implement-i-review
Gate: D
Layer: code
Blocked by: 18, 20
Traces to: FR-04, FR-06, FR-18, FR-19, FR-24
Concurrency problem: #4

## Vì sao có ticket này

Chỗ giữ ghế không tự hết hạn. Cần một worker quét và giải phóng. Nhưng worker chạy **đồng
thời** với người mua — và cả hai đều đụng vào **cùng một dòng ghế**. Đây là bài toán
concurrency số 4.

Nó cũng chứa một bài toàn nhỏ nhưng thú vị: **quy trình chết giữa chừng**. Nếu worker đã
chuyển đơn sang `Expired` nhưng chưa kịp trả lại ghế, thì những ghế đó bị giữ mà không ai
giữ — tài nguyên rò rỉ. Và nếu chạy lại, nó phải không trả lại hai lần.

Đây cũng là nơi `FOR UPDATE SKIP LOCKED` tỏa sáng: nhiều instance dispatcher chạy song song
mà không xử lý trùng message.

## Bạn tạo ra

- **Worker hết hạn chỗ giữ ghế**:
  - Quét đơn `Pending` có `hold_expires_at < now()`.
  - Chuyển đơn sang `Expired`, **có điều kiện theo trạng thái hiện tại**.
  - Chỉ khi transition thành công mới trả lại ghế.
  - Thứ tự thao tác được chốt ở T12 — và nó phải **an toàn khi process chết giữa chừng**.
  - **Trả lại ghế theo đúng số ghế trong đơn**, không phải theo một số đếm tổng.
- **Dispatcher outbox**:
  - Quét message chưa xử lý, theo thứ tự tạo.
  - Giao (stub), rồi đánh dấu đã xử lý.
  - Retry với số lần thử giới hạn.
  - Dùng `FOR UPDATE SKIP LOCKED` để nhiều instance không xử lý trùng message.
- Cấu hình: thời gian giữ ghế và chu kỳ quét lấy từ config, không hardcode.
- Test.

## AI làm gì

AI **không viết code**. Khi review, AI hỏi:

- **"Process chết giữa lúc chuyển đơn và lúc trả ghế thì sao?"** Đây là câu hỏi trung
  tâm của ticket. Nếu trả ghế là một lệnh riêng sau transition, thì có một cửa sổ mất ghế.
  Lời giải phải là: transition và trả ghế **cùng transaction**, hoặc transition ghi outbox
  rồi dispatcher trả ghế.
- "Worker chạy hai instance cùng lúc thì có trả ghế hai lần không? Cơ chế nào chặn?"
- "Câu trả ghế có điều kiện trạng thái ghế không (`Held` → `Available`)? Nếu thiếu, chuyện gì
  xảy ra khi người mua đã thanh toán trong khoảnh khắc đó?"
- "Dispatcher dùng `SKIP LOCKED` thì khi nào một message **không bao giờ** được xử lý?"
- "Số lần retry thử hạn chế là bao nhiêu, và message hết hạn được ghi lại thế nào để không mất
  dấu?"

## Tiêu chí hoàn thành

- [ ] Đơn chưa thanh toán quá hạn → `Expired`, **đúng số ghế** được trả lại
- [ ] Worker chạy hai instance cùng lúc → **không** trả ghế hai lần
- [ ] Process chết giữa transition và trả ghế → hệ thống **tự phục hồi**, không rò rỉ ghế
- [ ] Không có ghế nào ở trạng thái trung gian vô thời hạn
- [ ] Dispatcher không xử lý trùng message giữa hai instance (`SKIP LOCKED`)
- [ ] Thời gian giữ ghế và chu kỳ quét lấy từ config
- [ ] Có test cho race giữa worker expire và người mua mới
