import React from 'react';
import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const ProtectedRoute = ({ children, requireAdmin = false, requirePermissionsAdmin = false }) => {
  const { user, loading, canAccessPermissions } = useAuth();

  if (loading) {
    return <div style={{ padding: 24, textAlign: 'center' }}>Đang tải...</div>;
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  if (requireAdmin && user.role !== 'admin') {
    return (
      <div style={{ padding: 24, textAlign: 'center' }}>
        <h2>Bạn không có quyền truy cập trang này</h2>
        <p>Chỉ quản trị viên mới có thể truy cập.</p>
      </div>
    );
  }

  if (requirePermissionsAdmin && !canAccessPermissions()) {
    return (
      <div style={{ padding: 24, textAlign: 'center' }}>
        <h2>Bạn không có quyền truy cập trang Phân quyền</h2>
        <p>Chỉ quản trị viên và Tổng giám đốc mới có thể truy cập.</p>
      </div>
    );
  }

  return children;
};

export default ProtectedRoute;

