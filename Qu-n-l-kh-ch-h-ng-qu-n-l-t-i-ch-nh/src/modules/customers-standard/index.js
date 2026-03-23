import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, DatePicker, Form, Input, InputNumber, Modal, Popconfirm, Row, Select, Space, Table, message } from 'antd';
import { DeleteOutlined, EditOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import axios from 'axios';
import dayjs from 'dayjs';
import { useAuth } from '../../context/AuthContext';
import { handleApiError } from '../../utils/errorHelper';

const CustomersStandard = () => {
  const { isAdmin } = useAuth();
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState([]);
  const [search, setSearch] = useState('');
  const [tableParams, setTableParams] = useState({
    pagination: { current: 1, pageSize: 10, showSizeChanger: true, showQuickJumper: true },
    sortField: 'id',
    sortOrder: 'descend',
  });
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form] = Form.useForm();

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

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const cleanFilters = {};
      if (tableParams.filters) {
        Object.keys(tableParams.filters).forEach((key) => {
          if (tableParams.filters[key] && tableParams.filters[key].length > 0) {
            cleanFilters[key] = tableParams.filters[key][0];
          }
        });
      }

      const res = await axios.get('/api/customers', {
        params: {
          search,
          page: tableParams.pagination?.current || 1,
          pageSize: tableParams.pagination?.pageSize || 10,
          sortField: tableParams.sortField,
          sortOrder: tableParams.sortOrder,
          ...cleanFilters,
        },
      });
      setItems(res.data.customers || []);
      setTableParams((prev) => ({
        ...prev,
        pagination: {
          ...prev.pagination,
          total: res.data.customerCount || 0,
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
    tableParams.filters,
  ]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const canEdit = useMemo(() => {
    return isAdmin && isAdmin();
  }, [isAdmin]);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    setOpen(true);
  };

  const openEdit = (record) => {
    setEditing(record);
    form.resetFields();
    form.setFieldsValue({
      ...record,
      startDate: record.startDate ? dayjs(record.startDate) : null,
      endDate: record.endDate ? dayjs(record.endDate) : null,
    });
    setOpen(true);
  };

  const onSubmit = async () => {
    try {
      const values = await form.validateFields();
      const payload = {
        ...values,
        startDate: values.startDate ? values.startDate.format('YYYY-MM-DD') : undefined,
        endDate: values.endDate ? values.endDate.format('YYYY-MM-DD') : undefined,
        schemaVersion: 'standard',
      };

      if (editing) {
        await axios.put(`/api/customers/${editing.id}`, payload);
        message.success('Đã cập nhật khách hàng');
      } else {
        await axios.post('/api/customers', payload);
        message.success('Đã tạo khách hàng');
      }

      setOpen(false);
      setEditing(null);
      form.resetFields();
      fetchData();
    } catch (error) {
      if (error?.errorFields) return;
      handleApiError(error, 'Không thể lưu khách hàng');
    }
  };

  const handleDelete = async (id) => {
    try {
      await axios.delete(`/api/customers/${id}`);
      message.success('Đã xóa khách hàng');
      fetchData();
    } catch (error) {
      handleApiError(error, 'Không thể xóa khách hàng');
    }
  };

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

  const columns = useMemo(() => {
    return [
      { title: 'STT', dataIndex: 'id', key: 'id', width: 80, fixed: 'left', sorter: true, ...getColumnSearchProps('id') },
      { title: 'Tên khách hàng', dataIndex: 'name', key: 'name', width: 220, fixed: 'left', sorter: true, ellipsis: true, ...getColumnSearchProps('name') },
      { title: 'Quy mô', dataIndex: 'businessScale', key: 'businessScale', width: 120, sorter: true, ellipsis: true, ...getColumnSearchProps('businessScale') },
      { title: 'Mã số thuế', dataIndex: 'taxCode', key: 'taxCode', width: 140, sorter: true, ellipsis: true, ...getColumnSearchProps('taxCode') },
      { title: 'Địa chỉ', dataIndex: 'address', key: 'address', width: 220, sorter: true, ellipsis: true, ...getColumnSearchProps('address') },
      { title: 'Người đại diện', dataIndex: 'representativeName', key: 'representativeName', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('representativeName') },
      { title: 'Chức vụ', dataIndex: 'representativePosition', key: 'representativePosition', width: 140, sorter: true, ellipsis: true, ...getColumnSearchProps('representativePosition') },
      { title: 'Email', dataIndex: 'email', key: 'email', width: 200, sorter: true, ellipsis: true, ...getColumnSearchProps('email') },
      { title: 'Số điện thoại', dataIndex: 'phone', key: 'phone', width: 140, sorter: true, ellipsis: true, ...getColumnSearchProps('phone') },
      { title: 'CCCD/Hộ chiếu', dataIndex: 'idNumber', key: 'idNumber', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('idNumber') },
      { title: 'Người liên hệ', dataIndex: 'contactPerson', key: 'contactPerson', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('contactPerson') },
      { title: 'SĐT liên hệ', dataIndex: 'contactPhone', key: 'contactPhone', width: 140, sorter: true, ellipsis: true, ...getColumnSearchProps('contactPhone') },
      { title: 'Email liên hệ', dataIndex: 'contactEmail', key: 'contactEmail', width: 200, sorter: true, ellipsis: true, ...getColumnSearchProps('contactEmail') },
      { title: 'Nhu cầu', dataIndex: 'businessNeeds', key: 'businessNeeds', width: 180, sorter: true, ellipsis: true, ...getColumnSearchProps('businessNeeds') },
      { title: 'Chi tiết nhu cầu', dataIndex: 'needDetail', key: 'needDetail', width: 240, sorter: true, ellipsis: true, ...getColumnSearchProps('needDetail') },
      { title: 'Tiềm năng', dataIndex: 'potentialLevel', key: 'potentialLevel', width: 120, sorter: true, ellipsis: true, ...getColumnSearchProps('potentialLevel') },
      { title: 'Ưu tiên', dataIndex: 'priority', key: 'priority', width: 120, sorter: true, ellipsis: true, ...getColumnSearchProps('priority') },
      { title: 'Phân loại nguồn', dataIndex: 'sourceClassification', key: 'sourceClassification', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('sourceClassification') },
      { title: 'Nguồn NSNN', dataIndex: 'nsnnSource', key: 'nsnnSource', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('nsnnSource') },
      { title: 'Tình trạng tư vấn', dataIndex: 'consultingStatus', key: 'consultingStatus', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('consultingStatus') },
      { title: 'Số hợp đồng', dataIndex: 'contractNumber', key: 'contractNumber', width: 140, sorter: true, ellipsis: true, ...getColumnSearchProps('contractNumber') },
      {
        title: 'Giá trị hợp đồng',
        dataIndex: 'contractValue',
        key: 'contractValue',
        width: 160,
        sorter: true,
        ...getColumnSearchProps('contractValue'),
        render: (v) => (typeof v === 'number' ? v.toLocaleString('vi-VN') : v || ''),
      },
      { title: 'Tình trạng hợp đồng', dataIndex: 'contractStatus', key: 'contractStatus', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('contractStatus') },
      { title: 'Ngày bắt đầu', dataIndex: 'startDate', key: 'startDate', width: 130, sorter: true, ellipsis: true, ...getColumnSearchProps('startDate') },
      { title: 'Ngày kết thúc', dataIndex: 'endDate', key: 'endDate', width: 130, sorter: true, ellipsis: true, ...getColumnSearchProps('endDate') },
      { title: 'Số ngày triển khai', dataIndex: 'implementationDays', key: 'implementationDays', width: 150, sorter: true, ellipsis: true, ...getColumnSearchProps('implementationDays') },
      { title: 'Người tạo', dataIndex: 'createdByName', key: 'createdByName', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('createdByName') },
      { title: 'Người cập nhật', dataIndex: 'updatedByName', key: 'updatedByName', width: 160, sorter: true, ellipsis: true, ...getColumnSearchProps('updatedByName') },
      { title: 'Ngày cập nhật', dataIndex: 'updatedAt', key: 'updatedAt', width: 130, sorter: true, ellipsis: true, ...getColumnSearchProps('updatedAt') },
      {
        title: 'Link hồ sơ giấy tờ',
        dataIndex: 'documentLink',
        key: 'documentLink',
        width: 200,
        render: (v) => (v ? <a href={v} target="_blank" rel="noreferrer">Mở</a> : ''),
      },
      {
        title: 'Link sản phẩm',
        dataIndex: 'productLink',
        key: 'productLink',
        width: 160,
        render: (v) => (v ? <a href={v} target="_blank" rel="noreferrer">Mở</a> : ''),
      },
      canEdit
        ? {
            title: 'Thao tác',
            key: 'actions',
            fixed: 'right',
            width: 88,
            render: (_, record) => (
              <Space size={6}>
                <Button icon={<EditOutlined />} onClick={() => openEdit(record)} />
                <Popconfirm title="Xóa khách hàng này?" okText="Xóa" cancelText="Hủy" onConfirm={() => handleDelete(record.id)}>
                  <Button danger icon={<DeleteOutlined />} />
                </Popconfirm>
              </Space>
            ),
          }
        : null,
    ].filter(Boolean);
  }, [canEdit]);

  return (
    <Card title="Khách hàng" bodyStyle={{ padding: 12 }}>
      <Row gutter={12} align="middle" style={{ marginBottom: 12 }}>
        <Col flex="auto">
          <Input
            allowClear
            placeholder="Tìm theo tên/email/sđt"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onPressEnter={() => fetchData()}
            style={{ maxWidth: 420 }}
          />
        </Col>
        <Col>
          <Space>
            <Button icon={<SearchOutlined />} onClick={() => fetchData()}>
              Tìm
            </Button>
            {canEdit && (
              <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
                Thêm khách hàng
              </Button>
            )}
          </Space>
        </Col>
      </Row>

      <Table
        rowKey="id"
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={tableParams.pagination}
        onChange={handleTableChange}
        size="small"
        bordered
        sticky
        scroll={{ x: 2600, y: 'calc(100vh - 290px)' }}
      />

      <Modal
        open={open}
        title={editing ? 'Cập nhật khách hàng' : 'Thêm khách hàng'}
        onCancel={() => {
          setOpen(false);
          setEditing(null);
        }}
        onOk={onSubmit}
        okText="Lưu"
        destroyOnClose
        width={900}
      >
        <Form form={form} layout="vertical">
          <Row gutter={16}>
            <Col xs={24}>
              <Form.Item name="name" label="Tên khách hàng" rules={[{ required: true, message: 'Vui lòng nhập tên' }]}>
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="businessScale" label="Quy mô doanh nghiệp">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="taxCode" label="Mã số thuế">
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24}>
              <Form.Item name="address" label="Địa chỉ">
                <Input.TextArea rows={2} />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="representativeName" label="Người đại diện">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="representativePosition" label="Chức vụ">
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="email" label="Email">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="phone" label="Số điện thoại">
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="idNumber" label="CCCD/Hộ chiếu">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="contactPerson" label="Người liên hệ">
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="contactPhone" label="SĐT người liên hệ">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="contactEmail" label="Email người liên hệ">
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="businessNeeds" label="Nhu cầu">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="potentialLevel" label="Mức độ tiềm năng">
                <Select
                  allowClear
                  options={[
                    { value: 'Thấp', label: 'Thấp' },
                    { value: 'Trung bình', label: 'Trung bình' },
                    { value: 'Cao', label: 'Cao' },
                  ]}
                />
              </Form.Item>
            </Col>

            <Col xs={24}>
              <Form.Item name="needDetail" label="Chi tiết nhu cầu">
                <Input.TextArea rows={3} />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="priority" label="Ưu tiên">
                <Select
                  allowClear
                  options={[
                    { value: 'Mức 1', label: 'Mức 1' },
                    { value: 'Mức 2', label: 'Mức 2' },
                    { value: 'Mức 3', label: 'Mức 3' },
                  ]}
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="sourceClassification" label="Phân loại nguồn">
                <Select
                  allowClear
                  options={[
                    { value: 'NSNN', label: 'NSNN' },
                    { value: 'Đối tác', label: 'Đối tác' },
                    { value: 'Marketing', label: 'Marketing' },
                    { value: 'Vãng lai', label: 'Vãng lai' },
                  ]}
                />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="nsnnSource" label="Nguồn NSNN">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="consultingStatus" label="Tình trạng tư vấn">
                <Select
                  allowClear
                  options={[
                    { value: 'Mới tiếp nhận', label: 'Mới tiếp nhận' },
                    { value: 'Đang tư vấn', label: 'Đang tư vấn' },
                    { value: 'Đã tư vấn', label: 'Đã tư vấn' },
                  ]}
                />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="contractNumber" label="Số hợp đồng">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="contractValue" label="Giá trị hợp đồng">
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="contractStatus" label="Tình trạng hợp đồng">
                <Select
                  allowClear
                  options={[
                    { value: 'Mới', label: 'Mới' },
                    { value: 'Đang thực hiện', label: 'Đang thực hiện' },
                    { value: 'Sắp hết hạn', label: 'Sắp hết hạn' },
                    { value: 'Đã thanh lý', label: 'Đã thanh lý' },
                  ]}
                />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="implementationDays" label="Số ngày triển khai">
                <InputNumber style={{ width: '100%' }} min={0} />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item name="startDate" label="Ngày bắt đầu">
                <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
              </Form.Item>
            </Col>
            <Col xs={24} md={12}>
              <Form.Item name="endDate" label="Ngày kết thúc">
                <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
              </Form.Item>
            </Col>

            <Col xs={24}>
              <Form.Item name="documentLink" label="Link hồ sơ giấy tờ">
                <Input />
              </Form.Item>
            </Col>
            <Col xs={24}>
              <Form.Item name="productLink" label="Link sản phẩm">
                <Input />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </Card>
  );
};

export default CustomersStandard;
