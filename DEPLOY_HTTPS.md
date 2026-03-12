# Deploy HTTPS (Docker Compose)

## Yêu cầu

- Domain `crm.anneco.vn` trỏ A record về IP server chạy docker.
- Mở firewall/SG: TCP 80 và 443.

## Biến môi trường

Khuyến nghị tạo file `.env` cùng cấp với `docker-compose.yml` để Let’s Encrypt có email liên hệ (không bắt buộc):

```
ACME_EMAIL=you@example.com
```

## Chạy

```
docker compose up -d --build
```

## Kiểm tra

- Web: https://crm.anneco.vn
- API: https://crm.anneco.vn/api/swagger
