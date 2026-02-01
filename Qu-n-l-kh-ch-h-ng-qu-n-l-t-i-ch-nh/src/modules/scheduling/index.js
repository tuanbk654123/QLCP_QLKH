import React, { useState, useCallback, useEffect } from 'react';
import { Card, Button, Form, InputNumber, Table, Row, Col, Alert, Statistic, Space, Typography, message, DatePicker, Input } from 'antd';
import { CalendarOutlined, DownloadOutlined, DatabaseOutlined, PlusOutlined, MinusCircleOutlined } from '@ant-design/icons';
import * as XLSX from 'xlsx-js-style';
import { saveAs } from 'file-saver';
import moment from 'moment';
import axios from 'axios';
import { useAuth } from '../../context/AuthContext';

const { Title, Text } = Typography;
const { RangePicker } = DatePicker;
const { TextArea } = Input;

// Helper to generate realistic names
const lastNames = ['Nguyễn', 'Trần', 'Lê', 'Phạm', 'Hoàng', 'Huỳnh', 'Phan', 'Vũ', 'Võ', 'Đặng', 'Bùi', 'Đỗ', 'Hồ', 'Ngô', 'Dương', 'Lý'];
const middleNames = ['Văn', 'Thị', 'Hữu', 'Minh', 'Ngọc', 'Đức', 'Thanh', 'Quang', 'Xuân', 'Thu', 'Mạnh', 'Tuấn', 'Hải', 'Thùy'];
const firstNames = ['Anh', 'Bình', 'Châu', 'Dũng', 'Em', 'Giang', 'Hà', 'Hải', 'Hiếu', 'Hòa', 'Hùng', 'Huy', 'Khánh', 'Lan', 'Linh', 'Long', 'Mai', 'Minh', 'Nam', 'Nga', 'Nhi', 'Nhung', 'Oanh', 'Phát', 'Phúc', 'Phượng', 'Quân', 'Quang', 'Quốc', 'Sơn', 'Tài', 'Tâm', 'Tân', 'Thảo', 'Thắng', 'Thanh', 'Thảo', 'Thịnh', 'Thu', 'Thủy', 'Tiến', 'Toàn', 'Trang', 'Trí', 'Trọng', 'Trung', 'Tú', 'Tuấn', 'Tùng', 'Uyên', 'Vân', 'Việt', 'Vinh', 'Vy', 'Yến'];

const generateRandomName = () => {
  const last = lastNames[Math.floor(Math.random() * lastNames.length)];
  const middle = middleNames[Math.floor(Math.random() * middleNames.length)];
  const first = firstNames[Math.floor(Math.random() * firstNames.length)];
  return `${last} ${middle} ${first}`;
};

const companyPrefixes = ['Công ty TNHH', 'Công ty CP', 'Tập đoàn', 'Doanh nghiệp tư nhân', 'Hợp tác xã'];
const companyNames = ['Thành Công', 'Đại Phát', 'Hoà Bình', 'Việt Hưng', 'Thăng Long', 'Sông Đà', 'Trường Hải', 'FPT', 'Viettel', 'VinGroup', 'Masan', 'SunGroup', 'NovaLand', 'Bình Minh', 'Sao Mai', 'Hồng Hà', 'Cửu Long', 'Phương Đông', 'An Phát', 'Hải Hà', 'Thiên Long', 'Đất Việt', 'Hoàng Anh', 'Quốc tế', 'Toàn Cầu', 'Hưng Thịnh', 'Vạn Thịnh Phát', 'Bảo Việt', 'Dầu khí', 'Điện lực'];

const generateCompanyName = (index) => {
  const prefix = companyPrefixes[Math.floor(Math.random() * companyPrefixes.length)];
  const name = companyNames[index % companyNames.length] || `Công ty ${index + 1}`;
  return `${prefix} ${name} ${index + 1}`; // Add index to ensure uniqueness if needed
};

const parseList = (str) => (str || '').split('\n').map(s => s.trim()).filter(s => s);

const getDayName = (dateMoment) => {
  const days = ['Chủ Nhật', 'Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7'];
  return days[dateMoment.day()];
};

const SchedulingPage = () => {
  const [form] = Form.useForm();
  const [scheduleData, setScheduleData] = useState([]);
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(false);
  const [dynamicColumns, setDynamicColumns] = useState([]);
  const { isAdmin } = useAuth();
  const [fieldPermissions, setFieldPermissions] = useState({});

  const fetchPermissions = useCallback(async () => {
    try {
      const response = await axios.get('/api/permissions/current', {
        params: { module: 'scheduling' },
      });
      setFieldPermissions(response.data.permissions || {});
    } catch (error) {
      console.error('Error fetching permissions:', error);
      setFieldPermissions({});
    }
  }, []);

  useEffect(() => {
    fetchPermissions();
  }, [fetchPermissions]);

  const canReadField = (field) => {
    if (isAdmin && isAdmin()) return true;
    const level = fieldPermissions[field];
    return level && level !== 'N';
  };

  const canEditField = (field) => {
    if (isAdmin && isAdmin()) return true;
    const level = fieldPermissions[field];
    return level === 'W' || level === 'A';
  };

  // Default Values (Seed Data)
  const seedData = {
    daysPerCompany: 28,
    dateRange: [moment(), moment().add(89, 'days')], // 90 days total
  };

  const handleSeedData = () => {
    // Generate realistic data strings
    const companies = Array.from({ length: 35 }, (_, i) => generateCompanyName(i)).join('\n');
    const shtt = Array.from({ length: 9 }, () => generateRandomName()).join('\n');
    const pttt = Array.from({ length: 9 }, () => generateRandomName()).join('\n');

    // Only set dateRange if not already set
    const currentValues = form.getFieldsValue();
    const newValues = {
      ...seedData,
      companyList: companies,
      shttStaff: shtt,
      ptttStaff: pttt,
      otherGroups: []
    };

    if (currentValues.dateRange) {
        delete newValues.dateRange;
    }

    form.setFieldsValue(newValues);
    message.success('Đã nạp dữ liệu mẫu!');
  };

  const generateSchedule = (values) => {
    setLoading(true);
    try {
      const { daysPerCompany, dateRange, companyList, shttStaff, ptttStaff, otherGroups } = values;
      
      if (!dateRange || dateRange.length !== 2) {
          message.error("Vui lòng chọn khoảng thời gian dự án!");
          setLoading(false);
          return;
      }

      // Ensure we work with Moment objects (Antd 5 uses Dayjs by default)
      const startDate = moment(dateRange[0].toDate ? dateRange[0].toDate() : dateRange[0]);
      const endDate = moment(dateRange[1].toDate ? dateRange[1].toDate() : dateRange[1]);

      const projectDays = endDate.diff(startDate, 'days') + 1; // Inclusive
      const dateRangeStr = `${startDate.format('DD/MM/YYYY')} - ${endDate.format('DD/MM/YYYY')} (${projectDays} ngày)`;

      // 1. Initialize Resources
      const companyNamesArr = parseList(companyList);

      if (companyNamesArr.length === 0) {
        message.error("Vui lòng nhập danh sách công ty!");
        setLoading(false);
        return;
      }

      // Combine fixed group with dynamic groups
      const staffGroups = [
        { roleName: 'Sở hữu trí tuệ', staffList: parseList(shttStaff) },
        { roleName: 'Phát triển thị trường', staffList: parseList(ptttStaff) },
        ...(otherGroups || []).map(g => ({
            roleName: g.roleName || 'Unnamed Role',
            staffList: parseList(g.staffList)
        }))
      ];

      // Validate staff groups
      for (const group of staffGroups) {
        if (group.staffList.length === 0) {
          message.error(`Nhóm "${group.roleName}" chưa có nhân viên nào!`);
          setLoading(false);
          return;
        }
      }

      const numCompanies = companyNamesArr.length;
      const companies = companyNamesArr.map((name, i) => ({
        id: i + 1,
        name: name,
        visitsNeeded: daysPerCompany,
        visitsDone: 0,
      }));

      // Max teams per day = min(staff count of all groups)
      // Because each visit requires 1 person from EACH group
      const maxTeamsPerDay = Math.min(...staffGroups.map(g => g.staffList.length));
      
      const schedule = [];
      let totalVisits = 0;

      // TRACKING: Staff usage per month to enforce 22-day limit
      // structure: { 'MM-YYYY': { 'Staff Name': count } }
      const staffUsage = {};

      const getStaffUsage = (month, name) => {
          if (!staffUsage[month]) return 0;
          return staffUsage[month][name] || 0;
      };

      const incrementStaffUsage = (month, name) => {
          if (!staffUsage[month]) staffUsage[month] = {};
          staffUsage[month][name] = (staffUsage[month][name] || 0) + 1;
      };

      // 2. Algorithm: Greedy with "Most Needs First" Strategy
      let workingDayCount = 0;
      for (let day = 0; day < projectDays; day++) {
        const dateObj = moment(startDate).add(day, 'days');
        const currentDate = dateObj.format('DD/MM/YYYY');
        const currentMonthKey = dateObj.format('MM-YYYY');
        const dayOfWeek = dateObj.day(); // 0: Sunday, 1: Monday...
        const dayName = getDayName(dateObj);

        // Check if Sunday -> Rest Day
        if (dayOfWeek === 0) {
            schedule.push({
                key: `${day}-sunday`,
                date: currentDate,
                dayIndex: day + 1,
                dayOfWeek: dayName,
                company: 'NGHỈ CHỦ NHẬT',
                isRestDay: true
            });
            continue; // Skip assignment for this day
        }
        
        workingDayCount++; // Increment Working Day Counter

        // Shuffle staff for randomness for EACH group, BUT filter by 22-day rule
        const dailyStaffPools = staffGroups.map(group => {
          const availableStaff = group.staffList.filter(name => getStaffUsage(currentMonthKey, name) < 22);
          return {
            roleName: group.roleName,
            pool: [...availableStaff].sort(() => 0.5 - Math.random())
          };
        });

        // Prioritize companies that need visits the most
        const companiesNeedingVisits = companies
          .filter(c => c.visitsDone < c.visitsNeeded)
          .sort((a, b) => (b.visitsNeeded - b.visitsDone) - (a.visitsNeeded - a.visitsDone)); // Descending needs

        // Assign for today
        // Constraint: Can't assign more than available staff in any group
        const availableTeamsToday = Math.min(...dailyStaffPools.map(g => g.pool.length));
        const assignmentsToday = Math.min(availableTeamsToday, companiesNeedingVisits.length);

        for (let i = 0; i < assignmentsToday; i++) {
          const company = companiesNeedingVisits[i];
          
          // Create a schedule entry
          const entry = {
            key: `${day}-${company.id}`,
            date: currentDate,
            dayIndex: day + 1,
            workingDayIndex: workingDayCount,
            dayOfWeek: dayName,
            company: company.name,
          };

          // Assign one person from each group
          dailyStaffPools.forEach((groupPool, groupIndex) => {
            // Because i < availableTeamsToday and availableTeamsToday <= groupPool.pool.length, 
            // groupPool.pool[i] is guaranteed to exist
            const staffName = groupPool.pool[i];
            entry[`role_${groupIndex}`] = staffName;
            incrementStaffUsage(currentMonthKey, staffName);
          });

          schedule.push(entry);

          company.visitsDone++;
          totalVisits++;
        }
      }

      // 3. Calculate Statistics
      const totalDemand = numCompanies * daysPerCompany;
      const shortage = totalDemand - totalVisits;
      
      // Recommendation Logic
      let recommendation = null;
      if (shortage > 0) {
        // Correct calculation: Divide by working days, not total project days
        const neededTeams = Math.ceil(totalDemand / workingDayCount);
        const missingTeams = neededTeams - maxTeamsPerDay;
        
        if (missingTeams > 0) {
           const bottlenecks = staffGroups
            .filter(g => g.staffList.length < neededTeams)
            .map(g => `• Nhóm "${g.roleName}": cần thêm ${neededTeams - g.staffList.length} người (Tổng cần: ${neededTeams})`)
            .join('\n');
           
           recommendation = `Để đáp ứng đủ ${totalDemand} ngày làm việc trong ${workingDayCount} ngày làm việc (trừ Chủ Nhật), mỗi nhóm cần tối thiểu ${neededTeams} nhân sự.\nChi tiết:\n${bottlenecks}`;
        }
      }

      setStats({
        totalDemand,
        totalVisits,
        shortage,
        maxCapacity: maxTeamsPerDay * workingDayCount,
        numCompanies,
        recommendation,
        dateRangeStr,
        groups: staffGroups.map(g => ({
          name: g.roleName,
          count: g.staffList.length
        })),
        companies: companies.map(c => ({
          name: c.name,
          needed: c.visitsNeeded,
          done: c.visitsDone,
          missing: c.visitsNeeded - c.visitsDone
        }))
      });

      // Set Dynamic Columns for Table
      const newColumns = [
        { title: 'Ngày', dataIndex: 'date', key: 'date', width: 120 },
        { title: 'Thứ', dataIndex: 'dayOfWeek', key: 'dayOfWeek', width: 100 },
        { title: 'Công ty', dataIndex: 'company', key: 'company', width: 250 },
        ...staffGroups.map((g, index) => ({
          title: g.roleName,
          dataIndex: `role_${index}`,
          key: `role_${index}`,
          width: 150
        }))
      ];
      setDynamicColumns(newColumns);
      setScheduleData(schedule);
      
      if (shortage > 0) {
        message.warning(`Lịch đã xếp xong nhưng còn thiếu ${shortage} ngày làm việc!`);
      } else {
        message.success('Xếp lịch thành công, đáp ứng đủ yêu cầu!');
      }

    } catch (error) {
      console.error(error);
      message.error('Có lỗi xảy ra khi xếp lịch');
    } finally {
      setLoading(false);
    }
  };

  const exportExcel = () => {
    if (scheduleData.length === 0) {
      message.warning('Chưa có dữ liệu lịch để xuất!');
      return;
    }

    const values = form.getFieldsValue();
    const { shttStaff, ptttStaff, otherGroups } = values;

    // Reconstruct staffGroups for export logic
    const staffGroups = [
        { roleName: 'Sở hữu trí tuệ', staffList: parseList(shttStaff) },
        { roleName: 'Phát triển thị trường', staffList: parseList(ptttStaff) },
        ...(otherGroups || []).map(g => ({
            roleName: g.roleName || 'Unnamed Role',
            staffList: parseList(g.staffList)
        }))
    ];

    // 1. Prepare Data Structure
    const scheduleMap = {};
    const allDates = new Set();
    const allCompanies = new Map(); // Name -> Name (using Name as ID since it's unique enough for display)

    // Helper to get role info string
    const roleColumns = dynamicColumns.slice(3); // Skip Date, Day, Company

    scheduleData.forEach(item => {
      allDates.add(item.date); 
      // Only add company if it's not a rest day
      if (!item.isRestDay) {
        allCompanies.set(item.company, item.company);
      }
      
      if (!scheduleMap[item.company]) {
        scheduleMap[item.company] = {};
      }
      
      // Combine roles into one string with newlines
      const roleInfo = roleColumns.map(col => {
        const staffName = item[col.dataIndex];
        return staffName ? `${col.title}: ${staffName}` : null;
      }).filter(Boolean).join('\n');
      
      scheduleMap[item.company][item.date] = item.isRestDay ? "NGHỈ" : roleInfo;
    });

    // Sort Dates properly
    const sortedDates = Array.from(allDates).sort((a, b) => 
      moment(a, 'DD/MM/YYYY').toDate() - moment(b, 'DD/MM/YYYY').toDate()
    );
    const companiesArr = Array.from(allCompanies.values());

    // 2. Build Sheet Data (Weekly Blocks)
    const ws_data = [
      ["BẢNG PHÂN CÔNG CÔNG VIỆC"], // Main Title
      [""] // Spacer
    ];

    const chunkSize = 7;
    for (let i = 0; i < sortedDates.length; i += chunkSize) {
      const weekDates = sortedDates.slice(i, i + chunkSize);
      
      // Header Row for this week block
      // Add 'STT' and 'Công ty' columns, then the dates with Day Name
      const headerRow = ["STT", "Công ty", ...weekDates.map(date => {
        const d = moment(date, 'DD/MM/YYYY');
        const dayName = getDayName(d);
        return `${date} (${dayName})`;
      })];
      ws_data.push(headerRow);

      // Data Rows for this week
      companiesArr.forEach((compName, idx) => {
        const row = [
          idx + 1,
          compName,
          ...weekDates.map(date => {
             // Check if Sunday
             if (moment(date, 'DD/MM/YYYY').day() === 0) return "NGHỈ";
             // Return the staff info or empty string
            return scheduleMap[compName]?.[date] || "";
          })
        ];
        ws_data.push(row);
      });

      // Spacer between weeks
      ws_data.push([""]); 
    }

    // 3. Create Workbook & Sheet
    const wb = XLSX.utils.book_new();
    const ws = XLSX.utils.aoa_to_sheet(ws_data);

    // 4. Formatting (Styles & Widths)
    
    // Column Widths
    const wscols = [
      { wch: 5 }, // STT
      { wch: 35 }, // Company Name
      ...Array(chunkSize).fill({ wch: 30 }) // Dates
    ];
    ws['!cols'] = wscols;

    // Define Styles
    const borderStyle = {
      top: { style: "thin" },
      bottom: { style: "thin" },
      left: { style: "thin" },
      right: { style: "thin" }
    };

    const titleStyle = {
      font: { name: "Arial", sz: 18, bold: true },
      alignment: { horizontal: "center", vertical: "center" }
    };

    const headerStyle = {
      font: { name: "Arial", bold: true },
      border: borderStyle,
      alignment: { horizontal: "center", vertical: "center", wrapText: true },
      fill: { fgColor: { rgb: "E0E0E0" } } // Light gray background
    };

    const dataStyle = {
      font: { name: "Arial" },
      border: borderStyle,
      alignment: { vertical: "top", wrapText: true }
    };

    // Apply Styles to Cells
    const range = XLSX.utils.decode_range(ws['!ref']);
    
    // Merge Title Row
    if (!ws['!merges']) ws['!merges'] = [];
    ws['!merges'].push({ s: { r: 0, c: 0 }, e: { r: 0, c: chunkSize + 1 } });
    
    // Apply styles loop
    for (let R = range.s.r; R <= range.e.r; ++R) {
      for (let C = range.s.c; C <= range.e.c; ++C) {
        const cell_address = XLSX.utils.encode_cell({ r: R, c: C });
        if (!ws[cell_address]) continue;

        // Apply Title Style
        if (R === 0) {
          ws[cell_address].s = titleStyle;
          continue;
        }

        // Check content to determine style (Heuristic)
        const cellVal = ws[cell_address].v;
        
        // Header Row Detection: If Column A is "STT"
        const colAVal = ws[XLSX.utils.encode_cell({ r: R, c: 0 })]?.v;
        
        if (colAVal === "STT") {
           ws[cell_address].s = headerStyle;
        } else if (typeof colAVal === 'number') {
           // Data Row
           ws[cell_address].s = dataStyle;
        }
      }
    }

    XLSX.utils.book_append_sheet(wb, ws, "Lịch Phân Công");

    // 5. Create Individual Sheets for Each Staff
    const allStaff = {}; // name -> { role, assignments: [ {date, company} ] }

    scheduleData.forEach(item => {
        // dynamicColumns has [date, company, role_0, role_1...]
        // role_0 corresponds to staffGroups[0]
        staffGroups.forEach((group, idx) => {
             const staffName = item[`role_${idx}`];
             if (staffName) {
                 if (!allStaff[staffName]) {
                     allStaff[staffName] = { role: group.roleName, assignments: [] };
                 }
                 allStaff[staffName].assignments.push({
                     date: item.date,
                     company: item.company,
                     dayIndex: item.dayIndex,
                     workingDayIndex: item.workingDayIndex
                 });
             }
        });
    });

    Object.keys(allStaff).sort().forEach(staffName => {
        const staffData = allStaff[staffName];
        
        // Sort assignments by date
        staffData.assignments.sort((a, b) => 
            moment(a.date, 'DD/MM/YYYY').toDate() - moment(b.date, 'DD/MM/YYYY').toDate()
        );

        const staffWsData = [
            [`LỊCH CÔNG TÁC CÁ NHÂN`],
            [`Họ tên: ${staffName}`],
            [`Chức vụ: ${staffData.role}`],
            [""],
            ["STT", "Ngày", "Thứ", "Thứ tự ngày", "Công ty"]
        ];

        staffData.assignments.forEach((assign, idx) => {
            const d = moment(assign.date, 'DD/MM/YYYY');
            staffWsData.push([
                idx + 1,
                assign.date,
                getDayName(d),
                `Ngày làm việc thứ ${assign.workingDayIndex}`,
                assign.company
            ]);
        });

        const staffWs = XLSX.utils.aoa_to_sheet(staffWsData);
        
        // Formatting for Staff Sheet
        staffWs['!cols'] = [{ wch: 5 }, { wch: 15 }, { wch: 15 }, { wch: 15 }, { wch: 40 }];
        
        // Merge Title
        if (!staffWs['!merges']) staffWs['!merges'] = [];
        staffWs['!merges'].push({ s: { r: 0, c: 0 }, e: { r: 0, c: 4 } }); // Title merge

        // Apply Styles
        const staffRange = XLSX.utils.decode_range(staffWs['!ref']);
        for (let R = staffRange.s.r; R <= staffRange.e.r; ++R) {
            for (let C = staffRange.s.c; C <= staffRange.e.c; ++C) {
                const cell_address = XLSX.utils.encode_cell({ r: R, c: C });
                if (!staffWs[cell_address]) continue;

                if (R === 0) staffWs[cell_address].s = titleStyle;
                else if (R === 4) staffWs[cell_address].s = headerStyle; // Header row
                else if (R > 4) staffWs[cell_address].s = dataStyle; // Data rows
            }
        }
        
        // Sheet name max length 31 chars
        const safeSheetName = staffName.substring(0, 30).replace(/[\\/?*[\]]/g, "");
        XLSX.utils.book_append_sheet(wb, staffWs, safeSheetName);
    });

    const excelBuffer = XLSX.write(wb, { bookType: 'xlsx', type: 'array' });
    const data = new Blob([excelBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet;charset=UTF-8' });
    saveAs(data, `Lich_Phan_Cong_Chi_Tiet_${moment().format('DDMMYYYY_HHmm')}.xlsx`);
  };

  return (
    <div style={{ padding: 24 }}>
      <Title level={2}><CalendarOutlined /> Xếp Lịch Dự Án (Đa Nhân Sự)</Title>
      
      <Row gutter={[16, 16]}>
        <Col span={8}>
          {canReadField('config') && (
          <Card title="Tham số đầu vào" bordered={false} bodyStyle={{ padding: '12px 24px' }}>
            <Form form={form} layout="vertical" onFinish={generateSchedule} initialValues={{ otherGroups: [] }}>
              <Form.Item 
                name="companyList" 
                label="Danh sách Công ty (Mỗi dòng 1 công ty)" 
                rules={[{ required: true, message: 'Vui lòng nhập danh sách công ty' }]}
              >
                <TextArea rows={5} placeholder="Nhập tên các công ty..." />
              </Form.Item>

              <Text strong style={{ display: 'block', marginBottom: 8 }}>Nhóm nhân sự cố định:</Text>
              <Form.Item
                name="shttStaff"
                label="Sở hữu trí tuệ (Mỗi dòng 1 nhân sự)"
                rules={[{ required: true, message: 'Nhập danh sách nhân viên SHTT' }]}
              >
                <TextArea placeholder="Danh sách nhân viên SHTT..." rows={6} />
              </Form.Item>

              <Form.Item
                name="ptttStaff"
                label="Phát triển thị trường (Mỗi dòng 1 nhân sự)"
                rules={[{ required: true, message: 'Nhập danh sách nhân viên PTTT' }]}
              >
                <TextArea placeholder="Danh sách nhân viên PTTT..." rows={6} />
              </Form.Item>

              <Text strong style={{ display: 'block', marginBottom: 8 }}>Các nhóm nhân sự khác:</Text>
              <Form.List name="otherGroups">
                {(fields, { add, remove }) => (
                  <>
                    {fields.map(({ key, name, ...restField }) => (
                      <Space key={key} style={{ display: 'flex', marginBottom: 8 }} align="baseline">
                        <Form.Item
                          {...restField}
                          name={[name, 'roleName']}
                          rules={[{ required: true, message: 'Nhập tên nhóm' }]}
                          style={{ width: 120 }}
                        >
                          <Input placeholder="Tên (VD: PTTT)" />
                        </Form.Item>
                        <Form.Item
                          {...restField}
                          name={[name, 'staffList']}
                          rules={[{ required: true, message: 'Nhập danh sách NV' }]}
                          style={{ width: 200 }}
                        >
                          <TextArea placeholder="Danh sách nhân viên (Mỗi dòng 1 người)..." rows={4} />
                        </Form.Item>
                        <MinusCircleOutlined onClick={() => remove(name)} />
                      </Space>
                    ))}
                    <Form.Item>
                      <Button type="dashed" onClick={() => add()} block icon={<PlusOutlined />}>
                        Thêm nhóm nhân sự
                      </Button>
                    </Form.Item>
                  </>
                )}
              </Form.List>

              <Form.Item name="daysPerCompany" label="Số ngày công / cty" rules={[{ required: true }]}>
                <InputNumber min={1} style={{ width: '100%' }} />
              </Form.Item>
              <Form.Item name="dateRange" label="Thời gian dự án" rules={[{ required: true }]}>
                 <RangePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
              
              <Space style={{ width: '100%', justifyContent: 'space-between' }}>
                <Button icon={<DatabaseOutlined />} onClick={handleSeedData} disabled={!canEditField('seedData')}>Dữ liệu mẫu</Button>
                <Button type="primary" htmlType="submit" loading={loading} disabled={!canEditField('generate')}>Xếp Lịch</Button>
              </Space>
            </Form>
          </Card>
          )}

          {stats && canReadField('view') && (
            <Card title="Thống kê kết quả" style={{ marginTop: 16 }} bordered={false}>
               <div style={{ marginBottom: 16, padding: '8px 12px', background: '#f0f2f5', borderRadius: 4 }}>
                  <Text strong>Phạm vi lịch:</Text> <Text>{stats.dateRangeStr}</Text>
               </div>
               <Row gutter={16}>
                 <Col span={24}>
                    <Statistic title="Số công ty" value={stats.numCompanies} />
                 </Col>
               </Row>
               <Row gutter={16} style={{ marginTop: 16 }}>
                 {stats.groups.map((g, idx) => (
                    <Col span={12} key={idx} style={{ marginBottom: 8 }}>
                        <Statistic title={`Nhân sự ${g.name}`} value={g.count} valueStyle={{ fontSize: 16 }} />
                    </Col>
                 ))}
               </Row>
               
               <div style={{ marginTop: 16 }}>
                 <Statistic title="Tổng nhu cầu (ngày làm việc)" value={stats.totalDemand} />
                 <Statistic title="Đáp ứng được (ngày làm việc)" value={stats.totalVisits} valueStyle={{ color: stats.shortage > 0 ? '#cf1322' : '#3f8600' }} />
               </div>
               
               {stats.shortage > 0 && (
                 <Alert 
                   message={`Thiếu ${stats.shortage} ngày làm việc!`} 
                   description={
                     <div style={{ whiteSpace: 'pre-line' }}>
                       {stats.recommendation || "Số lượng nhân sự thấp nhất trong các nhóm đang giới hạn năng suất toàn dự án."}
                     </div>
                   }
                   type="warning" 
                   showIcon 
                   style={{ marginTop: 10 }}
                 />
               )}
            </Card>
          )}
        </Col>

        <Col span={16}>
          {canReadField('view') && (
          <Card 
            title="Kết quả lịch trình" 
            extra={<Button icon={<DownloadOutlined />} onClick={exportExcel} disabled={scheduleData.length === 0 || !canEditField('export')}>Xuất Excel</Button>}
            bordered={false}
          >
            <Table 
              dataSource={scheduleData} 
              columns={dynamicColumns} 
              pagination={{ pageSize: 10 }} 
              size="small"
              scroll={{ y: 500 }}
            />
          </Card>
          )}
        </Col>
      </Row>
    </div>
  );
};

export default SchedulingPage;
