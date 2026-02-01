import React, { useEffect, useState, useCallback } from 'react';
import {
  Table,
  Input,
  Button,
  Space,
  Tag,
  Modal,
  Form,
  message,
  Popconfirm,
  Select,
  Tabs,
  Row,
  Col,
  DatePicker,
  Upload,
} from 'antd';
import {
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import dayjs from 'dayjs';
import { useAuth } from '../../context/AuthContext';
import { handleApiError } from '../../utils/errorHelper';
import './index.css';

const Customers = () => {
  const { isAdmin } = useAuth();
  const [customers, setCustomers] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [tableParams, setTableParams] = useState({
    pagination: {
      current: 1,
      pageSize: 10,
      showSizeChanger: true,
    },
  });
  const [total, setTotal] = useState(0);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState(null);
  const [form] = Form.useForm();
  const [activeTab, setActiveTab] = useState('1');

  const onFinishFailed = ({ errorFields }) => {
    if (errorFields.length > 0) {
      const firstErrorField = errorFields[0].name[0];
      const fieldToTab = {
        name: '1', code: '1', taxCode: '1', address: '1', city: '1', district: '1', ward: '1', 
        phone: '1', email: '1', contactPerson: '1', contactPhone: '1', contactEmail: '1',
        representativeName: '1', representativePosition: '1', idNumber: '1',
        businessNeeds: '2', potentialLevel: '2', priority: '2', sourceClassification: '2', nsnnSource: '2',
        brandName: '3', productsServices: '3', ipGroup: '3', owner: '3', protectionTerritory: '3', authorization: '3',
        filingStatus: '4', filingDate: '4', applicationCode: '4', issueDate: '4', expiryDate: '4', applicationReviewStatus: '4', processingDeadline: '4',
        renewalCycle: '5', renewalDate: '5', reminderDate: '5', reminderStatus: '5',
        consultingStatus: '6', contractStatus: '6', contractNumber: '6', contractValue: '6', stateFee: '6', additionalFee: '6', startDate: '6', endDate: '6', implementationDays: '6',
        createdBy: '7', updatedBy: '7', updatedAt: '7', documentLink: '7'
      };
      
      const tabKey = fieldToTab[firstErrorField];
      if (tabKey) {
        setActiveTab(tabKey);
        setTimeout(() => {
          form.scrollToField(firstErrorField, { behavior: 'smooth', block: 'center' });
        }, 100);
      }
    }
  };

  const [fieldPermissions, setFieldPermissions] = useState({});

  const { Option } = Select;
  const { TextArea } = Input;

  const canReadField = (field) => {
    if (isAdmin && isAdmin()) return true;
    const level = fieldPermissions[field];
    return level && level !== 'N';
  };

  const canEditField = (field) => {
    if (isAdmin && isAdmin()) return true;
    const level = fieldPermissions[field];
    return level === 'W' || level === 'A';
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

  const fetchCustomers = useCallback(async () => {
    setLoading(true);
    try {
      // Clean filters
      const cleanFilters = {};
      if (tableParams.filters) {
        Object.keys(tableParams.filters).forEach(key => {
          if (tableParams.filters[key] && tableParams.filters[key].length > 0) {
            cleanFilters[key] = tableParams.filters[key][0];
          }
        });
      }

      const response = await axios.get('/api/customers', {
        params: { 
          search, 
          page: tableParams.pagination?.current || 1, 
          pageSize: tableParams.pagination?.pageSize || 10,
          sortField: tableParams.sortField,
          sortOrder: tableParams.sortOrder,
          ...cleanFilters
        },
      });
      setCustomers(response.data.customers);
      setTotal(response.data.customerCount);
      setTableParams((prev) => ({
        ...prev,
        pagination: {
          ...prev.pagination,
          total: response.data.customerCount,
        },
      }));
    } catch (error) {
      handleApiError(error, 'Lỗi khi tải dữ liệu khách hàng');
    } finally {
      setLoading(false);
    }
  }, [
    search, 
    tableParams.pagination?.current, 
    tableParams.pagination?.pageSize, 
    tableParams.sortField, 
    tableParams.sortOrder, 
    tableParams.filters
  ]);

  const fetchPermissions = useCallback(async () => {
    try {
      const response = await axios.get('/api/permissions/current', {
        params: { module: 'qlkh' },
      });
      setFieldPermissions(response.data.permissions || {});
    } catch (error) {
      setFieldPermissions({});
    }
  }, []);

  useEffect(() => {
    fetchPermissions();
    fetchCustomers();
  }, [fetchCustomers, fetchPermissions]);

  const handleTableChange = (pagination, filters, sorter) => {
    setTableParams({
      pagination,
      filters,
      sortOrder: Array.isArray(sorter) ? undefined : sorter.order,
      sortField: Array.isArray(sorter) ? undefined : sorter.field,
    });
  };

  const handleAdd = () => {
    setEditingCustomer(null);
    form.resetFields();
    setActiveTab('1');
    setIsModalVisible(true);
  };

  const handleEdit = (record) => {
    setEditingCustomer(record);
    setActiveTab('1');
    const formattedRecord = {
      ...record,
      startDate: record.startDate ? dayjs(record.startDate) : null,
      endDate: record.endDate ? dayjs(record.endDate) : null,
      filingDate: record.filingDate ? dayjs(record.filingDate) : null,
      issueDate: record.issueDate ? dayjs(record.issueDate) : null,
      expiryDate: record.expiryDate ? dayjs(record.expiryDate) : null,
      processingDeadline: record.processingDeadline ? dayjs(record.processingDeadline) : null,
      renewalDate: record.renewalDate ? dayjs(record.renewalDate) : null,
      reminderDate: record.reminderDate ? dayjs(record.reminderDate) : null,
      updatedAt: record.updatedAt ? dayjs(record.updatedAt) : null,
    };
    form.setFieldsValue(formattedRecord);
    setIsModalVisible(true);
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`/api/customers/${id}`);
      message.success('Xóa khách hàng thành công');
      fetchCustomers();
    } catch (error) {
      handleApiError(error, 'Lỗi khi xóa khách hàng');
    }
  };

  const handleSubmit = async (values) => {
    try {
      const formattedValues = {
        ...values,
        startDate: values.startDate ? values.startDate.format('YYYY-MM-DD') : null,
        endDate: values.endDate ? values.endDate.format('YYYY-MM-DD') : null,
        filingDate: values.filingDate ? values.filingDate.format('YYYY-MM-DD') : null,
        issueDate: values.issueDate ? values.issueDate.format('YYYY-MM-DD') : null,
        expiryDate: values.expiryDate ? values.expiryDate.format('YYYY-MM-DD') : null,
        processingDeadline: values.processingDeadline ? values.processingDeadline.format('YYYY-MM-DD') : null,
        renewalDate: values.renewalDate ? values.renewalDate.format('YYYY-MM-DD') : null,
        reminderDate: values.reminderDate ? values.reminderDate.format('YYYY-MM-DD') : null,
        updatedAt: values.updatedAt ? values.updatedAt.format('YYYY-MM-DD') : null,
      };

      // Remove export replacements from payload
      delete formattedValues.replacements;

      if (editingCustomer) {
        await axios.put(`/api/customers/${editingCustomer.id}`, formattedValues);
        message.success('Cập nhật khách hàng thành công');
      } else {
        await axios.post('/api/customers', formattedValues);
        message.success('Thêm khách hàng thành công');
      }
      setIsModalVisible(false);
      form.resetFields();
      fetchCustomers();
    } catch (error) {
      handleApiError(error, 'Lỗi khi lưu khách hàng');
    }
  };

  const formatCurrency = (value) => {
    if (!value) return '';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 60,
      fixed: 'left',
      sorter: true,
      ...getColumnSearchProps('id'),
    },
    {
      title: 'Tên khách hàng',
      dataIndex: 'name',
      key: 'name',
      width: 180,
      fixed: 'left',
      sorter: true,
      ...getColumnSearchProps('name'),
      hidden: !canReadField('name'),
    },
    {
      title: 'Mã số thuế',
      dataIndex: 'taxCode',
      key: 'taxCode',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('taxCode'),
      hidden: !canReadField('taxCode'),
    },
    {
      title: 'Công ty',
      dataIndex: 'company',
      key: 'company',
      width: 180,
      sorter: true,
      ...getColumnSearchProps('company'),
      hidden: !canReadField('company'),
    },
    {
      title: 'Địa chỉ',
      dataIndex: 'address',
      key: 'address',
      width: 200,
      sorter: true,
      ...getColumnSearchProps('address'),
      hidden: !canReadField('address'),
    },
    {
      title: 'Người đại diện',
      dataIndex: 'representativeName',
      key: 'representativeName',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('representativeName'),
      hidden: !canReadField('representativeName'),
    },
    {
      title: 'Chức vụ',
      dataIndex: 'representativePosition',
      key: 'representativePosition',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('representativePosition'),
      hidden: !canReadField('representativePosition'),
    },
    {
      title: 'SĐT Đại diện',
      dataIndex: 'representativePhone',
      key: 'representativePhone',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('representativePhone'),
      hidden: !canReadField('representativePhone'),
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      width: 180,
      sorter: true,
      ...getColumnSearchProps('email'),
      hidden: !canReadField('email'),
    },
    {
      title: 'Số điện thoại',
      dataIndex: 'phone',
      key: 'phone',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('phone'),
      hidden: !canReadField('phone'),
    },
    {
      title: 'Nhu cầu',
      dataIndex: 'businessNeeds',
      key: 'businessNeeds',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('businessNeeds'),
      hidden: !canReadField('businessNeeds'),
    },
    {
      title: 'Quy mô',
      dataIndex: 'businessScale',
      key: 'businessScale',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('businessScale'),
      hidden: !canReadField('businessScale'),
    },
    {
      title: 'Ngành nghề',
      dataIndex: 'businessIndustry',
      key: 'businessIndustry',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('businessIndustry'),
      hidden: !canReadField('businessIndustry'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('status'),
      render: (status) => (
        <Tag color={status === 'active' ? 'green' : 'red'}>
          {status === 'active' ? 'Hoạt động' : 'Không hoạt động'}
        </Tag>
      ),
    },
    {
      title: 'Tiềm năng',
      dataIndex: 'potentialLevel',
      key: 'potentialLevel',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('potentialLevel'),
      hidden: !canReadField('potentialLevel'),
    },
    {
      title: 'Ưu tiên',
      dataIndex: 'priority',
      key: 'priority',
      width: 100,
      sorter: true,
      ...getColumnSearchProps('priority'),
      hidden: !canReadField('priority'),
    },
    {
      title: 'Nguồn',
      dataIndex: 'sourceClassification',
      key: 'sourceClassification',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('sourceClassification'),
      hidden: !canReadField('sourceClassification'),
    },
    {
      title: 'Nguồn NSNN',
      dataIndex: 'nsnnSource',
      key: 'nsnnSource',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('nsnnSource'),
      hidden: !canReadField('nsnnSource'),
    },
    {
      title: 'Tình trạng tư vấn',
      dataIndex: 'consultingStatus',
      key: 'consultingStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('consultingStatus'),
      hidden: !canReadField('consultingStatus'),
    },
    {
      title: 'Nhóm SHTT',
      dataIndex: 'ipGroup',
      key: 'ipGroup',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('ipGroup'),
      hidden: !canReadField('ipGroup'),
    },
    {
      title: 'SP/Dịch vụ',
      dataIndex: 'productsServices',
      key: 'productsServices',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('productsServices'),
      hidden: !canReadField('productsServices'),
    },
    {
      title: 'Bản quyền',
      dataIndex: 'copyrightStatus',
      key: 'copyrightStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('copyrightStatus'),
      hidden: !canReadField('copyrightStatus'),
    },
    {
      title: 'Nhãn hiệu',
      dataIndex: 'trademarkStatus',
      key: 'trademarkStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('trademarkStatus'),
      hidden: !canReadField('trademarkStatus'),
    },
    {
      title: 'Sáng chế',
      dataIndex: 'patentStatus',
      key: 'patentStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('patentStatus'),
      hidden: !canReadField('patentStatus'),
    },
    {
      title: 'KDCN',
      dataIndex: 'industrialDesign',
      key: 'industrialDesign',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('industrialDesign'),
      hidden: !canReadField('industrialDesign'),
    },
    {
      title: 'Hợp đồng',
      dataIndex: 'contractStatus',
      key: 'contractStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('contractStatus'),
      hidden: !canReadField('contractStatus'),
    },
    {
      title: 'Giá trị HĐ',
      dataIndex: 'contractValue',
      key: 'contractValue',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('contractValue'),
      render: (val) => formatCurrency(val),
      hidden: !canReadField('contractValue'),
    },
    {
      title: 'Đã thanh toán',
      dataIndex: 'contractPaid',
      key: 'contractPaid',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('contractPaid'),
      hidden: !canReadField('contractPaid'),
    },
    {
      title: 'Tổng đơn',
      dataIndex: 'totalOrders',
      key: 'totalOrders',
      width: 100,
      sorter: true,
      ...getColumnSearchProps('totalOrders'),
      hidden: !canReadField('totalOrders'),
    },
    {
      title: 'Tổng doanh thu',
      dataIndex: 'totalRevenue',
      key: 'totalRevenue',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('totalRevenue'),
      render: (val) => formatCurrency(val),
      hidden: !canReadField('totalRevenue'),
    },
    {
      title: 'Ngày bắt đầu',
      dataIndex: 'startDate',
      key: 'startDate',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('startDate'),
      hidden: !canReadField('startDate'),
    },
    {
      title: 'Ngày kết thúc',
      dataIndex: 'endDate',
      key: 'endDate',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('endDate'),
      hidden: !canReadField('endDate'),
    },
    {
      title: 'Số ngày TK',
      dataIndex: 'implementationDays',
      key: 'implementationDays',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('implementationDays'),
      hidden: !canReadField('implementationDays'),
    },
    {
      title: 'Ngày tham gia',
      dataIndex: 'joinDate',
      key: 'joinDate',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('joinDate'),
      hidden: !canReadField('joinDate'),
    },
    {
      title: 'Tình trạng nộp đơn',
      dataIndex: 'filingStatus',
      key: 'filingStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('filingStatus'),
      hidden: !canReadField('filingStatus'),
    },
    {
      title: 'Link hồ sơ',
      dataIndex: 'documentLink',
      key: 'documentLink',
      width: 150,
      sorter: true,
      render: (text) => text ? <a href={text} target="_blank" rel="noopener noreferrer">Xem</a> : '',
      hidden: !canReadField('documentLink'),
    },
    {
      title: 'Uỷ quyền',
      dataIndex: 'authorization',
      key: 'authorization',
      width: 120,
      sorter: true,
      ...getColumnSearchProps('authorization'),
      hidden: !canReadField('authorization'),
    },
    {
      title: 'Xét duyệt đơn',
      dataIndex: 'applicationReviewStatus',
      key: 'applicationReviewStatus',
      width: 150,
      sorter: true,
      ...getColumnSearchProps('applicationReviewStatus'),
      hidden: !canReadField('applicationReviewStatus'),
    },
    {
      title: 'Ghi chú',
      dataIndex: 'notes',
      key: 'notes',
      width: 200,
      ...getColumnSearchProps('notes'),
      hidden: !canReadField('notes'),
    },
    {
      title: 'Thao tác',
      key: 'action',
      width: 150,
      fixed: 'right',
      render: (_, record) => (
        <Space size="middle">
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => handleEdit(record)}
          >
            Sửa
          </Button>
          <Popconfirm
            title="Bạn có chắc chắn muốn xóa?"
            onConfirm={() => handleDelete(record.id)}
            okText="Có"
            cancelText="Không"
          >
            <Button type="link" danger icon={<DeleteOutlined />}>
              Xóa
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const tabItems = [
    {
      key: '1',
      label: 'I. Nhóm thông tin chung',
      children: (
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="name"
              label="Doanh nghiệp"
              rules={[{ required: true, message: 'Vui lòng nhập tên doanh nghiệp' }]}
            >
              <Input disabled={!canEditField('name')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="businessScale"
              label="Quy mô doanh nghiệp"
            >
              <Select allowClear disabled={!canEditField('businessScale')}>
                <Option value="Siêu nhỏ">Siêu nhỏ</Option>
                <Option value="Nhỏ">Nhỏ</Option>
                <Option value="Vừa">Vừa</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="taxCode"
              label="Mã số thuế"
            >
              <Input disabled={!canEditField('taxCode')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="address"
              label="Địa chỉ"
            >
              <TextArea rows={2} disabled={!canEditField('address')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="representativeName"
              label="Người đại diện"
            >
              <Input disabled={!canEditField('representativeName')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="representativePosition"
              label="Chức vụ"
            >
              <Input disabled={!canEditField('representativePosition')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="idNumber"
              label="CCCD/Hộ chiếu"
            >
              <Input disabled={!canEditField('idNumber')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="phone"
              label="Số điện thoại"
            >
              <Input disabled={!canEditField('phone')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="email"
              label="Email"
              rules={[{ type: 'email', message: 'Email không hợp lệ' }]}
            >
              <Input disabled={!canEditField('email')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="contactPerson"
              label="Người liên hệ"
            >
              <Input disabled={!canEditField('contactPerson')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="contactPhone"
              label="SĐT người liên hệ"
            >
              <Input disabled={!canEditField('contactPhone')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="contactEmail"
              label="Email người liên hệ"
              rules={[{ type: 'email', message: 'Email không hợp lệ' }]}
            >
              <Input disabled={!canEditField('contactEmail')} />
            </Form.Item>
          </Col>
        </Row>
      ),
    },
    {
      key: '2',
      label: 'II. Nhu cầu - Lead - Tiềm năng',
      children: (
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="businessNeeds"
              label="Nhu cầu của DN"
            >
              <Select allowClear disabled={!canEditField('businessNeeds')}>
                <Option value="Bản quyền">Bản quyền</Option>
                <Option value="Công bố">Công bố</Option>
                <Option value="Nhãn hiệu">Nhãn hiệu</Option>
                <Option value="Kiểu dáng công nghiệp">Kiểu dáng công nghiệp</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="potentialLevel"
              label="Mức độ tiềm năng"
            >
              <Select allowClear disabled={!canEditField('potentialLevel')}>
                <Option value="Cao">Cao</Option>
                <Option value="Trung bình">Trung bình</Option>
                <Option value="Thấp">Thấp</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="priority"
              label="Ưu tiên"
            >
              <Select allowClear disabled={!canEditField('priority')}>
                <Option value="Mức 1">Mức 1</Option>
                <Option value="Mức 2">Mức 2</Option>
                <Option value="Mức 3">Mức 3</Option>
                <Option value="Mức 4">Mức 4</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="sourceClassification"
              label="Phân loại nguồn"
            >
              <Select allowClear disabled={!canEditField('sourceClassification')}>
                <Option value="NSNN">NSNN</Option>
                <Option value="Vãng lai">Vãng lai</Option>
                <Option value="Đối tác">Đối tác</Option>
                <Option value="Sự kiện">Sự kiện</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="nsnnSource"
              label="Nguồn NSNN"
            >
              <Select allowClear disabled={!canEditField('nsnnSource')}>
                <Option value="TACs">TACs</Option>
                <Option value="Sở TC Huế">Sở TC Huế</Option>
                <Option value="Sở CT Quảng Trị">Sở CT Quảng Trị</Option>
                <Option value="Sở CT Hậu Giang">Sở CT Hậu Giang</Option>
                <Option value="Chi cục PTNT Lâm Đồng">Chi cục PTNT Lâm Đồng</Option>
              </Select>
            </Form.Item>
          </Col>
        </Row>
      ),
    },
    {
      key: '3',
      label: 'III. Thông tin SHTT cốt lõi',
      children: (
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="brandName"
              label="Thương hiệu"
            >
              <Input disabled={!canEditField('brandName')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="productsServices"
              label="Sản phẩm/Dịch vụ đi kèm thương hiệu"
            >
              <Select allowClear disabled={!canEditField('productsServices')}>
                <Option value="Đăng ký">Đăng ký</Option>
                <Option value="Gia hạn">Gia hạn</Option>
                <Option value="Sửa đổi">Sửa đổi</Option>
                <Option value="Phản đối">Phản đối</Option>
                <Option value="Khiếu nại">Khiếu nại</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="ipGroup"
              label="Nhóm SHTT sau khi phân loại"
            >
              <Input disabled={!canEditField('ipGroup')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="owner"
              label="Chủ sở hữu"
            >
              <Input disabled={!canEditField('owner')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="protectionTerritory"
              label="Lãnh thổ bảo hộ"
            >
              <Select allowClear disabled={!canEditField('protectionTerritory')}>
                <Option value="Việt Nam">Việt Nam</Option>
                <Option value="Mỹ">Mỹ</Option>
                <Option value="Madrid">Madrid</Option>
                <Option value="Wipo">Wipo</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="authorization"
              label="Uỷ quyền"
            >
              <Select allowClear disabled={!canEditField('authorization')}>
                <Option value="Chưa có">Chưa có</Option>
                <Option value="Đã có">Đã có</Option>
              </Select>
            </Form.Item>
          </Col>
        </Row>
      ),
    },
    {
      key: '4',
      label: 'IV. Hồ sơ đơn – văn bằng – pháp lý',
      children: (
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="filingStatus"
              label="Tình trạng nộp đơn"
            >
              <Select allowClear disabled={!canEditField('filingStatus')}>
                <Option value="Chưa triển khai">Chưa triển khai</Option>
                <Option value="Đang viết hồ sơ">Đang viết hồ sơ</Option>
                <Option value="Đã viết hồ sơ">Đã viết hồ sơ</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="filingDate"
              label="Ngày nộp đơn"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('filingDate')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="applicationCode"
              label="Mã đơn/Công bố/Văn bằng"
            >
              <Input disabled={!canEditField('applicationCode')} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item
              name="issueDate"
              label="Ngày cấp"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('issueDate')} />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item
              name="expiryDate"
              label="Ngày hết hạn"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('expiryDate')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="applicationReviewStatus"
              label="Tình trạng xét duyệt đơn"
            >
              <Select allowClear disabled={!canEditField('applicationReviewStatus')}>
                <Option value="Đang xét duyệt">Đang xét duyệt</Option>
                <Option value="Có OA chưa phản hồi">Có OA chưa phản hồi</Option>
                <Option value="Được duyệt">Được duyệt</Option>
                <Option value="Có OA đã phản hồi">Có OA đã phản hồi</Option>
                <Option value="Từ chối">Từ chối</Option>
                <Option value="Hết hạn">Hết hạn</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="processingDeadline"
              label="Hạn xử lý"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('processingDeadline')} />
            </Form.Item>
          </Col>
        </Row>
      ),
    },
    {
      key: '5',
      label: 'V. Gia hạn & nhắc việc',
      children: (
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="renewalCycle"
              label="Chu kỳ gia hạn"
            >
              <Select allowClear disabled={!canEditField('renewalCycle')}>
                <Option value="5 năm">5 năm</Option>
                <Option value="10 năm">10 năm</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="renewalDate"
              label="Ngày cần gia hạn"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('renewalDate')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="reminderDate"
              label="Ngày nhắc hẹn (trước 3 tháng)"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('reminderDate')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="reminderStatus"
              label="Trạng thái nhắc"
            >
              <Select allowClear disabled={!canEditField('reminderStatus')}>
                <Option value="Chưa nhắc">Chưa nhắc</Option>
                <Option value="Đã nhắc">Đã nhắc</Option>
              </Select>
            </Form.Item>
          </Col>
        </Row>
      ),
    },
    {
      key: '6',
      label: 'VI. Hợp đồng – tài chính',
      children: (
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="consultingStatus"
              label="Tình trạng tư vấn"
            >
              <Select allowClear disabled={!canEditField('consultingStatus')}>
                <Option value="Chưa tư vấn">Chưa tư vấn</Option>
                <Option value="Đang tư vấn">Đang tư vấn</Option>
                <Option value="Đã tư vấn">Đã tư vấn</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="contractStatus"
              label="Tình trạng hợp đồng"
            >
              <Select allowClear disabled={!canEditField('contractStatus')}>
                <Option value="Đã gửi">Đã gửi</Option>
                <Option value="Đợi phản hồi">Đợi phản hồi</Option>
                <Option value="Đã kí">Đã kí</Option>
                <Option value="Tạm ngưng">Tạm ngưng</Option>
                <Option value="Đã thanh toán HĐ">Đã thanh toán HĐ</Option>
              </Select>
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="contractNumber"
              label="Số hợp đồng"
            >
              <Input disabled={!canEditField('contractNumber')} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="contractValue"
              label="Giá trị hợp đồng"
            >
              <Input type="number" disabled={!canEditField('contractValue')} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="stateFee"
              label="Lệ phí nhà nước"
            >
              <Input type="number" disabled={!canEditField('stateFee')} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="additionalFee"
              label="Phí phát sinh"
            >
              <Input type="number" disabled={!canEditField('additionalFee')} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="startDate"
              label="Ngày bắt đầu"
              rules={[{ required: true, message: 'Vui lòng chọn ngày bắt đầu' }]}
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('startDate')} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="endDate"
              label="Ngày kết thúc"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('endDate')} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item
              name="implementationDays"
              label="Số ngày triển khai"
            >
              <Input type="number" disabled={!canEditField('implementationDays')} />
            </Form.Item>
          </Col>
        </Row>
      ),
    },
    {
      key: '7',
      label: 'VII. Hệ thống – kiểm soát',
      children: (
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="createdBy"
              label="Người tạo"
            >
              <Input disabled={!canEditField('createdBy')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="updatedBy"
              label="Người cập nhật"
            >
              <Input disabled={!canEditField('updatedBy')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="updatedAt"
              label="Ngày cập nhật"
            >
              <DatePicker style={{ width: '100%' }} disabled={!canEditField('updatedAt')} />
            </Form.Item>
          </Col>
          <Col span={12}>
            <Form.Item
              name="documentLink"
              label="Link hồ sơ giấy tờ"
            >
              <Input disabled={!canEditField('documentLink')} />
            </Form.Item>
          </Col>
        </Row>
      ),
    },
  ];

  return (
    <div className="customers-container">
      <div className="customers-header">
        <h2>Quản lý khách hàng</h2>
        <Space>
          <Input
            placeholder="Tìm kiếm khách hàng..."
            prefix={<SearchOutlined />}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setTableParams(prev => ({
                ...prev,
                pagination: {
                  ...prev.pagination,
                  current: 1,
                },
              }));
            }}
            style={{ width: 300 }}
          />
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleAdd}
          >
            Thêm khách hàng
          </Button>
        </Space>
      </div>

      <Table
        columns={columns}
        dataSource={customers}
        loading={loading}
        rowKey="id"
        pagination={{
          ...tableParams.pagination,
          total: total,
          showSizeChanger: true,
        }}
        onChange={handleTableChange}
        scroll={{ x: 'max-content', y: 'calc(100vh - 300px)' }}
        bordered
      />
      <Modal
        title={editingCustomer ? 'Sửa khách hàng' : 'Thêm khách hàng'}
        open={isModalVisible}
        onCancel={() => {
          setIsModalVisible(false);
          form.resetFields();
          setActiveTab('1');
        }}
        footer={null}
        width={800}
      >
        <Form
          form={form}
          layout="vertical"
          scrollToFirstError
          onFinishFailed={onFinishFailed}
          onValuesChange={(changedValues, allValues) => {
            if (changedValues.startDate || changedValues.endDate) {
              const s = allValues.startDate;
              const e = allValues.endDate;
              if (s && e) {
                const diff = dayjs(e).diff(dayjs(s), 'day');
                form.setFieldsValue({ implementationDays: diff >= 0 ? diff : 0 });
              }
            }
          }}
          onFinish={handleSubmit}
        >
          <Tabs activeKey={activeTab} onChange={setActiveTab} items={tabItems} />
          
          <div style={{ textAlign: 'right', marginTop: 16 }}>
            <Space>
              <Button onClick={() => setIsModalVisible(false)}>Hủy</Button>
              <Button type="primary" htmlType="submit">
                {editingCustomer ? 'Cập nhật' : 'Thêm mới'}
              </Button>
            </Space>
          </div>
        </Form>
      </Modal>
    </div>
  );
};

export default Customers;
