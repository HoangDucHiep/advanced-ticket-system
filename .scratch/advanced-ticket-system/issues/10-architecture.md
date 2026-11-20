# 10: Architecture và module boundary

Mode: co-design
Gate: C
Layer: design
Blocked by: 06, 07, 08
Traces to: —

## Vì sao có ticket này

Đây là ticket dựng cấu trúc module — thứ mà bạn nói rõ không được làm trước khi có PRD và
DDD. Giờ đã đủ: context map cho biết **ranh giới**, aggregates cho biết **transaction
boundary**, state machines cho biết **hành vi**.

Kiến trúc ở đây không phải "folder nào chứa file nào". Kiến trúc là: **module nào biết gì về
module nào**, và điều đó quyết định cái gì có thể thay đổi độc lập, cái gì sẽ phải sửa cùng
nhau, và test nào cần mock cái gì.

## Bạn tạo ra

`docs/design/architecture.md`:

- **Sơ đồ module** — module nào, phụ thuộc hướng nào. Vẽ bằng Mermaid.
- **Quy tắc phụ thuộc** — bằng câu dạng khẳng định, ví dụ:
  - "Ordering không được đọc bảng của Payment."
  - "Mọi module giao tiếp qua interface trong Shared, không qua tham chiếu trực tiếp."
  - "Chỉ Ordering được ghi vào bảng trạng thái ghế của suất chiếu (`inventory.showtime_seats`)."
- **Vì sao chọn modular monolith** — dựa trên 12 bài toán concurrency đã chốt: có bài nào
    bắt buộc phải là transaction xuyên context không? Nếu có, tách service sớm là sai.
- **Cấu trúc thư mục** cụ thể: solution, project, tên thư mục, ai chứa gì.
- **Đường đi của một request** — từ controller xuống tới DB, đi qua mấy module.
- **Cơ chế outbox** — vì sao có, và ranh giới nó nằm ở đâu.
- **Điều kiện để tách thành service** — dựa trên số đo, không phải cảm tính.

Câu hỏi gợi ý:

1. Từ context map, module nào ứng với context nào? Có context nào nhỏ đến mức nên gộp?
2. **Bài toán concurrency nào bắt buộc cần transaction xuyên module?** (Ví dụ: giữ n ghế và
   tạo đơn đặt vé là một transaction hay hai? Kết quả trả lời quyết định ranh giới aggregate
   ở T07 và cơ chế all-or-nothing ở T12.) Nếu có bài như vậy, đó là lý do chưa tách service.
3. `Shared` chứa cái gì? Ranh giới của nó là gì — làm sao nó không thành cái "thùng rác"?
4. Ai giữ quyền ghi vào bảng nào? Bảng ghế của phòng do module nào sở hữu? Bảng **trạng thái
   ghế theo suất chiếu** do module nào sở hữu? Hai bảng đó có thuộc cùng một module không?
5. Nếu mai mốt tách `Ordering` ra service riêng, những gì phải thay đổi? Câu trả lời cho
   biết ranh giới module có đúng không.

## AI làm gì

AI hỏi từng quyết định kiến trúc một, đưa lựa chọn kèm khuyến nghị:

- **Một `DbContext` với `HasDefaultSchema` cho mọi module, hay mỗi module một `DbContext`?**
  Khuyến nghị: một `DbContext` — ít công sức hơn nhiều, và tách sau cũng dễ. Nhưng phải
  nói rõ đánh đổi.
- **Một project lớn hay nhiều project?** Khuyến nghị: nhiều project theo module, vì ranh giới
  project là ranh giới compile-time — không phụ thuộc kỷ luật.
- **Dapper và EF cùng tồn tại — quy tắc phân chia thế nào?** Khuyến nghị: một hot path
  chỉ đi qua một tầng.

AI phản biện khi bạn đề xuất cấu trúc có vẻ "đẹp" nhưng không khớp với context map.

## Tiêu chí hoàn thành

- [ ] Có sơ đồ module với hướng phụ thuộc
- [ ] Có ít nhất 5 quy tắc phụ thuộc viết dạng khẳng định, kiểm được
- [ ] Có lý do chọn modular monolith dựa trên bài toán concurrency cụ thể
- [ ] Có cấu trúc thư mục cụ thể với tên project
- [ ] Mỗi bảng được ghi rõ module nào sở hữu
- [ ] Có mô tả đường đi của một request đi qua các module
- [ ] Có điều kiện tách service dựa trên số đo
- [ ] Có nêu rõ bài toán concurrency nào buộc phải giữ transaction xuyên module

> **Ghi chú (2026-11-19):** nghiệp vụ đổi sang đặt vé xem phim. Điểm mới cần phân định rõ:
> **bảng ghế của phòng** (tĩnh, thuộc nghiệp vụ phòng chiếu) và **bảng trạng thái ghế theo
> suất chiếu** (thay đổi liên tục, thuộc nghiệp vụ đặt vé) là hai bảng khác nhau về vòng đời
> lẫn tần suất ghi. Việc chúng nằm cùng hay khác module là một quyết định kiến trúc cần
> lập luận, không phải mặc định.
