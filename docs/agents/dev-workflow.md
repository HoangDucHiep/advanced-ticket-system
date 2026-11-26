# Quy trình làm ticket — Gate D

Mỗi ticket là **một branch, một PR**, cỡ 1–3 ngày. Thứ tự và milestone:
[`../plan/gate-d-implementation.md`](../plan/gate-d-implementation.md). Định dạng ticket:
[`issue-tracker.md`](issue-tracker.md).

## Vòng đời

| Bước | Ai | Làm gì |
|---|---|---|
| 1. Ready | AI | Kiểm Definition of Ready. Thiếu thì nói rõ thiếu gì, không bắt đầu |
| 2. Brief | AI | In brief của ticket ra chat (mục dưới) |
| 3. Branch | Người học | `git switch main && git pull && git switch -c <mã-thường>-<slug>` (vd `m1-04-hold-seats`) |
| 4. Test đỏ | AI (`pair-tdd`) | Viết test đỏ, commit `test(<scope>): …` lên branch; từng test hoặc nhóm nhỏ, dừng chờ người học |
| 5. Code | Người học | Viết code + test, commit nhỏ, thường xuyên |
| 6. PR | Người học | `git push -u origin <branch>`, mở PR theo template `.github/pull_request_template.md` |
| 7. Review | AI | `/code-review` so với `main`; mỗi nhận xét có `file:dòng`, mã `C-xx` / `Bx` / `#n`, mức **chặn merge** / **nên sửa** / **gợi ý**; không viết sẵn code sửa |
| 8. Sửa | Người học | Tới khi hết mục "chặn merge" |
| 9. Verify | AI | `dotnet build -c Release` 0 warning, `dotnet test`, chạy lại phép thử âm tính, soát secret trong diff, CI xanh |
| 10. Đóng | AI + người học | Tick ticket, `Status: done`, log học tập, 3 câu phỏng vấn; AI commit phần docs lên branch |
| 11. Merge | Người học | Squash merge trên GitHub |

## Brief — AI in ra ở bước 2

1. **Mục tiêu** — một câu, kèm FR và bài toán concurrency.
2. **Đọc trước** — đúng mục design cần đọc.
3. **Cây file** sẽ tạo / sửa, đường dẫn đầy đủ.
4. **Các bước** — mỗi bước: 📍 thư mục + lệnh terminal, 📄 đường dẫn file, *vì sao*. Khái niệm mới (C#,
   ASP.NET Core, Postgres) giải thích ngắn khi gặp lần đầu. Đưa chữ ký + khung, ghi rõ từng chỗ cần viết
   gì; không viết thân production code (trừ `ai-implements`).
5. **Test** — tên, khẳng định, "đỏ nếu…".
6. **Câu hỏi thiết kế** — 2–4 câu người học tự trả lời khi code; AI hỏi lại lúc review.

## Definition of Ready

- [ ] Mọi ticket trong `Blocked by:` đã merge; `main` xanh trên CI
- [ ] Mục design liên quan tồn tại và không mâu thuẫn với ticket (mâu thuẫn ⇒ AI sửa design/ticket trước)
- [ ] Endpoint, mã lỗi, bảng của ticket có trong `api-contract.md` / `data-model.md`

## Definition of Done

- [ ] Mọi tiêu chí "Done khi" của ticket đạt; test xanh
- [ ] Mỗi test quan trọng **đã thấy đỏ** bằng phép thử âm tính — ghi trong PR
- [ ] Build Release 0 warning; CI xanh trên PR; review hết mục "chặn merge"
- [ ] Migration (nếu có) chạy từ DB trống; không sửa migration đã merge
- [ ] Cấu hình mới qua Options + `ValidateOnStart`; không secret trong diff
- [ ] Code khớp `api-contract.md` / `data-model.md` (lệch ⇒ sửa một bên, ghi lý do trong PR)
- [ ] `docs/learning/log.md` có mục của ticket (template ở `docs/learning/README.md`), viết bằng lời của
      người học; AI hỏi 3 câu phỏng vấn, câu trả lời chưa chắc ⇒ mục "Còn mở"

**Milestone xong** khi mọi ticket đã merge, demo của milestone chạy được bằng test tự động, và người học
trả lời được câu hỏi phỏng vấn của các ticket mà không mở tài liệu.

## Quy ước git

| Thứ | Quy ước |
|---|---|
| `main` | Luôn xanh; mọi thay đổi code qua PR |
| Branch | `<mã ticket viết thường>-<slug>`: `m0-01-result-types`; docs: `docs/<slug>` |
| Commit | Conventional Commits, tiếng Anh: `feat(booking): hold seats all-or-nothing`. `type`: `feat` `fix` `test` `refactor` `docs` `ci` `chore` `perf`; `scope`: tên module thường, hoặc `shared` `host` `infra` `load` |
| Merge | Squash; tiêu đề PR = `<type>(<scope>): <mô tả> (<mã ticket>)` |
| Migration | Một migration mỗi ticket có đổi schema, tên theo việc: `AddUsers`, `AddShowtimeSeats` |

## Thứ tự review

1. **Đúng đắn**: invariant, transaction, khoá, race — đối chiếu `concurrency.md` của bài toán.
2. **Hợp đồng**: status, `code`, shape JSON theo `api-contract.md`; bảng theo `data-model.md`.
3. **Khuôn chung**: `building-blocks.md` B1–B12.
4. **Checklist**: `coding-conventions.md` C-01…C-32 và bảng smell.
5. **Test**: có chồng lấn thật không, assert trên trạng thái quan sát được không, đã thấy đỏ chưa.
