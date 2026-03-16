# Hướng dẫn sử dụng hệ thống Quản lý Khách hàng & Tài chính (QLKHG)

Tài liệu này cung cấp hướng dẫn chi tiết về cách sử dụng các chức năng của hệ thống và giải thích các công thức tính toán trên Dashboard.

## 1. Dashboard (Bảng điều khiển)
Màn hình này cung cấp cái nhìn tổng quan về tình hình hoạt động của doanh nghiệp.

### Các chỉ số chính (Metrics)
*   **Tổng khách hàng**:
    *   **Ý nghĩa**: Tổng số lượng khách hàng có trong hệ thống (theo bộ lọc thời gian nếu có).
    *   **Công thức**: Đếm tất cả bản ghi trong danh sách Khách hàng (`customers`).
    *   **Lưu ý**: Có hiển thị số lượng khách hàng đang hoạt động (`Active`).

*   **Tổng doanh thu**:
    *   **Ý nghĩa**: Tổng số tiền thực thu từ các phiếu thu đã hoàn thành.
    *   **Công thức**: Tổng giá trị (`TotalAmount`) của các phiếu trong mục **Chi phí** có:
        *   Loại giao dịch (`TransactionType`) = "Thu"
        *   Trạng thái thanh toán (`PaymentStatus`) = "Đã thanh toán"

*   **Tổng chi phí**:
    *   **Ý nghĩa**: Tổng số tiền thực chi cho các hoạt động của doanh nghiệp.
    *   **Công thức**: Tổng giá trị (`TotalAmount`) của các phiếu trong mục **Chi phí** có:
        *   Loại giao dịch (`TransactionType`) = "Chi"
        *   Trạng thái thanh toán (`PaymentStatus`) = "Đã thanh toán"

### Biểu đồ
1.  **Tổng kết thu chi (Biểu đồ cột)**:
    *   Hiển thị dòng tiền Thu/Chi theo thời gian thực (ngày/tháng).
    *   Giúp so sánh trực quan giữa Thu và Chi.

2.  **Phân bổ Thu / Chi (Biểu đồ tròn)**:
    *   Tỷ lệ phần trăm giữa Tổng Thu và Tổng Chi trong khoảng thời gian được chọn.

3.  **Tăng trưởng khách hàng (Biểu đồ cột)**:
    *   **Tổng KH mới**: Số lượng khách hàng tham gia (`JoinDate`) trong từng tháng của năm được chọn.
    *   **Đã tư vấn**: Số lượng khách hàng trong tháng đó có trạng thái tư vấn (`ConsultingStatus`) chứa từ khóa "tư vấn".

4.  **Chi phí theo dự án (Biểu đồ tròn)**:
    *   Phân bổ chi phí theo từng Mã dự án (`ProjectCode`).
    *   Chỉ tính các khoản Chi đã thanh toán.

---

## 2. Quản lý Khách hàng (Customers)
Nơi lưu trữ và quản lý thông tin chi tiết của khách hàng.

*   **Chức năng chính**:
    *   **Thêm mới**: Nhập thông tin khách hàng, người đại diện, nhu cầu kinh doanh.
    *   **Theo dõi IP**: Quản lý trạng thái Bản quyền, Nhãn hiệu, Sáng chế, Kiểu dáng công nghiệp.
    *   **Phân loại**: Nhóm khách hàng (VIP, Mới, Thân thiết...), Nguồn khách hàng.
    *   **Trạng thái**: Active (Đang hoạt động) / Inactive (Ngừng hoạt động).

---

## 3. Quản lý Chi phí (Costs)
Module quan trọng để quản lý dòng tiền và quy trình phê duyệt thanh toán.

*   **Quy trình tạo phiếu**:
    1.  Người dùng tạo phiếu (Thu hoặc Chi).
    2.  Nhập thông tin: Số tiền, Nội dung, Mã dự án, Người yêu cầu.
    3.  Đính kèm hóa đơn/chứng từ (nếu có).

*   **Quy trình phê duyệt (Approval Workflow)**:
    *   Hệ thống hỗ trợ quy trình duyệt nhiều cấp với tính năng thông báo qua Email:
        1.  **Quản lý (Manager/IP Manager)**:
            *   Nhận thông báo khi có phiếu mới ở trạng thái "Đợi duyệt".
            *   Kiểm tra nội dung phiếu và quyết định Duyệt hoặc Từ chối.
            *   Sau khi duyệt, phiếu chuyển sang trạng thái "Quản lý duyệt".
        2.  **Giám đốc (Director/CEO)**:
            *   Nhận thông báo khi Quản lý đã duyệt xong.
            *   **Duyệt vượt cấp (Skip-level)**: Giám đốc có thể duyệt trực tiếp các phiếu đang ở trạng thái "Đợi duyệt" (bỏ qua bước Quản lý duyệt) nếu cần thiết.
            *   Sau khi duyệt, phiếu chuyển sang trạng thái "Giám đốc duyệt".
        3.  **Kế toán (Accountant)**:
            *   Nhận thông báo khi Giám đốc đã duyệt.
            *   Ở bước này (phiếu đang chờ thanh toán), Kế toán có quyền **chỉnh sửa thông tin chứng từ** (Số chứng từ, Ngày chứng từ, Hóa đơn,...) và thông tin thanh toán trước khi xác nhận.
            *   Khi nhấn **"Duyệt"**, hệ thống sẽ tự động lưu các chỉnh sửa và chuyển phiếu sang trạng thái **"Đã thanh toán"**.
    
    *   **Hệ thống thông báo (Notifications)**:
        *   **Email**: Mỗi khi thay đổi trạng thái phiếu, hệ thống sẽ gửi email thông báo tiêu đề "QLKH Notification" đến người có trách nhiệm tiếp theo.
        *   **Chuông thông báo (In-app)**: Biểu tượng chuông trên thanh menu sẽ hiển thị thông báo mới. Người dùng có thể nhấp vào thông báo để mở ngay chi tiết phiếu chi cần xử lý.
        *   **Chế độ xem từ thông báo**: Khi mở phiếu từ thông báo, giao diện sẽ tập trung vào việc duyệt/từ chối, ẩn bớt các nút không cần thiết (như nút Lưu) để tối ưu thao tác.

    *   Chỉ khi phiếu có trạng thái **"Đã thanh toán"**, số tiền mới được tính vào Dashboard.

---

## 4. Quản lý Hợp đồng (Contracts)
Quản lý các hợp đồng ký kết với khách hàng hoặc đối tác.

*   **Chức năng**:
    *   Lưu trữ số hợp đồng, ngày ký, ngày hiệu lực/hết hạn.
    *   Theo dõi giá trị hợp đồng và các điều khoản thanh toán.
    *   Trạng thái hợp đồng: Mới, Đang thực hiện, Đã thanh lý, v.v.

---

## 5. Hệ thống & Phân quyền (System)
Dành cho Quản trị viên (Admin).

*   **Quản lý người dùng (Users)**: Tạo tài khoản nhân viên, cấp lại mật khẩu.
*   **Phân quyền (Permissions)**: Cấu hình quyền truy cập cho từng vai trò (Role) vào từng module hoặc từng trường dữ liệu cụ thể.

---

## 6. Xuất văn bản (Export Word)
Module hỗ trợ tạo file Word hàng loạt từ mẫu (template) có sẵn.

*   **Chức năng chính**:
    *   **Tải lên mẫu (Upload Template)**: Hỗ trợ file định dạng `.docx`.
    *   **Khai báo trường dữ liệu**: Định nghĩa các từ khóa (key) trong file Word cần được thay thế (ví dụ: `text_1`, `text_2`).
    *   **Nhập dữ liệu (Data Sets)**:
        *   Tạo nhiều bộ dữ liệu để xuất ra nhiều file cùng lúc.
        *   Hệ thống sẽ thay thế các từ khóa trong mẫu bằng dữ liệu tương ứng.
    *   **Xuất file**:
        *   Nếu có 1 bộ dữ liệu: Tải về file `.docx`.
        *   Nếu có nhiều bộ dữ liệu: Tải về file nén `.zip` chứa tất cả các file đã tạo.

*   **Quy trình sử dụng**:
    1.  Chọn **Thêm template** (nếu cần xuất nhiều loại văn bản khác nhau).
    2.  Tải file mẫu `.docx` lên.

---

## Deploy HTTPS (Docker Compose)

Xem hướng dẫn: [DEPLOY_HTTPS.md](file:///c:/Users/NMT/Desktop/New%20folder/QLCP_QLKH/DEPLOY_HTTPS.md)
    3.  Thêm các dòng dữ liệu (Data Set).
    4.  Nhập giá trị cho từng trường tương ứng với từ khóa trong file mẫu.
    5.  Nhấn nút **Xuất file** để tải về kết quả.

---

## 7. Quản lý Thu Chi (Transactions)
Theo dõi dòng tiền vào (Thu) và ra (Chi) của doanh nghiệp.

*   **Chức năng chính**:
    *   **Thống kê**: Hiển thị Tổng thu, Tổng chi và Số dư (Balance) hiện tại.
    *   **Quản lý giao dịch**: Thêm mới, sửa, xóa các giao dịch thu/chi.
    *   **Bộ lọc**: Tìm kiếm theo từ khóa, lọc theo loại (Thu/Chi).

---

## 8. Chấm công dự án (Scheduling)
Mục **Chấm công dự án** giúp lập lịch phân công nhân sự theo từng ngày cho danh sách công ty trong một khoảng thời gian dự án. Kết quả có thể xuất ra Excel để gửi nội bộ hoặc dùng làm bảng theo dõi.

### 8.1. Tham số đầu vào
1.  **Danh sách Công ty (Mỗi dòng 1 công ty)**:
    *   Nhập danh sách công ty/đối tác cần đi công tác theo dự án.
    *   Mỗi dòng là một tên công ty.

2.  **Nhóm nhân sự cố định**:
    *   **Sở hữu trí tuệ**: mỗi dòng 1 nhân sự.
    *   **Phát triển thị trường**: mỗi dòng 1 nhân sự.

3.  **Các nhóm nhân sự khác (tuỳ chọn)**:
    *   Nhấn **Thêm nhóm nhân sự** để tạo thêm nhóm (ví dụ: “Kế toán”, “Pháp chế”, “Hành chính”…).
    *   Mỗi nhóm gồm:
        *   Tên nhóm
        *   Danh sách nhân sự (mỗi dòng 1 người)

4.  **Số ngày công / cty**:
    *   Là số “lượt” cần làm việc cho mỗi công ty trong toàn dự án.
    *   Ví dụ: nhập `28` nghĩa là mỗi công ty cần 28 ngày công trong phạm vi dự án.

5.  **Thời gian dự án**:
    *   Chọn khoảng ngày bắt đầu/kết thúc.
    *   Hệ thống tự tạo lịch theo từng ngày trong khoảng thời gian này.

### 8.2. Quy tắc xếp lịch
*   **Mỗi lượt công ty cần đủ người ở tất cả nhóm**:
    *   Mỗi dòng lịch (một công ty trong một ngày) sẽ gán 1 nhân sự ở từng nhóm.
    *   Do đó số “team” tối đa mỗi ngày bị giới hạn bởi nhóm có ít nhân sự nhất.
*   **Chủ nhật nghỉ**:
    *   Các ngày Chủ nhật được đánh dấu nghỉ và không phân công.
*   **Giới hạn 22 ngày/tháng/nhân sự**:
    *   Một nhân sự không được phân công quá 22 ngày trong cùng một tháng.
*   **Ưu tiên công ty thiếu nhiều nhất**:
    *   Thuật toán ưu tiên xếp những công ty còn thiếu nhiều ngày công trước.

### 8.3. Thao tác sử dụng
1.  Nhập danh sách công ty và danh sách nhân sự theo nhóm.
2.  Nhập **Số ngày công / cty** và **Thời gian dự án**.
3.  (Tuỳ chọn) Nhấn **Dữ liệu mẫu** để tự điền dữ liệu demo.
4.  Nhấn **Xếp Lịch** để tạo lịch.
5.  Xem kết quả ở bảng **Kết quả lịch trình** và khối **Thống kê kết quả**.

### 8.4. Thống kê kết quả (giải thích)
*   **Tổng nhu cầu (ngày làm việc)**:
    *   Công thức: `Số công ty * Số ngày công / cty`.
*   **Đáp ứng được (ngày làm việc)**:
    *   Số ngày công mà hệ thống xếp được với nguồn lực hiện có trong phạm vi dự án.
*   **Thiếu (shortage)**:
    *   Nếu > 0 nghĩa là lịch vẫn thiếu so với nhu cầu.
    *   Hệ thống hiển thị gợi ý nhóm nào đang là “nút thắt” và cần thêm bao nhiêu người.

### 8.5. Xuất Excel
Nhấn **Xuất Excel** để tải file `.xlsx`:
*   Sheet **Lịch Phân Công**:
    *   Dạng bảng theo tuần (block 7 ngày).
    *   Ô ngày chứa danh sách phân công theo từng nhóm (nhiều dòng).
*   Các sheet theo từng nhân sự:
    *   Mỗi nhân sự có 1 sheet lịch công tác cá nhân (ngày, thứ, thứ tự ngày làm việc, công ty).

### 8.6. Phân quyền
Quyền thao tác mục này được cấu hình trong **Quản lý Phân quyền** → tab **Chấm công dự án**. Tuỳ theo vai trò, có thể bị giới hạn:
*   Chỉ xem kết quả (không được xếp lịch / xuất Excel)
*   Không được xem module (ẩn hoàn toàn)



---

## 11. Cấu hình Email (Dành cho Admin/Kỹ thuật)

1.  Truy cập trang [Tài khoản Google](https://myaccount.google.com/).
2.  Chọn mục **Bảo mật** (Security) ở menu bên trái.
3.  Trong phần "Cách bạn đăng nhập vào Google" (How you sign in to Google), hãy đảm bảo **Xác minh 2 bước** (2-Step Verification) đã được bật.
4.  Sau khi bật xác minh 2 bước, tìm kiếm từ khóa "Mật khẩu ứng dụng" (App passwords) trên thanh tìm kiếm của Google Account.
5.  Tạo mật khẩu ứng dụng mới:
    *   **Tên ứng dụng**: Nhập tên gợi nhớ, ví dụ: "QLKH System".
    *   Nhấn **Tạo** (Create).
6.  Google sẽ hiển thị một chuỗi ký tự 16 chữ số (ví dụ: `xxxx xxxx xxxx xxxx`).
7.  Copy chuỗi ký tự này và dán vào file cấu hình `appsettings.json` ở phần `Password`.

### Kiểm tra Email
*   Sau khi cấu hình xong, hãy khởi động lại Backend.
*   Thử tạo một phiếu chi mới và gửi duyệt. Người duyệt (Manager/Director) sẽ nhận được email thông báo.
*   **Lưu ý**: Hệ thống đã được cấu hình để tự động cập nhật email của tài khoản `manager` thành `tuanvb96@gmail.com` khi khởi động lại.

## 12. Tài khoản đăng nhập mặc định
Hệ thống cung cấp các tài khoản mặc định với mật khẩu là `123456`.

| Tên đăng nhập | Vai trò | Email mặc định | Quyền hạn chính |
| :--- | :--- | :--- | :--- |
| **admin** | Quản trị hệ thống | admin@example.com | Toàn quyền quản lý hệ thống, người dùng, phân quyền. |
| **ceo** | Tổng giám đốc | ceo@example.com | Xem báo cáo tổng quan, duyệt các phiếu chi giá trị lớn. |
| **director** | Giám đốc | director@example.com | Quản lý điều hành chung, duyệt phiếu chi cấp 2. |
| **accountant** | Kế toán | accountant@example.com | Quản lý thu chi, xác nhận thanh toán, báo cáo tài chính. |
| **sales** | Marketing/Sales | sales@example.com | Quản lý khách hàng, cơ hội kinh doanh. |
| **manager** | Quản lý IP | tuanvb96@gmail.com | Quản lý nhóm IP, phân công công việc. |
| **executive** | Chuyên viên IP | executive@example.com | Thực hiện các công việc chuyên môn về IP. |

## Lưu ý chung
*   **Bộ lọc thời gian**: Trên Dashboard và các báo cáo, hãy chú ý chọn đúng khoảng thời gian (Tháng này, Năm nay, hoặc Tùy chọn) để xem dữ liệu chính xác.
*   **Dữ liệu 2026**: Hệ thống đã hỗ trợ xem báo cáo cho năm tương lai (ví dụ: 2026) nếu có dữ liệu dự báo hoặc kế hoạch được nhập trước.
