# Nhật ký học tập

Mỗi session làm việc với project để lại một mục ở đây. Mục này để session sau đọc là hiểu
ngay bối cảnh, không phải đọc lại toàn bộ lịch sử chat.

Cấu trúc một mục:

```markdown
## <YYYY-MM-DD> — <ticket / chủ đề>

**Học được**: <khái niệm mới>
**Làm được**: <thứ đã tồn tại sau session này>
**Vấp ở đâu**: <chỗ bị kẹt, và nguyên nhân thật>
**Còn mở**: <câu hỏi chưa giải>
```

---

## 2026-11-18 — Chốt kiến trúc tài liệu và hợp đồng làm việc

**Học được**:

- Tại sao "chốt hết đặc tả rồi làm một lần" là cách học sai cho một project CV. Vấn đề không
  nằm ở việc có hay không đặc tả — nó nằm ở **thứ tự**. Nếu bạn thiết kế cấu trúc module
  trước khi phân tích DDD, thì khi domain đổi, cấu trúc đó là chi phí, không phải nền.
- Aggregate boundary là **transaction boundary** là phạm vi của một lệnh SQL atomic. Chốt
  được ranh giới aggregate thì phần lớn câu hỏi "dùng cơ chế concurrency nào" tự động có
  đáp án.
- Một quyết định kỹ thu chỉ đáng ghi ADR khi nó đồng thời: khó đảo ngược, ngạc nhiên nếu
  không có context, và là kết quả của một đánh đổi thật. Viết ADR cho mọi thứ làm ADR trở
  nên vô nghĩa.
- Test concurrency chỉ có giá trị nếu nó **fail** khi cơ chế bị thay bằng cách sai. Đó là
  tiêu chuẩn để phân biệt một test có giá trị với một test rỗng.
- EF Core InMemory provider là cái bẫy của người mới: nó không có transaction, không có row
  lock, không enforce unique constraint — nên test viết trên đó **pass dù logic concurrency
  sai**.

**Làm được**:

- Cây tài liệu 4 lớp với cơ chế gate: `docs/prd/` → `docs/domain/` → `docs/design/` →
  `.scratch/advanced-ticket-system/issues/`.
- `AGENTS.md` viết lại thành hợp đồng giáo viên: quy tắc gate, quy tắc `Mode:`, quy tắc AI
  không tự bịa.
- `docs/README.md` — index và exit checklist của 4 gate.
- `docs/plan/00-roadmap.md` — plan tổng, ma trận truy vết, danh sách 26 ticket, log quyết định.
- 26 ticket với trường `Mode` / `Gate` / `Layer` / `Blocked by` / `Traces to`.
- Skill `/work-ticket` thay cho `/implement` và `/implement-spec`.
- ADR-0001 và ADR-0002 chuyển sang `status: provisional` kèm mục "Revalidate at Gate".

**Stack đã chốt**:

| Hạng mục | Quyết định |
|---|---|
| Runtime | .NET 10 (`net10.0`, LTS) |
| API | ASP.NET Core Controllers + `[ApiController]` |
| Data | EF Core (write, migrations) + Dapper (read nóng) + raw SQL (câu tranh chấp) |
| Database | PostgreSQL, schema riêng theo module, 1 DB |
| Cache | Redis (`StackExchange.Redis`) |
| Auth | JWT tự viết, refresh token rotation + reuse detection |
| Payments | Stripe PaymentIntent + webhook idempotent theo `event.id` |
| Check-in | QR ký HMAC-SHA256, verify offline, chống replay |
| Test | xUnit + Shouldly + Testcontainers (Postgres + Redis thật) |
| Load test | k6 (Docker image `grafana/k6`) |
| Observability | Serilog + OTel / Prometheus / Grafana |
| Infra | Docker Compose, GitHub Actions |

**10 bài toán concurrency** (giả định, sẽ rà lại ở Gate B):

| # | Bài toán | Cơ chế dự kiến |
|---|---|---|
| 1 | Oversell | atomic conditional `UPDATE` |
| 2 | Hold expiry đua với người mua mới | cùng đụng 1 row tier |
| 3 | Idempotency key race | `UNIQUE` + `ON CONFLICT DO NOTHING` |
| 4 | Refresh token reuse | rotation chain + `family_id` |
| 5 | Webhook trùng / sai thứ tự | idempotent theo `event.id` |
| 6 | Quét lại QR | Redis `SETNX` + DB `UNIQUE` |
| 7 | Cache stampede | single-flight lock |
| 8 | Cache stale giữa các instance | explicit invalidation |
| 9 | Flash sale cạn connection pool | admission control |
| 10 | Rate limit nhiều instance | Redis token bucket |

**Hợp đồng vai trò AI**:

| Mode | AI làm | Bạn làm |
|---|---|---|
| `explain` | Giảng, hỏi lại để kiểm tra | Hỏi, ghi vào nhật ký này |
| `co-design` | Hỏi từng quyết định một; ghi lại lời bạn | **Viết nội dung** |
| `you-implement-i-review` | Review code bạn viết | Viết code + test |
| `pair-tdd` | Viết test đỏ | Viết code cho xanh |
| `ai-implements` | Làm phần boilerplate/infra | Review |

Ở bốn mode đầu, AI không viết production code trừ khi ticket ghi rõ cho phép. AI không tự
bịa nội dung PRD hay domain — chỗ nào chưa có ý kiến của bạn thì ghi `(chưa xác định)`.

**Vấp ở đâu**:

- Đặc tả đầu tiên gộp PRD + DDD + kiến trúc + schema vào một file, và 12 ticket nhảy thẳng
  tới code. Nguyên nhân thật: **không có ranh giới nào buộc phải dừng lại**, nên AI coi
  "viết hết một lần" là tiết kiệm thời gian. Cách sửa không phải là "cảnh báo AI", mà là
  đưa quy tắc gate vào một file **luôn nằm trong context** của agent (`AGENTS.md`) và làm
  điều kiện mở ticket **kiểm tra được** bằng exit checklist, không kiểm tra bằng ý thức.

**Còn mở**:

- Mô hình tồn kho tier-based có đủ không, hay cần seat-based? → ADR-0001, Gate B.
- Conditional UPDATE hay `FOR UPDATE`? → ADR-0002, quyết bằng số đo ở T23.
- Mô hình domain thật sự có bao nhiêu bounded context? → T06.
- `reserved` và `sold` có cần tách bảng để giảm contention không? → T07/T11.
- Khi nào chuyển tài liệu sang tiếng Anh cho CV? → T26.

---

## 2026-11-18 — T01: Overview (Gate A)

**Học được**:

- **Job ≠ system behaviour.** Job là thứ actor *muốn* ("mua được vé trong flash sale"), phần
  hệ thống *làm* để đáp ứng nó là requirement. Trộn hai thứ này là cách PRD trở nên mơ hồ
  mà người đọc vẫn tưởng rõ ràng.
- **Success criteria phải sinh ra được phép đo.** "Hệ thống nhanh và ổn định" không sinh ra
  gì; "1000 request tranh 100 vé → đúng 100 thành công" thì sinh ra một test cụ thể. Tiêu
  chí không sinh ra được phép đo nào thì là văn hoa, và sẽ bị loại khỏi README ở tầng CV.
- **Ghi "sẽ chốt sau baseline" thay vì ghi con số cứng** là cách trung thực mà vẫn đo được.
  Ghi `p99 < 200ms` rồi đo ra `350ms` buộc bạn phải sửa PRD hoặc nói dối.
- **Actor là vai trò nghiệp vụ, không phải hệ thống bên ngoài.** Stripe là adapter, thuộc
  Lớp 3. Lẫn nó vào Lớp 1 sẽ kéo công nghệ vào tầng nghiệp vụ.
- **Job của một actor có thể là "hiểu vì sao mình thất bại"** — đây là job thật, và nó tạo ra
  những requirement mà nếu thiếu thì hệ thống vẫn "đúng" nhưng người dùng vẫn khó chịu.
- **Lớp 1 phải sạch từ khoá kỹ thu.** Lần đầu viết overview tôi đã lỡ dùng "webhook",
  "idempotency key", "connection pool", "cache" — và đó là vi phạm đúng tiêu chí của ticket.
  Nguyên nhân: khi bạn đã quen với một khái niệm kỹ thu, nó trở thành **cách nghĩ mặc định**,
  nên viết "hệ thống phải xử lý webhook" có vẻ tự nhiên hơn là "hệ thống phải xử lý kết quả
  thanh toán từ bên thứ ba". Cách sửa là dịch ngược: hỏi "nếu không có từ khoá này, tôi vẫn
  mô tả được hành vi đó không?" — nếu không thì đó là từ khoá lọt vào.

**Làm được**:

- `docs/prd/00-overview.md` — Vấn đề (4 nỗi đau), 4 actors, 17 jobs, 9 success criteria
  (SC-01..SC-09).
- Ticket T01 tick đủ tiêu chí; exit checklist Gate A tick mục đầu tiên.

**Vấp ở đâu**:

- Chỗ dễ nhất để làm văn hoa là success criteria. Áp lực bản thân muốn trông "chuyên nghiệp"
  khiến câu "hệ thống chịu được tải cao" trông hấp dẫn hơn câu "không có request nào timeout
  ở 500 req/s". Nguyên nhân thật: chưa tách được **mục tiêu** (điều tôi hứa) khỏi **kết quả
  đo** (thứ tôi biết sau khi đo). Tách hai thứ đó ra là cách thoát khỏi bẫy văn hoa.

**Còn mở**:

- Quy tắc giới hễn mua vé theo người (bao nhiêu vé, theo event hay theo tổng) → T04, có thể
  là một invariant tách khỏi aggregate tồn kho → ảnh hưởng T07.
- Ai là nguồn sự thật cho số "còn bao nhiêu vé" khi cache và kho dữ liệu lệch nhau → T07.
- Mốc thời gian cụ thể cho hold (10 phút) và hạn dùng vé → T04, chốt ở T13.

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-18 — T02: Requirements (Gate A)

> **Ghi chú (2026-11-19):** mục này ghi lại trạng thái tại thời điểm viết. Toàn bộ nội dung
> PRD đã được viết lại cho nghiệp vụ đặt vé xem phim — xem mục ngày 2026-11-19 bên dưới.

**Học được**:

- **"Hành vi bị cấm" và "hành vi bình thường" là hai loại requirement khác nhau, không phải
  hai phần của cùng một danh sách.** Hành vi bị cấm là *điều không bao giờ được xảy ra* —
  và chính vì vậy chúng là phần **chứng minh được** bằng test. "Hệ thống bán vé ổn" thì
  không chứng minh được gì; "hệ thống không bao giờ bán vượt số vé có sẵn" thì có. Vì vậy tôi
  đặt nhóm hành vi bị cấm lên đầu file, không phải cuối.
- **Một FR = một hành vi.** Khi tôi viết "không oversell **và** không cấp vé chưa thanh toán",
  đó là hai requirement bị gộp. Dấu hiệu nhận ra: nếu một requirement có thể tách ra mà mỗi
  nửa vẫn đứng vững, thì nó là hai. Nhưng ngược lại — cứ tách mãi thì mất ý nghĩa, nên
  tiêu chí là **tách được thì tách, không tách được thì để nguyên và nói rõ**.
- **Requirement không được chứa giải pháp.** "Dùng một câu cập nhật có điều kiện" là quyết
  định thiết kế (Lớp 3). Ở Lớp 1 chỉ được nói "hệ thống không bao giờ bán vượt số vé có sẵn".
  Cách tự kiểm: nếu thay công nghệ mà câu đó vẫn đúng, thì nó là requirement.
- **FR trùng lặp phải gộp chứ không nhân bản.** Yêu cầu "người mua chỉ xem vé của mình" (nhóm
  phân quyền) và "không xem đơn của người khác" (nhóm hành vi bị cấm) là **cùng một hành vi**
  chỉ khác góc nhìn. Hai FR cho cùng một hành vi sẽ tạo hai chỗ để sửa, và một chỗ sẽ bị
  quên.
- **"Làm sau" vẫn phải ghi đầy đủ, chỉ đánh dấu.** Huỷ sự kiện + hoàn tiền (FR-21) được
  ghi như một requirement bình thường kèm ghi chú "thực hiện sau T22". Xoá nó đi thì ma
  trận truy vết sẽ hở; ghi "chưa xác định" thì người đọc tưởng là ta chưa nghĩ ra.

**Làm được**:

- `docs/prd/01-requirements.md` — 35 requirement `FR-01..FR-35` trong 8 nhóm, mỗi cái có dòng
  `Kiểm chứng`, actor, job, SC; kèm bảng truy vết và bảng đối chiếu 9 SC.
- Ticket T02 tick đủ tiêu chí; exit checklist Gate A tick mục thứ hai.
- Ma trận truy vết trong `docs/plan/00-roadmap.md` có đủ 35 dòng `FR-nn`; log quyết định
  thêm 3 quyết định.

**Vấp ở đâu**:

- Khi chọn "làm thật" cho cả thông báo hết hạn lẫn hoàn tiền, hệ quả kiến trúc bị kéo theo:
  thông báo cần hàng đợi bất đồng bộ, hoàn tiền cần idempotency. Nguyên nhân thật: ở đầu
  session tôi chỉ nghĩ đến "làm thật hay làm vờ", chưa nghĩ đến **hệ quả kéo theo**. Cách
  sửa: khi chốt một hành vi phức tạp, ghi luôn hệ quả vào PRD như một quyết định, đừng giấu
  nó rồi hy vọng Lớp 3 tự xử lý.

**Còn mở**:

- Giới hạn vé mỗi người là invariant **tách khỏi tồn kho** → ảnh hưởng ranh giới aggregate ở
  T07.
- Cơ chế hàng đợi cho thông báo sắp hết hạn → ảnh hưởng T10/T21.
- Cơ chế hoàn tiền (idempotency, đối soát) → ảnh hưởng T20.
- Quy tắc giới hạn mua: tính theo từng sự kiện hay theo tổng → T04.

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-19 — Pivot: bán vé sự kiện → đặt vé xem phim (seat-based)

**Bối cảnh**: giữa các session, người học nhớ ra một điều quan trọng: hệ thống bán vé sự kiện
chưa có **chỗ ngồi cụ thể**, trong khi nghiệp vụ họ muốn học (bán vé xem phim) thì có. Nghiệp
vụ được đổi hoàn toàn sang **đặt vé xem phim theo ghế**, và mô hình tồn kho đổi từ
tier-based sang **seat-based**.

**Học được**:

- **Đổi nghiệp vụ sớm là rẻ; đổi muộn là đắt.** Tại thời điểm này mới có Lớp 1 (PRD) và
  hai ADR `provisional` — chưa có domain, chưa có design, chưa có một dòng code. Toàn bộ
  chi phí là viết lại ~20 file tài liệu. Nếu đợi thêm 2 tháng, chi phí là viết lại schema,
  migration, test và cả những test đã xanh. Nguyên tắc: **nghiệp vụ là giả định, và giả định
  rẻ nhất khi nó còn ở dạng chữ**. Đó cũng là lý do Gate A tồn tại — nó là chỗ để phát hiện
  giả định sai *trước khi* nó thành code.
- **Một mô hình tồn kho quyết định bài toán concurrency nào xuất hiện.** Tier-based cho
  bài toán "giảm một số đếm trên một dòng" — quen thuộc, và ít giá trị. Seat-based cho bài
  toán **"giữ n ghế all-or-nothing"** và **"tránh deadlock khi tranh ghế chồng nhau"** — khó
  hơn nhiều, và là loại bài toán ít CV nào có. Cùng một nghiệp vụ, hai mức độ khó hoàn toàn
  khác nhau, chỉ vì cách bạn mô hình hóa nó.
- **Seat-based không thay mô hình cũ, nó cộng vào.** Ghế phải thuộc một **loại ghế** →
  vẫn cần khái niệm "nhóm ghế theo giá", chỉ là nhóm đó không mang `capacity` nữa. Bài học: khi
  đổi mô hình, đừng vứt bỏ cái cũ một cách máy móc — hãy hỏi "phần nào của mô hình cũ vẫn
  đúng". Và chọn **một từ chuẩn**: gọi là "loại ghế" (Standard / Couple / VIP), không dùng
  "hạng vé" — vì "hạng vé" đã mang nghĩa cũ.
- **ADR cũ không bị xoá, nó bị thay thế.** ADR-0001 được đánh dấu
  `superseded by ADR-0003` và giữ nguyên lịch sử. Xoá file là mất câu chuyện: "chúng ta đã
  thử mô hình đếm số, nó không đủ, và đây là lý do" — câu chuyện đó có giá trị hơn một file
  đúng ngay từ đầu.
- **"Còn mở" trong ADR là một lời hứa với tương lai.** ADR-0001 đã ghi sẵn: *"Nếu nghiệp vụ
  sau này cần chỗ ngồi cụ thể, đây là ADR phải được mở lại."* Và nó đã xảy ra đúng như vậy.
  Viết ADR khi chưa chắc sẽ tốt, miễn là viết rõ **điều gì sẽ khiến nó bị mở lại**.

**Làm được**:

- `docs/prd/00-overview.md` và `docs/prd/01-requirements.md` viết lại cho nghiệp vụ mới:
  4 actor, 18 job, 12 SC, **38 FR** (`FR-01..FR-38`).
- `docs/adr/0003-seat-based-inventory.md` (mới) — seat-based, `provisional`, có mục "Revalidate
  at Gate B" với 3 câu hỏi cụ thể về aggregate.
- `docs/adr/0001-inventory-model.md` → `superseded by ADR-0003`, giữ lịch sử và đánh dấu câu
  hỏi nào đã được trả lời.
- `docs/adr/0002-oversell-prevention.md` — bổ sung biến thể nhiều dòng, thứ tự khoá, và cảnh
  báo rõ: *thứ tự `UPDATE` không được Postgres bảo đảm, phải kiểm chứng bằng test chứ không
  được coi là hiển nhiên*.
- Bảng **12 bài toán concurrency** trong roadmap (thêm #2 all-or-nothing, #3 deadlock, #10
  invalidate theo ghế) và ma trận truy vết 38 dòng.

**Vấp ở đâu**:

- Ban đầu tôi định giữ nguyên nghiệp vụ "sự kiện" và chỉ **thêm** ghế vào. Đó là lựa chọn
  sai, và nó hiện ra khi tôi cố viết lại `docs/prd/00-overview.md`: mọi câu đều cần chỉnh,
  và những chỗ chỉnh được đều là chỗ không tự nhiên — "hạng vé có ghế" là khái niệm không có
  ngoài đời. Nguyên nhân thật: **thêm đặc tả vào một mô hình sai không làm mô hình đúng**,
  nó chỉ làm mô hình sai khó nhận ra hơn. Khi bạn phải vật lộn với từ ngữ, đó là tín hiệu
  mô hình sai — hãy đổi mô hình, đừng sửa từ ngữ.
- Lần đầu viết PRD mới, tôi vẫn còn dùng "hạng vé" và "đơn hàng" trong vài chỗ. Nguyên nhân:
  **ngôn ngữ cũ đã bám vào cách nghĩ**. Cách sửa là dùng bảng từ vựng cũ → mới và đọc lại
  từng câu sau khi viết xong, tìm từ nào không khớp.
- Khi đồng bộ ma trận truy vết bằng script, tôi đã **viết lại cả file** từ một phần trích
  regex — và làm mất toàn bộ phần nội dung ngoài phần đó. Bài học: script sửa file phải
  **đọc → sửa → ghi lại toàn bộ file**, không được ghi ra một mảnh. Và trước khi tin, phải
  đọc lại file sau khi sửa.

**Còn mở**:

- Một suất chiếu có hàng trăm ghế — aggregate có phải chia nhỏ không? → Q12, T07.
- Giá theo loại ghế có đổi giữa các suất của cùng một phim không? → Q13, T07.
- Ghế `Blocked` (vệ sinh, dành riêng) ai quyết định, có bán lại online không? → Q14, T07.
- Cơ chế hàng đợi cho thông báo sắp hết hạn → ảnh hưởng T10/T21.
- Cơ chế hoàn tiền (idempotency, đối soát) → ảnh hưởng T20.

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-20 — Đóng Gate A: non-requirements, user journeys, business rules

**Bối cảnh**: Gate A còn thiếu 3 file Lớp 1. Phiên này làm T03 (`02-non-requirements.md`)
và T04 (`03-user-journeys.md` + `04-business-rules.md`) trong mode `co-design`, rồi đóng gate.

**Đã học**:

- **Non-requirements là một tài liệu có giá trị CV trực tiếp.** Câu hỏi "còn gì nữa không?"
  của interviewer được trả lời bằng câu "tôi đã chủ động loại bỏ X vì lý do Y" — mạnh hơn
  hẳn "chưa làm tới". Nhưng nó chỉ có giá trị nếu **mỗi mục có một lý do cụ thể**. Một danh
  sách 15 mục đều ghi "không quan trọng" thì tệ hơn là không có danh sách.
- **Phân loại lý do loại bỏ là thứ làm danh sách trở nên trung thực.** Ba loại: `nghiệp vụ`
  (nằm ngoài mô hình đã chốt), `thời gian` (làm được nhưng không đáng), `kỹ thu / quy mô`
  (không làm được trong scope một project học tập). Ba loại này có hậu quả khác nhau: loại
  thứ nhất thì **không bao giờ nên làm** trong mô hình này, loại thứ hai có thể làm sau,
  loại thứ ba cần kiến trúc khác.
- **Đừng viết non-requirements như một danh sách "không làm UI".** Đó là bài học từ ticket
  T03: một danh sách toàn những thứ giao diện bỏ qua là danh sách vô nghĩa, vì nó không
  nói gì về **ranh giới mô hình**. Mục đáng giá nhất ở đây là `NR-01` — không có mô hình vé
  theo hạng — vì nó nói rõ điều gì **không thể quay lại** mà không phải đổi cả kiến trúc.
- **Cắt scope và ghi lại số hiệu đã dùng là hai việc khác nhau.** Khi `FR-26` bị loại khỏi
  scope, tôi đã **giữ lại số `FR-26`** thay vì đánh số lại toàn bộ. Đánh số lại sẽ phá vỡ
  mọi truy vết đã viết ở roadmap, learning log, và các tài liệu khác — và số hiệu đã từng
  xuất hiện là **một đối tượng có tham chiếu**, không phải một nhãn tuần tự.
- **Giới hạn số lượng có hai tầng khác nhau, và cần cả hai.** `BR-06` (6 ghế mỗi đơn) chặn
  *một lần đặt* quá lớn — vì giữ nhiều ghế là thao tác khoá nhiều dòng. `BR-08` (10 ghế mỗi
  người mỗi suất, cộng dồn qua các đơn) chặn *một người* tích luỹ — vì mua số lượng lớn rồi
  chuyển nhượng ra ngoài sẽ khiến người thật không vào được xem phim. Khi tôi đặt 10 ghế cho
  cả hai, `BR-08` trở nên bất khả thi (không ai vượt được 4 ghế nếu đơn tối đa 10), và
  `FR-09` sẽ chết không ai test được. Bài học: **một invariant phải còn khả thi để vi phạm**
  mới test được.
- **Quy tắc đếm ghế không nhất thiết trùng với quy tắc sinh vé.** Ghế đôi tính là **2 ghế**
  khi đếm giới hạn (vì nó chiếm chỗ của 2 người), nhưng sinh ra **1 vé** (vì đó là một chỗ
  ngồi). Đây là hai câu hỏi khác nhau và trộn chúng sẽ sinh ra lỗi đếm.
- **Lỗ hổng nghiệp vụ nằm ở ranh giới giữa hai thời điểm.** Thanh toán đến muộn sau khi đơn
  hết hạn 15 phút là tình huống mà cả `FR-04` lẫn `FR-06` đều không phủ: tiền **đã bị lấy**
  nhưng vé **không được cấp**. Phải thêm `FR-39` (hoàn tiền tự động toàn bộ) thì mọi tổng
  tiền mới khớp. Nguyên tắc: khi viết luật, hãy hỏi "hai thứ này xảy ra ở **hai thời điểm
  khác nhau** thì sao" — đó là chỗ lỗ hổng của các luật viết riêng lẻ.
- **Không giả lập là một quyết định có lý do, không phải thiếu thời gian.** Người học chốt
  làm thật email, thanh toán, và mã QR. Lý do sâu hơn: **test concurrency chỉ có giá trị khi
  đường đi thật đã được đi qua**. Một webhook giả lập không bao giờ tạo ra tình huống "đến lặp
  ba lần" — mà đó đúng là tình huống cần chứng minh. Việc giả lập làm **test yếu đi một cách
  âm thầm**, và đó là loại lỗi tệ nhất: test vẫn xanh.
- **Mark journey bằng điểm chạm tài nguyên là cách tạo bản đồ tranh chấp mà không cần biết
  cơ chế.** Chỉ cần biết "bước này có **ghi** vào tồn kho ghế không" và "bước này có **ghi**
  vào tiền không", đã dựng được bản đồ điểm tranh chấp mà Lớp 3 dùng để chọn cơ chế. Nhãn
  `[GHẾ]` / `[TIỀN]` là đủ — đừng vẽ sơ đồ luồng ở Lớp 1.

**Làm được**:

- `docs/prd/02-non-requirements.md` — **15 mục** (`NR-01..NR-15`) chia 4 nhóm theo lý do,
  mỗi mục có "Lý do" + "Nếu sau này cần". Có mục riêng nói rõ **không có phần nào được làm
  giả**, và mục "Những thứ **KHÔNG** nằm trong danh sách này" để tránh hiểu nhầm.
- `docs/prd/03-user-journeys.md` — **7 journey** (`J-01..J-07`), đánh dấu `[GHẾ]` / `[TIỀN]`,
  bảng truy vết 39 FR → journey, và **bản đồ điểm tranh chấp** 12 dòng ở cuối file.
- `docs/prd/04-business-rules.md` — **30 luật** (`BR-01..BR-30`), mỗi luật có "Ràng buộc" và
  "Vi phạm thì", kèm bảng luật → FR → journey phủ hết 38 requirement còn trong scope.
- `FR-26` loại khỏi scope (số hiệu giữ lại), **thêm `FR-39`** cho hoàn tiền.
- `FR-09` và `FR-16` cập nhật lại cho khớp luật mới (giới hạn 6/10 ghế, giá theo từng ghế).
- Cập nhật `docs/prd/01-requirements.md` (bảng truy vết + bảng đối chiếu SC), `docs/README.md`,
  `docs/plan/gate-a-prd.md`, `docs/plan/00-roadmap.md` (log quyết định + câu hỏi đã đóng),
  `docs/prd/README.md`, `docs/domain/README.md`.

**Vấp ở đâu**:

- Tôi đã **đề xuất một giới hạn vé theo số vé** ở T04, trong khi người học nói rõ "số ghế đôi
  tính là 2 ghế". Nguyên nhân: tôi đọc `FR-09` cũ ("giới hạn số vé") theo thói quen mô hình
  cũ — trong đó vé là đơn vị. Bài học: **đọc lại requirement cũ sau khi mô hình đổi**, vì
  câu chữ của nó vẫn mang ngôn ngữ của mô hình đã bị thay thế.
- Tôi **lỡ thêm một requirement** (`FR-24b`) vào giữa Nhóm D, làm sai thứ tự mã hiệu. Tôi
  nhận ra ngay và hoàn tác, nhưng cách đúng là đặt mã kế tiếp (`FR-39`) ở **cuối danh sách**
  và cập nhật bảng truy vết — không chèn giữa.
- Người học chưa nắm phần check-in, nên tôi phải **giảng trước rồi hỏi lại** thay vì đưa
  lựa chọn. Đây là đúng tinh thần `co-design`: nếu người học chưa biết đáp án thì dạy trước,
  đừng chỉ đưa lựa chọn để họ bấm bừa.

**Còn mở**:

- Một suất chiếu có hàng trăm ghế — aggregate có phải chia nhỏ không? → Q12, T07.
- Giá theo loại ghế có đổi giữa các suất của cùng một phim không? → Q13, T07.
- `FR-09` (giới hạn ghế mỗi người mỗi suất) có nằm trong **cùng aggregate** với tồn kho ghế
  không? Nó đếm theo người, còn tồn kho ghế đếm theo ghế — hai tiêu chí đếm khác nhau
  → T07.
- Cơ chế hàng đợi cho thông báo sắp hạn → T10/T21.
- Cơ chế hoàn tiền (idempotency, đối soát) → T20.

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-20 — T05: Ubiquitous language workshop (Gate B)

**Bối cảnh**: mở Gate B. Ticket T05 (mode `explain`) — giảng về thuật ngữ, không viết production
code. Artifact: `docs/domain/ubiquitous-language.md`, 27 thuật ngữ trong 6 nhóm.

**Đã học**:

- **"Ghế" và "ghế suất chiếu" là hai thứ, và đây là chỗ dễ nhầm nhất của cả dự án.** `Ghế` thuộc
  về phòng, tĩnh, dùng chung cho mọi suất. `Ghế suất chiếu` thuộc về một suất cụ thể, có trạng
  thái riêng, và là đơn vị bán. Câu hỏi bắt buộc phải trả lời khác nhau: "phòng này có bao nhiêu
  ghế" (đếm ghế phòng) vs "suất này còn ghế nào trống" (xem ghế suất chiếu). Dùng chung một từ
  thì người đọc tự hiểu một người đã mua D7 thì D7 hết ở *mọi* suất — và hệ thống báo lỗi sai.
- **Ba cách đếm ghế, ba câu hỏi khác nhau.** `Tổng số ghế phòng` (100) ≠ `Số ghế còn trống`
  (chỉ `Trống`) ≠ `Số ghế còn bán online` (`Trống` + `Đang giữ` + `Đã bán`, không tính `Bị
  chặn`). Người xem thấy cái thứ ba. Ghế `Đang giữ` không thuộc con số nào cho người xem — nó
  không trống, nhưng cũng không đặt được.
- **Aggregate = ranh giới mà mọi thay đổi phải cùng thành công.** Câu hỏi quyết định: *"có thay
  đổi nào mà nếu chỉ một phần được thay đổi thì hệ thống thành sai không?"* Giữ ghế = đổi trạng
  thái ghế + đổi trạng thái đơn cùng lúc; tách ra thì một phần đổi là sai → chung ranh giới.
- **Ranh giới aggregate nằm ở giữa "giữ ghế" và "thanh toán".** Hai đó không phải một việc:
  thanh toán đến vài giây tới vài giờ sau. Gộp làm một aggregate thì một transaction phải mở
  suốt thời gian chờ người xem bấm "thanh toán" — không thể. Phần trước là một khối, phần sau là
  khối khác.
- **Aggregate boundary là transaction boundary là phạm vi một lệnh SQL atomic.** Chốt được ở
  T07 thì Lớp 3 gần như chỉ còn là hệ quả kỹ thu. Tách aggregate thì phải phối hợp hai
  transaction → outbox, saga, compensating transaction — phức tạp hơn và sinh bài toán mới.
- **Value Object bất biến và so sánh bằng giá trị.** `Loại ghế Standard giá 90k trong suất 19h` —
  đổi giá là *tạo VO mới*, không phải sửa. Hai VO bằng nhau thì bằng nhau thật.

**Làm được**:

- `docs/domain/ubiquitous-language.md` — 27 thuật ngữ trong 6 nhóm, mỗi mục có định nghĩa +
  `_Avoid_`. Kèm mục "Các cặp thuật ngữ dễ nhầm" và mục "Ranh giới aggregate".
- **Sửa mâu thuẫn PRD phát hiện trong buổi này:** `FR-23` nói ba con số cộng lại bằng tổng số
  ghế phòng, nhưng `BR-11` nói ghế `Bị chặn` vẫn tính vào tổng → mâu thuẫn. Đã sửa `FR-23`
  thành **bốn** con số (trống / đang giữ / đã bán / bị chặn), kèm ghi chú phân biệt con số
  người xem thấy (`FR-13`). Đồng thời làm rõ thêm trong `BR-11`.

**Vấp ở đâu**:

- **Câu hỏi kiểm tra của tôi sai, và người học bắt được.** Tôi hỏi "mua 4 ghế lúc 20:00, đến
  20:12 chưa thanh toán" trong khi 20:15 mới hết hạn — nên 20:12 ghế *vẫn còn được giữ*. Người
  học đúng khi chỉ ra. Bài học: **tự tính lại các con số trong câu hỏi trước khi hỏi**; một câu
  hỏi kiểm tra có giá trị phải có đáp án duy nhất, và đáp án đó phải đúng ngay ở thời điểm hỏi.
- **Tôi nhầm mức "ghế của phòng" với "ghế của suất chiếu" khi hỏi câu 3.** Người học trả lời
  theo đúng nghĩa "ghế của phòng" (nhiều suất chiếu dùng chung) — và câu trả lời đó đúng cho
  câu hỏi đó, nhưng sai cho câu hỏi tôi định hỏi. Phải hỏi lại, và phải **nói rõ tầng nào đang
  hỏi về**. Đây đúng là bài học cốt lõi của ticket này: người học không sai, **câu hỏi sai**.
- Tôi hỏi 4 nhóm thuật ngữ cùng lúc thì người học trả lời "chưa chắc" — họ cần **một câu hỏi
  một lúc**, và cần giảng bằng ví dụ cụ thể trước khi hỏi về ranh giới aggregate.

**Còn mở**:

- Một suất chiếu có hàng trăm ghế — có nên là một aggregate không? → T07, ADR-0003.
- `FR-09` (giới hạn ghế mỗi người mỗi suất) có nằm trong cùng aggregate với tồn kho ghế
  không? Nó đếm theo **người**, tồn kho ghế đếm theo **ghế** → T07.
- Ranh giới aggregate quanh **thanh toán và hoàn tiền** → T08 (state machines).
- Check-in thuộc context nào — cùng với vé, hay tách riêng? → T06.

Xem thêm: [`open-questions.md`](open-questions.md).
### Bổ sung (sau T05) — Thêm tên tiếng Anh cho mọi thuật ngữ

Người học nói rõ: **code viết bằng tiếng Anh**, không dùng tiếng Việt trong code (kể cả bản
transliterate không dấu như `Ghe`, `DonDatVe`). Nên mỗi thuật ngữ trong
`docs/domain/ubiquitous-language.md` nay có **hai tên**: tiếng Việt (để trao đổi) và tiếng Anh
(tiêu chuẩn dùng trong code).

Tên tiếng Anh chốt: `Room seat` / `Showtime seat` / `Ticket` / `Booking` / `Available` /
`Held` / `Sold` / `Blocked` / `Seat type` / `Ticket price` / `Showtime` / `Movie` /
`Seat hold` / `Expired` / `Cancelled` / `Paid` / `Customer` / `Theater manager` / `Gate staff` /
`Administrator`.

Bổ sung thêm: mục "Quy tắc đặt tên trong code" và bảng tra nhanh 27 dòng
tiếng Việt → tiếng Anh. Tên entity/bảng cụ thể (`ShowtimeSeat`, `RoomSeat`, `SeatHold`) để
T07/T11 chốt — hiện mới là **gợi ý**, ghi rõ là chưa chốt.