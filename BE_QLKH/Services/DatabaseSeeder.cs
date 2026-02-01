using BE_QLKH.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;

namespace BE_QLKH.Services;

public class DatabaseSeeder : IHostedService
{
    private readonly IMongoClient _client;
    private readonly MongoDbSettings _settings;

    public DatabaseSeeder(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        _client = client;
        _settings = options.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var db = _client.GetDatabase(_settings.DatabaseName);

        var existingCollections = await db.ListCollectionNames().ToListAsync(cancellationToken);

        async Task EnsureCollection(string name)
        {
            if (!existingCollections.Contains(name))
            {
                await db.CreateCollectionAsync(name, cancellationToken: cancellationToken);
            }
        }

        await EnsureCollection("users");
        await EnsureCollection("customers");
        await EnsureCollection("costs");
        await EnsureCollection("roles");
        await EnsureCollection("modules");
        await EnsureCollection("fields");
        await EnsureCollection("field_permissions");

        var usersCollection = db.GetCollection<User>("users");
        if (await usersCollection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken) == 0)
        {
            var now = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

            var defaultUsers = new List<User>
            {
                new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 1,
                    UserId = "U001",
                    Username = "admin",
                    FullName = "Quản trị hệ thống",
                    Email = "admin@example.com",
                    PasswordHash = HashPassword("123456"),
                    RoleCode = "admin",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = 0,
                    UpdatedBy = 0
                },
                new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 2,
                    UserId = "U002",
                    Username = "ceo",
                    FullName = "Tổng giám đốc",
                    Email = "ceo@example.com",
                    PasswordHash = HashPassword("123456"),
                    RoleCode = "ceo",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = 0,
                    UpdatedBy = 0
                },
                new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 3,
                    UserId = "U003",
                    Username = "director",
                    FullName = "Giám đốc",
                    Email = "director@example.com",
                    PasswordHash = HashPassword("123456"),
                    RoleCode = "director",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = 0,
                    UpdatedBy = 0
                },
                new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 4,
                    UserId = "U004",
                    Username = "accountant",
                    FullName = "Kế toán",
                    Email = "accountant@example.com",
                    PasswordHash = HashPassword("123456"),
                    RoleCode = "accountant",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = 0,
                    UpdatedBy = 0
                },
                new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 5,
                    UserId = "U005",
                    Username = "sales",
                    FullName = "Marketing/Sales",
                    Email = "sales@example.com",
                    PasswordHash = HashPassword("123456"),
                    RoleCode = "marketing_sales",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = 0,
                    UpdatedBy = 0
                },
                new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 6,
                    UserId = "U006",
                    Username = "manager",
                    FullName = "Quản lý IP",
                    Email = "tuanvb96@gmail.com",
                    PasswordHash = HashPassword("123456"),
                    RoleCode = "ip_manager",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = 0,
                    UpdatedBy = 0
                },
                new User
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 7,
                    UserId = "U007",
                    Username = "executive",
                    FullName = "Chuyên viên IP",
                    Email = "executive@example.com",
                    PasswordHash = HashPassword("123456"),
                    RoleCode = "ip_executive",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = 0,
                    UpdatedBy = 0
                }
            };

            await usersCollection.InsertManyAsync(defaultUsers, cancellationToken: cancellationToken);
        }

        var customersCollection = db.GetCollection<Customer>("customers");
        if (await customersCollection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken) == 0)
        {
            var customers = new List<Customer>
            {
                new Customer
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 1,
                    Name = "Nguyễn Văn A",
                    Email = "nguyenvana@example.com",
                    Phone = "0901234567",
                    Address = "123 Đường ABC, Quận 1, TP.HCM",
                    Company = "Công ty ABC",
                    TaxCode = "0101234567",
                    RepresentativeName = "Nguyễn Văn A",
                    RepresentativePosition = "Giám đốc",
                    RepresentativePhone = "0901234567",
                    BusinessNeeds = "Cần tư vấn giải pháp quản lý kho",
                    BusinessScale = "50-100 nhân sự",
                    BusinessIndustry = "Sản xuất",
                    CopyrightStatus = "Đã đăng ký",
                    TrademarkStatus = "Đã bảo hộ",
                    PatentStatus = "Không có",
                    IndustrialDesign = "Đang thẩm định",
                    ContractStatus = "Đang thực hiện",
                    Status = "active",
                    TotalOrders = 15,
                    TotalRevenue = 50000000m,
                    JoinDate = "2023-01-15",
                    Notes = "Khách hàng VIP",
                    ProductsServices = "Phần mềm quản lý",
                    IpGroup = "Nhóm 1",
                    ConsultingStatus = "Đã tư vấn",
                    FilingStatus = "Đã viết hồ sơ",
                    DocumentLink = "https://drive.google.com/drive/u/0/folders/1",
                    Authorization = "Đã có",
                    ApplicationReviewStatus = "Đang xét duyệt",
                    Priority = "Mức 1",
                    ContractPaid = "50%",
                    ContractValue = 100000000m,
                    StartDate = "2023-01-20",
                    EndDate = "2023-12-20",
                    ImplementationDays = 330,
                    PotentialLevel = "Cao",
                    SourceClassification = "Đối tác",
                    NsnnSource = ""
                },
                new Customer
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 2,
                    Name = "Trần Thị B",
                    Email = "tranthib@example.com",
                    Phone = "0902345678",
                    Address = "456 Đường XYZ, Quận 2, TP.HCM",
                    Company = "Công ty XYZ",
                    TaxCode = "0102345678",
                    RepresentativeName = "Trần Thị B",
                    RepresentativePosition = "Trưởng phòng mua hàng",
                    RepresentativePhone = "0902345678",
                    BusinessNeeds = "Tìm kiếm nguồn nguyên liệu mới",
                    BusinessScale = "10-50 nhân sự",
                    BusinessIndustry = "Thương mại",
                    CopyrightStatus = "Chưa có",
                    TrademarkStatus = "Đang đăng ký",
                    PatentStatus = "Không có",
                    IndustrialDesign = "Không có",
                    ContractStatus = "Sắp hết hạn",
                    Status = "active",
                    TotalOrders = 8,
                    TotalRevenue = 25000000m,
                    JoinDate = "2023-03-20",
                    Notes = "",
                    ProductsServices = "Thực phẩm sạch",
                    IpGroup = "Nhóm 2",
                    ConsultingStatus = "Đang tư vấn",
                    FilingStatus = "Chưa triển khai",
                    DocumentLink = "",
                    Authorization = "Chưa có",
                    ApplicationReviewStatus = "Chưa có",
                    Priority = "Mức 2",
                    ContractPaid = "0%",
                    ContractValue = 50000000m,
                    StartDate = "2023-04-01",
                    EndDate = "2023-10-01",
                    ImplementationDays = 180,
                    PotentialLevel = "Trung bình",
                    SourceClassification = "Vãng lai",
                    NsnnSource = ""
                },
                new Customer
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 3,
                    Name = "Lê Văn C",
                    Email = "levanc@example.com",
                    Phone = "0903456789",
                    Address = "789 Đường DEF, Quận 3, TP.HCM",
                    Company = "Công ty DEF",
                    TaxCode = "0103456789",
                    RepresentativeName = "Lê Văn C",
                    RepresentativePosition = "Phó Giám đốc",
                    RepresentativePhone = "0903456789",
                    BusinessNeeds = "Mở rộng thị trường miền Bắc",
                    BusinessScale = "100-200 nhân sự",
                    BusinessIndustry = "Xây dựng",
                    CopyrightStatus = "Đã đăng ký",
                    TrademarkStatus = "Đã bảo hộ",
                    PatentStatus = "Đã có bằng",
                    IndustrialDesign = "Đã bảo hộ",
                    ContractStatus = "Đã thanh lý",
                    Status = "inactive",
                    TotalOrders = 3,
                    TotalRevenue = 8000000m,
                    JoinDate = "2022-11-10",
                    Notes = "Khách hàng cũ",
                    ProductsServices = "Vật liệu xây dựng",
                    IpGroup = "Nhóm 3",
                    ConsultingStatus = "Đã tư vấn",
                    FilingStatus = "Đã viết hồ sơ",
                    DocumentLink = "https://drive.google.com/drive/u/0/folders/3",
                    Authorization = "Đã có",
                    ApplicationReviewStatus = "Được duyệt",
                    Priority = "Mức 3",
                    ContractPaid = "100%",
                    ContractValue = 200000000m,
                    StartDate = "2022-12-01",
                    EndDate = "2023-06-01",
                    ImplementationDays = 180,
                    PotentialLevel = "Thấp",
                    SourceClassification = "NSNN",
                    NsnnSource = "Sở TC Huế"
                },
                new Customer
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 4,
                    Name = "Phạm Thị D",
                    Email = "phamthid@example.com",
                    Phone = "0904567890",
                    Address = "321 Đường GHI, Quận 4, TP.HCM",
                    Company = "Công ty GHI",
                    TaxCode = "0104567890",
                    RepresentativeName = "Phạm Thị D",
                    RepresentativePosition = "Kế toán trưởng",
                    RepresentativePhone = "0904567890",
                    BusinessNeeds = "Nâng cấp hệ thống phần mềm kế toán",
                    BusinessScale = "20-30 nhân sự",
                    BusinessIndustry = "Dịch vụ",
                    CopyrightStatus = "Không áp dụng",
                    TrademarkStatus = "Chưa đăng ký",
                    PatentStatus = "Không có",
                    IndustrialDesign = "Không có",
                    ContractStatus = "Đang đàm phán gia hạn",
                    Status = "active",
                    TotalOrders = 12,
                    TotalRevenue = 40000000m,
                    JoinDate = "2023-02-05",
                    Notes = "",
                    ProductsServices = "Dịch vụ kế toán",
                    IpGroup = "Nhóm 4",
                    ConsultingStatus = "Chưa tư vấn",
                    FilingStatus = "Chưa triển khai",
                    DocumentLink = "",
                    Authorization = "Chưa có",
                    ApplicationReviewStatus = "Chưa có",
                    Priority = "Mức 2",
                    ContractPaid = "0%",
                    ContractValue = 30000000m,
                    StartDate = "",
                    EndDate = "",
                    ImplementationDays = 0,
                    PotentialLevel = "Trung bình",
                    SourceClassification = "Sự kiện",
                    NsnnSource = ""
                },
                new Customer
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 5,
                    Name = "Hoàng Văn E",
                    Email = "hoangvane@example.com",
                    Phone = "0905678901",
                    Address = "654 Đường JKL, Quận 5, TP.HCM",
                    Company = "Công ty JKL",
                    TaxCode = "0105678901",
                    RepresentativeName = "Hoàng Văn E",
                    RepresentativePosition = "Giám đốc điều hành",
                    RepresentativePhone = "0905678901",
                    BusinessNeeds = "Tư vấn tái cấu trúc doanh nghiệp",
                    BusinessScale = "500+ nhân sự",
                    BusinessIndustry = "Công nghệ",
                    CopyrightStatus = "Đã đăng ký nhiều bản quyền",
                    TrademarkStatus = "Thương hiệu mạnh",
                    PatentStatus = "Sở hữu 5 bằng sáng chế",
                    IndustrialDesign = "Đã đăng ký",
                    ContractStatus = "Hợp đồng dài hạn",
                    Status = "active",
                    TotalOrders = 20,
                    TotalRevenue = 75000000m,
                    JoinDate = "2022-09-15",
                    Notes = "Khách hàng thân thiết",
                    ProductsServices = "Giải pháp AI",
                    IpGroup = "Nhóm 1",
                    ConsultingStatus = "Đã tư vấn",
                    FilingStatus = "Đang viết hồ sơ",
                    DocumentLink = "https://drive.google.com/drive/u/0/folders/5",
                    Authorization = "Đã có",
                    ApplicationReviewStatus = "Có OA chưa phản hồi",
                    Priority = "Mức 1",
                    ContractPaid = "80%",
                    ContractValue = 500000000m,
                    StartDate = "",
                    EndDate = "",
                    ImplementationDays = 0,
                    PotentialLevel = "Cao",
                    SourceClassification = "Đối tác",
                    NsnnSource = ""
                },
                new Customer
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 6,
                    Name = "Nguyễn Văn F (2026)",
                    Email = "nguyenvanf@example.com",
                    Phone = "0906789012",
                    Address = "123 Đường 2026, Quận 1, TP.HCM",
                    Company = "Công ty 2026 A",
                    TaxCode = "0106789012",
                    RepresentativeName = "Nguyễn Văn F",
                    RepresentativePosition = "Giám đốc",
                    RepresentativePhone = "0906789012",
                    BusinessNeeds = "Tư vấn 2026",
                    BusinessScale = "100+ nhân sự",
                    BusinessIndustry = "Công nghệ",
                    CopyrightStatus = "Đã đăng ký",
                    TrademarkStatus = "Đang thẩm định",
                    PatentStatus = "Không có",
                    IndustrialDesign = "Không có",
                    ContractStatus = "Mới ký",
                    Status = "active",
                    TotalOrders = 5,
                    TotalRevenue = 150000000m,
                    JoinDate = "2026-01-10",
                    Notes = "Khách hàng mới 2026",
                    ProductsServices = "Giải pháp Cloud",
                    IpGroup = "Nhóm 1",
                    ConsultingStatus = "Đang tư vấn",
                    FilingStatus = "Đã nộp",
                    DocumentLink = "",
                    Authorization = "Đã có",
                    ApplicationReviewStatus = "Chưa có",
                    Priority = "Mức 1",
                    ContractPaid = "30%",
                    ContractValue = 500000000m,
                    StartDate = "2026-01-15",
                    EndDate = "2026-12-15",
                    ImplementationDays = 330,
                    PotentialLevel = "Cao",
                    SourceClassification = "Marketing",
                    NsnnSource = ""
                },
                new Customer
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 7,
                    Name = "Trần Thị G (2026)",
                    Email = "tranthig@example.com",
                    Phone = "0907890123",
                    Address = "456 Đường 2026, Quận 2, TP.HCM",
                    Company = "Công ty 2026 B",
                    TaxCode = "0107890123",
                    RepresentativeName = "Trần Thị G",
                    RepresentativePosition = "Trưởng phòng",
                    RepresentativePhone = "0907890123",
                    BusinessNeeds = "Đăng ký nhãn hiệu",
                    BusinessScale = "20 nhân sự",
                    BusinessIndustry = "Bán lẻ",
                    CopyrightStatus = "Chưa có",
                    TrademarkStatus = "Chưa đăng ký",
                    PatentStatus = "Không có",
                    IndustrialDesign = "Không có",
                    ContractStatus = "Đang đàm phán",
                    Status = "active",
                    TotalOrders = 2,
                    TotalRevenue = 20000000m,
                    JoinDate = "2026-02-15",
                    Notes = "",
                    ProductsServices = "Tư vấn luật",
                    IpGroup = "Nhóm 2",
                    ConsultingStatus = "Chưa tư vấn",
                    FilingStatus = "Chưa nộp",
                    DocumentLink = "",
                    Authorization = "Chưa có",
                    ApplicationReviewStatus = "Chưa có",
                    Priority = "Mức 2",
                    ContractPaid = "0%",
                    ContractValue = 50000000m,
                    StartDate = "",
                    EndDate = "",
                    ImplementationDays = 0,
                    PotentialLevel = "Trung bình",
                    SourceClassification = "Giới thiệu",
                    NsnnSource = ""
                }
            };

            await customersCollection.InsertManyAsync(customers, cancellationToken: cancellationToken);
        }
        else
        {
            // Ensure 2026 customers exist if collection is not empty
            var existing2026Customer = await customersCollection.Find(x => x.Email == "nguyenvanf@example.com").FirstOrDefaultAsync(cancellationToken);
            if (existing2026Customer == null)
            {
                var additionalCustomers = new List<Customer>
                {
                    new Customer
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        LegacyId = 6,
                        Name = "Nguyễn Văn F (2026)",
                        Email = "nguyenvanf@example.com",
                        Phone = "0906789012",
                        Address = "123 Đường 2026, Quận 1, TP.HCM",
                        Company = "Công ty 2026 A",
                        TaxCode = "0106789012",
                        RepresentativeName = "Nguyễn Văn F",
                        RepresentativePosition = "Giám đốc",
                        RepresentativePhone = "0906789012",
                        BusinessNeeds = "Tư vấn 2026",
                        BusinessScale = "100+ nhân sự",
                        BusinessIndustry = "Công nghệ",
                        CopyrightStatus = "Đã đăng ký",
                        TrademarkStatus = "Đang thẩm định",
                        PatentStatus = "Không có",
                        IndustrialDesign = "Không có",
                        ContractStatus = "Mới ký",
                        Status = "active",
                        TotalOrders = 5,
                        TotalRevenue = 150000000m,
                        JoinDate = "2026-01-10",
                        Notes = "Khách hàng mới 2026",
                        ProductsServices = "Giải pháp Cloud",
                        IpGroup = "Nhóm 1",
                        ConsultingStatus = "Đang tư vấn",
                        FilingStatus = "Đã nộp",
                        DocumentLink = "",
                        Authorization = "Đã có",
                        ApplicationReviewStatus = "Chưa có",
                        Priority = "Mức 1",
                        ContractPaid = "30%",
                        ContractValue = 500000000m,
                        StartDate = "2026-01-15",
                        EndDate = "2026-12-15",
                        ImplementationDays = 330,
                        PotentialLevel = "Cao",
                        SourceClassification = "Marketing",
                        NsnnSource = ""
                    },
                    new Customer
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        LegacyId = 7,
                        Name = "Trần Thị G (2026)",
                        Email = "tranthig@example.com",
                        Phone = "0907890123",
                        Address = "456 Đường 2026, Quận 2, TP.HCM",
                        Company = "Công ty 2026 B",
                        TaxCode = "0107890123",
                        RepresentativeName = "Trần Thị G",
                        RepresentativePosition = "Trưởng phòng",
                        RepresentativePhone = "0907890123",
                        BusinessNeeds = "Đăng ký nhãn hiệu",
                        BusinessScale = "20 nhân sự",
                        BusinessIndustry = "Bán lẻ",
                        CopyrightStatus = "Chưa có",
                        TrademarkStatus = "Chưa đăng ký",
                        PatentStatus = "Không có",
                        IndustrialDesign = "Không có",
                        ContractStatus = "Đang đàm phán",
                        Status = "active",
                        TotalOrders = 2,
                        TotalRevenue = 20000000m,
                        JoinDate = "2026-02-15",
                        Notes = "",
                        ProductsServices = "Tư vấn luật",
                        IpGroup = "Nhóm 2",
                        ConsultingStatus = "Chưa tư vấn",
                        FilingStatus = "Chưa nộp",
                        DocumentLink = "",
                        Authorization = "Chưa có",
                        ApplicationReviewStatus = "Chưa có",
                        Priority = "Mức 2",
                        ContractPaid = "0%",
                        ContractValue = 50000000m,
                        StartDate = "",
                        EndDate = "",
                        ImplementationDays = 0,
                        PotentialLevel = "Trung bình",
                        SourceClassification = "Giới thiệu",
                        NsnnSource = ""
                    }
                };
                await customersCollection.InsertManyAsync(additionalCustomers, cancellationToken: cancellationToken);
            }
        }

        var costsCollection = db.GetCollection<Cost>("costs");
        if (await costsCollection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken) == 0)
        {
            var costs = new List<Cost>
            {
                // Dữ liệu 2026
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 101,
                    Requester = "Nguyễn Văn A",
                    Department = "Kinh doanh",
                    RequestDate = "2026-01-05",
                    ProjectCode = "PJ2026-001",
                    TransactionType = "Chi",
                    TransactionObject = "Nhà hàng 2026",
                    Content = "Chi phí tiếp khách",
                    Description = "Tiếp khách dự án PJ2026-001",
                    AmountBeforeTax = 6000000m,
                    TaxRate = "10%",
                    TotalAmount = 6600000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Techcombank",
                    AccountNumber = "190333333333",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2026001",
                    VoucherDate = "2026-01-05",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 102,
                    Requester = "Trần Thị B",
                    Department = "Hành chính",
                    RequestDate = "2026-02-10",
                    ProjectCode = "PJ2026-002",
                    TransactionType = "Chi",
                    TransactionObject = "VPP 2026",
                    Content = "Mua sắm thiết bị",
                    Description = "Mua máy in mới",
                    AmountBeforeTax = 8000000m,
                    TaxRate = "8%",
                    TotalAmount = 8640000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "ACB",
                    AccountNumber = "12345678",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2026002",
                    VoucherDate = "2026-02-10",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 103,
                    Requester = "Lê Văn C",
                    Department = "Kỹ thuật",
                    RequestDate = "2026-03-15",
                    ProjectCode = "PJ2026-003",
                    TransactionType = "Tạm ứng",
                    TransactionObject = "Lê Văn C",
                    Content = "Công tác phí",
                    Description = "Đi công tác Đà Nẵng",
                    AmountBeforeTax = 5000000m,
                    TaxRate = "0%",
                    TotalAmount = 5000000m,
                    PaymentMethod = "Tiền mặt",
                    Bank = "",
                    AccountNumber = "",
                    VoucherType = "Giấy đề nghị",
                    VoucherNumber = "TU2026001",
                    VoucherDate = "2026-03-15",
                    Attachment = "",
                    PaymentStatus = "Đợi duyệt",
                    RejectionReason = "",
                    Note = ""
                },
                // Dữ liệu 2023
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 1,
                    Requester = "Nguyễn Văn A",
                    Department = "Kinh doanh",
                    RequestDate = "2023-01-15",
                    ProjectCode = "TACs25ND80",
                    TransactionType = "Chi",
                    TransactionObject = "Nhà hàng Biển Đông",
                    Content = "Chi phí tiếp khách",
                    Description = "Tiếp khách dự án TACs25ND80",
                    AmountBeforeTax = 5000000m,
                    TaxRate = "10%",
                    TotalAmount = 5500000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Vietcombank",
                    AccountNumber = "0011001234567",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD001234",
                    VoucherDate = "2023-01-15",
                    Attachment = "hoadon_tiepkhach.pdf",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = "Đã duyệt bởi Giám đốc"
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 2,
                    Requester = "Trần Thị B",
                    Department = "Hành chính",
                    RequestDate = "2023-01-20",
                    ProjectCode = "",
                    TransactionType = "Chi",
                    TransactionObject = "Công ty VPP Hồng Hà",
                    Content = "Mua văn phòng phẩm",
                    Description = "Mua giấy, bút cho văn phòng",
                    AmountBeforeTax = 2000000m,
                    TaxRate = "8%",
                    TotalAmount = 2160000m,
                    PaymentMethod = "Tiền mặt",
                    Bank = "",
                    AccountNumber = "",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD005678",
                    VoucherDate = "2023-01-20",
                    Attachment = "hoadon_vpp.pdf",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 3,
                    Requester = "Lê Văn C",
                    Department = "Kỹ thuật",
                    RequestDate = "2023-02-01",
                    ProjectCode = "STCHue25ND80",
                    TransactionType = "Tạm ứng",
                    TransactionObject = "Lê Văn C",
                    Content = "Tạm ứng công tác phí",
                    Description = "Đi công tác Huế triển khai dự án",
                    AmountBeforeTax = 3000000m,
                    TaxRate = "No VAT",
                    TotalAmount = 3000000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Techcombank",
                    AccountNumber = "1903456789012",
                    VoucherType = "Phiếu chi",
                    VoucherNumber = "PC001",
                    VoucherDate = "2023-02-01",
                    Attachment = "de_nghi_tam_ung.pdf",
                    PaymentStatus = "Đợi duyệt",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 4,
                    Requester = "Phạm Thị D",
                    Department = "Kế toán",
                    RequestDate = "2023-02-05",
                    ProjectCode = "",
                    TransactionType = "Thu",
                    TransactionObject = "Khách hàng XYZ",
                    Content = "Thu tiền hợp đồng đợt 1",
                    Description = "Khách hàng thanh toán 50% giá trị hợp đồng",
                    AmountBeforeTax = 50000000m,
                    TaxRate = "10%",
                    TotalAmount = 55000000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Vietinbank",
                    AccountNumber = "111222333",
                    VoucherType = "Phiếu thu",
                    VoucherNumber = "PT001",
                    VoucherDate = "2023-02-05",
                    Attachment = "unc_khachhang.pdf",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 5,
                    Requester = "Hoàng Văn E",
                    Department = "Marketing",
                    RequestDate = "2023-02-10",
                    ProjectCode = "SCTQTri25ND80",
                    TransactionType = "Hoàn ứng",
                    TransactionObject = "Hoàng Văn E",
                    Content = "Hoàn ứng chi phí chạy quảng cáo",
                    Description = "Chi phí chạy quảng cáo Facebook tháng 1",
                    AmountBeforeTax = 1500000m,
                    TaxRate = "No VAT",
                    TotalAmount = 1500000m,
                    PaymentMethod = "Tiền mặt",
                    Bank = "",
                    AccountNumber = "",
                    VoucherType = "Phiếu thu",
                    VoucherNumber = "PT002",
                    VoucherDate = "2023-02-10",
                    Attachment = "bang_ke_chi_phi.xlsx",
                    PaymentStatus = "Quản lý duyệt",
                    RejectionReason = "",
                    Note = ""
                },

                // Dữ liệu 2024 - phục vụ Dashboard
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 6,
                    Requester = "Nguyễn Văn A",
                    Department = "Kinh doanh",
                    RequestDate = "2024-01-05",
                    ProjectCode = "KH2024-01",
                    TransactionType = "Thu",
                    TransactionObject = "Công ty ABC",
                    Content = "Thu tiền dịch vụ tháng 1",
                    Description = "Khách hàng ABC thanh toán dịch vụ tháng 1",
                    AmountBeforeTax = 20000000m,
                    TaxRate = "10%",
                    TotalAmount = 22000000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Vietcombank",
                    AccountNumber = "0011009999999",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2024-001",
                    VoucherDate = "2024-01-05",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 7,
                    Requester = "Trần Thị B",
                    Department = "Hành chính",
                    RequestDate = "2024-01-10",
                    ProjectCode = "",
                    TransactionType = "Chi",
                    TransactionObject = "Công ty VPP Hồng Hà",
                    Content = "Mua văn phòng phẩm tháng 1",
                    Description = "Mua giấy in, bút viết",
                    AmountBeforeTax = 3000000m,
                    TaxRate = "8%",
                    TotalAmount = 3240000m,
                    PaymentMethod = "Tiền mặt",
                    Bank = "",
                    AccountNumber = "",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2024-010",
                    VoucherDate = "2024-01-10",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 8,
                    Requester = "Lê Văn C",
                    Department = "Kỹ thuật",
                    RequestDate = "2024-01-15",
                    ProjectCode = "DA2024-IT01",
                    TransactionType = "Chi",
                    TransactionObject = "Nhà cung cấp thiết bị",
                    Content = "Mua thiết bị triển khai dự án",
                    Description = "Mua router, switch, thiết bị mạng",
                    AmountBeforeTax = 15000000m,
                    TaxRate = "10%",
                    TotalAmount = 16500000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Techcombank",
                    AccountNumber = "1903999888777",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2024-015",
                    VoucherDate = "2024-01-15",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 9,
                    Requester = "Phạm Thị D",
                    Department = "Kế toán",
                    RequestDate = "2024-02-05",
                    ProjectCode = "",
                    TransactionType = "Thu",
                    TransactionObject = "Khách hàng XYZ",
                    Content = "Thu tiền hợp đồng đợt 2",
                    Description = "Khách hàng thanh toán 50% còn lại",
                    AmountBeforeTax = 50000000m,
                    TaxRate = "10%",
                    TotalAmount = 55000000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Vietinbank",
                    AccountNumber = "111222333",
                    VoucherType = "Phiếu thu",
                    VoucherNumber = "PT2024-002",
                    VoucherDate = "2024-02-05",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 10,
                    Requester = "Hoàng Văn E",
                    Department = "Marketing",
                    RequestDate = "2024-02-20",
                    ProjectCode = "MKT2024-01",
                    TransactionType = "Chi",
                    TransactionObject = "Đối tác quảng cáo",
                    Content = "Chi phí chạy quảng cáo tháng 2",
                    Description = "Chiến dịch quảng cáo Facebook/Google",
                    AmountBeforeTax = 8000000m,
                    TaxRate = "10%",
                    TotalAmount = 8800000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "ACB",
                    AccountNumber = "666777888",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2024-020",
                    VoucherDate = "2024-02-20",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 11,
                    Requester = "Nguyễn Văn A",
                    Department = "Kinh doanh",
                    RequestDate = "2024-03-10",
                    ProjectCode = "KH2024-02",
                    TransactionType = "Thu",
                    TransactionObject = "Công ty DEF",
                    Content = "Thu tiền dịch vụ tháng 3",
                    Description = "Khách hàng DEF thanh toán dịch vụ tháng 3",
                    AmountBeforeTax = 25000000m,
                    TaxRate = "10%",
                    TotalAmount = 27500000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "MB Bank",
                    AccountNumber = "123456789",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2024-030",
                    VoucherDate = "2024-03-10",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                },
                new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 12,
                    Requester = "Trần Thị B",
                    Department = "Hành chính",
                    RequestDate = "2024-03-18",
                    ProjectCode = "",
                    TransactionType = "Chi",
                    TransactionObject = "Đơn vị tổ chức sự kiện",
                    Content = "Tổ chức sự kiện nội bộ quý I",
                    Description = "Chi phí hội thảo nội bộ",
                    AmountBeforeTax = 12000000m,
                    TaxRate = "10%",
                    TotalAmount = 13200000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "Vietcombank",
                    AccountNumber = "0011001231234",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD2024-035",
                    VoucherDate = "2024-03-18",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                }
            };

            await costsCollection.InsertManyAsync(costs, cancellationToken: cancellationToken);
        }

        var rolesCollection = db.GetCollection<Role>("roles");
        if (await rolesCollection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken) == 0)
        {
            var defaultRoles = new List<Role>
            {
                new Role { Code = "marketing_sales", Name = "Marketing/Sales", IsActive = true },
                new Role { Code = "ip_executive", Name = "IP Executive", IsActive = true },
                new Role { Code = "ip_manager", Name = "IP Manager", IsActive = true },
                new Role { Code = "accountant", Name = "Kế toán", IsActive = true },
                new Role { Code = "director", Name = "Giám đốc", IsActive = true },
                new Role { Code = "ceo", Name = "Tổng giám đốc", IsActive = true },
                new Role { Code = "admin", Name = "Admin", IsActive = true }
            };

            await rolesCollection.InsertManyAsync(defaultRoles, cancellationToken: cancellationToken);
        }

        var modulesCollection = db.GetCollection<ModuleDef>("modules");
        if (await modulesCollection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken) == 0)
        {
            var defaultModules = new List<ModuleDef>
            {
                new ModuleDef { Code = "qlkh", Name = "Quản lý khách hàng", IsActive = true },
                new ModuleDef { Code = "qlcp", Name = "Quản lý chi phí", IsActive = true },
                new ModuleDef { Code = "dashboard", Name = "Dashboard", IsActive = true },
                new ModuleDef { Code = "users", Name = "Quản lý nhân viên", IsActive = true },
                new ModuleDef { Code = "export", Name = "Xuất văn bản", IsActive = true },
                new ModuleDef { Code = "scheduling", Name = "Chấm công dự án", IsActive = true }
            };

            await modulesCollection.InsertManyAsync(defaultModules, cancellationToken: cancellationToken);
        }
        else 
        {
             // Ensure scheduling module exists if DB already populated
             var schedMod = await modulesCollection.Find(m => m.Code == "scheduling").FirstOrDefaultAsync(cancellationToken);
             if (schedMod == null)
             {
                 await modulesCollection.InsertOneAsync(new ModuleDef { Code = "scheduling", Name = "Chấm công dự án", IsActive = true }, cancellationToken: cancellationToken);
             }
        }

        var fieldsCollection = db.GetCollection<FieldDef>("fields");
        var fieldPermissionsCollection = db.GetCollection<FieldPermission>("field_permissions");

        if (await fieldsCollection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken) == 0)
        {
            var fieldDefs = new List<FieldDef>
            {
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "name", Label = "Doanh nghiệp", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "businessScale", Label = "Quy mô DN", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "taxCode", Label = "Mã số thuế", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "address", Label = "Địa chỉ", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "representativeName", Label = "Người đại diện", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 5 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "representativePosition", Label = "Chức vụ", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 6 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "idNumber", Label = "CCCD/Hộ chiếu", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 7 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "phone", Label = "SĐT DN", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 8 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "email", Label = "Email DN", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 9 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "contactPerson", Label = "Người liên hệ", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 10 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "contactPhone", Label = "SĐT người liên hệ", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 11 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "contactEmail", Label = "Email người liên hệ", GroupCode = "group_general", GroupLabel = "I. Nhóm thông tin chung", OrderIndex = 12 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "businessNeeds", Label = "Nhu cầu DN", GroupCode = "group_need", GroupLabel = "II. Nhu cầu - Lead - Tiềm năng", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "potentialLevel", Label = "Mức độ tiềm năng", GroupCode = "group_need", GroupLabel = "II. Nhu cầu - Lead - Tiềm năng", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "priority", Label = "Ưu tiên", GroupCode = "group_need", GroupLabel = "II. Nhu cầu - Lead - Tiềm năng", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "sourceClassification", Label = "Phân loại nguồn", GroupCode = "group_need", GroupLabel = "II. Nhu cầu - Lead - Tiềm năng", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "nsnnSource", Label = "Nguồn NSNN", GroupCode = "group_need", GroupLabel = "II. Nhu cầu - Lead - Tiềm năng", OrderIndex = 5 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "brandName", Label = "Thương hiệu", GroupCode = "group_core_ip", GroupLabel = "III. Thông tin SHTT cốt lõi", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "productsServices", Label = "Sản phẩm/DV", GroupCode = "group_core_ip", GroupLabel = "III. Thông tin SHTT cốt lõi", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "ipGroup", Label = "Nhóm SHTT", GroupCode = "group_core_ip", GroupLabel = "III. Thông tin SHTT cốt lõi", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "owner", Label = "Chủ sở hữu", GroupCode = "group_core_ip", GroupLabel = "III. Thông tin SHTT cốt lõi", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "protectionTerritory", Label = "Lãnh thổ bảo hộ", GroupCode = "group_core_ip", GroupLabel = "III. Thông tin SHTT cốt lõi", OrderIndex = 5 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "authorization", Label = "Uỷ quyền", GroupCode = "group_core_ip", GroupLabel = "III. Thông tin SHTT cốt lõi", OrderIndex = 6 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "filingStatus", Label = "Tình trạng nộp đơn", GroupCode = "group_legal", GroupLabel = "IV. Hồ sơ đơn – văn bằng – pháp lý", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "filingDate", Label = "Ngày nộp đơn", GroupCode = "group_legal", GroupLabel = "IV. Hồ sơ đơn – văn bằng – pháp lý", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "applicationCode", Label = "Mã đơn/Công bố/VB", GroupCode = "group_legal", GroupLabel = "IV. Hồ sơ đơn – văn bằng – pháp lý", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "issueDate", Label = "Ngày cấp", GroupCode = "group_legal", GroupLabel = "IV. Hồ sơ đơn – văn bằng – pháp lý", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "expiryDate", Label = "Ngày hết hạn", GroupCode = "group_legal", GroupLabel = "IV. Hồ sơ đơn – văn bằng – pháp lý", OrderIndex = 5 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "applicationReviewStatus", Label = "Tình trạng xét duyệt", GroupCode = "group_legal", GroupLabel = "IV. Hồ sơ đơn – văn bằng – pháp lý", OrderIndex = 6 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "processingDeadline", Label = "Hạn xử lý", GroupCode = "group_legal", GroupLabel = "IV. Hồ sơ đơn – văn bằng – pháp lý", OrderIndex = 7 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "renewalCycle", Label = "Chu kỳ gia hạn", GroupCode = "group_renewal", GroupLabel = "V. Gia hạn & nhắc việc", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "renewalDate", Label = "Ngày cần gia hạn", GroupCode = "group_renewal", GroupLabel = "V. Gia hạn & nhắc việc", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "reminderDate", Label = "Ngày nhắc (trước 3 tháng)", GroupCode = "group_renewal", GroupLabel = "V. Gia hạn & nhắc việc", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "reminderStatus", Label = "Trạng thái nhắc", GroupCode = "group_renewal", GroupLabel = "V. Gia hạn & nhắc việc", OrderIndex = 4 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "consultingStatus", Label = "Tình trạng tư vấn", GroupCode = "group_contract", GroupLabel = "VI. Hợp đồng – tài chính", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "contractStatus", Label = "Tình trạng hợp đồng", GroupCode = "group_contract", GroupLabel = "VI. Hợp đồng – tài chính", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "contractNumber", Label = "Số hợp đồng", GroupCode = "group_contract", GroupLabel = "VI. Hợp đồng – tài chính", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "contractValue", Label = "Giá trị hợp đồng", GroupCode = "group_contract", GroupLabel = "VI. Hợp đồng – tài chính", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "stateFee", Label = "Lệ phí NN", GroupCode = "group_contract", GroupLabel = "VI. Hợp đồng – tài chính", OrderIndex = 5 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "additionalFee", Label = "Phí phát sinh", GroupCode = "group_contract", GroupLabel = "VI. Hợp đồng – tài chính", OrderIndex = 6 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "createdBy", Label = "Người tạo", GroupCode = "group_system", GroupLabel = "VII. Hệ thống – kiểm soát", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "updatedBy", Label = "Người cập nhật", GroupCode = "group_system", GroupLabel = "VII. Hệ thống – kiểm soát", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "updatedAt", Label = "Ngày cập nhật", GroupCode = "group_system", GroupLabel = "VII. Hệ thống – kiểm soát", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", Code = "documentLink", Label = "Link hồ sơ giấy tờ", GroupCode = "group_system", GroupLabel = "VII. Hệ thống – kiểm soát", OrderIndex = 4 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "export", Code = "export_doc", Label = "Xuất văn bản", GroupCode = "group_export", GroupLabel = "VIII. Xuất văn bản", OrderIndex = 1 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "requester", Label = "Người đề nghị", GroupCode = "group_request", GroupLabel = "I. Nhóm thông tin đề nghị – hành chính", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "department", Label = "Phòng ban", GroupCode = "group_request", GroupLabel = "I. Nhóm thông tin đề nghị – hành chính", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "priority", Label = "Ưu tiên", GroupCode = "group_request", GroupLabel = "I. Nhóm thông tin đề nghị – hành chính", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "requestDate", Label = "Ngày phát sinh giao dịch", GroupCode = "group_request", GroupLabel = "I. Nhóm thông tin đề nghị – hành chính", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "projectCode", Label = "Mã dự án", GroupCode = "group_request", GroupLabel = "I. Nhóm thông tin đề nghị – hành chính", OrderIndex = 5 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "content", Label = "Nội dung", GroupCode = "group_content", GroupLabel = "II. Nhóm nội dung chi phí", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "description", Label = "Diễn giải", GroupCode = "group_content", GroupLabel = "II. Nhóm nội dung chi phí", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "transactionType", Label = "Loại giao dịch", GroupCode = "group_content", GroupLabel = "II. Nhóm nội dung chi phí", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "voucherType", Label = "Loại chứng từ", GroupCode = "group_content", GroupLabel = "II. Nhóm nội dung chi phí", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "transactionObject", Label = "Đối tượng Thu/Chi", GroupCode = "group_content", GroupLabel = "II. Nhóm nội dung chi phí", OrderIndex = 5 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "note", Label = "Ghi chú", GroupCode = "group_content", GroupLabel = "II. Nhóm nội dung chi phí", OrderIndex = 6 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "amountBeforeTax", Label = "Số tiền (Chưa thuế)", GroupCode = "group_finance", GroupLabel = "III. Nhóm tiền – thuế", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "taxRate", Label = "Thuế suất", GroupCode = "group_finance", GroupLabel = "III. Nhóm tiền – thuế", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "totalAmount", Label = "Tổng tiền", GroupCode = "group_finance", GroupLabel = "III. Nhóm tiền – thuế", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "taxCode", Label = "Mã số thuế", GroupCode = "group_finance", GroupLabel = "III. Nhóm tiền – thuế", OrderIndex = 4 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "voucherNumber", Label = "Số hóa đơn / Chứng từ / Hợp đồng", GroupCode = "group_voucher", GroupLabel = "IV. Nhóm hóa đơn – chứng từ", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "voucherDate", Label = "Ngày hóa đơn / Chứng từ / Hợp đồng", GroupCode = "group_voucher", GroupLabel = "IV. Nhóm hóa đơn – chứng từ", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "attachment", Label = "File đính kèm", GroupCode = "group_voucher", GroupLabel = "IV. Nhóm hóa đơn – chứng từ", OrderIndex = 3 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "paymentMethod", Label = "Phương thức thanh toán", GroupCode = "group_payment", GroupLabel = "V. Nhóm thanh toán", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "accountNumber", Label = "Tài khoản", GroupCode = "group_payment", GroupLabel = "V. Nhóm thanh toán", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "bank", Label = "Ngân hàng", GroupCode = "group_payment", GroupLabel = "V. Nhóm thanh toán", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "paymentStatus", Label = "Trạng thái thanh toán", GroupCode = "group_payment", GroupLabel = "V. Nhóm thanh toán", OrderIndex = 4 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "managerApproval", Label = "Quản lý duyệt", GroupCode = "group_control", GroupLabel = "VI. Nhóm phê duyệt – kiểm soát", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "directorApproval", Label = "Giám đốc duyệt", GroupCode = "group_control", GroupLabel = "VI. Nhóm phê duyệt – kiểm soát", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "accountantReview", Label = "Kế toán review", GroupCode = "group_control", GroupLabel = "VI. Nhóm phê duyệt – kiểm soát", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "adjustReason", Label = "Lý do điều chỉnh", GroupCode = "group_control", GroupLabel = "VI. Nhóm phê duyệt – kiểm soát", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "rejectionReason", Label = "Lý do từ chối", GroupCode = "group_control", GroupLabel = "VI. Nhóm phê duyệt – kiểm soát", OrderIndex = 5 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", Code = "riskFlag", Label = "Cờ kiểm soát rủi ro", GroupCode = "group_control", GroupLabel = "VI. Nhóm phê duyệt – kiểm soát", OrderIndex = 6 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", Code = "overview", Label = "Tổng quan", GroupCode = "group_dashboard", GroupLabel = "I. Dashboard", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", Code = "kpi", Label = "Chỉ số KPI", GroupCode = "group_dashboard", GroupLabel = "I. Dashboard", OrderIndex = 2 },

                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", Code = "list", Label = "Danh sách nhân viên", GroupCode = "group_users", GroupLabel = "I. Nhân viên", OrderIndex = 1 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", Code = "detail", Label = "Chi tiết nhân viên", GroupCode = "group_users", GroupLabel = "I. Nhân viên", OrderIndex = 2 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", Code = "create", Label = "Thêm nhân viên", GroupCode = "group_users", GroupLabel = "I. Nhân viên", OrderIndex = 3 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", Code = "update", Label = "Sửa nhân viên", GroupCode = "group_users", GroupLabel = "I. Nhân viên", OrderIndex = 4 },
                new FieldDef { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", Code = "delete", Label = "Xóa nhân viên", GroupCode = "group_users", GroupLabel = "I. Nhân viên", OrderIndex = 5 }
            };

            await fieldsCollection.InsertManyAsync(fieldDefs, cancellationToken: cancellationToken);
        }

        if (await fieldPermissionsCollection.CountDocumentsAsync(_ => true, cancellationToken: cancellationToken) == 0)
        {
            var fieldPermissions = new List<FieldPermission>();

            void AddQlkhFieldPermissions(
                string fieldCode,
                string marketingSales,
                string ipExecutive,
                string ipManager,
                string accountant,
                string director,
                string ceo,
                string admin)
            {
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", FieldCode = fieldCode, RoleCode = "marketing_sales", PermissionLevel = marketingSales });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", FieldCode = fieldCode, RoleCode = "ip_executive", PermissionLevel = ipExecutive });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", FieldCode = fieldCode, RoleCode = "ip_manager", PermissionLevel = ipManager });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", FieldCode = fieldCode, RoleCode = "accountant", PermissionLevel = accountant });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", FieldCode = fieldCode, RoleCode = "director", PermissionLevel = director });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", FieldCode = fieldCode, RoleCode = "ceo", PermissionLevel = ceo });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlkh", FieldCode = fieldCode, RoleCode = "admin", PermissionLevel = "A" });
            }

            string MapQlcpPermission(string value)
            {
                if (string.Equals(value, "CREATE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "UPDATE", StringComparison.OrdinalIgnoreCase))
                {
                    return "W";
                }

                if (string.Equals(value, "READ", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "SYSTEM", StringComparison.OrdinalIgnoreCase))
                {
                    return "R";
                }

                if (string.Equals(value, "APPROVE", StringComparison.OrdinalIgnoreCase))
                {
                    return "A";
                }

                if (string.Equals(value, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    return "N";
                }

                return "N";
            }

            void AddQlcpFieldPermissions(
                string fieldCode,
                string nhanVien,
                string quanLy,
                string keToan,
                string giamDoc,
                string tongGiamDoc)
            {
                var nv = MapQlcpPermission(nhanVien);
                var ql = MapQlcpPermission(quanLy);
                var kt = MapQlcpPermission(keToan);
                var gd = MapQlcpPermission(giamDoc);
                var ceo = MapQlcpPermission(tongGiamDoc);

                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", FieldCode = fieldCode, RoleCode = "marketing_sales", PermissionLevel = nv });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", FieldCode = fieldCode, RoleCode = "ip_executive", PermissionLevel = nv });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", FieldCode = fieldCode, RoleCode = "ip_manager", PermissionLevel = ql });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", FieldCode = fieldCode, RoleCode = "accountant", PermissionLevel = kt });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", FieldCode = fieldCode, RoleCode = "director", PermissionLevel = gd });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", FieldCode = fieldCode, RoleCode = "ceo", PermissionLevel = ceo });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "qlcp", FieldCode = fieldCode, RoleCode = "admin", PermissionLevel = "A" });
            }

            void AddDashboardPermissions(string fieldCode)
            {
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", FieldCode = fieldCode, RoleCode = "marketing_sales", PermissionLevel = "R" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", FieldCode = fieldCode, RoleCode = "ip_executive", PermissionLevel = "R" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", FieldCode = fieldCode, RoleCode = "ip_manager", PermissionLevel = "R" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", FieldCode = fieldCode, RoleCode = "accountant", PermissionLevel = "R" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", FieldCode = fieldCode, RoleCode = "director", PermissionLevel = "A" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", FieldCode = fieldCode, RoleCode = "ceo", PermissionLevel = "A" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "dashboard", FieldCode = fieldCode, RoleCode = "admin", PermissionLevel = "A" });
            }

            void AddUsersPermissions(string fieldCode)
            {
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", FieldCode = fieldCode, RoleCode = "marketing_sales", PermissionLevel = "N" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", FieldCode = fieldCode, RoleCode = "ip_executive", PermissionLevel = "N" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", FieldCode = fieldCode, RoleCode = "ip_manager", PermissionLevel = "R" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", FieldCode = fieldCode, RoleCode = "accountant", PermissionLevel = "N" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", FieldCode = fieldCode, RoleCode = "director", PermissionLevel = "R" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", FieldCode = fieldCode, RoleCode = "ceo", PermissionLevel = "R" });
                fieldPermissions.Add(new FieldPermission { Id = ObjectId.GenerateNewId().ToString(), ModuleCode = "users", FieldCode = fieldCode, RoleCode = "admin", PermissionLevel = "A" });
            }

            AddQlkhFieldPermissions("name", "W", "W", "W", "R", "A", "A", "W");
            AddQlkhFieldPermissions("businessScale", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("taxCode", "W", "W", "W", "R", "A", "A", "W");
            AddQlkhFieldPermissions("address", "W", "W", "W", "R", "A", "A", "W");
            AddQlkhFieldPermissions("representativeName", "W", "W", "W", "R", "A", "A", "W");
            AddQlkhFieldPermissions("representativePosition", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("idNumber", "N", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("phone", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("email", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("contactPerson", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("contactPhone", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("contactEmail", "W", "W", "W", "N", "A", "A", "W");

            AddQlkhFieldPermissions("businessNeeds", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("potentialLevel", "W", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("priority", "W", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("sourceClassification", "W", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("nsnnSource", "W", "W", "W", "R", "A", "A", "W");

            AddQlkhFieldPermissions("brandName", "R", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("productsServices", "R", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("ipGroup", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("owner", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("protectionTerritory", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("authorization", "N", "W", "A", "N", "A", "A", "W");

            AddQlkhFieldPermissions("filingStatus", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("filingDate", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("applicationCode", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("issueDate", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("expiryDate", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("applicationReviewStatus", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("processingDeadline", "N", "W", "A", "N", "A", "A", "W");

            AddQlkhFieldPermissions("renewalCycle", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("renewalDate", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("reminderDate", "N", "W", "A", "N", "A", "A", "W");
            AddQlkhFieldPermissions("reminderStatus", "N", "W", "A", "N", "A", "A", "W");

            AddQlkhFieldPermissions("consultingStatus", "R", "W", "W", "N", "A", "A", "W");
            AddQlkhFieldPermissions("contractStatus", "R", "W", "W", "R", "A", "A", "W");
            AddQlkhFieldPermissions("contractNumber", "N", "W", "W", "W", "A", "A", "W");
            AddQlkhFieldPermissions("contractValue", "N", "W", "W", "W", "A", "A", "W");
            AddQlkhFieldPermissions("stateFee", "N", "W", "W", "W", "A", "A", "W");
            AddQlkhFieldPermissions("additionalFee", "N", "W", "W", "W", "A", "A", "W");

            AddQlkhFieldPermissions("createdBy", "R", "R", "R", "R", "R", "R", "W");
            AddQlkhFieldPermissions("updatedBy", "R", "R", "R", "R", "R", "R", "W");
            AddQlkhFieldPermissions("updatedAt", "R", "R", "R", "R", "R", "R", "W");
            AddQlkhFieldPermissions("documentLink", "R", "W", "A", "R", "W", "W", "W");
            AddQlkhFieldPermissions("export_doc", "W", "W", "W", "W", "W", "W", "W");

            AddQlcpFieldPermissions("requester", "CREATE", "READ", "READ", "READ", "READ");
            AddQlcpFieldPermissions("department", "CREATE", "READ", "READ", "READ", "READ");
            AddQlcpFieldPermissions("priority", "CREATE", "UPDATE", "READ", "READ", "READ");
            AddQlcpFieldPermissions("requestDate", "CREATE", "READ", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("projectCode", "CREATE", "READ", "READ", "READ", "READ");

            AddQlcpFieldPermissions("content", "CREATE", "UPDATE", "READ", "READ", "READ");
            AddQlcpFieldPermissions("description", "CREATE", "UPDATE", "READ", "READ", "READ");
            AddQlcpFieldPermissions("transactionType", "CREATE", "UPDATE", "READ", "READ", "READ");
            AddQlcpFieldPermissions("voucherType", "CREATE", "UPDATE", "READ", "READ", "READ");
            AddQlcpFieldPermissions("transactionObject", "CREATE", "UPDATE", "READ", "READ", "READ");
            AddQlcpFieldPermissions("note", "CREATE", "UPDATE", "UPDATE", "READ", "READ");

            AddQlcpFieldPermissions("amountBeforeTax", "CREATE", "READ", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("taxRate", "CREATE", "READ", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("totalAmount", "SYSTEM", "SYSTEM", "SYSTEM", "READ", "READ");
            AddQlcpFieldPermissions("taxCode", "CREATE", "NONE", "UPDATE", "READ", "READ");

            AddQlcpFieldPermissions("voucherNumber", "CREATE", "READ", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("voucherDate", "CREATE", "READ", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("attachment", "CREATE", "READ", "UPDATE", "READ", "READ");

            AddQlcpFieldPermissions("paymentMethod", "CREATE", "NONE", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("accountNumber", "CREATE", "NONE", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("bank", "CREATE", "NONE", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("paymentStatus", "CREATE", "UPDATE", "UPDATE", "UPDATE", "READ");

            AddQlcpFieldPermissions("managerApproval", "READ", "UPDATE", "READ", "READ", "READ");
            AddQlcpFieldPermissions("directorApproval", "READ", "READ", "READ", "UPDATE", "READ");
            AddQlcpFieldPermissions("accountantReview", "READ", "READ", "UPDATE", "READ", "READ");
            AddQlcpFieldPermissions("adjustReason", "NONE", "CREATE", "UPDATE", "UPDATE", "READ");
            AddQlcpFieldPermissions("rejectionReason", "NONE", "CREATE", "NONE", "UPDATE", "READ");
            AddQlcpFieldPermissions("riskFlag", "NONE", "READ", "READ", "UPDATE", "UPDATE");

            string[] dashboardFieldCodes =
            {
                "overview",
                "kpi"
            };

            foreach (var code in dashboardFieldCodes)
            {
                AddDashboardPermissions(code);
            }

            string[] usersFieldCodes =
            {
                "list",
                "detail",
                "create",
                "update",
                "delete"
            };

            foreach (var code in usersFieldCodes)
            {
                AddUsersPermissions(code);
            }

            if (fieldPermissions.Count > 0)
            {
                await fieldPermissionsCollection.InsertManyAsync(fieldPermissions, cancellationToken: cancellationToken);
            }
        }

        // Force update permissions to ensure consistency (fix for Manager Approval issue)
        var fpCollection = db.GetCollection<FieldPermission>("field_permissions");
        
        // 1. Ensure IP Manager has Write permission on managerApproval
        var managerUpdate = Builders<FieldPermission>.Update.Set(p => p.PermissionLevel, "W");
        await fpCollection.UpdateOneAsync(
            p => p.ModuleCode == "qlcp" && p.FieldCode == "managerApproval" && p.RoleCode == "ip_manager",
            managerUpdate,
            new UpdateOptions { IsUpsert = true },
            cancellationToken
        );

        // 2. Ensure Director has Write permission on directorApproval
        var directorUpdate = Builders<FieldPermission>.Update.Set(p => p.PermissionLevel, "W");
        await fpCollection.UpdateOneAsync(
            p => p.ModuleCode == "qlcp" && p.FieldCode == "directorApproval" && p.RoleCode == "director",
            directorUpdate,
            new UpdateOptions { IsUpsert = true },
            cancellationToken
        );

        // 3. Ensure IP Manager has Write permission on paymentStatus
        var managerPaymentStatusUpdate = Builders<FieldPermission>.Update.Set(p => p.PermissionLevel, "W");
        await fpCollection.UpdateOneAsync(
            p => p.ModuleCode == "qlcp" && p.FieldCode == "paymentStatus" && p.RoleCode == "ip_manager",
            managerPaymentStatusUpdate,
            new UpdateOptions { IsUpsert = true },
            cancellationToken
        );

        // 4. Ensure Director has Write permission on paymentStatus
        var directorPaymentStatusUpdate = Builders<FieldPermission>.Update.Set(p => p.PermissionLevel, "W");
        await fpCollection.UpdateOneAsync(
            p => p.ModuleCode == "qlcp" && p.FieldCode == "paymentStatus" && p.RoleCode == "director",
            directorPaymentStatusUpdate,
            new UpdateOptions { IsUpsert = true },
            cancellationToken
        );

        // 5. Ensure export_doc field exists (Force update/upsert)
        // Always remove old qlkh field if it exists
        await fieldsCollection.DeleteOneAsync(f => f.ModuleCode == "qlkh" && f.Code == "export_doc", cancellationToken);
        
        // Upsert the export_doc field to ensure it exists and has correct properties
        var exportFieldDef = new FieldDef 
        { 
            // We need to be careful with ID if we are upserting. 
            // If we find one, use its ID. If not, generate new.
            // But since we want to enforce the definition, let's find it first.
            ModuleCode = "export", 
            Code = "export_doc", 
            Label = "Xuất văn bản", 
            GroupCode = "group_export", 
            GroupLabel = "VIII. Xuất văn bản", 
            OrderIndex = 1 
        };

        var existingExportField = await fieldsCollection.Find(f => f.ModuleCode == "export" && f.Code == "export_doc").FirstOrDefaultAsync(cancellationToken);
        if (existingExportField != null)
        {
            exportFieldDef.Id = existingExportField.Id;
            await fieldsCollection.ReplaceOneAsync(f => f.Id == existingExportField.Id, exportFieldDef, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
        else
        {
            exportFieldDef.Id = ObjectId.GenerateNewId().ToString();
            await fieldsCollection.InsertOneAsync(exportFieldDef, cancellationToken: cancellationToken);
        }

        // 6. Ensure export_doc permissions exist (A for all roles as per request)
        var rolesList = new[] { "marketing_sales", "ip_executive", "ip_manager", "accountant", "director", "ceo", "admin" };
        foreach (var role in rolesList)
        {
            var permLevel = "A";
            var permUpdate = Builders<FieldPermission>.Update.Set(p => p.PermissionLevel, permLevel);
            await fpCollection.UpdateOneAsync(
                p => p.ModuleCode == "export" && p.FieldCode == "export_doc" && p.RoleCode == role,
                permUpdate,
                new UpdateOptions { IsUpsert = true },
                cancellationToken
            );
        }

        Console.WriteLine("--- CHECKING 2026 DATA ---");

        // 7. Supplement Data for 2026 (Customers and Costs) to make charts look full
        // Check if we have enough customers in 2026
        var count2026Customers = await customersCollection.CountDocumentsAsync(
            c => c.JoinDate != null && c.JoinDate.Contains("2026"),
            cancellationToken: cancellationToken);
        
        Console.WriteLine($"Found {count2026Customers} customers in 2026");

        if (count2026Customers < 20)
        {
            Console.WriteLine("Seeding more customers for 2026...");
            var extraCustomers = new List<Customer>();
            var random = new Random();
            for (int month = 1; month <= 12; month++)
            {
                // Add 2-3 customers per month
                int count = random.Next(2, 4); 
                for (int i = 0; i < count; i++)
                {
                    string joinDate = $"2026-{month:00}-{random.Next(1, 28):00}";
                    string status = random.Next(0, 2) == 0 ? "Đã tư vấn" : "Mới tiếp nhận"; // Mix for Green/Blue bars
                    if (random.Next(0, 3) == 0) status = "Đang tư vấn"; // More mix

                    extraCustomers.Add(new Customer
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        LegacyId = 1000 + (month * 100) + i,
                        Name = $"KH 2026 Tháng {month} #{i+1}",
                        Email = $"kh2026_{month}_{i}@example.com",
                        Phone = $"09{month:00}{i:00}0000",
                        Address = $"Địa chỉ Tháng {month}",
                        Company = $"Công ty T{month}-{i}",
                        TaxCode = $"MST2026{month}{i}",
                        RepresentativeName = $"Đại diện {month}-{i}",
                        RepresentativePosition = "Giám đốc",
                        RepresentativePhone = "",
                        BusinessNeeds = "Nhu cầu 2026",
                        BusinessScale = "Vừa",
                        BusinessIndustry = "Dịch vụ",
                        CopyrightStatus = "",
                        TrademarkStatus = "",
                        PatentStatus = "",
                        IndustrialDesign = "",
                        ContractStatus = "Mới",
                        Status = "active",
                        TotalOrders = 1,
                        TotalRevenue = 10000000m,
                        JoinDate = joinDate,
                        Notes = "Auto-generated for chart",
                        ProductsServices = "Dịch vụ",
                        IpGroup = "Nhóm 1",
                        ConsultingStatus = status,
                        FilingStatus = "",
                        DocumentLink = "",
                        Authorization = "Chưa có",
                        ApplicationReviewStatus = "",
                        Priority = "Mức 1",
                        ContractPaid = "0%",
                        ContractValue = 20000000m,
                        StartDate = joinDate,
                        EndDate = "",
                        ImplementationDays = 0,
                        PotentialLevel = "Cao",
                        SourceClassification = "Marketing",
                        NsnnSource = ""
                    });
                }
            }
            if (extraCustomers.Any())
            {
                await customersCollection.InsertManyAsync(extraCustomers, cancellationToken: cancellationToken);
                Console.WriteLine($"Inserted {extraCustomers.Count} customers for 2026");
            }
        }

        // Check if we have enough costs in 2026
        var count2026Costs = await costsCollection.CountDocumentsAsync(
             c => c.VoucherDate != null && c.VoucherDate.Contains("2026"),
             cancellationToken: cancellationToken);
        
        Console.WriteLine($"Found {count2026Costs} costs in 2026");

        if (count2026Costs < 15)
        {
            Console.WriteLine("Seeding more costs for 2026...");
            var extraCosts = new List<Cost>();
            var random = new Random();
            
            // Add specifically for Jan 2026 to fill the Pie Chart
            string[] projectsJan = { "PJ2026-001", "PJ2026-002", "PJ2026-003", "PJ2026-004", "PJ2026-005" };
            foreach (var proj in projectsJan)
            {
                extraCosts.Add(new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 2000 + random.Next(1, 10000),
                    Requester = "Auto Generator",
                    Department = "Kinh doanh",
                    RequestDate = $"2026-01-{random.Next(5, 25):00}",
                    ProjectCode = proj,
                    TransactionType = "Chi",
                    TransactionObject = "NCC " + proj,
                    Content = "Chi phí " + proj,
                    Description = "Auto generated cost",
                    AmountBeforeTax = random.Next(1, 10) * 1000000m,
                    TaxRate = "10%",
                    TotalAmount = random.Next(1, 10) * 1100000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "MB",
                    AccountNumber = "0000",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = "HD" + proj + random.Next(1,100),
                    VoucherDate = $"2026-01-{random.Next(5, 25):00}",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán", // Must be paid to show up
                    RejectionReason = "",
                    Note = ""
                });
            }

            // Add for other months
            for (int month = 2; month <= 12; month++)
            {
                 extraCosts.Add(new Cost
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    LegacyId = 2000 + (month * 100) + random.Next(1, 99),
                    Requester = "Auto Generator",
                    Department = "Kinh doanh",
                    RequestDate = $"2026-{month:00}-15",
                    ProjectCode = $"PJ2026-{month:00}",
                    TransactionType = "Chi",
                    TransactionObject = "NCC T" + month,
                    Content = "Chi phí T" + month,
                    Description = "Auto generated cost",
                    AmountBeforeTax = 5000000m,
                    TaxRate = "10%",
                    TotalAmount = 5500000m,
                    PaymentMethod = "Chuyển khoản",
                    Bank = "MB",
                    AccountNumber = "0000",
                    VoucherType = "Hóa đơn",
                    VoucherNumber = $"HD-T{month}-{random.Next(1,100)}",
                    VoucherDate = $"2026-{month:00}-15",
                    Attachment = "",
                    PaymentStatus = "Đã thanh toán",
                    RejectionReason = "",
                    Note = ""
                });
            }

            if (extraCosts.Any())
            {
                await costsCollection.InsertManyAsync(extraCosts, cancellationToken: cancellationToken);
                Console.WriteLine($"Inserted {extraCosts.Count} costs for 2026");
            }
        }

        // ---------------------------------------------------------
        // Ensure Scheduling Module Fields and Permissions Exist
        // ---------------------------------------------------------
        
        // 1. Ensure Scheduling Fields
        var schedulingFields = new List<FieldDef>
        {
            new FieldDef { ModuleCode = "scheduling", Code = "seedData", Label = "Nạp dữ liệu mẫu", GroupCode = "actions", GroupLabel = "Chức năng", OrderIndex = 1 },
            new FieldDef { ModuleCode = "scheduling", Code = "generate", Label = "Xếp lịch", GroupCode = "actions", GroupLabel = "Chức năng", OrderIndex = 2 },
            new FieldDef { ModuleCode = "scheduling", Code = "export", Label = "Xuất Excel", GroupCode = "actions", GroupLabel = "Chức năng", OrderIndex = 3 },
            new FieldDef { ModuleCode = "scheduling", Code = "config", Label = "Cấu hình tham số", GroupCode = "config", GroupLabel = "Cấu hình", OrderIndex = 4 },
            new FieldDef { ModuleCode = "scheduling", Code = "view", Label = "Xem kết quả", GroupCode = "view", GroupLabel = "Xem", OrderIndex = 5 }
        };

        foreach (var field in schedulingFields)
        {
            var existingField = await fieldsCollection.Find(f => f.ModuleCode == "scheduling" && f.Code == field.Code).FirstOrDefaultAsync(cancellationToken);
            if (existingField == null)
            {
                field.Id = ObjectId.GenerateNewId().ToString();
                await fieldsCollection.InsertOneAsync(field, cancellationToken: cancellationToken);
            }
        }

        // 2. Ensure Scheduling Permissions
        var schedulingFieldCodes = new[] { "seedData", "generate", "export", "config", "view" };
        var allRoles = new[] { "marketing_sales", "ip_executive", "ip_manager", "accountant", "director", "ceo", "admin" };
        
        foreach (var fieldCode in schedulingFieldCodes)
        {
             foreach (var roleCode in allRoles)
             {
                 // Define permission level based on rules
                 string level = "N"; // Default
                 
                 if (roleCode == "admin" || roleCode == "ceo") level = "W";
                 else if (roleCode == "director" || roleCode == "ip_manager") level = "W";
                 else if (roleCode == "ip_executive")
                 {
                     if (fieldCode == "export" || fieldCode == "view") level = "W";
                     else level = "R";
                 }
                 else if (roleCode == "marketing_sales" || roleCode == "accountant") level = "N";

                 var permUpdate = Builders<FieldPermission>.Update.Set(p => p.PermissionLevel, level);
                 
                 // Upsert permission
                 await fpCollection.UpdateOneAsync(
                     p => p.ModuleCode == "scheduling" && p.FieldCode == fieldCode && p.RoleCode == roleCode,
                     permUpdate,
                     new UpdateOptions { IsUpsert = true },
                     cancellationToken
                 );
             }
        }

        // Ensure manager email is updated (Fix for existing data)
        var managerUser = await usersCollection.Find(u => u.Username == "manager").FirstOrDefaultAsync(cancellationToken);
        if (managerUser != null && managerUser.Email != "tuanvb96@gmail.com")
        {
            var update = Builders<User>.Update.Set(u => u.Email, "tuanvb96@gmail.com");
            await usersCollection.UpdateOneAsync(u => u.Id == managerUser.Id, update, cancellationToken: cancellationToken);
        }
    }

    private static string HashPassword(string password)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = System.Security.Cryptography.SHA256.HashData(passwordBytes);
        return Convert.ToHexString(hashBytes);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
