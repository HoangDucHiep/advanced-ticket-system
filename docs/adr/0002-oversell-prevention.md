---
id: 0002
title: Chống bán trùng bằng conditional UPDATE trên ghế
status: accepted
date: 2026-11-18
Updated: 2026-11-19 (sang mô hình seat-based, xem ADR-0003); accepted 2026-11-25 (Gate C, T12)
---

# Chống bán trùng bằng conditional UPDATE trên ghế

> **Chốt ở Gate C (2026-11-25).** Phiên bản cuối: **khoá tường minh theo thứ tự rồi cập nhật có
> điều kiện** — `SELECT … WHERE id = ANY(@ids) ORDER BY id FOR UPDATE` (khoá theo thứ tự id, đọc trạng
> thái để báo ghế nào thất bại), rồi `UPDATE … WHERE … AND (status = 'Available' OR (status = 'Held'
> AND held_until <= now()))`, trong `READ COMMITTED`. Đây gần với biến thể **C** bên dưới (nhưng ở
> `READ COMMITTED`) và khác bản `provisional` ở chỗ **không** tin vào thứ tự khoá của một câu `UPDATE …
> ANY(...)`. Lý do chốt **bằng tính đúng đắn, không bằng tốc độ**: thứ tự khoá của câu `UPDATE` là chi
> tiết của planner; `FR-15` cần biết **ghế nào** đã có người. Số đo ba biến thể vẫn chạy ở T23 và ghi
> vào đây — nếu biến thể khác nhanh hơn rõ rệt *và* vẫn đúng, mở ADR mới. Đầy đủ:
> `docs/design/concurrency.md` #1–#3; ghế `Held` quá hạn: ADR-0015.

## Context

Khi mở bán phim hot, N người tranh M ghế. Mô hình đọc-rồi-ghi (`SELECT` trạng thái ghế,
kiểm tra trong code, rồi `UPDATE`) có một cửa sổ race giữa hai câu lệnh, và cho phép bán
trùng ghế.

Khác với mô hình đếm số cũ (một số đếm trên một dòng), ở đây mỗi ghế là **một dòng riêng**,
và một đơn đặt vé có thể chứa **nhiều ghế**. Điều đó sinh ra ba bài toán riêng:

1. **Tranh cùng một ghế** — hai người cùng đặt ghế D7.
2. **All-or-nothing** — đặt 4 ghế, giữ được 3 thì phải trả lại cả 4.
3. **Deadlock** — hai người chọn ghế theo thứ tự ngược nhau cùng lúc.

## Decision

Chống bán trùng bằng **conditional `UPDATE`**, quyết định dựa trên **số dòng bị ảnh
hưởng**.

### Trường hợp một ghế

```sql
UPDATE inventory.showtime_seats
SET status = 'Held', held_until = @until, hold_id = @hold_id
WHERE showtime_id = @showtime_id
  AND seat_id = @seat_id
  AND status = 'Available'
RETURNING seat_id;
```

- Số dòng trả về là `1` → giữ ghế thành công.
- Số dòng trả về là `0` → ghế không còn trống, trả `409`.

### Trường hợp nhiều ghế (all-or-nothing)

Các ghế được giữ trong **một transaction**, và ghế được khoá theo **thứ tự cố định** (theo
`seat_id`) để tránh deadlock:

```sql
-- Trong một transaction. Thứ tự seat_id tăng dần là bắt buộc.
UPDATE inventory.showtime_seats
SET status = 'Held', held_until = @until, hold_id = @hold_id
WHERE showtime_id = @showtime_id
  AND seat_id = ANY(@seat_ids)
  AND status = 'Available'
RETURNING seat_id;
```

- Số dòng trả về **bằng** số ghế yêu cầu → giữ thành công.
- Số dòng trả về **ít hơn** → rollback toàn bộ transaction, giải phóng các ghế đã giữ
  trong lần đó, trả `409` kèm danh sách ghế đã có người.

Không có đường nào đọc trạng thái ghế rồi mới ghi.

Đây là quyết định **tạm**, chưa so sánh với các phương án khác bằng số đo.

## Why

- Một câu lệnh, một round trip, không cần giữ lock qua application code.
- Postgres đánh giá điều kiện `WHERE` trên trạng thái mới nhất của dòng, nên tính đúng ngay cả
  khi nhiều transaction cùng tranh dòng đó.
- Kết quả quyết định là **tính hiện đạo** — không phụ thuộc vào việc đọc trước có đúng hay không.
- **Thứ tự khoá cố định** là cách chống deadlock kinh điển: hai transaction cùng tranh các
  ghế A và B sẽ khoá A trước B, nên không thể tạo vòng chờ.

## Considered Options

| Phương án | Ưu | Nhược |
|---|---|---|
| **Conditional UPDATE** (chọn tạm) | 1 round trip, không giữ lock trong app, kết quả từ số dòng | Predicate phải viết đúng; không trả về *lý do* thất bại |
| `SELECT ... FOR UPDATE` từng ghế | Rõ ràng, đọc được trạng thái đầy đủ | Giữ lock qua application code, lock convoy, phải quản lý transaction thủ công |
| `SELECT ... FOR UPDATE` theo thứ tự `seat_id` | Vẫn tránh deadlock | Nhiều round trip; lock sống lâu hơn |
| Postgres advisory lock | Khóa theo key ở tầng DB | Thêm round trip; khóa sống lâu hơn transaction nếu không cẩn thận |
| `SERIALIZABLE` | Bảo đảm mạnh nhất | Phải retry serialization failure, latency cao hơn |
| Đọc-rồi-ghi trong transaction | Đơn giản nhất | **Có race → bán trùng.** Không dùng |
| Redis làm nguồn sự thật tồn kho | Nhanh | Redis không thay thế được guarantee ở DB; mất đồng bộ là mất tiền |
| Khóa toàn bộ ghế của một suất chiếu | Đơn giản tuyệt đối | **Mất ý nghĩa của seat-based**: mọi người tranh một hàng nghìn dòng |
| Giữ ghế từng cái một, nhiều transaction | Không cần transaction lớn | **Không all-or-nothing** — vi phạm FR-02 |

## Consequences

- Cơ chế này **không** dùng được bởi ORM ở dạng biểu đạt trực tiếp — cần raw SQL qua
  Dapper. Đây là điểm được chấp nhận có chủ ý, và là một ví dụ điển hình cho việc "đôi khi
  phải viết SQL tay".
- **Phải phân biệt "ghế không tồn tại" với "ghế đã có người".** Câu lệnh trả `0` dòng không
  phân biệt được hai trường hợp — có thể cần một câu kiểm tra riêng cho trường hợp thứ hai,
  hoặc chấp nhận cả hai trả `409`.
- **Không có `CHECK` constraint tương tự như mô hình đếm số cũ.** Trạng thái ghế là một
  enum, nên "một ghế thuộc đúng một chủ" được bảo vệ bởi việc trạng thái và `hold_id` nằm
  trên **cùng một dòng** — không thể tách. Đây là một lợi thế của seat-based. (Thêm `CHECK`
  `Held` ⇔ có `hold_id` + `held_until` — `INV-02` của `Inventory`.)
- **`UPDATE` của Postgres không có `ORDER BY`.** Một câu `UPDATE ... WHERE id = ANY(...)` **không**
  bảo đảm thứ tự lấy khoá. Thứ tự tăng dần phải được ép bằng một bước khoá riêng
  (`SELECT ... ORDER BY id FOR UPDATE`) hoặc bằng từng câu `UPDATE` theo thứ tự — chốt ở T12.
- Tên cột trong SQL mẫu ở trên (`seat_id`) là minh hoạ; tên thật chốt ở T11.
- **Thứ tự khoá phải được đảm bảo ở tầng code.** Không có cơ chế nào của database tự sắp xếp
  thứ tự `UPDATE` — nếu truy vấn `WHERE seat_id = ANY(...)` không bảo đảm thứ tự, thì phải
  chia thành nhiều câu `UPDATE` từng ghế theo thứ tự tăng dần. Đây là chi tiết phải kiểm chứng
  bằng test ở T22, không được coi là hiển nhiên.
- Kiểm tra giới hạn mua vé của một người (`UNIQUE` hoặc đếm) **không** nằm trong cùng câu
  lệnh này và phải là một ràng buộc riêng — xem [ADR-0007](0007-per-customer-showtime-quota.md).
- `hold_id` là tham chiếu giữ do bên gọi cung cấp (hiện là định danh `Booking`); `Inventory` không
  diễn giải nó — xem [ADR-0005](0005-showtime-seat-aggregate.md).
- **Điều kiện lệnh không chống được deadlock.** `UPDATE` có điều kiện vẫn lấy row lock; thứ có
  tác dụng chống deadlock là **thứ tự khoá**. Postgres tự phát hiện deadlock (sau
  `deadlock_timeout`, mặc định 1s) và huỷ một bên với `40P01` — nên một test chỉ kiểm "cả hai đều
  có phản hồi" sẽ **vẫn xanh khi bỏ thứ tự khoá**. Test ở T22 phải khẳng định **không có lỗi
  `40P01`** và mỗi yêu cầu thắng hoặc thua với lý do nghiệp vụ.

## Revalidate at Gate C

Quyết định này phải được **so sánh bằng số đo** ở Gate C, không bằng lý thuyết. Ở ticket T23,
chạy cùng một kịch bản mở bán phim hot với ba biến thể:

- **A**: conditional UPDATE trong `READ COMMITTED`, một câu nhiều dòng.
- **B**: conditional UPDATE chia thành nhiều câu từng ghế, theo thứ tự `seat_id` tăng dần.
- **C**: `SELECT ... FOR UPDATE` theo thứ tự `seat_id` trong `REPEATABLE READ`.

Đo: p50/p95/p99 latency, tỉ lệ lỗi, `monitor-lock-contentions` từ `dotnet-counters`, số request
thành công ở mỗi mức RPS, và **số lần deadlock phát sinh** (biến thể nào sinh deadlock thì
loại).

Sau đó:

- Nếu một biến thể thắng rõ rệt và không sinh deadlock → `status: accepted`, bổ sung số đo
  vào ADR.
- Nếu hòa → chọn biến thể đơn giản nhất, và ghi rõ trong ADR rằng đánh đổi là không trả về
  lý do thất bại.

Tiêu chí quyết định không nên là "con số đẹp hơn" — nó nên là: **ở mức tải cao nhất mà hệ
thống vẫn đáp ứng được, cơ chế nào giữ được invariant với độ trễ chấp nhận được, và không
bao giờ kẹt?**
