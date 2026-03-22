import React, { useMemo } from 'react';
import { useAuth } from '../../context/AuthContext';
import Customers from './index';
import CustomersStandard from '../customers-standard';

const CustomersEntry = () => {
  const { companies, activeCompanyId } = useAuth();

  const activeCompanyCode = useMemo(() => {
    const c = (companies || []).find((x) => x.id === activeCompanyId);
    return c?.code || null;
  }, [companies, activeCompanyId]);

  if (activeCompanyCode === 'SHTT') {
    return <Customers />;
  }

  return <CustomersStandard />;
};

export default CustomersEntry;

