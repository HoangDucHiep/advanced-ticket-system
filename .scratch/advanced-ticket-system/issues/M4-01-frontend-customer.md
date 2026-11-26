# M4-01: Frontend người xem

Mode: you-implement-i-review
Status: ready
Blocked by: M2-11
Traces to: FR-11, FR-12, FR-13, FR-14, FR-16, FR-17, FR-36, FR-37
Concurrency: —
Branch: m4-01-frontend-customer

## Mục tiêu
Đi hết hành trình J-01/J-02 trên trình duyệt: chọn suất → chọn ghế → giữ → thanh toán Stripe → thấy vé QR. Cho
người xem README thấy sản phẩm chạy thật, và kiểm chứng `api-contract.md` từ phía client.

## Phạm vi
**Trong:**
- Thư mục `frontend/` ở gốc repo, tự chứa toolchain ([`architecture.md`](../../../docs/design/architecture.md) D2, mục "Bố cục repo").
- **Stack — quyết định của AI, xác nhận lúc mở ticket:** React + Vite + TypeScript. *Vì sao:* SPA thuần gọi HTTP đúng
  với "frontend chỉ là client của `api-contract.md`" (D2); Vite build nhanh, không kéo thêm framework SSR; TypeScript
  bắt lệch shape JSON sớm; Stripe.js có tích hợp React sẵn. *Vì sao không:* Next.js (SSR không cần, thêm một server);
  Blazor (trộn UI vào .NET, D2 đã loại). Người học đổi được nếu có lý do — ghi lại ở đầu PR.
- Luồng: danh sách phim → suất (`GET /api/movies`, `/api/movies/{id}/showtimes`) → sơ đồ ghế (`GET /api/showtimes/{id}/seats`)
  → giữ ghế `POST /api/showtimes/{id}/bookings` → thanh toán (`POST /api/bookings/{id}/payment`, Stripe.js với `clientSecret`)
  → poll `GET /api/bookings/{id}` → vé QR (`GET /api/bookings/{id}/tickets`).
- **`Idempotency-Key` sinh phía client**, một lần cho mỗi ý định giữ ghế, **dùng lại khi retry**; đổi ghế ⇒ key mới
  (hợp đồng ở [`api-contract.md`](../../../docs/design/api-contract.md) mục "Idempotency contract").
- Xử lý lỗi theo `code` + `action`: `409 SEATS_UNAVAILABLE` ⇒ đánh dấu các ghế `unavailableSeatIds`, cho chọn lại;
  `422` (`BOOKING_TOO_LARGE`, `CUSTOMER_SEAT_LIMIT`, `INVALID_SEATS`); `429`/`503` ⇒ chờ `Retry-After` rồi gửi lại cùng key;
  đếm ngược tới `expiresAt`.
- Auth: đăng ký / đăng nhập, access token 15 phút, **refresh tuần tự hoá** (một promise dùng chung — hai refresh đồng thời
  sẽ bị coi là dùng lại và đăng xuất, ADR-0014); `401` thì refresh một lần rồi retry request, refresh `401` ⇒ về trang đăng nhập.
- Backend thêm **CORS** cho origin frontend (cấu hình qua Options, không hard-code).
- `.github/workflows/frontend.yml` chạy **chỉ** khi `frontend/**` đổi (lint + type-check + build + test).

**Ngoài:**
- Giao diện quản lý rạp / admin / nhân viên cổng (J-05..J-07): không làm.
- Quét QR ở cổng: không (đã có `POST /api/check-in/scan`, dùng bằng API).
- Trang giám sát: Grafana (M3-06).
- Chạy frontend trong Docker Compose: tuỳ chọn, không bắt buộc.

## Đọc trước
- [`api-contract.md`](../../../docs/design/api-contract.md) — định dạng lỗi + bảng mã lỗi toàn cục, "Idempotency contract", Identity, Scheduling (danh sách phim, suất), Inventory (sơ đồ ghế), Booking, Payment, Ticketing.
- [`ADR-0014`](../../../docs/adr/0014-refresh-token-rotation-strict-reuse.md).
- [`03-user-journeys.md`](../../../docs/prd/03-user-journeys.md) — J-01, J-02, J-04.
- [`architecture.md`](../../../docs/design/architecture.md) — D2 (bố cục repo).

## Test
Gợi ý — người học viết (unit/component + một E2E tay hoặc Playwright):

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `HoldSeats_RetryAfterNetworkError_ReusesSameIdempotencyKey` | retry dùng lại key; chọn ghế khác thì sinh key mới | mỗi retry một key mới ⇒ có thể giữ hai bộ ghế |
| `HoldSeats_409SeatsUnavailable_MarksSeatsAndAllowsRepick` | ghế trong `unavailableSeatIds` bị đánh dấu, các ghế còn lại giữ nguyên lựa chọn | UI báo lỗi chung, mất lựa chọn |
| `Refresh_ConcurrentRequests_SingleRefreshCall` | nhiều request `401` đồng thời chỉ gọi `/auth/refresh` một lần | hai refresh đồng thời ⇒ bị đăng xuất |
| `Error_429And503_WaitRetryAfterThenRetrySameKey` | tôn trọng `Retry-After` | retry ngay lập tức |
| E2E J-01 (tay hoặc Playwright, Stripe test mode) | từ danh sách phim tới màn vé QR | bất kỳ bước nào kẹt |

## Câu hỏi review
- Vì sao client phải tuần tự hoá refresh, và điều gì xảy ra với hai tab cùng mở?
- Khi mất response của `POST …/bookings` (mạng chập chờn), client biết trạng thái bằng cách nào và vì sao gửi lại cùng key là an toàn?
- Sơ đồ ghế là "ảnh chụp" (`BR-23`): UI xử lý thế nào khi ghế vừa chọn đã bị người khác giữ?
- Access token / refresh token lưu ở đâu trong trình duyệt, và đánh đổi XSS / CSRF của lựa chọn đó là gì?

## Done khi
- [ ] Đi hết J-01 → vé QR trên trình duyệt với Stripe test mode
- [ ] Idempotency-Key, 409, 422, 429/503, refresh tuần tự đúng như bảng test (đã thấy đỏ khi bỏ từng hành vi)
- [ ] CORS cấu hình qua Options; không mở `*`
- [ ] `frontend.yml` chạy khi `frontend/**` đổi và **không** chạy khi chỉ `backend/**` đổi
- [ ] Không secret trong diff (chỉ publishable key của Stripe qua biến môi trường)
