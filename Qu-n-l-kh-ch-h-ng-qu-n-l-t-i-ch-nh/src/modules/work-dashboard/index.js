import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, DatePicker, Drawer, Input, Progress, Row, Select, Space, Statistic, Table, Tabs, Tag, Typography, message } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import axios from 'axios';
import dayjs from 'dayjs';
import { useAuth } from '../../context/AuthContext';
import './index.css';

const { Title, Text } = Typography;

const statusOptions = [
  { value: 'NOT_STARTED', label: 'Chưa bắt đầu', color: 'default' },
  { value: 'IN_PROGRESS', label: 'Đang thực hiện', color: 'blue' },
  { value: 'IN_REVIEW', label: 'Đang kiểm tra', color: 'gold' },
  { value: 'DONE', label: 'Hoàn thành', color: 'green' },
  { value: 'PAUSED', label: 'Tạm dừng', color: 'orange' },
  { value: 'CANCELLED', label: 'Hủy', color: 'red' },
];

const WorkDashboard = () => {
  const { user } = useAuth();

  const isGlobal = useMemo(() => {
    const role = user?.role;
    return ['admin', 'ceo', 'assistant_ceo'].includes(role);
  }, [user]);

  const canViewCompany = useMemo(() => {
    const role = user?.role;
    return ['director', 'giam_doc', 'assistant_director', 'ip_manager', 'manager', 'quan_ly'].includes(role);
  }, [user]);

  const canView = useMemo(() => {
    return !!user?.role;
  }, [user]);

  const isSelfOnly = useMemo(() => {
    return canView && !isGlobal && !canViewCompany;
  }, [canView, canViewCompany, isGlobal]);

  const [companyOptions, setCompanyOptions] = useState([]);
  const [companyId, setCompanyId] = useState(null);
  const [dateRange, setDateRange] = useState(null);
  const [status, setStatus] = useState(null);
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [search, setSearch] = useState('');
  const [assigneeUserId, setAssigneeUserId] = useState(null);

  const [summary, setSummary] = useState(null);
  const [summaryLoading, setSummaryLoading] = useState(false);

  const [team, setTeam] = useState([]);
  const [teamLoading, setTeamLoading] = useState(false);

  const [tasks, setTasks] = useState([]);
  const [tasksTotal, setTasksTotal] = useState(0);
  const [tasksLoading, setTasksLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [drawerUser, setDrawerUser] = useState(null);
  const [userKanban, setUserKanban] = useState({});
  const [userTimeline, setUserTimeline] = useState([]);
  const [userTimelineTotal, setUserTimelineTotal] = useState(0);
  const [userTimelinePage, setUserTimelinePage] = useState(1);
  const [userTimelinePageSize, setUserTimelinePageSize] = useState(20);
  const [drillLoading, setDrillLoading] = useState(false);

  const statusTag = useCallback((v) => {
    const meta = statusOptions.find((x) => x.value === v);
    return <Tag color={meta?.color || 'default'}>{meta?.label || v}</Tag>;
  }, []);

  const buildParams = useCallback(() => {
    const params = {};
    if (isGlobal && companyId) params.companyId = companyId;
    if (typeof assigneeUserId === 'number') params.assigneeUserId = assigneeUserId;
    if (status) params.status = status;
    if (overdueOnly) params.overdueOnly = true;
    if (search) params.search = search;
    if (dateRange?.length === 2) {
      params.fromDate = dayjs(dateRange[0]).format('YYYY-MM-DD');
      params.toDate = dayjs(dateRange[1]).format('YYYY-MM-DD');
    }
    return params;
  }, [assigneeUserId, companyId, dateRange, isGlobal, overdueOnly, search, status]);

  const fetchCompanies = useCallback(async () => {
    if (!isGlobal) return;
    try {
      const res = await axios.get('/api/work-dashboard/companies');
      setCompanyOptions(res.data.items || []);
    } catch {
      setCompanyOptions([]);
    }
  }, [isGlobal]);

  const fetchSummary = useCallback(async () => {
    if (!canView) return;
    setSummaryLoading(true);
    try {
      const res = await axios.get('/api/work-dashboard/summary', { params: buildParams() });
      setSummary(res.data);
    } catch (e) {
      setSummary(null);
      message.error('Không thể tải thống kê công việc');
    } finally {
      setSummaryLoading(false);
    }
  }, [buildParams, canView]);

  const fetchTeam = useCallback(async () => {
    if (!canView) return;
    setTeamLoading(true);
    try {
      const res = await axios.get('/api/work-dashboard/team', { params: buildParams() });
      setTeam(res.data.items || []);
    } catch {
      setTeam([]);
    } finally {
      setTeamLoading(false);
    }
  }, [buildParams, canView]);

  const fetchTasks = useCallback(async () => {
    if (!canView) return;
    setTasksLoading(true);
    try {
      const params = { ...buildParams(), page, pageSize };
      const res = await axios.get('/api/work-dashboard/tasks', { params });
      setTasks(res.data.items || []);
      setTasksTotal(res.data.total || 0);
    } catch {
      setTasks([]);
      setTasksTotal(0);
    } finally {
      setTasksLoading(false);
    }
  }, [buildParams, canView, page, pageSize]);

  const refreshAll = useCallback(() => {
    fetchSummary();
    fetchTeam();
    fetchTasks();
  }, [fetchSummary, fetchTeam, fetchTasks]);

  const fetchUserDrill = useCallback(async () => {
    if (!drawerUser?.assigneeUserId) return;
    setDrillLoading(true);
    try {
      const base = buildParams();
      const params = { ...base, assigneeUserId: drawerUser.assigneeUserId };
      const [kRes, tRes] = await Promise.all([
        axios.get('/api/work-dashboard/user-kanban', { params }),
        axios.get('/api/work-dashboard/user-timeline', { params: { ...params, page: userTimelinePage, pageSize: userTimelinePageSize } }),
      ]);
      setUserKanban(kRes.data.columns || {});
      setUserTimeline(tRes.data.items || []);
      setUserTimelineTotal(tRes.data.total || 0);
    } catch {
      setUserKanban({});
      setUserTimeline([]);
      setUserTimelineTotal(0);
    } finally {
      setDrillLoading(false);
    }
  }, [buildParams, drawerUser, userTimelinePage, userTimelinePageSize]);

  useEffect(() => {
    fetchCompanies();
  }, [fetchCompanies]);

  useEffect(() => {
    if (!canView) return;
    fetchSummary();
    fetchTeam();
    setPage(1);
  }, [canView, fetchSummary, fetchTeam]);

  useEffect(() => {
    if (!canView) return;
    fetchTasks();
  }, [canView, fetchTasks]);

  useEffect(() => {
    if (!drawerOpen) return;
    fetchUserDrill();
  }, [drawerOpen, fetchUserDrill]);

  const teamColumns = useMemo(() => {
    return [
      { title: 'Nhân viên', dataIndex: 'assigneeName', key: 'assigneeName', width: 220 },
      { title: 'Tổng', dataIndex: 'total', key: 'total', width: 90 },
      { title: 'Quá hạn', dataIndex: 'overdue', key: 'overdue', width: 110, render: (v) => (v > 0 ? <Tag color="red">{v}</Tag> : <Tag>0</Tag>) },
      { title: 'Hoàn thành', dataIndex: 'doneRate', key: 'doneRate', width: 220, render: (v) => <Progress percent={v || 0} size="small" /> },
      { title: 'TB tiến độ', dataIndex: 'avgProgress', key: 'avgProgress', width: 110, render: (v) => `${v || 0}%` },
      { title: 'Workload', dataIndex: 'workloadMinutes', key: 'workloadMinutes', width: 120, render: (v) => `${v || 0}p` },
    ];
  }, []);

  const taskColumns = useMemo(() => {
    return [
      isGlobal
        ? { title: 'Cty', dataIndex: 'companyCode', key: 'companyCode', width: 90 }
        : null,
      { title: 'ID', dataIndex: 'id', key: 'id', width: 80 },
      { title: 'Task', dataIndex: 'name', key: 'name', width: 280 },
      { title: 'Dự án', dataIndex: 'projectCode', key: 'projectCode', width: 170, render: (v, r) => (v ? `${v}${r.projectName ? ` - ${r.projectName}` : ''}` : '') },
      { title: 'Module', dataIndex: 'moduleName', key: 'moduleName', width: 180 },
      { title: 'Người làm', dataIndex: 'assigneeName', key: 'assigneeName', width: 170 },
      { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 150, render: (v) => statusTag(v) },
      { title: 'Tiến độ', dataIndex: 'progress', key: 'progress', width: 140, render: (v) => <Progress percent={v || 0} size="small" /> },
      { title: 'Kết thúc', dataIndex: 'endDate', key: 'endDate', width: 120 },
      { title: 'Quá hạn', dataIndex: 'deadlineOverdue', key: 'deadlineOverdue', width: 100, render: (v) => (v ? <Tag color="red">Quá hạn</Tag> : '') },
    ].filter(Boolean);
  }, [isGlobal, statusTag]);

  const userKanbanColumns = useMemo(() => {
    const cols = {};
    statusOptions.forEach((s) => {
      cols[s.value] = userKanban[s.value] || [];
    });
    return cols;
  }, [userKanban]);

  const timelineColumns = useMemo(() => {
    return [
      isGlobal ? { title: 'Cty', dataIndex: 'companyCode', key: 'companyCode', width: 90 } : null,
      { title: 'ID', dataIndex: 'id', key: 'id', width: 80 },
      { title: 'Task', dataIndex: 'name', key: 'name', width: 280 },
      { title: 'Dự án', dataIndex: 'projectCode', key: 'projectCode', width: 170, render: (v, r) => (v ? `${v}${r.projectName ? ` - ${r.projectName}` : ''}` : '') },
      { title: 'Module', dataIndex: 'moduleName', key: 'moduleName', width: 180 },
      { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 150, render: (v) => statusTag(v) },
      { title: 'Tiến độ', dataIndex: 'progress', key: 'progress', width: 140, render: (v) => <Progress percent={v || 0} size="small" /> },
      { title: 'Kết thúc', dataIndex: 'endDate', key: 'endDate', width: 120 },
      { title: 'Quá hạn', dataIndex: 'deadlineOverdue', key: 'deadlineOverdue', width: 100, render: (v) => (v ? <Tag color="red">Quá hạn</Tag> : '') },
    ].filter(Boolean);
  }, [isGlobal, statusTag]);

  if (!canView) {
    return (
      <Card>
        <Text>Bạn không có quyền xem Dashboard công việc.</Text>
      </Card>
    );
  }

  const byStatus = summary?.byStatus || {};

  return (
    <div className="work-dashboard-page">
      <div className="work-dashboard-header">
        <div>
          <Title level={4} style={{ margin: 0 }}>
            Dashboard công việc
          </Title>
        </div>
      </div>

      <Card className="work-filters" bodyStyle={{ padding: 12 }}>
        <Space wrap style={{ width: '100%', justifyContent: 'space-between' }}>
          <Space wrap>
            {isGlobal && (
              <Select
                allowClear
                placeholder="Tất cả công ty"
                style={{ width: 260 }}
                value={companyId}
                onChange={(v) => {
                  setCompanyId(v || null);
                  setAssigneeUserId(null);
                  setDrawerOpen(false);
                  setPage(1);
                }}
                options={(companyOptions || []).map((c) => ({ value: c.id, label: `${c.code} - ${c.name}` }))}
              />
            )}
            <DatePicker.RangePicker
              value={dateRange}
              onChange={(v) => {
                setDateRange(v);
                setPage(1);
              }}
              format="YYYY-MM-DD"
              style={{ width: 260 }}
            />
            <Select
              allowClear
              placeholder="Trạng thái"
              value={status}
              onChange={(v) => {
                setStatus(v || null);
                setPage(1);
              }}
              style={{ width: 190 }}
              options={statusOptions.map((s) => ({ value: s.value, label: s.label }))}
            />
            <Select
              value={overdueOnly ? 'overdue' : 'all'}
              onChange={(v) => {
                setOverdueOnly(v === 'overdue');
                setPage(1);
              }}
              style={{ width: 160 }}
              options={[
                { value: 'all', label: 'Tất cả' },
                { value: 'overdue', label: 'Chỉ quá hạn' },
              ]}
            />
            <Input
              allowClear
              placeholder="Tìm task"
              style={{ width: 220 }}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onPressEnter={() => {
                setPage(1);
                refreshAll();
              }}
            />
          </Space>

          <Space wrap>
            {typeof assigneeUserId === 'number' ? (
              <Tag closable onClose={() => setAssigneeUserId(null)} color="blue">
                Đang lọc theo nhân viên
              </Tag>
            ) : null}
            <Button
              onClick={() => {
                setCompanyId(null);
                setDateRange(null);
                setStatus(null);
                setOverdueOnly(false);
                setSearch('');
                setAssigneeUserId(null);
                setDrawerOpen(false);
                setPage(1);
              }}
            >
              Reset
            </Button>
            <Button icon={<ReloadOutlined />} onClick={refreshAll}>
              Refresh
            </Button>
          </Space>
        </Space>
      </Card>

      <Row gutter={[12, 12]}>
        <Col xs={12} md={6}>
          <Card className="work-kpi work-kpi--total" loading={summaryLoading}>
            <Statistic title="Tổng task" value={summary?.total || 0} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card className="work-kpi work-kpi--overdue" loading={summaryLoading}>
            <Statistic title="Quá hạn" value={summary?.overdue || 0} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card className="work-kpi work-kpi--avg" loading={summaryLoading}>
            <Statistic title="TB tiến độ" value={`${summary?.avgProgress || 0}%`} />
          </Card>
        </Col>
        <Col xs={12} md={6}>
          <Card className="work-kpi work-kpi--done" loading={summaryLoading}>
            <Statistic title="Hoàn thành" value={`${summary?.doneRate || 0}%`} />
          </Card>
        </Col>
      </Row>

      <Row gutter={[12, 12]}>
        {!isSelfOnly ? (
          <Col xs={24} lg={10}>
            <Card title="Theo nhân viên" className="work-card">
              <Table
                rowKey={(r) => `${r.assigneeUserId ?? 'unassigned'}`}
                loading={teamLoading}
                columns={teamColumns}
                dataSource={team}
                className="work-table-clickable"
                pagination={false}
                rowClassName={(record) => {
                  const id = typeof record.assigneeUserId === 'number' ? record.assigneeUserId : null;
                  if (id && typeof assigneeUserId === 'number' && assigneeUserId === id) return 'work-row-selected';
                  return '';
                }}
                onRow={(record) => ({
                  onClick: () => {
                    const id = typeof record.assigneeUserId === 'number' ? record.assigneeUserId : null;
                    setAssigneeUserId(id);
                    setPage(1);
                    if (id) {
                      setDrawerUser(record);
                      setUserTimelinePage(1);
                      setDrawerOpen(true);
                    }
                  },
                })}
              />
            </Card>
          </Col>
        ) : null}
        <Col xs={24} lg={isSelfOnly ? 24 : 14}>
          <Card
            title={isSelfOnly ? 'Công việc của tôi' : 'Danh sách task'}
            className="work-card"
            extra={
              <Space size={8} wrap className="work-status-summary">
                <Text strong>Trạng thái:</Text>
                <Tag color="blue">
                  {byStatus?.inProgress || 0} Đang làm
                </Tag>
                <Tag color="gold">
                  {byStatus?.inReview || 0} Kiểm tra
                </Tag>
                <Tag color="green">
                  {byStatus?.done || 0} Hoàn thành
                </Tag>
              </Space>
            }
          >
            <Table
              rowKey="id"
              loading={tasksLoading}
              columns={taskColumns}
              dataSource={tasks}
              scroll={{ x: 1180 }}
              rowClassName={(record) => (record.deadlineOverdue ? 'work-row-overdue' : '')}
              pagination={{
                current: page,
                pageSize,
                total: tasksTotal,
                showSizeChanger: true,
                onChange: (p, ps) => {
                  setPage(p);
                  setPageSize(ps);
                },
              }}
            />
          </Card>
        </Col>
      </Row>

      {!isSelfOnly ? (
        <Drawer
          open={drawerOpen}
          onClose={() => setDrawerOpen(false)}
          title={drawerUser ? `Công việc: ${drawerUser.assigneeName}` : 'Công việc'}
          width={920}
          destroyOnClose
        >
          {drawerUser && (
            <Row gutter={[12, 12]} style={{ marginBottom: 12 }}>
              <Col xs={12} md={6}>
                <Card size="small" className="work-kpi" loading={drillLoading}>
                  <Statistic title="Tổng task" value={drawerUser.total || 0} />
                </Card>
              </Col>
              <Col xs={12} md={6}>
                <Card size="small" className="work-kpi" loading={drillLoading}>
                  <Statistic title="Quá hạn" value={drawerUser.overdue || 0} />
                </Card>
              </Col>
              <Col xs={12} md={6}>
                <Card size="small" className="work-kpi" loading={drillLoading}>
                  <Statistic title="TB tiến độ" value={`${drawerUser.avgProgress || 0}%`} />
                </Card>
              </Col>
              <Col xs={12} md={6}>
                <Card size="small" className="work-kpi" loading={drillLoading}>
                  <Statistic title="Hoàn thành" value={`${drawerUser.doneRate || 0}%`} />
                </Card>
              </Col>
            </Row>
          )}

          <Tabs
            items={[
              {
                key: 'kanban',
                label: 'Kanban',
                children: (
                  <Row gutter={[12, 12]}>
                    {statusOptions.map((s) => (
                      <Col xs={24} md={12} lg={8} key={s.value}>
                        <Card title={s.label} size="small" className="work-kanban-col" bodyStyle={{ maxHeight: 520, overflow: 'auto' }}>
                          {(userKanbanColumns[s.value] || []).map((t) => (
                            <Card key={t.id} size="small" className="work-task-card">
                              <Space direction="vertical" style={{ width: '100%' }} size={6}>
                                <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8 }}>
                                  <div style={{ fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis' }}>{t.name}</div>
                                  {t.deadlineOverdue ? <Tag color="red">Quá hạn</Tag> : null}
                                </div>
                                <Text type="secondary">
                                  {t.projectCode ? `${t.projectCode} • ` : ''}{t.moduleName || ''}
                                </Text>
                                <Progress percent={t.progress || 0} size="small" />
                              </Space>
                            </Card>
                          ))}
                        </Card>
                      </Col>
                    ))}
                  </Row>
                ),
              },
              {
                key: 'timeline',
                label: 'Timeline',
                children: (
                  <Table
                    rowKey="id"
                    loading={drillLoading}
                    columns={timelineColumns}
                    dataSource={userTimeline}
                    scroll={{ x: 1180 }}
                    pagination={{
                      current: userTimelinePage,
                      pageSize: userTimelinePageSize,
                      total: userTimelineTotal,
                      showSizeChanger: true,
                      onChange: (p, ps) => {
                        setUserTimelinePage(p);
                        setUserTimelinePageSize(ps);
                      },
                    }}
                  />
                ),
              },
            ]}
          />
        </Drawer>
      ) : null}
    </div>
  );
};

export default WorkDashboard;
