# Roadmap

## Mục tiêu

Một backend **đặt vé xem phim theo ghế** bằng ASP.NET Core, tập trung vào phần khó: concurrency, tính
đúng đắn dưới tranh chấp, idempotency, hiệu năng **đo được** — mỗi claim có test hoặc số đo đi kèm. Kết
quả là một repo public kể được câu chuyện: *bài toán nào, vì sao khó, giải thế nào, chứng minh ra sao,
đo được gì.*

## Nguyên tắc

1. **Spec trước code.** PRD → domain → design → code; mỗi lớp sinh ra lớp sau (Gate A–C đã đóng).
2. **Test là bằng chứng.** Mỗi bài toán concurrency có test **đỏ khi bỏ cơ chế**
   ([`../design/concurrency.md`](../design/concurrency.md)).
3. **Đo trước khi tối ưu.** Baseline trước, tối ưu sau, đo lại — cache chỉ thêm sau baseline.
4. **Walking skeleton trước, làm cứng sau.** Đi hết luồng đặt vé sớm để lộ rủi ro tích hợp, rồi làm cứng
   từng bài toán.
5. **Mỗi milestone kết thúc bằng một demo chạy được.** Dừng ở cuối milestone nào repo vẫn hoàn chỉnh.
6. **Không giả lập** thành phần thật trong test: Postgres, Redis (Testcontainers), Stripe test mode.

## Milestone

| Milestone | Kết quả | Demo |
|---|---|---|
| **M0 — Nền móng** | Skeleton, CI, khuôn lỗi chung, log, health | Request sai ⇒ problem+json có `traceId` tìm được trong log |
| **M1 — Walking skeleton** | Đi hết J-01: đăng ký → xem ghế → giữ ghế → thanh toán → vé | Test E2E `J01_happy_path` |
| **M2 — Đúng dưới tranh chấp** | 11 bài toán concurrency có test + phép thử âm tính | `dotnet test --filter Category=Concurrency` |
| **M3 — Đo, tối ưu, đo lại** | Baseline k6, cache, rate limit, số trước / sau | `perf/report.md` + dashboard Grafana |
| **M4 — Ra mắt** | Frontend người xem, README, CV | Người lạ đọc README 3 phút là hiểu |

Ticket, thứ tự, exit checklist: [`gate-d-implementation.md`](gate-d-implementation.md).
Truy vết requirement: [`traceability.md`](traceability.md).
