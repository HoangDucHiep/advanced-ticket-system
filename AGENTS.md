# Hợp đồng làm việc — Advanced Ticket System

Đây là project **học tập** dùng để xây CV. Người học (bạn) là người viết production code.

> **Đổi hợp đồng (2026-11-25, theo yêu cầu người học):** ở các lớp **spec** (PRD, domain, design,
> ADR), **AI tự quyết mọi quyết định** và viết tài liệu sao cho đọc là hiểu: mỗi quyết định ghi
> "vì sao chọn / vì sao không chọn". Người học đọc spec đã hoàn thiện rồi implement theo. Ở lớp
> **code**, người học vẫn là người viết production code; AI review và viết test đỏ theo `Mode:`.
> Các ticket spec mode `co-design` / `explain` từ nay hiểu là: AI quyết, ghi nguồn gốc quyết định
> trung thực ("AI quyết định"), giảng trong tài liệu.

## Thứ tự đọc

Đọc theo thứ tự này, dừng lại ở từng gate. Không đọc trước, không viết trước.

1. `docs/README.md` — index + trạng thái gate hiện tại.
2. `docs/plan/00-roadmap.md` — plan tổng + ma trận truy vết.
3. Tài liệu của lớp đang mở (xem bảng dưới).
4. Ticket cụ thể trong `.scratch/advanced-ticket-system/issues/`.

| Lớp | Thư mục | Nội dung | Mở khi |
|---|---|---|---|
| 1 — PRD | `docs/prd/` | vấn đề, actors, requirements, non-requirements, user journeys, business rules | ngay từ đầu |
| 2 — Domain | `docs/domain/` | ubiquitous language, context map, aggregates, state machines, cross-context contracts | Gate A đóng |
| 3 — Design | `docs/design/` | architecture, data model, concurrency, api contract | Gate B đóng |
| 4 — Code | `.scratch/advanced-ticket-system/issues/` | ticket triển khai | Gate C đóng |

## Gate

Bốn gate nối tiếp. Mỗi gate có exit checklist trong `docs/README.md`.

- **Gate A** — PRD đủ để làm ra domain model.
- **Gate B** — Domain đủ để làm ra kiến trúc.
- **Gate C** — Design đủ để viết code.
- **Gate D** — Code đủ để đưa lên CV.

Quy tắc: **ticket của lớp N+1 không được mở khi exit checklist của lớp N chưa tick đủ.**
Khi bị chặn bởi gate, hãy nói rõ gate nào đang chặn và cần làm gì để mở — đừng tự nhảy.

## Vai trò theo `Mode:` của ticket

Mỗi ticket có trường `Mode:`. Nó quyết định AI làm gì và bạn làm gì.

| Mode | AI làm | Bạn làm |
|---|---|---|
| `explain` | Giảng trong tài liệu, kèm câu hỏi tự kiểm tra | Đọc |
| `co-design` | **Tự quyết** và viết tài liệu, mỗi quyết định có lý do + phương án bị loại | Đọc, hỏi lại chỗ chưa hiểu |
| `you-implement-i-review` | Review code bạn viết | Viết code + test |
| `pair-tdd` | Viết test đỏ | Viết code cho xanh |
| `ai-implements` | Làm phần boilerplate/infra | Review |

Ở bốn mode đầu, AI **không viết production code** trừ khi ticket ghi rõ cho phép.

## Quyết định phải có lý do

Từ 2026-11-25 AI quyết ở các lớp spec (xem đầu file). Thay cho "không tự bịa":

- Mỗi quyết định ghi **vì sao chọn** và **vì sao không chọn** phương án khác — tài liệu là giáo trình.
- Ghi trung thực nguồn gốc: quyết định của người học hay của AI.
- Chỉ ghi `(chưa xác định)` khi thật sự cần **số đo** chưa có (ví dụ ngưỡng hiệu năng trước T23).
- Khi phát hiện hai từ cùng nghĩa, chọn một từ chuẩn và ghi những từ nên tránh vào `GLOSSARY.md`.
- Quyết định khó đảo ngược thì **tạo ADR** ngay.

## Tài liệu phải trung thực

- Requirement đánh số `FR-nn` và được trích dẫn từ ticket qua trường `Traces to:`.
- Mọi claim về hiệu năng hoặc tính đúng đắn phải có test hoặc số đo đi kèm. Câu "xử lý concurrency" không có giá trị; câu "test này fail nếu bỏ `FOR UPDATE`" thì có.
- Đo baseline **trước** khi tối ưu. Tối ưu xong mới đo thì không có gì để so.

## Plan

- `docs/plan/00-roadmap.md` — plan tổng, luôn tồn tại, cập nhật dần.
- `docs/plan/gate-<X>-<tên>.md` — plan chi tiết của giai đoạn, **tạo đúng lúc bắt đầu giai đoạn đó**. Viết trước sẽ biến lại thành một đặc tả phẳng vô nghĩa.

## Ngôn ngữ

Tài liệu viết bằng **tiếng Việt**. Thuật ngữ kỹ thu giữ nguyên tiếng Anh (`aggregate`, `invariant`, `idempotency`).
Chuyển sang tiếng Anh ở tầng polish CV (ticket T26) nếu cần.

## Issue tracker

Issues nằm ở `.scratch/advanced-ticket-system/issues/`, mỗi ticket một file.
Convention chi tiết: `docs/agents/issue-tracker.md`.

## Skill nào dùng ở đâu

| Skill | Dùng khi |
|---|---|
| `/work-ticket` | Bắt đầu một ticket. Đây là flow chính. |
| `/grill-with-docs` | Cần làm rõ một quyết định thiết kế — tạo ADR + glossary cập nhật |
| `/domain-modeling` | Đang làm Lớp 2, cần chốt thuật ngữ |
| `/tdd` | Đang ở mode `pair-tdd` |
| `/code-review` | Review code bạn viết, hoặc review diff sau khi xong một nhóm ticket |
| `/research` | Cần tra tài liệu chính thức (Stripe, Postgres, ASP.NET Core) |
| `/prototype` | Câu hỏi thiết kế cần một thứ cụ thể để nhìn, không trả lời được trên giấy |

**Không dùng** `/implement` và `/implement-spec` ở project này — chúng được thiết kế để AI tự viết code và tự điều phối subagent, trái với hợp đồng trên.

**Không dùng** `/to-spec` và `/to-tickets` để tạo đặc tả phẳng. Pipeline chính của project là
`docs/prd → docs/domain → docs/design → .scratch/.../issues/`. Hai skill đó vẫn dùng được cho
việc nhỏ bên trong một gate.

## Domain docs

Đọc `docs/agents/domain.md` để biết quy ước đọc glossary và ADR. `GLOSSARY.md` được tạo ở
cuối Gate B — không có thì cứ tiếp tục, đừng dừng lại.
