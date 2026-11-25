# 26: README, ADR và báo cáo benchmark — sẵn sàng cho CV

Mode: co-design
Gate: D
Layer: code
Blocked by: 22, 23, 24, 25
Traces to: —

> **Đồng bộ với design (2026-11-25, Gate C đóng).** Khi ticket này mâu thuẫn với `docs/design/`,
> **design thắng**. Bảng bài toán concurrency có **14** dòng (`concurrency.md`); bộ ADR là 0001–0015.

## Vì sao có ticket này

Repo của bạn sẽ được đọc bởi người không biết bạn. 30 giây đầu tiên quyết định họ có đọc
tiếp không. Và CV của bạn sẽ trỏ tới repo này — nên nó phải kể được một câu chuyện.

Câu chuyện của một dự án concurrency **không phải** "tôi dùng Redis và Kafka". Nó là:
*bài toán nào, vì sao khó, tôi đo thế nào, kết quả ra sao, và tôi đã học được gì.*

Ticket cuối cùng này là nơi tất cả mọi thứ bạn làm trong 12 tuần được **nén lại thành bằng
chứng**. Và vì nó ở mode `co-design`, bạn viết — AI chỉ ghi lại.

## Bạn tạo ra

### `README.md`

Cấu trúc:

1. **Dòng đầu** — tên dự án + một câu: hệ thống bán vé xử lý concurrency thế nào.
2. **Vấn đề** — 2–3 đoạn, dẫn tới `docs/prd/00-overview.md`.
3. **Bảng "14 bài toán concurrency"** — cột: bài toán | cơ chế | test chứng minh | link.
   Đây là **bảng quan trọng nhất của README**. Người đọc scan bảng này để quyết định có đọc
   tiếp không.
4. **Kiến trúc** — sơ đồ, và giải thích bằng 3–4 câu (module nào giữ transaction nào).
5. **Số đo** — bảng so sánh trước/sau, kèm điều kiện chạy (máy, Postgres config, số VU).
   **Phải ghi rõ điều kiện**, nếu không con số vô nghĩa.
6. **Chạy thử** — `docker compose up -d && dotnet test`, cùng script k6.
7. **Stack** — bảng ngắn.
8. **ADR** — link danh sách, kèm 1–2 câu về quyết định đáng chú ý nhất.
9. **Bài học** — 3–5 điều bạn thực sự học được. Phần này chỉ có giá trị nếu thật.
10. **Giới hạn đã biết** — những gì chưa làm và tại sao. Trung thực ở đây là điểm cộng.

### `docs/adr/` — rà soát cuối

- Mọi ADR `provisional` phải được chốt (`accepted`) hoặc ghi `superseded by`.
- ADR nào có thêm phương án đã loại mà giờ bạn đã **thật sự** thử thì đưa vào, kèm số đo.

### `perf/baseline.csv` → `perf/report.md`

Bảng so sánh hai phương án giữ ghế (conditional `UPDATE` đa dòng vs `SELECT ... FOR UPDATE`),
kèm phương pháp đo.

### `docs/learning/log.md` — tổng kết

Mục tổng kết: sau 12 tuần, bạn học được gì, cái gì vẫn chưa hiểu, và kế hoạch tiếp theo
nếu có thêm 3 tháng.

### Cập nhật nghiệp vụ

- `docs/prd/02-non-requirements.md`: những gì bạn **không** làm — đối chiếu với cái bạn đã
  làm. Mục nào cuối cùng bị lọt vào scope thì ghi thật.
- `docs/plan/00-roadmap.md`: ma trận truy vết **đầy đủ**, mọi `FR-nn` có ticket.
- `docs/plan/gate-d-implementation.md`: plan chi tiết của giai đoạn cuối, nếu chưa tạo.

## AI làm gì

AI **không tự viết README**. AI:

1. Hỏi từng phần một, lấy nội dung từ bạn.
2. Ghi lại lời bạn, giữ giọng của bạn — README viết bởi AI nghe như AI.
3. **Phản biện nội dung**: nếu một claim trong README không có test hoặc số đo đi kèm, chỉ ra
   và đề xuất cách sửa hoặc bỏ.
4. Kiểm tra tính nhất quán: mọi `FR-nn` có trong ma trận truy vết không; mọi ADR
   `provisional` đã được chốt chưa; có ticket nào còn `[ ]` chưa tick không.
5. Với phần "Bài học" và "Giới hạn đã biết" — đây là phần bạn phải viết, AI chỉ góp ý.

## Tiêu chí hoàn thành

- [ ] README có đủ 10 phần nêu trên
- [ ] Bảng "14 bài toán concurrency" đầy đủ, mỗi dòng có link tới test
- [ ] Bảng số đo có **điều kiện đo** ghi rõ
- [ ] Mọi claim trong README có test hoặc số đo đi kèm — không có claim nào trần
- [ ] Không còn ADR nào ở trạng thái `provisional`
- [ ] Ma trận truy vết trong roadmap đầy đủ: mọi `FR-nn` có ít nhất một ticket
- [ ] Không còn tiêu chí hoàn thành nào chưa tick ở bất kỳ ticket nào
- [ ] Phần "Giới hạn đã biết" trung thực, không giấu giếm
- [ ] Đọc lại README trong 3 phút: người chưa biết bạn có hiểu hệ thống này làm gì không?
