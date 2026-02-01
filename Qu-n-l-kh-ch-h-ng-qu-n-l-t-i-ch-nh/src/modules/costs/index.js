import React, { useEffect, useState, useCallback } from 'react';
import {
  Table,
  Input,
  Button,
  Space,
  Tag,
  Popconfirm,
  Select,
  message,
} from 'antd';
import {
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import { useAuth } from '../../context/AuthContext';
import { useNotification } from '../../context/NotificationContext';
import { handleApiError } from '../../utils/errorHelper';
import { useLocation } from 'react-router-dom';
import './index.css';
import CostFormModal from './CostFormModal';

const { Option } = Select;

const Costs = () => {
  const location = useLocation();
  const { user } = useAuth();
  const { refreshNotifications } = useNotification();
  
  const [tableParams, setTableParams] = useState({
    pagination: {
      current: 1,
      pageSize: 10,
      showSizeChanger: true,
    },
  });

  const [costs, setCosts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingCost, setEditingCost] = useState(null);
  const [isNotificationView, setIsNotificationView] = useState(false);

  const [fieldPermissions, setFieldPermissions] = useState({});
  const [users, setUsers] = useState([]);

  useEffect(() => {
    // Fetch users for notification dropdown (passed to modal)
    axios.get('/api/users')
      .then(res => {
        if (res.data && res.data.users) {
          setUsers(res.data.users);
        }
      })
      .catch(err => console.error('Error fetching users:', err));
  }, []);

  // Handle opening from notification
  useEffect(() => {
    if (location.state?.openCostId) {
      const costId = location.state.openCostId;
      axios.get(`/api/costs/${costId}`)
        .then(response => {
           handleEdit(response.data, true);
           window.history.replaceState({}, document.title);
        })
        .catch(error => {
          console.error(error);
          message.error('Không thể tải thông tin phiếu chi');
        });
    }
  }, [location.state]);

  const mapFieldToPermissionKey = (field) => {
    const mapping = {
      adjustmentReason: 'adjustReason',
    };
    return mapping[field] || field;
  };

  const canReadField = (field) => {
    const key = mapFieldToPermissionKey(field);
    const level = fieldPermissions[key];
    return level && level !== 'N';
  };

  const fetchCosts = useCallback(async () => {
    setLoading(true);
    try {
      const { current, pageSize } = tableParams.pagination;
      const params = {
        search,
        page: current,
        limit: pageSize,
        sortField: tableParams.sortField,
        sortOrder: tableParams.sortOrder,
        ...tableParams.filters,
      };

      const response = await axios.get('/api/costs', { params });
      setCosts(response.data.costs);
      
      setTableParams((prev) => ({
        ...prev,
        pagination: {
          ...prev.pagination,
          total: response.data.costCount,
        },
      }));
    } catch (error) {
      handleApiError(error, 'Lỗi khi tải dữ liệu chi phí');
    } finally {
      setLoading(false);
    }
  }, [search, tableParams.pagination.current, tableParams.pagination.pageSize, tableParams.sortField, tableParams.sortOrder, tableParams.filters]);

  const fetchPermissions = useCallback(async () => {
    try {
      const response = await axios.get('/api/permissions/current', {
        params: { module: 'qlcp' },
      });
      setFieldPermissions(response.data.permissions || {});
    } catch (error) {
      setFieldPermissions({});
    }
  }, []);

  useEffect(() => {
    fetchPermissions();
    fetchCosts();
  }, [fetchCosts, fetchPermissions]);

  const handleTableChange = (pagination, filters, sorter) => {
    const newFilters = {};
    Object.keys(filters).forEach((key) => {
      if (filters[key] && filters[key].length > 0) {
        newFilters[key] = filters[key][0];
      }
    });

    setTableParams({
      pagination,
      filters: newFilters,
      sortField: sorter.field,
      sortOrder: sorter.order,
    });
  };

  const getColumnSearchProps = (dataIndex) => ({
    filterDropdown: ({ setSelectedKeys, selectedKeys, confirm, clearFilters }) => (
      <div style={{ padding: 8 }} onKeyDown={(e) => e.stopPropagation()}>
        <Input
          placeholder={`Tìm kiếm ${dataIndex}`}
          value={selectedKeys[0]}
          onChange={(e) => setSelectedKeys(e.target.value ? [e.target.value] : [])}
          onPressEnter={() => confirm()}
          style={{ marginBottom: 8, display: 'block' }}
        />
        <Space>
          <Button
            type="primary"
            onClick={() => confirm()}
            icon={<SearchOutlined />}
            size="small"
            style={{ width: 90 }}
          >
            Tìm kiếm
          </Button>
          <Button
            onClick={() => {
              clearFilters();
              confirm();
            }}
            size="small"
            style={{ width: 90 }}
          >
            Đặt lại
          </Button>
        </Space>
      </div>
    ),
    filterIcon: (filtered) => (
      <SearchOutlined style={{ color: filtered ? '#1890ff' : undefined }} />
    ),
  });

  const handleAdd = () => {
    setEditingCost(null);
    setIsNotificationView(false);
    setIsModalVisible(true);
  };

  const handleEdit = (record, fromNotification = false) => {
    setEditingCost(record);
    setIsNotificationView(fromNotification);
    setIsModalVisible(true);
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`/api/costs/${id}`);
      message.success('Xóa phiếu chi thành công');
      fetchCosts();
    } catch (error) {
      handleApiError(error, 'Lỗi khi xóa phiếu chi');
    }
  };

  const handleSuccess = () => {
      setIsModalVisible(false);
      fetchCosts();
      refreshNotifications();
  };

  const formatCurrency = (amount) => {
    if (!amount && amount !== 0) return '';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const getStatusColor = (status) => {
    const colors = {
      'Đã thanh toán': 'green',
      'Đợi duyệt': 'orange',
      'Quản lý duyệt': 'blue',
      'Giám đốc duyệt': 'cyan',
      'Huỷ': 'red',
      'Thanh toán 1 phần': 'purple',
    };
    return colors[status] || 'default';
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 80,
      fixed: 'left',
      sorter: true,
      ...getColumnSearchProps('id'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'paymentStatus',
      key: 'paymentStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('paymentStatus'),
      render: (status) => (
        <Tag color={getStatusColor(status)}>{status}</Tag>
      ),
      hidden: !canReadField('paymentStatus'),
    },
    {
      title: 'Nội dung',
      dataIndex: 'content',
      key: 'content',
      width: 200,
      sorter: true,
      ...getColumnSearchProps('content'),
      hidden: !canReadField('content'),
    },
    {
      title: 'Người đề nghị',
      dataIndex: 'requester',
      key: 'requester',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('requester'),
      hidden: !canReadField('requester'),
    },
    {
      title: 'Bộ phận',
      dataIndex: 'department',
      key: 'department',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('department'),
      hidden: !canReadField('department'),
    },
    {
      title: 'Ngày đề nghị',
      dataIndex: 'requestDate',
      key: 'requestDate',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('requestDate'),
      hidden: !canReadField('requestDate'),
    },
    {
      title: 'Mã dự án',
      dataIndex: 'projectCode',
      key: 'projectCode',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('projectCode'),
      hidden: !canReadField('projectCode'),
    },
    {
      title: 'Loại giao dịch',
      dataIndex: 'transactionType',
      key: 'transactionType',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('transactionType'),
      render: (type) => (
        <Tag color={type === 'Thu' || type === 'Hoàn ứng' ? 'blue' : 'volcano'}>
          {type}
        </Tag>
      ),
      hidden: !canReadField('transactionType'),
    },
    {
      title: 'Đối tượng',
      dataIndex: 'transactionObject',
      key: 'transactionObject',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('transactionObject'),
      hidden: !canReadField('transactionObject'),
    },
    {
      title: 'Mô tả',
      dataIndex: 'description',
      key: 'description',
      width: 200,
      sorter: true,
      ...getColumnSearchProps('description'),
      hidden: !canReadField('description'),
    },
    {
      title: 'Số tiền trước thuế',
      dataIndex: 'amountBeforeTax',
      key: 'amountBeforeTax',
      width: 150,
      align: 'right',
      sorter: true,
      ...getColumnSearchProps('amountBeforeTax'),
      render: (amount) => formatCurrency(amount),
      hidden: !canReadField('amountBeforeTax'),
    },
    {
      title: 'Thuế suất',
      dataIndex: 'taxRate',
      key: 'taxRate',
      width: 100,
      sorter: true,
      ...getColumnSearchProps('taxRate'),
      hidden: !canReadField('taxRate'),
    },
    {
      title: 'Tổng tiền',
      dataIndex: 'totalAmount',
      key: 'totalAmount',
      width: 150,
      align: 'right',
      sorter: true,
      ...getColumnSearchProps('totalAmount'),
      render: (amount) => <b>{formatCurrency(amount)}</b>,
      hidden: !canReadField('totalAmount'),
    },
    {
      title: 'PTTT',
      dataIndex: 'paymentMethod',
      key: 'paymentMethod',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('paymentMethod'),
      hidden: !canReadField('paymentMethod'),
    },
    {
      title: 'Ngân hàng',
      dataIndex: 'bank',
      key: 'bank',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('bank'),
      hidden: !canReadField('bank'),
    },
    {
      title: 'Số tài khoản',
      dataIndex: 'accountNumber',
      key: 'accountNumber',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('accountNumber'),
      hidden: !canReadField('accountNumber'),
    },
    {
      title: 'Loại chứng từ',
      dataIndex: 'voucherType',
      key: 'voucherType',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('voucherType'),
      hidden: !canReadField('voucherType'),
    },
    {
      title: 'Số chứng từ',
      dataIndex: 'voucherNumber',
      key: 'voucherNumber',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('voucherNumber'),
      hidden: !canReadField('voucherNumber'),
    },
    {
      title: 'Ngày chứng từ',
      dataIndex: 'voucherDate',
      key: 'voucherDate',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('voucherDate'),
      hidden: !canReadField('voucherDate'),
    },
    {
      title: 'Ngày giao dịch',
      dataIndex: 'transactionDate',
      key: 'transactionDate',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('transactionDate'),
      hidden: !canReadField('transactionDate'),
    },

    {
      title: 'Lý do từ chối',
      dataIndex: 'rejectionReason',
      key: 'rejectionReason',
      width: 200,
      sorter: true,
      ...getColumnSearchProps('rejectionReason'),
      hidden: !canReadField('rejectionReason'),
    },
    {
      title: 'Ghi chú',
      dataIndex: 'note',
      key: 'note',
      width: 200,
      sorter: true,
      ...getColumnSearchProps('note'),
      hidden: !canReadField('note'),
    },
    {
      title: 'Thao tác',
      key: 'action',
      width: 100,
      fixed: 'right',
      render: (_, record) => (
        <Space size="middle">
          <Button
            type="text"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record, false)}
          />
          <Popconfirm
            title="Bạn có chắc chắn muốn xóa?"
            onConfirm={() => handleDelete(record.id)}
            okText="Có"
            cancelText="Không"
          >
            <Button type="text" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const visibleColumns = columns.filter((col) => !col.hidden);

  return (
    <div className="costs-container" style={{ padding: 24 }}>
      <div className="costs-header" style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between' }}>
        <h2>Quản lý chi phí</h2>
        <Space>
          <Input
            placeholder="Tìm kiếm nội dung, người đề nghị..."
            prefix={<SearchOutlined />}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setTableParams(prev => ({ ...prev, pagination: { ...prev.pagination, current: 1 } }));
            }}
            style={{ width: 250 }}
          />
          <Button type="primary" icon={<PlusOutlined />} onClick={handleAdd}>
            Thêm mới
          </Button>
        </Space>
      </div>

      <Table
        columns={visibleColumns}
        dataSource={costs}
        loading={loading}
        rowKey="id"
        pagination={tableParams.pagination}
        onChange={handleTableChange}
        scroll={{ x: 'max-content', y: 'calc(100vh - 300px)' }}
        bordered
      />

      <CostFormModal
        visible={isModalVisible}
        onCancel={() => setIsModalVisible(false)}
        onSuccess={handleSuccess}
        editingCost={editingCost}
        fieldPermissions={fieldPermissions}
        user={user}
        users={users} // Pass users to modal
        isNotificationView={isNotificationView}
      />
    </div>
  );
};

export default Costs;
