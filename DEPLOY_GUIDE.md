# Hướng dẫn Deploy QLKHG lên Server

## 1. Chuẩn bị Server
SSH vào server (như hướng dẫn trước):
```bash
ssh root@103.153.73.78
```

## 2. Cài đặt Docker và Docker Compose (trên Server)
Copy và chạy từng lệnh sau:

```bash
# Cập nhật hệ thống
apt-get update
apt-get install -y ca-certificates curl gnupg

# Thêm Docker GPG key
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg

# Thêm repository
echo \
  "deb [arch="$(dpkg --print-architecture)" signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  "$(. /etc/os-release && echo "$VERSION_CODENAME")" stable" | \
  tee /etc/apt/sources.list.d/docker.list > /dev/null

# Cài đặt Docker
apt-get update
apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

# Kiểm tra cài đặt
docker compose version
```

## 3. Upload Code lên Server
Bạn có 2 cách để đưa code lên server:

### Cách 1: Dùng Git (Khuyên dùng)
Nếu code của bạn đã đẩy lên Github/Gitlab:
```bash
git clone <link_repo_cua_ban>
cd <ten_thu_muc_repo>
```

### Cách 2: Upload trực tiếp từ máy tính (nếu chưa có Git online)
Bạn cần nén thư mục dự án lại thành file `.zip` hoặc `.tar.gz`.
Sau đó dùng lệnh `scp` (trên máy tính của bạn, không phải trên server) để gửi file lên:

```bash
# Trên máy tính Windows của bạn (Terminal)
scp C:\path\to\your\project.zip root@103.153.73.78:/root/
```
Sau đó SSH vào server và giải nén:
```bash
unzip project.zip
cd project
```

## 4. Chạy dự án
Tại thư mục chứa file `docker-compose.yml` trên server:

```bash
docker compose up -d --build
```

Lệnh này sẽ:
1.  Tự động tải MongoDB.
2.  Build Backend và Frontend.
3.  Chạy tất cả dưới nền.

## 5. Kiểm tra
Truy cập trình duyệt: `http://103.153.73.78`
- Frontend sẽ hiện ra.
- API sẽ được gọi qua `http://103.153.73.78/api/...` (đã được cấu hình tự động).
