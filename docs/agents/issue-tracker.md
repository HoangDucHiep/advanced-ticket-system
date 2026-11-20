# Issue tracker: Local Markdown

Issue của repo này sống dưới dạng file markdown trong `.scratch/`.

## Vị trí

- Feature: `.scratch/<feature-slug>/`
- Ticket: `.scratch/<feature-slug>/issues/<NN>-<slug>.md`, đánh số từ `01`, **mỗi ticket một
  file** — không gộp nhiều ticket vào một file.
- Feature hiện tại: `.scratch/advanced-ticket-system/`

## Convention metadata của ticket

Mỗi ticket file bắt đầu bằng một khối metadata, ngay dưới tiêu đề:

```markdown
# NN: <tiêu đề>

Mode: co-design | explain | you-implement-i-review | pair-tdd | ai-implements
Gate: A | B | C | D
Layer: prd | domain | design | code
Blocked by: NN, NN
Traces to: FR-01, FR-07
Concurrency problem: #1
```

| Trường | Ý nghĩa | Bắt buộc |
|---|---|---|
| `Mode:` | Vai trò AI khi làm ticket này. Xem bảng dưới. | Có |
| `Gate:` | Gate mà ticket thuộc về. Dùng để kiểm tra điều kiện mở. | Có |
| `Layer:` | Lớp tài liệu mà ticket đóng góp vào. | Có |
| `Blocked by:` | Số ticket phải xong trước. `—` nghĩa là không có. | Có |
| `Traces to:` | Các `FR-nn` mà ticket đáp ứng. Rỗng nếu ticket là tài liệu. | Nếu có |
| `Concurrency problem:` | Số bài toán concurrency mà ticket giải, theo bảng trong `docs/plan/00-roadmap.md`. | Nếu có |

### `Mode:` — vai trò AI

| Mode | AI làm | Bạn làm |
|---|---|---|
| `explain` | Giảng, rồi hỏi lại để kiểm tra bạn hiểu | Hỏi, ghi vào `docs/learning/log.md` |
| `co-design` | Hỏi từng quyết định một; ghi lại lời bạn | **Viết nội dung** |
| `you-implement-i-review` | Review code bạn viết | Viết code + test |
| `pair-tdd` | Viết test đỏ | Viết code cho xanh |
| `ai-implements` | Làm phần boilerplate/infra | Review |

Ở bốn mode đầu, AI **không viết production code** trừ khi ticket ghi rõ cho phép.

### Cấu trúc một ticket file

```markdown
# NN: <tiêu đề>

<khối metadata ở trên>

## Vì sao có ticket này
<Vì sao bước này cần làm, và nó mở ra điều gì cho phần sau.>

## Bạn tạo ra
<Artifact cụ thể sẽ tồn tại sau khi xong. Không mô tả từng bước code.>

## AI làm gì
<Vai trò của AI theo Mode ở trên.>

## Tiêu chí hoàn thành
- [ ] ...
```

## Trạng thái ticket

Triage state ghi bằng dòng `Status:` gần đầu file (xem `triage-labels.md`):

```
Status: needs-triage | needs-info | ready-for-agent | ready-for-human | wontfix | done
```

Với project học tập này, `ready-for-agent` **không** có nghĩa là "AI tự làm được". Nghĩa là
ticket đủ rõ để vào `/work-ticket`. Việc làm vẫn theo `Mode:`.

## Điều kiện mở ticket

Một ticket được mở khi **cả ba** điều kiện đúng:

1. `Gate:` của ticket đã mở — exit checklist của gate trước đã tick đủ (xem `docs/README.md`).
2. Mọi ticket trong `Blocked by:` đã hoàn thành.
3. Tài liệu của lớp trước đã tồn tại và có nội dung.

Thiếu một trong ba thì **dừng** và nói rõ cái gì đang chặn.

## Khi skill nói "publish to the issue tracker"

Tạo file mới dưới `.scratch/<feature-slug>/issues/` (tạo thư mục nếu chưa có).

## Khi skill nói "fetch the relevant ticket"

Đọc file tại đường dẫn được nêu. Người dùng thường truyền số ticket hoặc đường dẫn.

## Khi ticket xong

- Tick những tiêu chí hoàn thành đạt được.
- Thêm mục mới vào `docs/learning/log.md`.
- Cập nhật ma trận truy vết trong `docs/plan/00-roadmap.md` nếu ticket có `Traces to:`.

## Wayfinding operations

Dùng bởi `/wayfinder`. **Map** là một file, mỗi **child ticket** là một file.

- **Map**: `.scratch/<effort>/map.md`
- **Child ticket**: `.scratch/<effort>/issues/NN-<slug>.md`, có dòng `Type:` (`research` /
  `prototype` / `grilling` / `task`) và `Status:` (`claimed` / `resolved`).
- **Blocking**: dòng `Blocked by: NN, NN`. Ticket được coi là mở khi mọi file nó liệt kê đã
  `resolved`.
- **Frontier**: quét `.scratch/<effort>/issues/` lấy file đang mở, không bị chặn, chưa được
  claim; số nhỏ hơn thắng.
- **Claim**: đặt `Status: claimed` và lưu trước khi bắt đầu.
- **Resolve**: ghi câu trả lời dưới heading `## Answer`, đặt `Status: resolved`, rồi thêm
  context pointer vào mục Decisions-so-far của `map.md`.
