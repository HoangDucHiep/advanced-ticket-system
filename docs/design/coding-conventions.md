# Coding conventions — checklist viết và review code

Lớp 3 — Design. Đi kèm [`building-blocks.md`](building-blocks.md) (B1–B12).

Mỗi mục là một câu **kiểm được** khi đọc diff. Khi review (`/code-review`), AI đối chiếu với file này và
trích số mục (vd "C-07"). Mục nào có "vì sao" thì lý do nằm ngay cạnh — đây cũng là giáo trình.

Luật đã được compiler / analyzer / arch test kiểm thì **không** lặp ở đây: `Nullable`, `TreatWarningsAsErrors`
(`Directory.Build.props`), style `.editorconfig`, quy tắc phụ thuộc D9 (`ArchitectureTests`).

---

## 1. Async và tài nguyên

| # | Luật | Vì sao |
|---|---|---|
| C-01 | Method chạm I/O là `async`, tên kết thúc `Async`, nhận `CancellationToken ct` **cuối cùng** và truyền tiếp xuống mọi lời gọi | Client huỷ ⇒ query Postgres cũng huỷ, không giữ connection vô ích (quan trọng khi mở bán phim hot) |
| C-02 | Không `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` | Sync-over-async ⇒ chặn thread pool, deadlock dưới tải |
| C-03 | Không `async void` (trừ event handler của framework) | Exception không bắt được, làm sập tiến trình |
| C-04 | Không tự `new NpgsqlConnection` / mở connection trong module — luôn đi qua `IUnitOfWork` | ADR-0008: một connection, một transaction mỗi request |
| C-05 | Không `Dispose` thứ DI sở hữu (`DbContext`, `IUnitOfWork`) | DI dispose khi hết scope; dispose sớm ⇒ lỗi ở bước sau |

## 2. Thời gian, id, tiền

| # | Luật | Vì sao |
|---|---|---|
| C-06 | Không `DateTime.Now` / `DateTime.UtcNow` / `DateTimeOffset.UtcNow` trong code nghiệp vụ — dùng `TimeProvider` (B10); domain nhận `now` làm tham số | Test tua giờ được; một nguồn thời gian |
| C-07 | Thời gian là `DateTimeOffset` UTC, DB là `timestamptz` | Không lẫn múi giờ |
| C-08 | Id mới: `Guid.CreateVersion7()`; không `Guid.NewGuid()` cho khoá chính | UUIDv7 tăng dần, không làm phân mảnh B-tree (`data-model.md`) |
| C-09 | Tiền là `long` đơn vị **đồng**; không `decimal`/`double` cho VND | `api-contract.md`: VND không có phần lẻ |

## 3. Kiểu và hiển thị

| # | Luật | Vì sao |
|---|---|---|
| C-10 | Type trong project nội bộ module là `internal`; class là `sealed` trừ khi thiết kế để kế thừa. Ngoại lệ: `<Module>Module`, controller | Bề mặt public nhỏ = ít thứ phải giữ ổn định; `sealed` giúp JIT và nói rõ ý định |
| C-11 | Inject qua **primary constructor**; không service locator (`IServiceProvider.GetService` trong code nghiệp vụ) | Phụ thuộc nhìn thấy ở chữ ký |
| C-12 | DTO là `record` bất biến. Ba loại DTO không dùng lẫn: HTTP (`Api/`), `.Contracts`, domain ([`building-blocks.md`](building-blocks.md#cấu-trúc-thư-mục-bên-trong-một-module)) | Đổi schema DB không làm đổi API; không lộ `password_hash` |
| C-13 | Không dùng `!` (null-forgiving) để im compiler — sửa kiểu, hoặc kiểm rồi ném `InvalidOperationException` có message | `!` là nói dối compiler; null vẫn tới lúc chạy |
| C-14 | Không magic string cho mã lỗi, tên constraint, tên policy, tên section cấu hình — dùng hằng số (`UserErrors.EmailTaken`, `JwtOptions.SectionName`) | Đổi một chỗ; tìm được bằng "Find references" |

## 4. Lỗi và luồng điều khiển

| # | Luật | Vì sao |
|---|---|---|
| C-15 | Lỗi nghiệp vụ trả `Result` thất bại, không `throw` (B1, B2) | Chữ ký nói thật; rẻ ở hot path |
| C-16 | Không `catch (Exception)` trong module. Chỉ bắt exception **cụ thể** mà mình xử lý được (vd `PostgresException` có `SqlState == "23505"` + tên constraint) | Nuốt lỗi = bug biến mất khỏi log; `GlobalExceptionHandler` lo phần còn lại |
| C-17 | Mọi `Error` có `Code`, `Title`, `Action` không rỗng; `Code` khớp bảng của `api-contract.md` | `FR-36`; client dựa vào `code` |
| C-18 | Tài nguyên của người khác ⇒ `NotFound`, không `Forbidden` | `FR-10`: không rò rỉ việc tài nguyên tồn tại |
| C-19 | Handler mở transaction **tường minh** và chỉ commit ở cuối; không commit hai lần; không gọi `BeginAsync` trong vòng lặp (B7) | Ranh giới transaction phải review được |
| C-20 | Mỗi câu SQL tay có comment một dòng: **vì sao ORM không biểu đạt được** (quy tắc ở [`README.md`](README.md)) và tham chiếu `concurrency.md #n` nếu có | Lý do của SQL tay là thứ dễ mất nhất khi refactor |

## 5. Log và bảo mật

| # | Luật | Vì sao |
|---|---|---|
| C-21 | Log dạng **message template**: `logger.LogInformation("User {UserId} registered", id)` — không string interpolation `$"..."` | Structured log query được theo trường (xem phần observability ở [`architecture.md`](architecture.md#stack)); không cấp phát chuỗi khi log level tắt |
| C-22 | Không log password, token (access / refresh), hash, `Authorization` header, connection string, secret Stripe | Log là nơi rò rỉ phổ biến nhất |
| C-23 | So sánh giá trị bí mật (hash token) bằng `CryptographicOperations.FixedTimeEquals`, không `==` / `SequenceEqual` | Chống timing attack |
| C-24 | Số ngẫu nhiên cho token: `RandomNumberGenerator`, không `Random` | `Random` đoán được |
| C-25 | Cấu hình qua Options pattern + `ValidateOnStart` (B11); secret chỉ từ user-secrets / biến môi trường | Fail fast; không commit secret |

## 6. Đặt tên

| Thứ | Mẫu | Ví dụ |
|---|---|---|
| Lệnh | `<Verb><Noun>Command` | `RegisterUserCommand`, `HoldSeatsCommand` |
| Truy vấn | `Get<Noun>Query` / `List<Noun>Query` | `GetCurrentUserQuery` |
| Handler | `<Command/Query bỏ hậu tố>Handler` | `RegisterUserHandler` |
| HTTP request / response | `<Action>Request`, `<Noun>Response` | `RegisterRequest`, `UserResponse` |
| Lỗi | `<Aggregate>Errors.<Tên>` | `UserErrors.EmailTaken` |
| Repository | `I<Aggregate>Repository` | `IUserRepository` |
| SQL tay | tên theo việc | `SeatHoldSql` |
| Options | `<Nhóm>Options` + `SectionName` | `JwtOptions` |

Tên class, bảng, trạng thái lấy **đúng** từ [`GLOSSARY.md`](../../GLOSSARY.md) — không dịch, không đồng
nghĩa (`Booking`, không `Order`; `Showtime`, không `Screening`).

## 7. Test

| # | Luật | Vì sao |
|---|---|---|
| C-26 | Tên test: `<Method/Hành vi>_<điều kiện>_<kết quả>` (vd `Register_email_taken_returns_409`) hoặc câu mô tả bằng `_` như arch test hiện có | Đọc tên test là biết hành vi |
| C-27 | Cấu trúc Arrange / Act / Assert, mỗi test **một** hành vi; assert bằng Shouldly | Test đỏ chỉ ra đúng một nguyên nhân |
| C-28 | Unit test domain (`tests/Modules/TicketSystem.<Module>.Tests/`) không chạm DB, không mock domain | Domain thuần ⇒ test nhanh |
| C-29 | Integration test đi qua HTTP thật (`WebApplicationFactory<Program>`) + Postgres/Redis thật (Testcontainers). **Không** EF InMemory, **không** SQLite | Exit checklist Gate D; InMemory không có constraint, không có khoá |
| C-30 | Mỗi test concurrency ghi rõ **phép thử âm tính**: bỏ cơ chế X ⇒ test này đỏ — và đã thật sự chạy thử một lần | "Test chưa từng đỏ thì chưa chứng minh gì" |
| C-31 | Assert hợp đồng lỗi bằng `code`, không bằng `title`/`detail` | `title` là văn bản cho người, đổi được |
| C-32 | Thời gian trong test dùng `FakeTimeProvider`; không `Task.Delay` để "chờ hết hạn" | Test nhanh và không flaky |

## 8. Smell — dừng lại khi thấy

| Smell | Thường có nghĩa là |
|---|---|
| Handler > ~80 dòng, hoặc gọi > 3 repository | Logic nghiệp vụ nằm ở handler thay vì domain; hoặc use case cần tách |
| Entity có `public set` | Anemic model — invariant không được bảo vệ |
| `if (user.Role == ...)` rải ở nhiều handler | Luật thiếu một method trên aggregate, hoặc thiếu policy phân quyền |
| Controller có `if` nghiệp vụ | Logic sai tầng — controller chỉ map HTTP ↔ command/result |
| `DbContext` của module A có `DbSet` bảng schema B | Vi phạm D5 |
| Class tên `*Helper`, `*Utils`, `*Manager` | Chưa tìm ra trách nhiệm thật |
| Thêm vào `Shared` "phòng khi sau này cần" | Vi phạm luật 2 của D6 |
| `try { … } catch { }` | Nuốt lỗi |
| Comment giải thích **cái gì** code làm | Đổi tên cho rõ; comment chỉ để giải thích **vì sao** |
