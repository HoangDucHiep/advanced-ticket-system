# Advanced Ticket System — hướng dẫn cho AI

Project **học tập** để xây CV: backend đặt vé xem phim theo ghế (ASP.NET Core .NET 10, modular monolith,
Postgres, Redis, Stripe). Trọng tâm: concurrency, tính đúng đắn, idempotency, hiệu năng đo được.

## Hợp đồng

- **Spec** (PRD, domain, design, ADR): **AI quyết định**. Mỗi quyết định ghi *vì sao chọn* và *vì sao
  không chọn* — tài liệu là giáo trình. Spec đã đóng (Gate A–C); khi phát hiện thiếu hoặc mâu thuẫn, AI
  sửa thẳng vào file canonical, quyết định khó đảo ngược thì tạo ADR mới.
- **Code**: **người học viết production code.** AI làm theo `Mode:` của ticket.

| Mode | AI làm | Người học làm |
|---|---|---|
| `you-implement-i-review` | In hướng dẫn từng bước, review | Viết code + test |
| `pair-tdd` | Viết test đỏ, in hướng dẫn, review | Viết code cho xanh |
| `ai-implements` | Viết boilerplate / hạ tầng | Review |
| `co-design` | Hỏi, ghi lại, phản biện (chỉ dùng cho README / CV) | Viết nội dung |

Trừ `ai-implements`, AI **không viết thân production code**: chỉ đưa chữ ký, khung, giải thích từng chỗ
cần viết. Hướng dẫn luôn có 📍 thư mục + lệnh terminal, 📄 đường dẫn file đầy đủ, và lý do.

## Làm một ticket

Quy trình đầy đủ (branch, PR, review, Definition of Done, đóng ticket):
[`docs/agents/dev-workflow.md`](docs/agents/dev-workflow.md). Tóm tắt luật cứng:

- Ticket ở `.scratch/advanced-ticket-system/issues/<mã>-<slug>.md`; thứ tự ở
  [`docs/plan/gate-d-implementation.md`](docs/plan/gate-d-implementation.md). Không mở ticket khi
  `Blocked by:` chưa xong.
- **Ticket mâu thuẫn với `docs/design/` ⇒ design thắng**; sửa ticket, báo người học.
- AI **không push, không merge**. AI chỉ commit khi người học yêu cầu hoặc khi đóng ticket (docs).

## Đọc gì ở đâu

| Cần | File (canonical) |
|---|---|
| Tổng quan, trạng thái gate | [`docs/README.md`](docs/README.md) |
| Thuật ngữ, tên trong code (tiếng Anh) | [`GLOSSARY.md`](GLOSSARY.md) |
| Module, phụ thuộc, stack | [`docs/design/architecture.md`](docs/design/architecture.md) |
| Bảng, constraint | [`docs/design/data-model.md`](docs/design/data-model.md) |
| 14 bài toán concurrency, SQL, test "đỏ nếu…" | [`docs/design/concurrency.md`](docs/design/concurrency.md) |
| Endpoint, mã lỗi | [`docs/design/api-contract.md`](docs/design/api-contract.md) |
| Khuôn code: Result, handler, transaction, repository | [`docs/design/building-blocks.md`](docs/design/building-blocks.md) |
| Checklist review C-01…C-32 | [`docs/design/coding-conventions.md`](docs/design/coding-conventions.md) |
| Quyết định khó đảo ngược | [`docs/adr/README.md`](docs/adr/README.md) |
| FR ↔ domain ↔ design ↔ ticket | [`docs/plan/traceability.md`](docs/plan/traceability.md) |

## Luật

- **Trung thực.** Claim về tính đúng đắn / hiệu năng phải có test hoặc số đo. "Xử lý concurrency" vô
  nghĩa; "test này đỏ nếu bỏ `FOR UPDATE`" mới có giá trị. Test chưa từng thấy đỏ thì chưa chứng minh gì.
- **Đo trước khi tối ưu.** Không có baseline thì không có tối ưu.
- **Mỗi sự thật một nơi.** Không chép nội dung design vào ticket hay chỗ khác — link tới mục canonical.
  Không ghi lịch sử ("sửa ngày…", gạch ngang) vào docs; lịch sử nằm ở git và ADR.
- **Từ vựng.** Dùng đúng tên trong `GLOSSARY.md`; code dùng tên tiếng Anh. Thấy hai từ cùng nghĩa ⇒ chọn
  một, ghi từ còn lại vào mục "tránh".
- **Bí mật.** Không commit `.env`, `secrets.json`, connection string, khoá Stripe/JWT.
- **Ngôn ngữ.** Tài liệu tiếng Việt, thuật ngữ kỹ thuật giữ tiếng Anh. Code, commit message tiếng Anh.

## Skill

| Skill | Khi |
|---|---|
| `/work-ticket` | Bắt đầu một ticket |
| `/tdd` | Ticket `pair-tdd` — vòng test đỏ |
| `/code-review` | Review branch so với `main`; đối chiếu `coding-conventions.md` + `building-blocks.md` |
| `/research` | Tra tài liệu chính thức (ASP.NET Core, Npgsql, Stripe, Redis) |
| `/prototype` | Câu hỏi thiết kế cần thứ chạy được để trả lời |
| `/grill-with-docs` | Cần chốt lại một quyết định thiết kế ⇒ ADR + GLOSSARY |

Skill trong `.agents/skills/` là bộ dùng chung, có thể chứa luật khác (ví dụ "chỉ ghi lời người học",
"đề xuất ADR, đừng tự tạo"). **Khi mâu thuẫn, file này và `dev-workflow.md` thắng.**
