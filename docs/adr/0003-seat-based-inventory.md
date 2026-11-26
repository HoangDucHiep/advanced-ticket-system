---
id: 0003
title: Mô hình tồn kho theo ghế của suất chiếu
status: accepted
date: 2026-11-19
Supersedes: ADR-0001
Related: ADR-0005
---

# Mô hình tồn kho theo ghế của suất chiếu

## Context

Hệ thống đặt vé xem phim. Nghiệp vụ này khác bản chất với bán vé sự kiện theo hạng: **đơn vị
bán là từng chiếc ghế cụ thể**, và người xem thường mua **nhiều ghế cùng lúc** (mua cho cả
nhóm).

Hai hệ quả trực tiếp:

1. **Số lượng vé của một suất chiếu không phải là một con số được cấu hình** — nó là số ghế
   trong sơ đồ phòng. Không có "capacity" độc lập để đối chiếu.
2. **Giữ ghế là thao tác trên nhiều ghế cùng lúc, phải thành công toàn bộ hoặc không
   giữ ghế nào.** Đây là bài toán all-or-nothing trên nhiều dòng — khác hẳn bài toán giảm
   một số đếm trên một dòng.

ADR-0001 đã chốt tạm mô hình tier-based với `capacity` + `sold` + `reserved`. Mô hình đó
giả định một sự kiện có hạng vé đếm được, và không biểu diễn được chỗ ngồi cụ thể. Nghiệp
vụ đã đổi, nên ADR-0001 bị thay thế.

## Decision

Dùng **mô hình seat-based**: mỗi ghế của một suất chiếu là một đơn vị bán độc lập, mang
trạng thái riêng.

- **Sơ đồ ghế** thuộc về phòng chiếu: mỗi ghế có vị trí (hàng, số) và **loại ghế**
  (Standard / Couple / VIP).
- **Trạng thái ghế** thuộc về **suất chiếu** — vì cùng một ghế trong phòng, ở suất 19h và
  suất 21h là hai ghế bán độc lập. Mỗi (suất chiếu, ghế) là một đơn vị có trạng thái:
  `Available` / `Held` / `Sold` / `Blocked`.
- **Giá vé** thuộc về suất chiếu × loại ghế. Giá là thuộc tính của *loại ghế trong suất
  chiếu đó*, không phải của ghế cụ thể — vì cùng một loại ghế trong cùng một suất có cùng giá.
- **Không có cột `capacity`.** Số vé của một suất chiếu = số dòng ghế của suất đó. Tính
  được bằng `COUNT`, không lưu.
- **Loại ghế không mang giá riêng** ngoài suất chiếu. Giá thuộc về cặp (suất chiếu, loại
  ghế).

Một đơn đặt vé thuộc **đúng một suất chiếu** và chứa **nhiều ghế**. Mỗi ghế trong đơn sinh ra
**một vé** (mỗi vé có một mã QR riêng và ghi rõ ghế nào).

## Why

- **Nghiệp vụ đúng.** "Chọn ghế D7, D8" là thứ người xem thực sự làm khi đi xem phim. Mô hình
  đếm số không biểu diễn được nó, và một hệ thống bán vé không cho chọn ghế thì không phải
  hệ thống bán vé xem phim.
- **Bài toán concurrency khó hơn và đáng học hơn.** Bài toán "giữ N ghế all-or-nothing"
  buộc phải suy nghĩ về thứ tự khoá để tránh deadlock — một bài toán ít CV nào có. Bài toán
  tier-based (giảm một số đếm) quen thuộc hơn và ít giá trị hơn.
- **Tính đúng đắn tự nhiên hơn.** Với tier-based, "còn bao nhiêu vé" là một con số phải
  đồng bộ. Với seat-based, mỗi ghế có trạng thái riêng và nằm cùng một dòng — trạng thái
  ghế và chỗ giữ của ghế đó không thể lệch nhau.
- **Contention thấp hơn khi tải cao.** Nhiều người tranh cùng một hạng sẽ đụng một dòng;
  nhiều người tranh các ghế khác nhau sẽ đụng các dòng khác nhau. Mở bán phim hot tự nhiên phân
  tán thay vì dồn vào một điểm nóng.

## Considered Options

| Phương án | Ưu | Nhược |
|---|---|---|
| **Seat-based** (chọn) | Đúng nghiệp vụ xem phim; bài toán all-or-nothing + deadlock đáng học; trạng thái ghế nhất quán | Thêm bảng; phải suy nghĩ thứ tự khoá; cần seed sơ đồ ghế |
| Tier-based (ADR-0001) | Đơn giản, invariant `sold + reserved ≤ capacity` ép được bằng `CHECK` | Không biểu diễn được chỗ ngồi; bài toán concurrency ít giá trị hơn |
| Hybrid: tier + ghế tùy chọn | Linh hoạt | Hai mô hình tồn kho cùng lúc → hai bộ invariant, hai bộ câu SQL tranh chấp, và người đọc không biết cái nào đang áp dụng |
| Số lượng vé theo hạng + gán ghế sau khi thanh toán | Không cần chọn ghế lúc mua | Không giải quyết được "hai người chọn cùng ghế" — đẩy xung đột sang lúc sau, tệ hơn |

## Consequences

- **Số ghế của một suất chiếu là số dòng ghế.** Muốn biết "còn bao nhiêu vé" phải đếm hoặc
  đọc từ một bảng tổng hợp; cache và số đo cần tính đến điều này.
- **Giữ n ghế là thao tác nhiều dòng.** Cơ chế phải bảo đảm all-or-nothing và phải tránh
  deadlock khi hai người chọn ghế chồng nhau theo thứ tự ngược nhau. Xem
  [ADR-0002](0002-oversell-prevention.md).
- **Có thêm một trạng thái ghế: `Blocked`** (ghế bị vệ sinh, ghế dành cho khách VIP không
  bán online). Nó làm số ghế "bán được" khác số ghế "còn trống" — cần phân biệt rõ trong
  báo cáo tình trạng bán.
- **Cùng một ghế vật lý thuộc nhiều suất chiếu**, nên mô hình dữ liệu có hai tầng: ghế của
  phòng (tĩnh) và ghế của suất chiếu (thay đổi). Tách hai tầng này là điều kiện để một suất
  chiếu không ảnh hưởng suất khác.
- **Xoá suất chiếu phải xử lý cả vé đã bán** — đã có FR-24 (hủy suất chiếu và hoàn tiền),
  thực hiện sau T22.

## Revalidate at Gate B

Quyết định này được chốt **tạm** — nghiệp vụ đã đổi nhưng ranh giới aggregate chưa được xác
định. Ở Gate B (T07), phải trả lời:

- **Một suất chiếu có phải là một aggregate không?** Nó chứa N dòng ghế — với phòng lớn,
  đó có thể là hàng trăm dòng trong một aggregate. Nếu quá lớn, phải chia.
- Trạng thái ghế và trạng thái đơn đặt vé có thuộc cùng một aggregate không, hay tách?
- Invariant "một ghế thuộc đúng một chủ sở hữu tại một thời điểm" được bảo vệ thế nào khi
  aggregate bị chia nhỏ?

Nếu câu trả lời là "giữ nguyên", chuyển `status` thành `accepted`. Nếu không, viết ADR thay
thế và đặt `status: superseded by ADR-NNNN`.

---

## Kết quả xem lại ở Gate B (2026-11-21) — T07

Cả ba câu hỏi đã được trả lời. Xem [`../domain/aggregates/`](../domain/aggregates/).

| Câu hỏi | Trả lời |
|---|---|
| Một suất chiếu có phải là một aggregate không? | **KHÔNG.** ~~`Seat hold` là aggregate root, chứa ≤ 6 `Showtime seat`~~ → **từ 2026-11-25: mỗi `Showtime seat` là một aggregate root (ADR-0005)** |
| Trạng thái ghế và trạng thái đơn có cùng aggregate không? | **KHÔNG.** Chúng ở hai context khác nhau (`Inventory` / `Booking`), và việc giữ ghế ghi cả hai trong **cùng một transaction** (ranh giới bị cắt R3) |
| "Một ghế = đúng một chủ" bảo vệ thế nào khi chia nhỏ? | Bằng **điều kiện lệnh** (`UPDATE ... WHERE status = 'Available'`), không phải bằng ranh giới aggregate |

**Quyết định của ADR này vẫn giữ nguyên:** mô hình seat-based, ghế suất chiếu là đơn vị bán,
không có `capacity`. Điều được bổ sung là **ranh giới aggregate** — và đó là một quyết định
riêng, đáng ghi riêng.

### Trạng thái

**`status: accepted` (2026-11-25).**

Lý do: cả ba câu hỏi "Revalidate at Gate B" đều đã có câu trả lời rõ ràng và ghi vào
`docs/domain/aggregates/`. Quyết định "seat-based" không còn lý do để nghi ngờ.

**Đề xuất thêm:** viết **ADR-0004** cho quyết định "ranh giới aggregate của tồn kho ghế"
(`Seat hold` là aggregate root, `BR-01` ép bằng điều kiện lệnh, `BR-02` không được cắt) —
vì đó là một quyết định **khó đảo ngược**, **ngạc nhiên nếu không có context** (vì sao một
"giữ ghế" lại là aggregate root?), và là **kết quả của một đánh đổi thật** (an toàn của
`BR-02` vs contention khi mở bán phim hot).

> **AI không tự tạo ADR.** Người học duyệt nội dung và trạng thái trước khi file được tạo.
