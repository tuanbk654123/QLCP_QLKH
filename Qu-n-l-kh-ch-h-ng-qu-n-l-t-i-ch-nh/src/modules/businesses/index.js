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
} from 'antd';
import {
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import './index.css';

const Businesses = () => {
  const [businesses, setBusinesses] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingBusiness, setEditingBusiness] = useState(null);
  const [form] = Form.useForm();

  const fetchBusinesses = useCallback(async () => {
    setLoading(true);
    try {
      const response = await axios.get('/api/businesses', {
        params: { search, page },
      });
      setBusinesses(response.data.businesses);
      setTotal(response.data.businessCount);
    } catch (error) {
      message.error('Lỗi khi tải dữ liệu doanh nghiệp');
    } finally {
      setLoading(false);
    }
  }, [search, page]);

  useEffect(() => {
    fetchBusinesses();
  }, [fetchBusinesses]);

  const handleAdd = () => {
    setEditingBusiness(null);
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleEdit = (record) => {
    setEditingBusiness(record);
    form.setFieldsValue(record);
    setIsModalVisible(true);
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`/api/businesses/${id}`);
      message.success('Xóa doanh nghiệp thành công');
      fetchBusinesses();
    } catch (error) {
      message.error('Lỗi khi xóa doanh nghiệp');
    }
  };

  const handleSubmit = async (values) => {
    try {
      if (editingBusiness) {
        await axios.put(`/api/businesses/${editingBusiness.id}`, values);
        message.success('Cập nhật doanh nghiệp thành công');
      } else {
        await axios.post('/api/businesses', values);
        message.success('Thêm doanh nghiệp thành công');
      }
      setIsModalVisible(false);
      form.resetFields();
      fetchBusinesses();
    } catch (error) {
      message.error('Lỗi khi lưu doanh nghiệp');
    }
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 80,
    },
    {
      title: 'Tên doanh nghiệp',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: 'Mã số thuế',
      dataIndex: 'taxCode',
      key: 'taxCode',
    },
    {
      title: 'Ngành nghề',
      dataIndex: 'industry',
      key: 'industry',
    },
    {
      title: 'Số nhân viên',
      dataIndex: 'employeeCount',
      key: 'employeeCount',
      align: 'right',
    },
    {
      title: 'Doanh thu',
      dataIndex: 'revenue',
      key: 'revenue',
      align: 'right',
      render: (amount) =>
        new Intl.NumberFormat('vi-VN', {
          style: 'currency',
          currency: 'VND',
        }).format(amount),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status) => (
        <Tag color={status === 'active' ? 'green' : 'red'}>
          {status === 'active' ? 'Hoạt động' : 'Không hoạt động'}
        </Tag>
      ),
    },
    {
      title: 'Người liên hệ',
      dataIndex: 'contactPerson',
      key: 'contactPerson',
    },
    {
      title: 'Thao tác',
      key: 'action',
      width: 150,
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

  return (
    <div className="businesses-container">
      <div className="businesses-header">
        <h2>Quản lý doanh nghiệp</h2>
        <Space>
          <Input
            placeholder="Tìm kiếm doanh nghiệp..."
            prefix={<SearchOutlined />}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            style={{ width: 300 }}
          />
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleAdd}
          >
            Thêm doanh nghiệp
          </Button>
        </Space>
      </div>

      <Table
        columns={columns}
        dataSource={businesses}
        loading={loading}
        rowKey="id"
        pagination={{
          current: page,
          pageSize: 10,
          total: total,
          onChange: (page) => setPage(page),
        }}
      />

      <Modal
        title={editingBusiness ? 'Sửa doanh nghiệp' : 'Thêm doanh nghiệp'}
        open={isModalVisible}
        onCancel={() => {
          setIsModalVisible(false);
          form.resetFields();
        }}
        footer={null}
        width={700}
      >
        <Form
          form={form}
          layout="vertical"
          scrollToFirstError
          onFinish={handleSubmit}
        >
          <Form.Item
            name="name"
            label="Tên doanh nghiệp"
            rules={[{ required: true, message: 'Vui lòng nhập tên doanh nghiệp' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="taxCode"
            label="Mã số thuế"
            rules={[{ required: true, message: 'Vui lòng nhập mã số thuế' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="address"
            label="Địa chỉ"
            rules={[{ required: true, message: 'Vui lòng nhập địa chỉ' }]}
          >
            <Input.TextArea rows={2} />
          </Form.Item>

          <Form.Item
            name="phone"
            label="Số điện thoại"
            rules={[{ required: true, message: 'Vui lòng nhập số điện thoại' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Vui lòng nhập email' },
              { type: 'email', message: 'Email không hợp lệ' },
            ]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="website"
            label="Website"
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="industry"
            label="Ngành nghề"
            rules={[{ required: true, message: 'Vui lòng nhập ngành nghề' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="contactPerson"
            label="Người liên hệ"
            rules={[{ required: true, message: 'Vui lòng nhập người liên hệ' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="contactPhone"
            label="Số điện thoại liên hệ"
            rules={[{ required: true, message: 'Vui lòng nhập số điện thoại liên hệ' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="status"
            label="Trạng thái"
            initialValue="active"
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="notes"
            label="Ghi chú"
          >
            <Input.TextArea rows={3} />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                {editingBusiness ? 'Cập nhật' : 'Thêm mới'}
              </Button>
              <Button onClick={() => {
                setIsModalVisible(false);
                form.resetFields();
              }}>
                Hủy
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Businesses;

