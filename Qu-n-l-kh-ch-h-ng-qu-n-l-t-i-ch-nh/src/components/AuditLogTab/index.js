import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Descriptions, Modal, Table, Tag } from 'antd';
import axios from 'axios';
import { handleApiError } from '../../utils/errorHelper';

const formatValue = (value) => {
  if (value === null || value === undefined) return '';
  if (typeof value === 'string' || typeof value === 'number' || typeof value === 'boolean') return String(value);
  try {
    return JSON.stringify(value, null, 2);
  } catch (e) {
    return String(value);
  }
};

const actionLabel = (action) => {
  if (action === 'create') return { text: 'Tạo', color: 'green' };
  if (action === 'update') return { text: 'Sửa', color: 'blue' };
  if (action === 'delete') return { text: 'Xóa', color: 'red' };
  return { text: action, color: 'default' };
};

const buildDescriptionsItems = (data, changedFields) => {
  const keys = Object.keys(data || {}).sort((a, b) => a.localeCompare(b));
  return keys.map((k) => ({
    key: k,
    label: k,
    children: (
      <span style={changedFields?.includes(k) ? { fontWeight: 700 } : undefined}>
        {formatValue(data[k])}
      </span>
    ),
  }));
};

const AuditLogTab = ({ entityType, entityId, enabled }) => {
  const [loading, setLoading] = useState(false);
  const [items, setItems] = useState([]);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detail, setDetail] = useState(null);
  const [detailOpen, setDetailOpen] = useState(false);

  const fetchList = useCallback(async () => {
    if (!enabled || !entityId) return;
    setLoading(true);
    try {
      const res = await axios.get('/api/audit-logs', { params: { entityType, entityId } });
      setItems(res.data.items || []);
    } catch (error) {
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, [enabled, entityId, entityType]);

  useEffect(() => {
    fetchList();
  }, [fetchList]);

  const openDetail = async (row) => {
    setDetailOpen(true);
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
        render: (_, r) => `${r.actorFullName || ''}${r.actorPosition ? ` - ${r.actorPosition}` : ''}`,
      },
    ],
    [],
  );

  const detailChangedFields = detail?.changedFields || [];
  const oldItems = useMemo(() => buildDescriptionsItems(detail?.oldData, detailChangedFields), [detail, detailChangedFields]);
  const newItems = useMemo(() => buildDescriptionsItems(detail?.newData, detailChangedFields), [detail, detailChangedFields]);

  const showTwoCols = detail?.action === 'update';

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 8 }}>
        <Button onClick={fetchList} loading={loading} disabled={!enabled || !entityId}>
          Tải lại
        </Button>
      </div>

      <Table
        size="small"
        rowKey="id"
        loading={loading}
        columns={columns}
        dataSource={items}
        pagination={false}
        onRow={(record) => ({
          onClick: () => openDetail(record),
        })}
      />

      <Modal
        title="Chi tiết lịch sử"
        open={detailOpen}
        onCancel={() => setDetailOpen(false)}
        footer={null}
        width={showTwoCols ? 1100 : 700}
      >
        {detailLoading ? (
          <div>Đang tải...</div>
        ) : detail ? (
          <div>
            <div style={{ marginBottom: 12 }}>
              <div><strong>Thời gian:</strong> {detail.createdAt}</div>
              <div><strong>Người thực hiện:</strong> {detail.actorFullName}{detail.actorPosition ? ` - ${detail.actorPosition}` : ''}</div>
            </div>

            {showTwoCols ? (
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                <div>
                  <div style={{ fontWeight: 700, marginBottom: 8 }}>Bản ghi cũ</div>
                  <Descriptions size="small" bordered column={1} items={oldItems} />
                </div>
                <div>
                  <div style={{ fontWeight: 700, marginBottom: 8 }}>Bản ghi mới</div>
                  <Descriptions size="small" bordered column={1} items={newItems} />
                </div>
              </div>
            ) : (
              <Descriptions size="small" bordered column={1} items={buildDescriptionsItems(detail.newData || detail.oldData, [])} />
            )}
          </div>
        ) : (
          <div>Không có dữ liệu</div>
        )}
      </Modal>
    </div>
  );
};

export default AuditLogTab;
