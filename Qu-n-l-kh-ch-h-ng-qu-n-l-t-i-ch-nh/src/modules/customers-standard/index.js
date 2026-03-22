import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, DatePicker, Form, Input, InputNumber, Modal, Select, Space, Table, message } from 'antd';
import { EditOutlined, PlusOutlined } from '@ant-design/icons';
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
    pagination: { current: 1, pageSize: 10, showSizeChanger: true },
  });
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState(null);
  const [form] = Form.useForm();

  const fetchData = useCallback(async () => {
    setLoading(true);
    try {
      const res = await axios.get('/api/customers', {
        params: {
          search,
          page: tableParams.pagination?.current || 1,
          pageSize: tableParams.pagination?.pageSize || 10,
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
  }, [search, tableParams.pagination?.current, tableParams.pagination?.pageSize]);

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

  const columns = useMemo(() => {
    return [
      { title: 'STT', dataIndex: 'id', key: 'id', width: 80 },
      { title: 'Tên khách hàng', dataIndex: 'name', key: 'name', width: 200 },
      { title: 'Quy mô', dataIndex: 'businessScale', key: 'businessScale', width: 120 },
      { title: 'Mã số thuế', dataIndex: 'taxCode', key: 'taxCode', width: 140 },
      { title: 'Địa chỉ', dataIndex: 'address', key: 'address', width: 220 },
      { title: 'Người đại diện', dataIndex: 'representativeName', key: 'representativeName', width: 160 },
      { title: 'Chức vụ', dataIndex: 'representativePosition', key: 'representativePosition', width: 140 },
      { title: 'Email', dataIndex: 'email', key: 'email', width: 180 },
      { title: 'Số điện thoại', dataIndex: 'phone', key: 'phone', width: 140 },
      { title: 'CCCD/Hộ chiếu', dataIndex: 'idNumber', key: 'idNumber', width: 160 },
      { title: 'Người liên hệ', dataIndex: 'contactPerson', key: 'contactPerson', width: 160 },
      { title: 'SĐT liên hệ', dataIndex: 'contactPhone', key: 'contactPhone', width: 140 },
      { title: 'Email liên hệ', dataIndex: 'contactEmail', key: 'contactEmail', width: 180 },
      { title: 'Nhu cầu', dataIndex: 'businessNeeds', key: 'businessNeeds', width: 180 },
      { title: 'Chi tiết nhu cầu', dataIndex: 'needDetail', key: 'needDetail', width: 220 },
      { title: 'Tiềm năng', dataIndex: 'potentialLevel', key: 'potentialLevel', width: 120 },
      { title: 'Ưu tiên', dataIndex: 'priority', key: 'priority', width: 120 },
      { title: 'Phân loại nguồn', dataIndex: 'sourceClassification', key: 'sourceClassification', width: 160 },
      { title: 'Nguồn NSNN', dataIndex: 'nsnnSource', key: 'nsnnSource', width: 160 },
      { title: 'Tình trạng tư vấn', dataIndex: 'consultingStatus', key: 'consultingStatus', width: 160 },
      { title: 'Số hợp đồng', dataIndex: 'contractNumber', key: 'contractNumber', width: 140 },
      {
        title: 'Giá trị hợp đồng',
        dataIndex: 'contractValue',
        key: 'contractValue',
        width: 160,
        render: (v) => (typeof v === 'number' ? v.toLocaleString('vi-VN') : v || ''),
      },
      { title: 'Tình trạng hợp đồng', dataIndex: 'contractStatus', key: 'contractStatus', width: 160 },
      { title: 'Ngày bắt đầu', dataIndex: 'startDate', key: 'startDate', width: 130 },
      { title: 'Ngày kết thúc', dataIndex: 'endDate', key: 'endDate', width: 130 },
      { title: 'Số ngày triển khai', dataIndex: 'implementationDays', key: 'implementationDays', width: 150 },
      { title: 'Người tạo', dataIndex: 'createdByName', key: 'createdByName', width: 160 },
      { title: 'Người cập nhật', dataIndex: 'updatedByName', key: 'updatedByName', width: 160 },
      { title: 'Ngày cập nhật', dataIndex: 'updatedAt', key: 'updatedAt', width: 130 },
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
            width: 90,
            render: (_, record) => (
              <Button icon={<EditOutlined />} onClick={() => openEdit(record)} />
            ),
          }
        : null,
    ].filter(Boolean);
  }, [canEdit]);

  return (
    <div>
      <Space style={{ marginBottom: 12 }}>
        <Input
          placeholder="Tìm theo tên/email/sđt"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onPressEnter={() => fetchData()}
          style={{ width: 260 }}
        />
        <Button onClick={() => fetchData()}>Tìm</Button>
        {canEdit && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
            Thêm khách hàng
          </Button>
        )}
      </Space>

      <Table
        rowKey="id"
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={tableParams.pagination}
        onChange={(pagination) => setTableParams((prev) => ({ ...prev, pagination }))}
        scroll={{ x: 2400 }}
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
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Tên khách hàng" rules={[{ required: true, message: 'Vui lòng nhập tên' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="businessScale" label="Quy mô doanh nghiệp">
            <Input />
          </Form.Item>
          <Form.Item name="taxCode" label="Mã số thuế">
            <Input />
          </Form.Item>
          <Form.Item name="address" label="Địa chỉ">
            <Input />
          </Form.Item>
          <Form.Item name="representativeName" label="Người đại diện">
            <Input />
          </Form.Item>
          <Form.Item name="representativePosition" label="Chức vụ">
            <Input />
          </Form.Item>
          <Form.Item name="email" label="Email">
            <Input />
          </Form.Item>
          <Form.Item name="phone" label="Số điện thoại">
            <Input />
          </Form.Item>
          <Form.Item name="idNumber" label="CCCD/Hộ chiếu">
            <Input />
          </Form.Item>
          <Form.Item name="contactPerson" label="Người liên hệ">
            <Input />
          </Form.Item>
          <Form.Item name="contactPhone" label="SĐT người liên hệ">
            <Input />
          </Form.Item>
          <Form.Item name="contactEmail" label="Email người liên hệ">
            <Input />
          </Form.Item>
          <Form.Item name="businessNeeds" label="Nhu cầu">
            <Input />
          </Form.Item>
          <Form.Item name="needDetail" label="Chi tiết nhu cầu">
            <Input.TextArea rows={3} />
          </Form.Item>
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
          <Form.Item name="nsnnSource" label="Nguồn NSNN">
            <Input />
          </Form.Item>
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
          <Form.Item name="contractNumber" label="Số hợp đồng">
            <Input />
          </Form.Item>
          <Form.Item name="contractValue" label="Giá trị hợp đồng">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
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
          <Form.Item name="startDate" label="Ngày bắt đầu">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="endDate" label="Ngày kết thúc">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="implementationDays" label="Số ngày triển khai">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
          <Form.Item name="documentLink" label="Link hồ sơ giấy tờ">
            <Input />
          </Form.Item>
          <Form.Item name="productLink" label="Link sản phẩm">
            <Input />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default CustomersStandard;

