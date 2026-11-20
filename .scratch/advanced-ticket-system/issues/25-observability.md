# 25: Observability — log, metric, trace, dashboard

Mode: you-implement-i-review
Gate: D
Layer: code
Blocked by: 23
Traces to: FR-30
Concurrency problem: #11 #12

## Vì sao có ticket này

Hệ thống chạy trong production mà không có observability thì bạn **mù**. Khi mở bán phim hot
chậm lại, bạn không biết chậm ở đâu: ở DB, ở Redis, ở connection pool, hay ở chỗ đang chờ
lock. Đó là sự khác biệt giữa một engineer và một người chỉ biết code.

Và observability cũng là thứ cho phép bạn **chứng minh** các quyết định ở T12. Nếu bạn nói
"conditional UPDATE ít lock contention hơn", bạn cần một dashboard cho thấy counter đó.

Ba điều cần có:

1. **Structured log** với correlation ID đi xuyên qua mọi lớp — điều tra một request là
   câu chuyện, không phải việc săn dò chuỗi log.
2. **Metric** — và đây là điểm quan trọng: metric phải đo được **hành vi nghiệp vụ**, không
   chỉ hạ tầng. "Số lần bán trùng ghế bị chặn" là metric nghiệp vụ; "số request" thì không.
3. **Trace** — đi xuyên qua các module, để thấy một request đặt vé đi qua những bước nào và
   chỗ nào chậm.

## Bạn tạo ra

- **Serilog** với structured JSON output, correlation ID (lấy từ header `X-Correlation-Id`,
  sinh nếu thiếu, trả lại trong response).
- **OpenTelemetry**: trace với exporter OTLP, span cho từng thao tác DB và Redis.
- **Prometheus metrics** qua endpoint `/metrics`:
  - Hạ tầng: HTTP request duration (histogram theo route), connection pool saturation, Redis
    latency.
  - **Nghiệp vụ**: số đơn bị từ chối vì ghế đã có người, số đặt n ghế bị từ chối vì
    all-or-nothing, số chỗ giữ ghế bị hết hạn, số webhook bị từ chối vì trùng, số lần chặn do
    idempotency, số check-in bị từ chối.
  - **Counter quan trọng nhất**: `lock_contentions` — đây là thứ đo được khác biệt giữa hai
    phương án giữ ghế.
- **Docker Compose** cho Grafana + Prometheus (+ Loki nếu bạn muốn log), với dashboard.
- **Alert rule** cơ bản: tỉ lệ lỗi tăng, p99 vượt ngưỡng, số lần bán trùng ghế bị chặn tăng
  đột ngột.
- Test: correlation ID đi xuyên suốt và xuất hiện trong log.

## AI làm gì

AI **không viết code**. Khi review, AI hỏi:

- "Correlation ID có đi xuyên qua **cả** các lớp không — controller, service, repository,
  HTTP client, background worker?" Hỏi riêng phần background worker, vì đó là chỗ hay bị
  bỏ sót.
- "Metric nghiệp vụ nào đo được **hành vi** chứ không đo hạ tầng?" Nếu bạn chỉ có metric HTTP
  thì bạn chưa có observability cho bài toán này.
- "Histogram có `buckets` phù hợp không? Bucket mặc định của .NET có đủ chi tiết ở vùng p99
  không?"
- "Dashboard có câu hỏi nào trả lời được không? Ví dụ: 'đang có bán trùng ghế không', 'lock có
  đang tranh chấp không'?"
- "Alert rule có **nghĩa** không, hay chỉ bắn khi có gì đó hỏng?"
- "Metric có bị **ghi theo label động** không (ví dụ `ticketId`)? Nếu có, số series sẽ phình
  vô hạn — đây là cái giết chết Prometheus phổ biến nhất."

## Tiêu chí hoàn thành

- [ ] Log JSON có correlation ID; response trả lại `X-Correlation-Id` khớp
- [ ] Correlation ID đi xuyên qua cả background worker
- [ ] Có metric nghiệp vụ đo hành vi: đơn bị từ chối vì ghế đã có người, đặt n ghế bị từ chối
      vì all-or-nothing, chỗ giữ ghế hết hạn, webhook trùng, idempotency chặn
- [ ] Có `lock_contentions` counter
- [ ] Trace span cho thao tác DB và Redis
- [ ] Grafana dashboard trả lời được câu hỏi "đang có bán trùng ghế / lock contention không"
- [ ] Có ít nhất 2 alert rule
- [ ] **Không metric nào dùng label động không giới hạn**
- [ ] Test chứng minh correlation ID đi xuyên suốt
