CREATE TABLE Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Phone NVARCHAR(20),
    Password NVARCHAR(255) NOT NULL,
    Address NVARCHAR(255),
    DateRegistered DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(20) DEFAULT 'Active'
);

CREATE TABLE Staff (
    StaffID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Phone NVARCHAR(20),
    Password NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) DEFAULT 'Restaurant Staff',
    Status NVARCHAR(20) DEFAULT 'Active'
);

CREATE TABLE Admin (
    AdminID INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) DEFAULT 'Admin'
);

CREATE TABLE MenuCategory (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255)
);

CREATE TABLE RestaurantTable (
    TableID INT IDENTITY(1,1) PRIMARY KEY,
    TableNumber NVARCHAR(20) NOT NULL UNIQUE,
    Capacity INT NOT NULL,
    Status NVARCHAR(30) DEFAULT 'Available',
    Location NVARCHAR(100)
);

CREATE TABLE Reservation (
    ReservationID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    TableID INT NOT NULL,
    Date DATE NOT NULL,
    Time TIME NOT NULL,
    Status NVARCHAR(30) DEFAULT 'Pending',
    CreatedAt DATETIME DEFAULT GETDATE(),

    CONSTRAINT FK_Reservation_Customer
        FOREIGN KEY (CustomerID)
        REFERENCES Customer(CustomerID),

    CONSTRAINT FK_Reservation_Table
        FOREIGN KEY (TableID)
        REFERENCES RestaurantTable(TableID)
);

CREATE TABLE MenuItem (
    MenuItemID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID INT NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(255),
    Price DECIMAL(10,2) NOT NULL,
    Availability BIT DEFAULT 1,
    ImageURL NVARCHAR(255),

    CONSTRAINT FK_MenuItem_Category
        FOREIGN KEY (CategoryID)
        REFERENCES MenuCategory(CategoryID)
);

CREATE TABLE CustomerOrder (
    OrderID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    StaffID INT NULL,
    ReservationID INT NULL,
    OrderDate DATETIME DEFAULT GETDATE(),
    Status NVARCHAR(30) DEFAULT 'Pending',
    TotalAmount DECIMAL(10,2) DEFAULT 0,

    CONSTRAINT FK_Order_Customer
        FOREIGN KEY (CustomerID)
        REFERENCES Customer(CustomerID),

    CONSTRAINT FK_Order_Staff
        FOREIGN KEY (StaffID)
        REFERENCES Staff(StaffID),

    CONSTRAINT FK_Order_Reservation
        FOREIGN KEY (ReservationID)
        REFERENCES Reservation(ReservationID)
);

CREATE TABLE OrderItem (
    OrderItemID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    MenuItemID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    Subtotal DECIMAL(10,2) NOT NULL,

    CONSTRAINT FK_OrderItem_Order
        FOREIGN KEY (OrderID)
        REFERENCES CustomerOrder(OrderID),

    CONSTRAINT FK_OrderItem_MenuItem
        FOREIGN KEY (MenuItemID)
        REFERENCES MenuItem(MenuItemID)
);

CREATE TABLE Payment (
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,
    OrderID INT NOT NULL,
    PaymentDate DATETIME DEFAULT GETDATE(),
    Amount DECIMAL(10,2) NOT NULL,
    Method NVARCHAR(50),
    Status NVARCHAR(30) DEFAULT 'Pending',
    TransactionNo NVARCHAR(100),

    CONSTRAINT FK_Payment_Order
        FOREIGN KEY (OrderID)
        REFERENCES CustomerOrder(OrderID)
);

CREATE TABLE Notification (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    UserType NVARCHAR(20) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    IsRead BIT DEFAULT 0
);