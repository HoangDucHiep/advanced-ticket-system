# Identity & Access — Aggregate và invariant

Bounded context `Identity & Access` (xem [`../context-map.md`](../context-map.md)).

Thuật ngữ: [`../ubiquitous-language.md`](../ubiquitous-language.md).

## Trách nhiệm

Quản lý người dùng và quyền của họ.

`Identity & Access` là **Supplier** của mọi context khác (xem R8): các context khác **hỏi**,
chứ không sở hữu dữ liệu này. Vì vậy aggregate ở đây là aggregate đơn giản nhất trong hệ thống.

---

## Aggregate: `User`

**Bản chất**: entity. Một tài khoản — có định danh, có vòng đời.

**Chứa**:

| Thành phần | Bản chất | Vai trò |
|---|---|---|
| Vai trò (`Customer`, `Theater manager`, `Gate staff`, `Administrator`) | value object | Người dùng thuộc vai trò nào |
| Tập quyền | value object | Các quyền cụ thể được cấp |
| Lịch sử thay đổi quyền | append-only | Mọi lần cấp/thu quyền, kèm thời điểm và người thực hiện |
| Mật khẩu / thông tin đăng nhập | — | Xem T16 (Lớp 4) |

**Boundary**: nằm **ngoài** aggregate này —

- `Booking` (thuộc `Booking`) — đơn và vé, để kiểm tra chủ sở hữu;
- `Showtime` (thuộc `Scheduling`) — lịch chiếu, để kiểm tra quyền sở hữu;
- `Ticket` (thuộc `Ticketing`) — vé, để kiểm tra chủ sở hữu.

### Invariants

- **INV-01** — Chỉ **quản trị viên** xem được danh sách người dùng, và cấp hoặc thu quyền của họ.
  Mọi lần thay đổi quyền được **ghi lại**.
  - *Cơ chế bảo vệ*: quyền thuộc `Identity & Access`, và lệnh cấp/thu quyền **kiểm tra vai trò
    của người gọi** ở biên aggregate. Không có lệnh nào khác ghi vào tập quyền.
  - *Ai có thể vi phạm*: một endpoint cấp quyền không kiểm tra vai trò — bất kỳ ai đăng nhập đều
    có thể tự phong quản trị viên.
  - *Traces to*: `BR-26`, `FR-29`.

- **INV-02** — Thu quyền phải có **hiệu lực ngay**: sau khi thu quyền, người dùng **mất** khả
  năng tương ứng, và thay đổi được ghi nhận lại.
  - *Cơ chế bảo vệ*: lệnh thu quyền là **một transaction duy nhất** gồm: gỡ quyền khỏi tập
    quyền **và** ghi một dòng lịch sử. Nếu chỉ một phía được ghi, thì hoặc quyền vẫn còn (lỗ hổng)
    hoặc mất quyền mà không có dấu vết.
  - *Ai có thể vi phạm*: code thu quyền ở một nơi và ghi log ở nơi khác, không cùng transaction.
  - *Traces to*: `BR-26`, `FR-29`.

- **INV-03** — Lịch sử thay đổi quyền là **append-only** — không sửa, không xoá.
  - *Cơ chế bảo vệ*: aggregate chỉ **thêm** vào lịch sử; không có hành vi nào sửa hoặc xoá.
  - *Ai có thể vi phạm*: mã nguồn có quyền ghi trực tiếp bảng lịch sử ngoài lĩnh vực của
    aggregate.
  - *Traces to*: `BR-26`, `FR-29`.

- **INV-04** — Một người dùng có **đúng một** vai trò.
  - *Cơ chế bảo vệ*: mô hình — vai trò là **một** value object, không phải tập. Điều này loại
    bỏ một lớp câu hỏi "quản lý rạp có được quét vé không?" — trả lời: không, vì chỉ `Gate staff`
    và `Administrator` được quét vé.
  - *Ai có thể vi phạm*: không ai, miễn là không ai thêm trường "danh sách vai trò". Nếu sau này
    cần nhiều vai trò cùng lúc thì phải **thiết kế lại** mô hình, chứ không vá bằng cách cho
    phép một trường chứa nhiều giá trị.
  - *Traces to*: `BR-15`, `BR-16`, `BR-26`.

- **INV-05** — Người dùng chỉ thao tác được trên **đơn và vé của chính mình**.
  - *Cơ chế bảo vệ*: điều kiện này **không thuộc** aggregate `User` — nó là quan hệ giữa
    người dùng và `Booking`/`Ticket`. Vì vậy nó được bảo vệ ở **biên aggregate của `Booking` và
    `Ticket`** (xem `INV-09` ở [`03-booking.md`](03-booking.md) và `INV-07` ở
    [`05-ticketing.md`](05-ticketing.md)). Ở đây chỉ ghi lại để thấy rõ **nguồn sự thật của điều
    kiện nằm ở đâu**: không phải ở `Identity & Access`.
  - *Ai có thể vi phạm*: endpoint đọc đơn/vé bằng định danh mà không kiểm tra chủ sở hữu.
  - *Traces to*: `BR-14`, `FR-10`.

- **INV-06** — Chỉ **nhân viên cổng** và **quản trị viên** ghi nhận check-in; nhân viên cổng chỉ
  ghi nhận được vé thuộc suất chiếu mà họ được phân công trực.
  - *Cơ chế bảo vệ*: **hai điều kiện** được kiểm tra cùng lúc — vai trò (thuộc `User`) **và**
    phân công suất chiếu (thuộc `Scheduling`). Điều kiện thứ hai **không** thuộc aggregate
    `User` — nó là quan hệ giữa `Identity & Access` và `Scheduling`.
  - *Ai có thể vi phạm*: chỉ kiểm tra vai trò mà bỏ qua phân công — nhân viên cổng A quét vé của
    suất chiếu thuộc cổng B.
  - *Traces to*: `BR-15`, `FR-33`, `FR-35`.

### Transaction boundary

Một transaction duy nhất cho **mỗi lần thay đổi quyền**:

1. Kiểm tra vai trò người gọi là quản trị viên;
2. Cập nhật tập quyền;
3. Ghi một dòng lịch sử thay đổi quyền.

Một transaction duy nhất cho **mỗi lần đăng nhập / làm mới phiên đăng nhập** — chi tiết cơ chế
(JWT, refresh rotation, reuse detection) là Lớp 4, xem T16.

Ranh giới transaction = **ranh giới aggregate**.

---

## Điểm đáng chú ý: aggregate này không chứa gì của aggregate khác

`Identity & Access` là context duy nhất trong hệ thống mà aggregate của nó **không tham chiếu
tới** aggregate của context nào khác. Mọi thứ nó biết về người khác — đơn, vé, lịch chiếu —
đều là **thứ được hỏi**, không phải thứ được giữ.

Đây là hệ quả trực tiếp của việc chọn quan hệ `Customer/Supplier` thay vì `Shared Kernel` ở
T06. Nếu ta chọn `Shared Kernel`, aggregate `User` sẽ phải chứa một phần mô hình của `Booking`,
và ranh giới sẽ nở ra — và mọi thay đổi ở một bên sẽ phải đổi cả bên kia.

---

## Nguồn sự thật

| Dữ liệu | Nguồn sự thật | Ai được đọc |
|---|---|---|
| Người dùng và quyền | `Identity & Access` | tất cả |
| Đơn và vé (để kiểm tra chủ sở hữu) | `Booking` / `Ticketing` | `Identity & Access` (hỏi khi cần) |
| Phân công cổng (để kiểm tra quyền quét vé) | `Scheduling` | `Identity & Access` (hỏi khi cần) |
