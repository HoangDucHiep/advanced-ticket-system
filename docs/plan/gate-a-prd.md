# Plan Gate A — PRD

Gate A là cửa sổ đầu tiên của project. Mục tiêu: **làm ra một PRD đủ để bắt đầu phân tích
domain**, và không hơn thế.

## Entry criteria

- [x] `AGENTS.md` đã viết, quy tắc gate và `Mode:` đã rõ
- [x] `docs/README.md` tồn tại, exit checklist Gate A đã định nghĩa
- [x] `docs/plan/00-roadmap.md` tồn tại với stack và 12 bài toán concurrency
- [x] 4 ticket Gate A (T01–T04) đã có trong `.scratch/advanced-ticket-system/issues/`
- [x] Người học đã đọc `AGENTS.md` và hiểu hợp đồng vai trò

## Bước chi tiết

### Bước 1 — T01: Overview

Mở ticket T01 bằng `/work-ticket` (Mode `co-design`).

AI hỏi, bạn trả lời và **viết nội dung**. AI ghi vào `docs/prd/00-overview.md`.

Câu hỏi cốt lõi cần trả lời:

1. Bài toán nghiệp vụ này là gì, và ai gặp nó?
2. Ai là người dùng? Mỗi người dùng cần đạt được điều gì?
3. Vì sao chọn nghiệp vụ bán vé? Đã cân nhắc và bỏ phương án nào khác?
4. "Thành công" trông như thế nào, đo bằng cách nào?
5. **Bạn muốn học được gì từ dự án này?** — câu này quan trọng nhất, vì nó là tiêu chuẩn để
   cắt scope sau này.

Artifact: `docs/prd/00-overview.md`.

### Bước 2 — T02: Requirements

Mở ticket T02 (Mode `co-design`).

Artifact: `docs/prd/01-requirements.md` — danh sách `FR-nn`, mỗi cái có dòng "Kiểm chứng".

Ưu tiên các requirement **hành vi bị cấm**: không bán trùng ghế, đặt n ghế phải all-or-nothing,
không kẹt khi tranh ghế chồng nhau, không giữ ghế vô thời hạn, không trừ tiền hai lần. Chúng
là phần cốt lõi của dự án.

Sau khi xong: cập nhật ma trận truy vết trong `docs/plan/00-roadmap.md`.

### Bước 3 — T03: Non-requirements

Mở ticket T03 (Mode `co-design`).

Artifact: `docs/prd/02-non-requirements.md` — danh sách `NR-nn`.

Viết **trước khi** làm code. Tài liệu này là chốt chặn scope creep, và viết sau sẽ không còn
tác dụng gì.

### Bước 4 — T04: User journeys + business rules

Mở ticket T04 (Mode `co-design`).

Artifact: `docs/prd/03-user-journeys.md` và `docs/prd/04-business-rules.md`.

Với mỗi journey, đánh dấu bước nào **chạm tồn kho ghế** và bước nào **chạm tiền** — đây là bản
đồ các điểm tranh chấp, dùng lại ở T12.

## Câu hỏi cần trả lời trong gate này

- Hệ thống phục vụ bao nhiêu loại người dùng, và khác nhau ở đâu?
- "Thành công" đo được bằng con số nào?
- Hành vi nào **không được phép** xảy ra? (Đây là câu hỏi sinh ra phần lớn requirement.)
- Hệ thống cố tình loại bỏ những gì, và vì sao?
- Ở đâu trong hành trình người dùng có thể xảy ra tranh chấp?

## Artifact sẽ tạo ra

| File | Nội dung | Ticket |
|---|---|---|
| `docs/prd/00-overview.md` | Vấn đề, actors, jobs, success criteria | T01 |
| `docs/prd/01-requirements.md` | `FR-nn` kèm dòng kiểm chứng | T02 |
| `docs/prd/02-non-requirements.md` | `NR-nn` kèm lý do | T03 |
| `docs/prd/03-user-journeys.md` | Journey từng bước, đánh dấu điểm tranh chấp | T04 |
| `docs/prd/04-business-rules.md` | `BR-nn` | T04 |

## Exit checklist — đóng Gate A

> **Gate A đóng ngày 2026-11-20.** Toàn bộ checklist dưới đây đã tick. Nội dung thực tế:
> `FR-01..FR-39` (39 requirement, `FR-26` đã loại khỏi scope vì `NR-15`), `NR-01..NR-15`,
> `J-01..J-07`, `BR-01..BR-30`, 12 `SC`.
>
> **Kết quả:** PRD đủ để làm ra domain model → **Gate B được mở**. Bước tiếp theo: T05
> (ubiquitous language workshop).

- [x] `docs/prd/00-overview.md` — vấn đề, actors, jobs, success criteria
- [x] `docs/prd/01-requirements.md` — requirements đánh số `FR-nn`
- [x] `docs/prd/02-non-requirements.md` — những gì cố tình không làm, kèm lý do
- [x] `docs/prd/03-user-journeys.md` — đặt vé, mở bán phim hot, check-in từng bước
- [x] `docs/prd/04-business-rules.md` — luật nghiệp vụ
- [x] Mỗi actor trong overview có ít nhất một job trong user journeys
- [x] Mỗi `FR-nn` có thể truy vết tới một hành vi trong user journeys
- [x] Không từ khoá kỹ thu nào lọt vào các file ở Lớp 1
- [x] Mọi requirement có dòng "Kiểm chứng"
- [x] Ma trận truy vết trong `docs/plan/00-roadmap.md` đã điền các `FR-nn` tương ứng
- [x] `docs/learning/log.md` có mục tổng kết Gate A

## Điều chưa biết lúc mở Gate A

Ghi lại để biết sau này cần quay lại:

- ~~Mô hình tồn kho có cần seat-based không~~ → **đã trả lời 2026-11-19: CÓ**, xem ADR-0003.
  Câu hỏi còn lại ở Gate B: một suất chiếu (hàng trăm ghế) có phải là **một** aggregate không?
- Có cần context nào tách riêng cho Payment, hay Stripe chỉ là adapter ra ngoài?
- Số bài toán concurrency thực sự là 12, hay domain sẽ lộ ra bài toán thứ 13?
- Quy tắc giới hạn mua vé theo người: bao nhiêu vé, theo suất chiếu hay theo tổng? Đây là một
  invariant, và nó có thể không nằm trong cùng aggregate với tồn kho ghế — cần biết ở Gate B.
- Ghế `Blocked` (vệ sinh, dành riêng) ai quyết định, có bán lại online không?
