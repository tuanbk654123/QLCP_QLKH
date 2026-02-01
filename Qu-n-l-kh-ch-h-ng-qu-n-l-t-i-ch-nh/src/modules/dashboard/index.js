import React, { useEffect, useState } from 'react';
import { Card, Row, Col, Statistic, DatePicker, Select, Space } from 'antd';
import {
  UserOutlined,
  DollarOutlined,
  ArrowUpOutlined,
  ArrowDownOutlined,
} from '@ant-design/icons';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import axios from 'axios';
import dayjs from 'dayjs';
import './index.css';

const { RangePicker } = DatePicker;
const { Option } = Select;

const COLORS = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884d8', '#82ca9d', '#FF6666', '#AAAAAA'];

const Dashboard = () => {
  const [stats, setStats] = useState({
    totalCustomers: 0,
    totalRevenue: 0,
  });
  const [transactions, setTransactions] = useState([]);
  const [dateRange, setDateRange] = useState(null);

  // New states for charts
  const [customerStats, setCustomerStats] = useState([]);
  const [projectStats, setProjectStats] = useState([]);
  const [custYear, setCustYear] = useState(dayjs().year());
  const [projMonth, setProjMonth] = useState(dayjs().month() + 1);
  const [projYear, setProjYear] = useState(dayjs().year());

  useEffect(() => {
    fetchStats();
    fetchTransactions();
  }, [dateRange]);

  useEffect(() => {
    fetchCustomerGrowth();
  }, [custYear]);

  useEffect(() => {
    fetchProjectCosts();
  }, [projMonth, projYear]);

  const fetchStats = async () => {
    try {
      setStats({
        totalCustomers: 0,
        totalRevenue: 0,
      });

      const [from, to] = dateRange || [];
      const params = {};
      if (from && to) {
        params.fromDate = from.format('YYYY-MM-DD');
        params.toDate = to.format('YYYY-MM-DD');
      }

      const response = await axios.get('/api/dashboard/overview', {
        params,
      });
      setStats({
        totalCustomers: response.data.totalCustomers,
        totalRevenue: response.data.totalRevenue,
      });
    } catch (error) {
      console.error('Lỗi khi tải thống kê:', error);
    }
  };

  const fetchTransactions = async () => {
    try {
      const [from, to] = dateRange || [];
      const params = {};
      if (from && to) {
        params.fromDate = from.format('YYYY-MM-DD');
        params.toDate = to.format('YYYY-MM-DD');
      }

      const response = await axios.get('/api/dashboard/transactions', {
        params,
      });
      setTransactions(response.data.transactions);
    } catch (error) {
      console.error('Lỗi khi tải giao dịch:', error);
    }
  };

  const fetchCustomerGrowth = async () => {
    try {
      const response = await axios.get('/api/dashboard/customer-growth', {
        params: { year: custYear }
      });
      setCustomerStats(response.data);
    } catch (error) {
      console.error('Lỗi khi tải thống kê khách hàng:', error);
    }
  };

  const fetchProjectCosts = async () => {
    try {
      const response = await axios.get('/api/dashboard/project-costs', {
        params: { month: projMonth, year: projYear }
      });
      setProjectStats(response.data);
    } catch (error) {
      console.error('Lỗi khi tải thống kê chi phí dự án:', error);
    }
  };

  const formatCurrency = (amount) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(amount);
  };

  const getChartData = () => {
    const barData = transactions.map((t) => {
      const label = dayjs(t.date).isValid()
        ? dayjs(t.date).format('DD/MM')
        : t.date;
      const revenue = t.type === 'revenue' && t.status === 'completed' ? t.amount : 0;
      const expense = t.type === 'expense' && t.status === 'completed' ? t.amount : 0;
      return {
        name: label,
        date: t.date,
        Thu: revenue,
        Chi: expense,
      };
    });

    return { barData };
  };

  const { barData } = getChartData();

  const totalRevenue = barData.reduce((sum, item) => sum + item.Thu, 0);
  const totalExpense = barData.reduce((sum, item) => sum + item.Chi, 0);
  const pieData = [
    { name: 'Thu', value: totalRevenue },
    { name: 'Chi', value: totalExpense },
  ];
  const pieColors = ['#49BD65', '#F44D50'];

  const handlePresetChange = (value) => {
    const today = dayjs();
    if (value === 'thisMonth') {
      setDateRange([today.startOf('month'), today.endOf('month')]);
    } else if (value === 'lastMonth') {
      const lastMonth = today.subtract(1, 'month');
      setDateRange([lastMonth.startOf('month'), lastMonth.endOf('month')]);
    } else if (value === 'thisYear') {
      setDateRange([today.startOf('year'), today.endOf('year')]);
    } else if (value === 'all') {
      setDateRange(null);
    }
  };

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <h1>Dashboard - Tổng quan hệ thống</h1>
        <div className="dashboard-controls">
          <Select
            defaultValue="all"
            style={{ width: 170, marginRight: 12 }}
            onChange={handlePresetChange}
          >
            <Option value="thisMonth">Tháng này</Option>
            <Option value="lastMonth">Tháng trước</Option>
            <Option value="thisYear">Năm nay</Option>
            <Option value="all">Tất cả thời gian</Option>
          </Select>
          <RangePicker
            value={dateRange}
            onChange={(values) => {
              if (!values || !values[0] || !values[1]) {
                setDateRange(null);
              } else {
                setDateRange(values);
              }
            }}
            format="DD/MM/YYYY"
          />
        </div>
      </div>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Tổng khách hàng"
              value={stats.totalCustomers}
              prefix={<UserOutlined />}
              valueStyle={{ color: '#1890ff' }}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Tổng doanh thu"
              value={stats.totalRevenue}
              prefix={<DollarOutlined />}
              valueStyle={{ color: '#3f8600' }}
              formatter={(value) => formatCurrency(value)}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Thu (theo kỳ chọn)"
              value={totalRevenue}
              prefix={<ArrowUpOutlined />}
              valueStyle={{ color: '#49BD65' }}
              formatter={(value) => formatCurrency(value)}
            />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card>
            <Statistic
              title="Chi (theo kỳ chọn)"
              value={totalExpense}
              prefix={<ArrowDownOutlined />}
              valueStyle={{ color: '#F44D50' }}
              formatter={(value) => formatCurrency(value)}
            />
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24} lg={16}>
          <Card 
            title="Tổng kết thu chi" 
            extra={
              <div>
                <span style={{ color: '#49BD65', marginRight: 16 }}>
                  <ArrowUpOutlined /> Thu: {formatCurrency(totalRevenue)}
                </span>
                <span style={{ color: '#F44D50' }}>
                  <ArrowDownOutlined /> Chi: {formatCurrency(totalExpense)}
                </span>
              </div>
            }
            className="chart-card"
          >
            {barData.length > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart
                  data={barData}
                  margin={{
                    top: 15,
                    right: 0,
                    left: 0,
                    bottom: 0,
                  }}
                >
                  <CartesianGrid
                    strokeDasharray="3 1"
                    horizontal={true}
                    vertical={false}
                  />
                  <XAxis dataKey="name" dy={10} />
                  <YAxis />
                  <Tooltip 
                    labelStyle={{ color: 'black' }}
                    formatter={(value) => formatCurrency(value)}
                  />
                  <Legend />
                  <Bar dataKey="Thu" fill="#49BD65" barSize={8} />
                  <Bar dataKey="Chi" fill="#F44D50" barSize={8} />
                </BarChart>
              </ResponsiveContainer>
            ) : (
              <div style={{ textAlign: 'center', padding: '50px', color: '#999' }}>
                Không có dữ liệu cho khoảng thời gian này
              </div>
            )}
          </Card>
        </Col>
        <Col xs={24} lg={8}>
          <Card title="Phân bổ Thu / Chi" className="chart-card">
            {(totalRevenue + totalExpense) > 0 ? (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Tooltip formatter={(value) => formatCurrency(value)} />
                  <Legend />
                  <Pie
                    data={pieData}
                    dataKey="value"
                    nameKey="name"
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={90}
                    label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                  >
                    {pieData.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={pieColors[index % pieColors.length]} />
                    ))}
                  </Pie>
                </PieChart>
              </ResponsiveContainer>
            ) : (
              <div style={{ textAlign: 'center', padding: '50px', color: '#999' }}>
                Không có dữ liệu để hiển thị
              </div>
            )}
          </Card>
        </Col>
      </Row>

      {/* New Charts Section */}
      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        {/* Customer Growth Chart */}
        <Col xs={24} lg={12}>
          <Card 
            title={
              <Space>
                <span>Tăng trưởng khách hàng</span>
                <Select value={custYear} onChange={setCustYear} style={{ width: 100 }}>
                  {[...Array(5)].map((_, i) => {
                    const year = dayjs().year() - 2 + i;
                    return <Option key={year} value={year}>{year}</Option>;
                  })}
                </Select>
              </Space>
            }
            className="chart-card"
          >
            {customerStats && customerStats.length > 0 ? (
             <ResponsiveContainer width="100%" height={300}>
                <BarChart data={customerStats} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 1" />
                  <XAxis dataKey="name" />
                  <YAxis />
                  <Tooltip labelStyle={{ color: 'black' }} />
                  <Legend />
                  <Bar dataKey="total" name="Tổng KH mới" fill="#1890ff" />
                  <Bar dataKey="consulted" name="Đã tư vấn" fill="#52c41a" />
                </BarChart>
             </ResponsiveContainer>
            ) : (
              <div style={{ textAlign: 'center', padding: '50px', color: '#999' }}>
                Không có dữ liệu cho năm {custYear}
              </div>
            )}
          </Card>
        </Col>

        {/* Project Cost Chart */}
        <Col xs={24} lg={12}>
          <Card 
            title={
              <Space>
                <span>Chi phí theo dự án</span>
                <Select value={projMonth} onChange={setProjMonth} style={{ width: 100 }}>
                  <Option value={0}>Cả năm</Option>
                  {[...Array(12)].map((_, i) => (
                    <Option key={i + 1} value={i + 1}>Tháng {i + 1}</Option>
                  ))}
                </Select>
                <Select value={projYear} onChange={setProjYear} style={{ width: 100 }}>
                   {[...Array(5)].map((_, i) => {
                    const year = dayjs().year() - 2 + i;
                    return <Option key={year} value={year}>{year}</Option>;
                  })}
                </Select>
              </Space>
            }
            className="chart-card"
          >
            {projectStats.length > 0 ? (
              <ResponsiveContainer width="100%" height={300}>
                <PieChart>
                  <Pie
                    data={projectStats}
                    cx="50%"
                    cy="50%"
                    labelLine={false}
                    label={({ name, percent }) => `${name}: ${(percent * 100).toFixed(0)}%`}
                    outerRadius={100}
                    fill="#8884d8"
                    dataKey="value"
                  >
                    {projectStats.map((entry, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip formatter={(value) => formatCurrency(value)} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            ) : (
               <div style={{ textAlign: 'center', padding: '50px', color: '#999' }}>
                Không có dữ liệu chi phí dự án
              </div>
            )}
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]} style={{ marginTop: 16 }}>
        <Col xs={24}>
          <Card title="Biểu đồ thu chi theo thời gian" className="chart-card">
            {barData.length > 0 ? (
              <ResponsiveContainer width="100%" height={350}>
                <LineChart
                  data={barData}
                  margin={{
                    top: 15,
                    right: 30,
                    left: 0,
                    bottom: 0,
                  }}
                >
                  <CartesianGrid
                    strokeDasharray="3 1"
                    horizontal={true}
                    vertical={false}
                  />
                  <XAxis dataKey="name" dy={10} />
                  <YAxis />
                  <Tooltip 
                    labelStyle={{ color: 'black' }}
                    formatter={(value) => formatCurrency(value)}
                  />
                  <Legend />
                  <Line 
                    type="monotone" 
                    dataKey="Thu" 
                    stroke="#49BD65" 
                    strokeWidth={3}
                    dot={{ fill: '#49BD65', r: 4 }}
                    activeDot={{ r: 6 }}
                  />
                  <Line 
                    type="monotone" 
                    dataKey="Chi" 
                    stroke="#F44D50" 
                    strokeWidth={3}
                    dot={{ fill: '#F44D50', r: 4 }}
                    activeDot={{ r: 6 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            ) : (
              <div style={{ textAlign: 'center', padding: '50px', color: '#999' }}>
                Không có dữ liệu cho khoảng thời gian này
              </div>
            )}
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Dashboard;
