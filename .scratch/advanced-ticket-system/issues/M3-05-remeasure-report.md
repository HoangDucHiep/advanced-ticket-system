# M3-05: Đo lại, so biến thể khoá ghế, `perf/report.md`

Mode: pair-tdd
Status: ready
Blocked by: M3-03, M3-04
Traces to: FR-30, FR-13
Concurrency: —
Branch: m3-05-remeasure-report

## Mục tiêu
Biến "tôi đã tối ưu" thành bảng trước / sau có điều kiện đo (`SC-12`): đo lại các kịch bản của M3-02 sau cache và
rate limit, so các biến thể khoá ghế, rồi **chốt `PermitLimit` / `QueueLimit` bằng số**. Đóng Q32, Q33 và dữ liệu
cho Q34.

## Phạm vi
**Trong:**
- Chạy lại **đúng** các kịch bản k6 của M3-02 (`browse`, `hot-release`, `checkin`) với cache + rate limit, cùng seed,
  cùng máy, cùng cấu hình Postgres; mỗi cấu hình chạy **nhiều lần** (số lần chốt trong PR, tối thiểu 3) và ghi
  độ phân tán; mỗi lần một dòng trong `perf/baseline.csv`.
- **So biến thể khoá ghế** của ADR-0002 (mục "Revalidate at Gate C"): A (một `UPDATE … ANY`, `READ COMMITTED`),
  B (nhiều `UPDATE` từng ghế theo thứ tự id), C (`SELECT … FOR UPDATE` theo thứ tự trong `REPEATABLE READ`), và
  **biến thể đã chọn** ở Gate C (khoá tường minh `ORDER BY id FOR UPDATE` rồi `UPDATE` có điều kiện, `READ
  COMMITTED`). Mỗi biến thể là một cờ cấu hình chỉ trong build đo (không vào production path); đo p50/p95/p99,
  tỉ lệ lỗi, `monitor-lock-contentions`, và **số `40P01` (deadlock)** — biến thể sinh deadlock bị loại.
- Chọn `PermitLimit` / `QueueLimit` từ số đo (quét một vài cặp giá trị quanh 30/100).
- `perf/report.md`: bảng trước / sau, **điều kiện đo** (máy, CPU/RAM, cấu hình Postgres, pool size, VU, seed,
  số lần chạy, độ phân tán), kết luận, giới hạn của phép đo.
- Cập nhật hậu quả vào đúng chỗ: số đo ba biến thể ghi vào ADR-0002 (hoặc ADR mới nếu biến thể khác thắng rõ
  *và* vẫn đúng); đáp án Q32/Q33 vào `concurrency.md` #11 / ADR-0002 rồi **xoá hàng** khỏi `open-questions.md`.

**Ngoài:**
- Viết cache / rate limit → M3-03, M3-04. Dashboard + alert → M3-06. README tổng → M4-02.
- Đổi cơ chế khoá ghế production dựa trên số đo: nếu số đo gợi ý đổi, đó là ADR + ticket mới, không làm ở đây.
- Quyết định tách service (Q34, `architecture.md` D10): chỉ ghi số liệu liên quan, không quyết.

## Đọc trước
- [`ADR-0002`](../../../docs/adr/0002-oversell-prevention.md) — mục "Revalidate at Gate C" (các biến thể + tiêu chí).
- [`concurrency.md`](../../../docs/design/concurrency.md) — #1–#3 (cơ chế đã chọn), #9, #11.
- [`open-questions.md`](../../../docs/learning/open-questions.md) — Q32, Q33, Q34.
- `perf/baseline.csv` và ticket M3-02 (cột, kịch bản).

## Test
`pair-tdd`: AI viết các test / script này trước; người học chạy, đọc số, **viết kết luận** (AI giúp đọc số, không viết hộ kết luận).

| Test | Khẳng định | Đỏ nếu… |
|---|---|---|
| `Invariant1_AfterEachVariantRun_NoSeatHasTwoBookings` (mọi biến thể, mọi lần chạy) | không ghế nào có hai đơn | biến thể nào bán trùng ⇒ loại, không ghi số |
| `SeatLockVariants_AllReturnSameBusinessResults` (integration, cùng kịch bản nhỏ) | mọi biến thể cho cùng tập `201`/`409` hợp lệ cho cùng đầu vào | một biến thể khác kết quả nghiệp vụ |
| Script k6 `browse` / `hot-release` / `checkin` có thể chạy lại bằng một lệnh, thresholds giữ nguyên so với M3-02 | điều kiện đo lặp lại được | thay đổi kịch bản giữa trước / sau ⇒ bảng so sánh vô giá trị |
| Kiểm `perf/baseline.csv` đủ cột, mỗi dòng có biến thể + số lần chạy | dữ liệu so sánh đầy đủ | thiếu cột / dòng "sau" không có dòng "trước" tương ứng |

## Câu hỏi review
- Cái gì khác giữa dòng "trước" và "sau" ngoài cache? Có biến nhiễu nào (cache nóng/lạnh, seed khác, máy bận) chưa kiểm soát?
- Độ phân tán giữa các lần chạy lớn cỡ nào so với mức cải thiện bạn tuyên bố?
- Biến thể nhanh nhất có sinh `40P01` không? Vì sao "nhanh hơn" chưa đủ để đổi cơ chế (tiêu chí của ADR-0002)?
- `QueueLimit` lớn hơn làm p99 request được nhận tệ đi hay tốt lên, và đổi lại cái gì?

## Done khi
- [ ] `perf/report.md` có bảng trước / sau, điều kiện đo, số lần chạy + độ phân tán, và kết luận do người học viết
- [ ] `perf/baseline.csv` có dòng trước và sau mỗi lần tối ưu, cho cache và cho từng biến thể khoá ghế
- [ ] `PermitLimit` / `QueueLimit` chốt bằng số, ghi vào `concurrency.md` #11; Q32, Q33 đóng (hàng bị xoá, đáp án nằm ở design / ADR-0002)
- [ ] Mọi claim hiệu năng trong `report.md` trỏ tới dòng `baseline.csv` hoặc đồ thị đo được
