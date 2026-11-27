# Bài học

Những điều đã học được trong project, gom theo **chủ đề** (không theo ngày). Chỉ giữ khái niệm,
nguyên nhân thật của chỗ vấp và các phép thử âm tính cụ thể. Mục mới thêm ở cuối theo template
trong [`README.md`](README.md). Khái niệm giảng giải đầy đủ: [`concepts.md`](concepts.md).
Câu hỏi chưa có đáp án: [`open-questions.md`](open-questions.md).

---

## Đặc tả và domain modeling

- **Job ≠ hành vi hệ thống.** Job là thứ actor *muốn*; hệ thống *làm* gì để đáp ứng là requirement.
  Trộn hai thứ làm PRD mơ hồ mà vẫn tưởng rõ.
- **Success criteria phải sinh ra được phép đo.** "1000 request tranh 100 vé → đúng 100 thành công"
  sinh ra một test; "hệ thống nhanh và ổn định" không sinh ra gì. Chưa có số đo thì ghi "chốt sau
  baseline", đừng ghi con số cứng rồi đo ra khác.
- **Lớp PRD phải sạch từ khoá kỹ thuật.** Khi quen một khái niệm, nó thành cách nghĩ mặc định
  ("webhook", "idempotency key" lọt vào PRD). Tự kiểm: bỏ từ khoá đó đi, còn mô tả được hành vi không?
- **Actor là vai trò nghiệp vụ.** Stripe là adapter (tầng design), không phải actor.
- **"Hành vi bị cấm" là loại requirement chứng minh được bằng test** ("không bao giờ bán vượt số ghế"),
  nên đặt lên đầu. Một FR = một hành vi; requirement không chứa giải pháp (thay công nghệ mà câu vẫn
  đúng thì mới là requirement).
- **Một invariant phải còn khả thi để vi phạm** thì mới test được. Đặt 10 ghế cho cả giới hạn mỗi đơn
  lẫn mỗi người thì `BR-08` không bao giờ bị vi phạm → không ai test được.
- **Lỗ hổng luật nằm ở ranh giới giữa hai thời điểm.** Thanh toán đến muộn sau khi đơn hết hạn: tiền
  đã lấy mà vé không cấp, không FR nào phủ → phải thêm `FR-39` (hoàn tiền tự động). Hỏi: "hai thứ này
  xảy ra ở hai thời điểm khác nhau thì sao?"
- **Cắt scope không đánh số lại.** Số hiệu đã từng xuất hiện (`FR-26`) là đối tượng có tham chiếu, không
  phải nhãn tuần tự; đánh số lại phá mọi truy vết.
- **Non-requirements chỉ có giá trị khi mỗi mục có lý do cụ thể** (nghiệp vụ / thời gian / quy mô). Mục
  đáng giá nhất là mục nói điều gì không thể quay lại mà không đổi kiến trúc.
- **Không giả lập thanh toán/email/QR là quyết định có lý do**: webhook giả không bao giờ "đến lặp ba
  lần" — đúng tình huống cần chứng minh. Giả lập làm test yếu đi một cách âm thầm, test vẫn xanh.
- **Mô hình tồn kho quyết định bài toán concurrency nào xuất hiện.** Tier-based chỉ là "giảm một số
  đếm"; seat-based sinh ra "giữ n ghế all-or-nothing" và "tránh deadlock khi tranh ghế chồng nhau".
- **Đổi nghiệp vụ khi còn là chữ thì rẻ.** Khi phải vật lộn với từ ngữ, đó là tín hiệu mô hình sai —
  đổi mô hình, đừng sửa từ ngữ. Sau khi đổi mô hình, đọc lại requirement cũ vì câu chữ vẫn mang ngôn ngữ
  mô hình cũ (suýt giới hạn "theo số vé" trong khi ghế đôi tính 2 ghế).
- **ADR bị thay thế chứ không bị xoá** (giữ câu chuyện "đã thử, không đủ, vì sao"). Ghi rõ điều gì sẽ
  khiến ADR bị mở lại. Chỉ viết ADR khi quyết định khó đảo ngược + ngạc nhiên nếu thiếu context + là
  đánh đổi thật.
- **Hai khái niệm dễ nhầm nhất**: `Room seat` (tĩnh, thuộc phòng) vs `Showtime seat` (có trạng thái,
  thuộc một suất, là đơn vị bán). Ba cách đếm ghế (tổng phòng / còn trống / còn bán online) trả lời ba
  câu hỏi khác nhau.
- **Tên trạng thái phải được hỏi, không được đặt** — nó thành tên cột; trùng tên giữa hai context
  (`AwaitingPayment`) gây đọc log sai → đổi `Unpaid`/`Closed` (ADR-0010). Trạng thái "thất bại" thừa
  nếu nghiệp vụ không cần; một trạng thái thừa tốn một transition.
- **Sau mỗi quyết định lớn, rà lại tài liệu cũ.** Một kết quả "không thể xảy ra" mà vẫn có trong danh
  sách (check-in "vé chưa trả tiền") là dấu hiệu tài liệu viết trước khi mô hình chốt. Quyền sở hữu
  ("ai được làm gì trên cái gì") là một phần mô hình, cần luật riêng. Máy trạng thái phải trả lời "khi
  bên kia kết thúc thì tôi đi đâu".
- **Context map**: bounded context là ranh giới *ngôn ngữ*, không phải ranh giới transaction. Tìm ranh
  giới bằng "bất biến nào bị cắt" chứ không theo tên entity (suýt tách `Check-in` riêng, cắt `FR-07`).
  ACL nằm đúng chỗ hai mô hình gặp nhau (`Room seat`→`Showtime seat`; Stripe→`Captured`); bảng "có phải
  cùng nghĩa?" là chỗ phát hiện ACL. Stripe là tầng design, không phải context.
- **Aggregate**: câu hỏi quyết định là "có thay đổi nào mà chỉ một phần thành công thì hệ thống sai
  không?". Aggregate ≠ DB transaction: một transaction cập nhật N dòng vẫn atomic, nên all-or-nothing
  của `BR-02` không cần aggregate chứa N ghế (ADR-0005). Aggregate chỉ là *một* cách bảo vệ invariant;
  điều kiện lệnh (`UPDATE … WHERE state = Available`) còn mạnh hơn read-modify-write vì không có cửa sổ
  TOCTOU.
- **Ba dạng invariant không bảo vệ được bằng transaction đơn lẻ**: tích luỹ xuyên aggregate (`BR-08`),
  quan hệ giữa hai aggregate (`BR-20`), điều phối nhiều bước qua nhiều context (`BR-17`). Cùng gốc:
  ranh giới aggregate ≠ ranh giới transaction. Số dòng không quan trọng bằng tần suất ghi (ghế vật lý
  tĩnh nên `Room` chứa trăm ghế không sao). Hình dạng dữ liệu cũng là cơ chế bảo vệ: `Booking` mang
  tham chiếu suất, `Booking line` không có → "trộn suất" không biểu diễn được.
- **State machine định nghĩa idempotency**; bảng "Transitions bị từ chối" mới nói đường không hợp lệ
  cho kết quả gì (không phải lỗi). "Idempotent" có hai nghĩa: no-op cùng kết quả (`Held→Sold` chạy lại)
  vs bị từ chối trả kết quả khác (check-in lần hai). Ghi rõ đang nói nghĩa nào.
- **Hết hạn định nghĩa bằng thời gian, không bằng worker**: điều kiện `→ Paid` phải kiểm `expires_at`,
  nếu không webhook đến sau hạn nhưng trước lượt quét vẫn thành `Paid`, trái `BR-04` (ADR-0006).
- **Trước khi dựng prototype, hỏi "câu hỏi có phụ thuộc thứ tự *đọc* không".** Race hết hạn vs webhook
  chốt được trên giấy vì cả hai luồng ghi cùng cột trạng thái bằng điều kiện lệnh → chỉ một thắng.
- **Hợp đồng context ghi nơi dữ liệu bị dịch**; đọc chéo qua interface, không đọc bảng của context
  khác (nếu không ranh giới module chỉ còn trên giấy). Hủy suất là ranh giới bị cắt thứ tư và khác ba
  cái trước: nó sinh ra N lượt, mỗi lượt phải idempotent.

## Concurrency và khoá

- **Test concurrency chỉ có giá trị nếu nó fail khi cơ chế bị thay bằng cách sai.** Câu "xử lý
  concurrency" không có giá trị; "test này đỏ nếu bỏ `FOR UPDATE`" thì có.
- **EF Core InMemory không có transaction, row lock, unique constraint** → test trên đó pass dù logic
  concurrency sai. Dùng Postgres thật (Testcontainers).
- **`UPDATE … WHERE id = ANY(...)` không có `ORDER BY`**: thứ tự khoá là chi tiết của planner. Muốn
  chống deadlock phải khoá tường minh `SELECT … ORDER BY id FOR UPDATE`. Test deadlock có thể tình cờ
  xanh vì quét PK đã tăng dần → phép thử âm tính dùng biến thể khoá theo thứ tự của client.
- **Test deadlock chỉ kiểm "cả hai đều có phản hồi" vẫn xanh khi không sắp thứ tự khoá**, vì Postgres
  tự phát hiện deadlock và huỷ một bên.
- **`FOR KEY SHARE` vs `FOR UPDATE`**: hàng trăm lệnh giữ ghế cùng khoá chia sẻ dòng suất mà không chặn
  nhau, còn hủy suất (khoá độc quyền) phải chờ hết — chặn race hủy-suất mà không tuần tự hoá người mua.
- **Thứ tự khoá toàn cục** (suất → quota → đơn → ghế → intent → vé) làm các bài toán không deadlock *với
  nhau*, không chỉ trong từng bài.
- **Lý do chấp nhận một race phải đúng**: `BR-08` chống phe vé, người bị hại là người khác; khoá theo
  (người xem, suất) không phải điểm tranh chấp toàn cục (ADR-0007).
- **Redis cho tối ưu, DB cho đúng đắn**; có chỗ Redis còn không đáng tối ưu (check-in, ADR-0013).
  Cache theo version (ghi đè chỉ khi mới hơn) làm thứ tự đến của cập nhật hết quan trọng.
- **`now()` của Postgres là thời điểm bắt đầu transaction** — mọi so sánh hạn trong một transaction
  dùng chung một mốc.

## Transaction và Unit of Work

- **Ba cách gọi giữa module, chọn bằng hai câu hỏi**: (1) bên gọi cần kết quả để quyết định tiếp?
  → interface. (2) phần sau có phải atomic? → event in-process trong transaction; nếu không hoặc chạm
  hệ thống ngoài → outbox.
- **Hướng phụ thuộc là ràng buộc thứ ba, hay bị quên**: downstream reference `.Contracts` của upstream,
  không ngược lại; upstream muốn kích hoạt downstream thì phát event. Quan hệ hai chiều về dữ liệu vẫn
  phải có một hướng compile-time, nếu không đồ thị module có vòng.
- **"Outbox + outbox" cho hủy suất sai vì mất atomic** (cửa sổ suất đã hủy mà đơn vẫn chờ → cấp vé cho
  suất đã hủy). Event in-tx và interface chạy giống nhau lúc runtime; khác nhau ở ai biết ai lúc
  compile.
- **Một `DbContext` chung làm ranh giới module chỉ còn trên giấy.** Mỗi module một `DbContext`, chung
  connection + transaction giữ được cả ranh giới lẫn atomic.
- **Gọi ra ngoài (Stripe) không nằm trong DB transaction.** Hoàn tiền cần `RefundPending` + outbox +
  idempotency key phía Stripe.
- **Tự động hoá transaction sai ở đúng chỗ quan trọng nhất**: filter "commit khi `Result.IsSuccess`"
  làm hỏng ADR-0011 (giữ ghế thất bại vẫn phải commit kết quả `409` vào bản ghi idempotency). Ranh giới
  transaction phải nằm trong code mà reviewer đọc.
- **Dispose phải chịu được gọi nhiều lần.** `IUnitOfWork` đăng ký bằng factory trỏ tới `NpgsqlUnitOfWork`
  → DI theo dõi dispose của cả hai đăng ký → cùng object `DisposeAsync` hai lần → lần hai rollback
  transaction đã dispose. `/health` không lộ lỗi vì không qua nhánh "không commit"; integration test bắt
  được ngay lần chạy đầu. Không đưa `IDisposable` lên interface do DI sở hữu; `Dispose()` gọi
  `DisposeAsync().GetResult()` là sync-over-async.

## Idempotency

- **Một unique index là một hàng đợi.** Request trùng `Idempotency-Key` chèn cùng khoá sẽ *chờ* request
  đầu commit — không cần trạng thái `InProgress`. Savepoint giữ được kết quả từ chối mà vẫn nhả ghế
  (ADR-0011).
- **Webhook idempotent theo `event.id`**; thất bại của giao dịch chính rollback thì lần thất bại cũng
  phải idempotent (xem ADR-0011).
- **Hai nghĩa của idempotent** (no-op vs bị từ chối) — xem mục Đặc tả; ghi rõ nghĩa nào trong từng
  transition.

## Test và phép thử âm tính

- **Test chưa từng thấy đỏ thì chưa chứng minh gì.** Mỗi test cần một phép thử âm tính cụ thể:
  - thêm cạnh `Scheduling → Inventory.Contracts` → test quy tắc phụ thuộc đỏ;
  - cho `DbContext` tự mở connection → test Unit of Work đỏ.
- **Architecture test đọc csproj, không đọc assembly**: compiler bỏ reference không dùng khỏi assembly,
  nên assembly không phản ánh "ý định phụ thuộc".
- **`TreatWarningsAsErrors`** biến "build không warning" thành điều compiler kiểm và bắt cả API
  obsolete (constructor không tham số của `PostgreSqlBuilder`, Testcontainers 4.17).
- **Mọi claim hiệu năng/đúng đắn cần số đo hoặc test đi kèm**; đo baseline *trước* khi tối ưu.
- **Câu hỏi kiểm tra phải có đáp án duy nhất và đúng ở thời điểm hỏi** (tự tính lại số liệu trước khi
  hỏi; nói rõ tầng nào đang hỏi — "ghế phòng" hay "ghế suất").

## Hạ tầng .NET

- **Ghim `global.json`**: `dotnet new` dùng SDK mới nhất (11) nếu không ghim. `global.json` /
  `Directory.Build.props` chỉ có hiệu lực từ thư mục chứa nó trở xuống → bố cục `backend/` + `frontend/`
  mỗi toolchain một thư mục gốc.
- **Central Package Management**: template test ghi `Version=` trên `PackageReference` xung đột với CPM
  → xoá `Version=` ở csproj, khai báo ở `Directory.Packages.props`.
- **App đang chạy khoá DLL** làm `dotnet build` báo lỗi copy — không phải lỗi code; tắt app trước.
- **Testcontainers không chạy init script của Compose** → extension (`btree_gist`) phải khai báo trong
  migration (`HasPostgresExtension`), không chỉ trong init script.
- **Định dạng và cơ chế là hai quyết định khác nhau**: chốt shape JSON lỗi (problem+json) chưa nói lỗi
  đi từ domain tới HTTP bằng đường nào → `building-blocks.md`. Không phải "best practice" nào cũng hợp:
  generic repository, success envelope, MediatR đều có lý do cụ thể để không dùng ở đây (mục "Không
  chọn" của ADR-0016/0017).

## Quy trình làm việc

- **Ranh giới buộc dừng phải nằm ở file luôn trong context** (`AGENTS.md`) và kiểm được bằng exit
  checklist — "chốt hết đặc tả rồi làm một lần" thất bại vì không có gì buộc dừng, không phải vì thiếu
  cảnh báo.
- **Đưa tiêu chí trước, lựa chọn sau.** Người học không chọn được khi câu hỏi đưa lựa chọn trước khi có
  tiêu chí; chưa biết đáp án thì giảng bằng ví dụ cụ thể rồi mới hỏi. Một vòng hỏi một quyết định
  (tối đa hai nếu liên quan).
- **Đọc phần chữ, không chỉ đọc sơ đồ.** Mũi tên context map nói `Booking` upstream của `Ticketing`,
  nhưng phần chữ R4/R6 ghi ngược lại; lập luận theo sơ đồ dẫn tới kết luận sai.
- **Script ghi file tiếng Việt có thể làm hỏng ký tự** (`Add-Content` từng khối, `Write` nội dung dài
  → U+FFFD). Sau khi ghi: đọc lại và quét regex ký tự thay thế; script sửa file phải đọc → sửa → ghi
  *toàn bộ* file (một lần ghi mảnh regex đã làm mất nội dung).
- **Tiêu chí hoàn thành là câu hỏi cần trả lời, không phải ô checkbox.** Làm theo gợi ý của ticket
  (dựng prototype) thay vì đặc tính của câu hỏi là sai nguyên nhân.
- **Ticket code viết trước khi có design thì lệch tên và cơ chế**; rà lại ticket ngay khi design chốt,
  không đợi lúc bắt đầu code.
- **Khi AI tự viết glossary, đừng thêm từ mới.** Glossary là bản gom từ UL, context map, state machines,
  không phải chỗ đặt từ `_Avoid_` mới.
- **Số ticket là ID, không phải thứ tự** — chèn T28 thay vì đánh số lại T16–T27.

---

## Nhật ký ticket

<!-- Mục mới thêm ở đây theo template trong README.md -->

## 2026-11-26 — M0-01 Results
**Làm gì**: Viết `Result`, `Result<T>`, `Error`, `ErrorType`, `PagedResponse<T>` ở `Shared.Abstractions/Results/` theo 10 test đỏ; thêm project `TicketSystem.Shared.Tests` vào `.sln`.
**Vì sao làm thế này**: Lỗi nghiệp vụ là lỗi đã biết, xác định được, nên trả bằng `Result` thay vì exception: rẻ hơn và không bị bỏ sót `catch`. `Result` có constructor `protected` để `Result<T>` kế thừa được; `Result<T>` là `sealed` với constructor `private` để không tạo được instance không hợp lệ. `Result<T>.Failure` che `Result.Failure` và cần `new` (bỏ thì CS0108 thành lỗi build do `TreatWarningsAsErrors`).
**Phương án khác và vì sao không**: Exception cho lỗi nghiệp vụ (đắt, dễ bị `catch` nuốt); FluentResults/ErrorOr/OneOf (xem B1).
**Test chứng minh, đỏ khi nào**: `Failure_value_access_throws` đỏ đúng trên bản đầu `_value ?? throw`, vì với `Result<int>` thì `default` là `0` nên `Value` không ném mà trả `0`, người gọi tưởng thành công. Sửa bằng cách kiểm `IsFailure` thay vì kiểm `null`, và dùng `[AllowNull]` thay `!`. Các test còn lại đã chạy phép thử âm tính (bảng ở brief).
**Vấp ở đâu**: Kiểm "giá trị có null không" để suy ra thất bại chỉ đúng với kiểu tham chiếu; phải dựa vào trạng thái thất bại của lớp cha. Hai `Error` cùng `Code` nhưng khác `Extensions` không bằng nhau vì `Extensions` là reference type, nên test so `Code`/`Type`, không so cả `Error`.
**Còn mở**: —

## 2026-11-27 — M0-02 Domain primitives
**Làm gì**: Viết `IEntityId` / `IEntityId<TSelf>`, `EntityId.New<TId>()`, `IDomainEvent`, `ValueObject`, `Entity<TId>`, `AggregateRoot<TId>` ở `Shared.Abstractions/Domain/` theo 9 test (21 case) ở `Shared.Tests/Domain/`.
**Vì sao làm thế này**: Nếu handler quên gọi `DequeueDomainEvents` thì event vẫn còn trong aggregate (không được publish); gọi hai lần thì chỉ lần đầu có event. GUID v7 có timestamp nên sắp xếp được, tối ưu hơn cho index của database so với bản cũ (v4 ngẫu nhiên).
**Phương án khác và vì sao không**: Mỗi typed id tự gọi `Guid.CreateVersion7()`, typed id tự chặn `Guid.Empty` (B9 "Không chọn").
**Test chứng minh, đỏ khi nào**: 8 phép thử âm tính, mỗi cái làm đỏ đúng test của nó: `NewGuid` → `TypedId_new_is_uuidv7_and_ordered`; bỏ so `GetType()` → `Entities_with_different_id_or_type_are_not_equal`; `Dequeue` trả list nội bộ hoặc không `Clear` → `Dequeue_returns_events_and_clears`; `Id` có `set` → `Entity_has_no_public_setters`; bỏ chặn id rỗng → `TypedId_default_is_rejected_by_entity`; hash theo tham chiếu → `Entities_with_same_id_are_equal`; `ValueObject` có property `set` → `ValueObject_is_immutable`.
**Vấp ở đâu**: UUIDv7 chỉ tăng dần giữa các mili-giây; trong cùng mili-giây phần đuôi là ngẫu nhiên, nên test thứ tự phải tạo id cách nhau ≥ 1 ms.
**Còn mở**:
- Vì sao `ValueObject` chỉ là record marker, không cần `GetEqualityComponents()`.
- Vì sao `Entity.Equals` phải tự so `GetType()` dù `record` tự so kiểu.

## 2026-11-27 — M0-03 Application primitives
**Làm gì**: Viết `ICommand` / `IQuery`, `ICommandHandler` / `IQueryHandler`, `ICurrentUser`, `IOutboxWriter` ở `Shared.Abstractions/Application/`; `IIntegrationEvent`, `IEventPublisher`, `IEventHandler<T>` ở `Shared.Abstractions/Events/`; quét đăng ký `AddApplicationHandlers`, decorator log + trace, bus in-process ở `Shared.Infrastructure`. Spec tách integration event khỏi domain event (ADR-0019).
**Vì sao làm thế này**: Domain event là chuyện nội bộ của aggregate; event cho module khác là hợp đồng phẳng ở `.Contracts`, do module phát dịch ra và làm giàu được (ví dụ ghế của phòng cho `ShowtimeCreated`). Bus chạy đồng bộ trong transaction nên handler lỗi ở module nhận làm rollback cả module phát — giữ atomic cho năm ranh giới bị cắt. Decorator chỉ quan sát, không mở transaction (B7).
**Phương án khác và vì sao không**: Một loại event cho cả hai (domain bị trói vào hợp đồng, không làm giàu được payload); integration event luôn qua outbox (mất atomic); MediatR (license, che luồng gọi).
**Test chứng minh, đỏ khi nào**: Bus nuốt exception ⇒ `Failing_event_handler_rolls_back_whole_use_case` và `Failing_integration_handler_rolls_back_publisher` đỏ (chạy lại trên code cuối); dispatch theo kiểu khai báo ⇒ `EventBus_dispatches_by_runtime_type` đỏ; bỏ decorator ⇒ mọi `HandlerDecoratorTests` đỏ; đăng ký Singleton ⇒ `Handlers_and_bus_are_scoped` đỏ; quét cả lớp abstract ⇒ `Scan_ignores_non_handlers_and_abstracts` đỏ.
**Vấp ở đâu**: Placeholder trong message template là **tên trường** của structured log — `{HandlerName}` ở log bắt đầu và `{Handler}` ở log kết thúc là hai trường khác nhau, truy vấn theo `Handler` bỏ sót log bắt đầu (2 test đỏ). Visual Studio tự chèn `<Compile Include>` vào `.csproj` SDK-style ⇒ `NETSDK1022` duplicate item.
**Còn mở**:
- Vì sao domain event không được đặt ở `.Contracts`, và lớp dịch thuộc module nào.
- Bus đồng bộ trong transaction so với publish sau commit: được gì, mất gì.
