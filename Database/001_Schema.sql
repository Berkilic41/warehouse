USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'WarehouseDb')
    CREATE DATABASE WarehouseDb;
GO

USE WarehouseDb;
GO

CREATE TABLE Users (
    Id           INT           IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    PasswordSalt NVARCHAR(512) NOT NULL,
    Role         NVARCHAR(20)  NOT NULL DEFAULT 'Viewer',
    DisplayName  NVARCHAR(100),
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_Email    UNIQUE (Email),
    CONSTRAINT CK_Users_Role     CHECK (Role IN ('Admin','Staff','Viewer'))
);

CREATE TABLE Categories (
    Id          INT           IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
);

CREATE TABLE Suppliers (
    Id          INT           IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(150) NOT NULL,
    ContactName NVARCHAR(150),
    Email       NVARCHAR(150),
    Phone       NVARCHAR(50),
    Address     NVARCHAR(500),
    IsActive    BIT           NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE Products (
    Id                  INT            IDENTITY(1,1) PRIMARY KEY,
    SKU                 NVARCHAR(50)   NOT NULL,
    Name                NVARCHAR(200)  NOT NULL,
    Description         NVARCHAR(1000),
    CategoryId          INT            NOT NULL,
    Unit                NVARCHAR(20)   NOT NULL DEFAULT 'pcs',
    MinStockThreshold   INT            NOT NULL DEFAULT 0,
    CurrentStock        INT            NOT NULL DEFAULT 0,
    UnitPrice           DECIMAL(18,2),
    IsActive            BIT            NOT NULL DEFAULT 1,
    CreatedAt           DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    CONSTRAINT UQ_Products_SKU UNIQUE (SKU),
    CONSTRAINT CK_Products_Stock CHECK (CurrentStock >= 0),
    CONSTRAINT CK_Products_MinStock CHECK (MinStockThreshold >= 0)
);

CREATE TABLE SupplierProducts (
    SupplierId INT NOT NULL,
    ProductId  INT NOT NULL,
    CONSTRAINT PK_SupplierProducts PRIMARY KEY (SupplierId, ProductId),
    CONSTRAINT FK_SP_Supplier FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SP_Product  FOREIGN KEY (ProductId)  REFERENCES Products(Id)  ON DELETE CASCADE
);

CREATE TABLE StockMovements (
    Id           INT           IDENTITY(1,1) PRIMARY KEY,
    MovementType NVARCHAR(20)  NOT NULL,
    Reference    NVARCHAR(100),
    Reason       NVARCHAR(500),
    SupplierId   INT           NULL,
    UserId       INT           NOT NULL,
    MovementDate DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
    Notes        NVARCHAR(1000),
    CONSTRAINT FK_SM_Supplier FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id),
    CONSTRAINT FK_SM_User     FOREIGN KEY (UserId)     REFERENCES Users(Id),
    CONSTRAINT CK_SM_Type     CHECK (MovementType IN ('In','Out','Adjustment'))
);

CREATE INDEX IX_StockMovements_Date ON StockMovements(MovementDate DESC);

CREATE TABLE MovementItems (
    Id          INT            IDENTITY(1,1) PRIMARY KEY,
    MovementId  INT            NOT NULL,
    ProductId   INT            NOT NULL,
    Quantity    INT            NOT NULL,
    UnitPrice   DECIMAL(18,2),
    CONSTRAINT FK_MI_Movement FOREIGN KEY (MovementId) REFERENCES StockMovements(Id) ON DELETE CASCADE,
    CONSTRAINT FK_MI_Product  FOREIGN KEY (ProductId)  REFERENCES Products(Id),
    CONSTRAINT CK_MI_Quantity CHECK (Quantity <> 0)
);

CREATE INDEX IX_MovementItems_Product ON MovementItems(ProductId);
GO
