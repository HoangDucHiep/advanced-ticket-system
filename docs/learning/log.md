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

---

## 2026-11-20 — T06: Context map workshop (Gate B)

**Bối cảnh**: T06 (mode `co-design`) — cắt domain model thành các bounded context. Artifact:
`docs/domain/context-map.md`, 6 context, 8 quan hệ (R1–R8).

**Đã học**:

- **Bounded context là ranh giới *ngôn ngữ*, không phải ranh giới *transaction*.** Đây là
  điều dễ hiểu sai nhất của DDD khi áp dụng vào modular monolith. Vì ta chưa tách service,
  ranh giới context và ranh giới transaction **không trùng nhau** — và sẽ có những chỗ bất
  biến bị cắt ngang ranh giới context.
- **Cách tìm ranh giới context là tìm chỗ bất biến KHÔNG bị cắt.** Ví dụ: bất biến "mỗi vé chỉ
  check-in một lần" (`FR-07`) → vé và check-in phải cùng context (`Ticketing`). Bất biến "ghế
  `Held` luôn có thời điểm hết hạn" → `Seat hold` phải ở `Inventory`. Ranh giới đặt sai chỗ thì
  bất biến bị cắt, và lúc đó bạn buộc phải bịa cơ chế phối hợp (saga/outbox) — hoặc âm thầm
  để invariant hỏng.
- **Anti-Corruption Layer là chỗ "mô hình của hai bên gặp nhau".** Ở R1, `Scheduling` nói "phòng
  có 100 ghế", `Inventory` nói "mỗi ghế phải có trạng thái riêng". Lớp dịch nằm đúng chỗ đó.
  Ở R5, Stripe nói `payment_intent` / `requires_payment_method`, ta nói `Paid` / "chờ thanh
  toán" — không có lớp dịch thì mô hình của Stripe lấn át vào bên trong và ta sẽ có code kiểu
  `if (paymentIntent.Status == "requires_confirmation")` rải khắp nơi.
- **`Customer/Supplier` và `Shared Kernel` khác nhau ở chỗ ai sở hữu.** `Identity & Access` là
  Supplier: một bên sở hữu, bên kia chỉ **hỏi**. `Shared Kernel` là hai bên **cùng sở hữu**
  một phần mô hình — cùng kiểu dữ liệu, cùng quy ước, đổi một bên thì cả hai đều đổi. Ta
  không có trường hợp nào thứ hai.
- **Stripe là L3, không phải context của ta.** Nên `Payment` → Stripe là `Anti-Corruption Layer`,
  không phải `Customer/Supplier`. Lẫn hệ thống bên ngoài vào bản đồ context là lỗi phân loại
  làm mờ ranh giới Lớp 1 và Lớp 3.

**Làm được**:

- `docs/domain/context-map.md` — 6 bounded context (`Scheduling`, `Inventory`, `Booking`,
  `Payment`, `Ticketing`, `Identity & Access`), 22 thuật ngữ riêng theo context, sơ đồ mermaid,
  8 quan hệ R1–R8 (mỗi quan hệ ghi dạng / ai gọi ai / hướng dữ liệu / cơ chế), 3 ACL, bảng
  **nguồn sự thật** 11 dòng, và mục **"Nhóm ba ranh giới bị cắt"** liệt kê 3 nơi invariant bị
  cắt kèm hậu quả nếu tách service.
- Mục "Đối chiếu với điểm tranh chấp ở T04" — 12 dòng khớp 12 điểm tranh chấp ở
  `docs/prd/03-user-journeys.md`, kèm cột "Cắt ranh giới?".
- 6 quyết định được chốt trong phiên (xem ticket T06), trong đó 3 quyết định về **vị trí** của
  `Seat hold`, `Room seat`, `Ticket price` — mỗi quyết định đều dựa trên câu hỏi "bất biến nào
  không được bị cắt".

**Vấp ở đâu**:

- **Có lúc tôi suýt đặt ranh giới theo "tên entity" thay vì theo bất biến.** Ví dụ suýt tách
  `Check-in` thành context riêng vì nó là một khái niệm riêng — nhưng như vậy là cắt bất biến
  `FR-07`. Nguyên nhân thật: **tách theo cây từ vựng thì dễ, tách theo bất biến thì phải suy
  nghĩ**. Bài học: khi phân vị trí một khái niệm, hãy hỏi "bất biến nào của nó sẽ bị cắt" —
  đó là câu hỏi duy nhất có ích.
- **Còn một mâu thuẫn PRD đã sửa ở T05 mà bị ảnh hưởng bởi T06.** Mốc thời gian hết hạn hold và
  mốc hết hạn vé: người học nhận ra giữ ghế hết hạn lúc 20:15 còn vé chỉ hết hạn khi suất
  chiếu bắt đầu — hai mốc khác nhau, và ranh giới `Inventory`/`Booking` phải tôn trọng cả hai.
- Việc viết file bằng `Add-Content` từng khối làm mất dấu tiếng Việt ở phần đuôi file
  (không còn dấu). Nguyên nhân: shell script ghi bằng encoding không đúng. Bài học: **sau khi
  ghi file bằng script, phải đọc lại file** — không tin script.

**Còn mở**:

- Một suất chiếu có hàng trăm ghế — có nên là một aggregate không? → T07, ADR-0003.
- `FR-09` (giới hạn ghế mỗi người mỗi suất) có nằm trong cùng aggregate với tồn kho ghế
  không? → T07 (Q15).
- Ranh giới aggregate quanh **thanh toán và hoàn tiền** → T08 (Q17).
- Lịch sử trạng thái đơn (cho `FR-31` truy vết) nằm ở `Booking` hay mỗi context giữ riêng
  → T07/T08 (Q19).
- `Seat hold` khi tách service sẽ nằm ở `Inventory` hay `Booking`? → T07 (Q18).
- Ba "ranh giới bị cắt" có cần chuyển sang saga khi tách service không? → T10 (Q20).

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-21 — T07: Aggregates và invariants workshop (Gate B)

**Bối cảnh**: T07 (mode `co-design`) — chốt ranh giới aggregate cho 6 bounded context. Artifact:
`docs/domain/aggregates/` — 1 file index + 6 file theo context, **8 aggregate**, **47 invariant**.

**Đã học**:

- **Aggregate boundary KHÔNG phải lúc nào cũng đủ để bảo vệ invariant.** Đây là bài học lớn
  nhất của ticket, và nó chỉ hiện ra khi bắt buộc phải trả lời câu hỏi "invariant nào **không**
  thể bảo vệ bằng transaction đơn lẻ". Có **ba dạng** khác nhau:
  1. **Tích luỹ xuyên nhiều aggregate** — `BR-08` (10 ghế/người/mỗi suất): đếm theo *người* qua
     *nhiều đơn*. Về nguyên tắc không thể nằm trong một aggregate.
  2. **Quan hệ giữa hai aggregate** — `BR-20` (không có suất chiếu chồng nhau): hai `Showtime`
     khác nhau; aggregate không thể ghi ra ngoài ranh giới của mình.
  3. **Cần điều phối nhiều bước qua nhiều context** — `BR-17` (webhook đến lặp): tiền → đơn →
     vé, ở ba context khác nhau.
  Cả ba đều có **cùng một nguyên nhân gốc: ranh giới aggregate không trùng ranh giới
  transaction**. Và điều này chỉ hiện ra khi ta **ghi ra tường minh** hậu quả, chứ không phải khi
  ta "chọn ranh giới cho đẹp".
- **Một invariant trông như "toàn cục" có thể hoá thành "cục bộ"** nếu đặt ranh giới aggregate
  đúng chỗ. `BR-10` (mỗi vé chỉ check-in một lần) nghe như cần transaction xuyên nhiều vé —
  nhưng nó là invariant về **một vé**, và hai lượt quét đồng thời là hai transaction cùng tranh
  **một dòng**. Ràng buộc duy nhất là đủ. Ngược lại, `BR-08` trông như cục bộ (nằm trong một
  đơn) nhưng hoá thành toàn cục. **Cái quyết định là ranh giới, không phải bản chất của luật.**
- **Aggregate lớn là mùi code, nhưng "aggregate nhỏ" không tự động là đúng.** Bài toán thật ở T07
  là mâu thuẫn giữa hai nguyên tắc: `BR-02` (giữ n ghế all-or-nothing) kéo ranh giới **bao
  trọn** n ghế, còn "aggregate không được quá to" kéo ranh giới **thu nhỏ**. Giải pháp không
  phải chọn một bên, mà là nhận ra: **all-or-nothing chỉ cần bao trọn n ghế của *một lần giữ***
  (≤6 ghế theo `BR-06`), không cần bao trọn cả suất chiếu. Đó là ranh giới `Seat hold`.
- **"Ranh giới aggregate đúng" và "invariant do aggregate bảo vệ" là hai câu hỏi khác nhau.**
  `Seat hold` không bảo vệ được `BR-01` (một ghế = một chủ) bằng ranh giới của nó — nó bảo vệ
  bằng **điều kiện lệnh**. Đây là điều mà nhiều người học DDD hiểu sai: aggregate là **một**
  cách bảo vệ invariant, không phải cách duy nhất. Với seat-based, điều kiện lệnh còn **mạnh
  hơn** khoá đọc-sửa-ghi: không có cửa sổ TOCTOU, và hai lệnh đồng thời trên cùng một ghế thì
  chỉ một thắng (lệnh còn lại ảnh hưởng 0 dòng và phải coi là thất bại).
- **Số dòng trong aggregate không quan trọng bằng tần suất ghi.** `Room` chứa hàng trăm
  `Room seat` — nghe thì giống hệt trường hợp "suất chiếu 200 ghế" mà ta lo. Nhưng ghế vật lý
  **tĩnh**: nó không bao giờ đổi trạng thái. Contention chỉ sinh ra khi có **ghi**. Cùng số dòng,
  khác hẳn hành vi.
- **Tính bất biến trong mô hình cũng là một cơ chế bảo vệ.** `BR-12` (một đơn = một suất chiếu)
  được ép bằng **hình dạng dữ liệu**: `Booking` mang một tham chiếu suất chiếu, `Booking line`
  không có trường suất chiếu. "Trộn suất chiếu" **không thể biểu diễn được** — mạnh hơn là
  kiểm tra lúc ghi.

**Làm được**:

- `docs/domain/aggregates/README.md` — index: bảng tổng hợp aggregate → invariant → cơ chế (24
  dòng), bảng **3 invariant không bảo vệ được bằng transaction đơn lẻ** (tách 3 dạng), sơ đồ
  mermaid, bảng entity vs value object (14 dòng), và bảng kiểm tra "không aggregate nào chứa
  thứ của aggregate khác" (đối chiếu bảng nguồn sự thật ở T06).
- 6 file theo context: `01-scheduling.md`, `02-inventory.md`, `03-booking.md`,
  `04-payment.md`, `05-ticketing.md`, `06-identity-access.md`. Mỗi aggregate có mục **Bản chất**,
  **Chứa**, **Boundary**, danh sách invariant (mỗi cái ghi cơ chế bảo vệ + **ai có thể vi phạm** +
  `Traces to`), và **Transaction boundary** mô tả bằng hành vi.
- 8 aggregate: `Seat hold`, `Booking`, `Payment intent`, `Ticket`, `Showtime`, `Room`, `Movie`,
  `User`. 47 invariant.
- Đồng bộ: `docs/domain/README.md`, `docs/README.md` (Gate B), `docs/domain/ubiquitous-language.md`
  (mục "Câu hỏi còn mở cho T07" → đã trả lời), ticket T07 tick đủ 7 tiêu chí,
  `docs/plan/00-roadmap.md` (T06→T07 xong, +5 dòng log quyết định, 2 câu hỏi đóng),
  `docs/learning/open-questions.md` (Q12/Q15/Q17/Q18/Q19 đóng, thêm Q21–Q24).

**Vấp ở đâu**:

- **Tôi suýt viết "3 dạng" là "1 dạng" khi đọc lại ticket.** Ticket chỉ yêu cầu "ít nhất một
  invariant không thể bảo vệ bằng transaction đơn lẻ". Nhưng khi thực sự phân tích, ba bất biến
  bất khả bảo vệ **khác nhau về nguyên nhân**, và mỗi nguyên nhân cần một cơ chế khác nhau.
  Nguyên nhân thật: tôi đã đọc tiêu chí theo nghĩa "điền một ô checkbox" thay vì đọc nó như
  một **câu hỏi cần trả lời đúng**. Bài học: tiêu chí hoàn thành có giá trị nhất khi nó ép ta
  **đào sâu hơn mức tối thiểu**, chứ không phải khi ta lấp đúng số ô.
- **Tôi đã từng nghĩ "mọi invariant đều phải do aggregate bảo vệ"** — và viết `INV-07` (ghế
  `Blocked` không bán) như thể đó là sự thật hiển nhiên. May mắn là khi viết tới phần "cơ chế
  bảo vệ" mới lộ ra: nó thực ra là **hệ quả** của `INV-01` (ghế chỉ chuyển sang `Held` khi
  đang `Available`). Bài học: **cơ chế bảo vệ là chỗ dễ dùng lời hứa suông**, nên phải hỏi
  "cơ chế nào **thật sự** ép được nó" cho từng invariant.
- Ghi file bằng `Add-Content` từng khối lần nữa mất dấu tiếng Việt ở một đoạn. Đã sửa bằng
  cách đọc lại file và viết lại đoạn đó. Bài học lặp lại: **sau khi ghi file bằng script, phải
  đọc lại file** — không tin script.

**Còn mở**:

- `BR-08` có nên nâng lên ràng buộc ở tầng lưu trữ không? → Q21, Gate C.
- Giá trị khoảng đệm giữa hai suất chiếu cùng phòng → Q22, T13.
- `BR-08` có cần test concurrency thật để thấy rõ cửa sổ race không? → Q23, T22.
- `Gate assignment` hiện không thuộc aggregate nào — xem lại nếu cần bảo vệ "một cổng không
  phục vụ hai suất chồng nhau" → Q24.
- Ba "ranh giới bị cắt" có cần chuyển sang saga khi tách service không? → Q20, T10.
- ADR-0003: đề xuất chuyển `provisional → accepted` — **chờ người học duyệt**.
- ADR-0004 (ranh giới aggregate của tồn kho ghế): **đã tạo**, `status: provisional` — chờ người học
  duyệt nội dung rồi mới chuyển `accepted` ở Gate C. Ranh giới `Seat hold` được xem lại khi T11
  (data model) và T12 (concurrency design) chốt xong.
- Ranh giới aggregate quanh state machine của ghế và đơn → T08.

---

## 2026-11-24 — T08: State machines workshop (Gate B)

**Bối cảnh**: T08 (mode `co-design`) — vẽ vòng đời của các thứ có trạng thái. Artifact:
`docs/domain/state-machines.md` — **5 máy trạng thái**, 22 `FR-nn` có bảng truy vết.

**Đã học**:

- **State machine là nơi định nghĩa idempotency — và bảng "Transitions bị từ chối" mới là nơi
  định nghĩa nó.** Bảng "Transitions" chỉ nói *đường đi hợp lệ*; bảng "bị từ chối" mới nói
  *đường đi không hợp lệ thì kết quả mong đợi là gì*. Một transition bị từ chối **không phải
  là lỗi** — nó là một kết quả hợp lệ có thông điệp riêng (`FR-36`). Đây là lý do ticket yêu
  cầu bảng này và cũng là lý do nó là phần dễ bị bỏ sót nhất.
- **"Idempotent" có hai nghĩa khác nhau, và phải ghi rõ đang nói nghĩa nào.** Ở `Showtime
  seat`, `Held → Sold` chạy lại là **no-op** (cùng kết quả). Ở `Ticket`, check-in chạy lại
  là **bị từ chối** (kết quả *khác* — trả về thời điểm + cổng lần đầu). Cả hai đều đúng, và
  cả hai đều chống được việc ghi hai lần. Nếu ghi chung chung "idempotent" thì tài liệu sẽ
  nói sai một trong hai.
- **Bài toán race "hết hạn đúng lúc webhook đến" chốt được TRÊN GIẤY** — không cần
  `/prototype`, dù ticket gợi ý dùng. Lý do: nó **không** phải bài toán "hệ thống biết gì",
  mà là bài toán "ai đến trước". Cả hai luồng ghi vào **cùng một trạng thái nguồn** của cùng
  một aggregate bằng **điều kiện lệnh** ⇒ chỉ một lệnh thắng, và nhánh "thua" đã được định
  nghĩa sẵn. Kết quả **không** phụ thuộc thứ tự **đọc**.
  > Bài học: trước khi dựng prototype, hãy hỏi "câu hỏi này có phụ thuộc thứ tự **đọc**
  > không". Nếu không thì nó thường chốt được trên giấy. Prototype là đắn nhất — dùng nó
  > khi câu hỏi *thực sự* không trả lời được trên giấy, không phải khi ticket gợi ý.
- **Một trạng thái thừa tốn kém một transition.** Vé bị vô hiệu do hủy suất chiếu (`FR-24`)
  *đáng lẽ* là trạng thái thứ ba của `Ticket` — nhưng nếu thêm vào, ta phải nghĩ xem nó đi
  được đâu, ai chuyển nó, và ai đọc nó. Nó không đi đâu: nó là **điều kiện tiên quyết** của
  transition check-in. Vậy nên nó là **tác dụng phụ** của transition hủy suất chiếu, không phải
  một trạng thái. Máy `Ticket` chỉ có `Issued` → `CheckedIn`.
- **Tên trạng thái là quyết định *kiến trúc*, không phải quyết định *ngôn ngữ*.** `Pending` bị
  glossary cấm (dễ nhầm với `Held`), nên trạng thái "chờ thanh toán" phải đặt tên khác ⇒
  `AwaitingPayment`. Và `Payment intent` **không** dùng `Paid` (đã là trạng thái của `Booking`)
  mà dùng `Captured` — vì hai context khác nhau dùng cùng một tên trạng thái sẽ khiến log dễ
  đọc sai. Tên này sẽ thành tên cột trong schema, nên phải chốt **trước** Lớp 3.
- **Trạng thái "thất bại" là một trạng thái bị lãng phí nếu nghiệp vụ không cần nó.** Kết quả
  thanh toán thất bại **không** cần trạng thái riêng: intent bị **bỏ**, đơn đi theo đường
  `Expired`/`Cancelled` bình thường. Đây là hệ quả của `INV-04` — ta cố tình **không** mô
  hình hoá trạng thái của Stripe.
- **Ranh giới bị cắt thứ tư: hủy suất chiếu.** T06 đã liệt kê ba chỗ. T08 phát hiện chỗ thứ
  tư — và nó **khác** ba chỗ kia ở một điểm cốt lõi: nó chạy **N đơn**, không phải một
  aggregate. Khi tách service, ba chỗ kia là "một lệnh phải chia thành nhiều bước", còn chỗ
  này là "**một lệnh sinh ra N lượt**", mỗi lượt phải idempotent. Đó là lý do nó cần được ghi
  tường minh.

**Làm được**:

- `docs/domain/state-machines.md` — 5 máy trạng thái: `Showtime seat`, `Booking`,
  `Payment intent`, `Ticket`, `Showtime`. Mỗi máy có **4 phần** (States / Transitions /
  Transitions bị từ chối / Điều kiện tiên quyết) **+ bảng Idempotency riêng** ghi rõ
  transition nào phải idempotent và idempotent **bằng cơ chế nào**.
- Tên trạng thái chốt (dùng trong code): `Available`/`Held`/`Sold`/`Blocked`;
  `AwaitingPayment`/`Paid`/`Expired`/`Cancelled`; `AwaitingPayment`/`Captured`/`Refunded`;
  `Issued`/`CheckedIn`; `NotOnSale`/`OnSale`/`Cancelled`.
- Bảng truy vết phủ **22 `FR-nn`**: 20 `FR` có transition riêng, **+ bảng "FR không có transition
  riêng"** giải thích 2 `FR` là hệ quả (`FR-08`, `FR-19`) — vì "không có transition" cũng là
  một câu trả lời cần viết ra, không phải một chỗ trống.
- Mục riêng về race hết hạn vs webhook, kèm lý do **vì sao không cần prototype**.
- Bảng **"Ranh giới bị cắt — bốn chỗ"** (3 chỗ của T06 + 1 chỗ mới).
- Đồng bộ: ticket T08 tick đủ 7 tiêu chí + ghi chú hoàn thành, `docs/domain/README.md`,
  `docs/README.md` (Gate B), `docs/plan/00-roadmap.md` (T08 xong + 6 dòng log quyết định +
  đóng 1 câu hỏi), `docs/learning/open-questions.md` (thêm Q25–Q29).

**Vấp ở đâu**:

- **Lần đầu tiên trong project này, tôi phải *hỏi* tên trạng thái thay vì *đặt* tên.** Ở T07
  tôi tự chọn tên entity (`Seat hold`, `Booking`) — vì tên entity thì suy ra từ nghiệp vụ, còn
  tên **trạng thái** thì phải khớp với tên trạng thái của các thứ khác trong hệ thống. Đặt
  `Pending` là tự tạo vấn đề mà glossary đã cảnh báo trước. Bài học: **tên trạng thái phải
  được hỏi, không được đặt** — vì nó sẽ thành tên cột, và sửa tên cột sau khi có dữ liệu là
  việc đắt.
- **Suýt cho rằng cần prototype cho bài toán race.** Ticket gợi ý `/prototype` cho tình huống
  "giữ ghế hết hạn đúng lúc webhook đến", và tôi đã chuẩn bị dựng mô hình. Nhưng khi viết
  ra cơ chế (cả hai luồng dùng cùng một điều kiện lệnh trên cùng một cột trạng thái) thì
  câu trả lời đã rõ. Nguyên nhân thật: tôi làm theo **gợi ý của ticket** thay vì theo **đặc
  tính của câu hỏi**. Bài học: `/prototype` là công cụ để giải bài toán **không** chốt được
  trên giấy — không phải để làm theo lịch trình.
- **Ghi file bằng `Write` với nội dung dài tiếng Việt vẫn hỏng một ký tự** (`thứ` bị thay
  bằng ký tự thay thế, dòng 3). Đã phát hiện bằng `Grep` tìm ký tự thay thế (U+FFFD) và sửa.
  Bài học lặp lại lần thứ ba: **sau khi ghi file, phải quét lại bằng regex tìm ký tự thay
  thế** — đọc bằng mắt thì dễ bỏ sót vì não tự điền chữ đúng.
- **Cần 4 vòng hỏi (8 câu) thay vì 1 vòng.** Ban đầu tôi dự định hỏi gộp mọi thứ trong
  một lần, nhưng đó là cách nhanh nhất để tạo ra một tài liệu dài mà ít sâu. Hỏi từng máy một,
  mỗi câu có khuyến nghị và đánh đổi rõ ràng, thì mỗi câu trả lời đều có lý do — và tài liệu
  viết ra được là **bằng chứng** của các quyết định đó, không phải một bản tóm tắt.

**Còn mở**:

- Ghế `Held` đã quá hạn nhưng chưa được giải phóng → Q25, T12/T21.
- Đơn `Paid` khi hủy suất chiếu có cần trạng thái `Refunded` riêng không → Q27, T09/T11.
- `AwaitingPayment` dùng chung cho hai context → Q28, T11.
- Hủy suất chiếu: một transaction hay N transaction → Q29, T10/T12.
- `BR-08` có nên nâng lên ràng buộc ở tầng lưu trữ không? → Q21, Gate C.
- Giá trị khoảng đệm giữa hai suất chiếu cùng phòng → Q22, T13.
- `BR-08` có cần test concurrency thật để thấy rõ cửa sổ race không? → Q23, T22.
- `Gate assignment` hiện không thuộc aggregate nào → Q24.
- Ba "ranh giới bị cắt" (nay là **bốn**) có cần chuyển sang saga khi tách service không? →
  Q20, T10.
- ADR-0003: đề xuất chuyển `provisional → accepted` — **chờ người học duyệt**.
- ADR-0004 (ranh giới aggregate của tồn kho ghế): **đã tạo**, `status: provisional` — chờ người học
  duyệt nội dung rồi mới chuyển `accepted` ở Gate C.

> **Đề xuất ADR (chưa tạo — chờ người học duyệt):** quyết định "hủy suất chiếu là ranh giới
> bị cắt thứ tư, và nó chạy N đơn chứ không phải một aggregate" đủ điều kiện để làm ADR —
> khó đảo ngược, ngạc nhiên nếu không có context, và là kết quả của một đánh đổi thật.

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-24 — T09: Cross-context contracts workshop (Gate B)

**Bối cảnh**: T09 (mode `co-design`) — định nghĩa hợp đồng giữa các bounded context. Artifact:
`docs/domain/cross-context-contracts.md` — 8 hợp đồng (R1–R8), 7 cặp thuật ngữ khác nghĩa, 4 ranh giới bị cắt.

**Đã học**:

- **Hợp đồng context là nơi dữ liệu bị dịch, thuật ngữ bị hiểu khác, và ranh giới rõ ràng được
  ghi lại.** Nếu không ghi, khi triển khai code sẽ đọc thẳng bảng của context khác vì "nhanh
  hơn", và bạn quay lại với ranh giới module chỉ còn trên giấy.
- **Mô hình đọc chéo context: qua interface/contract, không đọc trực tiếp bảng.** Cách này giữ
  ranh giới module rõ ràng ngay từ design stage, và tránh tình trạng "nhanh hơn" thành lý do
  xoá sạch ranh giới ở tầng code.
- **Anti-Corruption Layer xuất hiện khi mô hình của hai bên gặp nhau.** Ở R1, `Scheduling`
  nói "phòng có 100 ghế", `Inventory` nói "mỗi ghế phải có trạng thái riêng" ⇒ ACL ở phía
  `Inventory` dịch `Room seat` → `Showtime seat`. Ở R5, Stripe nói `payment_intent`,
  `requires_payment_method` — ta nói `Captured`, "chờ thanh toán" ⇒ ACL ở phía `Payment` dịch
  Stripe lại.
- **Bảng thuật ngữ "Có phải cùng nghĩa?" chính là chỗ phát hiện ACL.** Nếu cùng nghĩa thì
  không cần dịch; nếu khác thì phải ghi rõ nơi dịch. Đây là cách biến "loại từ dựa trên trực
  giác" thành "loại từ dựa trên định nghĩa".
- **R1 và R2 tách riêng (không gộp bidirectional) giúp nhìn rõ ứng viên tách service.** R2
  (`Inventory → Scheduling` đọc-duy-nhất 4 con số) rõ ràng là ứng viên hàng đầu — và cách gom
  hợp đồng ảnh hưởng tới cách nhìn service boundary sau này. Nếu gộp lại, R2 sẽ bị "nhấn chìm"
  trong một hợp đồng hai chiều, và sự thật "nó là một service candidate" sẽ không rõ ràng.
- **Q27 decision (Booking KHÔNG có trạng thái `Refunded`)** ảnh hưởng trực tiếp cách mô hình
  "hoàn tiền" ở hợp đồng R4. Dấu vết hoàn tiền nằm ở `Payment intent.Refunded`, không phải
  ở `Booking`. Lý do: `Booking` đã kết thúc chu kỳ chính (giữ → thanh toán → cấp vé); hoàn
  tiền do hủy suất là sự kiện ngoại lai, không phải một bước trong chu kỳ.
- **8 cặp thuật ngữ khác nghĩa là những chỗ DDD gặp nhau.** Mỗi cặp cần một lớp dịch, và mỗi
  lớp dịch cần được ghi tường minh nơi chúng được tạo. Nếu không ghi, hai team code (hoặc một
  team vài tháng sau) sẽ "khám phá lại" lớp dịch ấy, hoặc quên nó đi.

**Làm được**:

- `docs/domain/cross-context-contracts.md` — 8 hợp đồng (R1–R8):
  - **R1** `Scheduling → Inventory` — ghi chung transaction; bảng 4 thuật ngữ; `Room seat` vs
    `Showtime seat` khác nghĩa.
  - **R2** `Inventory → Scheduling` — đọc-duy-nhất; bảng 1 thuật ngữ (4 con số); ứng viên tách
    service **hàng đầu**.
  - **R3** `Inventory ↔ Booking` — ghi chung transaction; bảng 3 thuật ngữ; `Seat hold` vs
    `Booking` khác nghĩa; **ranh giới bị cắt #1 & #3**.
  - **R4** `Booking ↔ Payment` — ghi chung transaction khi thanh toán; bảng 4 thuật ngữ;
    `AwaitingPayment` (Booking) vs `AwaitingPayment` (Payment intent) cùng tên khác aggregate
    (Q28); Q27 decision ghi rõ.
  - **R5** `Payment → Stripe` — API + webhook; bảng 7 thuật ngữ; `Payment intent` vs
    `payment_intent` khác nghĩa; bảng so sánh Stripe vs ta; **ranh giới bị cắt #2 kèm R6**.
  - **R6** `Booking → Ticketing` — ghi chung transaction khi cấp vé; bảng 3 thuật ngữ;
    **ranh giới bị cắt #2 kèm R4 + R5**.
  - **R7** `Scheduling → Ticketing` — đọc-duy-nhất (suất chiếu + cổng); bảng 2 thuật ngữ; ứng
    viên tách service.
  - **R8** `Identity & Access → tất cả` — đọc-duy-nhất (kiểm tra quyền); bảng 4 vai trò; ứng
    viên tách service.

- Mục "Ranh giới bị cắt — bốn chỗ" — tường minh hóa 4 chỗ; mục "Các cặp thuật ngữ khác nghĩa"
  — 8 cặp; mục "Ứng viên tách service" — ma trận (8 hợp đồng × 5 tiêu chí) + ghi chú ứng viên
  hàng đầu (R2, R7, R8); mục "Câu hỏi còn mở" — Q27 đã giải, Q26/Q28/Q20 defer.

- Ticket T09 tick đủ 6 tiêu chí, ghi chú hoàn thành. Đồng bộ: `docs/domain/README.md`,
  `docs/README.md` (Gate B), `docs/plan/00-roadmap.md` (T09 xong + 5 dòng log quyết định),
  `docs/learning/open-questions.md` (Q27 resolve kèm lý do).

**Vấp ở đâu**:

- **Hỏi nhiều quyết định cùng lúc (3 câu hỏi) thay vì 1 vòng hỏi-trả-lời đơn lẻ.** Ban đầu
  plan định hỏi về mô hình đọc chéo, cấu trúc gom hợp đồng, và Q27 cùng lúc. Tôi phát hiện ngay
  là quá nhiều, nên chia thành 2 vòng. Bài học: **một vòng hỏi nên chứa một quyết định duy
  nhất**, hoặc tối đa 2 quyết định có liên quan (như cấu trúc gom + Q27, vì cả hai đều ảnh hưởng
  cách viết R4). Nhiều hơn thì tôi suýt chôn một quyết định trong "quá nhiều tùy chọn".
- **Người học trả lời "tôi không rõ" cho câu hỏi về cách gom hợp đồng R1/R2.** Đúng như tinh
  thần `co-design`: người học chưa biết đáp án thì **dạy trước rồi hỏi lại**, đừng chỉ đưa
  lựa chọn để họ bấm bừa. Sau khi giảng (vì sao cách gom ảnh hưởng tới việc nhìn service
  boundary), câu hỏi mới có giá trị.

**Còn mở**:

- Tên `AwaitingPayment` dùng chung cho hai context → Q28, T11.
- Hủy suất chiếu: một transaction hay N transaction → Q29, T10/T12.
- Ba "ranh giới bị cắt" (nay là **bốn**) có cần chuyển sang saga khi tách service không? →
  Q20, T10.
- `BR-08` có nên nâng lên ràng buộc ở tầng lưu trữ không? → Q21, Gate C.
- Giá trị khoảng đệm giữa hai suất chiếu cùng phòng → Q22, T13.
- `BR-08` có cần test concurrency thật để thấy rõ cửa sổ race không? → Q23, T22.
- `Gate assignment` hiện không thuộc aggregate nào → Q24.
- Ghế `Held` đã quá hạn nhưng chưa được giải phóng → Q25, T12/T21.
- Cờ "đã cấp vé" trong `Booking` → Q26, T11.
- ADR-0003: đề xuất chuyển `provisional → accepted` — **chờ người học duyệt**.

> **Đề xuất ADR (chưa tạo — chờ người học duyệt):** quyết định "mọi đọc chéo context đều qua
> interface của context sở hữu, không đọc trực tiếp bảng" đủ điều kiện để làm ADR — khó đảo
> ngược, ngạc nhiên nếu không có context, và là kết quả của một đánh đổi thật (tiện lợi ở tầng
> code vs. giữ ranh giới module).

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-25 — Review T01–T09 và đóng Gate B

**Bối cảnh**: trước khi mở T10, rà lại toàn bộ PRD + domain. Review tìm ra 7 vấn đề nghiêm trọng
và 17 mâu thuẫn giữa các file; người học chốt và sửa (commit `docs: fix`), sinh ra ADR-0005,
ADR-0006, ADR-0007. Sau đó gom `GLOSSARY.md` và đóng Gate B.

**Học được** (theo các quyết định đã ghi ở log quyết định roadmap ngày 2026-11-25):

- **Aggregate ≠ DB transaction.** Một DB transaction cập nhật N dòng vẫn atomic, nên all-or-nothing
  của `BR-02` không cần một aggregate chứa N ghế. `Showtime seat` tồn tại trước và sống qua nhiều
  lần giữ ⇒ không thể là entity bên trong một lần giữ ⇒ nó là aggregate root (ADR-0005).
- **Hết hạn phải định nghĩa bằng thời gian, không bằng worker.** "Ai chuyển trạng thái trước thì
  thắng" vẫn cho webhook đến sau hạn nhưng trước lượt quét được `Paid` — trái `BR-04`. Điều kiện
  `→ Paid` phải kiểm `expires_at` (ADR-0006).
- **Gọi ra ngoài không nằm trong DB transaction được.** Hoàn tiền cần trạng thái trung gian
  `RefundPending` + outbox + idempotency key phía Stripe (ADR-0006).
- **Lý do chấp nhận race phải đúng.** `BR-08` là luật chống phe vé — người bị hại là người khác;
  khoá theo (người xem, suất) không phải điểm tranh chấp toàn cục (ADR-0007).
- **Test deadlock phải đỏ khi bỏ cơ chế.** Postgres tự phát hiện deadlock và huỷ một bên, nên test
  chỉ kiểm "cả hai đều có phản hồi" vẫn xanh khi không sắp thứ tự khoá.
- **Glossary một file vẫn chứa được nhiều context** nếu từ trùng tên được ghi kèm context.

**Làm được**:

- Sửa PRD + domain theo review; ADR-0005/0006/0007; ADR-0003 → `accepted`; thêm `FR-40`, `FR-41`,
  `BR-31`, quan hệ R9.
- `GLOSSARY.md` ở root — 6 mục theo context; định nghĩa gom từ UL, context map, state machines.
- Tick đủ exit checklist Gate B; tạo `docs/plan/gate-c-design.md`.

**Vấp ở đâu**:

- Khi gom glossary, AI tự thêm vài từ `_Avoid_` không có trong UL — đã gỡ ra trước khi duyệt.
  Glossary là bản gom, không phải chỗ đặt từ mới.

**Còn mở**:

- Q20 — module tham gia chung DB transaction thế nào → T10.
- Q28 — tên `AwaitingPayment` ở hai máy → T11.
- Q13 — UL đã ghi "hai suất của cùng một phim có thể có giá khác nhau", nhưng Q13 vẫn mở trong
  `open-questions.md` → người học xác nhận đóng ở T11.

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-25 — T10: Architecture và module boundary (Gate C)

**Bối cảnh**: ticket đầu tiên của Gate C, mode `co-design`. Người học chốt D1–D3; từ D4 người học
nói "đang học, chưa ra quyết định được" và yêu cầu AI quyết định kèm giải thích. Các quyết định đó
ghi *(AI đề xuất, người học chấp nhận)* trong `docs/design/architecture.md`.

**Học được**:

- **Ba cách gọi giữa module, chọn bằng hai câu hỏi.** (1) Bên gọi có cần kết quả để quyết định
  tiếp không? Có ⇒ interface. (2) Phần phía sau có phải atomic không? Có ⇒ event in-process trong
  transaction; không, hoặc chạm hệ thống ngoài ⇒ outbox.
- **Hướng phụ thuộc là ràng buộc thứ ba, và hay bị quên.** Downstream được reference `.Contracts`
  của upstream, không bao giờ ngược lại. Upstream muốn kích hoạt downstream thì phát event. Ví dụ:
  hủy suất (#4) — `Scheduling` phát `ShowtimeCancelled`, không gọi `Booking`.
- **Đi qua bài kiểm tra #4:** "outbox + outbox" sai vì mất atomic (cửa sổ suất đã hủy mà đơn vẫn
  chờ ⇒ cấp vé cho suất đã hủy — đúng lỗi A7 của buổi review). "Interface + outbox" đúng về tính
  đúng đắn nhưng phụ thuộc ngược, và thêm module phản ứng là phải sửa `Scheduling`.
- **Event in-tx và interface chạy giống nhau lúc runtime** (code đồng bộ, cùng transaction). Khác
  nhau chỉ ở **ai biết ai** lúc compile.
- **Quan hệ hai chiều về dữ liệu vẫn phải có một hướng compile-time** (R4, R6), nếu không đồ thị
  module có vòng.
- **Một `DbContext` chung làm ranh giới module chỉ còn trên giấy.** Mỗi module một `DbContext`,
  chung connection + transaction, giữ được cả hai: ranh giới và atomic.

**Làm được**:

- `docs/design/architecture.md` — D1–D10, sơ đồ phụ thuộc, 8 quy tắc phụ thuộc kiểm được, cấu trúc
  thư mục, bảng sở hữu bảng, sequence diagram giữ ghế, điều kiện tách service.
- Q20 đóng. Roadmap: sửa dòng Database trong "Stack đã chốt", thêm 5 dòng log quyết định.

**Vấp ở đâu**:

- AI nói "`Booking` là upstream của `Ticketing` (R6)" dựa vào mũi tên context map, rồi khi kiểm
  kỹ mới thấy phần chữ của R6 ghi `Ticketing` là Supplier (R4 cũng vậy). Context map tự mâu thuẫn ở
  hai quan hệ hai chiều. Bài học: **đọc phần chữ, không chỉ đọc sơ đồ**, trước khi lập luận theo
  hướng phụ thuộc.
- Người học hai lần chưa chọn được. Nguyên nhân thật: câu hỏi đưa lựa chọn **trước khi** có tiêu
  chí để chọn. Giảng tiêu chí (bảng hai câu hỏi) rồi mới hỏi thì câu hỏi mới có nghĩa.

**Còn mở**:

- Race giữ ghế ↔ hủy suất (đọc `OnSale` trong transaction không đủ) → T12.
- Khoá quota: advisory lock hay bảng quota → T12.
- Idempotency của lần thất bại khi transaction chính rollback → T19, T13.
- Sửa R4, R6 trong `context-map.md` cho khớp D9 → người học quyết.
- Hai ADR đề xuất (D3; D4 + D9) → chờ người học duyệt.

Xem thêm: [`open-questions.md`](open-questions.md).

---

## 2026-11-25 — Rà toàn bộ docs sau T10; đổi hợp đồng làm việc

**Bối cảnh**: người học đổi hợp đồng — "đang học, chưa ra quyết định được; sẽ đọc spec hoàn chỉnh
rồi implement theo". Từ nay **AI tự quyết mọi quyết định spec** (PRD, domain, design, ADR), mỗi quyết
định ghi lý do + phương án bị loại; người học viết code (`AGENTS.md` đã sửa). Sau đó AI rà lại toàn
bộ `docs/` và sửa.

**Đã sửa — lỗ hổng nghiệp vụ (quyết định mới):**

- **Phòng và phim không có chủ.** Không luật nào nói ai được xếp suất vào phòng nào, ai tạo phim.
  Thêm `BR-32`: phòng thuộc quản lý rạp tạo ra nó; phim là danh mục chung của quản trị viên.
  `Room` `INV-14`.
- **Intent của đơn hết hạn không có trạng thái để đi**, và trùng tên `AwaitingPayment` với `Booking`
  (Q28). ADR-0010: `Unpaid` / `Closed`.
- **Check-in "vé chưa trả tiền"** là kết quả không thể xảy ra (vé chỉ cấp khi `Paid`). 5 kết quả nay
  là: hợp lệ / đã dùng / không hợp lệ / suất khác / suất đã hủy. "Không được phân công" là từ chối
  về quyền, xét trước vé.
- **Khoảng đệm giữa hai suất** = 15 phút (Q22). **Giá theo suất** — xác nhận và đóng Q13.
- **"Ẩn phim"** (FR-28) không có trạng thái nào — bỏ; phim hiện khi có suất đang bán.
- **Sơ đồ ghế do module nào phục vụ** — `Inventory` (ghép bố cục + giá từ `Scheduling`), vì là bên
  duy nhất biết cả hai phía mà không tạo vòng.

**Đã sửa — mâu thuẫn chéo file:** FR-20 có "giá" ở phòng (giá thuộc suất); FR-24 trỏ nhầm FR-19/FR-34;
ghi chú FR-38 chen giữa các dòng; "hết hạn (coi như đã huỷ)" ở J-04.2 và BR-04 (trái glossary);
J-04.2 gắn FR-18 (huỷ chủ động — nay là J-04.8); BR intro trỏ BR-04 thay vì BR-22; J-03.5 còn kiểm
chứng deadlock cũ; context map R2/R4/R6 lệch với hướng phụ thuộc T10; nguồn sự thật "`Scheduling`
đọc `Inventory`", "`Booking` đọc vé" lệch D9; `User` có "tập quyền" trong khi INV-04 nói một vai trò;
ADR-0002 còn `booking_id` trên dòng ghế và ngầm định `UPDATE` khoá theo thứ tự (Postgres `UPDATE`
không có `ORDER BY`); "12 bài toán" trong khi bảng đã có 13.

**ADR mới:** 0008 (transaction xuyên module), 0009 (cách gọi giữa module + hướng phụ thuộc), 0010
(`Unpaid` / `Closed`).

**Học được (cho người đọc spec):**

- Một trạng thái "không thể xảy ra" mà vẫn có trong danh sách kết quả là dấu hiệu tài liệu viết trước
  khi mô hình chốt — phải rà lại sau mỗi quyết định lớn.
- Quyền sở hữu là một phần của mô hình: "ai được làm gì trên cái gì" cần luật riêng, không suy được từ
  vai trò.
- Một máy trạng thái phải trả lời "khi bên kia kết thúc thì tôi đi đâu" — thiếu là có thực thể treo.

**Còn mở**: Q25 (ghế quá hạn chưa trả — T12), Q01/Q02/Q06 (cơ chế khoá — T12), khoá quota (T12),
race giữ ghế ↔ hủy suất (T12), idempotency của lần thất bại (T19/T13).

Xem thêm: [`open-questions.md`](open-questions.md).
