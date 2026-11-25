---
id: 0007
title: Giới hạn 10 ghế mỗi người mỗi suất (BR-08) ép bằng khoá theo cặp (người xem, suất chiếu)
status: accepted
date: 2026-11-25
Related: ADR-0005
Updated: 2026-11-25 (Gate C, T12) — chốt cách hiện thực khoá
---

# Giới hạn 10 ghế mỗi người mỗi suất (`BR-08`) ép bằng khoá theo cặp (người xem, suất chiếu)

## Context

Ở T07, `BR-08` được xếp vào loại "không bảo vệ được bằng một transaction đơn lẻ" và **chấp nhận
cửa sổ race**: hai yêu cầu đồng thời của cùng một người có thể cùng đọc "đang có 8 ghế" và cùng
giữ thêm, vượt 10. Review 2026-11-25 thấy lập luận đó có ba lỗi:

1. **Đếm sai thứ.** `BR-08` nói "**mua** tối đa 10, cộng dồn qua mọi đơn". Mô tả cơ chế lại đếm
   ghế "đang **giữ**". Nếu chỉ đếm đang giữ, người xem trả tiền xong 10 ghế là giữ tiếp được 10
   nữa.
2. **"Chỉ người dùng tự hại" sai.** Luật tồn tại để chống **phe vé** — người bị hại là người xem
   khác. Và phe vé chính là kẻ sẽ bắn yêu cầu song song để khai thác đúng cửa sổ race này.
3. **"Thêm điểm tranh chấp toàn cục" sai.** Một khoá theo cặp `(người xem, suất chiếu)` chỉ tranh
   giữa các yêu cầu **của cùng một người cho cùng một suất** — gần như không bao giờ xảy ra với
   người dùng thật, và không ảnh hưởng gì tới người khác.

## Decision

- **Cái được đếm:** tổng số ghế (theo quy tắc đếm ghế — ghế đôi tính 2) trong các đơn của người đó
  cho suất đó ở trạng thái `AwaitingPayment` **còn hạn** hoặc `Paid`.
- **Cơ chế:** trong transaction giữ ghế, **trước** khi đếm, lấy một khoá theo cặp
  `(customer_id, showtime_id)` giữ tới hết transaction. Mọi yêu cầu giữ ghế của cùng người cho cùng
  suất bị **xếp hàng** qua khoá này ⇒ "đếm rồi giữ" không còn cửa sổ race.
- Cách hiện thực khoá — **chốt ở T12: advisory lock theo transaction**
  `pg_advisory_xact_lock(hashtextextended(customer_id || '/' || showtime_id, 0))`. Không chọn dòng quota
  `SELECT … FOR UPDATE`: phải chèn dòng trước khi khoá, thêm một bảng tăng theo (người × suất) mà không
  ai đọc, và phải dọn. Trùng băm chỉ làm hai cặp xếp chung hàng — chậm hơn, không sai. Khoá tự nhả khi
  transaction kết thúc (không dùng bản mức session).
- Thứ tự khoá trong transaction giữ ghế: **khoá quota trước, dòng ghế sau** (tăng dần theo id ghế)
  — cố định để không sinh deadlock giữa hai loại khoá.

## Considered Options

| Phương án | Ưu | Nhược |
|---|---|---|
| **Khoá theo (người xem, suất)** (chọn) | Không race; contention chỉ giữa các yêu cầu của chính người đó | Thêm một khoá vào đường giữ ghế; phải giữ thứ tự khoá |
| Chấp nhận race (quyết định T07) | Không thêm gì | Phe vé khai thác được; lập luận gốc sai |
| Bảng tổng hợp + `CHECK (count <= 10)` | Ràng buộc ở tầng lưu trữ | Phải đồng bộ bộ đếm khi hết hạn / huỷ / thanh toán — thêm nhiều chỗ ghi dễ lệch |
| `SERIALIZABLE` cho transaction giữ ghế | Chặn mọi race | Retry serialization failure trên đường nóng nhất khi mở bán phim hot |

## Consequences

- `BR-08` chuyển từ "không bảo vệ được" sang "bảo vệ được bằng một khoá **ngoài** ranh giới
  aggregate". Bảng "invariant không bảo vệ được bằng một transaction đơn lẻ" còn hai mục: `BR-20`,
  `BR-17` — và cả hai thực ra đều bảo vệ được bằng **một DB transaction** (xem ADR-0005), chỉ là
  không bằng **một aggregate**.
- Test bắt buộc (T22): 20 yêu cầu song song của **cùng** một người, mỗi yêu cầu 2 ghế ⇒ tổng ghế
  của người đó cho suất ≤ 10. Test này phải **fail** nếu bỏ khoá.
- Đóng Q21 và Q23.

## Revalidate at Gate C

Đo ở T23: thêm khoá này có làm tăng p99 của thao tác giữ ghế khi mở bán phim hot không. Nếu có,
ghi số đo vào đây.
