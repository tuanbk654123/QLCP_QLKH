import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, Form, Input, Modal, Select, Space, Table, Tag, Typography, message } from 'antd';
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import axios from 'axios';

const { Title, Text } = Typography;

const Roles = () => {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState([]);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form] = Form.useForm();

  const fetchRoles = useCallback(async () => {
    setLoading(true);
    try {
      const res = await axios.get('/api/roles/all');
      setItems(res.data.items || []);
    } catch {
      setItems([]);
      message.error('Không thể tải danh sách chức danh');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchRoles();
  }, [fetchRoles]);

  const openCreate = useCallback(() => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true });
    setOpen(true);
  }, [form]);

  const openEdit = useCallback(
    (r) => {
      setEditing(r);
      form.setFieldsValue({
        code: r.code,
        name: r.name,
        description: r.description,
        isActive: r.isActive,
      });
      setOpen(true);
    },
    [form],
  );

  const onSubmit = useCallback(async () => {
    try {
      const values = await form.validateFields();
      if (editing?.code) {
        await axios.put(`/api/roles/${editing.code}`, values);
        message.success('Cập nhật chức danh thành công');
      } else {
        await axios.post('/api/roles', values);
        message.success('Tạo chức danh thành công');
      }
      setOpen(false);
      setEditing(null);
      fetchRoles();
    } catch (e) {}
  }, [editing, fetchRoles, form]);

  const onDelete = useCallback(
    async (r) => {
      try {
        await axios.delete(`/api/roles/${r.code}`);
        message.success('Đã xóa/ngừng kích hoạt chức danh');
        fetchRoles();
      } catch (e) {
        message.error(e?.response?.data?.message || 'Không thể xóa chức danh');
      }
    },
    [fetchRoles],
  );

  const columns = useMemo(() => {
    return [
      { title: 'Tên chức danh', dataIndex: 'name', key: 'name', width: 260 },
      { title: 'Code', dataIndex: 'code', key: 'code', width: 180, render: (v) => <Text code>{v}</Text> },
      {
        title: 'Trạng thái',
        dataIndex: 'isActive',
        key: 'isActive',
        width: 120,
        render: (v) => (v ? <Tag color="green">Đang dùng</Tag> : <Tag>Ngừng</Tag>),
      },
      {
        title: 'Hệ thống',
        dataIndex: 'isSystem',
        key: 'isSystem',
        width: 120,
        render: (v) => (v ? <Tag color="gold">System</Tag> : ''),
      },
      { title: 'Mô tả', dataIndex: 'description', key: 'description' },
      {
        title: 'Thao tác',
        key: 'action',
        width: 180,
        fixed: 'right',
        render: (_, r) => (
          <Space>
            <Button onClick={() => openEdit(r)}>Sửa</Button>
            <Button danger disabled={r.isSystem} onClick={() => onDelete(r)}>
              Xóa
            </Button>
          </Space>
        ),
      },
    ];
  }, [onDelete, openEdit]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 12, flexWrap: 'wrap' }}>
        <div>
          <Title level={4} style={{ margin: 0 }}>
            Quản lý chức danh
          </Title>
          <Text type="secondary">Tạo/sửa/xóa chức danh (role) và dùng trong phân quyền</Text>
        </div>
        <Space wrap>
          <Button icon={<ReloadOutlined />} onClick={fetchRoles}>
            Làm mới
          </Button>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Thêm chức danh
          </Button>
        </Space>
      </div>

      <Card bodyStyle={{ padding: 0 }} style={{ borderRadius: 12, overflow: 'hidden' }}>
        <Table rowKey="code" loading={loading} columns={columns} dataSource={items} pagination={false} scroll={{ x: 980 }} />
      </Card>

      <Modal
        open={open}
        title={editing ? 'Cập nhật chức danh' : 'Thêm chức danh'}
        onCancel={() => {
          setOpen(false);
          setEditing(null);
        }}
        onOk={onSubmit}
        okText="Lưu"
      >
        <Form form={form} layout="vertical">
          <Form.Item
            name="code"
            label="Code"
            rules={[{ required: true, message: 'Nhập code (vd: ke_toan, truong_phong)' }]}
          >
            <Input disabled={editing?.isSystem} placeholder="vd: ke_toan" />
          </Form.Item>
          <Form.Item name="name" label="Tên chức danh" rules={[{ required: true, message: 'Nhập tên chức danh' }]}>
            <Input placeholder="vd: Kế toán" />
          </Form.Item>
          <Form.Item name="description" label="Mô tả">
            <Input.TextArea rows={3} placeholder="Mô tả ngắn" />
          </Form.Item>
          <Form.Item name="isActive" label="Trạng thái" initialValue={true}>
            <Select
              options={[
                { value: true, label: 'Đang dùng' },
                { value: false, label: 'Ngừng' },
              ]}
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Roles;

