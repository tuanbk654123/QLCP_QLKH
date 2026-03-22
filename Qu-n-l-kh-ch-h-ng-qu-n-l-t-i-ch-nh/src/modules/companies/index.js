import React, { useEffect, useMemo, useState } from 'react';
import { Button, Card, Form, Input, Modal, Space, Table, Tag, message, Select } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import axios from 'axios';

const Companies = () => {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState([]);
  const [search, setSearch] = useState('');
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form] = Form.useForm();

  const fetchData = async (q) => {
    setLoading(true);
    try {
      const res = await axios.get('/api/companies', { params: { search: q || undefined } });
      setItems(res.data.items || []);
    } catch (err) {
      message.error('Không thể tải danh sách công ty');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData('');
  }, []);

  const columns = useMemo(
    () => [
      { title: 'Mã', dataIndex: 'code', key: 'code', width: 120 },
      { title: 'Tên công ty', dataIndex: 'name', key: 'name' },
      {
        title: 'Trạng thái',
        dataIndex: 'status',
        key: 'status',
        width: 140,
        render: (v) => (v === 'active' ? <Tag color="green">Hoạt động</Tag> : <Tag color="red">Ngưng</Tag>),
      },
      { title: 'Domain', dataIndex: 'domain', key: 'domain', width: 220 },
      {
        title: 'Thao tác',
        key: 'actions',
        width: 140,
        render: (_, record) => (
          <Space>
            <Button
              size="small"
              onClick={() => {
                setEditing(record);
                setOpen(true);
                form.setFieldsValue({
                  code: record.code,
                  name: record.name,
                  domain: record.domain,
                  status: record.status,
                });
              }}
            >
              Sửa
            </Button>
          </Space>
        ),
      },
    ],
    [form],
  );

  const onSubmit = async () => {
    const values = await form.validateFields();
    setLoading(true);
    try {
      if (editing) {
        await axios.put(`/api/companies/${editing.id}`, values);
        message.success('Cập nhật công ty thành công');
      } else {
        await axios.post('/api/companies', values);
        message.success('Tạo công ty thành công');
      }
      setOpen(false);
      setEditing(null);
      form.resetFields();
      await fetchData(search);
    } catch (err) {
      message.error(err?.response?.data?.message || 'Thao tác thất bại');
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card
      title="Quản lý Công ty"
      extra={
        <Space>
          <Input
            placeholder="Tìm theo mã/tên"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ width: 220 }}
            onPressEnter={() => fetchData(search)}
          />
          <Button onClick={() => fetchData(search)}>Tìm</Button>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditing(null);
              setOpen(true);
              form.resetFields();
              form.setFieldsValue({ status: 'active' });
            }}
          >
            Thêm công ty
          </Button>
        </Space>
      }
    >
      <Table
        rowKey="id"
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={{ pageSize: 50 }}
      />

      <Modal
        open={open}
        title={editing ? 'Sửa công ty' : 'Thêm công ty'}
        onCancel={() => {
          setOpen(false);
          setEditing(null);
        }}
        onOk={onSubmit}
        okText="Lưu"
      >
        <Form form={form} layout="vertical">
          <Form.Item name="code" label="Mã" rules={[{ required: true, message: 'Vui lòng nhập mã' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="name" label="Tên công ty" rules={[{ required: true, message: 'Vui lòng nhập tên công ty' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="domain" label="Domain">
            <Input />
          </Form.Item>
          <Form.Item name="status" label="Trạng thái" rules={[{ required: true, message: 'Vui lòng chọn trạng thái' }]}>
            <Select
              options={[
                { value: 'active', label: 'Hoạt động' },
                { value: 'inactive', label: 'Ngưng' },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default Companies;
