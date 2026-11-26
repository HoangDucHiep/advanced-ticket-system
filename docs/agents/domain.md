# Domain docs — cách đọc khi khám phá code

Trước khi đặt tên, viết test hay đề xuất thay đổi, đọc:

- **`GLOSSARY.md`** (root) — thuật ngữ, tên tiếng Anh dùng trong code, từ nên tránh.
- **`docs/adr/README.md`** — chỉ mục ADR; đọc ADR chạm vào khu vực đang làm.

## Từ vựng

- Dùng đúng thuật ngữ trong `GLOSSARY.md` ở tiêu đề, tên test, tên class. Không dùng từ glossary ghi
  là nên tránh.
- **Code dùng tên tiếng Anh** (class, biến, bảng, trạng thái, endpoint) — không dùng tiếng Việt, kể cả
  không dấu (`Ghe`, `DonDatVe`). Tài liệu dùng tiếng Việt, kèm tên tiếng Anh khi nhắc khái niệm có
  trong code.
- Khái niệm cần dùng mà glossary chưa có ⇒ hoặc đang dùng ngôn ngữ project không dùng, hoặc có khoảng
  trống thật: bổ sung glossary (AI quyết, theo `AGENTS.md`).

## Mâu thuẫn với ADR

Nói ra tường minh thay vì lặng lẽ làm khác: *"Mâu thuẫn ADR-0011 (…), đề xuất ADR mới vì…"*. ADR
không sửa thân; quyết định mới = ADR mới, ADR cũ chuyển `superseded by ADR-NNNN`.
