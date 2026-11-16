-- ============================================================================
-- MVP SHIPPER APP - DATABASE SCHEMA SCRIPT (SQL SERVER)
-- Bao gồm: Users, Drivers, Orders, OrderStatusLogs + Indexes + Constraints + Trigger
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ShipperMvpDb')
BEGIN
    CREATE DATABASE ShipperMvpDb;
END
GO

USE ShipperMvpDb;
GO

-- ============================================================================
-- 1. BẢNG USERS (Tài khoản dùng chung cho Customer và Shipper)
-- ============================================================================
CREATE TABLE dbo.Users (
    Id              INT IDENTITY(1,1)   NOT NULL,
    FullName        NVARCHAR(100)       NOT NULL,
    PhoneNumber     VARCHAR(15)         NOT NULL,
    PasswordHash    NVARCHAR(255)       NOT NULL,
    Role            VARCHAR(20)         NOT NULL, -- 'Customer' | 'Shipper'
    IsActive        BIT                 NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedAt       DATETIME2(3)        NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(3)        NULL,

    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Users_PhoneNumber UNIQUE (PhoneNumber),
    CONSTRAINT CK_Users_Role CHECK (Role IN ('Customer', 'Shipper')),
    CONSTRAINT CK_Users_PhoneNumber_NotEmpty CHECK (LEN(LTRIM(RTRIM(PhoneNumber))) >= 9)
);
GO

-- ============================================================================
-- 2. BẢNG DRIVERS (Thông tin mở rộng 1-1 dành riêng cho Shipper)
-- ============================================================================
CREATE TABLE dbo.Drivers (
    Id              INT IDENTITY(1,1)   NOT NULL,
    UserId          INT                 NOT NULL,
    LicensePlate    VARCHAR(20)         NOT NULL,
    VehicleType     NVARCHAR(50)        NOT NULL CONSTRAINT DF_Drivers_VehicleType DEFAULT (N'Xe máy'),
    IsOnline        BIT                 NOT NULL CONSTRAINT DF_Drivers_IsOnline DEFAULT (0),
    CreatedAt       DATETIME2(3)        NOT NULL CONSTRAINT DF_Drivers_CreatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Drivers PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Drivers_UserId UNIQUE (UserId),
    CONSTRAINT FK_Drivers_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Drivers_VehicleType CHECK (VehicleType IN (N'Xe máy', N'Xe ba gác', N'Xe tải nhỏ'))
);
GO

CREATE NONCLUSTERED INDEX IX_Drivers_IsOnline
    ON dbo.Drivers (IsOnline)
    INCLUDE (UserId, VehicleType);
GO

-- ============================================================================
-- 3. BẢNG ORDERS (Thông tin đơn hàng & Ràng buộc dữ liệu cốt lõi)
-- Status Mapping:
--   0 = Pending    (Đang tìm Shipper)
--   1 = Accepted   (Đã có Shipper nhận)
--   2 = PickedUp   (Đã lấy hàng)
--   3 = Delivering (Đang giao hàng)
--   4 = Completed  (Giao thành công)
--   5 = Cancelled  (Đã hủy)
-- ============================================================================
CREATE TABLE dbo.Orders (
    Id              INT IDENTITY(1,1)   NOT NULL,
    OrderCode       VARCHAR(20)         NOT NULL, -- VD: ORD20250001
    CustomerId      INT                 NOT NULL,
    DriverId        INT                 NULL,     -- NULL khi đang Pending
    PickupAddress   NVARCHAR(255)       NOT NULL,
    PickupLatitude  DECIMAL(9,6)        NULL,
    PickupLongitude DECIMAL(9,6)        NULL,
    PickupPhone     VARCHAR(15)         NOT NULL,
    DeliveryAddress NVARCHAR(255)       NOT NULL,
    DeliveryLatitude DECIMAL(9,6)       NULL,
    DeliveryLongitude DECIMAL(9,6)      NULL,
    ReceiverName    NVARCHAR(100)       NOT NULL,
    ReceiverPhone   VARCHAR(15)         NOT NULL,
    ItemDescription NVARCHAR(500)       NOT NULL,
    ShippingFee     DECIMAL(18, 0)      NOT NULL,
    CodAmount       DECIMAL(18, 0)      NOT NULL CONSTRAINT DF_Orders_CodAmount DEFAULT (0),
    Status          TINYINT             NOT NULL CONSTRAINT DF_Orders_Status DEFAULT (0),
    RowVersion      ROWVERSION          NOT NULL, -- Dùng để chống 2 Shipper nhận trùng 1 đơn
    CreatedAt       DATETIME2(3)        NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(3)        NOT NULL CONSTRAINT DF_Orders_UpdatedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_Orders PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Orders_OrderCode UNIQUE (OrderCode),
    CONSTRAINT FK_Orders_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Orders_Driver FOREIGN KEY (DriverId) REFERENCES dbo.Drivers(Id) ON DELETE NO ACTION,

    -- Ràng buộc miền giá trị
    CONSTRAINT CK_Orders_ShippingFee CHECK (ShippingFee >= 0),
    CONSTRAINT CK_Orders_CodAmount CHECK (CodAmount >= 0),
    CONSTRAINT CK_Orders_Status CHECK (Status BETWEEN 0 AND 5),

    -- Ràng buộc tính nhất quán giữa Status và DriverId:
    -- + Pending (0) bắt buộc chưa có DriverId
    -- + Accepted (1), PickedUp (2), Delivering (3), Completed (4) bắt buộc phải có DriverId
    -- + Cancelled (5) có thể có hoặc không (theo MVP chỉ hủy ở Pending nên DriverId IS NULL)
    CONSTRAINT CK_Orders_Status_DriverConsistency CHECK (
        (Status = 0 AND DriverId IS NULL) OR
        (Status IN (1, 2, 3, 4) AND DriverId IS NOT NULL) OR
        (Status = 5 AND DriverId IS NULL)
    )
);
GO

-- Index phục vụ "Chợ đơn hàng" cho Shipper (Lọc Status = 0 và sắp xếp mới nhất)
CREATE NONCLUSTERED INDEX IX_Orders_Status_CreatedAt
    ON dbo.Orders (Status, CreatedAt DESC)
    INCLUDE (OrderCode, PickupAddress, DeliveryAddress, ShippingFee, CodAmount);
GO

-- Index phục vụ Customer xem "Đơn hàng của tôi"
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId_CreatedAt
    ON dbo.Orders (CustomerId, CreatedAt DESC)
    INCLUDE (OrderCode, Status, ShippingFee, DriverId);
GO

-- Filtered Index phục vụ Shipper xem "Đơn đang chạy & Lịch sử"
CREATE NONCLUSTERED INDEX IX_Orders_DriverId_Status
    ON dbo.Orders (DriverId, Status, UpdatedAt DESC)
    WHERE DriverId IS NOT NULL;
GO

-- ============================================================================
-- 4. BẢNG ORDER STATUS LOGS (Lịch sử thay đổi trạng thái đơn hàng)
-- ============================================================================
CREATE TABLE dbo.Reviews (
    Id          INT IDENTITY(1,1) NOT NULL,
    OrderId     INT NOT NULL,
    CustomerId  INT NOT NULL,
    DriverId    INT NOT NULL,
    Rating      INT NOT NULL,
    Comment     NVARCHAR(1000) NULL,
    CreatedAt   DATETIME2(3) NOT NULL CONSTRAINT DF_Reviews_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Reviews PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Reviews_OrderId UNIQUE (OrderId),
    CONSTRAINT FK_Reviews_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Reviews_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Reviews_Drivers FOREIGN KEY (DriverId) REFERENCES dbo.Drivers(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
);
GO
CREATE NONCLUSTERED INDEX IX_Reviews_DriverId_CreatedAt ON dbo.Reviews (DriverId, CreatedAt DESC);
GO

CREATE TABLE dbo.OrderStatusLogs (
    Id              BIGINT IDENTITY(1,1) NOT NULL,
    OrderId         INT                  NOT NULL,
    FromStatus      TINYINT              NULL,     -- NULL khi đơn mới được khởi tạo
    ToStatus        TINYINT              NOT NULL,
    ChangedByUserId INT                  NOT NULL,
    Note            NVARCHAR(255)        NULL,
    ChangedAt       DATETIME2(3)         NOT NULL CONSTRAINT DF_OrderStatusLogs_ChangedAt DEFAULT (SYSUTCDATETIME()),

    CONSTRAINT PK_OrderStatusLogs PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_OrderStatusLogs_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderStatusLogs_Users FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION,

    CONSTRAINT CK_OrderStatusLogs_FromStatus CHECK (FromStatus IS NULL OR (FromStatus BETWEEN 0 AND 5)),
    CONSTRAINT CK_OrderStatusLogs_ToStatus CHECK (ToStatus BETWEEN 0 AND 5),
    CONSTRAINT CK_OrderStatusLogs_NoSelfTransition CHECK (FromStatus IS NULL OR FromStatus <> ToStatus)
);
GO

-- Index phục vụ xem Timeline lịch sử của 1 đơn hàng
CREATE NONCLUSTERED INDEX IX_OrderStatusLogs_OrderId_ChangedAt
    ON dbo.OrderStatusLogs (OrderId, ChangedAt ASC)
    INCLUDE (FromStatus, ToStatus, ChangedByUserId, Note);
GO

-- ============================================================================
-- 5. TRIGGER CHẶN NHẢY CÓC TRẠNG THÁI (STATE MACHINE GUARD Ở TẦNG DATABASE)
-- ============================================================================
CREATE TRIGGER dbo.TR_Orders_ValidateStateTransition
ON dbo.Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Chỉ kiểm tra nếu cột Status có sự thay đổi
    IF NOT UPDATE(Status)
        RETURN;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN deleted d ON i.Id = d.Id
        WHERE i.Status <> d.Status
          AND NOT (
              (d.Status = 0 AND i.Status IN (1, 5)) OR -- Pending -> Accepted hoặc Cancelled
              (d.Status = 1 AND i.Status = 2)       OR -- Accepted -> PickedUp
              (d.Status = 2 AND i.Status = 3)       OR -- PickedUp -> Delivering
              (d.Status = 3 AND i.Status = 4)          -- Delivering -> Completed
          )
    )
    BEGIN
        RAISERROR (N'Lỗi nghiệp vụ: Chuyển đổi trạng thái đơn hàng không hợp lệ (Cấm nhảy cóc hoặc đi lùi trạng thái).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO
