# 03 — User journeys

Lớp 1 — PRD. File này nói **người dùng đi qua những bước nào**, và ở bước nào hệ thống phải
ra tay.

Bối cảnh: [`00-overview.md`](00-overview.md), [`01-requirements.md`](01-requirements.md),
[`02-non-requirements.md`](02-non-requirements.md).

## Cách đọc

Mỗi bước ghi rõ **hệ thống phải làm gì** ở bước đó.

Bước được đánh dấu bằng hai nhãn để chỉ ra **điểm tranh chấp** — nơi nhiều người cùng chạm vào
một thứ. Danh sách các bài toán tranh chấp và cơ chế xử lý nằm ở
[`../design/concurrency.md`](../design/concurrency.md):

| Nhãn | Nghĩa | Bài toán concurrency liên quan |
|---|---|---|
| `[GHẾ]` | Bước này **chạm vào tồn kho ghế** — trạng thái ghế có thể bị thay đổi, và nhiều người có thể tranh cùng một ghế | Gán trùng ghế, giữ n ghế all-or-nothing, kẹt khi tranh chồng nhau |
| `[TIỀN]` | Bước này **chạm vào tiền** — có thể trừ tiền, hoặc trả tiền lại | Thanh toán đến lặp, hoàn tiền, trừ tiền hai lần |

Bước không có nhãn nào là bước chỉ **đọc**.

---

## J-01 — Tìm phim và lịch chiếu

> Người xem muốn biết phim nào đang chiếu, suất nào, và còn chỗ không.

1. Người xem xem danh sách phim đang chiếu — hệ thống trả về các phim **đang có suất chiếu
   mở bán**, không trả về phim chưa có lịch.
   - FR-11, FR-34
2. Người xem mở một phim — hệ thống trả về các suất chiếu kèm giờ, phòng, và giá vé.
   - FR-12
3. Người xem chọn một suất chiếu — hệ thống trả về **sơ đồ ghế**: vị trí từng ghế, loại ghế
   (Standard / Couple / VIP), giá của từng ghế, và trạng thái **trống / đã có người**.
   - FR-13
4. Người xem thấy tổng số ghế còn trống của suất chiếu — hệ thống hiển thị con số này
   cùng sơ đồ.

> **Ghi chú đánh dấu:** cả 4 bước đều **chỉ đọc**. Bước 3 là bước được đọc nhiều nhất khi mở
> bán phim hot — mọi người xem sơ đồ ghế cùng lúc, trong khi bước 4 của J-02 đang **ghi** vào
> đúng những cái ghế mà bước 3 này vừa hiển thị. Đó là lý do hiển thị phải nói rõ "ghế nào
> còn trống **tại thời điểm bạn vừa xem**", không phải "ghế nào sẽ còn trống".

---

## J-02 — Đặt vé trong điều kiện bình thường

> Người xem muốn mua vé cho một suất chiếu, ghế do mình chọn.

1. Người xem chọn các ghế muốn ngồi — hệ thống hiển thị tổng giá tạm tính. Người xem có
   thể chọn **ghế thuộc nhiều loại khác nhau** trong cùng một đơn; giá tính theo từng ghế.
   - FR-20, FR-13
2. Người xem xác nhận — hệ thống **giữ lại** những ghế đó cho người xem trong **15 phút**,
   và trả về thời điểm hết hạn để người xem đếm ngược.
   - FR-14
   - `[GHẾ]`
3. Hệ thống **kiểm tra lại trạng thái thực tế** của từng ghế ngay lúc giữ, không tin vào
   con số đã hiển thị ở J-01. Nếu một ghế đã có người, hệ thống từ chối và nói rõ **ghế nào**
   đã có người.
   - FR-15, FR-01
   - `[GHẾ]`
4. Người xem thanh toán — hệ thống gửi yêu cầu thanh toán tới cổng thanh toán, gắn yêu
   cầu này với đơn của họ.
   - `[TIỀN]`
5. Người xem nhận thông báo kết quả thanh toán — hệ thống **chỉ xử lý lần thông báo đầu
   tiên**; các lần đến sau bị bỏ qua.
   - FR-38
   - `[TIỀN]`
6. Hệ thống cấp vé: **mỗi ghế trong đơn sinh ra một vé**, mỗi vé có một mã riêng dùng tại
   cổng và ghi rõ **ghế nào** của vé đó.
   - FR-05, FR-16
   - `[GHẾ]`, `[TIỀN]`
7. Người xem xem danh sách vé của mình — hệ thống trả về các vé đã mua kèm suất chiếu,
   ghế, và mã vé; **chỉ** vé của chính người đó.
   - FR-17, FR-10
8. Trước khi hết hạn, người xem nhận được thông báo nhắc nhở kèm thời điểm hết hạn. Nếu
   đơn đã thanh toán hoặc đã huỷ, không có thông báo này.
   - FR-19

> **Điểm tranh chấp:** bước 2 là nơi **hai người chọn cùng một ghế** va nhau. Bước 3 là
> nơi hệ thống phát hiện điều đó. Bước 5 và 6 là nơi **cùng một thông báo** có thể đến
> nhiều lần.

---

## J-03 — Mở bán phim hot

> Nhiều người cùng đặt vé cho một suất chiếu được quan tâm, tại cùng một thời điểm. Ghế
> không đủ cho tất cả.

1. Trước thời điểm mở bán, hệ thống đã có sẵn lịch chiếu và sơ đồ ghế của phòng.
   - FR-21, FR-22
2. Quản lý rạp đưa suất chiếu vào trạng thái **đang bán** — từ thời điểm đó, người xem
   nhìn thấy suất chiếu và bắt đầu đặt vé.
   - FR-22, FR-34
3. Hàng trăm người cùng xem sơ đồ ghế gần như cùng lúc — hệ thống phục vụ được lượng xem
   này mà không phải đọc tới nơi lưu trữ dữ liệu gốc ở mọi lượt xem.
   - FR-13
   - FR đáp ứng: SC-11
4. Hàng trăm người cùng bấm "xác nhận", cùng tranh cùng một vài ghế giữa chỗ —
   - **đúng** một người giữ được mỗi ghế, những người còn lại bị từ chối rõ ràng.
   - FR-01, FR-02
   - `[GHẾ]`
5. Hai người chọn hai ghế **ngược thứ tự** (A chọn ghế 3 rồi ghế 7; B chọn ghế 7 rồi ghế 3) —
   hệ thống không để yêu cầu nào bị treo hay bị huỷ ngang vì kẹt; mỗi người hoặc thắng, hoặc bị từ
   chối với lý do "ghế đã có người".
   - FR-03
   - `[GHẾ]`
6. Người xem đặt được nhiều ghế, trong đó có một ghế vừa bị người khác giữ — hệ thống
   **không giữ ghế nào cả**, kể cả những ghế vẫn còn trống.
   - FR-02
   - `[GHẾ]`
7. Người xem nhận được câu trả lời **có lý do** cho mỗi lần bị từ chối, kèm gợi ý làm gì
   tiếp theo.
   - FR-36

> **Ghi chú:** bước 4 và 5 là **cùng một cơ chế** phải đúng trong hai tình huống khác nhau.
> Bước 4 kiểm tra "một ghế không thuộc về hai người"; bước 5 kiểm tra "hai người tranh ngược
> thứ tự không kẹt nhau". Không có bước nào trong journey này được phép chờ một tác vụ
> nền nào đó xong — mọi yêu cầu phải có kết quả ngay.

---

## J-04 — Bị từ chối

> Người xem không đặt được vé. Hệ thống phải nói rõ chuyện gì xảy ra và người xem phải làm
> gì tiếp.

1. **Ghế đã có người.** Hệ thống từ chối yêu cầu giữ ghế và nói rõ **ghế nào** đã có
   người; những ghế khác trong lựa chọn vẫn ở trạng thái trống, không bị giữ nhầm.
   - FR-01, FR-02, FR-15
   - `[GHẾ]`
2. **Hết thời gian giữ ghế.** Hệ thống tự giải phóng những ghế đã giữ và trả chúng về
   khả dụng cho người khác. Đơn của người xem chuyển sang trạng thái **hết hạn** (khác với
   **đã huỷ** — xem `GLOSSARY.md`), và **không có vé nào được cấp**.
   - FR-04
   - `[GHẾ]`
3. **Vượt giới hạn số ghế.** Một đơn vượt quá **6 ghế**, hoặc tổng số ghế người đó đã mua
   cho suất chiếu này vượt quá **10 ghế** (cộng dồn qua mọi đơn của họ) — hệ thống từ
   chối.
   - FR-09
   - `[GHẾ]`
4. **Thẻ bị từ chối.** Cổng thanh toán báo thất bại — hệ thống báo rõ, **không** cấp vé,
   và **giữ nguyên** chỗ giữ ghế để người xem thử lại được trong thời gian còn lại.
   - FR-05, FR-36
   - `[TIỀN]`
5. **Thanh toán thành công nhưng đến muộn** — thông báo kết quả thanh toán tới sau khi đơn
   đã hết hạn. Hệ thống **không** hồi sinh đơn, **không** cấp vé, trả ghế về cho người
   khác, và **hoàn lại tiền** cho người xem.
   - FR-06, FR-24, FR-39
   - `[GHẾ]`, `[TIỀN]`
6. **Yêu cầu bị gửi lại nhiều lần** (bấm hai lần, mạng chập chờn) — hệ thống tạo **một**
   đơn duy nhất và trả về cùng kết quả cho mọi lần gửi, kể cả khi lần đầu bị từ chối.
   - FR-37
7. **Suất đã bắt đầu chiếu.** Người xem giữ ghế sau giờ bắt đầu — hệ thống từ chối với lý do
   "suất đã bắt đầu".
   - FR-41, FR-36
8. **Người xem tự huỷ đơn** đang chờ thanh toán — hệ thống trả ghế về ngay cho người khác đặt;
   đơn chưa trả tiền nên không có hoàn tiền.
   - FR-18
   - `[GHẾ]`

> **Ghi chú:** tình huống 5 là lỗ hổng nghiệp vụ dễ bỏ sót nhất: tiền đã bị lấy nhưng vé
> không cấp được. Xem luật `BR-05` trong [`04-business-rules.md`](04-business-rules.md).

---

## J-05 — Check-in tại cổng

> Nhân viên cổng quét mã vé để khách vào rạp.

1. Nhân viên cổng quét mã vé — hệ thống trả về **một kết quả xác định**, thuộc một trong
   năm trường hợp:
   - vé hợp lệ và chưa dùng → ghi nhận check-in, mời khách vào;
   - vé **đã dùng** → báo đã check-in, kèm **thời điểm** check-in lần đầu;
   - vé **không hợp lệ** (sai chữ ký, hoặc không ứng với vé nào đã cấp — kể cả mã dựng cho ghế
     của một đơn chưa thanh toán) → từ chối vào;
   - vé **thuộc suất chiếu khác** → từ chối vào;
   - vé của **suất chiếu đã hủy** → từ chối vào (người xem đã/đang được hoàn tiền).
   - FR-25, FR-08, FR-24
   - `[GHẾ]`
2. Nhân viên cổng quét lại **cùng một vé** (quét nhầm, hoặc khách đứng lại) — hệ thống
   báo "đã check-in" kèm thời điểm và cổng đã check-in lần đầu, và **không** ghi nhận thêm
   một lần.
   - FR-07
   - `[GHẾ]`
3. Hai nhân viên cổng quét **cùng một vé cùng lúc** — chỉ **một** lần check-in được ghi
   nhận; lần còn lại báo "đã check-in".
   - FR-07
   - `[GHẾ]`
4. Hệ thống ghi nhận mỗi lần check-in thành công — quản lý rạp xem được tổng số lượt đã
   check-in của suất chiếu.
   - FR-27
   - `[GHẾ]`
5. Nhân viên cổng quét vé của suất chiếu **không được phân công** trực — hệ thống từ
   chối.
   - FR-35

> **Ghi chú:** bước 3 là bài toán tranh chấp duy nhất của phần check-in. Bước 1 trả về
> "kết quả xác định" — không có trường hợp nào là "không rõ", vì nhân viên cổng đứng giữa
> hành khách không thể xử lý một câu trả lời mơ hồ.

---

## J-06 — Quản lý rạp dựng phòng và mở bán

> Quản lý rạp chuẩn bị một suất chiếu để bán.

1. Quản lý rạp tạo phòng chiếu với sơ đồ ghế — khai báo từng ghế: vị trí (hàng, số) và
   loại ghế (Standard / Couple / VIP). Phòng thuộc về quản lý rạp đó.
   - FR-20
2. Quản lý rạp đặt giá vé cho từng loại ghế của suất chiếu — giá là thuộc tính của **loại
   ghế trong suất chiếu đó**, nên cùng một loại ghế ở hai suất khác nhau có thể có giá
   khác nhau.
   - FR-21
3. Quản lý rạp chọn phim từ danh mục (do quản trị viên quản lý) và đặt vào **phòng của mình**
   vào một giờ cụ thể — một phòng không thể có hai suất chiếu chồng nhau (kể cả khoảng đệm dọn
   dẹp).
   - FR-21
4. Quản lý rạp **chặn** những ghế không bán online (vệ sinh, dành riêng) — làm được ngay sau
   khi tạo suất, trước khi mở bán. Ghế bị chặn **không xuất hiện** trên sơ đồ của người xem.
   Chỉ quản lý rạp gỡ chặn được; ghế bị chặn không tự quay lại bán. Ghế đang được giữ hoặc đã
   bán thì không chặn được.
   - FR-20
   - `[GHẾ]`
5. Quản lý rạp đưa suất chiếu vào trạng thái **đang bán**.
   - FR-22
6. Quản lý rạp **phân công nhân viên cổng** trực suất chiếu.
   - FR-40
7. Quản lý rạp xem tình trạng bán: số ghế còn trống, đang được giữ, đã bán, và bị chặn.
   **Bốn** con số cộng lại bằng tổng số ghế của phòng.
   - FR-23
8. Quản lý rạp **hủy** một suất chiếu — những người đã mua vé được **hoàn lại tiền**, vé của
   họ không còn dùng được để vào, và những đơn đang chờ thanh toán của suất bị huỷ.
   - FR-24, FR-39
   - `[TIỀN]`, `[GHẾ]`
9. Quản lý rạp chỉ sửa hoặc hủy được lịch chiếu **do chính họ tạo**.
   - FR-32

---

## J-07 — Quản trị viên giám sát và truy vết

> Quản trị viên cần biết hệ thống đang ổn không, và khi có khiếu nại thì lần theo được
> chuyện gì đã xảy ra.

1. Quản trị viên xem các chỉ số vận hành trong lúc mở bán phim hot: tốc độ xử lý, tỉ lệ
   yêu cầu bị từ chối, và tình trạng sức khoẻ tổng thể.
   - FR-30
2. Quản trị viên tra lịch sử của một đơn đặt vé: trạng thái hiện tại, các lần thay đổi
   trạng thái theo đúng thứ tự thời gian, ghế nào đã được giữ, và các thông báo đã nhận.
   - FR-31
3. Quản trị viên quản lý danh mục phim, và xem, sửa, hủy lịch chiếu của **bất kỳ rạp nào**;
   quản lý rạp thì không.
   - FR-28
4. Quản trị viên xem danh sách người dùng, và cấp hoặc thu quyền của họ.
   - FR-29
5. Khi có khiếu nại "bị trừ tiền hai lần", quản trị viên xem được mọi thông báo thanh toán đã
   nhận cho đơn đó — kể cả thông báo đến lặp — và thấy chúng chỉ được xử lý **một lần**.
   - FR-31, FR-38

---

FR nào xuất hiện ở journey nào: xem [`../plan/traceability.md`](../plan/traceability.md). Các điểm tranh chấp tổng hợp: xem [`../design/concurrency.md`](../design/concurrency.md).
