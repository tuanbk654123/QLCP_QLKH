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
  DatePicker,
  Card,
  Statistic,
  Row,
  Col,
} from 'antd';
import {
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  SearchOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import moment from 'moment';
import './index.css';

const { Option } = Select;

const Transactions = () => {
  const [transactions, setTransactions] = useState([]);
  const [loading, setLoading] = useState(false);
  const [summary, setSummary] = useState({ totalRevenue: 0, totalExpense: 0, balance: 0 });
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [editingTransaction, setEditingTransaction] = useState(null);
  const [form] = Form.useForm();

  const fetchTransactions = useCallback(async () => {
    setLoading(true);
    try {
      const response = await axios.get('/api/transactions', {
        params: { search, type: typeFilter, page },
      });
      setTransactions(response.data.transactions);
      setTotal(response.data.transactionCount);
    } catch (error) {
      message.error('Lỗi khi tải dữ liệu thu chi');
    } finally {
      setLoading(false);
    }
  }, [search, typeFilter, page]);

  useEffect(() => {
    fetchTransactions();
    fetchSummary();
  }, [fetchTransactions]);

  const fetchSummary = async () => {
    try {
      const response = await axios.get('/api/transactions/summary');
      setSummary(response.data);
    } catch (error) {
      console.error('Lỗi khi tải tổng kết');
    }
  };

  const handleAdd = () => {
    setEditingTransaction(null);
    form.resetFields();
    setIsModalVisible(true);
  };

  const handleEdit = (record) => {
    setEditingTransaction(record);
    form.setFieldsValue({
      ...record,
      date: record.date ? moment(record.date) : null,
    });
    setIsModalVisible(true);
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`/api/transactions/${id}`);
      message.success('Xóa giao dịch thành công');
      fetchTransactions();
      fetchSummary();
    } catch (error) {
      message.error('Lỗi khi xóa giao dịch');
    }
  };

  const handleSubmit = async (values) => {
    try {
      const submitData = {
        ...values,
        date: values.date ? values.date.format('YYYY-MM-DD') : new Date().toISOString().split('T')[0],
      };
      if (editingTransaction) {
        await axios.put(`/api/transactions/${editingTransaction.id}`, submitData);
        message.success('Cập nhật giao dịch thành công');
      } else {
        await axios.post('/api/transactions', submitData);
        message.success('Thêm giao dịch thành công');
      }
      setIsModalVisible(false);
      form.resetFields();
      fetchTransactions();
      fetchSummary();
    } catch (error) {
      message.error('Lỗi khi lưu giao dịch');
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 80,
    },
    {
      title: 'Loại',
      dataIndex: 'type',
      key: 'type',
      render: (type) => (
        <Tag color={type === 'revenue' ? 'green' : 'red'}>
          {type === 'revenue' ? 'Thu' : 'Chi'}
        </Tag>
      ),
    },
    {
      title: 'Danh mục',
      dataIndex: 'category',
      key: 'category',
    },
    {
      title: 'Mô tả',
      dataIndex: 'description',
      key: 'description',
      width: 300,
    },
    {
      title: 'Số tiền',
      dataIndex: 'amount',
      key: 'amount',
      align: 'right',
      render: (amount, record) => (
        <span style={{ color: record.type === 'revenue' ? 'green' : 'red' }}>
          {record.type === 'revenue' ? '+' : '-'} {formatCurrency(amount)}
        </span>
      ),
    },
    {
      title: 'Ngày',
      dataIndex: 'date',
      key: 'date',
    },
    {
      title: 'Phương thức',
      dataIndex: 'paymentMethod',
      key: 'paymentMethod',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status) => (
        <Tag color={status === 'completed' ? 'green' : 'orange'}>
          {status === 'completed' ? 'Hoàn thành' : 'Chờ xử lý'}
        </Tag>
      ),
    },
    {
      title: 'Tham chiếu',
      dataIndex: 'reference',
      key: 'reference',
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
    <div className="transactions-container">
      <div className="transactions-header">
        <h2>Quản lý thu chi</h2>
        <Space>
          <Input
            placeholder="Tìm kiếm giao dịch..."
            prefix={<SearchOutlined />}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            style={{ width: 300 }}
          />
          <Select
            placeholder="Lọc theo loại"
            value={typeFilter}
            onChange={(value) => {
              setTypeFilter(value);
              setPage(1);
            }}
            style={{ width: 150 }}
            allowClear
          >
            <Option value="revenue">Thu</Option>
            <Option value="expense">Chi</Option>
          </Select>
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={handleAdd}
          >
            Thêm giao dịch
          </Button>
        </Space>
      </div>

      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={8}>
          <Card>
            <Statistic
              title="Tổng thu"
              value={summary.totalRevenue}
              prefix={<ArrowUpOutlined />}
              valueStyle={{ color: '#3f8600' }}
              formatter={(value) => formatCurrency(value)}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="Tổng chi"
              value={summary.totalExpense}
              prefix={<ArrowDownOutlined />}
              valueStyle={{ color: '#cf1322' }}
              formatter={(value) => formatCurrency(value)}
            />
          </Card>
        </Col>
        <Col span={8}>
          <Card>
            <Statistic
              title="Số dư"
              value={summary.balance}
              valueStyle={{ color: summary.balance >= 0 ? '#3f8600' : '#cf1322' }}
              formatter={(value) => formatCurrency(value)}
            />
          </Card>
        </Col>
      </Row>

      <Table
        columns={columns}
        dataSource={transactions}
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
        title={editingTransaction ? 'Sửa giao dịch' : 'Thêm giao dịch'}
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
            name="type"
            label="Loại"
            rules={[{ required: true, message: 'Vui lòng chọn loại' }]}
            initialValue="revenue"
          >
            <Select>
              <Option value="revenue">Thu</Option>
              <Option value="expense">Chi</Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="category"
            label="Danh mục"
            rules={[{ required: true, message: 'Vui lòng nhập danh mục' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="amount"
            label="Số tiền"
            rules={[{ required: true, message: 'Vui lòng nhập số tiền' }]}
          >
            <Input type="number" />
          </Form.Item>

          <Form.Item
            name="description"
            label="Mô tả"
            rules={[{ required: true, message: 'Vui lòng nhập mô tả' }]}
          >
            <Input.TextArea rows={3} />
          </Form.Item>

          <Form.Item
            name="date"
            label="Ngày"
            rules={[{ required: true, message: 'Vui lòng chọn ngày' }]}
            initialValue={moment()}
          >
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>

          <Form.Item
            name="paymentMethod"
            label="Phương thức thanh toán"
            rules={[{ required: true, message: 'Vui lòng chọn phương thức' }]}
            initialValue="Chuyển khoản"
          >
            <Select>
              <Option value="Chuyển khoản">Chuyển khoản</Option>
              <Option value="Tiền mặt">Tiền mặt</Option>
              <Option value="Thẻ tín dụng">Thẻ tín dụng</Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="status"
            label="Trạng thái"
            rules={[{ required: true, message: 'Vui lòng chọn trạng thái' }]}
            initialValue="completed"
          >
            <Select>
              <Option value="completed">Hoàn thành</Option>
              <Option value="pending">Chờ xử lý</Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="reference"
            label="Tham chiếu"
          >
            <Input />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                {editingTransaction ? 'Cập nhật' : 'Thêm mới'}
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

export default Transactions;

