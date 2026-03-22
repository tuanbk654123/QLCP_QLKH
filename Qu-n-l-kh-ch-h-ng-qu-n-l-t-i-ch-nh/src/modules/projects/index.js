import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, DatePicker, Form, Input, InputNumber, Modal, Progress, Select, Space, Table, Tag, Typography, message } from 'antd';
import { PlusOutlined, SearchOutlined } from '@ant-design/icons';
import axios from 'axios';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import './index.css';

const { Title, Text } = Typography;

const statusOptions = [
  { value: 'NOT_STARTED', label: 'Chưa bắt đầu', color: 'default' },
  { value: 'IN_PROGRESS', label: 'Đang thực hiện', color: 'blue' },
  { value: 'IN_REVIEW', label: 'Đang kiểm tra', color: 'gold' },
  { value: 'DONE', label: 'Hoàn thành', color: 'green' },
  { value: 'PAUSED', label: 'Tạm dừng', color: 'orange' },
  { value: 'CANCELLED', label: 'Hủy', color: 'red' },
];

const Projects = () => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [customerSearch, setCustomerSearch] = useState('');
  const [customerOptions, setCustomerOptions] = useState([]);
  const [form] = Form.useForm();

  const canManage = useMemo(() => {
    const role = user?.role;
    return ['admin', 'ceo', 'assistant_ceo', 'director', 'giam_doc', 'assistant_director', 'ip_manager', 'manager', 'quan_ly'].includes(role);
  }, [user]);

  const fetchCustomers = useCallback(async () => {
    try {
      const res = await axios.get('/api/projects/lookups/customers', { params: { search: customerSearch } });
      setCustomerOptions(res.data.items || []);
    } catch {
      setCustomerOptions([]);
    }
  }, [customerSearch]);

  useEffect(() => {
    if (!open) return;
    fetchCustomers();
  }, [open, fetchCustomers]);

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await axios.get('/api/projects', { params: { search, page, pageSize } });
      setItems(res.data.items || []);
      setTotal(res.data.total || 0);
    } catch (e) {
      message.error('Lỗi khi tải danh sách dự án');
    } finally {
      setLoading(false);
    }
  }, [search, page, pageSize]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({
      status: 'NOT_STARTED',
      progress: 0,
    });
    setOpen(true);
  };

  const openEdit = useCallback((record) => {
    setEditing(record);
    form.resetFields();
    form.setFieldsValue({
      ...record,
      startDate: record.startDate ? dayjs(record.startDate) : null,
      endDate: record.endDate ? dayjs(record.endDate) : null,
      customerLegacyId: record.customerLegacyId ?? null,
    });
    setOpen(true);
  }, [form]);

  const onSubmit = async () => {
    try {
      const values = await form.validateFields();
      const payload = {
        name: values.name,
        code: values.code,
        customerLegacyId: values.customerLegacyId || null,
        startDate: values.startDate ? values.startDate.format('YYYY-MM-DD') : null,
        endDate: values.endDate ? values.endDate.format('YYYY-MM-DD') : null,
        managerUserId: values.managerUserId || null,
        status: values.status,
        progress: values.progress || 0,
        description: values.description || null,
        budget: typeof values.budget === 'number' ? values.budget : null,
      };

      if (editing) {
        await axios.put(`/api/projects/${editing.id}`, payload);
        message.success('Đã cập nhật dự án');
      } else {
        await axios.post('/api/projects', payload);
        message.success('Đã tạo dự án');
      }

      setOpen(false);
      setEditing(null);
      form.resetFields();
      fetchData();
    } catch (e) {
      if (e?.errorFields) return;
      message.error('Không thể lưu dự án');
    }
  };

  const statusTag = useCallback((status) => {
    const meta = statusOptions.find((x) => x.value === status);
    return <Tag color={meta?.color || 'default'}>{meta?.label || status}</Tag>;
  }, []);

  const columns = useMemo(() => {
    return [
      { title: 'STT', dataIndex: 'id', key: 'id', width: 72, align: 'center' },
      {
        title: 'Tên dự án',
        dataIndex: 'name',
        key: 'name',
        render: (v, r) => (
          <div className="projects-name-cell">
            <Button type="link" onClick={() => navigate(`/projects/${r.id}`)} style={{ padding: 0 }}>
              {v}
            </Button>
            <div className="projects-sub">
              <Text type="secondary">{r.code}</Text>
              {r.customerName ? <Text type="secondary">• {r.customerName}</Text> : null}
            </div>
          </div>
        ),
      },
      { title: 'Quản lý', dataIndex: 'managerName', key: 'managerName', width: 160, render: (v) => v || <Text type="secondary">-</Text> },
      { title: 'Bắt đầu', dataIndex: 'startDate', key: 'startDate', width: 110, render: (v) => v || <Text type="secondary">-</Text> },
      { title: 'Kết thúc', dataIndex: 'endDate', key: 'endDate', width: 110, render: (v) => v || <Text type="secondary">-</Text> },
      {
        title: 'Trạng thái',
        dataIndex: 'status',
        key: 'status',
        width: 150,
        render: (v) => statusTag(v),
      },
      {
        title: 'Tiến độ',
        dataIndex: 'progress',
        key: 'progress',
        width: 200,
        render: (v) => (
          <div className="projects-progress">
            <Progress percent={v || 0} size="small" showInfo={false} />
            <Text style={{ width: 44, textAlign: 'right' }}>{v || 0}%</Text>
          </div>
        ),
      },
      canManage
        ? {
            title: 'Thao tác',
            key: 'action',
            width: 110,
            fixed: 'right',
            render: (_, r) => <Button onClick={() => openEdit(r)}>Sửa</Button>,
          }
        : null,
    ].filter(Boolean);
  }, [canManage, navigate, openEdit, statusTag]);

  return (
    <div className="projects-page">
      <div className="projects-header">
        <div>
          <Title level={4} style={{ margin: 0 }}>
            Quản lý dự án
          </Title>
          <Text type="secondary">Tổng: {total || 0} dự án</Text>
        </div>
        <Space wrap>
          <Input
            placeholder="Tìm theo tên/mã/khách hàng"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onPressEnter={() => {
              setPage(1);
              fetchData();
            }}
            style={{ width: 320 }}
            prefix={<SearchOutlined />}
            allowClear
          />
          <Button
            onClick={() => {
              setPage(1);
              fetchData();
            }}
          >
            Tìm
          </Button>
          <Button
            onClick={() => {
              setSearch('');
              setPage(1);
            }}
          >
            Reset
          </Button>
          {canManage && (
            <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
              Thêm dự án
            </Button>
          )}
        </Space>
      </div>

      <Card className="projects-card" bodyStyle={{ padding: 0 }}>
        <Table
          rowKey="id"
          loading={loading}
          columns={columns}
          dataSource={items}
          size="middle"
          scroll={{ x: 980 }}
          rowClassName={(_, index) => (index % 2 === 0 ? 'projects-row-even' : 'projects-row-odd')}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            onChange: (p, ps) => {
              setPage(p);
              setPageSize(ps);
            },
          }}
        />
      </Card>

      <Modal
        open={open}
        title={editing ? 'Cập nhật dự án' : 'Thêm dự án'}
        onCancel={() => {
          setOpen(false);
          setEditing(null);
        }}
        onOk={onSubmit}
        okText="Lưu"
        destroyOnClose
      >
        <Form layout="vertical" form={form}>
          <Form.Item name="name" label="Tên dự án" rules={[{ required: true, message: 'Vui lòng nhập tên dự án' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="code" label="Mã dự án" rules={[{ required: true, message: 'Vui lòng nhập mã dự án' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="customerLegacyId" label="Khách hàng">
            <Select
              showSearch
              allowClear
              filterOption={false}
              onSearch={(v) => setCustomerSearch(v)}
              onDropdownVisibleChange={(visible) => visible && fetchCustomers()}
              options={(customerOptions || []).map((c) => ({ value: c.id, label: c.name }))}
            />
          </Form.Item>
          <Form.Item name="managerUserId" label="Quản lý dự án (ID nhân viên)">
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>
          <Form.Item name="status" label="Trạng thái">
            <Select options={statusOptions} />
          </Form.Item>
          <Form.Item name="progress" label="Tiến độ (%)">
            <InputNumber style={{ width: '100%' }} min={0} max={100} />
          </Form.Item>
          <Form.Item name="budget" label="Ngân sách">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
          <Form.Item name="startDate" label="Ngày bắt đầu">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="endDate" label="Ngày kết thúc">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="description" label="Mô tả dự án">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Projects;
