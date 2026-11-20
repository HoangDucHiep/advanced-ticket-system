# 05: Ubiquitous language workshop

Mode: explain
Gate: B
Layer: domain
Blocked by: 04
Traces to: —

## Vì sao có ticket này

Mỗi người trong một dự án dùng từ khác nhau cho cùng một thứ: "đơn", "booking", "order",
"transaction". Khi mỗi từ mang một nghĩa hơi khác nhau, code sẽ phản ánh sự nhầm lẫn đó — và
bug sẽ nằm ở chỗ ranh giới giữa hai nghĩa.

Đây là ticket dạng `explain`: **AI giảng, bạn hỏi và trả lời**. Bạn không viết tài liệu nào ở
đây trừ khi AI ghi lại thuật ngữ bạn xác nhận.

Có 4 nhóm khái niệm cần phân biệt rõ, vì chúng là nguồn của các bài toán concurrency khó
nhất:

1. **Ghế / vé / đơn đặt vé / giữ ghế** — bốn thứ khác nhau:
   - Ghế: một vị trí ngồi trong phòng, thuộc về phòng, có loại và vị trí.
   - Ghế **của một suất chiếu**: cùng một cái ghế vật lý, ở suất 19h và suất 21h là hai ghế
     bán độc lập. Đây là đơn vị bán thật sự.
   - Vé: một mã QR gắn với **một ghế của một suất chiếu**. 1 ghế = 1 vé.
   - Đơn đặt vé: một lần thanh toán, thuộc **một suất chiếu**, chứa **nhiều ghế**.
   - Giữ ghế: ghế ở trạng thái "tạm giữ" kèm thời điểm hết hạn, chưa bán, sẽ tự động giải
     phóng nếu không thanh toán.
   Bốn cái này khác nhau ở **thời điểm trừ tồn kho** và **ai chịu trách nhiệm khi nó thất bại**.
2. **Trạng thái ghế** — "còn 40 chỗ" là gì? Tổng số ghế của phòng? Ghế còn trống? Ghế còn
   bán online (không tính ghế `Blocked`)? Ba con số này khác nhau và người dùng thấy cái nào?
3. **Loại ghế và giá** — giá thuộc về *loại ghế trong suất chiếu đó*, không phải của ghế cụ
   thể và không phải của phòng. Hai suất của cùng một phim có thể có giá khác nhau.
4. **Suất chiếu và phim** — một phim có nhiều suất; mỗi suất có sơ đồ ghế **riêng** (trạng
   thái ghế độc lập), dù dùng chung sơ đồ vật lý của phòng.

## Bạn tạo ra

`docs/domain/ubiquitous-language.md` — bản làm việc, sẽ hoàn thiện dần. Mỗi thuật ngữ:

```
**Tên thuật**:
<định nghĩa trong 1–2 câu>
_Avoid_: các từ khác dùng cho cùng thứ
```

AI sẽ hỏi bạn về các thuật ngữ này và ghi lại. Những thuật ngữ bạn xác nhận sẽ được dùng
xuyên suốt các tài liệu về sau, và trong `GLOSSARY.md` cuối Gate B.

## AI làm gì

AI giảng:

- Sự khác biệt giữa các khái niệm trên, bằng ví dụ cụ thể (một người mua 4 ghế, mở bán phim
  hot với 100 ghế, đơn hết giờ chưa thanh toán, cùng một cái ghế ở hai suất).
- Vì sao DDD phân biệt "aggregate" với "entity" với "value object" — chỉ ở mức cần để bạn
  định nghĩa được ranh giới ở T07.
- Vì sao thuật ngữ phải thống nhất trước khi vẽ context map.

Rồi AI **hỏi lại** để kiểm tra bạn đã hiểu, bằng câu hỏi cụ thể, không phải "bạn hiểu chứ?".
Ví dụ: "Nếu một người mua 4 ghế hạng Standard và hết giờ chưa thanh toán, hệ thống phải trả
lại 4 ghế đó cho ai, và 4 mã QR có được cấp không — và nếu cùng cái ghế đó ở suất 21h thì
sao?"

AI ghi thuật ngữ bạn xác nhận vào `docs/domain/ubiquitous-language.md`.

## Tiêu chí hoàn thành

- [x] Có ít nhất 15 thuật ngữ, mỗi thuật ngữ có định nghĩa và danh sách từ nên tránh — 27 thuật
  ngữ, chia 6 nhóm, mỗi mục có định nghĩa + `_Avoid_`
- [x] Bốn nhóm khái niệm ở trên đã được phân biệt rõ bằng định nghĩa, không phải bằng ví dụ —
  mục "Các cặp thuật ngữ dễ nhầm" nêu rõ **vì sao** khác nhau, không chỉ ví dụ
- [x] Phân biệt rõ **ghế của phòng** và **ghế của suất chiếu** (đây là chỗ dễ nhầm nhất) —
  có mục riêng "Ghế (của phòng) vs Ghế suất chiếu" với bảng hai câu hỏi phải trả lời khác nhau
- [x] Không thuật ngữ nào trùng nghĩa với thuật ngữ khác
- [x] Bạn trả lời được các câu hỏi kiểm tra của AI mà không cần mở tài liệu

> **Ghi chú:** người học trả lời đúng 4/5 câu kiểm tra ngay lần đầu (ghế suất chiếu, số ghế
> còn bán online, hết hạn 15 phút, ghế đôi). Hai câu hỏi phải hỏi lại vì **câu hỏi của AI sai**
> (hỏi về 20:12 trong khi hết hạn là 20:15) và vì AI nhầm mức "ghế của phòng" với "ghế của suất
> chiếu" — chính là lỗi mà ticket này muốn diễn giải.
>
> **Mâu thuẫn PRD phát hiện và đã sửa:** `FR-23` (ba con số cộng lại = tổng số ghế) mâu thuẫn với
> `BR-11` (ghế bị chặn vẫn tính vào tổng) → đã sửa `FR-23` thành bốn con số.
