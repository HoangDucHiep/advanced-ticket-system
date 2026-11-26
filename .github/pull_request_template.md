## Ticket

<!-- vd: M1-04 — Giữ n ghế all-or-nothing (.scratch/advanced-ticket-system/issues/M1-04-hold-seats.md) -->

## Thay đổi

<!-- 2–5 gạch đầu dòng: hành vi mới, bảng/migration, endpoint -->

## Test và phép thử âm tính

| Test | Phép thử âm tính đã chạy | Kết quả khi phá |
|---|---|---|
|  |  | đỏ |

## Checklist

- [ ] `dotnet build -c Release` 0 warning; `dotnet test` xanh
- [ ] Mỗi test quan trọng đã thấy đỏ (bảng trên)
- [ ] Khớp `api-contract.md` / `data-model.md`; lệch thì đã ghi lý do
- [ ] Không secret trong diff; cấu hình mới qua Options + `ValidateOnStart`
- [ ] Đã tự soát theo `docs/design/coding-conventions.md`
- [ ] Ticket đã tick "Done khi"; `docs/learning/log.md` có mục của ticket
