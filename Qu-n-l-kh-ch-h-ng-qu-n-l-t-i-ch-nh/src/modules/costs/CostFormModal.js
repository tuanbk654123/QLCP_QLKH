import React, { useEffect, useState } from 'react';
import {
  Input,
  Button,
  Space,
  Tag,
  Modal,
  Form,
  message,
  notification,
  Popconfirm,
  Select,
  DatePicker,
  Tabs,
  Upload,
  Row,
  Col,
} from 'antd';
import {
  UploadOutlined,
  FileTextOutlined,
  BellOutlined,
} from '@ant-design/icons';
import axios from 'axios';
import dayjs from 'dayjs';
import { handleApiError } from '../../utils/errorHelper';

const { Option } = Select;
const { TextArea } = Input;
const API_BASE_URL = 'http://localhost:58457';

const CostFormModal = ({
  visible,
  onCancel,
  onSuccess,
  editingCost,
  user,
  users,
  fieldPermissions,
  isNotificationView,
}) => {
  const [form] = Form.useForm();
  const [rejectReasonModalVisible, setRejectReasonModalVisible] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [activeTab, setActiveTab] = useState('1');
  const [projectCodes, setProjectCodes] = useState([]);
  const [projectCodesLoading, setProjectCodesLoading] = useState(false);
  const [projectCodeNewValue, setProjectCodeNewValue] = useState('');
  const [isProjectCodeManageVisible, setIsProjectCodeManageVisible] = useState(false);
  const [projectCodeManageDrafts, setProjectCodeManageDrafts] = useState({});

  const onFinishFailed = ({ errorFields }) => {
    if (errorFields.length > 0) {
      const firstErrorField = errorFields[0].name[0];
      const fieldToTab = {
        requester: '1', department: '1', requestDate: '1', transactionDate: '1', projectCode: '1', priority: '1',
        transactionType: '1', transactionObject: '1', notificationRecipients: '1', taxCode: '1', content: '1', description: '1',
        amountBeforeTax: '2', taxRate: '2', totalAmount: '2', paymentMethod: '2', bank: '2', accountNumber: '2',
        voucherType: '3', voucherNumber: '3', voucherDate: '3', invoiceNumber: '3', invoiceSeries: '3', vatAmount: '3', attachment: '3',
        paymentStatus: '4', rejectionReason: '4', adjustmentReason: '4', riskFlag: '4', note: '4',
        vendorName: '5', vendorTaxCode: '5', costCategory: '5', costSubCategory: '5', costCenter: '5',
        payDate: '6', dueDate: '6'
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

  useEffect(() => {
    if (visible) {
      setActiveTab('1');
      setProjectCodesLoading(true);
      axios
        .get('/api/project-codes')
        .then((res) => setProjectCodes(res.data.items || []))
        .catch(() => setProjectCodes([]))
        .finally(() => setProjectCodesLoading(false));
      if (editingCost) {
        const formattedRecord = {
          ...editingCost,
          requestDate: editingCost.requestDate ? dayjs(editingCost.requestDate) : null,
          transactionDate: editingCost.transactionDate ? dayjs(editingCost.transactionDate) : null,
          voucherDate: editingCost.voucherDate ? dayjs(editingCost.voucherDate) : null,
          payDate: editingCost.payDate ? dayjs(editingCost.payDate) : null,
          dueDate: editingCost.dueDate ? dayjs(editingCost.dueDate) : null,
          attachments: Array.isArray(editingCost.attachments) 
            ? editingCost.attachments 
            : (editingCost.attachment ? [{ path: editingCost.attachment, name: 'Đính kèm' }] : []),
          notificationRecipients: [] // Clear notification recipients to allow re-selection
        };
        form.setFieldsValue(formattedRecord);
      } else {
        form.resetFields();
        form.setFieldsValue({
          requestDate: dayjs(),
          taxRate: '10%',
          paymentStatus: 'Đợi duyệt',
        });
      }
    }
  }, [visible, editingCost, form]);

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

  const canEditField = (field) => {
    // Nếu là kế toán và đang xem từ thông báo (chờ duyệt), cho phép sửa thông tin chứng từ
    if (isNotificationView) {
       const isAccountant = user?.role === 'admin' || user?.role === 'ke_toan' || user?.role === 'accountant';
       const isWaitingAccountant = editingCost?.paymentStatus === 'Giám đốc duyệt';

       if (isAccountant && isWaitingAccountant) {
          const accountantAllowed = [
              'voucherType', 'voucherNumber', 'voucherDate', 'invoiceNumber', 'invoiceSeries', 'vatAmount', 'attachment', 'attachments',
              'payDate', 'dueDate', 'bank', 'accountNumber', 'paymentMethod'
          ];
          if (accountantAllowed.includes(field)) return true;
       }

      const allowed = ['notificationRecipients', 'attachments', 'attachment', 'adjustmentReason', 'note', 'payDate', 'dueDate'];
      if (allowed.includes(field)) return true;
      return false;
    }

    const key = mapFieldToPermissionKey(field);
    const level = fieldPermissions[key];
    return level === 'W' || level === 'A';
  };

  const canManageProjectCodes = user?.role === 'admin';

  const fetchProjectCodes = async () => {
    setProjectCodesLoading(true);
    try {
      const res = await axios.get('/api/project-codes');
      setProjectCodes(res.data.items || []);
    } catch (error) {
      setProjectCodes([]);
    } finally {
      setProjectCodesLoading(false);
    }
  };

  const addProjectCode = async () => {
    const nextValue = projectCodeNewValue.trim();
    if (!nextValue) return;
    try {
      await axios.post('/api/project-codes', { code: nextValue });
      setProjectCodeNewValue('');
      await fetchProjectCodes();
      message.success('Đã thêm mã dự án');
    } catch (error) {
      handleApiError(error, 'Không thể thêm mã dự án');
    }
  };

  const openProjectCodeManage = () => {
    const drafts = {};
    (projectCodes || []).forEach((x) => {
      drafts[x.id] = x.code;
    });
    setProjectCodeManageDrafts(drafts);
    setIsProjectCodeManageVisible(true);
  };

  const saveProjectCode = async (id) => {
    const nextValue = (projectCodeManageDrafts[id] || '').trim();
    if (!nextValue) return;
    try {
      await axios.put(`/api/project-codes/${id}`, { code: nextValue });
      await fetchProjectCodes();
      message.success('Đã cập nhật mã dự án');
    } catch (error) {
      handleApiError(error, 'Không thể cập nhật mã dự án');
    }
  };

  const deleteProjectCode = async (id) => {
    try {
      await axios.delete(`/api/project-codes/${id}`);
      await fetchProjectCodes();
      message.success('Đã xóa mã dự án');
    } catch (error) {
      handleApiError(error, 'Không thể xóa mã dự án');
    }
  };

  const calculateTotal = (changedValues, allValues) => {
    if (changedValues.amountBeforeTax || changedValues.taxRate) {
      const amount = parseFloat(allValues.amountBeforeTax) || 0;
      let rate = 0;
      if (allValues.taxRate === '10%') rate = 0.1;
      else if (allValues.taxRate === '8%') rate = 0.08;
      else if (allValues.taxRate === '5%') rate = 0.05;
      
      const vat = amount * rate;
      const total = amount + vat;
      form.setFieldsValue({ vatAmount: vat });
      form.setFieldsValue({ totalAmount: total });
    }
  };

  const formatDateValue = (value) => {
    if (!value) return null;
    if (typeof value?.format === 'function') return value.format('YYYY-MM-DD');
    if (typeof value === 'string') {
      const parsed = dayjs(value);
      return parsed.isValid() ? parsed.format('YYYY-MM-DD') : null;
    }
    const parsed = dayjs(value);
    return parsed.isValid() ? parsed.format('YYYY-MM-DD') : null;
  };

  const handleUpload = async (options) => {
    const { onSuccess, onError, file } = options;
    const formData = new FormData();
    formData.append('file', file);

    try {
      const response = await axios.post('/api/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      
      onSuccess(response.data);
      message.success('Tải file thành công');
      const current = form.getFieldValue('attachments') || [];
      const next = [...current, { path: response.data.path, name: response.data.originalName }];
      form.setFieldsValue({ attachments: next });
    } catch (err) {
      onError({ err });
      message.error('Tải file thất bại');
    }
  };

  const handleApproveAction = () => {
    // Validate trước khi thực hiện
    form.validateFields().then(values => {
        Modal.confirm({
            title: 'Xác nhận duyệt',
            content: 'Bạn có chắc chắn muốn duyệt phiếu chi này?',
            okText: 'Duyệt',
            cancelText: 'Hủy',
            onOk: async () => {
            try {
                // 1. Lưu thông tin phiếu (PUT) để cập nhật các chỉnh sửa (nếu có)
                // Format lại date
                const formattedValues = {
                    ...values,
                    requestDate: formatDateValue(values.requestDate),
                    transactionDate: formatDateValue(values.transactionDate),
                    voucherDate: formatDateValue(values.voucherDate),
                    payDate: formatDateValue(values.payDate),
                    dueDate: formatDateValue(values.dueDate),
                };
                
                // Giữ nguyên status hiện tại khi update data (việc đổi status sẽ do API approve làm)
                // Tuy nhiên, nếu user đã thay đổi status ở form thì sao? 
                // Ở view notification, status bị disable hoặc hiển thị tag, nên values.paymentStatus có thể là undefined hoặc giá trị cũ
                // Tốt nhất ta update data nhưng loại bỏ field paymentStatus để tránh conflict với Approve logic
                const updateData = { ...formattedValues };
                delete updateData.paymentStatus; 
                
                // Update DB
                await axios.put(`/api/costs/${editingCost.id}`, updateData);

                // 2. Gọi API Approve để chuyển trạng thái và gửi thông báo
                const recipients = form.getFieldValue('notificationRecipients') || [];
                const res = await axios.post(`/api/costs/${editingCost.id}/approve`, {
                    notificationRecipients: recipients
                });

                message.success(res.data.message || 'Duyệt và lưu thành công');
                onSuccess();
            } catch (error) {
                handleApiError(error, 'Lỗi khi duyệt phiếu');
            }
            },
        });
    }).catch(errorInfo => {
        // Nếu lỗi ở notificationRecipients thì báo riêng, hoặc báo chung
        if (errorInfo.errorFields.find(f => f.name.includes('notificationRecipients'))) {
             message.error('Vui lòng chọn người nhận thông báo');
             form.scrollToField('notificationRecipients');
        } else {
             message.error('Vui lòng kiểm tra lại các trường thông tin');
             onFinishFailed(errorInfo); // Sẽ tự switch tab
        }
    });
  };

  const handleRejectAction = () => {
    form.validateFields(['notificationRecipients']).then(() => {
      setRejectReason('');
      setRejectReasonModalVisible(true);
    }).catch(() => {
      message.error('Vui lòng chọn người nhận thông báo trước khi từ chối');
      form.scrollToField('notificationRecipients');
    });
  };

  const confirmReject = () => {
    if (!rejectReason.trim()) {
      message.error('Vui lòng nhập lý do từ chối');
      return;
    }

    let updates = { rejectionReason: rejectReason, paymentStatus: 'Huỷ' };
    
    // Removed legacy fields update
    /*
    if (canEditField('approverManager')) {
      updates.approverManager = 'Từ chối';
    } else if (canEditField('approverDirector')) {
      updates.approverDirector = 'Từ chối';
    }
    */

    form.setFieldsValue(updates);
    setRejectReasonModalVisible(false);
    form.submit();
  };

  const handleSubmit = async (values) => {
    try {
      const formattedValues = {
        ...values,
        requestDate: formatDateValue(values.requestDate),
        transactionDate: formatDateValue(values.transactionDate),
        voucherDate: formatDateValue(values.voucherDate),
        payDate: formatDateValue(values.payDate),
        dueDate: formatDateValue(values.dueDate),
      };

      if (editingCost) {
        await axios.put(`/api/costs/${editingCost.id}`, formattedValues);
        message.success('Cập nhật phiếu chi thành công');

        // Logic xử lý thông báo và email
        const newStatus = values.paymentStatus;
        const oldStatus = editingCost.paymentStatus;
        let notifData = null;
        
        // Manual recipients from form
        const manualRecipients = values.notificationRecipients || [];

        // Lấy thông tin người tạo phiếu để biết gửi thông báo cho ai (Nhân viên & Manager của họ)
        let creator = null;
        try {
            if (editingCost && editingCost.createdByUserId) {
                const res = await axios.get(`/api/users/${editingCost.createdByUserId}`);
                creator = res.data;
            } else if (editingCost && editingCost.createdBy) {
                 // Fallback if legacy field name is used
                const res = await axios.get(`/api/users/${editingCost.createdBy}`);
                creator = res.data;
            }
        } catch (e) {
            console.error('Error fetching creator info', e);
        }

        // 1. Nếu bị HUỶ (Từ chối)
        if (newStatus === 'Huỷ' && oldStatus !== 'Huỷ') {
             // Thông báo cho Người yêu cầu (Requester) và Manager của họ
             const userIdsToNotify = [];
             if (creator) {
                userIdsToNotify.push(creator.id);
                if (creator.managerId) userIdsToNotify.push(creator.managerId);
             } else {
                 // Fallback
                 if (editingCost.createdByUserId) userIdsToNotify.push(editingCost.createdByUserId);
                 else if (editingCost.createdBy) userIdsToNotify.push(editingCost.createdBy);
                 
                 userIdsToNotify.push(2); 
             }

             notifData = {
                title: 'Phiếu chi bị từ chối',
                message: `Phiếu chi #${editingCost.id} đã bị từ chối. Lý do: ${values.rejectionReason}`,
                type: 'CostApproval',
                relatedId: editingCost.id.toString(),
                userIds: [...new Set([...userIdsToNotify, ...manualRecipients])]
             };

             notification.info({
                 message: '📧 Hệ thống Email (Gmail)',
                 description: `Đã gửi email TỪ CHỐI đến Requester, Manager và ${manualRecipients.length} người khác. Lý do: ${values.rejectionReason}`,
                 placement: 'topRight',
                 duration: 5,
             });
        } 
        // 2. Nếu Manager duyệt -> Chuyển Giám đốc
        else if (newStatus === 'Quản lý duyệt' && oldStatus !== 'Quản lý duyệt') {
             // Gửi cho Manager của người đang duyệt (tức là Giám đốc)
             // user là người đang thao tác (Manager)
             const directorId = user.managerId || 3; // Fallback to CEO

             notifData = {
                title: 'Phiếu chi cần duyệt (GĐ)',
                message: `Manager đã duyệt phiếu #${editingCost.id}. Vui lòng xem xét.`,
                type: 'CostApproval',
                relatedId: editingCost.id.toString(),
                userIds: [directorId]
             };

             notification.success({
                message: '📧 Hệ thống Email (Gmail)',
                description: 'Đã gửi email yêu cầu phê duyệt cho Giám đốc.',
                placement: 'topRight',
                duration: 5,
             });
        } 
        // 3. Nếu Giám đốc duyệt -> Chuyển Kế toán
        else if (newStatus === 'Giám đốc duyệt' && oldStatus !== 'Giám đốc duyệt') {
             // Gửi cho Kế toán (User ID 4)
             notifData = {
                title: 'Phiếu chi đã được duyệt',
                message: `Giám đốc đã duyệt phiếu #${editingCost.id}. Vui lòng thực hiện chi tiền.`,
                type: 'CostApproval',
                relatedId: editingCost.id.toString(),
                userIds: [4] // Accountant
             };

             notification.success({
                message: '📧 Hệ thống Email (Gmail)',
                description: 'Đã gửi email thông báo cho Kế toán.',
                placement: 'topRight',
                duration: 5,
             });
        }
        // 4. Nếu Kế toán hoàn thành (Đã thanh toán)
        else if (newStatus === 'Đã thanh toán' && oldStatus !== 'Đã thanh toán') {
             // Gửi cho Requester và Manager
             const userIdsToNotify = [];
             if (creator) {
                userIdsToNotify.push(creator.id);
                if (creator.managerId) userIdsToNotify.push(creator.managerId);
             } else {
                 userIdsToNotify.push(editingCost.createdBy);
                 userIdsToNotify.push(2);
             }

             notifData = {
                title: 'Phiếu chi đã thanh toán',
                message: `Phiếu chi #${editingCost.id} đã được thanh toán hoàn tất.`,
                type: 'CostApproval',
                relatedId: editingCost.id.toString(),
                userIds: [...new Set([...userIdsToNotify, ...manualRecipients])]
             };

             notification.success({
                message: '📧 Hệ thống Email (Gmail)',
                description: `Đã gửi email xác nhận thanh toán cho Nhân viên, Quản lý và ${manualRecipients.length} người khác.`,
                placement: 'topRight',
                duration: 5,
             });
        }

        if (notifData) {
            await axios.post('/api/notifications/create', notifData);
        }

      } else {
        const res = await axios.post('/api/costs', formattedValues);
        // const newCostId = res.data.id;
        message.success('Tạo phiếu chi thành công');
        
        // Gửi thông báo cho những người được chọn
        if (values.notificationRecipients && values.notificationRecipients.length > 0) {
          // Backend đã tự động gửi thông báo dựa trên notificationRecipients
          notification.success({
              message: '📧 Hệ thống Email (Gmail)',
              description: `Đã gửi email yêu cầu phê duyệt cho ${values.notificationRecipients.length} người nhận.`,
              placement: 'topRight',
              duration: 5,
          });
        }
      }
      
      onSuccess();
      form.resetFields();
    } catch (error) {
      handleApiError(error, 'Lỗi khi lưu phiếu chi');
    }
  };

  const renderGeneralInfo = () => (
    <>
      <Row gutter={16}>
        {canReadField('requester') && (
          <Col span={12}>
            <Form.Item
              name="requester"
              label="Người đề nghị"
              rules={[{ required: true, message: 'Vui lòng nhập người đề nghị' }]}
            >
              <Input disabled={!canEditField('requester')} />
            </Form.Item>
          </Col>
        )}
        {canReadField('department') && (
          <Col span={12}>
            <Form.Item
              name="department"
              label="Phòng ban"
            >
              <Select allowClear disabled={!canEditField('department')}>
                <Option value="Marketing">Marketing</Option>
                <Option value="Phát triển thị trường">Phát triển thị trường</Option>
                <Option value="Mua hàng">Mua hàng</Option>
                <Option value="Pháp chế">Pháp chế</Option>
                <Option value="Hành chính">Hành chính</Option>
                <Option value="Kế toán">Kế toán</Option>
              </Select>
            </Form.Item>
          </Col>
        )}
      </Row>
      <Row gutter={16}>
        {canReadField('requestDate') && (
          <Col span={12}>
            <Form.Item
              name="requestDate"
              label="Ngày đề nghị"
              rules={[{ required: true, message: 'Vui lòng chọn ngày' }]}
            >
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" disabled={!canEditField('requestDate')} />
            </Form.Item>
          </Col>
        )}
        <Col span={12}>
          <Form.Item
            name="transactionDate"
            label="Ngày phát sinh giao dịch"
          >
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" disabled={isNotificationView} />
          </Form.Item>
        </Col>
      </Row>
      <Row gutter={16}>
        {canReadField('projectCode') && (
          <Col span={12}>
            <Form.Item
              name="projectCode"
              label="Mã dự án"
            >
              <Select
                allowClear
                disabled={!canEditField('projectCode')}
                loading={projectCodesLoading}
                dropdownRender={(menu) => (
                  <div>
                    {menu}
                    {canManageProjectCodes && (
                      <div style={{ padding: 8 }}>
                        <Space style={{ width: '100%' }}>
                          <Input
                            size="small"
                            placeholder="Thêm mã dự án..."
                            value={projectCodeNewValue}
                            onChange={(e) => setProjectCodeNewValue(e.target.value)}
                            onPressEnter={addProjectCode}
                          />
                          <Button size="small" type="primary" onClick={addProjectCode}>
                            Thêm
                          </Button>
                          <Button size="small" onClick={openProjectCodeManage}>
                            Sửa/Xóa
                          </Button>
                        </Space>
                      </div>
                    )}
                  </div>
                )}
              >
                {(projectCodes || []).map((x) => (
                  <Option key={x.id} value={x.code}>
                    {x.code}
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </Col>
        )}
        {canReadField('priority') && (
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
                <Option value="Mức 5">Mức 5</Option>
              </Select>
            </Form.Item>
          </Col>
        )}
      </Row>
      <Row gutter={16}>
        {canReadField('transactionType') && (
          <Col span={12}>
            <Form.Item
              name="transactionType"
              label="Loại giao dịch"
              rules={[{ required: true, message: 'Vui lòng chọn loại giao dịch' }]}
            >
              <Select disabled={!canEditField('transactionType')}>
                <Option value="Chi">Chi</Option>
                <Option value="Thu">Thu</Option>
                <Option value="Hoàn ứng">Hoàn ứng</Option>
                <Option value="Chuyển nội bộ">Chuyển nội bộ</Option>
                <Option value="Tạm ứng">Tạm ứng</Option>
              </Select>
            </Form.Item>
          </Col>
        )}
        {canReadField('transactionObject') && (
          <Col span={12}>
            <Form.Item
              name="transactionObject"
              label="Đối tượng Thu/Chi"
              rules={[{ required: true, message: 'Vui lòng nhập đối tượng' }]}
            >
              <Input disabled={!canEditField('transactionObject')} />
            </Form.Item>
          </Col>
        )}
      </Row>
      <Row gutter={16}>
        <Col span={24}>
            <Form.Item
                name="notificationRecipients"
                label={
                    <span>
                        Gửi thông báo đến <BellOutlined style={{ color: '#1890ff' }} />
                    </span>
                }
                rules={[{ required: true, message: 'Vui lòng chọn người nhận thông báo' }]}
            >
                <Select
                    mode="multiple"
                    placeholder="Chọn người nhận thông báo"
                    optionFilterProp="children"
                    filterOption={(input, option) =>
                        String(option.children).toLowerCase().includes(input.toLowerCase())
                    }
                >
                    {(users || []).filter(u => u && u.username !== 'admin' && u.fullName !== 'Quản trị hệ thống' && u.id !== 1).map(u => (
                        <Option key={u.id} value={u.id}>
                            {`${u.fullName} (${u.username})`}
                        </Option>
                    ))}
                </Select>
            </Form.Item>
        </Col>
      </Row>
      <Row gutter={16}>
        {canReadField('taxCode') && (
          <Col span={12}>
            <Form.Item
              name="taxCode"
              label="Mã số thuế"
            >
              <Input disabled={!canEditField('taxCode')} />
            </Form.Item>
          </Col>
        )}
      </Row>
      {canReadField('content') && (
        <Form.Item
          name="content"
          label="Nội dung"
          rules={[{ required: true, message: 'Vui lòng chọn nội dung' }]}
        >
          <Select allowClear disabled={!canEditField('content')}>
            <Option value="Di chuyển">Di chuyển</Option>
            <Option value="Ăn uống">Ăn uống</Option>
            <Option value="Khách sạn">Khách sạn</Option>
            <Option value="Đổ xăng">Đổ xăng</Option>
            <Option value="Thanh toán dịch vụ">Thanh toán dịch vụ</Option>
            <Option value="Khác">Khác</Option>
          </Select>
        </Form.Item>
      )}
      {canReadField('description') && (
        <Form.Item
          name="description"
          label="Diễn giải chi tiết"
        >
          <TextArea rows={3} disabled={!canEditField('description')} />
        </Form.Item>
      )}
    </>
  );

  const renderFinancialInfo = () => (
    <>
      <Row gutter={16}>
        {canReadField('amountBeforeTax') && (
          <Col span={8}>
            <Form.Item
              name="amountBeforeTax"
              label="Số tiền (Chưa thuế)"
              rules={[{ required: true, message: 'Vui lòng nhập số tiền' }]}
            >
              <Input type="number" suffix="VND" disabled={!canEditField('amountBeforeTax')} />
            </Form.Item>
          </Col>
        )}
        {canReadField('taxRate') && (
          <Col span={8}>
            <Form.Item
              name="taxRate"
              label="Thuế suất"
            >
              <Select disabled={!canEditField('taxRate')}>
                <Option value="No VAT">Không chịu thuế</Option>
                <Option value="0%">VAT 0%</Option>
                <Option value="5%">VAT 5%</Option>
                <Option value="8%">VAT 8%</Option>
                <Option value="10%">VAT 10%</Option>
              </Select>
            </Form.Item>
          </Col>
        )}
        {canReadField('totalAmount') && (
          <Col span={8}>
            <Form.Item
              name="totalAmount"
              label="Tổng tiền"
            >
              <Input type="number" suffix="VND" readOnly disabled={!canEditField('totalAmount')} />
            </Form.Item>
          </Col>
        )}
      </Row>
      <Row gutter={16}>
        {canReadField('paymentMethod') && (
          <Col span={8}>
            <Form.Item
              name="paymentMethod"
              label="Phương thức thanh toán"
            >
              <Select disabled={!canEditField('paymentMethod')}>
                <Option value="Tiền mặt">Tiền mặt</Option>
                <Option value="Chuyển khoản">Chuyển khoản</Option>
                <Option value="Ví điện tử">Ví điện tử</Option>
              </Select>
            </Form.Item>
          </Col>
        )}
        {canReadField('bank') && (
          <Col span={8}>
            <Form.Item
              name="bank"
              label="Ngân hàng"
            >
              <Input disabled={!canEditField('bank')} />
            </Form.Item>
          </Col>
        )}
        {canReadField('accountNumber') && (
          <Col span={8}>
            <Form.Item
              name="accountNumber"
              label="Số tài khoản"
            >
              <Input disabled={!canEditField('accountNumber')} />
            </Form.Item>
          </Col>
        )}
      </Row>
    </>
  );

  const renderVoucherInfo = () => (
    <>
      <Row gutter={16}>
        {canReadField('voucherType') && (
          <Col span={8}>
            <Form.Item
              name="voucherType"
              label="Loại chứng từ"
            >
              <Select disabled={!canEditField('voucherType')}>
                <Option value="Hóa đơn">Hóa đơn</Option>
                <Option value="Phiếu thu">Phiếu thu</Option>
                <Option value="Phiếu chi">Phiếu chi</Option>
                <Option value="Hợp đồng">Hợp đồng</Option>
              </Select>
            </Form.Item>
          </Col>
        )}
        {canReadField('voucherNumber') && (
          <Col span={8}>
            <Form.Item
              name="voucherNumber"
              label="Số chứng từ/Số hợp đồng"
            >
              <Input disabled={!canEditField('voucherNumber')} />
            </Form.Item>
          </Col>
        )}
        {canReadField('voucherDate') && (
          <Col span={8}>
            <Form.Item
              name="voucherDate"
              label="Ngày chứng từ"
            >
              <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" disabled={!canEditField('voucherDate')} />
            </Form.Item>
          </Col>
        )}
      </Row>
      <Row gutter={16}>
        <Col span={8}>
          <Form.Item
            name="invoiceNumber"
            label="Số hóa đơn"
          >
            <Input disabled={isNotificationView} />
          </Form.Item>
        </Col>
        <Col span={8}>
          <Form.Item
            name="invoiceSeries"
            label="Ký hiệu hóa đơn"
          >
            <Input disabled={isNotificationView} />
          </Form.Item>
        </Col>
        <Col span={8}>
          <Form.Item
            name="vatAmount"
            label="Tiền VAT"
          >
            <Input type="number" suffix="VND" disabled={isNotificationView} />
          </Form.Item>
        </Col>
      </Row>
      {canReadField('attachment') && (
        <Form.Item
          label="File đính kèm"
          shouldUpdate={(prev, curr) => JSON.stringify(prev.attachments) !== JSON.stringify(curr.attachments)}
        >
          {({ getFieldValue }) => {
            const items = getFieldValue('attachments') || [];
            return (
              <Space direction="vertical" style={{ width: '100%' }}>
                <Form.Item name="attachments" noStyle hidden>
                  <Input />
                </Form.Item>
                <Upload 
                  multiple
                  customRequest={handleUpload}
                  showUploadList={false}
                  disabled={!canEditField('attachment')}
                >
                  <Button icon={<UploadOutlined />}>Tải lên file</Button>
                </Upload>
                {items.length > 0 && (
                  <Space direction="vertical">
                    {items.map((it, idx) => {
                      const full = it.path?.startsWith('http') ? it.path : `${API_BASE_URL}${it.path}`;
                      return (
                        <Space key={`${it.path}-${idx}`}>
                          <a href={full} target="_blank" rel="noopener noreferrer">
                            <Tag icon={<FileTextOutlined />} color="blue">{it.name || 'Tệp đính kèm'}</Tag>
                          </a>
                          <Button size="small" danger onClick={() => {
                            const next = [...items];
                            next.splice(idx, 1);
                            form.setFieldsValue({ attachments: next });
                          }}>Xóa</Button>
                        </Space>
                      );
                    })}
                  </Space>
                )}
              </Space>
            );
          }}
        </Form.Item>
      )}
    </>
  );

  const renderApprovalInfo = () => (
    <>
      <Row gutter={16}>
        {canReadField('paymentStatus') && (
          <Col span={12}>
            {!editingCost ? (
              <Form.Item label="Trạng thái thanh toán">
                 <Tag color="orange">ĐỢI DUYỆT</Tag>
                 <Form.Item name="paymentStatus" hidden initialValue="Đợi duyệt">
                   <Input />
                 </Form.Item>
              </Form.Item>
            ) : (
              <Form.Item
                label="Trạng thái thanh toán"
                shouldUpdate
              >
                 {() => {
                    // Nếu là notification view, hiển thị Tag
                    if (isNotificationView) {
                         return getStatusTag(editingCost.paymentStatus);
                    }
                    // Ngược lại hiển thị Select để edit (nếu có quyền)
                    return (
                        <Form.Item name="paymentStatus" noStyle>
                            <Select disabled={!canEditField('paymentStatus')}>
                              <Option value="Đợi duyệt">Đợi duyệt</Option>
                              <Option value="Quản lý duyệt">Quản lý duyệt</Option>
                              <Option value="Giám đốc duyệt">Giám đốc duyệt</Option>
                              <Option value="Đã thanh toán">Đã thanh toán</Option>
                              <Option value="Thanh toán 1 phần">Thanh toán 1 phần</Option>
                              <Option value="Huỷ">Huỷ</Option>
                            </Select>
                        </Form.Item>
                    );
                 }}
              </Form.Item>
            )}
          </Col>
        )}
        {canReadField('rejectionReason') && (
          <Col span={12}>
            <Form.Item
              name="rejectionReason"
              label="Lý do từ chối"
            >
              <Input disabled={!canEditField('rejectionReason')} />
            </Form.Item>
          </Col>
        )}
      </Row>
      {/* Removed approverManager, approverDirector, accountantReview fields */}
      <Row gutter={16}>
        {canReadField('adjustmentReason') && (
          <Col span={12}>
            <Form.Item
              name="adjustmentReason"
              label="Lý do điều chỉnh"
            >
              <Input disabled={!canEditField('adjustmentReason')} />
            </Form.Item>
          </Col>
        )}
        {canReadField('riskFlag') && (
          <Col span={12}>
            <Form.Item
            name="riskFlag"
            label="Cờ kiểm soát rủi ro"
          >
            <Select allowClear disabled={isNotificationView}>
              <Option value="Có">Có</Option>
              <Option value="Không">Không</Option>
            </Select>
          </Form.Item>
          </Col>
        )}
      </Row>
      {canReadField('note') && (
        <Form.Item
          name="note"
          label="Ghi chú"
        >
          <TextArea rows={3} disabled={!canEditField('note')} />
        </Form.Item>
      )}
    </>
  );

  const renderVendorAndAccounting = () => (
    <>
      <Row gutter={16}>
        <Col span={12}>
          <Form.Item
            name="vendorName"
            label="Nhà cung cấp/Đối tác"
          >
            <Input disabled={isNotificationView} />
          </Form.Item>
        </Col>
        <Col span={12}>
          <Form.Item
            name="vendorTaxCode"
            label="MST nhà cung cấp"
          >
            <Input disabled={isNotificationView} />
          </Form.Item>
        </Col>
      </Row>
      <Row gutter={16}>
        <Col span={8}>
          <Form.Item
            name="costCategory"
            label="Nhóm chi phí"
          >
            <Select allowClear disabled={isNotificationView}>
              <Option value="Văn phòng phẩm">Văn phòng phẩm</Option>
              <Option value="Đi lại">Đi lại</Option>
              <Option value="Marketing">Marketing</Option>
              <Option value="Dịch vụ">Dịch vụ</Option>
              <Option value="Khác">Khác</Option>
            </Select>
          </Form.Item>
        </Col>
        <Col span={8}>
          <Form.Item
            name="costSubCategory"
            label="Tiểu mục chi phí"
          >
            <Input disabled={isNotificationView} />
          </Form.Item>
        </Col>
        <Col span={8}>
          <Form.Item
            name="costCenter"
            label="Mã hạch toán (Trung tâm chi phí)"
          >
            <Input disabled={isNotificationView} />
          </Form.Item>
        </Col>
      </Row>
    </>
  );

  const renderPaymentDeadline = () => (
    <>
      <Row gutter={16}>
        <Col span={12}>
          <Form.Item
            name="payDate"
            label="Ngày thanh toán"
            getValueProps={(i) => ({ value: i ? dayjs(i) : null })}
            getValueFromEvent={(e) => (e ? e.format('YYYY-MM-DD') : null)}
          >
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
        </Col>
        <Col span={12}>
          <Form.Item
            name="dueDate"
            label="Hạn thanh toán"
            getValueProps={(i) => ({ value: i ? dayjs(i) : null })}
            getValueFromEvent={(e) => (e ? e.format('YYYY-MM-DD') : null)}
          >
            <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
          </Form.Item>
        </Col>
      </Row>
    </>
  );

  const getStatusTag = (status) => {
      let color = 'default';
      if (status === 'Đợi duyệt') color = 'orange';
      else if (status === 'Quản lý duyệt') color = 'blue';
      else if (status === 'Giám đốc duyệt') color = 'purple';
      else if (status === 'Đã thanh toán') color = 'green';
      else if (status === 'Huỷ' || status === 'Từ chối') color = 'red';
      
      return <Tag color={color}>{status}</Tag>;
  };

  const getModalTitle = () => {
      if (isNotificationView) {
          return (
              <Space>
                  <span>Thông tin phiếu chi</span>
                  {editingCost && getStatusTag(editingCost.paymentStatus)}
              </Space>
          );
      }
      return editingCost ? 'Cập nhật phiếu chi' : 'Tạo phiếu chi mới';
  };

  return (
    <>
      <Modal
        title={getModalTitle()}
        open={visible}
        onCancel={onCancel}
        width={800}
        footer={[
          <Button key="back" onClick={onCancel}>
            Đóng
          </Button>,
          (() => {
            if (!editingCost) {
              return (
                <Button key="submit" type="primary" onClick={form.submit}>
                  Gửi duyệt
                </Button>
              );
            }

            const { paymentStatus } = editingCost;
            
            // Quyền duyệt của Manager: Chỉ khi Đợi duyệt
            const allowManager = (user?.role === 'ip_manager' || user?.role === 'quan_ly' || user?.role === 'manager') && paymentStatus === 'Đợi duyệt';
            // Quyền duyệt của Director: Đợi duyệt (nếu được nhảy cóc) hoặc Quản lý duyệt
            const allowDirector = (user?.role === 'admin' || user?.role === 'director' || user?.role === 'giam_doc') && ['Đợi duyệt', 'Quản lý duyệt'].includes(paymentStatus);
            // Quyền duyệt của Accountant: Giám đốc duyệt
            const allowAccountant = (user?.role === 'admin' || user?.role === 'ke_toan' || user?.role === 'accountant') && paymentStatus === 'Giám đốc duyệt';

            // Nút Duyệt và Từ chối
            if (allowManager || allowDirector || allowAccountant) {
              return (
                <>
                  <Button key="reject" danger onClick={handleRejectAction}>
                    Từ chối
                  </Button>
                  {!isNotificationView && (
                    <Button key="save" onClick={form.submit} style={{ marginRight: 8, marginLeft: 8 }}>
                      Lưu
                    </Button>
                  )}
                  <Button key="approve" type="primary" onClick={handleApproveAction}>
                    Duyệt
                  </Button>
                </>
              );
            }

            if (['Đã thanh toán', 'Huỷ'].includes(paymentStatus)) {
                 return null;
            }

            // Nếu đang xem từ thông báo và không có quyền duyệt, chỉ hiện nút Đóng (đã có ở trên)
            // hoặc nếu muốn cho phép sửa thì vẫn hiện nút Lưu
            // User yêu cầu: "Khi nhấn vào chuông thì k có nút lưu"
            if (isNotificationView) {
                return null;
            }

            return (
              <Button key="submit" type="primary" onClick={form.submit}>
                Lưu
              </Button>
            );
          })()
        ]}
      >
        <Form
          form={form}
          layout="vertical"
          scrollToFirstError
          onFinish={handleSubmit}
          onFinishFailed={onFinishFailed}
          onValuesChange={calculateTotal}
        >
          <Tabs activeKey={activeTab} onChange={setActiveTab}>
            <Tabs.TabPane tab="Thông tin chung" key="1">
              {renderGeneralInfo()}
            </Tabs.TabPane>
            <Tabs.TabPane tab="Tài chính" key="2">
              {renderFinancialInfo()}
            </Tabs.TabPane>
            <Tabs.TabPane tab="Chứng từ" key="3">
              {renderVoucherInfo()}
            </Tabs.TabPane>
            <Tabs.TabPane tab="Phê duyệt" key="4">
              {renderApprovalInfo()}
            </Tabs.TabPane>
            <Tabs.TabPane tab="Đối tác & Hạch toán" key="5">
              {renderVendorAndAccounting()}
            </Tabs.TabPane>
            <Tabs.TabPane tab="Thanh toán & Hạn" key="6">
              {renderPaymentDeadline()}
            </Tabs.TabPane>
          </Tabs>
        </Form>
      </Modal>

      <Modal
        title="Quản lý Mã dự án"
        open={isProjectCodeManageVisible}
        onCancel={() => setIsProjectCodeManageVisible(false)}
        footer={null}
        width={520}
      >
        {(projectCodes || []).map((x) => (
          <div key={x.id} style={{ display: 'flex', gap: 8, alignItems: 'center', marginBottom: 8 }}>
            <Input
              value={projectCodeManageDrafts[x.id] ?? x.code}
              onChange={(e) =>
                setProjectCodeManageDrafts((prev) => ({
                  ...prev,
                  [x.id]: e.target.value,
                }))
              }
            />
            <Button type="primary" onClick={() => saveProjectCode(x.id)}>
              Lưu
            </Button>
            <Popconfirm
              title="Xóa mã này?"
              onConfirm={() => deleteProjectCode(x.id)}
              okText="Có"
              cancelText="Không"
            >
              <Button danger>Xóa</Button>
            </Popconfirm>
          </div>
        ))}
      </Modal>

      <Modal
        title="Lý do từ chối"
        open={rejectReasonModalVisible}
        onOk={confirmReject}
        onCancel={() => setRejectReasonModalVisible(false)}
        okText="Xác nhận từ chối"
        cancelText="Hủy"
      >
        <Input.TextArea 
          rows={4} 
          value={rejectReason} 
          onChange={(e) => setRejectReason(e.target.value)} 
          placeholder="Nhập lý do từ chối..." 
        />
      </Modal>
    </>
  );
};

export default CostFormModal;
