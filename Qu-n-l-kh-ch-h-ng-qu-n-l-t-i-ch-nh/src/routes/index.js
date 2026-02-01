import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import ProtectedRoute from '../components/ProtectedRoute';
import AppLayout from '../components/AppLayout';
import Login from '../modules/auth/Login';
import Customers from '../modules/customers';
import Costs from '../modules/costs';
import Dashboard from '../modules/dashboard';
import MyAccount from '../modules/account/MyAccount';
import Users from '../modules/users';
import PermissionModule from '../modules/permissions';
import Contracts from '../modules/contracts';
import ExportWord from '../modules/export-word';
import SchedulingPage from '../modules/scheduling';

const AppRoutes = () => {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        path="/*"
        element={
          <ProtectedRoute>
            <AppLayout>
              <Routes>
                <Route path="/" element={<Navigate to="/dashboard" replace />} />
                <Route path="/dashboard" element={<Dashboard />} />
                <Route path="/customers" element={<Customers />} />
                <Route path="/costs" element={<Costs />} />
                <Route path="/my-account" element={<MyAccount />} />
                <Route path="/users" element={<Users />} />
                <Route
                  path="/permissions"
                  element={
                    <ProtectedRoute requirePermissionsAdmin={true}>
                      <PermissionModule />
                    </ProtectedRoute>
                  }
                />
                <Route path="/contracts" element={<Contracts />} />
                <Route path="/export-word" element={<ExportWord />} />
                <Route path="/scheduling" element={<SchedulingPage />} />
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </AppLayout>
          </ProtectedRoute>
        }
      />
    </Routes>
  );
};

export default AppRoutes;

