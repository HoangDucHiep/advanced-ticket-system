# 27: Frontend người xem tối thiểu

Mode: you-implement-i-review
Gate: D
Layer: code
Blocked by: 22
Traces to: —

> **Nguồn gốc (2026-11-25, AI quyết định).** PRD ngầm định có giao diện web cho người xem (`NR-06`,
> `NR-14`) nhưng Gate C không có chỗ cho nó. Bố cục repo `backend/` + `frontend/` chốt ở
> `docs/design/architecture.md` D2. **Stack frontend chốt khi mở ticket này**. Hiện nghiêng về
> React + Vite + TypeScript. Đây là ticket **cắt đầu tiên** nếu trễ.

## Vì sao có ticket này

Giúp demo hành trình người xem (`J-01`) trên trình duyệt thay vì qua OpenAPI. Ticket này không
chứng minh điều gì về concurrency. Giá trị của nó là làm cho phần backend nhìn thấy được.

## Bạn tạo ra

- `frontend/` — SPA chỉ gọi API theo `docs/design/api-contract.md`: xem suất → sơ đồ ghế → giữ ghế
  (gửi `Idempotency-Key`) → thanh toán bằng Stripe.js → xem vé QR.
- Xử lý đúng các mã lỗi: `409 SEATS_UNAVAILABLE` (đánh dấu ghế đã có người, cho chọn lại), `422`, `401` + refresh tuần tự (ADR-0014).
- Backend thêm CORS cho origin của frontend.
- `.github/workflows/frontend.yml` — chỉ chạy khi `frontend/**` đổi.

## AI làm gì

Review code bạn viết. Riêng việc chọn stack (mở ticket) thì AI quyết, có ghi lý do.

## Tiêu chí hoàn thành

- [ ] Đi hết `J-01` trên trình duyệt với Stripe test mode
- [ ] Bấm "giữ ghế" hai lần liên tiếp chỉ tạo **một** đơn (cùng `Idempotency-Key`)
- [ ] `409` hiển thị đúng những ghế đã có người
- [ ] CI frontend xanh
