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
  Row,
  Col,
  InputNumber,
} from 'antd';
import {
  EditOutlined,
  DeleteOutlined,
  PlusOutlined,
  SearchOutlined,
  FileTextOutlined,
  PrinterOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import moment from 'moment';
import ContractTemplate from './ContractTemplate';
import { exportContractToWord } from './ContractExport';
import './index.css';

const { Option } = Select;
const { TextArea } = Input;

const Contracts = () => {
  const [contracts, setContracts] = useState([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [isModalVisible, setIsModalVisible] = useState(false);
  const [isPreviewVisible, setIsPreviewVisible] = useState(false);
  const [editingContract, setEditingContract] = useState(null);
  const [previewContract, setPreviewContract] = useState(null);
  const [form] = Form.useForm();

  const fetchContracts = useCallback(async () => {
    setLoading(true);
    try {
      const response = await axios.get('/api/contracts', {
        params: { search, page },
      });
      setContracts(response.data.contracts);
      setTotal(response.data.contractCount);
    } catch (error) {
      message.error('Lỗi khi tải dữ liệu hợp đồng');
    } finally {
      setLoading(false);
    }
  }, [search, page]);

  useEffect(() => {
    fetchContracts();
  }, [fetchContracts]);

  const handleAdd = () => {
    setEditingContract(null);
    form.resetFields();
    form.setFieldsValue({
      contractType: 'Hợp đồng lao động',
      contractDate: moment(),
      effectiveDate: moment(),
      status: 'active',
    });
    setIsModalVisible(true);
  };

  const handleEdit = (record) => {
    setEditingContract(record);
    form.setFieldsValue({
      ...record,
      contractDate: record.contractDate ? moment(record.contractDate) : null,
      effectiveDate: record.effectiveDate ? moment(record.effectiveDate) : null,
      expiryDate: record.expiryDate ? moment(record.expiryDate) : null,
    });
    setIsModalVisible(true);
  };

  const handlePreview = (record) => {
    setPreviewContract(record);
    setIsPreviewVisible(true);
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`/api/contracts/${id}`);
      message.success('Xóa hợp đồng thành công');
      fetchContracts();
    } catch (error) {
      message.error('Lỗi khi xóa hợp đồng');
    }
  };

  const handleSubmit = async (values) => {
    try {
      const submitData = {
        ...values,
        contractDate: values.contractDate ? values.contractDate.format('YYYY-MM-DD') : null,
        effectiveDate: values.effectiveDate ? values.effectiveDate.format('YYYY-MM-DD') : null,
        expiryDate: values.expiryDate ? values.expiryDate.format('YYYY-MM-DD') : null,
      };
      if (editingContract) {
        await axios.put(`/api/contracts/${editingContract.id}`, submitData);
        message.success('Cập nhật hợp đồng thành công');
      } else {
        await axios.post('/api/contracts', submitData);
        message.success('Thêm hợp đồng thành công');
      }
      setIsModalVisible(false);
      form.resetFields();
      fetchContracts();
    } catch (error) {
      message.error('Lỗi khi lưu hợp đồng');
    }
  };

  const handlePrint = () => {
    window.print();
  };

  const handleExportWord = async () => {
    if (previewContract) {
      try {
        await exportContractToWord(previewContract);
        message.success('Xuất file Word thành công!');
      } catch (error) {
        message.error('Lỗi khi xuất file Word');
      }
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
      title: 'Số hợp đồng',
      dataIndex: 'contractNumber',
      key: 'contractNumber',
    },
    {
      title: 'Loại hợp đồng',
      dataIndex: 'contractType',
      key: 'contractType',
    },
    {
      title: 'Bên A',
      key: 'partyA',
      render: (_, record) => record.partyA?.name || '-',
    },
    {
      title: 'Bên B',
      key: 'partyB',
      render: (_, record) => record.partyB?.name || '-',
    },
    {
      title: 'Ngày ký',
      dataIndex: 'contractDate',
      key: 'contractDate',
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (status) => (
        <Tag color={status === 'active' ? 'green' : 'red'}>
          {status === 'active' ? 'Có hiệu lực' : 'Hết hiệu lực'}
        </Tag>
      ),
    },
    {
      title: 'Thao tác',
      key: 'action',
      width: 200,
      render: (_, record) => (
        <Space size="middle">
          <Button
            type="link"
            icon={<FileTextOutlined />}
            onClick={() => handlePreview(record)}
          >
            Xem
          </Button>
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
    <div className="contracts-container">
      <div className="contracts-header">
        <h2>Quản lý hợp đồng</h2>
        <Space>
          <Input
            placeholder="Tìm kiếm hợp đồng..."
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
            Thêm hợp đồng
          </Button>
        </Space>
      </div>

      <Table
        columns={columns}
        dataSource={contracts}
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
        title={editingContract ? 'Sửa hợp đồng' : 'Thêm hợp đồng'}
        open={isModalVisible}
        onCancel={() => {
          setIsModalVisible(false);
          form.resetFields();
        }}
        footer={null}
        width={900}
        style={{ top: 20 }}
      >
        <Form
          form={form}
          layout="vertical"
          scrollToFirstError
          onFinish={handleSubmit}
          className="contract-form"
        >
          <Form.Item
            name="contractType"
            label="Loại hợp đồng"
            rules={[{ required: true, message: 'Vui lòng chọn loại hợp đồng' }]}
          >
            <Select>
              <Option value="Hợp đồng lao động">Hợp đồng lao động</Option>
              <Option value="Hợp đồng dịch vụ">Hợp đồng dịch vụ</Option>
              <Option value="Hợp đồng mua bán">Hợp đồng mua bán</Option>
              <Option value="Hợp đồng hợp tác">Hợp đồng hợp tác</Option>
            </Select>
          </Form.Item>

          <Form.Item
            name="contractNumber"
            label="Số hợp đồng"
            rules={[{ required: true, message: 'Vui lòng nhập số hợp đồng' }]}
          >
            <Input placeholder="VD: HD-2024-001" />
          </Form.Item>

          <div className="form-section">
            <h3>Bên A (Bên thuê/Bên mua)</h3>
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  name={['partyA', 'name']}
                  label="Tên công ty"
                  rules={[{ required: true, message: 'Vui lòng nhập tên công ty' }]}
                >
                  <Input />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name={['partyA', 'taxCode']}
                  label="Mã số thuế"
                >
                  <Input />
                </Form.Item>
              </Col>
            </Row>
            <Form.Item
              name={['partyA', 'address']}
              label="Địa chỉ"
            >
              <Input.TextArea rows={2} />
            </Form.Item>
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  name={['partyA', 'representative']}
                  label="Người đại diện"
                >
                  <Input />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name={['partyA', 'position']}
                  label="Chức vụ"
                >
                  <Input />
                </Form.Item>
              </Col>
            </Row>
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  name={['partyA', 'phone']}
                  label="Số điện thoại"
                >
                  <Input />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name={['partyA', 'email']}
                  label="Email"
                >
                  <Input />
                </Form.Item>
              </Col>
            </Row>
          </div>

          <div className="form-section">
            <h3>Bên B (Bên được thuê/Bên bán)</h3>
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  name={['partyB', 'name']}
                  label="Tên cá nhân/Công ty"
                  rules={[{ required: true, message: 'Vui lòng nhập tên' }]}
                >
                  <Input />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name={['partyB', 'idCard']}
                  label="CMND/CCCD/Mã số thuế"
                >
                  <Input />
                </Form.Item>
              </Col>
            </Row>
            <Form.Item
              name={['partyB', 'address']}
              label="Địa chỉ"
            >
              <Input.TextArea rows={2} />
            </Form.Item>
            <Row gutter={16}>
              <Col span={12}>
                <Form.Item
                  name={['partyB', 'phone']}
                  label="Số điện thoại"
                >
                  <Input />
                </Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item
                  name={['partyB', 'email']}
                  label="Email"
                >
                  <Input />
                </Form.Item>
              </Col>
            </Row>
            <Form.Item
              name={['partyB', 'position']}
              label="Chức vụ/Vị trí"
            >
              <Input />
            </Form.Item>
          </div>

          <Row gutter={16}>
            <Col span={8}>
              <Form.Item
                name="contractDate"
                label="Ngày ký hợp đồng"
                rules={[{ required: true, message: 'Vui lòng chọn ngày ký' }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                name="effectiveDate"
                label="Ngày có hiệu lực"
                rules={[{ required: true, message: 'Vui lòng chọn ngày có hiệu lực' }]}
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                name="expiryDate"
                label="Ngày hết hạn"
              >
                <DatePicker style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item
            name="terms"
            label="Điều khoản hợp đồng"
            tooltip="Mỗi điều khoản một dòng, sẽ được đánh số tự động"
          >
            <TextArea rows={6} placeholder="Ví dụ:&#10;Bên B cam kết thực hiện đầy đủ các công việc được giao&#10;Bên A cam kết trả lương đúng hạn&#10;Hai bên tuân thủ các quy định của pháp luật" />
          </Form.Item>

          {form.getFieldValue('contractType') === 'Hợp đồng lao động' && (
            <>
              <Row gutter={16}>
                <Col span={12}>
                  <Form.Item
                    name="salary"
                    label="Mức lương (VND)"
                  >
                    <InputNumber
                      style={{ width: '100%' }}
                      formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                      parser={(value) => value.replace(/\$\s?|(,*)/g, '')}
                    />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    name="workingHours"
                    label="Thời gian làm việc"
                  >
                    <Input placeholder="VD: 8 giờ/ngày, 5 ngày/tuần" />
                  </Form.Item>
                </Col>
              </Row>
              <Form.Item
                name="jobDescription"
                label="Mô tả công việc"
              >
                <TextArea rows={3} />
              </Form.Item>
            </>
          )}

          {form.getFieldValue('contractType') === 'Hợp đồng dịch vụ' && (
            <>
              <Form.Item
                name="serviceDescription"
                label="Mô tả dịch vụ"
              >
                <TextArea rows={3} />
              </Form.Item>
              <Row gutter={16}>
                <Col span={12}>
                  <Form.Item
                    name="serviceFee"
                    label="Phí dịch vụ (VND)"
                  >
                    <InputNumber
                      style={{ width: '100%' }}
                      formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                      parser={(value) => value.replace(/\$\s?|(,*)/g, '')}
                    />
                  </Form.Item>
                </Col>
                <Col span={12}>
                  <Form.Item
                    name="paymentTerms"
                    label="Điều kiện thanh toán"
                  >
                    <Input placeholder="VD: Thanh toán 50% khi ký, 50% khi hoàn thành" />
                  </Form.Item>
                </Col>
              </Row>
            </>
          )}

          {form.getFieldValue('contractType') === 'Hợp đồng mua bán' && (
            <>
              <Form.Item
                name="productDescription"
                label="Mô tả sản phẩm"
              >
                <TextArea rows={3} />
              </Form.Item>
              <Row gutter={16}>
                <Col span={8}>
                  <Form.Item
                    name="totalAmount"
                    label="Tổng giá trị (VND)"
                  >
                    <InputNumber
                      style={{ width: '100%' }}
                      formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                      parser={(value) => value.replace(/\$\s?|(,*)/g, '')}
                    />
                  </Form.Item>
                </Col>
                <Col span={8}>
                  <Form.Item
                    name="paymentTerms"
                    label="Điều kiện thanh toán"
                  >
                    <Input placeholder="VD: Thanh toán 100% khi giao hàng" />
                  </Form.Item>
                </Col>
                <Col span={8}>
                  <Form.Item
                    name="deliveryTerms"
                    label="Điều kiện giao hàng"
                  >
                    <Input placeholder="VD: Giao hàng trong vòng 7 ngày" />
                  </Form.Item>
                </Col>
              </Row>
            </>
          )}

          <Form.Item
            name="notes"
            label="Ghi chú"
          >
            <TextArea rows={2} />
          </Form.Item>

          <Form.Item
            name="status"
            label="Trạng thái"
            initialValue="active"
          >
            <Select>
              <Option value="active">Có hiệu lực</Option>
              <Option value="expired">Hết hiệu lực</Option>
            </Select>
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                {editingContract ? 'Cập nhật' : 'Thêm mới'}
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

      <Modal
        title="Xem hợp đồng"
        open={isPreviewVisible}
        onCancel={() => setIsPreviewVisible(false)}
        footer={[
          <Button key="export" type="primary" icon={<FileTextOutlined />} onClick={handleExportWord}>
            Xuất Word
          </Button>,
          <Button key="print" icon={<PrinterOutlined />} onClick={handlePrint}>
            In hợp đồng
          </Button>,
          <Button key="close" onClick={() => setIsPreviewVisible(false)}>
            Đóng
          </Button>,
        ]}
        width={900}
        style={{ top: 20 }}
      >
        {previewContract && (
          <div className="contract-preview">
            <ContractTemplate contract={previewContract} />
          </div>
        )}
      </Modal>
    </div>
  );
};

export default Contracts;

