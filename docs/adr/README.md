# Architecture Decision Records

Mỗi ADR ghi **một quyết định khó đảo ngược**: bối cảnh, quyết định, vì sao chọn, phương án đã loại, hệ
quả. ADR là **bất biến**: không sửa nội dung quyết định sau khi đã `accepted`. ADR bị thay thế vẫn được
giữ lại để làm lịch sử (status ghi `superseded by ADR-xxxx`). Quyết định mới đổi ý quyết định cũ ⇒ viết
**ADR mới** có trường `Supersedes`, rồi đổi status của ADR cũ.

| ADR | Tiêu đề | Status | Thay thế / bị thay bởi |
|---|---|---|---|
| [0001](0001-inventory-model.md) | Mô hình tồn kho theo tier | superseded | bị thay bởi ADR-0003 |
| [0002](0002-oversell-prevention.md) | Chống bán trùng bằng khoá ghế theo thứ tự rồi conditional UPDATE | accepted | — |
| [0003](0003-seat-based-inventory.md) | Mô hình tồn kho theo ghế của suất chiếu | accepted | thay ADR-0001 |
| [0004](0004-seat-hold-aggregate-boundary.md) | Ranh giới aggregate — `Seat hold` là aggregate root | superseded | bị thay bởi ADR-0005 |
| [0005](0005-showtime-seat-aggregate.md) | `Showtime seat` là aggregate root riêng; giữ n ghế là một DB transaction | accepted | thay ADR-0004 |
| [0006](0006-payment-expiry-refund-flow.md) | Luồng tiền — hết hạn theo thời gian, hoàn tiền qua outbox | accepted | — |
| [0007](0007-per-customer-showtime-quota.md) | Giới hạn 10 ghế mỗi người mỗi suất ép bằng khoá theo cặp | accepted | — |
| [0008](0008-cross-module-transaction.md) | Mỗi module một `DbContext`, chung một connection và transaction | accepted | — |
| [0009](0009-module-communication.md) | Module gọi nhau qua interface / event trong transaction / outbox | accepted | — |
| [0010](0010-payment-intent-states.md) | `Payment intent` dùng `Unpaid` và `Closed` | accepted | — |
| [0011](0011-idempotency-in-business-transaction.md) | Idempotency key chiếm trong transaction nghiệp vụ; savepoint giữ kết quả lỗi | accepted | — |
| [0012](0012-seat-map-cache-per-seat-versioned.md) | Cache sơ đồ ghế ghi đè từng ghế theo version qua outbox | accepted | — |
| [0013](0013-check-in-db-unique-only.md) | Check-in đúng một lần chỉ bằng khoá chính ở DB | accepted | — |
| [0014](0014-refresh-token-rotation-strict-reuse.md) | Refresh token rotation, dùng lại nghiêm ngặt, không ân hạn | accepted | — |
| [0015](0015-take-over-expired-holds.md) | Lệnh giữ ghế được lấy ghế `Held` đã quá hạn | accepted | — |
| [0016](0016-result-and-problem-details.md) | Lỗi nghiệp vụ là `Result`; mọi lỗi ra ngoài là problem+json | accepted | — |
| [0017](0017-handler-per-use-case-no-mediator.md) | Mỗi use case một handler, transaction tường minh, không mediator | accepted — một phần bị thay | một phần bị thay bởi ADR-0018 |
| [0018](0018-domain-and-application-primitives.md) | Domain primitives có base class; handler hiện thực interface chung; event gom trong aggregate, publish tường minh | accepted — một phần bị thay | thay một phần ADR-0017; một phần bị thay bởi ADR-0019 |
| [0019](0019-integration-events-separate-from-domain-events.md) | Integration event tách khỏi domain event; module phát dịch domain event sang integration event | accepted | thay một phần ADR-0018 |
