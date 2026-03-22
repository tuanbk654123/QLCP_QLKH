import React, { useState } from 'react';
import { Form, Input, Button, message, Typography } from 'antd';
import { UserOutlined, LockOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../../context/AuthContext';
import './index.css';

const { Title, Text } = Typography;

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
      <div className="login-left">
        <div className="login-left-content">
          <Title level={2} className="login-title">
            Đăng nhập
          </Title>

          <Form name="login" onFinish={onFinish} autoComplete="off" size="large" layout="vertical">
            <Form.Item
              label="Email hoặc tên đăng nhập"
              name="username"
              rules={[{ required: true, message: 'Vui lòng nhập email hoặc tên đăng nhập!' }]}
            >
              <Input prefix={<UserOutlined />} placeholder="example@gmail.com" autoFocus />
            </Form.Item>

            <Form.Item label="Mật khẩu" name="password" rules={[{ required: true, message: 'Vui lòng nhập mật khẩu!' }]}>
              <Input.Password prefix={<LockOutlined />} placeholder="Nhập mật khẩu" />
            </Form.Item>

            <Button type="primary" htmlType="submit" block loading={loading} className="login-submit">
              Đăng nhập
            </Button>
          </Form>
        </div>

        <div className="login-footer">
          <Text type="secondary">© 2026</Text>
        </div>
      </div>

      <div className="login-right" aria-hidden="true">
        <div className="login-blob login-blob-1" />
        <div className="login-blob login-blob-2" />
      </div>
    </div>
  );
};

export default Login;
