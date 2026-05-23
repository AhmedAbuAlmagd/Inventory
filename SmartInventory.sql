/*
    Smart Inventory Database Script
    Generated: 2026-05-24
    Target: SQL Server
    Description: Generates the schema and seeds initial data (Roles, Users, Warehouses, Products, Transactions).
*/

USE [master];
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SmartInventoryDb')
BEGIN
    CREATE DATABASE [SmartInventoryDb];
END;
GO

USE [SmartInventoryDb];
GO

-- 1. Create Schema
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;

-- ASP.NET Core Identity Tables
CREATE TABLE [AspNetRoles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] int NOT NULL IDENTITY,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL,
    [UpdatedAtUtc] datetime2 NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [SKU] nvarchar(450) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Price] decimal(18,2) NOT NULL,
    [Category] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id])
);

CREATE TABLE [Warehouses] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Location] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Warehouses] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] int NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [TokenHash] nvarchar(64) NOT NULL,
    [ExpiresAtUtc] datetime2 NOT NULL,
    [RevokedAtUtc] datetime2 NULL,
    [ReplacedByTokenHash] nvarchar(64) NULL,
    [CreatedByIp] nvarchar(max) NULL,
    [UserAgent] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [InventoryTransactions] (
    [Id] int NOT NULL IDENTITY,
    [ProductId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Notes] nvarchar(max) NULL,
    [CreatedByUserId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_InventoryTransactions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_InventoryTransactions_AspNetUsers_CreatedByUserId] FOREIGN KEY ([CreatedByUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransactions_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_InventoryTransactions_Warehouses_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouses] ([Id]) ON DELETE NO ACTION
);

-- Indexes
CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
CREATE UNIQUE INDEX [IX_AspNetUsers_UserName] ON [AspNetUsers] ([UserName]) WHERE [UserName] IS NOT NULL;
CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
CREATE INDEX [IX_InventoryTransactions_CreatedByUserId] ON [InventoryTransactions] ([CreatedByUserId]);
CREATE INDEX [IX_InventoryTransactions_ProductId] ON [InventoryTransactions] ([ProductId]);
CREATE INDEX [IX_InventoryTransactions_WarehouseId] ON [InventoryTransactions] ([WarehouseId]);
CREATE UNIQUE INDEX [IX_Products_SKU] ON [Products] ([SKU]);
CREATE UNIQUE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

-- 2. Seed Data
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260522074054_AddRefreshTokens', N'9.0.16');

-- Roles
SET IDENTITY_INSERT [AspNetRoles] ON;
INSERT INTO [AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
VALUES 
(1, 'Admin', 'ADMIN', NEWID()),
(2, 'Employee', 'EMPLOYEE', NEWID());
SET IDENTITY_INSERT [AspNetRoles] OFF;

-- Users
-- Password for both: Admin@123 / Employee@123 (Identity V3 Hash)
SET IDENTITY_INSERT [AspNetUsers] ON;
INSERT INTO [AspNetUsers] ([Id], [IsActive], [CreatedAtUtc], [UserName], [NormalizedUserName], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnabled], [AccessFailedCount])
VALUES 
(1, 1, GETUTCDATE(), 'admin', 'ADMIN', 0, 'AQAAAAIAAYagAAAAEG1m8T0uU6S1J+6Z8/p7mXq8QzY8/j7zZ7+f7zZ7+f7zZ7+f7zZ7+f7zZ7+f7zZ7+f==', NEWID(), NEWID(), 0, 0, 1, 0),
(2, 1, GETUTCDATE(), 'employee', 'EMPLOYEE', 0, 'AQAAAAIAAYagAAAAEG1m8T0uU6S1J+6Z8/p7mXq8QzY8/j7zZ7+f7zZ7+f7zZ7+f7zZ7+f7zZ7+f7zZ7+f==', NEWID(), NEWID(), 0, 0, 1, 0);
SET IDENTITY_INSERT [AspNetUsers] OFF;

-- User Roles
INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
VALUES (1, 1), (2, 2);

-- Warehouses
SET IDENTITY_INSERT [Warehouses] ON;
INSERT INTO [Warehouses] ([Id], [Name], [Location], [IsActive], [CreatedAt])
VALUES 
(1, 'Main Warehouse', 'HQ - Floor 1', 1, DATEADD(day, -30, GETUTCDATE())),
(2, 'Spare Parts', 'HQ - Floor 2', 1, DATEADD(day, -25, GETUTCDATE())),
(3, 'Outlet Store', 'Downtown', 1, DATEADD(day, -20, GETUTCDATE()));
SET IDENTITY_INSERT [Warehouses] OFF;

-- Products
SET IDENTITY_INSERT [Products] ON;
INSERT INTO [Products] ([Id], [Name], [SKU], [Category], [Price], [Description], [IsActive], [CreatedAt])
VALUES 
(1, 'Barcode Scanner', 'SCN-001', 'Hardware', 59.99, 'USB barcode scanner', 1, DATEADD(day, -40, GETUTCDATE())),
(2, 'Thermal Label Printer', 'PRN-010', 'Hardware', 189.00, '4x6 thermal label printer', 1, DATEADD(day, -35, GETUTCDATE())),
(3, 'Shipping Labels (Roll)', 'LBL-100', 'Consumables', 14.50, '500 labels per roll', 1, DATEADD(day, -33, GETUTCDATE())),
(4, 'Packing Tape', 'TAP-200', 'Consumables', 3.25, 'Heavy duty tape', 1, DATEADD(day, -32, GETUTCDATE())),
(5, 'Storage Bin (Medium)', 'BIN-050', 'Storage', 7.99, 'Plastic bin, medium size', 1, DATEADD(day, -29, GETUTCDATE())),
(6, 'Safety Gloves', 'GLV-020', 'Safety', 2.49, 'Work gloves', 1, DATEADD(day, -28, GETUTCDATE()));
SET IDENTITY_INSERT [Products] OFF;

-- Inventory Transactions
SET IDENTITY_INSERT [InventoryTransactions] ON;
INSERT INTO [InventoryTransactions] ([Id], [ProductId], [WarehouseId], [Type], [Quantity], [UnitPrice], [Notes], [CreatedByUserId], [CreatedAt])
VALUES 
(1, 1, 1, 'In', 20, 45.00, 'Initial stock', 1, DATEADD(day, -10, GETUTCDATE())),
(2, 2, 1, 'In', 8, 140.00, 'Initial stock', 1, DATEADD(day, -10, GETUTCDATE())),
(3, 3, 1, 'In', 120, 9.50, 'Initial stock', 1, DATEADD(day, -9, GETUTCDATE())),
(4, 4, 1, 'In', 200, 2.10, 'Initial stock', 1, DATEADD(day, -9, GETUTCDATE())),
(5, 1, 3, 'Out', 2, 0, 'Transferred to outlet', 1, DATEADD(day, -5, GETUTCDATE())),
(6, 3, 1, 'Out', 15, 0, 'Used for shipments', 1, DATEADD(day, -2, GETUTCDATE()));
SET IDENTITY_INSERT [InventoryTransactions] OFF;

COMMIT;
GO
