import React, { useState } from 'react';
import { Layout, Menu, Dropdown, Avatar, Space, Badge, Popover, List, Typography, Button, Empty } from 'antd';
import { useNavigate, useLocation } from 'react-router-dom';
import {
  DashboardOutlined,
  UserOutlined,
  ShopOutlined,
  CheckCircleOutlined,
  DollarOutlined,
  TeamOutlined,
  SettingOutlined,
  LogoutOutlined,
  FileTextOutlined,
  SafetyCertificateOutlined,
  BellOutlined,
  MenuUnfoldOutlined,
  MenuFoldOutlined,
  CalendarOutlined,
} from '@ant-design/icons';
import { useAuth } from '../../context/AuthContext';
import { useNotification } from '../../context/NotificationContext';
import './index.css';

const { Header, Sider, Content } = Layout;
const { Text } = Typography;
const { useBreakpoint } = Grid;

const AppLayout = ({ children }) => {
  const screens = useBreakpoint();
  const [collapsed, setCollapsed] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const navigate = useNavigate();
  
  // Close mobile menu when route changes
  useEffect(() => {
    setMobileMenuOpen(false);
  }, [location.pathname]);

  const isMobile = !screens.lg;
  const location = useLocation();
  const { user, logout, canAccessUsersModule, canAccessPermissions, getPermissionLevel, isAdmin } = useAuth();
  const { notifications, unreadCount, markAsRead, markAllAsRead } = useNotification();
  const [visible, setVisible] = useState(false);

  const handleMenuClick = ({ key }) => {
    navigate(key);
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const handleNotificationClick = (item) => {
    if (!item.isRead) {
      markAsRead(item.id);
    }
    // Navigate based on type
    if (item.type === 'CostApproval' && item.relatedId) {
      navigate('/costs', { state: { openCostId: item.relatedId } });
    }
    setVisible(false);
  };

  const notificationContent = (
    <div style={{ width: 350, maxHeight: 400, overflow: 'auto' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', padding: '8px 0', borderBottom: '1px solid #f0f0f0' }}>
        <Text strong>Thông báo</Text>
        {unreadCount > 0 && (
          <Button type="link" size="small" onClick={markAllAsRead}>
            Đánh dấu đã đọc tất cả
          </Button>
        )}
      </div>
      <List
        itemLayout="horizontal"
        dataSource={notifications}
        locale={{ emptyText: <Empty description="Không có thông báo nào" /> }}
        renderItem={(item) => (
          <List.Item 
            className={`notification-item ${!item.isRead ? 'unread' : ''}`}
            onClick={() => handleNotificationClick(item)}
            style={{ 
              cursor: 'pointer', 
              background: item.isRead ? '#fff' : '#e6f7ff',
              padding: '8px',
              borderBottom: '1px solid #f0f0f0'
            }}
          >
            <List.Item.Meta
              title={
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Text strong={!item.isRead}>{item.title}</Text>
                  <Text type="secondary" style={{ fontSize: '12px' }}>
                    {new Date(item.createdAt).toLocaleDateString()}
                  </Text>
                </div>
              }
              description={
                <div>
                  <div style={{ marginBottom: 4 }}>{item.message}</div>
                  <Text type="secondary" style={{ fontSize: '11px' }}>
                    {new Date(item.createdAt).toLocaleTimeString()}
                  </Text>
                </div>
              }
            />
          </List.Item>
        )}
      />
    </div>
  );

  const getMenuItems = () => {
    const items = [
      {
        key: '/dashboard',
        icon: <DashboardOutlined />,
        label: 'Dashboard',
      },
      {
        key: '/customers',
        icon: <UserOutlined />,
        label: 'Quản lý khách hàng',
      },
      {
        key: '/costs',
        icon: <DollarOutlined />,
        label: 'Quản lý chi phí',
      },
      {
        key: '/scheduling',
        icon: <CalendarOutlined />,
        label: 'Chấm công dự án',
      },
    ];

    const exportDocPerm = getPermissionLevel('export', 'export_doc');
    if (exportDocPerm && exportDocPerm !== 'N') {
      items.push({
        key: '/export-word',
        icon: <FileTextOutlined />,
        label: 'Xuất văn bản',
      });
    }

    if (canAccessUsersModule()) {
      items.push({
        key: '/users',
        icon: <TeamOutlined />,
        label: 'Quản lý nhân viên',
      });
    }

    // Chỉ admin và CEO mới thấy menu Phân quyền
    if (canAccessPermissions()) {
      items.push({
        key: '/permissions',
        icon: <SafetyCertificateOutlined />,
        label: 'Phân quyền',
      });
    }

    return items;
  };

  const userMenuItems = [
    {
      key: 'account',
      icon: <SettingOutlined />,
      label: 'Tài khoản của tôi',
      onClick: () => navigate('/my-account'),
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Đăng xuất',
      onClick: handleLogout,
    },
  ];

  const renderSiderContent = (collapsedState) => (
    <>
      <div className="logo">
        <h2 style={{ color: '#1890ff', padding: '16px', margin: 0, whiteSpace: 'nowrap', overflow: 'hidden' }}>
          {collapsedState ? 'QLNS' : 'Quản lý Công ty'}
        </h2>
      </div>
      <Menu
        mode="inline"
        selectedKeys={[location.pathname]}
        items={getMenuItems()}
        onClick={handleMenuClick}
      />
    </>
  );

  return (
    <Layout style={{ minHeight: '100vh' }}>
      {!isMobile ? (
        <Sider
          collapsible
          collapsed={collapsed}
          onCollapse={setCollapsed}
          trigger={null}
          theme="light"
          width={250}
        >
          {renderSiderContent(collapsed)}
        </Sider>
      ) : (
        <Drawer
          placement="left"
          onClose={() => setMobileMenuOpen(false)}
          open={mobileMenuOpen}
          width={250}
          bodyStyle={{ padding: 0 }}
        >
          {renderSiderContent(false)}
        </Drawer>
      )}
      <Layout>
        <Header
          style={{
            background: '#fff',
            padding: isMobile ? '0 12px' : '0 24px',
            boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
          }}
        >
          <div style={{ display: 'flex', alignItems: 'center' }}>
            {React.createElement(isMobile ? MenuUnfoldOutlined : (collapsed ? MenuUnfoldOutlined : MenuFoldOutlined), {
              className: 'trigger',
              onClick: () => isMobile ? setMobileMenuOpen(true) : setCollapsed(!collapsed),
              style: { fontSize: '20px', marginRight: isMobile ? '12px' : '24px', cursor: 'pointer', color: '#1890ff' }
            })}
            <h1 style={{ margin: 0, fontSize: isMobile ? '16px' : '20px', fontWeight: 600 }}>
              {isMobile ? 'QLNS' : 'Hệ thống Quản lý Công ty'}
            </h1>
          </div>
          <Space size={isMobile ? "small" : "large"}>
            <Popover
              content={notificationContent}
              trigger="click"
              visible={visible}
              onVisibleChange={setVisible}
              placement="bottomRight"
              overlayClassName="notification-popover"
            >
              <Badge count={unreadCount} overflowCount={99}>
                <BellOutlined style={{ fontSize: '20px', cursor: 'pointer' }} />
              </Badge>
            </Popover>

            <Space>
              {!isMobile && (
                <span style={{ color: '#666' }}>
                  {user?.fullName || user?.username}
                </span>
              )}
              <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
                <Avatar
                  style={{ backgroundColor: '#1890ff', cursor: 'pointer' }}
                  icon={<UserOutlined />}
                />
              </Dropdown>
            </Space>
          </Space>
        </Header>
        <Content style={{ margin: isMobile ? '12px' : '24px', background: '#fff' }}>
          {children}
        </Content>
      </Layout>
    </Layout>
  );
};

export default AppLayout;

