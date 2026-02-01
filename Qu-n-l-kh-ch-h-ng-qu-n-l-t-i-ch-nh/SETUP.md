# Hướng dẫn cài đặt và chạy dự án

## Yêu cầu hệ thống

- Node.js >= 14.0.0
- npm >= 6.0.0

## Cài đặt

1. Cài đặt các dependencies:
```bash
npm install
```

## Chạy ứng dụng

```bash
npm start
```

Ứng dụng sẽ chạy tại http://localhost:3000

## Cấu trúc dự án

```
QLNS/
├── public/
│   └── index.html          # HTML template
├── src/
│   ├── components/          # Components dùng chung
│   │   └── AppLayout/       # Layout chính với sidebar
│   ├── modules/             # Các module chính
│   │   ├── customers/       # Quản lý khách hàng
│   │   ├── businesses/      # Quản lý doanh nghiệp
│   │   ├── tasks/           # Quản lý công việc nội bộ
│   │   ├── transactions/    # Quản lý thu chi
│   │   └── dashboard/       # Dashboard tổng quan
│   ├── mockapi/             # Mock API và dữ liệu giả
│   │   ├── fakedb/          # Dữ liệu giả
│   │   └── index.js         # Cấu hình mock API
│   ├── routes/              # Cấu hình routing
│   ├── App.js               # Component chính
│   └── index.js             # Entry point
├── package.json
└── README.md
```

## Tính năng

### 1. Quản lý khách hàng
- Xem danh sách khách hàng
- Thêm, sửa, xóa khách hàng
- Tìm kiếm khách hàng
- Xem thống kê đơn hàng và doanh thu

### 2. Quản lý doanh nghiệp
- Xem danh sách doanh nghiệp
- Thêm, sửa, xóa doanh nghiệp
- Tìm kiếm doanh nghiệp
- Quản lý thông tin doanh nghiệp (mã số thuế, ngành nghề, v.v.)

### 3. Quản lý công việc nội bộ
- Xem danh sách công việc
- Thêm, sửa, xóa công việc
- Lọc theo trạng thái (Chờ xử lý, Đang thực hiện, Hoàn thành, Đã hủy)
- Theo dõi tiến độ công việc
- Quản lý ưu tiên và người thực hiện

### 4. Quản lý thu chi
- Xem danh sách giao dịch thu/chi
- Thêm, sửa, xóa giao dịch
- Tìm kiếm và lọc giao dịch
- Xem tổng kết thu chi và số dư
- Thống kê theo thời gian

### 5. Dashboard
- Tổng quan số liệu thống kê
- Hiển thị tổng khách hàng, doanh nghiệp, công việc
- Hiển thị tổng doanh thu

## Công nghệ sử dụng

- **React 18.2.0** - UI Framework
- **Ant Design 5.0.1** - UI Component Library
- **React Router 6.6.0** - Routing
- **Axios 1.2.1** - HTTP Client
- **Axios Mock Adapter** - Mock API
- **Moment.js** - Date handling
- **Styled Components** - CSS-in-JS

## Lưu ý

- Dữ liệu hiện tại là dữ liệu giả (mock data)
- Tất cả các thao tác CRUD đều được mock, không lưu vào database thật
- Để kết nối với backend thật, cần thay thế các API endpoints trong các module

