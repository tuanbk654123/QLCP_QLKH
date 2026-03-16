# Test MinIO trên Windows (local)

## 1) Bật Docker Desktop

- Cài Docker Desktop (Windows).
- Mở Docker Desktop và chờ đến khi trạng thái “Docker Engine running”.

## 2) Chạy MinIO bằng docker compose

Mở PowerShell tại thư mục repo:

```powershell
cd "C:\Users\NMT\Desktop\New folder\qlcp_qlkh"
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d minio minio-init
```

MinIO sẽ chạy tại:
- API: http://localhost:9000
- Console: http://localhost:9001

Tài khoản mặc định (local):
- User: `minioadmin`
- Pass: `minioadmin123`

Bucket mặc định được tạo tự động: `qlkh-uploads`

## 3) Chạy Backend (local)

Backend ở môi trường Development đã cấu hình sẵn lưu file lên MinIO tại:
[BE_QLKH/appsettings.Development.json](file:///c:/Users/NMT/Desktop/New%20folder/qlcp_qlkh/BE_QLKH/appsettings.Development.json)

Chạy BE:

```powershell
cd "C:\Users\NMT\Desktop\New folder\qlcp_qlkh\BE_QLKH"
dotnet run
```

## 4) Test upload từ UI

- Vào “Quản lý chi phí” -> Upload chứng từ/đính kèm
- Vào “Quản lý khách hàng” -> “Link hồ sơ giấy tờ” -> bấm Upload

Sau khi upload:
- DB chỉ lưu link (URL) trả về từ API `/api/upload`
- File nằm trong bucket `qlkh-uploads` trên MinIO

## 5) Nếu muốn chạy full stack bằng Docker

```powershell
cd "C:\Users\NMT\Desktop\New folder\qlcp_qlkh"
docker compose -f docker-compose.yml -f docker-compose.local.yml up -d --build
```

## 6) Download file qua domain (Production)

Nếu deploy bằng Caddy như repo đang dùng (domain `crm.anneco.vn`) thì đã cấu hình sẵn:
- Download object: `https://crm.anneco.vn/minio/<bucket>/<key>`
- Console MinIO: `https://crm.anneco.vn/minio-console/`

Cấu hình này nằm ở: [Caddyfile](file:///c:/Users/NMT/Desktop/New%20folder/qlcp_qlkh/Caddyfile)

Khi backend upload, URL trả về sẽ dùng `Storage:S3:PublicBaseUrl`. Production đã set mặc định trong compose:
- `Storage__S3__PublicBaseUrl=https://crm.anneco.vn/minio`

Nếu m dùng domain khác thì set env `MINIO_PUBLIC_BASE_URL` khi chạy compose:

```powershell
$env:MINIO_PUBLIC_BASE_URL="https://<domain-cua-m>/minio"
docker compose -f docker-compose.yml up -d --build
```

## 6) Tuỳ biến cấu hình (tuỳ chọn)

Có thể set env trước khi chạy compose:

```powershell
$env:MINIO_ROOT_USER="minioadmin"
$env:MINIO_ROOT_PASSWORD="minioadmin123"
$env:MINIO_BUCKET="qlkh-uploads"
$env:MINIO_PUBLIC_BASE_URL="http://localhost:9000"
```
