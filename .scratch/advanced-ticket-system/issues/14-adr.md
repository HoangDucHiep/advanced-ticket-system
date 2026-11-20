# 14: Bộ ADR

Mode: co-design
Gate: C
Layer: design
Blocked by: 10, 11, 12, 13
Traces to: —

## Vì sao có ticket này

ADR ghi lại **một quyết định và lý do của nó**. Ba điều kiện để viết ADR:

1. **Khó đảo ngược** — chi phí đổi ý sau này đáng kể.
2. **Ngạc nhiên nếu không có context** — người đọc code sau này sẽ tưởng bạn làm vậy vì
   không hiểu.
3. **Kết quả của một đánh đổi thật** — có các lựa chọn thật, và bạn chọn vì lý do cụ thể.

Không đạt cả ba thì đừng viết ADR — chỉ ghi vào `docs/design/` là đủ.

Tài liệu này cũng là thứ interviewer đọc để đánh giá **quy trình tư duy**, không chỉ kỹ năng
viết code. Một bộ ADR tốt nói lên rằng bạn biết mình đã quyết định điều gì và tại sao.

## Bạn tạo ra

Các file trong `docs/adr/`, đánh số liên tục từ `0001`. Format theo `docs/agents/domain.md`:
tiêu đề, 1–3 câu context + quyết định + lý do, rồi các mục tùy chọn (`Considered Options`,
`Consequences`), và frontmatter `status` khi quyết định còn được xem lại.

Danh sách ADR dự kiến — bạn xác nhận, bỏ, hoặc thêm:

| ADR | Nội dung | Trạng thái hiện tại |
|---|---|---|
| `0001-inventory-model` | Tier-based hay seat-based? | **`superseded by ADR-0003`** — đã thay thế 2026-11-19 |
| `0002-oversell-prevention` | Conditional UPDATE (đơn và nhiều dòng) hay `FOR UPDATE`? | `provisional` — so sánh bằng số đo ở Gate C |
| `0003-seat-based-inventory` | Mô hình seat-based: ghế của suất chiếu là đơn vị bán | `provisional` — phải xem lại ở Gate B |
| `0004-idempotency-strategy` | Cơ chế idempotency chung, hash, xử lý race | chưa có |
| `0005-hold-and-release` | Mô hình giữ ghế, thứ tự thao tác khi hết hạn | chưa có |
| `0006-cache-consistency` | Cache-aside, single-flight, invalidate theo ghế | chưa có |
| `0007-check-in-exactly-once` | Redis là fast-reject, constraint DB là guarantee | chưa có |
| `0008-modular-monolith` | Vì sao chưa tách service | chưa có |
| `0009-refresh-token-rotation` | Rotation + reuse detection | chưa có |

> **Vì sao `0003` không nằm ở cuối:** ADR-0003 đã tồn tại từ trước khi bộ ADR này được
> lên kế hoạch — vì nghiệp vụ đã đổi vào 2026-11-19 và cần một ADR riêng để ghi lại quyết
> định đó. Đánh số phải liên tục và không trùng; `0003` được giữ nguyên chỗ và các ADR còn
> lại dồn từ `0004` trở đi.

Câu hỏi gợi ý:

1. Trong 4 lớp tài liệu đã viết, quyết định nào **khó đảo ngược nhất**? Đó là ứng viên ADR
   mạnh nhất.
2. Quyết định nào bạn đã đổi ý trong quá trình làm? Ghi lại cả hướng đi lẫn hướng về — đó là
   loại ADR đáng đọc nhất.
3. Với ADR-0002 đang `provisional` và ADR-0003 đang `provisional`: sau khi có domain model và
   cơ chế concurrency đã chốt, chúng còn đúng không? Cần sửa, hay giữ nguyên và ghi lý do
   giữ?
4. Quyết định nào bạn **không** muốn viết ra vì nó trông quá hiển nhiên? (Chính những cái đó
   thường là cái đáng viết nhất.)
5. ADR-0001 đã bị thay thế — bạn có muốn ghi lại ở đâu đó **lý do** mô hình đếm số bị bỏ, hay
   giữ nguyên lịch sử trong chính ADR-0001 là đủ?

## AI làm gì

AI hỏi từng quyết định một:

- "Có lựa chọn thật nào khác không? Nếu không, đây không phải ADR."
- "Chi phí đổi ý sau này là bao nhiêu? Nếu nhỏ, bỏ qua."
- "Người đọc sau này sẽ tưởng gì khi thấy code này?"

AI **đề xuất nội dung**, bạn **xác nhận và chỉnh**. AI viết file ADR, nhưng chỉ những gì bạn
đã đồng ý.

Với ADR-0001 và ADR-0002, AI đọc lại phần domain và design đã viết và chỉ ra: chỗ nào nội
dung hiện tại **mâu thuẫn** với ADR cũ, cần sửa.

## Tiêu chí hoàn thành

- [ ] Có ít nhất 6 file ADR trong `docs/adr/`
- [ ] Mỗi ADR có context, quyết định, lý do; ADR nào có lựa chọn bị loại thì có mục
      `Considered Options`
- [ ] ADR nào có hệ quả không mong muốn thì có mục `Consequences`
- [ ] ADR-0001 và ADR-0002 đã được **xem lại** sau khi có domain và concurrency design, và
      trạng thái đã cập nhật (`provisional` → `accepted`, hoặc `superseded by ADR-NNNN`)
- [ ] ADR-0001 giữ nguyên lịch sử với `status: superseded by ADR-0003` (không bị xoá)
- [ ] Không ADR nào chỉ ghi "chọn X vì X tốt"
- [ ] Có ít nhất một ADR ghi lại một quyết định **đã đổi ý**
- [ ] Đánh số liên tục, không trùng, không thiếu
