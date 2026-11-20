# 04: User journeys và business rules

Mode: co-design
Gate: A
Layer: prd
Blocked by: 02
Traces to: —

## Vì sao có ticket này

Requirements nói hệ thống **phải** làm gì. User journey nói **người dùng đi qua những bước
nào** — và bước nào trong đó là lúc hệ thống phải ra tay.

Đây là nơi các bài toán khó của dự án xuất hiện lần đầu. Mở bán phim hot không phải là "mua
vé nhanh" — nó là một hành trình gồm nhiều người cùng chen vào một khoảnh khắc, và chính chi
tiết của hành trình đó quyết định bạn cần những cơ chế concurrency nào.

Business rules là luật nghiệp vụ áp dụng xuyên suốt: giới hạn bao nhiêu vé một người, giữ ghế
bao lâu, ai được ưu tiên khi ghế hết.

## Bạn tạo ra

### `docs/prd/03-user-journeys.md`

Mỗi journey là một chuỗi bước, mỗi bước ghi rõ:

```
## Journey: <tên>

<Ai đang làm gì, và vì sao>

1. <bước> — <điều hệ thống phải làm>
2. <bước> — <điều hệ thống phải làm>
...
```

Journey tối thiểu phải có:

- **Tìm phim và lịch chiếu** — người dùng tìm phim, xem suất nào, còn ghế nào ở mỗi loại.
- **Đặt vé trong điều kiện bình thường** — xem sơ đồ ghế, chọn ghế, giữ chỗ, thanh toán, nhận vé.
- **Mở bán phim hot** — nhiều người cùng lúc, ghế hạn chế, có người thắng có người thua.
- **Bị từ chối** — ghế đã có người, hết thời gian giữ ghế, thẻ hỏng. Người dùng cần biết
  chuyện gì xảy ra và làm gì tiếp.
- **Check-in tại cổng** — quét vé, hợp lệ thì vào, quét lại thì báo đã check-in, vé giả thì bị
  chặn.

Với mỗi bước, đánh dấu bước nào **chạm vào tồn kho ghế** và bước nào **chạm vào tiền** — vì
đó là hai chỗ bài toán concurrency nặng nhất.

### `docs/prd/04-business-rules.md`

Danh sách luật, mỗi luật một mã `BR-nn`:

```
### BR-01 — <tên luật>

<Luật là gì.>

- Ràng buộc: <cơ chế nghiệp vụ buộc phải giữ>
- Vi phạm thì: <điều gì xảy ra nếu bị vi phạm>
```

Luật cần chốt (nếu chưa có trong T02):

- Một người được mua tối đa mấy vé cho một suất chiếu?
- Giữ ghế bao lâu? Có đồng hồ đếm ngược hiển thị cho người dùng không?
- Khi hết thời gian giữ ghế, chuyện gì xảy ra với đơn đó?
- Một đơn được đặt tối đa mấy ghế?
- Ai được quyền check-in? Một vé có thể check-in ở nhiều cổng không?
- Ghế `Blocked` (vệ sinh, dành riêng) ai quyết định, có bán lại online không?
- Có được đặt ghế của hai loại khác nhau trong cùng một đơn không?

## AI làm gì

AI hỏi từng journey một. Với mỗi bước AI hỏi:

- "Bước này hệ thống phải trả lời gì / ghi gì / giữ gì?"
- "Ở bước này có ai khác đang làm cùng lúc không?" — đây là cách phát hiện điểm tranh chấp.
- "Nếu bước này thất bại, người dùng thấy gì?"

AI ghi vào hai file. AI **không tự thêm luật** — nếu gợi ý một luật chưa có, hỏi bạn trước.

## Tiêu chí hoàn thành

- [x] Có 5 journey tối thiểu, gồm cả mở bán phim hot và cả trường hợp bị từ chối — 7 journey
  (`J-01`..`J-07`)
- [x] Mỗi journey ghi rõ hệ thống phải làm gì ở từng bước
- [x] Bước chạm tồn kho ghế và bước chạm tiền được đánh dấu — nhãn `[GHẾ]` / `[TIỀN]`, cộng
  bảng "Bản đồ điểm tranh chấp" ở cuối file
- [x] `docs/prd/04-business-rules.md` có ít nhất 5 luật đánh số `BR-nn` — 20 luật
  (`BR-01`..`BR-20`)
- [x] Mỗi luật ghi rõ "vi phạm thì sao"
- [x] Với mỗi `FR-nn` ở T02, đã chỉ ra nó xuất hiện ở journey nào — bảng truy vết trong
  `docs/prd/03-user-journeys.md`

> **Ghi chú:** người học chốt trong lúc co-design: giữ ghế 15 phút · tối đa 6 ghế mỗi đơn ·
> tối đa 10 ghế mỗi người mỗi suất (cộng dồn qua các đơn) · ghế đôi tính 2 ghế khi đếm
> giới hạn nhưng vẫn sinh 1 vé · hết hạn thì đơn coi như đã huỷ · trả tiền muộn thì hoàn
> tự động toàn bộ → thêm `FR-39` · ghế `Blocked` do quản lý rạp quyết định, không bán lại online.
