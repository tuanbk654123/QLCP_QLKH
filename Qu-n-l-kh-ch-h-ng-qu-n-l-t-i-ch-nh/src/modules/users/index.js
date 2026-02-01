import React, { useEffect, useState, useCallback } from 'react';
import {
  Table,
  Input,
  Button,
  Space,
  Tag,
  Modal,
  Form,
  message,
  Popconfirm,
  Select,
  DatePicker,
  Row,
  Col,
} from 'antd';
import {
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import dayjs from 'dayjs';
import { useAuth } from '../../context/AuthContext';
import { handleApiError } from '../../utils/errorHelper';
import './index.css';

const { Option } = Select;

const ROLE_OPTIONS = [
  { value: 'marketing_sales', label: 'Marketing/Kinh doanh' },
  { value: 'ip_executive', label: 'Chuyên viên SHTT' },
  { value: 'ip_manager', label: 'Trưởng phòng SHTT' },
  { value: 'director', label: 'Giám đốc' },
  { value: 'ceo', label: 'Tổng giám đốc' },
  { value: 'accountant', label: 'Kế toán' },
  { value: 'admin', label: 'Hành chính' },
];

const ROLE_LABELS = ROLE_OPTIONS.reduce((acc, r) => {
  acc[r.value] = r.label;
  return acc;
}, {});

const ROLE_COLORS = {
  admin: 'gold',
  marketing_sales: 'purple',
  ip_executive: 'geekblue',
  ip_manager: 'blue',
  director: 'red',
  ceo: 'volcano',
  accountant: 'green',
};

const Users = () => {
  const { isAdmin } = useAuth();
  const [users, setUsers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  
  const [tableParams, setTableParams] = useState({
    pagination: {
      current: 1,
      pageSize: 10,
      showSizeChanger: true,
    },
  });

  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingUser, setEditingUser] = useState(null);
  const [form] = Form.useForm();
  const [fieldPermissions, setFieldPermissions] = useState({});

  const fetchPermissions = useCallback(async () => {
    try {
      const response = await axios.get('/api/permissions/current', {
        params: { module: 'users' },
      });
      setFieldPermissions(response.data.permissions || {});
    } catch (error) {
      setFieldPermissions({});
    }
  }, []);

  const fetchUsers = useCallback(async () => {
    const level = fieldPermissions.list;
    if (!level && !isAdmin()) return;
    if (level === 'N') return;

    setLoading(true);
    try {
      const { current, pageSize } = tableParams.pagination;
      const params = {
        search,
        page: current,
        limit: pageSize,
        sortField: tableParams.sortField,
        sortOrder: tableParams.sortOrder,
        ...tableParams.filters,
      };

      const response = await axios.get('/api/users', { params });
      setUsers(response.data.users);
      setTableParams((prev) => ({
        ...prev,
        pagination: {
          ...prev.pagination,
          total: response.data.userCount,
        },
      }));
    } catch (error) {
      handleApiError(error, 'Lỗi khi tải dữ liệu nhân viên');
    } finally {
      setLoading(false);
    }
  }, [search, tableParams.pagination.current, tableParams.pagination.pageSize, tableParams.sortField, tableParams.sortOrder, tableParams.filters, fieldPermissions, isAdmin]);

  useEffect(() => {
    fetchPermissions();
  }, [fetchPermissions]);

  useEffect(() => {
    fetchUsers();
  }, [fetchUsers]);

  const handleTableChange = (pagination, filters, sorter) => {
    const newFilters = {};
    Object.keys(filters).forEach((key) => {
      if (filters[key] && filters[key].length > 0) {
        newFilters[key] = filters[key][0];
      }
    });

    setTableParams({
      pagination,
      filters: newFilters,
      sortField: sorter.field,
      sortOrder: sorter.order,
    });
  };

  const getColumnSearchProps = (dataIndex) => ({
    filterDropdown: ({ setSelectedKeys, selectedKeys, confirm, clearFilters }) => (
      <div style={{ padding: 8 }} onKeyDown={(e) => e.stopPropagation()}>
        <Input
          placeholder={`Tìm kiếm ${dataIndex}`}
          value={selectedKeys[0]}
          onChange={(e) => setSelectedKeys(e.target.value ? [e.target.value] : [])}
          onPressEnter={() => confirm()}
          style={{ marginBottom: 8, display: 'block' }}
        />
        <Space>
          <Button
            type="primary"
            onClick={() => confirm()}
            icon={<SearchOutlined />}
            size="small"
            style={{ width: 90 }}
          >
            Tìm kiếm
          </Button>
          <Button
            onClick={() => {
              clearFilters();
              confirm();
            }}
            size="small"
            style={{ width: 90 }}
          >
            Đặt lại
          </Button>
        </Space>
      </div>
    ),
    filterIcon: (filtered) => (
      <SearchOutlined style={{ color: filtered ? '#1890ff' : undefined }} />
    ),
  });

  const handleAdd = () => {
    setEditingUser(null);
    form.resetFields();
    form.setFieldsValue({
      role: 'marketing_sales',
      status: 'active',
    });
    setIsModalVisible(true);
  };

  const handleEdit = (record) => {
    setEditingUser(record);
    form.setFieldsValue(record);
    setIsModalVisible(true);
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`/api/users/${id}`);
      message.success('Xóa nhân viên thành công');
      fetchUsers();
    } catch (error) {
      handleApiError(error, 'Lỗi khi xóa nhân viên');
    }
  };

  const handleSubmit = async (values) => {
    try {
      if (editingUser) {
        await axios.put(`/api/users/${editingUser.id}`, values);
        message.success('Cập nhật nhân viên thành công');
      } else {
        await axios.post('/api/users', {
          ...values,
          password: values.password || '123456', // Default password
        });
        message.success('Thêm nhân viên thành công');
      }
      setIsModalVisible(false);
      form.resetFields();
      fetchUsers();
    } catch (error) {
      handleApiError(error, 'Lỗi khi lưu nhân viên');
    }
  };

  const getRoleText = (role) => {
    return ROLE_LABELS[role] || role;
  };

  const getRoleColor = (role) => {
    return ROLE_COLORS[role] || 'default';
  };

  const formatCurrency = (amount) => {
    if (!amount && amount !== 0) return '';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const listLevel = fieldPermissions.list;
  const createLevel = fieldPermissions.create;
  const updateLevel = fieldPermissions.update;
  const deleteLevel = fieldPermissions.delete;

  const canViewUsers = listLevel ? listLevel !== 'N' : isAdmin();
  const canCreateUser = createLevel ? createLevel === 'W' || createLevel === 'A' : isAdmin();
  const canUpdateUser = updateLevel ? updateLevel === 'W' || updateLevel === 'A' : isAdmin();
  const canDeleteUser = deleteLevel ? deleteLevel === 'W' || deleteLevel === 'A' : isAdmin();

  if (!canViewUsers) {
    return (
      <div style={{ padding: 24, textAlign: 'center' }}>
        <h2>Bạn không có quyền truy cập trang này</h2>
        <p>Vui lòng liên hệ quản trị hệ thống để được cấp quyền.</p>
      </div>
    );
  }

  const columns = [
    { 
      title: 'ID', 
      dataIndex: 'id', 
      key: 'id', 
      width: 70,
      fixed: 'left',
      sorter: true,
      ...getColumnSearchProps('id'),
    },
    { 
      title: 'User_ID', 
      dataIndex: 'userId', 
      key: 'userId', 
      width: 160,
      sorter: true,
      ...getColumnSearchProps('userId'),
    },
    { 
      title: 'Tên đăng nhập', 
      dataIndex: 'username', 
      key: 'username', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('username'),
    },
    { 
      title: 'Họ và tên', 
      dataIndex: 'fullName', 
      key: 'fullName', 
      width: 180,
      sorter: true,
      ...getColumnSearchProps('fullName'),
    },
    { 
      title: 'Email', 
      dataIndex: 'email', 
      key: 'email', 
      width: 200,
      sorter: true,
      ...getColumnSearchProps('email'),
    },
    { 
      title: 'Số điện thoại', 
      dataIndex: 'phone', 
      key: 'phone', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('phone'),
    },
    { 
      title: 'Trạng thái', 
      dataIndex: 'status', 
      key: 'status', 
      width: 160, 
      sorter: true,
      ...getColumnSearchProps('status'),
      render: (status) => {
        if (status === 'active') return <Tag color="green">Hoạt động</Tag>;
        if (status === 'locked') return <Tag color="orange">Khóa</Tag>;
        return <Tag color="red">Không hoạt động</Tag>;
      }
    },
    { 
      title: 'Công ty', 
      dataIndex: 'company', 
      key: 'company', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('company'),
    },
    { 
      title: 'Phòng ban', 
      dataIndex: 'department', 
      key: 'department', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('department'),
    },
    { 
      title: 'Chức danh', 
      dataIndex: 'position', 
      key: 'position', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('position'),
    },
    { 
      title: 'Vai trò (Role)', 
      dataIndex: 'role', 
      key: 'role', 
      width: 160, 
      sorter: true,
      ...getColumnSearchProps('role'),
      render: (role) => (<Tag color={getRoleColor(role)}>{getRoleText(role)}</Tag>) 
    },
    { 
      title: 'Mã nhân viên', 
      dataIndex: 'employeeCode', 
      key: 'employeeCode', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('employeeCode'),
    },
    { 
      title: 'Loại nhân sự', 
      dataIndex: 'employmentType', 
      key: 'employmentType', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('employmentType'),
    },
    { 
      title: 'Địa chỉ', 
      dataIndex: 'address', 
      key: 'address', 
      width: 200,
      sorter: true,
      ...getColumnSearchProps('address'),
    },
    { 
      title: 'Ngày sinh', 
      dataIndex: 'dob', 
      key: 'dob', 
      width: 120,
      sorter: true,
      ...getColumnSearchProps('dob'),
    },
    { 
      title: 'CMND/CCCD', 
      dataIndex: 'idNumber', 
      key: 'idNumber', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('idNumber'),
    },
    { 
      title: 'Ngày cấp', 
      dataIndex: 'idIssuedDate', 
      key: 'idIssuedDate', 
      width: 120,
      sorter: true,
      ...getColumnSearchProps('idIssuedDate'),
    },
    { 
      title: 'Nơi cấp', 
      dataIndex: 'idIssuedPlace', 
      key: 'idIssuedPlace', 
      width: 150,
      sorter: true,
      ...getColumnSearchProps('idIssuedPlace'),
    },
    { 
      title: 'Mã số thuế', 
      dataIndex: 'personalTaxCode', 
      key: 'personalTaxCode', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('personalTaxCode'),
    },
    { 
      title: 'Ngân hàng', 
      dataIndex: 'bankName', 
      key: 'bankName', 
      width: 150,
      sorter: true,
      ...getColumnSearchProps('bankName'),
    },
    { 
      title: 'Số tài khoản', 
      dataIndex: 'bankAccount', 
      key: 'bankAccount', 
      width: 150,
      sorter: true,
      ...getColumnSearchProps('bankAccount'),
    },
    { 
      title: 'Số BHXH', 
      dataIndex: 'socialInsuranceNumber', 
      key: 'socialInsuranceNumber', 
      width: 150,
      sorter: true,
      ...getColumnSearchProps('socialInsuranceNumber'),
    },
    { 
      title: 'Số BHYT', 
      dataIndex: 'healthInsuranceNumber', 
      key: 'healthInsuranceNumber', 
      width: 150,
      sorter: true,
      ...getColumnSearchProps('healthInsuranceNumber'),
    },
    { 
      title: 'Ngày vào làm', 
      dataIndex: 'joinDate', 
      key: 'joinDate', 
      width: 120,
      sorter: true,
      ...getColumnSearchProps('joinDate'),
    },
    { 
      title: 'Lương', 
      dataIndex: 'salary', 
      key: 'salary', 
      width: 150,
      align: 'right',
      sorter: true,
      ...getColumnSearchProps('salary'),
      render: (amount) => formatCurrency(amount),
    },
    { 
      title: 'Loại hợp đồng', 
      dataIndex: 'contractType', 
      key: 'contractType', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('contractType'),
    },
    { 
      title: 'Ngày bắt đầu HĐ', 
      dataIndex: 'contractStartDate', 
      key: 'contractStartDate', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('contractStartDate'),
    },
    { 
      title: 'Ngày kết thúc HĐ', 
      dataIndex: 'contractEndDate', 
      key: 'contractEndDate', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('contractEndDate'),
    },
    { 
      title: 'Địa điểm làm việc', 
      dataIndex: 'workLocation', 
      key: 'workLocation', 
      width: 150,
      sorter: true,
      ...getColumnSearchProps('workLocation'),
    },
    { 
      title: 'Quản lý (Tên)', 
      dataIndex: 'managerName', 
      key: 'managerName', 
      width: 180,
      sorter: true,
      ...getColumnSearchProps('managerName'),
    },
    { 
      title: 'Quản lý (ID)', 
      dataIndex: 'managerId', 
      key: 'managerId', 
      width: 150,
      sorter: true,
      ...getColumnSearchProps('managerId'),
    },
    { 
      title: 'Liên hệ khẩn cấp', 
      dataIndex: 'emergencyContactName', 
      key: 'emergencyContactName', 
      width: 180,
      sorter: true,
      ...getColumnSearchProps('emergencyContactName'),
    },
    { 
      title: 'SĐT khẩn cấp', 
      dataIndex: 'emergencyContactPhone', 
      key: 'emergencyContactPhone', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('emergencyContactPhone'),
    },
    { 
      title: 'Nhóm', 
      dataIndex: 'group', 
      key: 'group', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('group'),
    },
    { 
      title: 'Scope dữ liệu', 
      dataIndex: 'dataScope', 
      key: 'dataScope', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('dataScope'),
      render: (scope) => scope === 'company' ? 'Toàn công ty' : scope === 'department' ? 'Phòng ban' : 'Cá nhân'
    },
    { 
      title: 'Quyền phê duyệt', 
      dataIndex: 'approveRight', 
      key: 'approveRight', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('approveRight'),
      render: (val) => val ? 'Có' : 'Không'
    },
    { 
      title: 'Quyền xem TC', 
      dataIndex: 'financeViewRight', 
      key: 'financeViewRight', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('financeViewRight'),
      render: (val) => val ? 'Có' : 'Không'
    },
    { 
      title: 'Quyền xuất DL', 
      dataIndex: 'exportDataRight', 
      key: 'exportDataRight', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('exportDataRight'),
      render: (val) => val ? 'Có' : 'Không'
    },
    { 
      title: 'Ngày tạo', 
      dataIndex: 'createdAt', 
      key: 'createdAt', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('createdAt'),
    },
    { 
      title: 'Người tạo', 
      dataIndex: 'createdBy', 
      key: 'createdBy', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('createdBy'),
    },
    { 
      title: 'Ngày cập nhật', 
      dataIndex: 'updatedAt', 
      key: 'updatedAt', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('updatedAt'),
    },
    { 
      title: 'Người cập nhật', 
      dataIndex: 'updatedBy', 
      key: 'updatedBy', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('updatedBy'),
    },
    { 
      title: 'Ngày nghỉ việc', 
      dataIndex: 'offboardDate', 
      key: 'offboardDate', 
      width: 140,
      sorter: true,
      ...getColumnSearchProps('offboardDate'),
    },
    { 
      title: 'Ghi chú', 
      dataIndex: 'notes', 
      key: 'notes', 
      width: 200,
      sorter: true,
      ...getColumnSearchProps('notes'),
    },
    {
      title: 'Thao tác',
      key: 'action',
      width: 150,
      fixed: 'right',
      render: (_, record) => (
        <Space size="middle">
          {canUpdateUser && (
            <Button type="link" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
              Sửa
            </Button>
          )}
          {canDeleteUser && (
            <Popconfirm
              title="Bạn có chắc chắn muốn xóa?"
              onConfirm={() => handleDelete(record.id)}
              okText="Có"
              cancelText="Không"
            >
              <Button type="link" danger icon={<DeleteOutlined />}>
                Xóa
              </Button>
            </Popconfirm>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div className="users-container">
      <div className="users-header">
        <h2>Quản lý nhân viên</h2>
        <Space>
          <Input
            placeholder="Tìm kiếm nhân viên..."
            prefix={<SearchOutlined />}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setTableParams(prev => ({ ...prev, pagination: { ...prev.pagination, current: 1 } }));
            }}
            style={{ width: 300 }}
          />
          {canCreateUser && (
            <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
              Thêm nhân viên
            </Button>
          )}
        </Space>
      </div>

      <Table
        columns={columns}
        dataSource={users}
        loading={loading}
        rowKey="id"
        pagination={tableParams.pagination}
        onChange={handleTableChange}
        scroll={{ x: 'max-content', y: 'calc(100vh - 300px)' }}
        bordered
      />
      
      {/* Modal code can remain as is, or I can copy it if needed. 
          The original code had a Modal inside Users component or imported?
          Original code had inline Modal rendering (which I missed in my manual copy above).
          Wait, I replaced the whole file but I didn't include the Modal render part!
          The original file had a Modal. I need to keep it.
          Let me check the original file content again to restore the Modal part.
      */}
      <Modal
        title={editingUser ? 'Sửa thông tin nhân viên' : 'Thêm nhân viên'}
        open={isModalVisible}
        onCancel={() => setIsModalVisible(false)}
        footer={null}
        width={800}
      >
        <Form
          form={form}
          layout="vertical"
          scrollToFirstError
          onFinish={handleSubmit}
        >
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="userId"
                label="User ID"
                rules={[{ required: true, message: 'Vui lòng nhập User ID' }]}
              >
                <Input disabled={!!editingUser} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="username"
                label="Tên đăng nhập"
                rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập' }]}
              >
                <Input disabled={!!editingUser} />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
             <Col span={12}>
              <Form.Item
                name="fullName"
                label="Họ và tên"
                rules={[{ required: true, message: 'Vui lòng nhập họ tên' }]}
              >
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="email"
                label="Email"
                rules={[
                  { required: true, message: 'Vui lòng nhập email' },
                  { type: 'email', message: 'Email không hợp lệ' },
                ]}
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
             <Col span={12}>
              <Form.Item
                name="phone"
                label="Số điện thoại"
              >
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="role"
                label="Vai trò"
                rules={[{ required: true, message: 'Vui lòng chọn vai trò' }]}
              >
                <Select>
                  {ROLE_OPTIONS.map((role) => (
                    <Option key={role.value} value={role.value}>
                      {role.label}
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
             <Col span={12}>
              <Form.Item
                name="company"
                label="Công ty"
              >
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="department"
                label="Phòng ban"
              >
                <Input />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
             <Col span={12}>
              <Form.Item
                name="position"
                label="Chức danh"
              >
                <Input />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="status"
                label="Trạng thái"
              >
                <Select>
                  <Option value="active">Hoạt động</Option>
                  <Option value="locked">Khóa</Option>
                  <Option value="inactive">Không hoạt động</Option>
                </Select>
              </Form.Item>
            </Col>
          </Row>

           {!editingUser && (
            <Form.Item
              name="password"
              label="Mật khẩu"
              rules={[{ required: true, message: 'Vui lòng nhập mật khẩu' }]}
            >
              <Input.Password placeholder="Mặc định: 123456" />
            </Form.Item>
          )}

          <Form.Item>
            <Space style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button onClick={() => setIsModalVisible(false)}>Hủy</Button>
              <Button type="primary" htmlType="submit">
                Lưu
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Users;
