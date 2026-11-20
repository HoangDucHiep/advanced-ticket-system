# 02 — Non-requirements

Lớp 1 — PRD. File này nói hệ thống **cố tình không làm gì**, và **vì sao**.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/03-non-requirements.md`.
Bối cảnh: [`00-overview.md`](00-overview.md), [`01-requirements.md`](01-requirements.md).

## Cách đọc

Mỗi mục có:

- **Lý do** — thuộc một trong ba loại, ghi rõ ngay đầu dòng:
  - `nghiệp vụ` — nằm ngoài mô hình nghiệp vụ đã chốt, hoặc không phục vụ mục tiêu học tập.
  - `thời gian` — làm được, nhưng tốn thời gian hơn giá trị học tập thu được trong 12 tuần.
  - `kỹ thu / quy mô` — không làm được trong phạm vi một project học tập.
- **Nếu sau này cần** — chi phí và thứ bị ảnh hưởng khi thêm vào.

Danh sách này là **hợp đồng với chính mình**. Trước khi thêm bất kỳ tính năng nào, đối chiếu
với file này trước. Nếu tính năng đó không có ở đây và lại tốn công, thì phải **chủ động thêm
vào danh sách này** kèm lý do — không lặng lẽ làm.

---

## Nhóm 1 — Lý do nghiệp vụ

### NR-01 — Không có mô hình vé theo hạng, chỉ có ghế

Hệ thống không có khái niệm hạng vé (`VIP hạng A`, `hạng B`...) với số lượng cấu phép riêng.
Đơn vị bán duy nhất là **từng ghế cụ thể** của một suất chiếu. Loại ghế (Standard / Couple /
VIP) chỉ quyết định **giá**, không quyết định số lượng.

- Lý do: `nghiệp vụ` — mô hình đếm số không biểu diễn được chỗ ngồi, và đã bị thay thế
  bằng ADR-0003. Giữ song song hai mô hình tồn kho nghĩa là hai bộ invariant và hai bộ câu
  tranh chấp cùng tồn tại, và người đọc không biết cái nào đang áp dụng.
- Nếu sau này cần: thêm một khái niệm hạng vé sẽ phải chốt lại toàn bộ mô hình tồn kho,
  và mọi cơ chế concurrency ở T12 phải thiết kế lại. Đây là thay đổi **không đảo ngược được**.

### NR-02 — Không bán sản phẩm kèm theo (combo bắp nước)

Hệ thống không bán vé kèm combo đồ ăn uống, không có đơn hàng chứa cả vé lẫn hàng hóa, và
không có quản lý tồn kho cho món ăn.

- Lý do: `nghiệp vụ` — món ăn là một loại tồn kho thứ hai, có vòng đời hết hạn riêng, và
  không sinh ra bài toán tranh chấp nào mà bài toán ghế chưa giải quyết. Thêm vào chỉ làm loãng
  trọng tâm.
- Nếu sau này cần: một bảng tồn kho món ăn theo suất chiếu, một loại đơn mới (đơn hàng
  hỗn hợp), và một nhánh thanh toán riêng. Ước lượng 1 tuần.

### NR-03 — Không có chuyển nhượng vé

Vé không thể chuyển nhượng cho người khác, không có mã quyền chuyển nhượng, và không có cơ chế
"người xem A nhường vé cho người xem B".

- Lý do: `nghiệp vụ` — sở hữu vé gắn với một tài khoản người xem; đây là bài toán **quyền sở
  hữu**, khác hẳn bài toán **tranh chấp tồn kho** mà dự án này chọn để học.
- Nếu sau này cần: thêm một khái niệm "quyền sử dụng vé" tách khỏi chủ vé, kèm cơ chế
  chấp nhận lời chuyển nhượng và xử lý vé đã chuyển khi đã check-in.

### NR-04 — Không có tích điểm, coupon, mã giảm giá, vé tặng

Hệ thống không có chương trình tích điểm, không có mã giảm giá, không có vé tặng kèm, không
có giá thành viên.

- Lý do: `nghiệp vụ` — cần một cơ chế định giá riêng và một bảng quy tắc khuyến mãi, không
  tạo ra bài toán concurrency nào. Trong khuôn khổ này, **giá là thuộc tính của loại ghế
  trong suất chiếu** — không có chỗ để "áp" khuyến mãi vào.
- Nếu sau này cần: một bảng quy tắc giảm giá và một bước tính giá trước khi thanh toán.
  Ước lượng 1–2 tuần.

### NR-05 — Không tự động xếp ghế cho đơn lớn

Khi người xem chọn nhiều ghế, hệ thống **không** tự tìm và gán một cụm ghế liền nhau hộ
người xem. Người xem tự chọn từng ghế trên sơ đồ.

- Lý do: `nghiệp vụ` — "tìm cụm ghế liền nhau tối ưu" là một bài toán thuật toán, không phải
  bài toán **tính đúng đắn dưới tranh chấp**. Dự án này chọn những bài toán mà tính đúng đắn
  mới là điểm khó, không phải hiệu năng.
- Nếu sau này cần: một thuật toán chọn cụm ghế liền nhau, cộng với việc giữ được cụm đó
  nguyên tử — tức là biến một yêu cầu trả lời thành một yêu cầu chiếm tài nguyên.

---

## Nhóm 2 — Lý do thời gian

### NR-06 — Không có ứng dụng mobile

Hệ thống chỉ có giao diện web. Không có app iOS, không có app Android, không có backend dành
riêng cho mobile.

- Lý do: `thời gian` — một app mobile kéo theo pipeline build, kho ứng dụng, vòng duyệt, và
  một bộ API riêng cho mobile; cắt nguyên một đường khỏi kế hoạch 12 tuần. Toàn bộ hành
  trình người xem đều demo được trên web.
- Nếu sau này cần: một client mobile và một bộ giao diện lập trình riêng cho mobile.
  Ước lượng 3–4 tuần.

### NR-07 — Chỉ tích hợp một cổng thanh toán

Hệ thống tích hợp **một** cổng thanh toán. Không hỗ trợ nhiều cổng, không có bảng định tuyến
chọn cổng thanh toán theo rạp hay theo phương thức.

- Lý do: `thời gian` — mỗi cổng là một bài toán thông báo kết quả riêng (xác thực chữ ký,
  định danh sự kiện lặp, thứ tự đến). Học một cách sâu còn hơn học ba cách nông, và bài toán
  thông báo đến lặp (mục #7 trong bảng 12 bài toán) là phần đáng khoe nhất của nhánh thanh
  toán.
- Nếu sau này cần: một lớp trung gian chung cho cổng thanh toán và một bộ chuyển đổi cho
  mỗi cổng. Ước lượng 2 tuần cho cổng thứ hai.

### NR-08 — Không có đánh giá phim, bình luận, diễn đàn

Không có chức năng đánh giá sao, bình luận phim, diễn đàn, hoặc xếp hạng phim theo lượt
xem.

- Lý do: `thời gian` — không liên quan đến bất kỳ bài toán tranh chấp nào, và là mẫu tính
  năng dễ làm mà không tạo ra giá trị học tập nào cho dự án này.
- Nếu sau này cần: một kho lưu đánh giá và một bộ thao tác ghi/đọc. Ước lượng 2–3 ngày.

### NR-09 — Không có hồ sơ người dùng giày dặn, không có đề xuất phim

Hệ thống không lưu lịch sử xem, không lưu phim yêu thích, không gợi ý phim, không có phân
khúc khách hàng.

- Lý do: `thời gian` — đây là chức năng **bảo trì dài hạn**, không phải điểm nhấn. Với
  dự án 12 tuần, thời gian đó dùng để làm phần concurrency chắc chắn hơn là để xây hồ sơ
  người dùng.
- Nếu sau này cần: một bảng lịch sử xem và một bộ luật gợi ý. Ước lượng 1 tuần.

### NR-10 — Không có push notification trên điện thoại

Thông báo cho người xem chỉ đi qua email. Không có push notification, không có tin nhắn SMS.

- Lý do: `thời gian` — email là đủ để đáp ứng FR-19 (nhắc sắp hết thời gian giữ ghế); push
  notification chỉ là một kênh bổ sung, không tạo ra bài toán mới.
- Nếu sau này cần: tích hợp một dịch vụ push, quản lý thiết bị và token. Ước lượng 1 tuần.

---

## Nhóm 3 — Lý do kỹ thu / quy mô

### NR-11 — Chạy một instance duy nhất

Hệ thống chạy trên **một** instance ứng dụng trong suốt dự án. Không chạy nhiều instance
cùng lúc, không cần chia tải ngang, không cần rolling deploy không gián đoạn.

- Lý do: `kỹ thu / quy mô` — chạy nhiều instance là điều kiện để một số bài toán tranh chấp
  thật sự xảy ra, nhưng các bài toán đó đều **tự chọn đúng instance** để chạy khi cần, và
  phần lớn chỉ khác nhau ở chỗ dữ liệu dùng chung nằm ở đâu. Học cơ chế đúng trên một
  instance rồi mới rộng ra là đúng thứ tự.
- Nếu sau này cần: một số bài toán trong bảng 12 sẽ cần kiểm chứng lại ở chế độ nhiều
  instance (đặc biệt là bài toán #9 — nhiều lượt xem cùng một suất — và bài toán #12 — nhiều
  instance dùng chung một hạn mức).

### NR-12 — Không có realtime bằng websocket

Sơ đồ ghế **không** tự cập nhật trong thời gian thực. Người xem phải tự làm mới trang để
thấy ghế vừa có người.

- Lý do: `kỹ thu / quy mô` — thêm một kênh giao tiếp hai chiều kéo theo vòng đời kết nối,
  quản lý trạng thái kết nối theo instance, và một lớp đồng bộ mà không bài toán tranh chấp
  nào cần đến.
- Nếu sau này cần: một kênh thông báo thay đổi sơ đồ ghế, và bài toán mới: thông báo có
  đến trễ thì ghế đã bán, người xem phải xử lý được.

### NR-13 — Không đa ngữ, không làm chuỗi rạp nhiều quốc gia

Toàn bộ nội dung hiển thị bằng một ngôn ngữ. Không có cơ chế đa ngữ, không có đa tiền tệ, và
dữ liệu mẫu chỉ mô phỏng một chuỗi rạp.

- Lý do: `kỹ thu / quy mô` — một rạp với vài phòng là đủ để sinh ra mọi tình huống tranh
  chấp mà dự án cần chứng minh: phòng nhỏ, phòng lớn, suất giáp nhau, ghế nhiều loại.
- Nếu sau này cần: bảng dịch, bảng tỷ giá, và luật giá theo từng thị trường.

### NR-14 — Không làm giao diện quản trị đầy đủ

Hệ thống có **API** cho toàn bộ nghiệp vụ của quản lý rạp và quản trị viên (tạo phòng, khai
báo ghế, lên lịch chiếu, mở bán, hủy suất chiếu, quản lý quyền), nhưng **không** xây một
giao diện quản trị hoàn chỉnh. Giao diện chỉ tập trung vào phía người xem.

- Lý do: `thời gian` — thời gian trong 12 tuần nên dồn vào phần concurrency, là phần có
  giá trị học tập và giá trị CV. Giao diện quản trị là công việc bảo trì lâu dài, không phải
  điểm khó.
- Nếu sau này cần: một màn hình cho mỗi nhóm thao tác quản trị. Ước lượng 2 tuần.

---

## Nhóm 4 — Cắt bớt phạm vi để tập trung

### NR-15 — Không làm check-in khi mất mạng

Check-in **online** làm đầy đủ: xác minh vé hợp lệ, phát hiện vé đã dùng, từ chối vé chưa
thanh toán, ghi nhận số người đã vào. Nhưng khi thiết bị tại cổng **mất kết nối**, hệ thống
**không** xác minh được vé tại chỗ.

- Lý do: `kỹ thu / quy mô` — xác minh offline đúng nghĩa là mã vé phải tự mang chứng thông tin
  và tự kiểm tra chữ ký không cần máy chủ, còn thao tác ghi nhận check-in phải được xếp hàng
  đợi để gửi lại khi có mạng — và phần đồng bộ đó là một dự án riêng, với bài toán riêng
  (cổng offline đồng bộ lại không được ghi nhận check-in lần hai). Bỏ nó giữ nguyên bài toán
  concurrency quan trọng nhất của phần check-in, đó là chống quét lại cùng một vé.
- Nếu sau này cần: mã vé ký số để máy quét tự kiểm tra, một hàng đợi phía thiết bị, và cơ
  chế hợp nhất khi kết nối lại. Ước lượng 1–2 tuần.

> **Hệ quả:** FR-26 bị loại khỏi phạm vi. Xem ghi chú bên dưới.

---

## Ghi chú về phạm vi đã cắt

### FR-26 bị loại khỏi scope

FR-26 ("xác minh vé khi mất mạng") không còn là requirement trong phạm vi dự án. Số hiệu
`FR-26` được **giữ lại** để không phá vỡ truy vết với các tài liệu đã viết — nó không được
dùng lại cho một requirement khác.

Trong [`01-requirements.md`](01-requirements.md), FR-26 được đánh dấu là đã loại khỏi scope
và không xuất hiện trong bảng đối chiếu success criteria.

### Những thứ **KHÔNG** nằm trong danh sách này

Danh sách trên loại bỏ những thứ. Để tránh hiểu nhầm, đây là những thứ **vẫn nằm trong phạm
vi** và được làm thật:

- **Nhiều loại ghế** (Standard / Couple / VIP) trong cùng một phòng, và giá khác nhau theo
  loại. Đây là nghiệp vụ cốt lõi, xem FR-20.
- **Trộn nhiều loại ghế trong cùng một đơn**, giá tính theo từng ghế.
- **Email thật**, **cổng thanh toán thật**, **mã QR thật** — không có phần nào trong dự án
  này được làm giả. Xem mục dưới.

### Không có phần nào được làm giả

Dự án này **không giả lập** bất kỳ thành phần nào. Cụ thể:

- Thanh toán: dùng cổng thanh toán thật, nhận thông báo kết quả thật, xử lý thông báo đến
  lặp thật (FR-38, bài toán concurrency #7).
- Vé: mã QR do hệ thống phát ra, ký số, và kiểm tra được tại chổ.
- Thông báo: gửi email thật (FR-19).

Lý do: một phần giả lập sẽ làm **test concurrency trở nên vô nghĩa** — test chỉ chứng minh
điều đúng khi đường đi thật đã được đi qua. Một thông báo kết quả giả lập sẽ không bao giờ
cho ra tình huống "đến lặp ba lần", mà đó đúng là tình huống cần chứng minh.
