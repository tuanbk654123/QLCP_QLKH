import React from 'react';
import { BrowserRouter } from 'react-router-dom';
import { ConfigProvider } from 'antd';
import viVN from 'antd/locale/vi_VN';
import { IntlProvider } from 'react-intl';
import { AuthProvider } from './context/AuthContext';
import { NotificationProvider } from './context/NotificationContext';
import AppRoutes from './routes';
import './App.css';

const messages = {
  'vi': {
    'app.title': 'Hệ thống Quản lý Công ty',
  },
};

function App() {
  return (
    <IntlProvider locale="vi" messages={messages.vi}>
      <ConfigProvider locale={viVN}>
        <AuthProvider>
          <NotificationProvider>
            <BrowserRouter>
              <AppRoutes />
            </BrowserRouter>
          </NotificationProvider>
        </AuthProvider>
      </ConfigProvider>
    </IntlProvider>
  );
}

export default App;

