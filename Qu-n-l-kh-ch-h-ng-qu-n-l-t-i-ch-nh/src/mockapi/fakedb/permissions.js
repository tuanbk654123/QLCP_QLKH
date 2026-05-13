export const roles = [
  { key: 'marketing_sales', label: 'Marketing/ Sales' },
  { key: 'ip_executive', label: 'IP Executive' },
  { key: 'ip_manager', label: 'IP Manager' },
  { key: 'director', label: 'Giám đốc' },
  { key: 'ceo', label: 'Tổng giám đốc' },
  { key: 'accountant', label: 'Kế toán' },
  { key: 'admin', label: 'Admin' },
];

export const schedulingFields = [
  {
    key: 'group_actions',
    label: 'I. Thao tác',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'seedData', label: 'Dữ liệu mẫu' },
      { key: 'generate', label: 'Tạo lịch' },
      { key: 'export', label: 'Xuất Excel' },
    ],
  },
  {
    key: 'group_config',
    label: 'II. Cấu hình',
    children: [
      { key: 'config', label: 'Form cấu hình' },
    ],
  },
  {
    key: 'group_view',
    label: 'III. Hiển thị',
    children: [
      { key: 'view', label: 'Xem lịch' },
    ],
  },
];

export const qlkhFields = [
  {
    key: 'group_general',
    label: 'I. Nhóm thông tin chung',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'name', label: 'Doanh nghiệp' },
      { key: 'businessScale', label: 'Quy mô DN' },
      { key: 'taxCode', label: 'Mã số thuế' },
      { key: 'address', label: 'Địa chỉ' },
      { key: 'representativeName', label: 'Người đại diện' },
      { key: 'representativePosition', label: 'Chức vụ' },
      { key: 'idNumber', label: 'CCCD/Hộ chiếu' },
      { key: 'phone', label: 'SĐT DN' },
      { key: 'email', label: 'Email DN' },
      { key: 'contactPerson', label: 'Người liên hệ' },
      { key: 'contactPhone', label: 'SĐT người liên hệ' },
      { key: 'contactEmail', label: 'Email người liên hệ' },
    ],
  },
  {
    key: 'group_need',
    label: 'II. Nhu cầu - Lead - Tiềm năng',
    children: [
      { key: 'businessNeeds', label: 'Nhu cầu DN' },
      { key: 'potentialLevel', label: 'Mức độ tiềm năng' },
      { key: 'priority', label: 'Ưu tiên' },
      { key: 'sourceClassification', label: 'Phân loại nguồn' },
      { key: 'nsnnSource', label: 'Nguồn NSNN' },
    ],
  },
  {
    key: 'group_core_ip',
    label: 'III. Thông tin SHTT cốt lõi',
    children: [
      { key: 'brandName', label: 'Thương hiệu' },
      { key: 'productsServices', label: 'Sản phẩm/DV' },
      { key: 'ipGroup', label: 'Nhóm SHTT' },
      { key: 'owner', label: 'Chủ sở hữu' },
      { key: 'protectionTerritory', label: 'Lãnh thổ bảo hộ' },
      { key: 'authorization', label: 'Uỷ quyền' },
    ],
  },
  {
    key: 'group_legal',
    label: 'IV. Hồ sơ đơn – văn bằng – pháp lý',
    children: [
      { key: 'filingStatus', label: 'Tình trạng nộp đơn' },
      { key: 'filingDate', label: 'Ngày nộp đơn' },
      { key: 'applicationCode', label: 'Mã đơn/Công bố/VB' },
      { key: 'issueDate', label: 'Ngày cấp' },
      { key: 'expiryDate', label: 'Ngày hết hạn' },
      { key: 'applicationReviewStatus', label: 'Tình trạng xét duyệt' },
      { key: 'processingDeadline', label: 'Hạn xử lý' },
    ],
  },
  {
    key: 'group_renewal',
    label: 'V. Gia hạn & nhắc việc',
    children: [
      { key: 'renewalCycle', label: 'Chu kỳ gia hạn' },
      { key: 'renewalDate', label: 'Ngày cần gia hạn' },
      { key: 'reminderDate', label: 'Ngày nhắc (trước 3 tháng)' },
      { key: 'reminderStatus', label: 'Trạng thái nhắc' },
    ],
  },
  {
    key: 'group_contract',
    label: 'VI. Hợp đồng – tài chính',
    children: [
      { key: 'consultingStatus', label: 'Tình trạng tư vấn' },
      { key: 'contractStatus', label: 'Tình trạng hợp đồng' },
      { key: 'contractNumber', label: 'Số hợp đồng' },
      { key: 'contractValue', label: 'Giá trị hợp đồng' },
      { key: 'stateFee', label: 'Lệ phí NN' },
      { key: 'additionalFee', label: 'Phí phát sinh' },
    ],
  },
  {
    key: 'group_system',
    label: 'VII. Hệ thống – kiểm soát',
    children: [
      { key: 'createdBy', label: 'Người tạo' },
      { key: 'updatedBy', label: 'Người cập nhật' },
      { key: 'updatedAt', label: 'Ngày cập nhật' },
      { key: 'documentLink', label: 'Link hồ sơ giấy tờ' },
      { key: 'auditLog', label: 'Lịch sử tác động' },
    ],
  },
];

export const qlcpFields = [
  {
    key: 'group_request',
    label: 'I. Nhóm thông tin đề nghị – hành chính',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'requester', label: 'Người đề nghị' },
      { key: 'department', label: 'Phòng ban' },
      { key: 'priority', label: 'Ưu tiên' },
      { key: 'requestDate', label: 'Ngày phát sinh giao dịch' },
      { key: 'projectCode', label: 'Mã dự án' },
    ],
  },
  {
    key: 'group_content',
    label: 'II. Nhóm nội dung chi phí',
    children: [
      { key: 'content', label: 'Nội dung' },
      { key: 'description', label: 'Diễn giải' },
      { key: 'transactionType', label: 'Loại giao dịch' },
      { key: 'voucherType', label: 'Loại chứng từ' },
      { key: 'transactionObject', label: 'Đối tượng Thu/Chi' },
      { key: 'note', label: 'Ghi chú' },
    ],
  },
  {
    key: 'group_finance',
    label: 'III. Nhóm tiền – thuế',
    children: [
      { key: 'amountBeforeTax', label: 'Số tiền (Chưa thuế)' },
      { key: 'taxRate', label: 'Thuế suất' },
      { key: 'totalAmount', label: 'Tổng tiền' },
      { key: 'taxCode', label: 'Mã số thuế' },
    ],
  },
  {
    key: 'group_voucher',
    label: 'IV. Nhóm hóa đơn – chứng từ',
    children: [
      { key: 'voucherNumber', label: 'Số hóa đơn / Chứng từ / Hợp đồng' },
      { key: 'voucherDate', label: 'Ngày hóa đơn / Chứng từ / Hợp đồng' },
      { key: 'attachment', label: 'File đính kèm' },
    ],
  },
  {
    key: 'group_payment',
    label: 'V. Nhóm thanh toán',
    children: [
      { key: 'paymentMethod', label: 'Phương thức thanh toán' },
      { key: 'accountNumber', label: 'Tài khoản' },
      { key: 'bank', label: 'Ngân hàng' },
      { key: 'paymentStatus', label: 'Trạng thái thanh toán' },
    ],
  },
  {
    key: 'group_control',
    label: 'VI. Nhóm phê duyệt – kiểm soát',
    children: [
      { key: 'managerApproval', label: 'Quản lý duyệt' },
      { key: 'directorApproval', label: 'Giám đốc duyệt' },
      { key: 'accountantReview', label: 'Kế toán review' },
      { key: 'adjustReason', label: 'Lý do điều chỉnh' },
      { key: 'rejectionReason', label: 'Lý do từ chối' },
      { key: 'riskFlag', label: 'Cờ kiểm soát rủi ro' },
      { key: 'auditLog', label: 'Lịch sử tác động' },
    ],
  },
];

export const userFields = [
  {
    key: 'group_users',
    label: 'I. Nhân viên',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'list', label: 'Danh sách nhân viên' },
      { key: 'detail', label: 'Chi tiết nhân viên' },
      { key: 'create', label: 'Thêm nhân viên' },
      { key: 'update', label: 'Sửa nhân viên' },
      { key: 'delete', label: 'Xóa nhân viên' },
    ],
  },
];

export const dashboardFields = [
  {
    key: 'group_dashboard',
    label: 'I. Dashboard',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'overview', label: 'Tổng quan' },
      { key: 'kpi', label: 'Chỉ số KPI' },
      { key: 'overview_all', label: 'Dashboard tổng (tất cả công ty)' },
    ],
  },
];

export const workDashboardFields = [
  {
    key: 'view',
    label: 'Xem',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'view', label: 'Xem dashboard công việc' },
      { key: 'viewTeam', label: 'Xem theo nhân viên' },
      { key: 'viewAllCompanies', label: 'Xem tổng nhiều công ty' },
    ],
  },
];

export const projectFields = [
  {
    key: 'group_view',
    label: 'I. Dự án',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'view', label: 'Xem danh sách/chi tiết dự án' },
      { key: 'manage', label: 'Thêm/Sửa/Xóa dự án' },
      { key: 'manageModules', label: 'Quản lý module' },
      { key: 'manageTasks', label: 'Quản lý task' },
    ],
  },
];

export const companyFields = [
  {
    key: 'view',
    label: 'Xem',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'view', label: 'Xem danh sách công ty' },
      { key: 'manage', label: 'Thêm/Sửa/Xóa công ty' },
    ],
  },
];

export const roleFields = [
  {
    key: 'view',
    label: 'Xem',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'view', label: 'Xem danh sách chức danh' },
      { key: 'manage', label: 'Thêm/Sửa/Xóa chức danh' },
    ],
  },
];

export const permissionFields = [
  {
    key: 'view',
    label: 'Xem',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'view', label: 'Xem cấu hình phân quyền' },
      { key: 'manage', label: 'Cập nhật phân quyền' },
    ],
  },
];

export const exportFields = [
  {
    key: 'group_export',
    label: 'VIII. Xuất văn bản',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'export_doc', label: 'Xuất văn bản' },
    ],
  },
];

export const auditFields = [
  {
    key: 'group_audit',
    label: 'I. Lịch sử tác động',
    children: [
      { key: 'access_all', label: '(tất cả)' },
      { key: 'view', label: 'Xem lịch sử tác động' },
    ],
  },
];

// Initial permissions (mock)
// R: Read, W: Write, A: Approve, N: None/Hide
export const initialPermissions = {
  qlkh: {},
  qlcp: {},
  scheduling: {},
  users: {},
  dashboard: {},
  work_dashboard: {},
  projects: {},
  companies: {},
  roles: {},
  permissions: {},
  export: {},
  audit: {},
};

// Helper to initialize permissions
const init = () => {
  const getRoleMap = (config) => {
    const map = {};
    roles.forEach((role) => {
      map[role.key] = config[role.key] || 'R';
    });
    return map;
  };

  const moduleDefinitions = [
    { key: 'qlkh', fields: qlkhFields },
    { key: 'qlcp', fields: qlcpFields },
    { key: 'scheduling', fields: schedulingFields },
    { key: 'users', fields: userFields },
    { key: 'dashboard', fields: dashboardFields },
    { key: 'work_dashboard', fields: workDashboardFields },
    { key: 'projects', fields: projectFields },
    { key: 'companies', fields: companyFields },
    { key: 'roles', fields: roleFields },
    { key: 'permissions', fields: permissionFields },
    { key: 'export', fields: exportFields },
    { key: 'audit', fields: auditFields },
  ];

  moduleDefinitions.forEach((mod) => {
    mod.fields.forEach((group) => {
      group.children.forEach((field) => {
        if (!initialPermissions[mod.key][field.key]) {
          initialPermissions[mod.key][field.key] = getRoleMap({
            admin: 'A',
            ceo: 'A',
            director: 'A',
          });
        }
      });
    });
  });
};

init();
