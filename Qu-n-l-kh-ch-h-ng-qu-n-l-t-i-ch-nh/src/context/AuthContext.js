import React, { createContext, useState, useEffect } from 'react';
import axios from 'axios';

export const AuthContext = createContext();

const TOKEN_KEY = 'authToken';

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [modulePermissions, setModulePermissions] = useState({});

  const setAuthToken = (token) => {
    if (token) {
      axios.defaults.headers.common.Authorization = `Bearer ${token}`;
      window.localStorage.setItem(TOKEN_KEY, token);
    } else {
      delete axios.defaults.headers.common.Authorization;
      window.localStorage.removeItem(TOKEN_KEY);
    }
  };

  const loadModulePermissions = async () => {
    try {
      const modules = ['users', 'dashboard', 'qlkh', 'export'];
      const results = await Promise.all(
        modules.map((module) =>
          axios
            .get('/api/permissions/current', { params: { module } })
            .then((res) => [module, res.data.permissions || {}])
            .catch(() => [module, {}]),
        ),
      );

      const perms = {};
      results.forEach(([module, permissions]) => {
        perms[module] = permissions;
      });
      setModulePermissions(perms);
    } catch (error) {
      setModulePermissions({});
    }
  };

  const checkAuth = async () => {
    setLoading(true);
    try {
      const response = await axios.get('/api/auth/me');
      if (response.data.user) {
        setUser(response.data.user);
        await loadModulePermissions();
      } else {
        setUser(null);
        setModulePermissions({});
      }
    } catch (error) {
      setUser(null);
      setModulePermissions({});
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const token = window.localStorage.getItem(TOKEN_KEY);
    if (token) {
      axios.defaults.headers.common.Authorization = `Bearer ${token}`;
      checkAuth();
    } else {
      setLoading(false);
    }
  }, []);

  const login = async (username, password) => {
    try {
      const response = await axios.post('/api/auth/login', { username, password });
      const { token, user: userData } = response.data || {};
      if (token && userData) {
        setAuthToken(token);
        setUser(userData);
        await loadModulePermissions();
        return { success: true, user: userData };
      }
      return { success: false, message: 'Đăng nhập thất bại' };
    } catch (error) {
      setAuthToken(null);
      console.error('Login error:', error);
      const errorMessage = error.response?.data?.message || error.message || 'Đăng nhập thất bại';
      return { success: false, message: errorMessage };
    }
  };

  const logout = async () => {
    try {
      await axios.post('/api/auth/logout');
    } catch (error) {
    } finally {
      setAuthToken(null);
      setUser(null);
      setModulePermissions({});
    }
    return { success: true };
  };

  const getPermissionLevel = (module, field) => {
    const modulePerms = modulePermissions[module] || {};
    const level = modulePerms[field];
    if (!level) {
      if (user && user.role === 'admin') {
        return 'W';
      }
      return null;
    }
    return level;
  };

  const isAdmin = () => {
    return user && user.role === 'admin';
  };

  const isManager = () => {
    return user && (user.role === 'admin' || user.role === 'manager');
  };

  const canAccessUsersModule = () => {
    if (!user) return false;
    const level = getPermissionLevel('users', 'list');
    if (!level) {
      return isAdmin();
    }
    return level !== 'N';
  };

  const canAccessPermissions = () => {
    return user && (user.role === 'admin' || user.role === 'ceo');
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        modulePermissions,
        login,
        logout,
        isAdmin,
        isManager,
        getPermissionLevel,
        canAccessUsersModule,
        canAccessPermissions,
        checkAuth,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = React.useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
};
