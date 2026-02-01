import React, { useEffect, useState } from 'react';
import { Card, Form, Input, Button, message, Avatar, Tag } from 'antd';
import { UserOutlined, SaveOutlined } from '@ant-design/icons';
import { useAuth } from '../../../context/AuthContext';
import axios from 'axios';
import './index.css';

const MyAccount = () => {
  const { user, checkAuth } = useAuth();
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user) {
      form.setFieldsValue({
        fullName: user.fullName,
        email: user.email,
        phone: user.phone,
        address: user.address,
      });
    }
  }, [user, form]);

  const handleSubmit = async (values) => {
    if (!user) return;

    setLoading(true);
    try {
      await axios.put(`/api/users/${user.id}`, {
        ...user,
        ...values,
      });
      message.success('Cập nhật thông tin thành công!');
      await checkAuth(); // Refresh user data
    } catch (error) {
      message.error('Cập nhật thông tin thất bại');
    } finally {
      setLoading(false);
    }
  };

  const getRoleText = (role) => {
    const roles = {
      admin: 'Admin',
      marketing_sales: 'Marketing/Sales',
      ip_executive: 'IP Executive',
      ip_manager: 'IP Manager',
      director: 'Giám đốc',
      ceo: 'Tổng giám đốc',
      accountant: 'Kế toán',
    };
    return roles[role] || role;
  };

  const getRoleColor = (role) => {
    const colors = {
      admin: 'gold',
      marketing_sales: 'purple',
      ip_executive: 'geekblue',
      ip_manager: 'blue',
      director: 'red',
      ceo: 'volcano',
      accountant: 'green',
    };
    return colors[role] || 'default';
  };

  if (!user) {
    return <div>Đang tải...</div>;
  }

  return (
    <div className="my-account-container">
      <h2>Thông tin tài khoản</h2>
      
      <Card className="account-card">
        <div className="account-header">
          <Avatar size={80} icon={<UserOutlined />} />
          <div className="account-info">
            <h3>{user.fullName}</h3>
            <Tag color={getRoleColor(user.role)}>{getRoleText(user.role)}</Tag>
            <p className="account-email">{user.email}</p>
          </div>
        </div>

        <Form
          form={form}
          layout="vertical"
          scrollToFirstError
          onFinish={handleSubmit}
          className="account-form"
        >
          <Form.Item
            label="Họ và tên"
            name="fullName"
            rules={[{ required: true, message: 'Vui lòng nhập họ và tên' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Email"
            name="email"
            rules={[
              { required: true, message: 'Vui lòng nhập email' },
              { type: 'email', message: 'Email không hợp lệ' },
            ]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Số điện thoại"
            name="phone"
            rules={[{ required: true, message: 'Vui lòng nhập số điện thoại' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            label="Địa chỉ"
            name="address"
          >
            <Input.TextArea rows={3} />
          </Form.Item>

          <div className="account-details">
            <div className="detail-item">
              <strong>Phòng ban:</strong> {user.department || '-'}
            </div>
            <div className="detail-item">
              <strong>Chức vụ:</strong> {user.position || '-'}
            </div>
            <div className="detail-item">
              <strong>Ngày vào làm:</strong> {user.joinDate || '-'}
            </div>
            <div className="detail-item">
              <strong>Trạng thái:</strong>{' '}
              <Tag color={user.status === 'active' ? 'green' : 'red'}>
                {user.status === 'active' ? 'Hoạt động' : 'Không hoạt động'}
              </Tag>
            </div>
          </div>

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              icon={<SaveOutlined />}
              loading={loading}
              size="large"
            >
              Lưu thay đổi
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
};

export default MyAccount;

