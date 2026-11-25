---
id: 0012
title: Cache sơ đồ ghế — ghi đè từng ghế theo version qua outbox, single-flight hai tầng khi miss
status: accepted
date: 2026-11-25
Related: ADR-0009
---

# Cache sơ đồ ghế — ghi đè từng ghế theo version qua outbox, single-flight hai tầng khi miss

Sơ đồ ghế là lượt đọc nóng nhất khi mở bán (`SC-11`) và đổi theo từng lần giữ ghế. Quyết định: cache
Redis một hash mỗi suất (mỗi ghế một field `status|heldUntil|version`); mỗi lần đổi trạng thái ghế tăng
`showtime_seats.version` và ghi outbox `SeatStatesChanged` cùng transaction; dispatcher ghi đè **từng
ghế** bằng script Lua **chỉ khi version mới hơn**; lần dựng lại từ DB dùng cùng quy tắc nên snapshot
cũ không đè được trạng thái mới. Khi miss: single-flight trong tiến trình (`Lazy<Task>`) + khoá Redis
`SET NX PX` giữa các instance (chỉ là tối ưu). Không chọn **xoá key khi đổi** (mỗi lần giữ ghế thành
một lần miss đúng lúc DB nóng nhất), **version theo suất** (một hot row mới mà mọi người mua cùng
cập nhật), **ghi cache ngay trong request** (tiến trình chết sau commit ⇒ cache cũ tới hết TTL).
Hệ quả: cột `version` trên `showtime_seats`; cache trễ cỡ chu kỳ dispatcher — chấp nhận được vì sơ đồ
là ảnh chụp (`BR-23`) và lệnh giữ luôn kiểm lại DB. Chi tiết: `docs/design/concurrency.md` #9, #10.
