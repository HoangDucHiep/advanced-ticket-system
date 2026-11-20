# 11: Data model

Mode: co-design
Gate: C
Layer: design
Blocked by: 07
Traces to: —

## Vì sao có ticket này

Schema là nơi invariant được cưỡng chế lần cuối bởi database. Một invariant mà chỉ được bảo
vệ bằng logic ứng dụng là invariant **sẽ** bị vi phạm — bởi một script, bởi một migration,
bởi một developer sửa code lúc 2 giờ sáng.

Và với dự án này, schema còn mang thêm một vai trò: **một số câu SQL phải được viết tay**.
`UPDATE ... WHERE status = 'Available'` trên nhiều dòng cùng lúc, rồi đếm số dòng bị ảnh
hưởng, không thể biểu đạt gọn trong EF Core. Đó không phải là hạn chế của ORM — đó là điểm
mấu chốt của bài toán.

## Bạn tạo ra

`docs/design/data-model.md`, mỗi bảng một mục:

```
## <schema>.<table>

**Invariant**: <luật mà bảng này bảo vệ>

| Cột | Kiểu | Null | Ghi chú |
|---|---|---|---|

### Index

- <index> — <lý do>

### Constraint

- <constraint> — <điều gì nó chặn>
```

Bắt buộc có:

- **Danh sách constraint kèm lý do** — đặc biệt là các `CHECK` và `UNIQUE` bảo vệ invariant
  từ T07. Mỗi constraint phải truy ngược được tới một `INV-nn`.
- **Index kèm lý do** — index này phục vụ truy vấn nào, hoặc ràng buộc nào.
- **Các câu SQL tay** — những câu không thể biểu đạt bằng ORM, ghi rõ tại sao.

Câu hỏi gợi ý:

1. Từ `INV-nn` ở T07: constraint nào ở tầng DB bảo vệ invariant đó? Constraint nào phải để
   logic ứng dụng gánh?
2. Từ `BR-nn` ở T04: luật nghiệp vụ nào cần thành `UNIQUE`?
3. Từ state machines: transition nào cần được bảo vệ bằng `WHERE status = ...` trong câu
   `UPDATE`?
4. Bảng nào sẽ lớn? Bảng nào được đọc nhiều? Index nào là đắt nhất khi ghi?
5. **Idempotency record** lưu ở đâu, unique trên cột nào, và hết hạn sau bao lâu?
6. Có bảng nào cần partition theo thời gian không? (lịch sử đơn đặt vé, check-in)
7. **Bảng ghế của suất chiếu** có cần một bảng riêng không, hay suất chiếu chỉ giữ danh
   sách id ghế? (Câu hỏi này quyết định tần suất ghi và kích thước bảng.)
8. **Giá vé** lưu ở đâu — theo (suất chiếu, loại ghế), hay nhân với giá của loại ghế trong
   phòng? (Nếu giá nhân với giá loại ghế trong phòng, thì đổi giá loại ghế sẽ đổi giá cả
   những suất đã mở bán — đó là một quyết định nghiệp vụ phải chốt.)
9. Trạng thái ghế có dùng `enum` Postgres hay `CHECK` trên `text`? Và làm sao thêm trạng
   thái mới (ví dụ `Blocked`) không phá dữ liệu cũ?

## AI làm gì

AI hỏi từng bảng một. Với mỗi bảng AI:

- **Đối chiếu invariant**: hỏi "invariant nào ở T07 mà bảng này phải bảo vệ?"
- **Đề xuất constraint** và hỏi bạn xác nhận.
- **Phản biện index**: hỏi "index này phục vụ truy vấn nào?" — index không có truy vấn đi kèm
  là chi phí ghi thừa.
- **Chỉ ra câu SQL tay bắt buộc** và giải thích tại sao ORM không diễn đạt được.

AI ghi vào `docs/design/data-model.md`. AI không tự thêm bảng nào bạn chưa nói.

## Tiêu chí hoàn thành

- [ ] Mọi bảng đều có invariant được nêu
- [ ] Mọi `CHECK` / `UNIQUE` đều truy ngược được tới một `INV-nn` hoặc `BR-nn`
- [ ] Mọi index đều có lý do
- [ ] Có ít nhất 2 câu SQL tay, giải thích vì sao ORM không làm được
- [ ] Bảng idempotency record có unique constraint và quy tắc hết hạn
- [ ] Có nêu rõ bảng nào dự kiến lớn và tác động của nó lên index
- [ ] Không có cột nào thừa mà chưa được dùng ở đâu
- [ ] Có câu SQL tay cho **giữ n ghế all-or-nothing** (nhiều dòng, có điều kiện, đếm số dòng
      ảnh hưởng), kèm giải thích vì sao ORM không diễn đạt được
- [ ] Đã chốt được câu hỏi về nơi lưu **giá vé** và về **bảng trạng thái ghế theo suất**
