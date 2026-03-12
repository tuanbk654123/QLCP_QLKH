import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, DatePicker, Input, Modal, Select, Space, Switch, Table, Tag, Typography, Descriptions, Tabs } from 'antd';
import axios from 'axios';
import dayjs from 'dayjs';
import { handleApiError } from '../../utils/errorHelper';

const { RangePicker } = DatePicker;
const { Text } = Typography;

const FIELD_LABELS = {
  customer: {
    name: 'Tên khách hàng',
    taxCode: 'Mã số thuế',
    address: 'Địa chỉ',
    businessScale: 'Quy mô doanh nghiệp',
    representativeName: 'Người đại diện',
    representativePosition: 'Chức vụ đại diện',
    idNumber: 'CCCD/Hộ chiếu',
    phone: 'Số điện thoại',
    email: 'Email',
    contactPerson: 'Người liên hệ',
    contactPhone: 'SĐT liên hệ',
    contactEmail: 'Email liên hệ',
    consultingStatus: 'Trạng thái tư vấn',
    contractStatus: 'Trạng thái hợp đồng',
    contractNumber: 'Số hợp đồng',
    contractValue: 'Giá trị hợp đồng',
    stateFee: 'Lệ phí nhà nước',
    additionalFee: 'Phí dịch vụ',
    brandName: 'Tên nhãn hiệu',
    productsServices: 'Sản phẩm/Dịch vụ',
    filingStatus: 'Trạng thái nộp đơn',
    filingDate: 'Ngày nộp đơn',
    applicationCode: 'Mã đơn',
    issueDate: 'Ngày cấp',
    expiryDate: 'Ngày hết hạn',
    joinDate: 'Ngày tham gia',
    createdAt: 'Ngày tạo',
    createdBy: 'Người tạo',
    updatedAt: 'Ngày cập nhật',
    updatedBy: 'Người cập nhật',
    documentLink: 'Link hồ sơ giấy tờ',
    legacyId: 'Mã hệ thống',
    id: 'ID',
  },
  cost: {
    requester: 'Người yêu cầu',
    department: 'Bộ phận',
    priority: 'Ưu tiên',
    requestDate: 'Ngày yêu cầu',
    projectCode: 'Mã dự án',
    content: 'Nội dung',
    description: 'Mô tả',
    transactionType: 'Loại giao dịch',
    voucherType: 'Loại chứng từ',
    transactionObject: 'Đối tượng',
    note: 'Ghi chú',
    amountBeforeTax: 'Tiền trước thuế',
    taxRate: 'Thuế suất',
    totalAmount: 'Tổng tiền',
    taxCode: 'Mã số thuế',
    voucherNumber: 'Số chứng từ',
    voucherDate: 'Ngày chứng từ',
    attachment: 'Tệp đính kèm',
    paymentMethod: 'Phương thức thanh toán',
    accountNumber: 'Số tài khoản',
    bank: 'Ngân hàng',
    paymentStatus: 'Trạng thái',
    managerApproval: 'Quản lý duyệt',
    directorApproval: 'Giám đốc duyệt',
    accountantReview: 'Kế toán review',
    adjustReason: 'Lý do điều chỉnh',
    rejectionReason: 'Lý do từ chối',
    riskFlag: 'Cờ rủi ro',
    createdAt: 'Ngày tạo',
    createdBy: 'Người tạo',
    updatedAt: 'Ngày cập nhật',
    updatedBy: 'Người cập nhật',
    legacyId: 'Mã hệ thống',
    id: 'ID',
  },
};

const actionLabel = (action) => {
  if (action === 'create') return { text: 'Tạo', color: 'green' };
  if (action === 'update') return { text: 'Sửa', color: 'blue' };
  if (action === 'approve') return { text: 'Duyệt', color: 'cyan' };
  if (action === 'reject') return { text: 'Từ chối', color: 'volcano' };
  if (action === 'delete') return { text: 'Xóa', color: 'red' };
  return { text: action, color: 'default' };
};

const moduleLabel = (entityType) => {
  if (entityType === 'customer') return 'QLKH';
  if (entityType === 'cost') return 'QLCP';
  return entityType;
};

const looksLikeDate = (s) => {
  if (typeof s !== 'string') return false;
  if (!s) return false;
  const d = dayjs(s);
  return d.isValid() && /\d{4}-\d{2}-\d{2}/.test(s);
};

const shouldFormatMoney = (fieldKey) => {
  const k = (fieldKey || '').toLowerCase();
  return (
    k.includes('amount') ||
    k.includes('fee') ||
    k.includes('value') ||
    k.includes('total') ||
    k.includes('contract')
  );
};

const formatValue = (value, fieldKey) => {
  if (value === null || value === undefined) return '';
  if (typeof value === 'boolean') return value ? 'Có' : 'Không';
  if (typeof value === 'number') {
    if (shouldFormatMoney(fieldKey) && Number.isFinite(value)) {
      return new Intl.NumberFormat('vi-VN').format(value);
    }
    return String(value);
  }
  if (typeof value === 'string') {
    if (looksLikeDate(value)) {
      const d = dayjs(value);
      if (d.isValid()) {
        return d.format(value.includes(':') ? 'DD/MM/YYYY HH:mm:ss' : 'DD/MM/YYYY');
      }
    }
    return value;
  }
  try {
    return JSON.stringify(value, null, 2);
  } catch (e) {
    return String(value);
  }
};

const humanizeKey = (k) => {
  if (!k) return '';
  const withSpaces = String(k)
    .replace(/_/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .trim();
  return withSpaces.charAt(0).toUpperCase() + withSpaces.slice(1);
};

const fieldLabel = (entityType, k) => {
  const map = FIELD_LABELS[entityType] || {};
  return map[k] || humanizeKey(k);
};

const buildFieldRows = (entityType, data, onlyKeys) => {
  const keys = Object.keys(data || {})
    .filter((k) => !onlyKeys || onlyKeys.includes(k))
    .sort((a, b) => fieldLabel(entityType, a).localeCompare(fieldLabel(entityType, b)));
  return keys.map((k) => ({
    key: k,
    label: fieldLabel(entityType, k),
    value: formatValue(data?.[k], k),
    raw: data?.[k],
  }));
};

const buildDescriptionsItems = (entityType, data, highlightKeys) => {
  const keys = Object.keys(data || {}).sort((a, b) => fieldLabel(entityType, a).localeCompare(fieldLabel(entityType, b)));
  return keys.map((k) => ({
    key: k,
    label: fieldLabel(entityType, k),
    children: (
      <span
        style={
          highlightKeys?.includes(k)
            ? { fontWeight: 700, padding: '2px 6px', borderRadius: 6, background: 'rgba(24, 144, 255, 0.10)' }
            : undefined
        }
      >
        {formatValue(data[k], k)}
      </span>
    ),
  }));
};

const AuditLogsPage = () => {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const [moduleFilter, setModuleFilter] = useState('all');
  const [actionFilter, setActionFilter] = useState('all');
  const [entityIdFilter, setEntityIdFilter] = useState('');
  const [actorNameFilter, setActorNameFilter] = useState('');
  const [range, setRange] = useState([dayjs().startOf('day').add(-7, 'day'), dayjs().endOf('day')]);

  const [detailOpen, setDetailOpen] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detail, setDetail] = useState(null);
  const [detailOnlyChanged, setDetailOnlyChanged] = useState(true);
  const [detailTab, setDetailTab] = useState('changes');

  const fetchList = useCallback(async () => {
    setLoading(true);
    try {
      const params = {
        page,
        pageSize,
      };

      if (moduleFilter !== 'all') params.module = moduleFilter;
      if (actionFilter !== 'all') params.action = actionFilter;

      const entityId = parseInt(entityIdFilter, 10);
      if (!Number.isNaN(entityId) && entityId > 0) params.entityId = entityId;

      if (actorNameFilter.trim()) params.actorName = actorNameFilter.trim();

      const [from, to] = range || [];
      if (from && to) {
        params.from = dayjs(from).format('YYYY-MM-DD HH:mm:ss');
        params.to = dayjs(to).format('YYYY-MM-DD HH:mm:ss');
      }

      const res = await axios.get('/api/audit-logs/recent', { params });
      setItems(res.data.items || []);
      setTotal(res.data.total || 0);
    } catch (error) {
      setItems([]);
      setTotal(0);
      handleApiError(error, 'Không thể tải lịch sử');
    } finally {
      setLoading(false);
    }
  }, [actionFilter, actorNameFilter, entityIdFilter, moduleFilter, page, pageSize, range]);

  useEffect(() => {
    fetchList();
  }, [fetchList]);

  const openDetail = async (row) => {
    setDetailOpen(true);
    setDetailTab('changes');
    setDetailLoading(true);
    try {
      const res = await axios.get(`/api/audit-logs/${row.id}`);
      setDetail(res.data);
    } catch (error) {
      setDetail(null);
      handleApiError(error, 'Không thể tải chi tiết lịch sử');
    } finally {
      setDetailLoading(false);
    }
  };

  const columns = useMemo(
    () => [
      {
        title: 'Thời gian',
        dataIndex: 'createdAt',
        key: 'createdAt',
        width: 170,
        render: (v) => (v ? formatValue(v, 'createdAt') : ''),
      },
      {
        title: 'Module',
        dataIndex: 'entityType',
        key: 'entityType',
        width: 90,
        render: (val) => <Tag color={val === 'customer' ? 'geekblue' : val === 'cost' ? 'gold' : undefined}>{moduleLabel(val)}</Tag>,
      },
      {
        title: 'Đối tượng',
        key: 'entity',
        width: 130,
        render: (_, r) => (
          <Text strong>
            #{r.entityId}
          </Text>
        ),
      },
      {
        title: 'Hành động',
        dataIndex: 'action',
        key: 'action',
        width: 100,
        render: (val) => {
          const a = actionLabel(val);
          return <Tag color={a.color}>{a.text}</Tag>;
        },
      },
      {
        title: 'Người thực hiện',
        key: 'actor',
        render: (_, r) => (
          <div>
            <div style={{ fontWeight: 600 }}>{r.actorFullName || ''}</div>
            <div style={{ color: 'rgba(0,0,0,0.45)', fontSize: 12 }}>
              {r.actorPosition || ''}{r.actorRole ? ` • ${r.actorRole}` : ''}
            </div>
          </div>
        ),
      },
      {
        title: '',
        key: 'view',
        width: 80,
        fixed: 'right',
        render: (_, r) => (
          <Button
            size="small"
            onClick={(e) => {
              e.stopPropagation();
              openDetail(r);
            }}
          >
            Xem
          </Button>
        ),
      },
    ],
    [],
  );

  const detailChangedFields = useMemo(() => detail?.changedFields || [], [detail]);
  const detailEntityType = detail?.entityType;
  const showTwoCols = detail?.action === 'update' || detail?.action === 'approve' || detail?.action === 'reject';

  const oldRows = useMemo(() => buildFieldRows(detailEntityType, detail?.oldData, detailOnlyChanged ? detailChangedFields : null), [detail, detailChangedFields, detailEntityType, detailOnlyChanged]);
  const newRows = useMemo(() => buildFieldRows(detailEntityType, detail?.newData, detailOnlyChanged ? detailChangedFields : null), [detail, detailChangedFields, detailEntityType, detailOnlyChanged]);

  const diffRows = useMemo(() => {
    if (!detail) return [];
    const entityType = detail.entityType;
    const oldData = detail.oldData || {};
    const newData = detail.newData || {};
    const keys = detailOnlyChanged ? detailChangedFields : Array.from(new Set([...Object.keys(oldData), ...Object.keys(newData)]));
    return (keys || [])
      .filter((k) => k)
      .map((k) => ({
        key: k,
        label: fieldLabel(entityType, k),
        oldValue: formatValue(oldData?.[k], k),
        newValue: formatValue(newData?.[k], k),
        changed: String(oldData?.[k]) !== String(newData?.[k]),
      }))
      .sort((a, b) => a.label.localeCompare(b.label));
  }, [detail, detailChangedFields, detailOnlyChanged]);

  const detailSummaryItems = useMemo(() => {
    if (!detail) return [];
    const a = actionLabel(detail.action);
    return [
      { key: 'time', label: 'Thời gian', children: formatValue(detail.createdAt, 'createdAt') },
      { key: 'module', label: 'Module', children: <Tag color={detail.entityType === 'customer' ? 'geekblue' : detail.entityType === 'cost' ? 'gold' : undefined}>{moduleLabel(detail.entityType)}</Tag> },
      { key: 'entity', label: 'Đối tượng', children: <Text strong>#{detail.entityId}</Text> },
      { key: 'action', label: 'Hành động', children: <Tag color={a.color}>{a.text}</Tag> },
      { key: 'actor', label: 'Người thực hiện', children: `${detail.actorFullName || ''}${detail.actorPosition ? ` - ${detail.actorPosition}` : ''}` },
    ];
  }, [detail]);

  return (
    <div style={{ padding: 16 }}>
      <Card title="Lịch sử tác động" bordered={false} bodyStyle={{ paddingBottom: 8 }}>
        <Space wrap style={{ marginBottom: 12 }}>
          <Select
            style={{ width: 140 }}
            value={moduleFilter}
            onChange={(v) => {
              setModuleFilter(v);
              setPage(1);
            }}
            options={[
              { value: 'all', label: 'Tất cả module' },
              { value: 'qlkh', label: 'QLKH' },
              { value: 'qlcp', label: 'QLCP' },
            ]}
          />
          <Select
            style={{ width: 120 }}
            value={actionFilter}
            onChange={(v) => {
              setActionFilter(v);
              setPage(1);
            }}
            options={[
              { value: 'all', label: 'Tất cả' },
              { value: 'create', label: 'Tạo' },
              { value: 'update', label: 'Sửa' },
              { value: 'approve', label: 'Duyệt' },
              { value: 'reject', label: 'Từ chối' },
              { value: 'delete', label: 'Xóa' },
            ]}
          />
          <Input
            style={{ width: 160 }}
            placeholder="Mã đối tượng (ID)"
            value={entityIdFilter}
            onChange={(e) => {
              setEntityIdFilter(e.target.value);
              setPage(1);
            }}
          />
          <Input
            style={{ width: 220 }}
            placeholder="Tên người thực hiện"
            value={actorNameFilter}
            onChange={(e) => {
              setActorNameFilter(e.target.value);
              setPage(1);
            }}
          />
          <RangePicker
            value={range}
            onChange={(v) => {
              setRange(v);
              setPage(1);
            }}
            showTime
            format="YYYY-MM-DD HH:mm:ss"
          />
          <Button onClick={fetchList} loading={loading}>
            Tải lại
          </Button>
        </Space>

        <Table
          size="small"
          rowKey="id"
          loading={loading}
          columns={columns}
          dataSource={items}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            onChange: (nextPage, nextSize) => {
              setPage(nextPage);
              setPageSize(nextSize);
            },
          }}
          onRow={(record) => ({
            onClick: () => openDetail(record),
          })}
          scroll={{ x: 900 }}
        />
      </Card>

      <Modal
        title="Chi tiết lịch sử"
        open={detailOpen}
        onCancel={() => setDetailOpen(false)}
        footer={null}
        width={showTwoCols ? 1100 : 700}
        bodyStyle={{ maxHeight: '70vh', overflow: 'auto' }}
      >
        {detailLoading ? (
          <div>Đang tải...</div>
        ) : detail ? (
          <div>
            <Descriptions size="small" bordered column={1} items={detailSummaryItems} />

            <div style={{ display: 'flex', gap: 12, alignItems: 'center', marginTop: 12, marginBottom: 8 }}>
              <Text type="secondary">Chỉ hiện field thay đổi</Text>
              <Switch checked={detailOnlyChanged} onChange={setDetailOnlyChanged} />
              {detailChangedFields.length > 0 && (
                <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6, marginLeft: 8 }}>
                  {detailChangedFields.slice(0, 12).map((k) => (
                    <Tag key={k}>{fieldLabel(detail.entityType, k)}</Tag>
                  ))}
                  {detailChangedFields.length > 12 && <Tag>+{detailChangedFields.length - 12}</Tag>}
                </div>
              )}
            </div>

            <Tabs
              activeKey={detailTab}
              onChange={setDetailTab}
              items={[
                {
                  key: 'changes',
                  label: showTwoCols ? 'Thay đổi' : 'Dữ liệu',
                  children: showTwoCols ? (
                    <Table
                      size="small"
                      rowKey="key"
                      pagination={false}
                      dataSource={diffRows}
                      columns={[
                        { title: 'Trường', dataIndex: 'label', key: 'label', width: 240 },
                        {
                          title: 'Cũ',
                          dataIndex: 'oldValue',
                          key: 'oldValue',
                          render: (v, r) => (
                            <span style={r.changed ? { background: 'rgba(255, 77, 79, 0.10)', padding: '2px 6px', borderRadius: 6 } : undefined}>
                              {v}
                            </span>
                          ),
                        },
                        {
                          title: 'Mới',
                          dataIndex: 'newValue',
                          key: 'newValue',
                          render: (v, r) => (
                            <span style={r.changed ? { background: 'rgba(82, 196, 26, 0.10)', padding: '2px 6px', borderRadius: 6, fontWeight: 600 } : undefined}>
                              {v}
                            </span>
                          ),
                        },
                      ]}
                      scroll={{ x: 900, y: 420 }}
                    />
                  ) : (
                    <Table
                      size="small"
                      rowKey="key"
                      pagination={false}
                      dataSource={detail.action === 'delete' ? oldRows : newRows}
                      columns={[
                        { title: 'Trường', dataIndex: 'label', key: 'label', width: 260 },
                        { title: 'Giá trị', dataIndex: 'value', key: 'value' },
                      ]}
                      scroll={{ x: 700, y: 420 }}
                    />
                  ),
                },
                {
                  key: 'old',
                  label: 'Bản ghi cũ',
                  children: (
                    <Descriptions size="small" bordered column={1} items={buildDescriptionsItems(detail.entityType, detail.oldData, detailOnlyChanged ? detailChangedFields : [])} />
                  ),
                },
                {
                  key: 'new',
                  label: 'Bản ghi mới',
                  children: (
                    <Descriptions size="small" bordered column={1} items={buildDescriptionsItems(detail.entityType, detail.newData, detailOnlyChanged ? detailChangedFields : [])} />
                  ),
                },
              ]}
            />
          </div>
        ) : (
          <div>Không có dữ liệu</div>
        )}
      </Modal>
    </div>
  );
};

export default AuditLogsPage;
