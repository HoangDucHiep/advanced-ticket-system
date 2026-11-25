# 00 — Overview

Lớp 1 — PRD. File này nói **hệ thống dành cho ai, giải quyết vấn đề gì, và thành côngtrông như thế nào**. Nó không chứa giải pháp kỹ thu.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/01-overview.md`.

## Vấn đề

> [!NOTE]
> **Hệ thống đặt vé xem phim** — người xem chọn **ghế** trong một suất chiếu, giữ chỗ, thanh
> toán, rồi mang mã QR tới cổng để vào rạp.

Bối cảnh của dự án này là **học tập**: mục tiêu là chọn một nghiệp vụ đủ khó để mọi bài toán
concurrency xuất hiện **tự nhiên**, chứ không phải cố tình chèn vào. Nghiệp vụ đặt vé xem phim
đạt điều kiện đó: mỗi **ghế** là một đơn vị bán riêng biệt, nhiều người tranh cùng một ghế,
người mua thường mua **nhiều ghế cùng lúc** (mua cho cả nhóm), có tiền thật, có dịch vụ bên
ngoài gửi tin không kiểm soát được, và có hành động vật lý tại chỗ.

Bốn nỗi đau mà hệ thống này tập trung giải quyết:

1. **Hai người cùng đặt được một ghế** — ghế đã có người nhưng hệ thống vẫn bán cho cả hai.
2. **Mua 4 ghế mà chỉ giữ được 3** — người mua bị trừ tiền cho cả 4 nhưng chỉ nhận được 3 vé.
3. **Thanh toán xong nhưng vé không cấp được, hoặc vé bị cấp hai lần** — do kết quả thanh
   toán từ bên thứ ba thông báo đến lặp hoặc sai thứ tự.
4. **Suất chiếu hot bị sập** — không chịu nổi tải, người xem không biết ai thắng.

Cả bốn đều là bài toán **tính đúng đắn dưới tranh chấp**, không phải bài toán tốc độ thuần
túy. Đó là lý do nghiệp vụ này được chọn.

## Actors

Bốn vai trò nghiệp vụ. Dịch vụ thanh toán bên thứ ba **không** phải actor: nó là một dịch vụ
bên ngoài được tích hợp, thuộc về Lớp 3 (thiết kế kỹ thuật), không phải Lớp 1.

| Actor              | Là ai                                                           |
| ------------------ | --------------------------------------------------------------- |
| **Người xem**      | Khách xem phim — cần đăng nhập để quản lý vé của mình           |
| **Quản lý rạp**    | Bên quản lý phòng chiếu, lịch chiếu và sơ đồ ghế                |
| **Nhân viên cổng** | Người trực cổng kiểm tra vé khi khách vào rạp                   |
| **Quản trị viên**  | Người quản lý toàn hệ thống — không phải quản lý rạp cụ thể nào |

## Jobs

Job là thứ actor **muốn**, không phải thứ hệ thống **làm**. Phần "hệ thống phải làm gì để
đáp ứng job" nằm ở `01-requirements.md` và `03-user-journeys.md`.

### Người xem

| Job                             | Ý nghĩa                                                                                                                |
| ------------------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| Tìm phim và lịch chiếu          | Biết phim nào đang chiếu, suất nào, còn ghế nào                                                                        |
| Chọn ghế                        | Biết sơ đồ phòng, ghế nào còn trống, ghế nào giá bao nhiêu                                                             |
| Đặt vé                          | Giữ ghế, đếm ngược, thanh toán, nhận vé                                                                                |
| Đặt được vé khi mở bán phim hot | Tranh được ghế khi có tranh chấp gay gắt                                                                               |
| Hiểu vì sao không đặt được      | Biết rõ hệ thống từ chối vì lý do gì (ghế đã có người, hết thời gian giữ chỗ, thanh toán lỗi) và biết phải làm gì tiếp |
| Xem và dùng vé đã mua           | Lấy được mã QR kèm số ghế để mang tới cổng                                                                             |

### Quản lý rạp

| Job                        | Ý nghĩa                                             |
| -------------------------- | --------------------------------------------------- |
| Quản lý phòng và sơ đồ ghế | Dựng phòng, khai báo ghế và loại ghế, giá theo loại |
| Lên lịch chiếu             | Đặt phim vào phòng vào một giờ cụ thể, đặt giá vé   |
| Mở bán vé                  | Đưa suất chiếu vào trạng thái bán                   |
| Theo dõi tình trạng bán    | Biết còn bao nhiêu ghế trống, đã bán bao nhiêu      |

### Nhân viên cổng

| Job                             | Ý nghĩa                                               |
| ------------------------------- | ----------------------------------------------------- |
| Quét và xác minh vé             | Xác định người đó có vé hợp lệ, ghế nào, và chưa dùng |
| Làm được khi mất mạng (pending) | Cổng không dừng hoạt động vì mất kết nối              |
| Chặn vé không hợp lệ            | Vé giả, vé đã quét trước, vé của suất chiếu khác      |
| Ghi nhận số người đã vào        | Đếm số lượt check-in thành công                       |

### Quản trị viên

| Job                         | Ý nghĩa                                         |
| --------------------------- | ----------------------------------------------- |
| Quản lý toàn bộ rạp         | Duyệt, ẩn, chỉnh phim và lịch chiếu của mọi rạp |
| Quản lý người dùng và quyền | Cấp, thu quyền                                  |
| Giám sát vận hành           | Theo dõi sức khỏe hệ thống khi mở bán phim hot  |
| Truy vết giao dịch          | Lần theo lịch sử khi có khiếu nại               |

## Success criteria

Mỗi tiêu chí dưới đây phải **kiểm chứng được** bằng một test hoặc một phép đo. Tiêu chí
không sinh ra được phép đo nào thì là văn hoa, và sẽ bị loại khỏi README ở tầng CV.

### Tính đúng đắn — bắt buộc đúng 100%

| #     | Tiêu chí                                                                                                       | Bằng chứng                                                                |
| ----- | -------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------- |
| SC-01 | Một ghế **không bao giờ** được bán cho hai người                                                               | 2 người cùng đặt 1 ghế → **đúng** 1 thắng, 1 thua                         |
| SC-02 | Đặt N ghế phải **giữ được đủ N** hoặc **không giữ ghế nào**                                                    | Giữ được 3 trong 4 ghế → cả 4 ghế phải trở về trống                       |
| SC-03 | Không bao giờ xảy ra **kẹt** khi hai người tranh cùng một lúc ghế                                              | 2 người chọn ghế ngược thứ tự → cả hai đều nhận được phản hồi, không treo |
| SC-04 | Mỗi lần thanh toán chỉ cấp **đúng một** bộ vé, dù thông báo kết quả thanh toán từ bên thứ ba đến lặp nhiều lần | Gửi cùng một thông báo 3 lần → đúng 1 lần cấp vé                          |
| SC-05 | Mỗi vé chỉ check-in được **đúng một lần** — kể cả khi quét liên tục, hoặc mất mạng rồi quét lại                | Quét cùng một vé 10 lần → 1 bản ghi check-in                              |
| SC-06 | Không tạo ra hai đơn đặt vé cho cùng một yêu cầu, kể cả khi người dùng gửi lại yêu cầu cũ                      | 50 request cùng định danh yêu cầu → đúng 1 đơn                            |
| SC-07 | Không mất ghế nào — chỗ giữ hết hạn phải trả lại ghế cho người khác đặt được                                   | Ghế hết thời gian giữ chỗ → lập tức người khác đặt được                   |

### Hiệu năng — mục tiêu, con số chốt sau khi đo baseline

| #     | Tiêu chí                                                                                                                                                         | Bằng chứng                                       |
| ----- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------ |
| SC-08 | Đọc: p99 đáp ứng mục tiêu dưới tải đọc thông thường                                                                                                              | Số chốt sau baseline (T23)                       |
| SC-09 | Ghi dưới tải mở bán phim hot: p99 đáp ứng mục tiêu                                                                                                               | Số chốt sau baseline (T23)                       |
| SC-10 | Mở bán phim hot **không sập**: không có yêu cầu nào bị treo quá thời gian chờ, hệ thống vẫn phản hồi được thay vì không đáp ứng, và không ghế nào bị bán hai lần | Kịch bản mở bán ở T23 + kiểm tra invariant SC-01 |
| SC-11 | Phần lớn lưu lượng xem sơ đồ ghế được phục vụ mà không phải đọc tới nơi lưu trữ dữ liệu gốc                                                                      | Tỉ lệ trúng đo được ở T17/T23                    |

> **Vì sao không ghi con số cứng:** nếu PRD ghi `p99 < 200ms` rồi đo ra `350ms`, bạn phải sửa PRD
> hoặc nói dối. Ghi "mục tiêu, chốt sau baseline" vẫn trung thực và vẫn đo được — và nó buộc
> bạn **đo trước khi cam kết**, đúng nguyên tắc của cả dự án.

### Tiêu chí bằng chứng

| #     | Tiêu chí                                                                                                                       |
| ----- | ------------------------------------------------------------------------------------------------------------------------------ |
| SC-12 | Mọi tuyên bố về tính đúng đắn và hiệu năng trong repo đều có **test hoặc số đo** đi kèm. Không một câu nào là khẳng định trần. |

SC-12 là tiêu chí quan trọng nhất với mục đích CV: nó biến "tôi đã xử lý concurrency" (không
có giá trị) thành "test này fail nếu bỏ cơ chế đó" (có giá trị, và không ai phản bác được).

## Bài học mong muốn có được

Đây là tiêu chí thành công quan trọng nhất về mặt cá nhân, và nó cũng là công cụ **cắt scope**
sau này: mỗi khi bạn muốn thêm một tính năng, hãy hỏi nó có giúp học được điều này không.

- Hiểu cơ chế nào thực sự ngăn được việc bán trùng, và **đo** sự khác biệt giữa các phương án.
- Hiểu vì sao giữ **nhiều ghế cùng lúc** là bài toán khác hẳn giữ **một ghế**, và vì sao thứ tự
  khoá lại quan trọng.
- Biết viết test concurrency có giá trị — test mà **fail** khi cơ chế bị thay bằng cách sai.
- Biết thiết kế để một request có thể đến lặp, đến trễ, đến sai thứ tự mà hệ thống vẫn đúng.
- Biết đo hiệu năng có baseline, không chỉ có con số đẹp.

## Ghi chú

- File này nói về **nghiệp vụ**. Cơ chế, mô hình lưu trữ, giao diện lập trình thuộc về Lớp 2 và
  Lớp 3 — không có ở đây. (Ngoại lệ duy nhất: `p99` trong success criteria là **cách đo**,
  không phải cơ chế.)
- 12 bài toán concurrency và stack công nghệ đã chốt nằm ở `docs/plan/00-roadmap.md`.
