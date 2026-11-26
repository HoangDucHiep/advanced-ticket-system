# Khái niệm

Giảng các khái niệm nền tảng được dùng trong tài liệu của project.

## DDD trong project này

### Ba dạng thứ DDD phân biệt

| | Có định danh? | Có vòng đời? | Ví dụ ở đây |
|---|---|---|---|
| **Entity** | Có | Có | `Showtime seat` D7 của suất 19h — là "cái ghế đó" qua mọi thay đổi trạng thái, dù đã được giữ rồi bán rồi check-in |
| **Value Object** | Không, chỉ có **giá trị** | Không | `Seat type Standard giá 90k trong suất 19h` — đổi giá là tạo VO mới, không phải sửa. `Ticket price` cũng là VO |
| **Aggregate** | — | — | Entity gốc + những thứ bảo vệ **cùng nhau** |

Value Object hữu ích vì **bất biến** và **so sánh bằng giá trị**: hai `Seat type Standard giá 90k` là
bằng nhau, không phải "cùng một cái".

### Câu hỏi quyết định ranh giới aggregate

> **"Có thay đổi nào mà nếu chỉ một phần được thay đổi thì hệ thống thành sai không?"**

Áp dụng vào thao tác **giữ ghế** — một thao tác làm hai việc cùng lúc:

1. `Showtime seat` D7 chuyển `Available` → `Held`.
2. `Booking` chuyển sang trạng thái chờ thanh toán, kèm 4 ghế trong danh sách.

- Chỉ (1) xảy ra: một ghế bị giữ mà không ai chịu trách nhiệm, không ai trả tiền, không ai có vé.
  Treo tới hết đời.
- Chỉ (2) xảy ra: một đơn chờ thanh toán cho 4 ghế mà không ai giữ. Người khác đặt được 4 ghế đó,
  rồi người xem trả tiền và nhận vé cho ghế **đã bán cho người khác**.

Cả hai đều sai ⇒ ghế và đơn phải thay đổi cùng lúc, không thể tách ⇒ chung một phạm vi atomic.

### Ranh giới nằm giữa "giữ ghế" và "thanh toán"

`Hold seats` (lúc người xem bấm xác nhận) và `Pay` (sau đó, qua cổng thanh toán bên ngoài) **không
phải một việc**: thanh toán có thể đến vài giây, vài phút, thậm chí vài giờ sau. Gộp chúng làm một
aggregate nghĩa là giữ một lần ghi mở suốt thời gian chờ người xem bấm "thanh toán" — không thể. Phần
trước (giữ ghế + trạng thái ghế + đơn) là một khối; phần sau (thanh toán) là khối khác.

### Ranh giới aggregate ≠ ranh giới atomic

Aggregate là đơn vị nhất quán **nhỏ nhất** mà một invariant cần. Một lần ghi atomic trong cùng một
database có thể chạm nhiều aggregate khi được ghi rõ (ví dụ giữ n ghế = n `Showtime seat` + 1
`Booking`). Chỉ khi N aggregate nằm ở N database mới cần compensating transaction. Câu "ranh giới
aggregate là transaction boundary" nghe gọn nhưng **không chính xác**.

### Một suất chiếu có hàng trăm ghế — có nên là một aggregate không?

Không: nếu tất cả ghế nằm trong cùng aggregate với đơn, đó là hàng trăm dòng bị khoá trong một lần
ghi. Mỗi `Showtime seat` là một aggregate root, all-or-nothing do một lần ghi atomic bảo đảm. Quyết
định và so sánh phương án: [`../domain/aggregates/README.md`](../domain/aggregates/README.md),
[ADR-0005](../adr/0005-showtime-seat-aggregate.md).

Ngược lại `Room seat` nằm gọn trong aggregate `Room` dù phòng có hàng trăm ghế: ghế vật lý **tĩnh**,
chỉ ghi khi quản lý rạp thiết lập lại phòng, nên không gây contention. Ranh giới aggregate quan tâm
tới **tần suất ghi**, không chỉ số dòng.
