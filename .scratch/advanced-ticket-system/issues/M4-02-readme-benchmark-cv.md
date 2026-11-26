# M4-02: README, báo cáo benchmark, CV

Mode: co-design
Status: ready
Blocked by: M3-05, M4-01
Traces to: FR-30
Concurrency: —
Branch: m4-02-readme-benchmark-cv

## Mục tiêu
Người lạ đọc README 3 phút là hiểu hệ thống, và mỗi câu khoe đều có test hoặc con số đi kèm (`SC-12`). Đây là ticket
**đóng Gate D**.

## Phạm vi
**Trong:** `README.md` ở gốc repo gồm:
- Một dòng pitch + vấn đề cần giải (vì sao đặt vé theo ghế khó khi mở bán phim hot).
- Bảng **"14 bài toán concurrency"**: `bài toán | cơ chế | test chứng minh | link` — mỗi dòng trỏ tới mục trong
  [`concurrency.md`](../../../docs/design/concurrency.md) và đúng tên test (không có test ⇒ không có dòng).
- Sơ đồ kiến trúc (modular monolith, 6 module, Postgres, Redis, Stripe, outbox) — link
  [`architecture.md`](../../../docs/design/architecture.md).
- **Số liệu kèm điều kiện đo**, lấy từ `perf/report.md` (trước / sau cache, biến thể khoá ghế); không số nào không có điều kiện đo.
- Cách chạy: `docker compose up`, `dotnet test`, k6 (lệnh đúng, chạy được từ máy sạch).
- Stack; điểm nhấn ADR (link vài ADR đáng nói nhất); **bài học** (người học viết); **giới hạn đã biết** (trung thực: không offline check-in, không waiting room, v.v.).
- Kiểm cuối: **mọi mục của exit checklist Gate D** trong [`gate-d-implementation.md`](../../../docs/plan/gate-d-implementation.md) đã tick, kèm bằng chứng (link test / số đo).
- Cân nhắc bản README tiếng Anh cho CV (Q10 — người học quyết lúc này; nếu dịch, ghi vào `open-questions.md` và đóng Q10).

**Ngoài:**
- Số đo mới: nếu thiếu số, quay lại M3-05 chứ không ước lượng ở đây.
- Dashboard / alert → M3-06 (chỉ chụp màn hình vào README).
- Thay đổi code hoặc design: phát hiện lệch thì mở ticket / sửa tài liệu riêng.

## Đọc trước
- [`gate-d-implementation.md`](../../../docs/plan/gate-d-implementation.md) — exit checklist Gate D.
- `perf/report.md`, [`concurrency.md`](../../../docs/design/concurrency.md) — bảng "Tổng hợp".
- [`docs/adr/README.md`](../../../docs/adr/README.md); [`00-overview.md`](../../../docs/prd/00-overview.md) — SC-12.
- [`open-questions.md`](../../../docs/learning/open-questions.md) — Q10.

## Cách làm (co-design)
Người học viết nội dung. AI **hỏi**, ghi lại, và **chất vấn mọi claim thiếu test hoặc số đo** ("test nào đỏ nếu câu
này sai?", "con số này đo ở điều kiện nào?"). AI không tự viết bài học hay tự nâng số liệu.

## Test
| Kiểm | Khẳng định | Đỏ nếu… |
|---|---|---|
| Duyệt từng dòng bảng 14 bài toán | test được nêu **có thật** và xanh trong `dotnet test --filter Category=Concurrency` | tên test không tồn tại / đỏ |
| Duyệt từng con số trong README | khớp một dòng `perf/baseline.csv` + điều kiện đo trong `report.md` | số không truy ra nguồn |
| Làm theo mục "Cách chạy" trên máy / clone sạch | chạy được đến khi thấy kết quả | thiếu bước |
| Quét link | mọi link nội bộ (docs, ADR, test) mở được | link chết |
| Quét secret + lịch sử commit | không secret; repo sẵn sàng public | có secret |

## Câu hỏi review
- Một nhà tuyển dụng đọc 3 phút nhớ được điều gì? Câu đầu tiên có nói được *vì sao khó* chưa?
- Claim nào trong README bạn sẽ không bảo vệ được nếu bị hỏi "chứng minh bằng gì"?
- Giới hạn nào bạn chủ động nói ra trước khi người khác hỏi?

## Done khi
- [ ] README có đủ các mục ở trên, người ngoài đọc ~3 phút hiểu
- [ ] Mọi dòng bảng 14 bài toán và mọi con số đều truy được tới test / dòng đo
- [ ] Mọi mục exit checklist Gate D đã tick (hoặc ghi rõ vì sao chưa, kèm ticket)
- [ ] Quyết định về bản tiếng Anh ghi lại (Q10 đóng)
- [ ] CI xanh, không secret trong lịch sử, repo sẵn sàng public
