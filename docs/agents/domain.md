# Domain Docs

Cách các skill kỹ thuật đọc tài liệu domain của repo này khi khám phá codebase.

## Trước khi khám phá, đọc các file này

- **`GLOSSARY.md`** ở root repo, hoặc
- **`GLOSSARY-MAP.md`** ở root repo nếu có: nó trỏ tới một `GLOSSARY.md` cho mỗi context. Đọc
  những cái liên quan tới chủ đề đang làm.
- **`docs/adr/`**: đọc các ADR chạm vào khu vực bạn sắp làm việc. Trong repo multi-context,
  kiểm tra thêm `src/<context>/docs/adr/` cho quyết định riêng của context.
- **`docs/domain/ubiquitous-language.md`**: bản làm việc ở Lớp 2 — nguồn thuật ngữ trong
  lúc domain model chưa đóng.

Nếu bất kỳ file nào chưa tồn tại, **cứ tiếp tục, không báo**. Đừng đề xuất tạo chúng
trước. Skill `/domain-modeling` tạo chúng khi thuật ngữ và quyết định thực sự được chốt.

## Cấu trúc file

Repo này **single-context** (một `GLOSSARY.md` + `docs/adr/` ở root):

```
/
├── AGENTS.md
├── GLOSSARY.md              ← tạo ở cuối Gate B
├── docs/
│   ├── README.md            ← index + bảng gate
│   ├── adr/
│   │   ├── 0001-inventory-model.md          ← superseded by ADR-0003 (giữ lịch sử)
│   │   ├── 0002-oversell-prevention.md
│   │   ├── 0003-seat-based-inventory.md
│   │   ├── 0004-seat-hold-aggregate-boundary.md  ← superseded by ADR-0005
│   │   ├── 0005-showtime-seat-aggregate.md
│   │   ├── 0006-payment-expiry-refund-flow.md
│   │   ├── 0007-per-customer-showtime-quota.md
│   │   ├── 0008-cross-module-transaction.md
│   │   ├── 0009-module-communication.md
│   │   ├── 0010-payment-intent-states.md
│   │   ├── 0011-idempotency-in-business-transaction.md
│   │   ├── 0012-seat-map-cache-per-seat-versioned.md
│   │   ├── 0013-check-in-db-unique-only.md
│   │   ├── 0014-refresh-token-rotation-strict-reuse.md
│   │   └── 0015-take-over-expired-holds.md
│   ├── agents/              ← cấu hình cho các skill
│   ├── design/              ← Lớp 3
│   ├── domain/              ← Lớp 2
│   ├── learning/            ← nhật ký học tập
│   ├── plan/                ← plan tổng + plan giai đoạn
│   └── prd/                 ← Lớp 1
└── src/
```

Nếu repo về sau trở thành multi-context, chuyển sang:

```
/
├── GLOSSARY-MAP.md
├── docs/adr/                          ← quyết định toàn hệ thống
└── src/
    ├── ordering/
    │   ├── GLOSSARY.md
    │   └── docs/adr/                  ← quyết định riêng của context
    └── billing/
        ├── GLOSSARY.md
        └── docs/adr/
```

## Dùng đúng từ vựng của glossary

Khi output của bạn nhắc tới một khái niệm domain (trong tiêu đề issue, đề xuất refactor,
một giả thuyết, một tên test), hãy dùng đúng từ như định nghĩa trong `GLOSSARY.md`. Đừng
lỡ tay dùng từ đồng nghĩa mà glossary ghi rõ là nên tránh.

Nếu khái niệm bạn cần chưa có trong glossary, đó là tín hiệu: hoặc bạn đang dùng ngôn ngữ
mà project không dùng (cân nhắc lại), hoặc có một khoảng trống thật (ghi vào
`docs/learning/open-questions.md`).

### Code dùng tên tiếng Anh

Mỗi thuật ngữ trong `docs/domain/ubiquitous-language.md` có **hai** tên: tiếng Việt (để
trao đổi) và tiếng Anh (chuẩn trong code).

- **Trong code** — tên class, biến, bảng, trạng thái, endpoint — dùng **tên tiếng Anh** trong
  glossary. Không dùng tiếng Việt, kể cả bản transliterate không dấu (`Ghe`, `DonDatVe`).
- **Trong tài liệu** — có thể dùng tiếng Việt, và nên kèm tên tiếng Anh khi nhắc tới khái
  niệm sắt được dùng trong code.
- Nếu một từ tiếng Anh trùng nghĩa với thuật ngữ khác, dùng tên trong glossary và ghi từ đó
  vào `_Avoid_`.

## Chỉ ra xung đột với ADR

Nếu output của bạn mâu thuẫn với một ADR đã có, nói ra một cách tường minh thay vì lặng lẽ
ghi đè:

> _Mâu thuẫn ADR-0007 (event-sourced orders), nhưng đáng mở lại vì…_

## Trạng thái ADR

| Trạng thái | Nghĩa là |
|---|---|
| `provisional` | Quyết định tạm, **phải xem lại** ở gate ghi trong dòng `Revalidate at Gate` |
| `accepted` | Đã chốt, áp dụng |
| `superseded by ADR-NNNN` | Đã bị quyết định sau thay thế — giữ lại để có lịch sử |

`provisional` không phải "chưa quyết". Nó nghĩa là **đã đủ để code chạy, chưa đủ để khóa**.
Mọi ADR `provisional` phải được chốt hoặc đánh dấu superseded trước khi Gate D đóng.
