import MockAdapter from 'axios-mock-adapter';
import axios from 'axios';
import { customersData } from './fakedb/customers';
import { businessesData } from './fakedb/businesses';
import { tasksData } from './fakedb/tasks';
import { transactionsData } from './fakedb/transactions';
import { usersData, setCurrentUser, getCurrentUser } from './fakedb/users';
import { costsData } from './fakedb/costs';
import { notificationsData } from './fakedb/notifications';
import { roles, qlkhFields, qlcpFields, schedulingFields, initialPermissions } from './fakedb/permissions';

const mock = new MockAdapter(axios, { delayResponse: 500 });

let currentPermissions = initialPermissions;

// API Authentication
mock.onPost('/api/auth/login').reply((config) => {
  const { username, password } = JSON.parse(config.data);
  const user = usersData.find(
    (u) => (u.username === username || u.email === username) && u.password === password
  );
  
  if (user) {
    const { password: _, ...userWithoutPassword } = user;
    setCurrentUser(userWithoutPassword);
    return [200, { user: userWithoutPassword, token: 'mock-token-' + user.id }];
  }
  return [401, { message: 'Tên đăng nhập hoặc mật khẩu không đúng' }];
});

mock.onPost('/api/auth/logout').reply(() => {
  setCurrentUser(null);
  return [200, { message: 'Đăng xuất thành công' }];
});

mock.onGet('/api/auth/me').reply(() => {
  const user = getCurrentUser();
  if (user) {
    return [200, { user }];
  }
  return [401, { message: 'Chưa đăng nhập' }];
});

// API Quản lý nhân viên (chỉ admin)
mock.onGet('/api/users').reply((config) => {
  const currentUser = getCurrentUser();
  if (!currentUser) {
    return [401, { message: 'Chưa đăng nhập' }];
  }

  const { search, page = 1, role } = config.params || {};
  let users = [...usersData];

  if (search) {
    users = users.filter(
      (user) =>
        user.fullName.toLowerCase().includes(search.toLowerCase()) ||
        user.email.toLowerCase().includes(search.toLowerCase()) ||
        user.username.toLowerCase().includes(search.toLowerCase()),
    );
  }

  if (role) {
    users = users.filter((user) => user.role === role);
  }

  const pageSize = 10;
  const start = (page - 1) * pageSize;
  const end = start + pageSize;

  // Remove password from response
  const usersWithoutPassword = users.map(({ password, ...user }) => user);

  return [
    200,
    {
      users: usersWithoutPassword.slice(start, end),
      userCount: users.length,
    },
  ];
});

mock.onGet(/\/api\/users\/\d+/).reply((config) => {
  const currentUser = getCurrentUser();
  if (!currentUser) {
    return [403, { message: 'Không có quyền truy cập' }];
  }

  const id = parseInt(config.url.split('/').pop());
  const user = usersData.find((u) => u.id === id);
  if (user) {
    const { password, ...userWithoutPassword } = user;
    return [200, userWithoutPassword];
  }
  return [404, { message: 'User not found' }];
});

mock.onPost('/api/users').reply((config) => {
  const currentUser = getCurrentUser();
  if (!currentUser || currentUser.role !== 'admin') {
    return [403, { message: 'Không có quyền truy cập' }];
  }

  const newUser = JSON.parse(config.data);
  const id = Math.max(...usersData.map((u) => u.id)) + 1;
  const now = new Date().toISOString().split('T')[0];
  const uuid = 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function(c) {
    const r = Math.random() * 16 | 0, v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
  const enriched = {
    ...newUser,
    id,
    userId: newUser.userId || uuid,
    status: newUser.status || 'active',
    directPermission: newUser.directPermission ?? false,
    approveRight: newUser.approveRight ?? false,
    financeViewRight: newUser.financeViewRight ?? false,
    exportDataRight: newUser.exportDataRight ?? false,
    createdAt: newUser.createdAt || now,
    createdBy: newUser.createdBy || currentUser.id,
    updatedAt: newUser.updatedAt || now,
    updatedBy: newUser.updatedBy || currentUser.id,
  };
  usersData.push(enriched);
  const { password, ...userWithoutPassword } = enriched;
  return [200, userWithoutPassword];
});

mock.onPut(/\/api\/users\/\d+/).reply((config) => {
  const currentUser = getCurrentUser();
  if (!currentUser || currentUser.role !== 'admin') {
    return [403, { message: 'Không có quyền truy cập' }];
  }

  const id = parseInt(config.url.split('/').pop());
  const updatedUser = JSON.parse(config.data);
  const index = usersData.findIndex((u) => u.id === id);
  if (index !== -1) {
    const now = new Date().toISOString().split('T')[0];
    usersData[index] = { ...usersData[index], ...updatedUser, updatedAt: now, updatedBy: currentUser.id };
    const { password, ...userWithoutPassword } = usersData[index];
    return [200, userWithoutPassword];
  }
  return [404, { message: 'User not found' }];
});

mock.onDelete(/\/api\/users\/\d+/).reply((config) => {
  const currentUser = getCurrentUser();
  if (!currentUser || currentUser.role !== 'admin') {
    return [403, { message: 'Không có quyền truy cập' }];
  }

  const id = parseInt(config.url.split('/').pop());
  const index = usersData.findIndex((u) => u.id === id);
  if (index !== -1) {
    usersData.splice(index, 1);
    return [200, { message: 'User deleted' }];
  }
  return [404, { message: 'User not found' }];
});

// API Quản lý khách hàng
mock.onGet('/api/customers').reply((config) => {
  const { search, page = 1 } = config.params || {};
  let customers = [...customersData];

  if (search) {
    customers = customers.filter(
      (customer) =>
        customer.name.toLowerCase().includes(search.toLowerCase()) ||
        customer.email.toLowerCase().includes(search.toLowerCase()) ||
        customer.phone.includes(search),
    );
  }

  const pageSize = 10;
  const start = (page - 1) * pageSize;
  const end = start + pageSize;

  return [
    200,
    {
      customers: customers.slice(start, end),
      customerCount: customers.length,
    },
  ];
});

mock.onGet(/\/api\/customers\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const customer = customersData.find((c) => c.id === id);
  return customer ? [200, customer] : [404, { message: 'Customer not found' }];
});

mock.onPost('/api/customers').reply((config) => {
  const newCustomer = JSON.parse(config.data);
  const id = Math.max(...customersData.map((c) => c.id)) + 1;
  customersData.push({ ...newCustomer, id });
  return [200, { ...newCustomer, id }];
});

mock.onPut(/\/api\/customers\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const updatedCustomer = JSON.parse(config.data);
  const index = customersData.findIndex((c) => c.id === id);
  if (index !== -1) {
    customersData[index] = { ...updatedCustomer, id };
    return [200, customersData[index]];
  }
  return [404, { message: 'Customer not found' }];
});

mock.onDelete(/\/api\/customers\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const index = customersData.findIndex((c) => c.id === id);
  if (index !== -1) {
    customersData.splice(index, 1);
    return [200, { message: 'Customer deleted' }];
  }
  return [404, { message: 'Customer not found' }];
});

// API Quản lý doanh nghiệp
mock.onGet('/api/businesses').reply((config) => {
  const { search, page = 1 } = config.params || {};
  let businesses = [...businessesData];

  if (search) {
    businesses = businesses.filter(
      (business) =>
        business.name.toLowerCase().includes(search.toLowerCase()) ||
        business.taxCode.includes(search) ||
        business.contactPerson.toLowerCase().includes(search.toLowerCase()),
    );
  }

  const pageSize = 10;
  const start = (page - 1) * pageSize;
  const end = start + pageSize;

  return [
    200,
    {
      businesses: businesses.slice(start, end),
      businessCount: businesses.length,
    },
  ];
});

mock.onGet(/\/api\/businesses\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const business = businessesData.find((b) => b.id === id);
  return business ? [200, business] : [404, { message: 'Business not found' }];
});

mock.onPost('/api/businesses').reply((config) => {
  const newBusiness = JSON.parse(config.data);
  const id = Math.max(...businessesData.map((b) => b.id)) + 1;
  businessesData.push({ ...newBusiness, id });
  return [200, { ...newBusiness, id }];
});

mock.onPut(/\/api\/businesses\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const updatedBusiness = JSON.parse(config.data);
  const index = businessesData.findIndex((b) => b.id === id);
  if (index !== -1) {
    businessesData[index] = { ...updatedBusiness, id };
    return [200, businessesData[index]];
  }
  return [404, { message: 'Business not found' }];
});

mock.onDelete(/\/api\/businesses\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const index = businessesData.findIndex((b) => b.id === id);
  if (index !== -1) {
    businessesData.splice(index, 1);
    return [200, { message: 'Business deleted' }];
  }
  return [404, { message: 'Business not found' }];
});

// API Quản lý công việc
mock.onGet('/api/tasks').reply((config) => {
  const { search, status, page = 1 } = config.params || {};
  let tasks = [...tasksData];

  if (search) {
    tasks = tasks.filter(
      (task) =>
        task.title.toLowerCase().includes(search.toLowerCase()) ||
        task.description.toLowerCase().includes(search.toLowerCase()),
    );
  }

  if (status) {
    tasks = tasks.filter((task) => task.status === status);
  }

  const pageSize = 10;
  const start = (page - 1) * pageSize;
  const end = start + pageSize;

  return [
    200,
    {
      tasks: tasks.slice(start, end),
      taskCount: tasks.length,
    },
  ];
});

mock.onGet(/\/api\/tasks\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const task = tasksData.find((t) => t.id === id);
  return task ? [200, task] : [404, { message: 'Task not found' }];
});

mock.onPost('/api/tasks').reply((config) => {
  const newTask = JSON.parse(config.data);
  const id = Math.max(...tasksData.map((t) => t.id)) + 1;
  tasksData.push({ ...newTask, id });
  return [200, { ...newTask, id }];
});

mock.onPut(/\/api\/tasks\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const updatedTask = JSON.parse(config.data);
  const index = tasksData.findIndex((t) => t.id === id);
  if (index !== -1) {
    tasksData[index] = { ...updatedTask, id };
    return [200, tasksData[index]];
  }
  return [404, { message: 'Task not found' }];
});

mock.onDelete(/\/api\/tasks\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const index = tasksData.findIndex((t) => t.id === id);
  if (index !== -1) {
    tasksData.splice(index, 1);
    return [200, { message: 'Task deleted' }];
  }
  return [404, { message: 'Task not found' }];
});

// API Quản lý thu chi
mock.onGet('/api/transactions').reply((config) => {
  const { search, type, page = 1, pageSize = 10, startDate, endDate } = config.params || {};
  let transactions = [...transactionsData];

  if (search) {
    transactions = transactions.filter(
      (transaction) =>
        transaction.description.toLowerCase().includes(search.toLowerCase()) ||
        transaction.reference.includes(search),
    );
  }

  if (type) {
    transactions = transactions.filter((transaction) => transaction.type === type);
  }

  if (startDate && endDate) {
    transactions = transactions.filter(
      (transaction) =>
        transaction.date >= startDate && transaction.date <= endDate,
    );
  }

  // Nếu pageSize lớn (như 1000) thì trả về tất cả
  if (pageSize >= 1000) {
    return [
      200,
      {
        transactions: transactions,
        transactionCount: transactions.length,
      },
    ];
  }

  const start = (page - 1) * pageSize;
  const end = start + pageSize;

  return [
    200,
    {
      transactions: transactions.slice(start, end),
      transactionCount: transactions.length,
    },
  ];
});

mock.onGet('/api/transactions/summary').reply(() => {
  const totalRevenue = transactionsData
    .filter((t) => t.type === 'revenue' && t.status === 'completed')
    .reduce((sum, t) => sum + t.amount, 0);
  const totalExpense = transactionsData
    .filter((t) => t.type === 'expense' && t.status === 'completed')
    .reduce((sum, t) => sum + t.amount, 0);
  const balance = totalRevenue - totalExpense;

  return [
    200,
    {
      totalRevenue,
      totalExpense,
      balance,
    },
  ];
});

mock.onGet(/\/api\/transactions\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const transaction = transactionsData.find((t) => t.id === id);
  return transaction
    ? [200, transaction]
    : [404, { message: 'Transaction not found' }];
});

mock.onPost('/api/transactions').reply((config) => {
  const newTransaction = JSON.parse(config.data);
  const id = Math.max(...transactionsData.map((t) => t.id)) + 1;
  transactionsData.push({ ...newTransaction, id });
  return [200, { ...newTransaction, id }];
});

mock.onPut(/\/api\/transactions\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const updatedTransaction = JSON.parse(config.data);
  const index = transactionsData.findIndex((t) => t.id === id);
  if (index !== -1) {
    transactionsData[index] = { ...updatedTransaction, id };
    return [200, transactionsData[index]];
  }
  return [404, { message: 'Transaction not found' }];
});

mock.onDelete(/\/api\/transactions\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const index = transactionsData.findIndex((t) => t.id === id);
  if (index !== -1) {
    transactionsData.splice(index, 1);
    return [200, { message: 'Transaction deleted' }];
  }
  return [404, { message: 'Transaction not found' }];
});

// API Quản lý chi phí
mock.onGet('/api/costs').reply((config) => {
  const { search, type, status, page = 1 } = config.params || {};
  let costs = [...costsData];

  if (search) {
    costs = costs.filter(
      (cost) =>
        cost.content.toLowerCase().includes(search.toLowerCase()) ||
        cost.requester.toLowerCase().includes(search.toLowerCase()) ||
        cost.voucherNumber.toLowerCase().includes(search.toLowerCase())
    );
  }

  if (type) {
    costs = costs.filter((cost) => cost.transactionType === type);
  }

  if (status) {
    costs = costs.filter((cost) => cost.paymentStatus === status);
  }

  const pageSize = 10;
  const start = (page - 1) * pageSize;
  const end = start + pageSize;

  return [
    200,
    {
      costs: costs.slice(start, end),
      costCount: costs.length,
    },
  ];
});

mock.onGet(/\/api\/costs\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const cost = costsData.find((c) => c.id === id);
  return cost ? [200, cost] : [404, { message: 'Cost not found' }];
});

mock.onPost('/api/costs').reply((config) => {
  const newCost = JSON.parse(config.data);
  const id = Math.max(...costsData.map((c) => c.id)) + 1;
  costsData.push({ ...newCost, id });
  return [200, { ...newCost, id }];
});

mock.onPut(/\/api\/costs\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const updatedCost = JSON.parse(config.data);
  const index = costsData.findIndex((c) => c.id === id);
  if (index !== -1) {
    costsData[index] = { ...updatedCost, id };
    return [200, costsData[index]];
  }
  return [404, { message: 'Cost not found' }];
});

mock.onDelete(/\/api\/costs\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const index = costsData.findIndex((c) => c.id === id);
  if (index !== -1) {
    costsData.splice(index, 1);
    return [200, { message: 'Cost deleted' }];
  }
  return [404, { message: 'Cost not found' }];
});

mock.onPost(/\/api\/costs\/\d+\/approve/).reply((config) => {
  const currentUser = getCurrentUser();
  if (!currentUser) {
    return [401, { message: 'Chưa đăng nhập' }];
  }

  const parts = config.url.split('/');
  const id = parseInt(parts[parts.length - 2]); // /api/costs/123/approve
  const { notificationRecipients } = JSON.parse(config.data);
  
  const costIndex = costsData.findIndex((c) => c.id === id);
  if (costIndex === -1) {
    return [404, { message: 'Không tìm thấy phiếu chi' }];
  }
  
  const cost = costsData[costIndex];
  let newStatus = cost.paymentStatus;
  
  // Logic duyệt
  const isManager = ['ip_manager', 'quan_ly', 'manager'].includes(currentUser.role);
  const isDirector = ['admin', 'director', 'giam_doc'].includes(currentUser.role);
  
  let notifMessage = '';
  let notifTitle = '';
  let targetUserIds = [];

  if (isManager && cost.paymentStatus === 'Đợi duyệt') {
    newStatus = 'Quản lý duyệt';
    notifTitle = 'Phiếu chi cần duyệt (GĐ)';
    notifMessage = `Manager ${currentUser.fullName} đã duyệt phiếu #${cost.id}. Vui lòng xem xét.`;
    // Gửi cho Director (Admin/CEO)
    // Find director users
    const directors = usersData.filter(u => ['admin', 'director', 'giam_doc'].includes(u.role));
    targetUserIds = directors.map(u => u.id);

  } else if (isDirector) {
    // Director approves
    newStatus = 'Giám đốc duyệt';
    notifTitle = 'Phiếu chi đã được duyệt';
    notifMessage = `Giám đốc ${currentUser.fullName} đã duyệt phiếu #${cost.id}. Vui lòng thực hiện chi tiền.`;
    // Gửi cho Accountant
    const accountants = usersData.filter(u => ['accountant', 'ke_toan'].includes(u.role) || u.department === 'Kế toán');
    targetUserIds = accountants.map(u => u.id);
  } else {
      // Fallback if permission logic on server allows but logic here is fuzzy
      // Or maybe it's already approved?
      if (cost.paymentStatus === 'Quản lý duyệt' && isManager) {
          // Manager approving again? Ignore or update?
      }
  }
  
  // Update cost
  costsData[costIndex] = { 
    ...cost, 
    paymentStatus: newStatus,
    updatedAt: new Date().toISOString().split('T')[0],
    updatedBy: currentUser.id
  };

  // Create notifications
  if (targetUserIds.length > 0) {
      const now = new Date().toISOString();
      targetUserIds.forEach(uid => {
        const notifId = Math.max(...notificationsData.map(n => n.id), 0) + 1;
        notificationsData.unshift({
            id: notifId,
            userId: uid,
            title: notifTitle,
            message: notifMessage,
            type: 'CostApproval',
            relatedId: cost.id.toString(),
            isRead: false,
            createdAt: now
        });
      });
  }

  // Handle manual recipients if any (optional, but good for demo)
  if (notificationRecipients && notificationRecipients.length > 0) {
      const now = new Date().toISOString();
      notificationRecipients.forEach(uid => {
          if (!targetUserIds.includes(uid)) { // Avoid duplicate
            const notifId = Math.max(...notificationsData.map(n => n.id), 0) + 1;
             notificationsData.unshift({
                id: notifId,
                userId: uid,
                title: 'Thông báo duyệt phiếu chi',
                message: `${currentUser.fullName} đã duyệt phiếu #${cost.id}`,
                type: 'CostApproval',
                relatedId: cost.id.toString(),
                isRead: false,
                createdAt: now
            });
          }
      });
  }

  return [200, { message: 'Duyệt phiếu thành công', status: newStatus }];
});

// API Thông báo
mock.onGet('/api/notifications').reply(() => {
  const currentUser = getCurrentUser();
  if (!currentUser) return [401, { message: 'Unauthorized' }];

  // Lọc thông báo của user hiện tại
  // Trong mock này ta giả định user id=2 (Manager) nhận được thông báo
  // Thực tế sẽ filter theo currentUser.id
  
  // Để test dễ dàng, ta trả về tất cả thông báo nếu là admin hoặc manager, 
  // hoặc filter đúng logic nếu muốn chặt chẽ.
  // Ở đây mình filter đơn giản:
  const userNotifs = notificationsData.filter(n => n.userId === currentUser.id || n.userId === currentUser.legacyId); // Support both id formats
  
  // Sort mới nhất trước
  userNotifs.sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));

  const unreadCount = userNotifs.filter(n => !n.isRead).length;

  return [200, {
    notifications: userNotifs,
    unreadCount
  }];
});

mock.onPost('/api/notifications/create').reply((config) => {
  const { title, message, type, relatedId, userIds } = JSON.parse(config.data);
  const now = new Date().toISOString();
  
  // Tạo thông báo cho danh sách userIds
  const newNotifs = [];
  userIds.forEach(uid => {
     const id = Math.max(...notificationsData.map(n => n.id), 0) + 1 + newNotifs.length;
     const notif = {
       id,
       userId: uid,
       title,
       message,
       type,
       relatedId,
       isRead: false,
       createdAt: now
     };
     notificationsData.unshift(notif); // Add to beginning
     newNotifs.push(notif);
  });

  return [200, { message: 'Notifications created', count: newNotifs.length }];
});

mock.onPost(/\/api\/notifications\/mark-read\/\d+/).reply((config) => {
  const id = parseInt(config.url.split('/').pop());
  const notif = notificationsData.find(n => n.id === id);
  if (notif) {
    notif.isRead = true;
    return [200, { message: 'Marked as read' }];
  }
  return [404, { message: 'Notification not found' }];
});

mock.onPost('/api/notifications/mark-all-read').reply(() => {
  const currentUser = getCurrentUser();
  if (currentUser) {
    notificationsData.forEach(n => {
        if (n.userId === currentUser.id) {
            n.isRead = true;
        }
    });
  }
  return [200, { message: 'All marked as read' }];
});

// API Phân quyền
mock.onGet('/api/permissions').reply(() => {
  return [200, {
    permissions: currentPermissions,
    roles,
    qlkhFields,
    qlcpFields,
    schedulingFields,
  }];
});

mock.onPost('/api/permissions').reply((config) => {
  const newPermissions = JSON.parse(config.data);
  currentPermissions = newPermissions;
  return [200, { message: 'Cập nhật phân quyền thành công' }];
});

// Initialize mock adapter
export default mock;

// Export for use in App.js
export { mock };
