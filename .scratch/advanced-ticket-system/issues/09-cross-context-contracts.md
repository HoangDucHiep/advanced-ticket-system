# 09: Cross-context contracts

Mode: co-design
Gate: B
Layer: domain
Blocked by: 07
Traces to: —

## Vì sao có ticket này

Hai context nói chuyện với nhau qua một **hợp đồng**. Hợp đồng đó là nơi dữ liệu bị dịch đi,
nơi thuật ngữ bị hiểu khác nhau, và nơi một thay đổi ở context A làm hỏng context B mà
không ai biết.

Nếu bạn không ghi hợp đồng này ra, thì khi triển khai, ranh giới context sẽ bị xói mòn: code
sẽ đọc thẳng bảng của context khác vì "nhanh hơn", và bạn quay lại với một database mà
ranh giới module chỉ còn trên giấy.

Đây cũng là tài liệu định nghĩa **một chiều hay hai chiều** cho mỗi quan hệ, quyết định
liệu một context có được đọc dữ liệu của context khác không.

## Bạn tạo ra

`docs/domain/cross-context-contracts.md`:

Mỗi hợp đồng một mục:

```
## Hợp đồng: <Tên context A> → <Tên context B>

**Dữ liệu đi theo hướng**: <A → B | B → A | hai chiều>
**Hình thức**: <gọi trực tiếp trong process | event | truy vấn đọc | hợp đồng publish/subscribe>

### Dữ liệu

| Thuật ngữ ở A | Thuật ngữ ở B | Có phải cùng nghĩa? |
|---|---|---|

### Hợp đồng

<Định nghĩa dữ liệu đi qua, ở mức khái niệm — không phải JSON schema>

### Điều khoản

- <điều context B được phép giả định gì về A>
- <điều A được phép thay đổi mà không phá B>
```

Câu hỏi gợi ý:

1. Với mỗi quan hệ ở T06: dữ liệu nào đi qua, theo hướng nào?
2. **Thuật ngữ nào mang hai nghĩa khác nhau ở hai bên?** Đó chính là chỗ cần một lớp dịch.
3. Context B có được đọc dữ liệu trực tiếp của context A không, hay phải qua hợp đồng? Nếu
   đọc trực tiếp thì bạn đang phá ranh giới — hãy nói rõ để đó là một quyết định có ý thức.
4. Khi context A đổi mô hình, context B cần biết không? Nếu có thì qua event hay kiểm tra
   tương thích?
5. Hợp đồng nào là **sẵn sàng để tách thành service riêng**? Đây là câu hỏi định hướng: một
   hợp đồng đã rõ và một chiều là ứng viên tách service đầu tiên.

## AI làm gì

AI hỏi từng hợp đồng một. Với mỗi cặp thuật ngữ AI hỏi:

- "Hai từ này có cùng nghĩa không? Nếu không, chỗ nào dịch?"
- "Nếu B nhận dữ liệu này và A đổi cách tính, B có hỏng không?"

AI đề xuất định dạng hợp đồng. AI ghi vào `docs/domain/cross-context-contracts.md`.

## Tiêu chí hoàn thành

- [ ] Mỗi quan hệ ở T06 có ít nhất một hợp đồng tương ứng
- [ ] Có bảng đối chiếu thuật ngữ giữa hai bên cho mỗi hợp đồng
- [ ] Có ít nhất một cặp thuật ngữ được ghi là **khác nghĩa**, kèm lớp dịch
- [ ] Mỗi hợp đồng ghi rõ điều khoản: B được giả định gì về A
- [ ] Có chỉ ra hợp đồng nào là ứng viên tách service, kèm lý do
- [ ] Không hợp đồng nào đòi hỏi context B đọc bảng của context A mà không nói rõ đó là
      quyết định có ý thức

> **Ghi chú (2026-11-19):** nghiệp vụ đổi sang đặt vé xem phim. Cặp thuật ngữ **khác nghĩa**
> mạnh nhất giờ là: "ghế" (của phòng — thuộc về nghiệp vụ phòng chiếu) và "ghế của suất
> chiếu" (một bản ghi trạng thái bán — thuộc về nghiệp vụ đặt vé). Cùng một từ, hai nghĩa, hai
> vùng ngôn ngữ → ứng viên rõ ràng cho `Anti-Corruption Layer`.
