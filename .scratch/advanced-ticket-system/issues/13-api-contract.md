# 13: API contract và idempotency contract

Mode: co-design
Gate: C
Layer: design
Blocked by: 09, 12
Traces to: FR-11, FR-12, FR-13, FR-16, FR-17, FR-18, FR-23, FR-36

## Vì sao có ticket này

Hợp đồng API là **hợp đồng với client** — kể cả khi client là chính bạn viết vài tháng sau.
Nó cũng là hợp đồng với người đọc repo của bạn, và là thứ interviewer nhìn đầu tiên khi hỏi
"API của bạn trông như thế nào".

Phần quan trọng nhất là **idempotency contract**. Vì client không biết request của mình đã
tới đâu, server phải trả lời được: "tôi đã thấy yêu cầu này chưa, và nếu rồi thì kết quả
là gì?" Đây là hợp đồng, không phải chi tiết implementation — nên nó thuộc về design, không
phải về code.

## Bạn tạo ra

### `docs/design/api-contract.md`

Mỗi endpoint một mục:

```
## <METHOD> <path>

**Auth**: <bearer / none / role>
**Idempotency**: <required / optional / n/a>

**Request**

| Trường | Kiểu | Ràng buộc |

**Response thành công**

`<status>` — <body>

**Lỗi**

| Status | Khi nào xảy ra |
|---|---|
```

Bắt buộc có:

- Bảng **error code toàn cục**: `400`, `401`, `403`, `404`, `409`, `422`, `429` — mỗi status
  dùng cho những tình huống nào, và **điều gì client nên làm** khi gặp nó.
- Endpoint nào **bắt buộc** có `Idempotency-Key`, và tại sao endpoint đó cần.
- Endpoint nào chỉ trả về dữ liệu công khai, endpoint nào cần phân quyền theo vai trò.
- Quy tắc phân trang, và quy tắc sort mặc định.

Câu hỏi gợi ý:

1. Với mỗi `FR-nn` ở T02: endpoint nào đáp ứng nó? Có FR nào không cần endpoint không?
2. Khi `POST /bookings` trả `409` vì một ghế đã có người, client làm gì? Có nên trả kèm
   **danh sách ghế đã có người** không, để client đánh dấu lại chỗ đó thay vì báo chung chung?
3. Khi trả `422` (idempotency key dùng sai), client có thể làm gì để tự phục hồi?
4. Endpoint nào đọc nhiều và nên có cache, endpoint nào ghi?
5. Có endpoint nào cần trả **partial success** không? (Gợi ý: với seat-based, câu trả lời
   có thể là **không** — đặt n ghế phải all-or-nothing, nên "giữ được 3/4" là lỗi chứ không
   phải kết quả một phần. Ghi rõ điều này ra, vì đó là điểm khác biệt so với mô hình đếm số.)
6. Endpoint nào trả về **sơ đồ ghế** — và trong response đó, ghế đang được giữ bởi người
   khác có hiển thị là "đã có người" hay "đang giữ chỗ"? (Hai cái này khác nhau về ý nghĩa với
   người xem, và khác nhau về thời gian hết hạn.)

### Phần idempotency contract

Trong `docs/design/api-contract.md` hoặc file riêng — một mục riêng mô tả:

- Header tên gì, giá trị lấy từ đâu (client tự sinh, UUID hay ULID?), giới hạn độ dài.
- **Request hash** tính trên cái — và vì sao phải có: hai request khác nhau dùng cùng key là
  lỗi của client, không phải retry.
- Vòng đời bản ghi idempotency: các trạng thái, và điều gì xảy ra khi request thứ hai đến
  trong lúc request thứ nhất **đang chạy**.
- Bao lâu thì hết hạn, và ai dọn.
- Cùng cơ chế đó có dùng lại được cho webhook không (với key là `event.id`)?
## AI làm gì

AI hỏi từng nhóm endpoint một. Với mỗi endpoint AI:

- Đối chiếu với `FR-nn` và user journey: endpoint này phục vụ bước nào?
- Hỏi "client xử lý lỗi này ra sao?" — một API mà không nói được điều đó là API chưa xong.
- **Phản biện** khi bạn định trả `200` cho một lỗi nghiệp vụ: `409` hay `422` nói rõ hơn, và
  client xử lý khác nhau.

Với idempotency contract, AI giảng phần khó trước:
- Vì sao "request trùng lặp" và "request khác nhau dùng cùng key" là hai việc khác nhau.
- Vì sao cần trạng thái `InProgress` thay vì chỉ có "đã xử lý / chưa xử lý".
- Vì sao race khi hai request cùng key đến đồng thời là bài toán thật, và cách xử lý nó.

## Tiêu chí hoàn thành

- [x] Mọi endpoint trong user journey đều có trong contract
- [x] Mọi `FR-nn` đều truy vết được tới ít nhất một endpoint
- [x] Bảng error code toàn cục đầy đủ, mỗi status có "client nên làm gì"
- [x] Endpoint nào cần `Idempotency-Key` đã được đánh dấu, kèm lý do
- [x] Idempotency contract mô tả đủ: hash, các trạng thái, xử lý race, hạn dùng, dọn dẹp
- [x] Đã nói rõ cơ chế idempotency có dùng lại được cho webhook không
- [x] Không endpoint nào trả `200` cho một lỗi nghiệp vụ mà không giải thích
- [x] Đã nói rõ endpoint nào **không** có partial success, và tại sao (đặt n ghế là
      all-or-nothing → `409` trả về kèm danh sách ghế đã có người, client tự chọn lại)

---

## Hoàn thành — 2026-11-25

Artifact: [`docs/design/api-contract.md`](../../../docs/design/api-contract.md). Quyết định: AI.

- Mọi endpoint theo module; truy vết FR → endpoint và journey → endpoint.
- Lỗi `application/problem+json` với `code` + `action` (`FR-36`); bảng `400`–`503` kèm "client nên làm
  gì"; phân biệt `409` (thế giới đổi) và `422` (yêu cầu sai luật).
- Idempotency bắt buộc ở giữ ghế; hash, chờ khi trùng đang chạy, hạn 24h, dọn dẹp; webhook **không**
  dùng chung cơ chế (dùng `UNIQUE stripe_event_id`).
- Không có partial success: giữ ghế all-or-nothing, `409` kèm `unavailableSeatIds`.
- Sơ đồ ghế phân biệt `Held` / `Sold`; định dạng QR; đóng Q04.
