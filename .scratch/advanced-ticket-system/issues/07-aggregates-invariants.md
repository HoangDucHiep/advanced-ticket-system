# 07: Aggregates và invariants

Mode: co-design
Gate: B
Layer: domain
Blocked by: 06
Traces to: —

## Vì sao có ticket này

Đây là ticket quan trọng nhất của Lớp 2. Aggregate là ranh giới nhất quán: **một transaction
duy nhất bảo vệ được những invariant nào?**

Câu trả lời quyết định trực tiếp cơ chế concurrency. Nếu invariant "một ghế của một suất
chiếu thuộc về đúng một chủ sở hữu tại một thời điểm" nằm trong một aggregate, thì việc thực
thi nó là **một lệnh nguyên tử**. Nếu nó nằm rải rác ở ba aggregate, thì bạn cần một cơ chế
khác, phức tạp hơn nhiều.

Và đây là lý do DDD không phải là lý thuyết cho vui: aggregate boundary **là** ranh giới
transaction **là** đơn vị nhất quán **là** phạm vi của một lệnh SQL atomic.

## Bạn tạo ra

`docs/domain/aggregates.md`, mỗi aggregate một mục:

```
## Aggregate: <Tên>

**Bản chất**: <identity / value>
**Chứa**: <các entity/value object bên trong>
**Boundary**: <điều gì nằm ngoài aggregate này>

### Invariants

- INV-01: <luật bất biến> — <vì sao phải đúng>
- INV-02: ...

### Transaction boundary

<Hành động nào nằm trong một transaction, và transaction đó bảo vệ invariant nào>
```

Câu hỏi gợi ý:

1. Với mỗi context ở T06: aggregate chính của nó là gì? (Một context có thể có nhiều aggregate.)
2. **Aggregate nào chứa ghế?** Một suất chiếu có hàng trăm ghế — đó là hàng trăm dòng trong
   **một** aggregate. Câu hỏi lớn nhất của ticket này: một suất chiếu có phải là một aggregate
   không, hay phải chia? Và nếu chia, invariant "một ghế thuộc đúng một chủ" bảo vệ thế nào?
3. Invariant nào phải luôn đúng, kể cả **giữa chừng một transaction**? Ví dụ: giữ 4 ghế xong
   mới tạo đơn — nếu transaction rollback ở giữa chừng, có ghế nào bị giữ lại không?
4. Invariant nào **không thể** bảo vệ bằng một transaction đơn lẻ? (Ví dụ: "mỗi người chỉ
   được mua tối đa 4 vé trên 1 suất chiếu" — có cần unique constraint ở DB chứ không phải logic?)
5. Aggregate nào có thể lớn? Nếu một aggregate phải chứa hàng nghìn row, đó là dấu hiệu
   boundary sai.
6. Điều gì là **value object** ở đây? (Ví dụ: một khoảng tiền, một vị trí ghế, một `BookingId`.)

## AI làm gì

AI giảng phần khó trước khi hỏi:

- Aggregate, entity, value object khác nhau thế nào, và vì sao ranh giới aggregate quyết định
  ranh giới transaction.
- Vì sao một aggregate lớn là mùi code (anemic domain model, nhiều query).
- Vì sao invariant phải được **bảo vệ bằng cơ chế**, không bằng ý thức của lập trình viên.
- **Vì sao "giữ n ghế all-or-nothing" buộc ranh giới aggregate phải bao trọn n ghế đó** — và
  điều đó có thể mâu thuẫn với nguyên tắc "aggregate không được quá lớn". Đây là cuộc đánh đổi
  trung tâm của ticket này.

Rồi AI hỏi từng invariant một, và với mỗi invariant hỏi:

- "Cơ chế nào bảo vệ nó?" (constraint DB / check constraint / conditional update / lock / không
  thể bảo vệ được?)
- "Ai có thể vi phạm nó?" (chỉ aggregate đó hay code ở nơi khác?)

AI ghi vào `docs/domain/aggregates.md`. AI **không tự quyết boundary** — đây là quyết định của
bạn, AI chỉ phản biện.

## Tiêu chí hoàn thành

- [ ] Có ít nhất 4 aggregate, mỗi cái ghi rõ boundary
- [ ] Mỗi aggregate có ít nhất 2 invariant, mỗi invariant ghi rõ cơ chế bảo vệ
- [ ] Có ít nhất một invariant được ghi rõ là **không thể** bảo vệ bằng transaction đơn lẻ,
      kèm hệ quả
- [ ] Transaction boundary của mỗi aggregate được mô tả bằng hành vi, không phải bằng SQL
- [ ] Có phân biệt rõ entity và value object
- [ ] Không aggregate nào bị coi là chứa thứ vốn thuộc về aggregate khác
- [ ] **Câu hỏi về kích thước aggregate (một suất chiếu có phải một aggregate không) đã được
      trả lời rõ ràng**, kèm lý do
