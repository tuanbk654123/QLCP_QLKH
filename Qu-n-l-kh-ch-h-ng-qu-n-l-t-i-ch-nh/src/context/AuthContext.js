import React, { createContext, useState, useEffect } from 'react';
import axios from 'axios';

export const AuthContext = createContext();

const TOKEN_KEY = 'authToken';

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);
  const [modulePermissions, setModulePermissions] = useState({});
  const [companies, setCompanies] = useState([]);
  const [activeCompanyId, setActiveCompanyId] = useState(null);
  const [activeCompanyScope, setActiveCompanyScope] = useState('single');

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
      const modules = ['users', 'dashboard', 'qlkh', 'qlcp', 'export', 'scheduling', 'audit'];
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

  const loadCompanies = async () => {
    try {
      const res = await axios.get('/api/auth/companies');
      setCompanies(res.data.items || []);
      setActiveCompanyId(res.data.activeCompanyId || null);
      setActiveCompanyScope(res.data.activeScope || 'single');
    } catch (error) {
      setCompanies([]);
      setActiveCompanyId(null);
      setActiveCompanyScope('single');
    }
  };

  const checkAuth = async () => {
    setLoading(true);
    try {
      const response = await axios.get('/api/auth/me');
      if (response.data.user) {
        setUser(response.data.user);
        await loadModulePermissions();
        await loadCompanies();
      } else {
        setUser(null);
        setModulePermissions({});
        setCompanies([]);
        setActiveCompanyId(null);
        setActiveCompanyScope('single');
      }
    } catch (error) {
      setUser(null);
      setModulePermissions({});
      setCompanies([]);
      setActiveCompanyId(null);
      setActiveCompanyScope('single');
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
        await loadCompanies();
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

  const switchCompany = async (companyId) => {
    if (!companyId) return { success: false };
    try {
      const res = await axios.post('/api/auth/switch-company', { companyId });
      const { token, user: userData } = res.data || {};
      if (token && userData) {
        setAuthToken(token);
        setUser(userData);
        setActiveCompanyId(userData.companyId || null);
        setActiveCompanyScope(userData.companyScope || 'single');
        window.location.reload();
        return { success: true };
      }
      return { success: false };
    } catch (error) {
      const errorMessage = error.response?.data?.message || error.message || 'Chuyển công ty thất bại';
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
      setCompanies([]);
      setActiveCompanyId(null);
      setActiveCompanyScope('single');
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
    return user && (user.role === 'admin' || user.role === 'ceo' || user.role === 'assistant_ceo');
  };

  const canAccessAuditLogs = () => {
    if (!user) return false;
    if (isAdmin()) return true;

    const level = getPermissionLevel('audit', 'view');
    return level && level !== 'N';
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        modulePermissions,
        companies,
        activeCompanyId,
        activeCompanyScope,
        login,
        logout,
        switchCompany,
        isAdmin,
        isManager,
        getPermissionLevel,
        canAccessUsersModule,
        canAccessPermissions,
        canAccessAuditLogs,
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
