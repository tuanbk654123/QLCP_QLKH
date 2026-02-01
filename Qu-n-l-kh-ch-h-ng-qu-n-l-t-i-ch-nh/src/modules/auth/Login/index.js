import React, { useState } from 'react';
import { Form, Input, Button, Card, message } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../../context/AuthContext';
import './index.css';

const Login = () => {
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const onFinish = async (values) => {
    setLoading(true);
    const result = await login(values.username, values.password);
    setLoading(false);

    if (result.success) {
      message.success('Đăng nhập thành công!');
      navigate('/dashboard');
    } else {
      message.error(result.message || 'Đăng nhập thất bại');
    }
  };

  return (
    <div className="login-container">
      <Card className="login-card" title="Đăng nhập hệ thống">
        <Form
          name="login"
          onFinish={onFinish}
          autoComplete="off"
          size="large"
        >
          <Form.Item
            name="username"
            rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập hoặc email!' }]}
          >
            <Input
              prefix={<UserOutlined />}
              placeholder="Tên đăng nhập hoặc Email"
            />
          </Form.Item>

          <Form.Item
            name="password"
            rules={[{ required: true, message: 'Vui lòng nhập mật khẩu!' }]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Mật khẩu"
            />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" block loading={loading}>
              Đăng nhập
            </Button>
          </Form.Item>
        </Form>

        <div className="login-info">
          <p><strong>Thông tin đăng nhập mẫu (Mật khẩu: 123456):</strong></p>
          <p>Admin: <code>admin</code> | CEO: <code>ceo</code></p>
          <p>Quản lý: <code>manager</code> | Kế toán: <code>accountant</code></p>
          <p>Sale: <code>sales</code> | Chuyên viên: <code>executive</code></p>
        </div>
      </Card>
    </div>
  );
};

export default Login;

