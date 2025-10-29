-- =============================================
-- SAFE CREATE DATABASE
-- =============================================
IF DB_ID('CloneEbayDb') IS NULL
BEGIN
    CREATE DATABASE CloneEbayDb;
END
GO

USE CloneEbayDb;
GO

-- =============================================
-- TABLES
-- =============================================

CREATE TABLE [User] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [username] NVARCHAR(100),
    [email] NVARCHAR(100) UNIQUE,
    [password] NVARCHAR(255),
    [role] NVARCHAR(20),
    [avatarURL] NVARCHAR(MAX)
);

CREATE TABLE [Address] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [userId] INT FOREIGN KEY REFERENCES [User](id),
    [fullName] NVARCHAR(100),
    [phone] NVARCHAR(20),
    [street] NVARCHAR(100),
    [city] NVARCHAR(50),
    [state] NVARCHAR(50),
    [country] NVARCHAR(50),
    [isDefault] BIT
);

CREATE TABLE [Category] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [name] NVARCHAR(100)
);

CREATE TABLE [Product] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [title] NVARCHAR(255),
    [description] NVARCHAR(MAX),
    [price] DECIMAL(10,2),
    [images] NVARCHAR(MAX),
    [categoryId] INT FOREIGN KEY REFERENCES [Category](id),
    [sellerId] INT FOREIGN KEY REFERENCES [User](id),
    [isAuction] BIT,
    [auctionEndTime] DATETIME
);

CREATE TABLE [OrderTable] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [buyerId] INT FOREIGN KEY REFERENCES [User](id),
    [addressId] INT FOREIGN KEY REFERENCES [Address](id),
    [orderDate] DATETIME,
    [totalPrice] DECIMAL(10,2),
    [status] NVARCHAR(20)
);

CREATE TABLE [OrderItem] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [orderId] INT FOREIGN KEY REFERENCES [OrderTable](id),
    [productId] INT FOREIGN KEY REFERENCES [Product](id),
    [quantity] INT,
    [unitPrice] DECIMAL(10,2)
);

CREATE TABLE [Payment] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [orderId] INT FOREIGN KEY REFERENCES [OrderTable](id),
    [userId] INT FOREIGN KEY REFERENCES [User](id),
    [amount] DECIMAL(10,2),
    [method] NVARCHAR(50),
    [status] NVARCHAR(20),
    [paidAt] DATETIME,
    [transactionId] NVARCHAR(100)
);

CREATE TABLE [ShippingInfo] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [orderId] INT FOREIGN KEY REFERENCES [OrderTable](id),
    [carrier] NVARCHAR(100),
    [trackingNumber] NVARCHAR(100),
    [status] NVARCHAR(50),
    [estimatedArrival] DATETIME
);

CREATE TABLE [ReturnRequest] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [orderId] INT FOREIGN KEY REFERENCES [OrderTable](id),
    [userId] INT FOREIGN KEY REFERENCES [User](id),
    [reason] NVARCHAR(MAX),
    [status] NVARCHAR(20),
    [createdAt] DATETIME
);

CREATE TABLE [Bid] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [productId] INT FOREIGN KEY REFERENCES [Product](id),
    [bidderId] INT FOREIGN KEY REFERENCES [User](id),
    [amount] DECIMAL(10,2),
    [bidTime] DATETIME
);

CREATE TABLE [Review] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [productId] INT FOREIGN KEY REFERENCES [Product](id),
    [reviewerId] INT FOREIGN KEY REFERENCES [User](id),
    [rating] INT,
    [comment] NVARCHAR(MAX),
    [createdAt] DATETIME
);

CREATE TABLE [Message] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [senderId] INT FOREIGN KEY REFERENCES [User](id),
    [receiverId] INT FOREIGN KEY REFERENCES [User](id),
    [content] NVARCHAR(MAX),
    [timestamp] DATETIME
);

CREATE TABLE [Coupon] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [code] NVARCHAR(50),
    [discountPercent] DECIMAL(5,2),
    [startDate] DATETIME,
    [endDate] DATETIME,
    [maxUsage] INT,
    [productId] INT FOREIGN KEY REFERENCES [Product](id)
);

CREATE TABLE [Inventory] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [productId] INT FOREIGN KEY REFERENCES [Product](id),
    [quantity] INT,
    [lastUpdated] DATETIME
);

CREATE TABLE [Feedback] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [sellerId] INT FOREIGN KEY REFERENCES [User](id),
    [averageRating] DECIMAL(3,2),
    [totalReviews] INT,
    [positiveRate] DECIMAL(5,2)
);

CREATE TABLE [Dispute] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [orderId] INT FOREIGN KEY REFERENCES [OrderTable](id),
    [raisedBy] INT FOREIGN KEY REFERENCES [User](id),
    [description] NVARCHAR(MAX),
    [status] NVARCHAR(20),
    [resolution] NVARCHAR(MAX)
);

CREATE TABLE [Store] (
    [id] INT IDENTITY(1,1) PRIMARY KEY,
    [sellerId] INT FOREIGN KEY REFERENCES [User](id),
    [storeName] NVARCHAR(100),
    [description] NVARCHAR(MAX),
    [bannerImageURL] NVARCHAR(MAX)
);

-- =============================================
-- SESSION CACHE TABLE (for ASP.NET Core)
-- =============================================
CREATE TABLE [dbo].[SessionCache] (
    [Id] NVARCHAR(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL PRIMARY KEY,
    [Value] VARBINARY(MAX) NOT NULL,
    [ExpiresAtTime] DATETIMEOFFSET NOT NULL,
    [SlidingExpirationInSeconds] BIGINT NULL,
    [AbsoluteExpiration] DATETIMEOFFSET NULL
);

-- =============================================
-- SEED DATA
-- =============================================
INSERT INTO [User] (username, email, password, role, avatarURL)
VALUES 
('dong', 'dong@example.com', '123456', 'buyer', NULL),
('seller1', 'seller1@example.com', '123456', 'seller', NULL),
('admin', 'admin@example.com', 'admin123', 'admin', NULL),
('Nam', 'namphhe181666@pt.edu.vn', '123456', 'buyer', NULL),
('Nam1', 'studyspacestorage@gmail.com', '123456', 'buyer', NULL);

INSERT INTO [Category] (name)
VALUES
('Electronics'), ('Fashion'), ('Home Appliances'), ('Books'),
('Sports'), ('Beauty & Health'), ('Toys & Games'), ('Groceries'),
('Automotive'), ('Pet Supplies');

INSERT INTO [Product] (title, description, price, images, categoryId, sellerId, isAuction, auctionEndTime)
VALUES
('Wireless Bluetooth Headphones', 'Noise-cancelling over-ear headphones', 1500000, 'headphones.jpg', 1, 2, 0, NULL),
('Men T-Shirt Cotton Classic', 'Soft cotton t-shirt available in multiple colors', 250000, 'tshirt.jpg', 2, 2, 0, NULL),
('Air Fryer 3.5L', 'Oil-free air fryer for healthy cooking', 1200000, 'airfryer.jpg', 3, 2, 0, NULL),
('Clean Code - Robert C. Martin', 'Best practices for writing maintainable code', 350000, 'cleancode.jpg', 4, 2, 0, NULL),
('Badminton Racket Yonex Astrox 77 Pro', 'Professional badminton racket', 3200000, 'racket.jpg', 5, 2, 0, NULL),
('Vitamin C Serum', 'Skin brightening serum', 450000, 'vitaminc.jpg', 6, 2, 0, NULL),
('LEGO Classic Bricks Set', 'Creative building blocks', 800000, 'lego.jpg', 7, 2, 0, NULL),
('Organic Arabica Coffee Beans 500g', 'Premium roasted Arabica beans', 300000, 'coffee.jpg', 8, 2, 0, NULL),
('Car Phone Holder', '360-degree adjustable mount', 200000, 'phoneholder.jpg', 9, 2, 0, NULL),
('Dog Leash Nylon 2m', 'Durable leash for small dogs', 150000, 'leash.jpg', 10, 2, 0, NULL);
GO
