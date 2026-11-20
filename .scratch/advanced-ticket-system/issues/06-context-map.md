# 06: Context map

Mode: co-design
Gate: B
Layer: domain
Blocked by: 05
Traces to: —

## Vì sao có ticket này

Một hệ thống luôn có nhiều "vùng ngôn ngữ" khác nhau. Mỗi vùng có thuật ngữ riêng và quy
tắc riêng. Khi hai vùng dùng chung một từ mà nghĩa khác nhau, chỗ giao nhau là nơi sinh bug.

Bounded context là ranh giới nơi một thuật ngữ mang **một** nghĩa duy nhất. Vẽ ra context map
là câu hỏi: "những phần nào của hệ thống nói chuyện với nhau, và nói cách nào?"

Đây là quyết định lớn nhất trong toàn bộ thiết kế, vì nó quyết định cấu trúc module, cách tách
service sau này, và transaction chạy được tới đâu.

## Bạn tạo ra

`docs/domain/context-map.md`:

- Danh sách các bounded context, mỗi cái ghi rõ **trách nhiệm** và **thuật ngữ riêng**.
- Sơ đồ quan hệ giữa chúng, với dạng quan hệ được ghi rõ:
  - `Customer/Supplier` — context này là nguồn chuẩn của dữ liệu mà context kia dùng.
  - `Conformist` — context này tuân theo mô hình của context kia hoàn toàn.
  - `Anti-Corruption Layer` — có một lớp dịch để bảo vệ mô hình riêng.
  - `Shared Kernel` — một phần mô hình dùng chung, phải giữ nhỏ.
  - `Published Language` — một ngôn ngữ/contract công khai mà hai bên hiểu chung.
- Với mỗi quan hệ: **ai gọi ai**, **theo hướng nào**, và **dữ liệu nào đi qua**.

Câu hỏi gợi ý:

1. Nhìn lại journey ở T04: có bao nhiêu vùng ngôn ngữ khác nhau bạn thấy? "Nhận vé" có cùng
   nghĩa với "kiểm tra vé" không?
2. "Tồn kho" — ai là nguồn sự thật? Hệ thống có cần biết trạng thái ghế của rạp khác không?
   Trong một rạp, ai là nguồn sự thật cho **sơ đồ ghế** và ai cho **trạng thái ghế theo suất**?
3. "Thanh toán" — Stripe có phải là một context không, hay chỉ là một adapter ra ngoài?
4. Ranh giới nào khó vẽ nhất? Chỗ đó thường là chỗ hai nghiệp vụ thực sự khác nhau bị gộp.

## AI làm gì

AI hỏi từng câu một. Với mỗi context, AI:

- Đề xuất một danh sách context ứng viên dựa trên journey, rồi hỏi bạn cái nào là thật.
- **Phản biện** khi bạn gộp hai thứ vào một context: "context này dùng từ X với nghĩa 1 ở
  chỗ này và nghĩa 2 ở chỗ kia — có phải nên tách không?"
- Với mỗi quan hệ, hỏi: "dữ liệu đi theo hướng nào, và nếu context kia sập thì context này
  còn chạy được không?"
- Với từ dùng chung giữa hai context, hỏi: "hai bên có dùng từ này cùng nghĩa không?"

AI ghi vào `docs/domain/context-map.md`.

## Tiêu chí hoàn thành

- [x] Có ít nhất 3 bounded context, mỗi cái có trách nhiệm ghi rõ bằng một câu — **6 context**:
  `Scheduling`, `Inventory`, `Booking`, `Payment`, `Ticketing`, `Identity & Access`
- [x] Mỗi context liệt kê thuật ngữ riêng của nó — mục "Thuật ngữ riêng của từng context" (22 mục)
- [x] Mỗi quan hệ giữa hai context có dạng quan hệ ghi tên, không phải mũi tên trần — R1..R8,
  mỗi quan hệ ghi rõ dạng, ai gọi ai, hướng dữ liệu, và cơ chế
- [x] Có ít nhất một quan hệ được ghi là `Anti-Corruption Layer` hoặc `Published Language`,
  kèm lý do — **ACL ở R1** (sinh ghế suất chiếu từ sơ đồ phòng), **ACL ở R3** (lớp dịch phía
  `Booking`), **ACL ở R5** (Stripe). Ngoài ra có `Customer/Supplier` ở R1, R2, R4, R6, R7, R8
- [x] Có ghi rõ context nào là nguồn sự thật cho dữ liệu nào — mục "Nguồn sự thật" (11 dòng)
- [x] Ranh giới context khớp với các điểm tranh chấp đã đánh dấu ở T04 — mục "Đối chiếu với
  điểm tranh chấp ở T04" (12 dòng, khớp 12 điểm trong `docs/prd/03-user-journeys.md`), kèm cột
  "Cắt ranh giới?" đánh dấu 3 nơi bất biến bị cắt

> **Ghi chú — các quyết định chính đã chốt trong phiên:**
>
> 1. `Seat hold` + thời điểm hết hạn thuộc **`Inventory`**, không phải `Booking` — vì bất biến
>    "ghế `Held` luôn có thời điểm hết hạn" không được cắt ngang ranh giới.
> 2. Ghế của phòng (`Room seat`) thuộc **`Scheduling`**; `Inventory` chỉ giữ ghế của suất chiếu.
> 3. `Ticket price` thuộc **`Scheduling`** (cùng chỗ với `Seat type` và `Showtime`).
> 4. `Payment` là Supplier của `Booking`; Stripe là **hệ thống bên ngoài**, không phải context
>    của ta — nên `Payment` → Stripe là `Anti-Corruption Layer`.
> 5. Vé **và** check-in cùng thuộc **`Ticketing`** — bất biến "mỗi vé chỉ check-in một lần" không
>    được cắt.
> 6. `Identity & Access` tách riêng, chỉ giữ "ai là ai, quyền gì"; **không** phải `Shared Kernel`.
>
> **Bài học ghi nhận:** bounded context ≠ transaction boundary. Ba nơi bất biến bị cắt ngang
> ranh giới được liệt kê tường minh trong mục "Nhóm ba ranh giới bị cắt", kèm hậu quả khi tách
> service — để T10/T12 không vô tình phá invariant đã chốt.

> **Ghi chú (2026-11-19):** nghiệp vụ đổi sang đặt vé xem phim. Điểm cần chú ý khi vẽ context
> map: **"ghế của phòng"** và **"ghế của suất chiếu"** là hai thuật ngữ khác nhau và thuộc
> hai vùng ngôn ngữ có thể khác nhau — đó là ứng viên mạnh nhất cho một `Anti-Corruption Layer`.
