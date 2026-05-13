import React, { useState, useEffect } from 'react';
import { Card, Table, Button, Tabs, message, Tag, Typography, Space, Dropdown } from 'antd';
import { SaveOutlined, ReloadOutlined } from '@ant-design/icons';
import axios from 'axios';

const { Title, Text } = Typography;

const PermissionModule = ({ initialTab = 'qlkh' }) => {
  const { checkAuth } = useAuth();
  const [loading, setLoading] = useState(false);
  const [roles, setRoles] = useState([]);
  const [permissions, setPermissions] = useState({
    qlkh: {},
    qlcp: {},
    users: {},
    dashboard: {},
    work_dashboard: {},
    export: {},
    scheduling: {},
    audit: {},
    companies: {},
    projects: {},
    roles: {},
    permissions: {},
  });
  const [fields, setFields] = useState({
    qlkh: [],
    qlcp: [],
    users: [],
    dashboard: [],
    work_dashboard: [],
    export: [],
    scheduling: [],
    audit: [],
    companies: [],
    projects: [],
    roles: [],
    permissions: [],
  });

  useEffect(() => {
    fetchPermissions();
  }, []);

  const fetchPermissions = async () => {
    setLoading(true);
    try {
      const { data } = await axios.get('/api/permissions');
      setRoles(data.roles || []);
      setPermissions({
        qlkh: data.qlkhPermissions || {},
        qlcp: data.qlcpPermissions || {},
        users: data.userPermissions || {},
        dashboard: data.dashboardPermissions || {},
        work_dashboard: data.workDashboardPermissions || {},
        export: data.exportPermissions || {},
        scheduling: data.schedulingPermissions || {},
        audit: data.auditPermissions || {},
        companies: data.companyPermissions || {},
        projects: data.projectPermissions || {},
        roles: data.rolesPermissions || {},
        permissions: data.permissionsPermissions || {},
      });
      setFields({
        qlkh: data.qlkhFields || [],
        qlcp: data.qlcpFields || [],
        users: data.userFields || [],
        dashboard: data.dashboardFields || [],
        work_dashboard: data.workDashboardFields || [],
        export: data.exportFields || [],
        scheduling: data.schedulingFields || [],
        audit: data.auditFields || [],
        companies: data.companyFields || [],
        projects: data.projectFields || [],
        roles: data.rolesFields || [],
        permissions: data.permissionsFields || [],
      });
    } catch (error) {
      console.error('Failed to fetch permissions:', error);
      message.error('Không thể tải dữ liệu phân quyền');
    } finally {
      setLoading(false);
    }
  };

  const handlePermissionChange = (module, fieldKey, roleKey, value) => {
    setPermissions(prev => {
      const newModulePerms = { ...prev[module] };
      
      if (fieldKey === 'access_all') {
        // Nếu chọn (tất cả), cập nhật tất cả các trường có trong module này
        const moduleFields = fields[module] || [];
        moduleFields.forEach(group => {
          group.children.forEach(field => {
            newModulePerms[field.key] = {
              ...(newModulePerms[field.key] || {}),
              [roleKey]: value
            };
          });
        });
      } else {
        // Chỉ cập nhật trường cụ thể
        newModulePerms[fieldKey] = {
          ...(newModulePerms[fieldKey] || {}),
          [roleKey]: value
        };
      }

      return {
        ...prev,
        [module]: newModulePerms
      };
    });
  };

  const handleSave = async () => {
    setLoading(true);
    try {
      await axios.post('/api/permissions', permissions);
      message.success('Cập nhật phân quyền thành công');
      await checkAuth(); // Tải lại quyền để menu cập nhật
    } catch (error) {
      console.error('Failed to save permissions:', error);
      message.error('Lỗi khi lưu phân quyền');
    } finally {
      setLoading(false);
    }
  };

  const getPermissionLabel = (value) => {
    switch (value) {
      case 'A': return 'Toàn quyền';
      case 'W': return 'Chỉnh sửa';
      case 'R': return 'Chỉ xem';
      case 'N': return 'Ẩn';
      default: return 'Ẩn';
    }
  };
  const getPermissionStyle = (value) => {
    switch (value) {
      case 'A':
        return { backgroundColor: '#E6F7E8', color: '#2A7D31', borderColor: '#49BD65' };
      case 'W':
        return { backgroundColor: '#FFF4E6', color: '#A35B00', borderColor: '#FFA940' };
      case 'R':
        return { backgroundColor: '#E6F0FF', color: '#2A4FB3', borderColor: '#69C0FF' };
      case 'N':
      default:
        return { backgroundColor: '#F5F5F5', color: '#595959', borderColor: '#d9d9d9' };
    }
  };

  const renderTable = (moduleName, fields) => {
    // Flatten fields for table
    const dataSource = [];
    fields.forEach(group => {
      group.children.forEach(field => {
        dataSource.push({
          key: field.key,
          fieldLabel: field.label,
          groupLabel: group.label,
          ...field
        });
      });
    });

    const columns = [
      {
        title: 'Nhóm dữ liệu',
        dataIndex: 'groupLabel',
        key: 'groupLabel',
        width: 180,
        fixed: 'left',
        onCell: (record, index) => {
          // Rowspan logic could be added here if needed, but keeping it simple for now
          // Simple grouping logic: check if previous row has same group
          if (index > 0 && dataSource[index - 1].groupLabel === record.groupLabel) {
            return { rowSpan: 0 };
          }
          // Calculate rowspan
          let count = 0;
          for (let i = index; i < dataSource.length; i++) {
            if (dataSource[i].groupLabel === record.groupLabel) count++;
            else break;
          }
          return { rowSpan: count };
        },
        render: (text) => <strong>{text}</strong>
      },
      {
        title: 'Trường dữ liệu',
        dataIndex: 'fieldLabel',
        key: 'fieldLabel',
        width: 240,
        fixed: 'left',
        render: (text) => <Text>{text}</Text>
      },
      ...roles.map(role => ({
        title: role.label,
        dataIndex: role.key,
        key: role.key,
        width: 140,
        render: (_, record) => {
          const currentPerm = permissions[moduleName]?.[record.key]?.[role.key] || 'N';
          
          let items = [
            { key: 'A', label: <div style={{ ...getPermissionStyle('A'), padding: 6, borderRadius: 6, border: '1px solid #49BD65', textAlign: 'center' }}>Toàn quyền</div> },
            { key: 'W', label: <div style={{ ...getPermissionStyle('W'), padding: 6, borderRadius: 6, border: '1px solid #FFA940', textAlign: 'center' }}>Chỉnh sửa</div> },
            { key: 'R', label: <div style={{ ...getPermissionStyle('R'), padding: 6, borderRadius: 6, border: '1px solid #69C0FF', textAlign: 'center' }}>Chỉ xem</div> },
            { key: 'N', label: <div style={{ ...getPermissionStyle('N'), padding: 6, borderRadius: 6, border: '1px solid #d9d9d9', textAlign: 'center' }}>Ẩn</div> },
          ];

          // For 'export' module, only allow 'A' (Toàn quyền) and 'N' (Ẩn)
          if (moduleName === 'export') {
            items = items.filter(item => ['A', 'N'].includes(item.key));
          }

          if (moduleName === 'audit') {
            items = items.filter(item => ['R', 'N'].includes(item.key));
          }

          return (
            <Dropdown
              trigger={['click']}
              menu={{
                items,
                onClick: ({ key }) => handlePermissionChange(moduleName, record.key, role.key, key),
              }}
            >
              <div
                style={{
                  width: '100%',
                  ...getPermissionStyle(currentPerm),
                  border: `1px solid ${getPermissionStyle(currentPerm).borderColor}`,
                  padding: 6,
                  borderRadius: 6,
                  textAlign: 'center',
                  cursor: 'pointer',
                }}
              >
                {getPermissionLabel(currentPerm)}
              </div>
            </Dropdown>
          );
        }
      }))
    ];

    return (
      <>
        <Space style={{ marginBottom: 12 }}>
          <Text style={{ marginRight: 8 }}>Chú thích:</Text>
          <Tag color="green">Toàn quyền</Tag>
          <Tag color="orange">Chỉnh sửa</Tag>
          <Tag color="blue">Chỉ xem</Tag>
          <Tag>Ẩn</Tag>
        </Space>
        <Table
          dataSource={dataSource}
          columns={columns}
          pagination={false}
          bordered
          size="small"
          sticky
          scroll={{ x: 1000 }}
        />
      </>
    );
  };

  return (
    <Card
      title={<Title level={4}>Quản lý Phân quyền</Title>}
      extra={
        <Space>
          <Button icon={<ReloadOutlined />} onClick={fetchPermissions}>
            Làm mới
          </Button>
          <Button type="primary" icon={<SaveOutlined />} onClick={handleSave} loading={loading}>
            Lưu thay đổi
          </Button>
        </Space>
      }
    >
      <Tabs
        defaultActiveKey={initialTab}
        items={[
          {
            key: 'companies',
            label: 'Quản lý công ty',
            children: renderTable('companies', fields.companies),
          },
          {
            key: 'qlkh',
            label: 'Quản lý Khách hàng',
            children: renderTable('qlkh', fields.qlkh),
          },
          {
            key: 'qlcp',
            label: 'Quản lý Chi phí',
            children: renderTable('qlcp', fields.qlcp),
          },
          {
            key: 'users',
            label: 'Quản lý Nhân viên',
            children: renderTable('users', fields.users),
          },
          {
            key: 'projects',
            label: 'Quản lý dự án',
            children: renderTable('projects', fields.projects),
          },
          {
            key: 'dashboard',
            label: 'Dashboard',
            children: renderTable('dashboard', fields.dashboard),
          },
          {
            key: 'work_dashboard',
            label: 'Dashboard công việc',
            children: renderTable('work_dashboard', fields.work_dashboard),
          },
          {
            key: 'export',
            label: 'Xuất văn bản',
            children: renderTable('export', fields.export),
          },
          {
            key: 'scheduling',
            label: 'Chấm công dự án',
            children: renderTable('scheduling', fields.scheduling),
          },
          {
            key: 'audit',
            label: 'Lịch sử tác động',
            children: renderTable('audit', fields.audit),
          },
          {
            key: 'roles',
            label: 'Quản lý chức danh',
            children: renderTable('roles', fields.roles),
          },
          {
            key: 'permissions',
            label: 'Cấu hình phân quyền',
            children: renderTable('permissions', fields.permissions),
          },
        ]}
      />
    </Card>
  );
};

export default PermissionModule;
