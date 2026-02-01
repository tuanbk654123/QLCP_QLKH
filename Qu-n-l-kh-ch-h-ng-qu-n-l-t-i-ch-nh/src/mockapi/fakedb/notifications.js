export const notificationsData = [
  {
    id: 1,
    userId: 2, // Manager
    title: 'Phiếu chi mới cần duyệt',
    message: 'Nhân viên A vừa tạo phiếu chi #1001. Vui lòng kiểm tra và phê duyệt.',
    type: 'CostApproval',
    relatedId: '1',
    isRead: false,
    createdAt: '2023-10-26T09:00:00',
  },
  {
    id: 2,
    userId: 2, // Manager
    title: 'Phiếu chi mới cần duyệt',
    message: 'Nhân viên B vừa tạo phiếu chi #1002. Vui lòng kiểm tra và phê duyệt.',
    type: 'CostApproval',
    relatedId: '2',
    isRead: true,
    createdAt: '2023-10-25T14:30:00',
  }
];
