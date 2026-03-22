import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, DatePicker, Descriptions, Form, Input, InputNumber, Modal, Progress, Row, Select, Space, Statistic, Table, Tabs, Tag, Typography, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import axios from 'axios';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import './ProjectDetail.css';

const { Title, Text } = Typography;

const statusOptions = [
  { value: 'NOT_STARTED', label: 'Chưa bắt đầu', color: 'default' },
  { value: 'IN_PROGRESS', label: 'Đang thực hiện', color: 'blue' },
  { value: 'IN_REVIEW', label: 'Đang kiểm tra', color: 'gold' },
  { value: 'DONE', label: 'Hoàn thành', color: 'green' },
  { value: 'PAUSED', label: 'Tạm dừng', color: 'orange' },
  { value: 'CANCELLED', label: 'Hủy', color: 'red' },
];

const priorityOptions = [
  { value: 'LOW', label: 'Thấp' },
  { value: 'MEDIUM', label: 'Trung bình' },
  { value: 'HIGH', label: 'Cao' },
  { value: 'URGENT', label: 'Khẩn' },
];

const ProjectDetail = () => {
  const { id } = useParams();
  const projectId = Number(id);
  const navigate = useNavigate();
  const { user } = useAuth();

  const [project, setProject] = useState(null);
  const [modules, setModules] = useState([]);
  const [kanban, setKanban] = useState({});
  const [selectedModuleId, setSelectedModuleId] = useState(null);
  const [loading, setLoading] = useState(false);
  const [teamSummary, setTeamSummary] = useState([]);
  const [teamSummaryLoading, setTeamSummaryLoading] = useState(false);
  const [teamMeta, setTeamMeta] = useState({ totalTasks: 0, overdueTasks: 0 });
  const [selectedAssigneeUserId, setSelectedAssigneeUserId] = useState(null);
  const [teamTasks, setTeamTasks] = useState([]);
  const [teamTasksTotal, setTeamTasksTotal] = useState(0);
  const [teamTaskStatus, setTeamTaskStatus] = useState(null);
  const [teamOverdueMode, setTeamOverdueMode] = useState('all');
  const [teamSearch, setTeamSearch] = useState('');
  const [teamTaskPage, setTeamTaskPage] = useState(1);
  const [teamTaskPageSize, setTeamTaskPageSize] = useState(20);

  const [moduleOpen, setModuleOpen] = useState(false);
  const [taskOpen, setTaskOpen] = useState(false);
  const [moduleForm] = Form.useForm();
  const [taskForm] = Form.useForm();

  const canManage = useMemo(() => {
    const role = user?.role;
    return ['admin', 'ceo', 'assistant_ceo', 'director', 'giam_doc', 'assistant_director', 'ip_manager', 'manager', 'quan_ly'].includes(role);
  }, [user]);

  const fetchProject = useCallback(async () => {
    setLoading(true);
    try {
      const res = await axios.get(`/api/projects/${projectId}`);
      setProject(res.data);
    } catch (e) {
      message.error('Không thể tải thông tin dự án');
    } finally {
      setLoading(false);
    }
  }, [projectId]);

  const fetchModules = useCallback(async () => {
    try {
      const res = await axios.get('/api/project-modules', { params: { projectId } });
      setModules(res.data.items || []);
    } catch (e) {
      setModules([]);
    }
  }, [projectId]);

  const fetchKanban = useCallback(async () => {
    try {
      const params = { projectId };
      if (selectedModuleId) {
        params.moduleId = selectedModuleId;
      }
      const res = await axios.get('/api/project-tasks/kanban', { params });
      setKanban(res.data.columns || {});
    } catch (e) {
      setKanban({});
    }
  }, [projectId, selectedModuleId]);

  const fetchTeamSummary = useCallback(async () => {
    if (!canManage) return;
    setTeamSummaryLoading(true);
    try {
      const params = { projectId };
      if (selectedModuleId) params.moduleId = selectedModuleId;
      const res = await axios.get('/api/project-tasks/team-summary', { params });
      setTeamSummary(res.data.items || []);
      setTeamMeta({ totalTasks: res.data.totalTasks || 0, overdueTasks: res.data.overdueTasks || 0 });
    } catch (e) {
      setTeamSummary([]);
      setTeamMeta({ totalTasks: 0, overdueTasks: 0 });
    } finally {
      setTeamSummaryLoading(false);
    }
  }, [canManage, projectId, selectedModuleId]);

  const fetchTeamTasks = useCallback(async () => {
    if (!canManage) return;
    const params = {
      projectId,
      page: teamTaskPage,
      pageSize: teamTaskPageSize,
    };
    if (selectedModuleId) params.moduleId = selectedModuleId;
    if (typeof selectedAssigneeUserId === 'number') params.assigneeUserId = selectedAssigneeUserId;
    if (teamTaskStatus) params.status = teamTaskStatus;
    if (teamSearch) params.search = teamSearch;
    if (teamOverdueMode === 'overdue') params.overdueOnly = true;

    try {
      const res = await axios.get('/api/project-tasks/team-tasks', { params });
      setTeamTasks(res.data.items || []);
      setTeamTasksTotal(res.data.total || 0);
    } catch (e) {
      setTeamTasks([]);
      setTeamTasksTotal(0);
    }
  }, [canManage, projectId, selectedModuleId, selectedAssigneeUserId, teamTaskPage, teamTaskPageSize, teamTaskStatus, teamSearch, teamOverdueMode]);

  useEffect(() => {
    if (!Number.isFinite(projectId)) return;
    fetchProject();
    fetchModules();
  }, [fetchProject, fetchModules, projectId]);

  useEffect(() => {
    fetchKanban();
  }, [fetchKanban]);

  useEffect(() => {
    fetchTeamSummary();
    setTeamTaskPage(1);
  }, [fetchTeamSummary]);

  useEffect(() => {
    fetchTeamTasks();
  }, [fetchTeamTasks]);

  const openCreateModule = () => {
    moduleForm.resetFields();
    moduleForm.setFieldsValue({ status: 'NOT_STARTED', priority: 'MEDIUM' });
    setModuleOpen(true);
  };

  const createModule = async () => {
    try {
      const values = await moduleForm.validateFields();
      const payload = {
        projectLegacyId: projectId,
        name: values.name,
        ownerUserId: values.ownerUserId || null,
        startDate: values.startDate ? values.startDate.format('YYYY-MM-DD') : null,
        endDate: values.endDate ? values.endDate.format('YYYY-MM-DD') : null,
        status: values.status,
        priority: values.priority,
        description: values.description || null,
      };
      await axios.post('/api/project-modules', payload);
      message.success('Đã tạo module');
      setModuleOpen(false);
      moduleForm.resetFields();
      fetchModules();
    } catch (e) {
      if (e?.errorFields) return;
      message.error('Không thể tạo module');
    }
  };

  const openCreateTask = () => {
    taskForm.resetFields();
    taskForm.setFieldsValue({
      status: 'NOT_STARTED',
      priority: 'MEDIUM',
      progress: 0,
      moduleId: selectedModuleId || null,
    });
    setTaskOpen(true);
  };

  const createTask = async () => {
    try {
      const values = await taskForm.validateFields();
      const payload = {
        projectId,
        moduleId: values.moduleId,
        name: values.name,
        assigneeUserId: values.assigneeUserId || null,
        status: values.status,
        priority: values.priority,
        progress: values.progress || 0,
        description: values.description || null,
        notes: values.notes || null,
        startDate: values.startDate ? values.startDate.format('YYYY-MM-DD') : null,
        endDate: values.endDate ? values.endDate.format('YYYY-MM-DD') : null,
        estimatedMinutes: values.estimatedMinutes || null,
        actualMinutes: values.actualMinutes || null,
      };
      await axios.post('/api/project-tasks', payload);
      message.success('Đã tạo task');
      setTaskOpen(false);
      taskForm.resetFields();
      fetchKanban();
      fetchProject();
      fetchModules();
    } catch (e) {
      if (e?.errorFields) return;
      message.error('Không thể tạo task');
    }
  };

  const updateTask = async (taskId, patch) => {
    try {
      await axios.put(`/api/project-tasks/${taskId}`, patch);
      fetchKanban();
      fetchProject();
      fetchModules();
    } catch (e) {
      message.error('Không thể cập nhật task');
    }
  };

  const renderTaskCard = (t) => {
    const statusMeta = statusOptions.find((s) => s.value === t.status);
    return (
      <Card key={t.id} size="small" style={{ marginBottom: 8 }}>
        <Space direction="vertical" style={{ width: '100%' }} size={6}>
          <div style={{ display: 'flex', justifyContent: 'space-between', gap: 8 }}>
            <div style={{ fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis' }}>{t.name}</div>
            <Tag color={statusMeta?.color || 'default'}>{statusMeta?.label || t.status}</Tag>
          </div>
          <Progress percent={t.progress || 0} size="small" />
          <Space wrap>
            <Select
              size="small"
              value={t.status}
              style={{ width: 160 }}
              options={statusOptions.map((s) => ({ value: s.value, label: s.label }))}
              onChange={(v) => updateTask(t.id, { status: v })}
            />
            <InputNumber
              size="small"
              value={t.progress || 0}
              min={0}
              max={100}
              onChange={(v) => updateTask(t.id, { progress: typeof v === 'number' ? v : 0 })}
            />
            {t.deadlineOverdue && <Tag color="red">Quá hạn</Tag>}
          </Space>
        </Space>
      </Card>
    );
  };

  const kanbanColumns = useMemo(() => {
    const columns = {};
    statusOptions.forEach((s) => {
      columns[s.value] = kanban[s.value] || [];
    });
    return columns;
  }, [kanban]);

  const statusLabel = useCallback((status) => {
    return statusOptions.find((x) => x.value === status)?.label || status;
  }, []);

  const teamSummaryColumns = useMemo(() => {
    return [
      { title: 'Nhân viên', dataIndex: 'assigneeName', key: 'assigneeName', width: 220 },
      { title: 'Tổng', dataIndex: 'total', key: 'total', width: 90 },
      {
        title: 'Quá hạn',
        dataIndex: 'overdue',
        key: 'overdue',
        width: 110,
        render: (v) => (v > 0 ? <Tag color="red">{v}</Tag> : <Tag>0</Tag>),
      },
      {
        title: 'Hoàn thành',
        dataIndex: 'doneRate',
        key: 'doneRate',
        width: 200,
        render: (v) => <Progress percent={v || 0} size="small" />,
      },
      { title: 'TB tiến độ', dataIndex: 'avgProgress', key: 'avgProgress', width: 110, render: (v) => `${v || 0}%` },
      { title: 'Workload (phút)', dataIndex: 'workloadMinutes', key: 'workloadMinutes', width: 140 },
    ];
  }, []);

  const teamTaskColumns = useMemo(() => {
    return [
      { title: 'ID', dataIndex: 'id', key: 'id', width: 80 },
      { title: 'Tên task', dataIndex: 'name', key: 'name', width: 260 },
      { title: 'Module', dataIndex: 'moduleName', key: 'moduleName', width: 180 },
      { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 160, render: (v) => statusLabel(v) },
      { title: 'Tiến độ', dataIndex: 'progress', key: 'progress', width: 140, render: (v) => <Progress percent={v || 0} size="small" /> },
      { title: 'Kết thúc', dataIndex: 'endDate', key: 'endDate', width: 120 },
      { title: 'Quá hạn', dataIndex: 'deadlineOverdue', key: 'deadlineOverdue', width: 100, render: (v) => (v ? <Tag color="red">Quá hạn</Tag> : '') },
    ];
  }, [statusLabel]);

  if (!project) {
    return (
      <div>
        <Button onClick={() => navigate('/projects')}>Quay lại</Button>
      </div>
    );
  }

  const projectStatus = statusOptions.find((x) => x.value === project.status);

  return (
    <div className="project-detail-page">
      <Card className="project-detail-header" bodyStyle={{ padding: 16 }}>
        <div className="project-detail-header-top">
          <Space>
            <Button onClick={() => navigate('/projects')}>Quay lại</Button>
            <div>
              <Title level={4} style={{ margin: 0 }}>
                {project.code} - {project.name}
              </Title>
              <Space size={6} wrap>
                {projectStatus ? <Tag color={projectStatus.color}>{projectStatus.label}</Tag> : <Tag>{project.status}</Tag>}
                {project.customerName ? <Text type="secondary">KH: {project.customerName}</Text> : null}
                {project.managerName ? <Text type="secondary">QL: {project.managerName}</Text> : null}
              </Space>
            </div>
          </Space>
          <div className="project-detail-header-progress">
            <Text type="secondary">Tiến độ</Text>
            <Progress percent={project.progress || 0} />
          </div>
        </div>

        <Row gutter={[12, 12]} style={{ marginTop: 8 }}>
          <Col xs={12} md={6}>
            <Card size="small" className="project-detail-kpi">
              <Statistic title="Module" value={project.moduleCount || 0} />
            </Card>
          </Col>
          <Col xs={12} md={6}>
            <Card size="small" className="project-detail-kpi">
              <Statistic title="Task" value={project.taskCount || 0} />
            </Card>
          </Col>
          <Col xs={12} md={6}>
            <Card size="small" className="project-detail-kpi">
              <Statistic title="Hoàn thành" value={project.taskDoneCount || 0} />
            </Card>
          </Col>
          <Col xs={12} md={6}>
            <Card size="small" className="project-detail-kpi">
              <Statistic title="Quá hạn" value={teamMeta.overdueTasks || 0} />
            </Card>
          </Col>
        </Row>
      </Card>

      <Row gutter={[12, 12]}>
        <Col xs={24} lg={8}>
          <Card title="Thông tin dự án" loading={loading}>
            <Descriptions size="small" column={1}>
              <Descriptions.Item label="Khách hàng">{project.customerName || '-'}</Descriptions.Item>
              <Descriptions.Item label="Quản lý">{project.managerName || '-'}</Descriptions.Item>
              <Descriptions.Item label="Bắt đầu">{project.startDate || '-'}</Descriptions.Item>
              <Descriptions.Item label="Kết thúc">{project.endDate || '-'}</Descriptions.Item>
              <Descriptions.Item label="Trạng thái">{statusOptions.find((x) => x.value === project.status)?.label || project.status}</Descriptions.Item>
              <Descriptions.Item label="Tiến độ">
                <Progress percent={project.progress || 0} size="small" />
              </Descriptions.Item>
            </Descriptions>
          </Card>
        </Col>
        <Col xs={24} lg={16}>
          <Card>
            <Tabs
              items={[
                {
                  key: 'modules',
                  label: 'Module',
                  children: (
                    <div>
                      <Space style={{ marginBottom: 12 }}>
                        <Select
                          allowClear
                          placeholder="Lọc theo module"
                          style={{ width: 260 }}
                          value={selectedModuleId}
                          onChange={(v) => setSelectedModuleId(v || null)}
                          options={(modules || []).map((m) => ({ value: m.id, label: `${m.id} - ${m.name}` }))}
                        />
                        {canManage && (
                          <Button type="primary" icon={<PlusOutlined />} onClick={openCreateModule}>
                            Thêm module
                          </Button>
                        )}
                      </Space>
                      <Row gutter={[12, 12]}>
                        {(modules || []).map((m) => (
                          <Col xs={24} md={12} key={m.id}>
                            <Card
                              size="small"
                              title={`${m.id} - ${m.name}`}
                              extra={
                                <Button onClick={() => setSelectedModuleId(m.id)}>
                                  Xem Kanban
                                </Button>
                              }
                            >
                              <Space direction="vertical" style={{ width: '100%' }}>
                                <div>Trạng thái: {statusOptions.find((x) => x.value === m.status)?.label || m.status}</div>
                                <div>Ưu tiên: {priorityOptions.find((x) => x.value === m.priority)?.label || m.priority}</div>
                                <div>Start: {m.startDate || '-'} | End: {m.endDate || '-'}</div>
                                <div>
                                  Tiến độ: <Progress percent={m.progress || 0} size="small" />
                                </div>
                                <div>Task: {m.taskCount || 0} | Done: {m.taskDoneCount || 0}</div>
                              </Space>
                            </Card>
                          </Col>
                        ))}
                      </Row>
                    </div>
                  ),
                },
                {
                  key: 'kanban',
                  label: 'Kanban',
                  children: (
                    <div>
                      <Space style={{ marginBottom: 12 }}>
                        <Select
                          allowClear
                          placeholder="Chọn module"
                          style={{ width: 260 }}
                          value={selectedModuleId}
                          onChange={(v) => setSelectedModuleId(v || null)}
                          options={(modules || []).map((m) => ({ value: m.id, label: `${m.id} - ${m.name}` }))}
                        />
                        <Button type="primary" icon={<PlusOutlined />} onClick={openCreateTask} disabled={!selectedModuleId}>
                          Thêm task
                        </Button>
                      </Space>
                      <Row gutter={[12, 12]}>
                        {statusOptions.map((s) => (
                          <Col xs={24} md={8} lg={8} key={s.value}>
                            <Card title={s.label} size="small" className="kanban-col" bodyStyle={{ maxHeight: 520, overflow: 'auto' }}>
                              {(kanbanColumns[s.value] || []).map(renderTaskCard)}
                            </Card>
                          </Col>
                        ))}
                      </Row>
                    </div>
                  ),
                },
                canManage
                  ? {
                      key: 'team',
                      label: 'Nhân viên',
                      children: (
                        <div>
                          <Space style={{ marginBottom: 12 }} wrap>
                            <Select
                              allowClear
                              placeholder="Chọn module"
                              style={{ width: 220 }}
                              value={selectedModuleId}
                              onChange={(v) => {
                                setSelectedModuleId(v || null);
                                setTeamTaskPage(1);
                              }}
                              options={(modules || []).map((m) => ({ value: m.id, label: `${m.id} - ${m.name}` }))}
                            />
                            <Input
                              placeholder="Tìm task theo tên"
                              style={{ width: 240 }}
                              value={teamSearch}
                              onChange={(e) => setTeamSearch(e.target.value)}
                              onPressEnter={() => {
                                setTeamTaskPage(1);
                                fetchTeamTasks();
                              }}
                            />
                            <Select
                              value={teamTaskStatus}
                              allowClear
                              placeholder="Trạng thái"
                              style={{ width: 180 }}
                              onChange={(v) => {
                                setTeamTaskStatus(v || null);
                                setTeamTaskPage(1);
                              }}
                              options={statusOptions.map((s) => ({ value: s.value, label: s.label }))}
                            />
                            <Select
                              value={teamOverdueMode}
                              style={{ width: 160 }}
                              onChange={(v) => {
                                setTeamOverdueMode(v);
                                setTeamTaskPage(1);
                              }}
                              options={[
                                { value: 'all', label: 'Tất cả' },
                                { value: 'overdue', label: 'Chỉ quá hạn' },
                              ]}
                            />
                            <Button
                              onClick={() => {
                                fetchTeamSummary();
                                fetchTeamTasks();
                              }}
                            >
                              Refresh
                            </Button>
                          </Space>

                          <Row gutter={[12, 12]}>
                            <Col xs={24} lg={10}>
                              <Card title="Thống kê theo nhân viên">
                                <Table
                                  rowKey={(r) => `${r.assigneeUserId ?? 'unassigned'}`}
                                  loading={teamSummaryLoading}
                                  columns={teamSummaryColumns}
                                  dataSource={teamSummary}
                                  pagination={false}
                                  rowClassName={(record) => {
                                    const rid = typeof record.assigneeUserId === 'number' ? record.assigneeUserId : null;
                                    return rid && selectedAssigneeUserId === rid ? 'team-row-selected' : '';
                                  }}
                                  onRow={(record) => ({
                                    onClick: () => {
                                      setSelectedAssigneeUserId(typeof record.assigneeUserId === 'number' ? record.assigneeUserId : null);
                                      setTeamTaskPage(1);
                                    },
                                  })}
                                />
                              </Card>
                            </Col>
                            <Col xs={24} lg={14}>
                              <Card title={`Danh sách task${selectedAssigneeUserId ? ` - User #${selectedAssigneeUserId}` : ''}`}>
                                <Table
                                  rowKey="id"
                                  columns={teamTaskColumns}
                                  dataSource={teamTasks}
                                  pagination={{
                                    current: teamTaskPage,
                                    pageSize: teamTaskPageSize,
                                    total: teamTasksTotal,
                                    showSizeChanger: true,
                                    onChange: (p, ps) => {
                                      setTeamTaskPage(p);
                                      setTeamTaskPageSize(ps);
                                    },
                                  }}
                                />
                              </Card>
                            </Col>
                          </Row>
                        </div>
                      ),
                    }
                  : null,
              ].filter(Boolean)}
            />
          </Card>
        </Col>
      </Row>

      <Modal
        open={moduleOpen}
        title="Thêm module"
        onCancel={() => setModuleOpen(false)}
        onOk={createModule}
        okText="Lưu"
        destroyOnClose
      >
        <Form form={moduleForm} layout="vertical">
          <Form.Item name="name" label="Tên module" rules={[{ required: true, message: 'Vui lòng nhập tên module' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="ownerUserId" label="Người phụ trách (ID nhân viên)">
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>
          <Form.Item name="priority" label="Ưu tiên">
            <Select options={priorityOptions} />
          </Form.Item>
          <Form.Item name="status" label="Trạng thái">
            <Select options={statusOptions.map((s) => ({ value: s.value, label: s.label }))} />
          </Form.Item>
          <Form.Item name="startDate" label="Ngày bắt đầu">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="endDate" label="Ngày kết thúc">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="description" label="Mô tả module">
            <Input.TextArea rows={3} />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        open={taskOpen}
        title="Thêm task"
        onCancel={() => setTaskOpen(false)}
        onOk={createTask}
        okText="Lưu"
        destroyOnClose
      >
        <Form form={taskForm} layout="vertical">
          <Form.Item name="moduleId" label="Module" rules={[{ required: true, message: 'Vui lòng chọn module' }]}>
            <Select options={(modules || []).map((m) => ({ value: m.id, label: `${m.id} - ${m.name}` }))} />
          </Form.Item>
          <Form.Item name="name" label="Tên công việc" rules={[{ required: true, message: 'Vui lòng nhập tên công việc' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="assigneeUserId" label="Người triển khai (ID nhân viên)">
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>
          <Form.Item name="priority" label="Ưu tiên">
            <Select options={priorityOptions} />
          </Form.Item>
          <Form.Item name="status" label="Trạng thái">
            <Select options={statusOptions.map((s) => ({ value: s.value, label: s.label }))} />
          </Form.Item>
          <Form.Item name="progress" label="Tiến độ (%)">
            <InputNumber style={{ width: '100%' }} min={0} max={100} />
          </Form.Item>
          <Form.Item name="estimatedMinutes" label="Ước tính (phút)">
            <InputNumber style={{ width: '100%' }} min={1} />
          </Form.Item>
          <Form.Item name="actualMinutes" label="Thực tế (phút)">
            <InputNumber style={{ width: '100%' }} min={0} />
          </Form.Item>
          <Form.Item name="startDate" label="Ngày bắt đầu">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="endDate" label="Ngày kết thúc">
            <DatePicker style={{ width: '100%' }} format="YYYY-MM-DD" />
          </Form.Item>
          <Form.Item name="description" label="Mô tả công việc">
            <Input.TextArea rows={3} />
          </Form.Item>
          <Form.Item name="notes" label="Ghi chú">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default ProjectDetail;
