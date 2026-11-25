---
id: 0015
title: Lệnh giữ ghế được lấy ghế Held đã quá hạn mà không chờ worker
status: accepted
date: 2026-11-25
Related: ADR-0005, ADR-0006
---

# Lệnh giữ ghế được lấy ghế `Held` đã quá hạn mà không chờ worker

Q25: ghế `Held` có `held_until` đã qua nhưng worker hết hạn chưa chạy — người khác có giữ được không?
Quyết định: **có** — điều kiện giữ ghế là `status = 'Available' OR (status = 'Held' AND held_until <=
now())`, và sơ đồ ghế hiển thị ghế đó là `Available`. An toàn vì: (1) đơn cũ không thể `Paid` sau
`expires_at` (ADR-0006); (2) lệnh trả ghế của worker lọc theo `hold_id` (ADR-0005), nên khi chạy muộn nó
không đụng ghế đã sang tay. Không chọn "chờ worker": khi đó "hết hạn" phụ thuộc lịch chạy worker, trái
`SC-07` ("ghế hết thời gian giữ ⇒ lập tức người khác đặt được"). Hệ quả: worker trở thành việc **dọn
dẹp** (đổi đơn sang `Expired`, đóng intent) chứ không còn quyết định ghế trống; đếm bốn con số
(`FR-23`) phải tính ghế `Held` quá hạn vào `available`. Chi tiết: `docs/design/concurrency.md` #4.
