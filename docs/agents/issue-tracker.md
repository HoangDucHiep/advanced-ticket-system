# Issue tracker — ticket dạng Markdown

## Vị trí và mã

- Ticket: `.scratch/advanced-ticket-system/issues/<mã>-<slug>.md`, mỗi ticket một file.
- Mã theo milestone: `M<milestone>-<số>` (`M1-04`). Milestone và thứ tự:
  [`../plan/gate-d-implementation.md`](../plan/gate-d-implementation.md).
- Ticket mới chèn thêm vào milestone lấy số tiếp theo của milestone đó; thứ tự thực hiện nằm ở plan,
  không suy ra từ số.

## Định dạng

```markdown
# M1-04: <tiêu đề>

Mode: pair-tdd | you-implement-i-review | ai-implements | co-design
Status: ready | in-progress | in-review | done
Blocked by: M1-03, ...            (— nếu không có)
Traces to: FR-01, FR-02           (— nếu không có)
Concurrency: #1 #2                (— nếu không có)
Branch: m1-04-hold-seats

## Mục tiêu
1–3 câu: vì sao cần, mở ra điều gì.

## Phạm vi
**Trong:** hành vi cần có (chi tiết kỹ thuật → link mục design, không chép lại).
**Ngoài:** thứ cố tình để ticket khác (ghi mã ticket).

## Đọc trước
Link tới đúng mục canonical (`concurrency.md#…`, `api-contract.md#…`, `data-model.md#…`).

## Test
| Test | Khẳng định | Đỏ nếu… |
`pair-tdd`: AI viết các test này. `you-implement-i-review`: gợi ý, người học viết.

## Câu hỏi review
2–4 câu riêng của ticket mà AI sẽ hỏi.

## Done khi
- [ ] tiêu chí kiểm được (DoD chung ở `dev-workflow.md`)
```

Ticket **không chứa code** (không khối C#, SQL, JSON…) — mô tả hành vi, link tới mục design; chữ ký
và khung code chỉ xuất hiện trong brief AI in ra chat lúc làm ticket (`AGENTS.md`).

Vai trò theo `Mode:` định nghĩa ở [`AGENTS.md`](../../AGENTS.md). Quy trình làm:
[`dev-workflow.md`](dev-workflow.md).

## Khi ticket xong

Tick "Done khi", đặt `Status: done`, thêm mục vào `docs/learning/log.md`, cập nhật cột Ticket trong
`docs/plan/traceability.md` nếu ticket đổi phạm vi.

## Ánh xạ cho skill dùng chung

Skill nói "publish to the issue tracker" ⇒ tạo file mới theo định dạng trên. "Fetch the ticket" ⇒ đọc
file theo mã. Trạng thái triage của skill: xem [`triage-labels.md`](triage-labels.md).
