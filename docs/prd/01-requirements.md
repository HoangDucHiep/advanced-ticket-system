# 01 — Requirements

Lớp 1 — PRD. File này nói **hệ thống phải làm gì**. Nó không nói *làm bằng cách nào*.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/02-requirements.md`.
Bối cảnh: [`00-overview.md`](00-overview.md).

## Cách đọc

Một requirement trong file này:

- Là **một hành vi quan sát được từ bên ngoài**, viết ở góc nhìn người dùng.
- **Không nhúng giải pháp.** "Hệ thống giữ ghế bằng một câu cập nhật có điều kiện" là quyết
  định thiết kế (Lớp 3). Requirement là "hệ thống không bao giờ cho hai người giữ cùng một
  ghế".
- Có **một** hành vi. Nếu cần nối bằng "và", đó là hai requirement.
- Có dòng **Kiểm chứng** — bằng gì chứng minh là đạt. Requirement không kiểm chứng được là
  một lời hứa rỗng.
- Truy vết tới ít nhất một **actor** và một **job** ở `00-overview.md`. Nếu requirement đáp
  ứng một **SC** (success criteria) thì ghi rõ SC đó; requirement nào không gắn với SC nào thì
  để trống — không bịa SC để lấp đầy bảng.

Tại sao có nhóm "hành vi bị cấm" đứng trước: các requirement đó là phần khó nhất của dự án
này, và chúng là phần **chứng minh được** bằng test. Một hệ thống "bán vé ổn" thì không
chứng minh được gì; một hệ thống "không bao giờ cho hai người giữ cùng một ghế" thì có.

---

## Nhóm A — Hành vi bị cấm

Những điều hệ thống **tuyệt đối không được phép** xảy ra. Mỗi cái đều có một test riêng.

### FR-01 — Một ghế không được thuộc về hai người

Khi một ghế đã được giữ hoặc đã bán cho một người, hệ thống không được cho bất kỳ người nào
khác giữ hay mua ghế đó, dù có bao nhiêu yêu cầu đến cùng lúc.

- Actor: Người xem
- Job: Đặt được vé khi mở bán phim hot
- Đáp ứng: SC-01
- Kiểm chứng: 100 yêu cầu đồng thời tranh cùng 1 ghế → **đúng** 1 yêu cầu thành công, 99
  yêu cầu còn lại bị từ chối.

### FR-02 — Đặt nhiều ghế phải giữ được đủ hoặc không giữ gì cả

Khi người xem đặt nhiều ghế trong một lần, hệ thống chỉ nhận đơn khi giữ được **đủ số ghế đã
chọn**. Nếu chỉ giữ được một phần, hệ thống phải trả lại toàn bộ — không giữ ghế nào cả.

- Actor: Người xem
- Job: Đặt vé
- Đáp ứng: SC-02
- Kiểm chứng: chọn 4 ghế trong đó 1 ghế đã có người → yêu cầu bị từ chối, và **cả 4 ghế**
  đều còn trống (ghế đã có người vẫn thuộc về người cũ).

### FR-03 — Không bao giờ kẹt vì tranh ghế chồng nhau

Hệ thống không được để hai người đặt vé cùng lúc trên cùng một suất chiếu bị treo vô hạn. Hai
yêu cầu tranh cùng ghế phải có kết quả, dù thứ tự ai thắng là ai.

- Actor: Người xem
- Job: Đặt vé
- Đáp ứng: SC-03
- Kiểm chứng: 2 yêu cầu cùng chọn 2 ghế theo thứ tự ngược nhau, chạy đồng thời, lặp lại nhiều
  lần → cả hai yêu cầu đều nhận được phản hồi trong thời gian giới hạn, **không** yêu cầu nào bị
  hệ thống huỷ ngang vì kẹt, và mỗi yêu cầu hoặc thành công, hoặc bị từ chối với lý do "ghế đã có
  người".

> **Ghi chú (2026-11-25):** kiểm chứng cũ chỉ đòi "cả hai đều có phản hồi". Nơi lưu dữ liệu tự phát
> hiện tình trạng kẹt và huỷ một bên — nên kiểm chứng cũ **vẫn đạt** kể cả khi hệ thống không hề
> phòng tránh kẹt. Kiểm chứng phải fail nếu bỏ cơ chế.

### FR-04 — Mọi lần giữ ghế đều có thời hạn và được tự giải phóng

Mỗi lần giữ ghế đều gắn với một thời điểm hết hạn xác định. Khi thời điểm đó đi qua mà người
xem chưa thanh toán, hệ thống tự giải phóng những ghế đã giữ và trả chúng về khả dụng cho
người khác. Không có lần giữ ghế nào tồn tại vô thời hạn.

- Actor: Người xem
- Job: Đặt vé
- Đáp ứng: SC-07
- Kiểm chứng: giữ 3 ghế rồi không thanh toán → sau thời điểm hết hạn, cả 3 ghế đều đặt được
  lại bởi người khác.

### FR-05 — Vé chỉ được cấp khi đơn đã thanh toán

Hệ thống không được cấp vé cho một đơn đặt vé chưa hoàn tất thanh toán. Vé chỉ tồn tại và chỉ
dùng được sau khi đơn đó đã được thanh toán thành công.

- Actor: Người xem
- Job: Đặt vé
- Đáp ứng: SC-04
- Kiểm chứng: trước khi thanh toán, không có vé nào được cấp cho đơn đó; số vé cấp ra bằng
  đúng số ghế trong đơn.

### FR-06 — Đơn hết hạn không được hồi sinh

Nếu kết quả thanh toán đến sau khi đơn đặt vé đã hết thời gian giữ ghế, hệ thống không được
đưa đơn đó trở lại trạng thái đang chờ. Đơn đã hết hạn vẫn là đã hết hạn. Đơn được coi là hết hạn
**ngay khi** thời điểm hết hạn đi qua — không phụ thuộc hệ thống đã kịp xử lý việc hết hạn hay
chưa.

- Actor: Người xem
- Job: Hiểu vì sao không đặt được
- Đáp ứng: SC-04, SC-07
- Kiểm chứng: thanh toán thành công cho một đơn đã hết hạn → đơn vẫn ở trạng thái hết hạn,
  không có vé nào được cấp, các ghế đã giữ được trả về.

### FR-07 — Mỗi vé chỉ check-in được một lần

Một vé không thể được ghi nhận check-in lần thứ hai. Khi người đó quét lại một vé đã được
check-in, hệ thống phải cho biết vé đó đã được sử dụng và **từ thời điểm nào**.

- Actor: Nhân viên cổng
- Job: Chặn vé không hợp lệ
- Đáp ứng: SC-05
- Kiểm chứng: quét cùng một vé 10 lần → đúng 1 lần ghi nhận check-in; 9 lần sau báo "đã
  check-in" kèm thời điểm gốc.

### FR-08 — Vé chưa được trả tiền không check-in được

Hệ thống không được ghi nhận check-in cho một vé thuộc đơn đặt vé chưa hoàn tất thanh toán.

- Actor: Nhân viên cổng
- Job: Chặn vé không hợp lệ
- Đáp ứng: SC-05
- Kiểm chứng: quét vé của một đơn chưa thanh toán → bị từ chối, không có bản ghi check-in.

### FR-09 — Không vượt giới hạn số ghế

Mỗi lần đặt vé chỉ được mua tối đa **6 ghế**. Và mỗi người chỉ được mua tối đa **10 ghế**
cho **một suất chiếu**, tính cộng dồn qua tất cả đơn của họ cho suất chiếu đó. Khi đếm số
ghế, mỗi ghế đôi (Couple) tính là **2 ghế**.

Hệ thống không được cho một người vượt quá giới hạn, kể cả khi người đó tách thành nhiều
đơn.

Số ghế đã có của một người cho một suất gồm cả ghế **đang giữ** (đơn chờ thanh toán còn hạn)
lẫn ghế **đã mua**.

- Actor: Người xem
- Job: Đặt vé
- Kiểm chứng: giới hạn 10 ghế mỗi suất → người xem đã mua 8 ghế ở đơn thứ nhất, đơn thứ hai
  chỉ được thêm tối đa 2 ghế, phần còn lại bị từ chối. Một đơn có 7 ghế (trong đó có ghế đôi)
  bị từ chối ngay vì vượt giới hạn 6 ghế mỗi đơn. Cùng một người gửi đồng thời nhiều yêu cầu
  cho cùng một suất → tổng số ghế của người đó vẫn không vượt 10.

### FR-10 — Không xem hay thao tác được đơn và vé của người khác

Người xem chỉ xem được và thao tác được trên các đơn đặt vé và vé của chính mình. Một người
không thể xem, tải mã QR, hay huỷ đơn của người khác.

- Actor: Người xem
- Job: Xem và dùng vé đã mua
- Kiểm chứng: yêu cầu đơn của người khác bị từ chối, không rò rỉ thông tin.

> **Ghi chú:** yêu cầu "người xem chỉ xem được vé của chính mình" trong nhóm phân quyền là
> **cùng một hành vi** với FR-10, chỉ khác góc nhìn. Đã gộp — không nhân bản.

---

## Nhóm B — Người xem

### FR-11 — Xem danh sách phim đang chiếu

Người xem xem được danh sách các phim đang có lịch chiếu.

- Actor: Người xem
- Job: Tìm phim và lịch chiếu
- Kiểm chứng: danh sách trả về đúng các phim đang chiếu, và không chứa phim chưa có lịch.

### FR-12 — Xem lịch chiếu của một phim

Khi xem một phim, người xem thấy các suất chiếu kèm thời gian, phòng, và giá vé.

- Actor: Người xem
- Job: Tìm phim và lịch chiếu
- Kiểm chứng: trang chi tiết hiển thị đủ các suất chiếu, đúng giờ, đúng phòng, đúng giá.

### FR-13 — Xem sơ đồ ghế của một suất chiếu

Khi chọn một suất chiếu, người xem thấy sơ đồ phòng: vị trí từng ghế, loại ghế, giá của
từng ghế, và **ghế nào còn trống, ghế nào đã có người**.

- Actor: Người xem
- Job: Chọn ghế
- Đáp ứng: SC-11
- Kiểm chứng: sơ đồ hiển thị đủ số ghế **không bị chặn** của phòng (ghế bị chặn không hiện —
  `BR-11`), đúng vị trí, đúng giá, trạng thái trống / đã có người khớp với thực tế, và số ghế còn
  trống hiển thị kèm sơ đồ khớp với số ghế trống trên sơ đồ.

### FR-14 — Giữ ghế có thời hạn và hiển thị thời điểm hết hạn

Khi người xem chọn ghế và xác nhận, hệ thống giữ lại những ghế đó cho người xem trong
**15 phút**, và trả về thời điểm hết hạn để người xem đếm ngược.

- Actor: Người xem
- Job: Đặt vé
- Kiểm chứng: sau khi giữ, các ghế đó hiển thị là đã có người; thời điểm hết hạn trả về có
  trong tương lai và là một mốc xác định.

### FR-15 — Số ghế trống được xác nhận lại tại lúc giữ

Tại thời điểm giữ ghế, hệ thống kiểm tra lại trạng thái thực tế của từng ghế chứ không dựa
vào con số đã hiển thị trước đó. Nếu một ghế đã có người, hệ thống từ chối và nói rõ ghế nào
đã có người.

- Actor: Người xem
- Job: Hiểu vì sao không đặt được
- Kiểm chứng: sơ đồ hiển thị ghế còn trống, nhưng trước lúc giữ thì ghế đó đã có người →
  yêu cầu bị từ chối với lý do "ghế đã có người", không phải một lỗi chung chung.

### FR-16 — Thanh toán và nhận vé

Sau khi thanh toán thành công, hệ thống cấp cho người xem đúng số vé bằng số ghế đã đặt,
mỗi vé có một mã để dùng tại cổng và ghi rõ **ghế nào** của vé đó.

Một đơn được phép trộn **nhiều loại ghế khác nhau**, và **giá tính theo từng ghế** — không
có một giá chung cho cả đơn.

- Actor: Người xem
- Job: Đặt vé
- Đáp ứng: SC-04
- Kiểm chứng: thanh toán thành công → đúng số vé được cấp, mỗi vé có mã riêng, mỗi vé ghi
  đúng ghế, và mã đó dùng để check-in được. Tổng tiền trả bằng tổng giá từng ghế.

### FR-17 — Xem vé đã mua và lấy mã

Người xem xem được danh sách vé của mình kèm suất chiếu và ghế, và lấy được mã của từng vé.

- Actor: Người xem
- Job: Xem và dùng vé đã mua
- Kiểm chứng: danh sách vé chứa đúng những vé đã mua; mã lấy được dùng check-in được.

### FR-18 — Huỷ đơn đang chờ thanh toán

Người xem có thể huỷ một đơn chưa thanh toán. Khi huỷ, những ghế đã giữ cho đơn đó được trả
về khả dụng cho người khác.

- Actor: Người xem
- Job: Đặt vé
- Đáp ứng: SC-07
- Kiểm chứng: huỷ đơn → các ghế đã giữ trở lại trạng thái trống và người khác đặt được.

### FR-19 — Nhận thông báo khi đơn sắp hết thời gian giữ ghế

Trước khi thời gian giữ ghế của một đơn kết thúc, người xem nhận được một thông báo nhắc
nhở kèm thời điểm hết hạn.

- Actor: Người xem
- Job: Đặt vé
- Kiểm chứng: một thông báo được gửi trước thời điểm hết hạn, chứa thời điểm hết hạn đúng;
  đơn đã thanh toán hoặc đã huỷ thì không nhận thông báo.

> **Ghi chú:** người xem còn thấy đồng hồ đếm ngược ngay trên màn hình thanh toán. Yêu cầu
> này là để xử lý tình huống người xem đã rời khỏi màn hình.

---

## Nhóm C — Quản lý rạp

### FR-20 — Quản lý phòng và sơ đồ ghế

Quản lý rạp tạo được phòng chiếu với sơ đồ ghế: khai báo từng ghế, vị trí, và loại ghế
(Standard / Couple / VIP) kèm giá.

- Actor: Quản lý rạp
- Job: Quản lý phòng và sơ đồ ghế
- Kiểm chứng: phòng tạo ra có đúng số ghế khai báo, mỗi ghế có vị trí và loại đúng; giá
  không âm.

### FR-21 — Lên lịch chiếu và đặt giá vé

Quản lý rạp đặt được một phim vào một phòng vào một giờ cụ thể, và đặt giá vé cho suất chiếu
đó.

- Actor: Quản lý rạp
- Job: Lên lịch chiếu
- Kiểm chứng: suất chiếu tạo ra gắn đúng phim, đúng phòng, đúng giờ; một phòng không thể
  có hai suất chiếu chồng nhau.

### FR-22 — Mở bán vé cho một suất chiếu

Quản lý rạp đưa một suất chiếu từ trạng thái chưa mở bán sang trạng thái đang bán. Từ thời
điểm đó, người xem nhìn thấy và đặt vé được.

- Actor: Quản lý rạp
- Job: Mở bán vé
- Kiểm chứng: trước khi mở bán, suất chiếu không xuất hiện trong danh sách và không đặt
  vé được; sau khi mở bán thì có.

### FR-23 — Xem tình trạng bán của một suất chiếu

Quản lý rạp xem được bốn con số của một suất chiếu: số ghế **còn trống**, số ghế **đang được
giữ**, số ghế **đã bán**, và số ghế **bị chặn** (vệ sinh, dành riêng — không bán online).

- Actor: Quản lý rạp
- Job: Theo dõi tình trạng bán
- Kiểm chứng: **bốn** con số cộng lại bằng tổng số ghế của phòng; số ghế còn trống khớp
  với trạng thái tồn kho.

> **Ghi chú (2026-11-20):** trước đây FR-23 chỉ có ba con số và nói "ba con số cộng lại bằng
> tổng số ghế" — mâu thuẫn với `BR-11`, vì ghế bị chặn vẫn tính vào tổng số ghế của phòng
> nhưng không thuộc ba nhóm "trống / giữ / bán". Đã sửa thành bốn con số.
>
> **Khác với con số người xem thấy:** người xem chỉ thấy **số ghế còn trống** (`FR-13`), trên một
> sơ đồ không có ghế bị chặn.
>
> **Ghi chú (2026-11-25):** bản trước nói người xem thấy "trống + giữ + bán". Con số đó gần như
> không đổi trong suốt đợt bán (ghế chỉ chuyển qua lại giữa ba nhóm), nên không cho người xem biết
> gì. Đã sửa.

### FR-24 — Hủy suất chiếu và hoàn tiền người đã mua

Khi quản lý rạp hủy một suất chiếu, những người đã mua vé được hoàn lại tiền, và vé của họ
không còn dùng được để vào. Những đơn **đang chờ thanh toán** của suất đó bị huỷ, ghế được trả;
nếu tiền của những đơn đó vẫn về sau khi huỷ, tiền đó được hoàn (`FR-39`).

- Actor: Quản lý rạp
- Job: Lên lịch chiếu
- Đáp ứng: SC-04
- Kiểm chứng: sau khi hủy, mỗi đơn đã trả tiền có đúng một lần hoàn tiền; không đơn nào của suất
  còn ở trạng thái chờ thanh toán; quét vé của suất đã hủy không ghi nhận check-in được; một thông
  báo thanh toán thành công đến sau khi hủy không cấp vé mà dẫn tới hoàn tiền.

> **Thứ tự thực hiện:** yêu cầu này đầy đủ, nhưng **thực hiện ở Gate D, sau T22**. Việc
> hoàn tiền phải an toàn khi hệ thống gián đoạn giữa chừng là cùng dạng bài toán lặp lại
> đã học ở FR-19 và FR-34 — nên làm sau khi nền tảng đã vững.

---

## Nhóm D — Nhân viên cổng

### FR-25 — Quét mã vé cho kết quả xác định

Khi nhân viên cổng quét một mã vé, hệ thống trả về một kết quả xác định: vé hợp lệ và chưa
dùng, vé đã dùng, vé chưa được trả tiền, vé không hợp lệ, hoặc vé của suất chiếu khác. Không
có trường hợp nào mà kết quả là "không rõ".

- Actor: Nhân viên cổng
- Job: Quét và xác minh vé
- Đáp ứng: SC-05
- Kiểm chứng: mỗi trong 5 tình huống trả về đúng kết quả tương ứng, phân biệt được với
  nhau.

### FR-26 — Xác minh vé khi mất mạng

> **ĐÃ LOẠI KHỎI PHẠM VI (2026-11-20).** Xác minh vé khi thiết bị cổng mất kết nối là một
> dự án riêng (mã vé phải tự kiểm tra chữ ký, thao tác ghi nhận check-in phải xếp hàng đợi
> để gửi lại khi có mạng, và phải hợp nhất mà không ghi nhận check-in lần hai). Số hiệu
> `FR-26` được **giữ lại** để không phá vỡ truy vết, nhưng không còn là requirement trong
> phạm vi dự án. Lý do loại bỏ: xem `NR-15` trong [`02-non-requirements.md`](02-non-requirements.md).
> FR này không xuất hiện trong bảng đối chiếu success criteria bên dưới.

Khi thiết bị tại cổng không có kết nối, nhân viên vẫn xác minh được vé có hợp lệ hay không
bằng cách kiểm tra tại chỗ.

- Actor: Nhân viên cổng
- Job: Làm được khi mất mạng
- Đáp ứng: SC-05
- Kiểm chứng: thiết bị ngắt kết nối → vẫn xác minh được vé hợp lệ, và báo rõ vé đã dùng
  chưa kiểm tra được hay chưa.

### FR-27 — Ghi nhận số người đã vào

Hệ thống ghi nhận mỗi lần check-in thành công, và quản lý rạp xem được tổng số lượt đã
check-in của suất chiếu.

- Actor: Nhân viên cổng
- Job: Ghi nhận số người đã vào
- Đáp ứng: SC-05
- Kiểm chứng: sau N lần check-in thành công, tổng số lượt ghi nhận bằng N.

---

## Nhóm E — Quản trị viên

### FR-28 — Quản lý toàn bộ rạp

Quản trị viên xem, sửa, ẩn và hủy được phim và lịch chiếu của bất kỳ rạp nào.

- Actor: Quản trị viên
- Job: Quản lý toàn bộ rạp
- Kiểm chứng: quản trị viên thực hiện được các thao tác đó trên lịch chiếu của rạp khác; quản
  lý rạp thì không.

### FR-29 — Quản lý người dùng và quyền của họ

Quản trị viên xem danh sách người dùng, và cấp hoặc thu quyền của người dùng.

- Actor: Quản trị viên
- Job: Quản lý người dùng và quyền
- Kiểm chứng: sau khi thu quyền, người dùng mất khả năng tương ứng và được ghi nhận lại.

### FR-30 — Giám sát sức khỏe hệ thống

Quản trị viên xem được các chỉ số vận hành của hệ thống trong lúc mở bán phim hot: tốc độ
xử lý, tỉ lệ yêu cầu bị từ chối, và tình trạng sức khỏe tổng thể.

- Actor: Quản trị viên
- Job: Giám sát vận hành
- Đáp ứng: SC-08, SC-09, SC-10, SC-11
- Kiểm chứng: các chỉ số hiển thị đúng số liệu thực tế của lưu lượng đang diễn ra.

### FR-31 — Truy vết lịch sử giao dịch

Quản trị viên tra được toàn bộ lịch sử của một đơn đặt vé: trạng thái hiện tại, các lần thay
đổi trạng thái, ghế nào đã giữ, và các thông báo đã nhận với đơn đó.

- Actor: Quản trị viên
- Job: Truy vết giao dịch
- Kiểm chứng: lịch sử hiển thị đầy đủ các lần thay đổi trạng thái theo đúng thứ tự thời
  gian.

---

## Nhóm F — Phân quyền

### FR-32 — Chỉ quản lý rạp sở hữu được sửa hoặc hủy lịch chiếu của họ

Quản lý rạp chỉ sửa hoặc hủy được lịch chiếu do chính họ tạo. Không sửa hoặc hủy được lịch
chiếu của rạp khác.

- Actor: Quản lý rạp
- Job: Lên lịch chiếu
- Kiểm chứng: quản lý rạp A bị từ chối khi thao tác lịch chiếu của quản lý rạp B.

### FR-33 — Chỉ nhân viên cổng được ghi nhận check-in

Việc ghi nhận check-in chỉ dành cho nhân viên cổng và quản trị viên. Người xem và quản lý
rạp không tự ghi nhận check-in được.

- Actor: Nhân viên cổng
- Job: Quét và xác minh vé
- Đáp ứng: SC-05
- Kiểm chứng: yêu cầu ghi nhận check-in từ tài khoản người xem bị từ chối.

### FR-34 — Suất chiếu chưa mở bán thì người xem không thấy và không đặt được

Suất chiếu chưa ở trạng thái đang bán không xuất hiện trong danh sách, không có trang chi tiết
với người xem, và không đặt vé được.

- Actor: Người xem
- Job: Tìm phim và lịch chiếu
- Kiểm chứng: yêu cầu đặt vé của suất chiếu chưa mở bán bị từ chối.

### FR-41 — Không đặt được vé cho suất đã bắt đầu chiếu

Từ giờ bắt đầu chiếu, người xem không giữ ghế được cho suất đó nữa, kể cả khi suất vẫn đang ở
trạng thái đang bán.

- Actor: Người xem
- Job: Đặt vé
- Kiểm chứng: yêu cầu giữ ghế gửi sau giờ bắt đầu chiếu bị từ chối với lý do "suất đã bắt đầu".

> **Ghi chú (2026-11-25):** thêm sau review — trước đó không có luật nào chặn bán vé cho suất đã
> chiếu xong.

### FR-35 — Nhân viên cổng chỉ quét được vé của suất chiếu được phân công

Nhân viên cổng chỉ ghi nhận check-in được vé thuộc suất chiếu mà họ được phân công trực.

- Actor: Nhân viên cổng
- Job: Quét và xác minh vé
- Đáp ứng: SC-05
- Kiểm chứng: quét vé của suất chiếu không được phân công bị từ chối.

### FR-40 — Phân công nhân viên cổng cho suất chiếu

Quản lý rạp phân công (và gỡ phân công) nhân viên cổng trực một suất chiếu do chính họ quản lý.
Chỉ người có vai trò nhân viên cổng mới được phân công.

- Actor: Quản lý rạp
- Job: Mở bán vé
- Đáp ứng: SC-05
- Kiểm chứng: sau khi phân công, nhân viên đó check-in được vé của suất; trước khi phân công hoặc
  sau khi gỡ thì không. Quản lý rạp không phân công được cho suất của rạp khác.

> **Ghi chú (2026-11-25):** thêm sau review — `FR-35` cần phân công tồn tại nhưng trước đó không có
> requirement nào để **tạo** phân công.

---

## Nhóm G — Thông báo thất bại

### FR-36 — Mọi lần từ chối đều kèm lý do và hành động tiếp theo

Khi hệ thống từ chối một yêu cầu, người dùng luôn biết **vì sao** bị từ chối và **nên làm
gì tiếp**. Không có trường hợp nào trả về một lỗi chung chung không giải thích được.

- Actor: Tất cả
- Job: Hiểu vì sao không đặt được
- Kiểm chứng: mỗi tình huống từ chối trong hệ thống đều trả về một lý do cụ thể và một
  gợi ý hành động.

---

## Nhóm H — Xử lý yêu cầu lặp lại

### FR-37 — Yêu cầu đặt vé lặp lại chỉ tạo ra một đơn

Khi cùng một yêu cầu đặt vé được gửi lại nhiều lần — do người dùng bấm hai lần, do mạng chập
chờn, hay do người dùng cố tình thử lại — hệ thống chỉ tạo ra **một** đơn, và trả về cùng
kết quả cho mọi lần gửi.

- Actor: Người xem
- Job: Đặt vé
- Đáp ứng: SC-06
- Kiểm chứng: 50 yêu cầu cùng định danh yêu cầu gửi đồng thời → đúng 1 đơn được tạo, 49
  lần còn lại trả về cùng kết quả với lần đầu. Nếu lần đầu bị từ chối (ghế đã có người), gửi lại
  cùng định danh vẫn nhận **cùng lời từ chối**, kể cả khi ghế đã trống trở lại.

### FR-38 — Thông báo kết quả thanh toán đến lặp chỉ cấp vé một lần

Khi cùng một thông báo kết quả thanh toán từ bên thứ ba được gửi đến nhiều lần, hệ thống chỉ
xử lý một lần. Vé không bị cấp hai lần, và trạng thái đơn không bị chuyển hai lần.

- Actor: Người xem (người được bảo vệ khỏi việc bị cấp vé/trừ tiền sai)
- Job: Đặt vé
- Đáp ứng: SC-04

> **Ghi chú (2026-11-25):** bản trước ghi actor là "Quản trị viên (vai trò duy nhất được phép xử lý
> thông báo này)". Không ai xử lý thông báo này bằng tay — hệ thống tự xử lý. Quản trị viên chỉ
> **xem lại** các thông báo đã nhận qua `FR-31`.
- Kiểm chứng: gửi cùng một thông báo 3 lần → đúng 1 lần cấp vé, đúng 1 lần thay đổi
  trạng thái, lịch sử đơn cho thấy 1 lần thay đổi trạng thái.

> **Ghi chú:** yêu cầu này nằm ở Lớp 1 vì nó là **hành vi quan sát được** ("vé chỉ được
> cấp một lần"), kể cả khi cơ chế xử lý thuộc Lớp 3.

---

## Nhóm I — Hoàn tiền

### FR-39 — Tiền đã trả mà không có vé thì phải được hoàn lại

Khi người xem **đã bị trừ tiền** nhưng không nhận được vé — vì đơn đã hết hạn trước khi
thông báo kết quả thanh toán đến, hoặc vì suất chiếu bị hủy — hệ thống **hoàn lại toàn bộ**
số tiền đã trả, và mỗi khoản tiền đã trừ được hoàn **đúng một lần**.

- Actor: Người xem
- Job: Hiểu vì sao không đặt được
- Đáp ứng: SC-04
- Kiểm chứng: thông báo thanh toán thành công đến sau khi đơn hết hạn → không có vé nào
  được cấp, ghế trả về, và người xem nhận lại đúng số tiền đã trả; xử lý lại cùng thông báo
  đó không hoàn thêm lần nữa.

---

## Bảng truy vết

Mỗi requirement → actor → job → success criteria.

| FR | Tên | Actor | Job | SC |
|---|---|---|---|---|
| FR-01 | Một ghế không thuộc về hai người | Người xem | Đặt được vé khi mở bán phim hot | SC-01 |
| FR-02 | Đặt n ghế: giữ đủ hoặc không giữ gì | Người xem | Đặt vé | SC-02 |
| FR-03 | Không kẹt khi tranh ghế chồng nhau | Người xem | Đặt vé | SC-03 |
| FR-04 | Giữ ghế có thời hạn, tự giải phóng | Người xem | Đặt vé | SC-07 |
| FR-05 | Vé chỉ cấp khi đã thanh toán | Người xem | Đặt vé | SC-04 |
| FR-06 | Đơn hết hạn không hồi sinh | Người xem | Hiểu vì sao không đặt được | SC-04, SC-07 |
| FR-07 | Vé chỉ check-in một lần | Nhân viên cổng | Chặn vé không hợp lệ | SC-05 |
| FR-08 | Vé chưa trả tiền không check-in được | Nhân viên cổng | Chặn vé không hợp lệ | SC-05 |
| FR-09 | Không vượt giới hạn ghế (6/đơn, 10/người/suất) | Người xem | Đặt vé | — |
| FR-10 | Không thao tác đơn/vé của người khác | Người xem | Xem và dùng vé đã mua | — |
| FR-11 | Xem danh sách phim đang chiếu | Người xem | Tìm phim và lịch chiếu | — |
| FR-12 | Xem lịch chiếu của một phim | Người xem | Tìm phim và lịch chiếu | — |
| FR-13 | Xem sơ đồ ghế của suất chiếu | Người xem | Chọn ghế | SC-11 |
| FR-14 | Giữ ghế có thời hạn + đếm ngược | Người xem | Đặt vé | — |
| FR-15 | Xác nhận lại ghế lúc giữ | Người xem | Hiểu vì sao không đặt được | — |
| FR-16 | Thanh toán và nhận vé | Người xem | Đặt vé | SC-04 |
| FR-17 | Xem vé đã mua và lấy mã | Người xem | Xem và dùng vé đã mua | — |
| FR-18 | Huỷ đơn chờ thanh toán, trả lại ghế | Người xem | Đặt vé | SC-07 |
| FR-19 | Thông báo sắp hết thời gian giữ ghế | Người xem | Đặt vé | — |
| FR-20 | Quản lý phòng và sơ đồ ghế | Quản lý rạp | Quản lý phòng và sơ đồ ghế | — |
| FR-21 | Lên lịch chiếu và đặt giá vé | Quản lý rạp | Lên lịch chiếu | — |
| FR-22 | Mở bán vé cho suất chiếu | Quản lý rạp | Mở bán vé | — |
| FR-23 | Xem tình trạng bán (4 con số) | Quản lý rạp | Theo dõi tình trạng bán | — |
| FR-24 | Hủy suất chiếu, huỷ đơn chờ, hoàn tiền đơn đã trả | Quản lý rạp | Lên lịch chiếu | SC-04 |
| FR-25 | Quét mã vé cho kết quả xác định | Nhân viên cổng | Quét và xác minh vé | SC-05 |
| FR-26 | ~~Xác minh vé khi mất mạng~~ | Nhân viên cổng | Làm được khi mất mạng | **đã loại khỏi scope (NR-15)** |
| FR-27 | Ghi nhận số người đã vào | Nhân viên cổng | Ghi nhận số người đã vào | SC-05 |
| FR-28 | Quản lý toàn bộ rạp | Quản trị viên | Quản lý toàn bộ rạp | — |
| FR-29 | Quản lý người dùng và quyền | Quản trị viên | Quản lý người dùng và quyền | — |
| FR-30 | Giám sát sức khỏe hệ thống | Quản trị viên | Giám sát vận hành | SC-08..SC-11 |
| FR-31 | Truy vết lịch sử giao dịch | Quản trị viên | Truy vết giao dịch | — |
| FR-32 | Chỉ chủ lịch chiếu được sửa/hủy | Quản lý rạp | Lên lịch chiếu | — |
| FR-33 | Chỉ nhân viên cổng ghi nhận check-in | Nhân viên cổng | Quét và xác minh vé | SC-05 |
| FR-34 | Suất chiếu chưa mở bán không thấy, không đặt | Người xem | Tìm phim và lịch chiếu | — |
| FR-35 | Nhân viên cổng chỉ quét vé được phân công | Nhân viên cổng | Quét và xác minh vé | SC-05 |
| FR-36 | Từ chối luôn kèm lý do và hành động tiếp | Tất cả | Hiểu vì sao không đặt được | — |
| FR-37 | Yêu cầu đặt vé lặp chỉ tạo một đơn | Người xem | Đặt vé | SC-06 |
| FR-38 | Thông báo thanh toán lặp chỉ cấp vé một lần | Người xem | Đặt vé | SC-04 |
| FR-39 | Tiền đã trả mà không có vé thì hoàn lại | Người xem | Hiểu vì sao không đặt được | SC-04 |
| FR-40 | Phân công nhân viên cổng cho suất chiếu | Quản lý rạp | Mở bán vé | SC-05 |
| FR-41 | Không đặt được vé cho suất đã bắt đầu | Người xem | Đặt vé | — |

## Đối chiếu success criteria

Mỗi SC ở `00-overview.md` có ít nhất một FR đáp ứng.

| SC | Nội dung | FR đáp ứng |
|---|---|---|
| SC-01 | Một ghế không bao giờ được bán cho hai người | FR-01 |
| SC-02 | Đặt N ghế: giữ đủ N hoặc không giữ ghế nào | FR-02 |
| SC-03 | Không bao giờ xảy ra kẹt khi tranh ghế chồng nhau | FR-03 |
| SC-04 | Mỗi lần thanh toán chỉ cấp một bộ vé | FR-05, FR-06, FR-16, FR-24, FR-38, FR-39 |
| SC-05 | Mỗi vé chỉ check-in một lần | FR-07, FR-08, FR-25, FR-27, FR-33, FR-35, FR-40 |
| SC-06 | Không tạo hai đơn cho cùng một yêu cầu | FR-37 |
| SC-07 | Không mất ghế nào khi hết thời gian giữ | FR-04, FR-06, FR-18 |
| SC-08 | Đọc: p99 đạt mục tiêu | FR-11, FR-12, FR-30 |
| SC-09 | Ghi dưới tải mở bán phim hot: p99 đạt mục tiêu | FR-14, FR-16, FR-21, FR-30 |
| SC-10 | Mở bán phim hot không sập, không ghế nào bán hai lần | FR-01, FR-30 |
| SC-11 | Phần lớn lưu lượng xem sơ đồ ghế không đọc tới nơi lưu trữ gốc | FR-13, FR-30 |
| SC-12 | Mọi tuyên bố đều có test hoặc số đo | Mọi FR đều có dòng Kiểm chứng |

> SC-08 đến SC-11 là **tiêu chí phi chức năng**: chúng không sinh ra thêm FR hành vi riêng,
> mà được kiểm chứng bằng cách đo trên các hành vi đã có (đọc lịch chiếu, ghi đơn, theo dõi
> vận hành).

## Ghi chú

- FR nào cần cơ chế kỹ thu thuộc **Lớp 3**. Ví dụ FR-02 sẽ được ánh xạ tới một quyết định
  thiết kế cụ thể ở `docs/design/concurrency.md` — nhưng ở Lớp 1, người đọc chỉ cần biết
  hệ thống **không bao giờ** giữ được một phần.
- FR-24 được đánh dấu thực hiện sau T22 vì hoàn tiền là một bài toán lặp lại phức tạp; nó
  vẫn là requirement đầy đủ, không phải non-requirement.
