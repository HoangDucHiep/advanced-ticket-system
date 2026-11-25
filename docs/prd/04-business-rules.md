# 04 — Business rules

Lớp 1 — PRD. File này nói các **luật nghiệp vụ** áp dụng xuyên suốt: giới hạn bao nhiêu vé
một người, giữ ghế bao lâu, ai được ưu tiên khi ghế hết.

Nguồn: ticket `.scratch/advanced-ticket-system/issues/04-user-journeys-business-rules.md`.
Bối cảnh: [`00-overview.md`](00-overview.md), [`01-requirements.md`](01-requirements.md),
[`02-non-requirements.md`](02-non-requirements.md),
[`03-user-journeys.md`](03-user-journeys.md).

## Cách đọc

Mỗi luật mang một mã `BR-nn`, và có hai dòng bắt buộc:

- **Ràng buộc** — cơ chế nghiệp vụ buộc phải giữ.
- **Vi phạm thì** — điều xảy ra nếu bị vi phạm, và người dùng thấy gì.

Một luật ở đây là **luật của nghiệp vụ**, không phải ràng buộc kỹ thu. "Ghế phải được khoá theo
thứ tự tăng dần để tránh deadlock" là Lớp 3 — nó là *cách hiện thực* luật `BR-04`, không phải
một luật riêng.

---

### BR-01 — Một ghế thuộc về đúng một người tại một thời điểm

Trong một suất chiếu, mỗi ghế ở mỗi thời điểm thuộc về **đúng một** trong bốn trạng thái:
`Trống` / `Đang giữ` / `Đã bán` / `Bị chặn`. Một ghế không thể vừa được giữ cho người này vừa
được bán cho người kia.

- Ràng buộc: khi giữ ghế, hệ thống kiểm tra trạng thái ghế **tại thời điểm giữ** — không
  tin vào dữ liệu đã hiển thị trước đó.
- Vi phạm thì: không được phép xảy ra. Nếu phát hiện ghế đã có người, hệ thống từ chối và
  nói rõ **ghế nào** đã có người (FR-01, FR-15).

### BR-02 — Giữ nhiều ghế phải là all-or-nothing

Khi một người xem giữ n ghế trong một lần, hệ thống chỉ giữ được **đủ n ghế**, hoặc
**không giữ ghế nào cả**. Không có trạng thái giữ được một phần.

- Ràng buộc: việc giữ n ghế là **một thao tác nguyên tử** trên tất cả n ghế. Nếu một ghế
  không còn trống, toàn bộ thao tác thất bại và những ghế đã giữ trong thao tác đó được trả
  về trạng thái `Trống`.
- Vi phạm thì: người xem bị từ chối, **không** bị trừ tiền cho bất kỳ ghế nào, và những ghế
  vẫn còn trống **không** bị giữ nhầm (FR-02).

### BR-03 — Mỗi lần giữ ghế có thời hạn

Mỗi lần giữ ghế gắn với một thời điểm hết hạn xác định, là **15 phút** kể từ lúc giữ.

- Ràng buộc: không có lần giữ ghế nào tồn tại vô thời hạn. Khi thời điểm hết hạn đi qua mà
  đơn chưa thanh toán, hệ thống tự giải phóng những ghế đã giữ.
- Vi phạm thì: ghế bị giữ vô thời hạn sẽ chặn người khác đặt — đây là lỗi nghiêm trọng
  nhất trong nhóm này. Hệ thống phải tự giải phóng mà không cần ai can thiệp (FR-04, FR-14).

### BR-04 — Hết thời gian giữ thì đơn hết hạn, không hồi sinh

Khi đơn hết thời gian giữ mà chưa thanh toán, đơn chuyển sang trạng thái **hết hạn** (coi
như đã huỷ), các ghế đã giữ được trả về `Trống`, và **không có vé nào được cấp**.

Nếu kết quả thanh toán đến **sau** thời điểm đó, đơn **không** trở lại trạng thái đang
chờ, và không có vé nào được cấp.

- Ràng buộc: một đơn đã hết hạn là **hết hạn vĩnh viễn**. Trạng thái đơn chỉ đi theo một
  chiều. Đơn được coi là hết hạn **ngay khi** thời điểm hết hạn đi qua — kết quả thanh toán đến
  sau mốc đó không cấp vé, kể cả khi hệ thống chưa kịp xử lý việc hết hạn.
- Vi phạm thì: nếu đơn hồi sinh, những ghế đã trả về cho người khác sẽ bị bán cho hai
  người. Hệ thống phải từ chối và giữ đơn ở trạng thái hết hạn (FR-06).

### BR-05 — Tiền đã trả thì phải được hoàn lại

Nếu người xem **đã bị trừ tiền** nhưng không nhận được vé — vì đơn đã hết hạn trước khi
thông báo kết quả thanh toán đến, hoặc vì suất chiếu bị hủy — hệ thống phải **hoàn lại
toàn bộ** số tiền đã trả.

- Ràng buộc: việc hoàn tiền **tự động**, không cần quản trị viên can thiệp. Mỗi khoản tiền
  đã trừ được hoàn **đúng một lần**. Việc hoàn có thể mất một khoảng thời gian, nhưng trạng thái
  "đang hoàn" phải xem được trong lịch sử đơn (`FR-31`), và hệ thống dừng giữa chừng thì phải làm
  tiếp được mà không hoàn hai lần.
- Vi phạm thì: người xem mất tiền mà không có vé — hậu quả nặng nhất với người dùng, và là
  lý do luật này tồn tại. Xem J-04.5 (FR-06, FR-24, FR-39).

### BR-06 — Giới hạn số ghế mỗi đơn

Một lần đặt vé chỉ được giữ **tối đa 6 ghế**.

- Ràng buộc: kiểm tra trước khi giữ, trên **tổng số ghế tính theo quy tắc đếm ghế** (xem
  `BR-07`).
- Vi phạm thì: yêu cầu bị từ chối, không giữ ghế nào (FR-09).

### BR-07 — Ghế đôi tính là 2 ghế khi đếm giới hạn

Khi đếm số ghế để áp dụng giới hạn, **mỗi ghế đôi (Couple) tính là 2 ghế**. Giới hạn này
áp dụng cho cả giới hạn mỗi đơn (`BR-06`) lẫn giới hạn mỗi người mỗi suất (`BR-08`).

Tuy nhiên, ghế đôi **vẫn sinh ra đúng 1 vé**, không phải 2 vé — một ghế là một chỗ ngồi,
nên một ghế là một vé (xem `BR-09`).

- Ràng buộc: số ghế tính theo quy tắc này là con số duy nhất dùng để so với mọi giới hạn
  về số lượng.
- Vi phạm thì: người xem có thể mua vượt giới hạn ghế bằng cách chọn ghế đôi. Ví dụ với
  giới hạn 6 ghế mỗi đơn: một ghế đôi + 5 ghế thường = 7 ghế tính theo quy tắc → bị từ
  chối.

### BR-08 — Giới hạn số ghế mỗi người mỗi suất

Một người xem được mua tối đa **10 ghế** cho **một suất chiếu**, tính **cộng dồn qua tất cả
đơn** của người đó cho suất chiếu đó.

- Ràng buộc: giới hạn này tính theo **người**, không theo **đơn**. Một người tách thành
  nhiều đơn vẫn không vượt được 10 ghế — tính cả ghế **đang giữ** lẫn ghế **đã mua**. Gửi nhiều
  yêu cầu cùng lúc cũng không vượt được. Người xem mua vé cho suất chiếu khác không bị tính
  vào.
- Vi phạm thì: yêu cầu bị từ chối, không giữ ghế nào. Lý do của luật này là chống việc một
  người mua số lượng lớn rồi chuyển nhượng ra ngoài, khiến người thật không vào được xem
  phim (FR-09).

> **Phân biệt hai giới hạn:** `BR-06` chặn *một lần đặt* quá lớn; `BR-08` chặn *một người*
> tích luỹ quá nhiều trong một suất. Vì `BR-08` rộng hơn `BR-06`, một người mua 8 ghế vẫn
> phải chia thành 2 đơn.

### BR-09 — Mỗi ghế sinh ra đúng một vé

Mỗi ghế trong đơn đã thanh toán sinh ra **đúng một vé**, và mỗi vé có một mã riêng, ghi rõ
**ghế nào** của vé đó.

- Ràng buộc: vé chỉ tồn tại **sau khi** đơn đã thanh toán thành công. Trước đó, đơn không
  có vé nào.
- Vi phạm thì: cấp vé cho đơn chưa thanh toán nghĩa là ai đó vào rạp không trả tiền.
  Quét vé của đơn chưa thanh toán phải bị từ chối (FR-05, FR-08, FR-16).

### BR-10 — Mỗi vé chỉ check-in được một lần

Một vé chỉ ghi nhận check-in **một lần duy nhất**, trong suất chiếu của nó, ở một cổng.

- Ràng buộc: ghi nhận check-in là **một thao tác nguyên tử**. Hai nhân viên cổng quét cùng
  một vé cùng lúc thì chỉ một lần được ghi nhận. Quét lại — kể cả ở cổng khác — báo "đã
  check-in" kèm **thời điểm** và **cổng** đã check-in lần đầu.
- Vi phạm thì: một vé vào được nhiều lần, nghĩa là số người thực vào rạp vượt quá số vé
  đã bán (FR-07, FR-25).

### BR-11 — Ghế bị chặn không bán online

Quản lý rạp có thể **chặn** một ghế không bán online (vệ sinh, dành riêng). Ghế bị chặn:

- **không** xuất hiện trên sơ đồ ghế của người xem;
- **không** tự quay lại bán — chỉ quản lý rạp **gỡ chặn** được, và chỉ khi đó ghế mới trở lại
  trống;
- chỉ chặn được ghế đang **trống** — ghế đang được giữ hoặc đã bán thì không chặn được;
- **vẫn tính** trong **tổng số ghế** của phòng. Vì vậy tình trạng bán của một suất chiếu có
  **bốn** con số — trống / đang giữ / đã bán / bị chặn — và bốn con số này cộng lại bằng
  tổng số ghế của phòng (`FR-23`).

- Ràng buộc: quyền chặn ghế thuộc về **quản lý rạp**, không phải quản trị viên và không
  phải người xem.
- Vi phạm thì: ghế bị chặn mà vẫn bán được là ghế không tồn tại trong thực tế — khách đến
  nơi mà không có chỗ ngồi (ADR-0003).

> **Ghi chú (2026-11-20):** trước đây BR-11 nói ghế bị chặn "không tính vào số ghế còn
> trống mà người xem thấy" — câu đó đúng, nhưng nó để mở khả năng hiểu nhầm rằng ghế bị
> chặn không tính vào tổng. Đã nói rõ: ghế bị chặn **vẫn tính vào tổng số ghế phòng**.

### BR-12 — Một đơn thuộc đúng một suất chiếu

Mọi ghế trong một đơn phải thuộc **cùng một suất chiếu**. Không có đơn ghé nhiều suất.

Một đơn được phép trộn **nhiều loại ghế khác nhau** (Standard / Couple / VIP), và **giá tính
theo từng ghế** — không phải một giá chung cho cả đơn.

- Ràng buộc: giá của đơn là **tổng giá từng ghế**, mà giá mỗi ghế là thuộc tính của **loại
  ghế trong suất chiếu đó**.
- Vi phạm thì: trộn suất chiếu làm mất ý nghĩa của "vé của suất chiếu này" ở bước check-in.
  Giá chung cho cả đơn là sai với nghiệp vụ xem phim (ADR-0003).

### BR-13 — Chỉ đặt vé được suất chiếu đang bán

Người xem chỉ nhìn thấy và chỉ đặt vé được suất chiếu đang ở trạng thái **đang bán**.

- Ràng buộc: suất chiếu chưa mở bán không xuất hiện trong danh sách, không có trang chi tiết
  với người xem, và yêu cầu đặt vé bị từ chối.
- Vi phạm thì: người xem đặt vé cho một suất chưa mở bán — nghĩa là bán vé trước khi rạp
  sẵn sàng nhận khách (FR-22, FR-34).

### BR-14 — Người xem chỉ thao tác được trên đơn và vé của chính mình

Người xem chỉ xem được, tải mã được, và huỷ được **đơn và vé của chính mình**.

- Ràng buộc: mọi thao tác trên đơn và vé đều kiểm tra chủ sở hữu.
- Vi phạm thì: yêu cầu bị từ chối và **không rò rỉ thông tin** của người khác (FR-10).

### BR-15 — Chỉ nhân viên cổng và quản trị viên ghi nhận check-in

Việc ghi nhận check-in chỉ dành cho **nhân viên cổng** và **quản trị viên**. Người xem và
quản lý rạp không tự ghi nhận check-in được.

Nhân viên cổng chỉ ghi nhận check-in được vé thuộc suất chiếu mà họ được **phân công
trực**.

- Ràng buộc: quyền được kiểm tra ở **hai** điều kiện: vai trò, và suất chiếu được phân
  công. Việc phân công do **quản lý rạp sở hữu suất chiếu** (hoặc quản trị viên) thực hiện, và
  chỉ phân công được người có vai trò nhân viên cổng (`FR-40`).
- Vi phạm thì: yêu cầu bị từ chối. Người xem tự check-in vé của mình là một lỗ hổng trực
  tiếp (FR-33, FR-35).

### BR-16 — Chỉ quản lý rạp sở hữu được sửa hoặc hủy lịch chiếu của họ

Quản lý rạp chỉ sửa hoặc hủy được lịch chiếu **do chính họ tạo**. Quản trị viên thì được
trên lịch chiếu của **bất kỳ rạp nào**.

- Ràng buộc: quyền được kiểm tra theo **chủ sở hữu lịch chiếu**.
- Vi phạm thì: một rạp sửa lịch chiếu của rạp khác là can thiệp vào vận hành của rạp
  khác (FR-28, FR-32).

### BR-17 — Mỗi lần thanh toán chỉ cấp vé một lần

Cùng một thông báo kết quả thanh toán đến nhiều lần thì chỉ được xử lý **một lần**. Vé
không bị cấp hai lần, và trạng thái đơn không bị chuyển hai lần.

- Ràng buộc: xử lý thông báo thanh toán phải **idempotent** — cùng một thông báo, chạy bao
  nhiêu lần thì kết quả như nhau.
- Vi phạm thì: cấp vé hai lần nghĩa là có vé không dùng được nhưng vẫn tồn tại, và số liệu
  trong lịch sử đơn sai (FR-38).

### BR-18 — Mỗi yêu cầu đặt vé chỉ tạo ra một đơn

Cùng một yêu cầu đặt vé được gửi lại nhiều lần — do bấm hai lần, do mạng chập chờn, hay do
thử lại — chỉ tạo ra **một** đơn, và trả về cùng kết quả cho mọi lần gửi.

- Ràng buộc: mỗi yêu cầu mang một **định danh** do người dùng cung cấp; cùng định danh thì
  cùng một đơn.
- Vi phạm thì: một lần bấm hai lần tạo ra hai đơn, mỗi đơn giữ một bộ ghế — nghĩa là người
  xem bị giữ ghế hai lần mà chỉ định ý mua một lần (FR-37).

### BR-19 — Mọi lần từ chối đều kèm lý do và hành động tiếp theo

Khi hệ thống từ chối một yêu cầu, người dùng luôn biết **vì sao** bị từ chối và **nên làm
gì tiếp**. Không có lỗi chung chung không giải thích được.

- Ràng buộc: mỗi kết quả từ chối mang **một lý do cụ thể** (không phải mã lỗi chung chung)
  và **một gợi ý hành động**.
- Vi phạm thì: người xem không biết phải đợi, thử lại, hay đổi ghế — và sẽ bấm lại, tạo
  thêm tải không cần thiết đúng lúc hệ thống đang chịu tải (FR-36).

### BR-20 — Mỗi phòng không có hai suất chiếu chồng nhau

Một phòng chiếu không thể có hai suất chiếu chồng nhau về thời gian.

- Ràng buộc: hai suất chiếu của cùng một phòng phải tách nhau bởi khoảng thời gian đủ để
  dọn dẹp giữa hai suất.
- Vi phạm thì: khách của suất sau không vào được phòng đang có suất trước (FR-21).

### BR-21 — Huỷ đơn đang chờ thanh toán thì trả ghế ngay

Người xem có thể huỷ một đơn **chưa thanh toán**. Ngay khi huỷ, những ghế đã giữ cho đơn đó
trở lại trạng thái `Trống` và người khác đặt được.

- Ràng buộc: huỷ chỉ có tác dụng với đơn **chưa thanh toán**. Đơn đã thanh toán không huỷ
  được bằng thao tác này — việc đó thuộc quyền của quản lý rạp (xem `BR-16` và `BR-05`).
- Vi phạm thì: ghế bị giữ mà không trả lại là ghế bị mất khỏi số ghế bán được, dù không ai
  mua (FR-18, SC-07).

### BR-22 — Tranh ghế chồng nhau không được kẹt

Khi hai người đặt vé cùng lúc trên cùng một suất chiếu, **không yêu cầu nào được treo vô
hạn**. Hai yêu cầu tranh cùng ghế phải có kết quả, dù thứ tự ai thắng là ai.

- Ràng buộc: mọi yêu cầu đặt vé phải có kết quả trong thời gian giới hạn. Hệ thống phải
  **phòng tránh** kẹt, không chỉ phát hiện rồi huỷ ngang một bên: không yêu cầu nào bị từ chối vì
  lý do "kẹt" — chỉ được từ chối vì lý do nghiệp vụ. Không có yêu cầu nào được phép chờ một tác vụ
  nền nào đó xong.
- Vi phạm thì: một yêu cầu treo giữ lùi toàn bộ kết nối đang phục vụ, và khi mở bán phim
  hot thì hệ thống sập (FR-03, SC-03).

### BR-23 — Sơ đồ ghế phản ánh trạng thái tại thời điểm người xem xem

Sơ đồ ghế hiển thị trạng thái `Trống` / `Đã có người` của từng ghế **tại thời điểm người xem
xem**, và giá của từng ghế theo loại ghế trong suất chiếu đó.

- Ràng buộc: sơ đồ là một **ảnh chụp tại một thời điểm**, không phải một lời hứa. Hệ thống
  không được dùng sơ đồ đã hiển thị làm căn cứ để giữ ghế — việc kiểm tra lại thực hiện tại
  lúc giữ (`BR-01`).
- Vi phạm thì: người xem bấm chọn một ghế đã có người và bị từ chối mà không hiểu vì sao
  (FR-13, FR-15, SC-11).

### BR-24 — Chỉ nhắc hết hạn cho đơn còn hiệu lực

Thông báo nhắc sắp hết thời gian giữ ghế chỉ được gửi cho đơn **còn hiệu lực** và **chưa
thanh toán**. Đơn đã thanh toán hoặc đã huỷ thì không có thông báo này.

- Ràng buộc: thông báo phải chứa **đúng** thời điểm hết hạn mà hệ thống đang dùng để giữ
  ghế, không phải một mốc báo chào.
- Vi phạm thì: người xem nhận thông báo cho một đơn đã xong — làm mất niềm tin vào mọi
  thông báo sau đó (FR-19).

### BR-25 — Mỗi lượt check-in thành công được đếm vào suất chiếu

Mỗi lần check-in thành công được ghi nhận, và tổng số lượt ghi nhận của một suất chiếu là
**tổng số vé đã check-in** của suất chiếu đó.

- Ràng buộc: con số này **không vượt quá** số vé đã bán của suất chiếu. Đây là hệ quả
  trực tiếp của `BR-10` — mỗi vé chỉ check-in một lần.
- Vi phạm thì: con số vượt số vé đã bán nghĩa là có người vào rạp không có vé, và báo cáo
  tình trạng bán đã sai (FR-27).

### BR-26 — Chỉ quản trị viên quản lý người dùng và quyền

Chỉ quản trị viên xem được danh sách người dùng, và cấp hoặc thu quyền của họ. Mọi lần
thay đổi quyền được **ghi lại**.

- Ràng buộc: thu quyền phải **có hiệu lực ngay** — sau khi thu quyền, người dùng mất khả
  năng tương ứng, và thay đổi được ghi nhận lại.
- Vi phạm thì: quyền bị thu nhưng vẫn dùng được, hoặc không ai biết quyền đã đổi khi cần
  truy vết (FR-29).

### BR-27 — Lịch sử đơn đặt vé là bản ghi chỉ-đọc

Lịch sử của một đơn đặt vé ghi lại **mọi** lần thay đổi trạng thái, theo đúng thứ tự thời
gian, cùng với ghế nào đã được giữ và các thông báo đã nhận. Lịch sử không bị sửa hay xoá.

- Ràng buộc: lịch sử là **append-only** — một lần thay đổi trạng thái đã ghi thì không đổi
  được, kể cả khi người xem đã trả tiền. Lịch sử đơn gồm cả các thông báo thanh toán và các
  bước hoàn tiền liên quan tới đơn.
- Vi phạm thì: lịch sử không khớp với hiện trạng thì không dùng được để giải quyết khiếu
  nại, và mất dấu vết của những lần xử lý sai (FR-31).

### BR-28 — Chỉ số vận hành phản ánh lưu lượng đang diễn ra

Các chỉ số vận hành (tốc độ xử lý, tỉ lệ yêu cầu bị từ chối, tình trạng sức khoẻ tổng
thể) phải phản ánh **lưu lượng đang diễn ra**, không phải số liệu tích luỹ từ trước.

- Ràng buộc: chỉ số phải **đo được** và **kiểm chứng được** bằng một phép đo cụ thể, không
  phải bằng một con số đẹp viết tay.
- Vi phạm thì: quản trị viên nhìn thấy hệ thống ổn trong khi nó đang sập — đúng lúc cần
  nhất thì thông tin sai (FR-30, SC-08..SC-11).

### BR-29 — Chỉ hiện phim và suất chiếu đang mở bán

Danh sách phim chỉ chứa phim **đang có suất chiếu mở bán**. Trang chi tiết phim chỉ hiện các
suất chiếu đang mở bán, kèm giờ chiếu, phòng, và giá vé.

- Ràng buộc: giá hiển thị là **giá theo loại ghế trong suất chiếu đó** — một suất có thể có
  nhiều loại ghế với nhiều mức giá.
- Vi phạm thì: người xem thấy một suất chiếu rồi bấm vào thì không đặt được — lãng phí
  thời gian của cả hai bên (FR-11, FR-12).

### BR-30 — Vé hiển thị kèm suất chiếu và ghế

Vé mà người xem xem được phải ghi rõ **suất chiếu** và **ghế** của nó, kèm **mã** dùng được
tại cổng.

- Ràng buộc: danh sách vé chỉ chứa vé **đã thanh toán**. Vé của đơn chưa thanh toán không
  xuất hiện. Vé của **suất đã hủy** vẫn xuất hiện nhưng ghi rõ "suất đã hủy — đang hoàn / đã hoàn
  tiền" và không dùng được để vào.
- Vi phạm thì: người xem thấy một vé không dùng được mà không biết vì sao, hoặc không biết vé đó
  của suất nào (FR-17, FR-24).

### BR-31 — Ngừng bán vé từ giờ bắt đầu chiếu

Từ giờ bắt đầu chiếu, không ai giữ ghế được cho suất đó nữa, kể cả khi suất vẫn ở trạng thái
**đang bán**.

- Ràng buộc: kiểm tra giờ bắt đầu tại lúc giữ ghế, cùng lúc với kiểm tra trạng thái đang bán
  (`BR-13`).
- Vi phạm thì: người xem trả tiền cho một suất đã chiếu xong (FR-41).

---

## Bảng luật → FR → journey

| BR | Nội dung | FR liên quan | Journey liên quan |
|---|---|---|---|
| BR-01 | Một ghế thuộc về đúng một người | FR-01, FR-15 | J-02, J-03, J-04 |
| BR-02 | Giữ n ghế là all-or-nothing | FR-02 | J-03 |
| BR-03 | Mỗi lần giữ ghế có thời hạn 15 phút | FR-04, FR-14 | J-02 |
| BR-04 | Hết hạn thì đơn hết hạn, không hồi sinh | FR-06 | J-04 |
| BR-05 | Tiền đã trả thì phải hoàn lại | FR-06, FR-24, FR-39 | J-04, J-06 |
| BR-06 | Tối đa 6 ghế mỗi đơn | FR-09 | J-04 |
| BR-07 | Ghế đôi tính là 2 ghế khi đếm giới hạn | FR-09 | J-04 |
| BR-08 | Tối đa 10 ghế mỗi người mỗi suất, cộng dồn qua các đơn | FR-09 | J-04 |
| BR-09 | Mỗi ghế sinh ra đúng một vé | FR-05, FR-08, FR-16 | J-02, J-05 |
| BR-10 | Mỗi vé chỉ check-in một lần | FR-07, FR-25 | J-05 |
| BR-11 | Ghế bị chặn không bán online | FR-20, FR-23 | J-06 |
| BR-12 | Một đơn thuộc đúng một suất chiếu; giá tính theo từng ghế | FR-16, FR-21 | J-02, J-06 |
| BR-13 | Chỉ đặt vé được suất chiếu đang bán | FR-22, FR-34 | J-03, J-06 |
| BR-14 | Chỉ thao tác được trên đơn/vé của chính mình | FR-10 | J-02 |
| BR-15 | Chỉ nhân viên cổng và quản trị viên check-in; theo suất được phân công | FR-33, FR-35, FR-40 | J-05, J-06 |
| BR-16 | Quản lý rạp chỉ sửa/hủy lịch chiếu của mình | FR-28, FR-32 | J-06, J-07 |
| BR-17 | Thanh toán đến lặp chỉ cấp vé một lần | FR-38 | J-02, J-07 |
| BR-18 | Yêu cầu lặp chỉ tạo một đơn | FR-37 | J-04 |
| BR-19 | Mọi lần từ chối đều kèm lý do và hành động tiếp | FR-36 | J-03, J-04 |
| BR-20 | Không có hai suất chiếu chồng nhau ở một phòng | FR-21 | J-06 |
| BR-21 | Huỷ đơn chờ thanh toán thì trả ghế ngay | FR-18 | J-04 |
| BR-22 | Tranh ghế chồng nhau không được kẹt | FR-03 | J-03 |
| BR-23 | Sơ đồ ghế phản ánh trạng thái tại thời điểm xem | FR-13, FR-15 | J-01, J-02 |
| BR-24 | Chỉ nhắc hết hạn cho đơn còn hiệu lực | FR-19 | J-02 |
| BR-25 | Mỗi lượt check-in được đếm vào suất chiếu | FR-27 | J-05 |
| BR-26 | Chỉ quản trị viên quản lý người dùng và quyền | FR-29 | J-07 |
| BR-27 | Lịch sử đơn đặt vé là bản ghi chỉ-đọc | FR-31 | J-07 |
| BR-28 | Chỉ số vận hành phản ánh lưu lượng đang diễn ra | FR-30 | J-07 |
| BR-29 | Chỉ hiện phim và suất chiếu đang mở bán | FR-11, FR-12 | J-01 |
| BR-30 | Vé hiển thị kèm suất chiếu và ghế; vé của suất đã hủy ghi rõ | FR-17, FR-24 | J-02, J-06 |
| BR-31 | Ngừng bán vé từ giờ bắt đầu chiếu | FR-41 | J-04 |
