CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE TABLE `Customers` (
        `Id` char(36) NOT NULL,
        `FirstName` varchar(100) NOT NULL,
        `LastName` varchar(100) NOT NULL,
        `Email` varchar(254) NOT NULL,
        `Phone` varchar(40) NOT NULL,
        `Company` varchar(160) NULL,
        `IsActive` tinyint(1) NOT NULL,
        `LifetimeValue` decimal(14,2) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE TABLE `Products` (
        `Id` char(36) NOT NULL,
        `Sku` varchar(40) NOT NULL,
        `Name` varchar(160) NOT NULL,
        `Category` varchar(80) NOT NULL,
        `Barcode` varchar(64) NULL,
        `UnitPrice` decimal(12,2) NOT NULL,
        `StockOnHand` int NOT NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE TABLE `Sales` (
        `Id` char(36) NOT NULL,
        `Number` varchar(48) NOT NULL,
        `CustomerId` char(36) NULL,
        `CustomerName` varchar(220) NOT NULL,
        `PaymentMethod` int NOT NULL,
        `Status` int NOT NULL,
        `TaxRate` decimal(18,2) NOT NULL,
        `Subtotal` decimal(14,2) NOT NULL,
        `TaxAmount` decimal(14,2) NOT NULL,
        `Total` decimal(14,2) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE TABLE `SaleLine` (
        `Id` char(36) NOT NULL,
        `SaleId` char(36) NOT NULL,
        `ProductId` char(36) NOT NULL,
        `Sku` varchar(40) NOT NULL,
        `ProductName` varchar(160) NOT NULL,
        `UnitPrice` decimal(12,2) NOT NULL,
        `Quantity` int NOT NULL,
        `LineTotal` decimal(14,2) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SaleLine_Sales_SaleId` FOREIGN KEY (`SaleId`) REFERENCES `Sales` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE UNIQUE INDEX `IX_Customers_Email` ON `Customers` (`Email`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE UNIQUE INDEX `IX_Products_Barcode` ON `Products` (`Barcode`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE UNIQUE INDEX `IX_Products_Sku` ON `Products` (`Sku`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE INDEX `IX_SaleLine_SaleId` ON `SaleLine` (`SaleId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    CREATE UNIQUE INDEX `IX_Sales_Number` ON `Sales` (`Number`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172332_InitialCreate')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126172332_InitialCreate', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172842_FiscalPrecisionAndCustomerRelation')
BEGIN
    ALTER TABLE `Sales` MODIFY `TaxRate` decimal(5,4) NOT NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172842_FiscalPrecisionAndCustomerRelation')
BEGIN
    CREATE INDEX `IX_Sales_CustomerId` ON `Sales` (`CustomerId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172842_FiscalPrecisionAndCustomerRelation')
BEGIN
    ALTER TABLE `Sales` ADD CONSTRAINT `FK_Sales_Customers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE SET NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126172842_FiscalPrecisionAndCustomerRelation')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126172842_FiscalPrecisionAndCustomerRelation', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE TABLE `AspNetRoles` (
        `Id` char(36) NOT NULL,
        `Name` varchar(256) NULL,
        `NormalizedName` varchar(256) NULL,
        `ConcurrencyStamp` longtext NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE TABLE `AspNetUsers` (
        `Id` char(36) NOT NULL,
        `DisplayName` varchar(160) NOT NULL,
        `UserName` varchar(256) NULL,
        `NormalizedUserName` varchar(256) NULL,
        `Email` varchar(256) NULL,
        `NormalizedEmail` varchar(256) NULL,
        `EmailConfirmed` tinyint(1) NOT NULL,
        `PasswordHash` longtext NULL,
        `SecurityStamp` longtext NULL,
        `ConcurrencyStamp` longtext NULL,
        `PhoneNumber` longtext NULL,
        `PhoneNumberConfirmed` tinyint(1) NOT NULL,
        `TwoFactorEnabled` tinyint(1) NOT NULL,
        `LockoutEnd` datetime NULL,
        `LockoutEnabled` tinyint(1) NOT NULL,
        `AccessFailedCount` int NOT NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE TABLE `AspNetRoleClaims` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `RoleId` char(36) NOT NULL,
        `ClaimType` longtext NULL,
        `ClaimValue` longtext NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AspNetRoleClaims_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE TABLE `AspNetUserClaims` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` char(36) NOT NULL,
        `ClaimType` longtext NULL,
        `ClaimValue` longtext NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_AspNetUserClaims_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE TABLE `AspNetUserLogins` (
        `LoginProvider` varchar(255) NOT NULL,
        `ProviderKey` varchar(255) NOT NULL,
        `ProviderDisplayName` longtext NULL,
        `UserId` char(36) NOT NULL,
        PRIMARY KEY (`LoginProvider`, `ProviderKey`),
        CONSTRAINT `FK_AspNetUserLogins_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE TABLE `AspNetUserRoles` (
        `UserId` char(36) NOT NULL,
        `RoleId` char(36) NOT NULL,
        PRIMARY KEY (`UserId`, `RoleId`),
        CONSTRAINT `FK_AspNetUserRoles_AspNetRoles_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `AspNetRoles` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_AspNetUserRoles_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE TABLE `AspNetUserTokens` (
        `UserId` char(36) NOT NULL,
        `LoginProvider` varchar(255) NOT NULL,
        `Name` varchar(255) NOT NULL,
        `Value` longtext NULL,
        PRIMARY KEY (`UserId`, `LoginProvider`, `Name`),
        CONSTRAINT `FK_AspNetUserTokens_AspNetUsers_UserId` FOREIGN KEY (`UserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE INDEX `IX_AspNetRoleClaims_RoleId` ON `AspNetRoleClaims` (`RoleId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE UNIQUE INDEX `RoleNameIndex` ON `AspNetRoles` (`NormalizedName`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE INDEX `IX_AspNetUserClaims_UserId` ON `AspNetUserClaims` (`UserId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE INDEX `IX_AspNetUserLogins_UserId` ON `AspNetUserLogins` (`UserId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE INDEX `IX_AspNetUserRoles_RoleId` ON `AspNetUserRoles` (`RoleId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE INDEX `EmailIndex` ON `AspNetUsers` (`NormalizedEmail`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    CREATE UNIQUE INDEX `UserNameIndex` ON `AspNetUsers` (`NormalizedUserName`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126175507_IdentityAndAccessControl')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126175507_IdentityAndAccessControl', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126181926_SaleNumberCapacity')
BEGIN
    ALTER TABLE `Sales` MODIFY `Number` varchar(64) NOT NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126181926_SaleNumberCapacity')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126181926_SaleNumberCapacity', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE TABLE `Tenants` (
        `Id` char(36) NOT NULL,
        `Name` varchar(160) NOT NULL,
        `Slug` varchar(80) NOT NULL,
        `OwnerEmail` varchar(254) NOT NULL,
        `TimeZoneId` varchar(100) NOT NULL,
        `CurrencyCode` varchar(3) NOT NULL,
        `PlanCode` varchar(40) NOT NULL,
        `Status` int NOT NULL,
        `TrialEndsAtUtc` datetime NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        PRIMARY KEY (`Id`)
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    INSERT INTO `Tenants` (`Id`, `Name`, `Slug`, `OwnerEmail`, `TimeZoneId`, `CurrencyCode`, `PlanCode`, `Status`, `TrialEndsAtUtc`, `CreatedAtUtc`, `UpdatedAtUtc`)
    VALUES ('00000000-0000-0000-0000-000000000001', 'Ojunny Demo', 'ojunny-demo', 'admin@ojunny.local', 'America/Santo_Domingo', 'DOP', 'trial', 0, NULL, TIMESTAMP '2026-11-26 00:00:00.000', NULL);
    SELECT ROW_COUNT();

END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    DROP INDEX IX_Sales_Number ON Sales;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    DROP INDEX IX_Products_Barcode ON Products;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    DROP INDEX IX_Products_Sku ON Products;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    DROP INDEX IX_Customers_Email ON Customers;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Sales` ADD `TenantId` char(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Products` ADD `TenantId` char(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Customers` ADD `TenantId` char(36) NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `AspNetUsers` ADD `TenantId` char(36) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    UPDATE AspNetUsers SET TenantId = '00000000-0000-0000-0000-000000000001' WHERE TenantId IS NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Sales` MODIFY `TenantId` char(36) NOT NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Products` MODIFY `TenantId` char(36) NOT NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Customers` MODIFY `TenantId` char(36) NOT NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE INDEX `IX_Sales_TenantId_CreatedAtUtc` ON `Sales` (`TenantId`, `CreatedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE UNIQUE INDEX `IX_Sales_TenantId_Number` ON `Sales` (`TenantId`, `Number`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE UNIQUE INDEX `IX_Products_TenantId_Barcode` ON `Products` (`TenantId`, `Barcode`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE INDEX `IX_Products_TenantId_IsActive_Name` ON `Products` (`TenantId`, `IsActive`, `Name`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE UNIQUE INDEX `IX_Products_TenantId_Sku` ON `Products` (`TenantId`, `Sku`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE UNIQUE INDEX `IX_Customers_TenantId_Email` ON `Customers` (`TenantId`, `Email`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE INDEX `IX_Customers_TenantId_IsActive_FirstName_LastName` ON `Customers` (`TenantId`, `IsActive`, `FirstName`, `LastName`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE INDEX `IX_AspNetUsers_TenantId` ON `AspNetUsers` (`TenantId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE INDEX `IX_Tenants_OwnerEmail` ON `Tenants` (`OwnerEmail`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    CREATE UNIQUE INDEX `IX_Tenants_Slug` ON `Tenants` (`Slug`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `AspNetUsers` ADD CONSTRAINT `FK_AspNetUsers_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE SET NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Customers` ADD CONSTRAINT `FK_Customers_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Products` ADD CONSTRAINT `FK_Products_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    ALTER TABLE `Sales` ADD CONSTRAINT `FK_Sales_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126182951_MultiTenantIsolation')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126182951_MultiTenantIsolation', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126184953_RequirePasswordRotation')
BEGIN
    ALTER TABLE `AspNetUsers` ADD `MustChangePassword` tinyint(1) NOT NULL DEFAULT FALSE;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126184953_RequirePasswordRotation')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126184953_RequirePasswordRotation', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE TABLE `InventoryMovements` (
        `Id` char(36) NOT NULL,
        `ProductId` char(36) NOT NULL,
        `Type` int NOT NULL,
        `QuantityDelta` int NOT NULL,
        `BalanceAfter` int NOT NULL,
        `Reference` varchar(100) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_InventoryMovements_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_InventoryMovements_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE TABLE `Suppliers` (
        `Id` char(36) NOT NULL,
        `Name` varchar(160) NOT NULL,
        `TaxIdentifier` varchar(40) NULL,
        `Email` varchar(254) NOT NULL,
        `Phone` varchar(40) NOT NULL,
        `Notes` varchar(1000) NULL,
        `IsActive` tinyint(1) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Suppliers_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE TABLE `PurchaseOrders` (
        `Id` char(36) NOT NULL,
        `Number` varchar(64) NOT NULL,
        `SupplierId` char(36) NOT NULL,
        `SupplierName` varchar(160) NOT NULL,
        `Reference` varchar(100) NULL,
        `Status` int NOT NULL,
        `Total` decimal(14,2) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PurchaseOrders_Suppliers_SupplierId` FOREIGN KEY (`SupplierId`) REFERENCES `Suppliers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_PurchaseOrders_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE TABLE `PurchaseOrderLines` (
        `Id` char(36) NOT NULL,
        `PurchaseOrderId` char(36) NOT NULL,
        `ProductId` char(36) NOT NULL,
        `Sku` varchar(40) NOT NULL,
        `ProductName` varchar(160) NOT NULL,
        `Quantity` int NOT NULL,
        `ReceivedQuantity` int NOT NULL,
        `UnitCost` decimal(12,2) NOT NULL,
        `LineTotal` decimal(14,2) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_PurchaseOrderLines_Products_ProductId` FOREIGN KEY (`ProductId`) REFERENCES `Products` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_PurchaseOrderLines_PurchaseOrders_PurchaseOrderId` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `PurchaseOrders` (`Id`) ON DELETE CASCADE
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE INDEX `IX_InventoryMovements_ProductId` ON `InventoryMovements` (`ProductId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE INDEX `IX_InventoryMovements_TenantId_ProductId_CreatedAtUtc` ON `InventoryMovements` (`TenantId`, `ProductId`, `CreatedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE INDEX `IX_PurchaseOrderLines_ProductId` ON `PurchaseOrderLines` (`ProductId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE INDEX `IX_PurchaseOrderLines_PurchaseOrderId` ON `PurchaseOrderLines` (`PurchaseOrderId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE INDEX `IX_PurchaseOrders_SupplierId` ON `PurchaseOrders` (`SupplierId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE INDEX `IX_PurchaseOrders_TenantId_CreatedAtUtc` ON `PurchaseOrders` (`TenantId`, `CreatedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE UNIQUE INDEX `IX_PurchaseOrders_TenantId_Number` ON `PurchaseOrders` (`TenantId`, `Number`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    CREATE INDEX `IX_Suppliers_TenantId_Name` ON `Suppliers` (`TenantId`, `Name`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126191731_PurchasingAndInventoryLedger')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126191731_PurchasingAndInventoryLedger', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126192136_BackfillOpeningInventoryMovements')
BEGIN
    INSERT INTO `InventoryMovements`
        (`Id`, `ProductId`, `Type`, `QuantityDelta`, `BalanceAfter`, `Reference`, `CreatedAtUtc`, `UpdatedAtUtc`, `TenantId`)
    SELECT UUID(), `Id`, 3, `StockOnHand`, `StockOnHand`, 'Initial stock when enabling ledger', UTC_TIMESTAMP(), NULL, `TenantId`
    FROM `Products`
    WHERE `StockOnHand` > 0;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126192136_BackfillOpeningInventoryMovements')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126192136_BackfillOpeningInventoryMovements', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    ALTER TABLE `Sales` ADD `CashSessionId` char(36) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    ALTER TABLE `Sales` ADD `CashierUserId` char(36) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    CREATE TABLE `CashSessions` (
        `Id` char(36) NOT NULL,
        `RegisterCode` varchar(40) NOT NULL,
        `OpeningFloat` decimal(14,2) NOT NULL,
        `OpenedByUserId` char(36) NOT NULL,
        `Status` int NOT NULL,
        `ClosedByUserId` char(36) NULL,
        `ClosedAtUtc` datetime NULL,
        `ExpectedCash` decimal(14,2) NULL,
        `CountedCash` decimal(14,2) NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CashSessions_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    CREATE TABLE `CashMovements` (
        `Id` char(36) NOT NULL,
        `SessionId` char(36) NOT NULL,
        `UserId` char(36) NOT NULL,
        `Kind` int NOT NULL,
        `Amount` decimal(14,2) NOT NULL,
        `Description` varchar(200) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CashMovements_CashSessions_SessionId` FOREIGN KEY (`SessionId`) REFERENCES `CashSessions` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CashMovements_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    CREATE INDEX `IX_Sales_CashSessionId` ON `Sales` (`CashSessionId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    CREATE INDEX `IX_CashMovements_SessionId` ON `CashMovements` (`SessionId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    CREATE INDEX `IX_CashMovements_TenantId_SessionId_CreatedAtUtc` ON `CashMovements` (`TenantId`, `SessionId`, `CreatedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    CREATE INDEX `IX_CashSessions_TenantId_RegisterCode_Status` ON `CashSessions` (`TenantId`, `RegisterCode`, `Status`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    ALTER TABLE `Sales` ADD CONSTRAINT `FK_Sales_CashSessions_CashSessionId` FOREIGN KEY (`CashSessionId`) REFERENCES `CashSessions` (`Id`) ON DELETE RESTRICT;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126194100_CashSessionsAndReceipts')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126194100_CashSessionsAndReceipts', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE TABLE `Opportunities` (
        `Id` char(36) NOT NULL,
        `CustomerId` char(36) NOT NULL,
        `Title` varchar(160) NOT NULL,
        `ExpectedValue` decimal(14,2) NOT NULL,
        `ExpectedCloseDate` date NULL,
        `Description` varchar(2000) NULL,
        `Stage` int NOT NULL,
        `ClosedAtUtc` datetime NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_Opportunities_Customers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_Opportunities_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE TABLE `CrmActivities` (
        `Id` char(36) NOT NULL,
        `CustomerId` char(36) NOT NULL,
        `OpportunityId` char(36) NULL,
        `CreatedByUserId` char(36) NOT NULL,
        `Kind` int NOT NULL,
        `Subject` varchar(160) NOT NULL,
        `Details` varchar(2000) NULL,
        `OccurredAtUtc` datetime NOT NULL,
        `DueAtUtc` datetime NULL,
        `CompletedAtUtc` datetime NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CrmActivities_Customers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CrmActivities_Opportunities_OpportunityId` FOREIGN KEY (`OpportunityId`) REFERENCES `Opportunities` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_CrmActivities_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE INDEX `IX_CrmActivities_CustomerId` ON `CrmActivities` (`CustomerId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE INDEX `IX_CrmActivities_OpportunityId` ON `CrmActivities` (`OpportunityId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE INDEX `IX_CrmActivities_TenantId_CustomerId_OccurredAtUtc` ON `CrmActivities` (`TenantId`, `CustomerId`, `OccurredAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE INDEX `IX_CrmActivities_TenantId_Kind_DueAtUtc_CompletedAtUtc` ON `CrmActivities` (`TenantId`, `Kind`, `DueAtUtc`, `CompletedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE INDEX `IX_Opportunities_CustomerId` ON `Opportunities` (`CustomerId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    CREATE INDEX `IX_Opportunities_TenantId_Stage_ExpectedCloseDate` ON `Opportunities` (`TenantId`, `Stage`, `ExpectedCloseDate`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126195505_CrmOpportunitiesAndActivities')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126195505_CrmOpportunitiesAndActivities', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126200512_CustomerOptimisticConcurrency')
BEGIN
    ALTER TABLE `Customers` ADD `Revision` bigint NOT NULL DEFAULT 0;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126200512_CustomerOptimisticConcurrency')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126200512_CustomerOptimisticConcurrency', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126201554_SupplierPayments')
BEGIN
    CREATE TABLE `SupplierPayments` (
        `Id` char(36) NOT NULL,
        `PurchaseOrderId` char(36) NOT NULL,
        `PaidByUserId` char(36) NOT NULL,
        `IdempotencyKey` char(36) NOT NULL,
        `Amount` decimal(14,2) NOT NULL,
        `Method` int NOT NULL,
        `Reference` varchar(100) NULL,
        `CashSessionId` char(36) NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_SupplierPayments_AspNetUsers_PaidByUserId` FOREIGN KEY (`PaidByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_SupplierPayments_CashSessions_CashSessionId` FOREIGN KEY (`CashSessionId`) REFERENCES `CashSessions` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_SupplierPayments_PurchaseOrders_PurchaseOrderId` FOREIGN KEY (`PurchaseOrderId`) REFERENCES `PurchaseOrders` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_SupplierPayments_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126201554_SupplierPayments')
BEGIN
    CREATE INDEX `IX_SupplierPayments_CashSessionId` ON `SupplierPayments` (`CashSessionId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126201554_SupplierPayments')
BEGIN
    CREATE INDEX `IX_SupplierPayments_PaidByUserId` ON `SupplierPayments` (`PaidByUserId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126201554_SupplierPayments')
BEGIN
    CREATE INDEX `IX_SupplierPayments_PurchaseOrderId` ON `SupplierPayments` (`PurchaseOrderId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126201554_SupplierPayments')
BEGIN
    CREATE UNIQUE INDEX `IX_SupplierPayments_TenantId_IdempotencyKey` ON `SupplierPayments` (`TenantId`, `IdempotencyKey`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126201554_SupplierPayments')
BEGIN
    CREATE INDEX `IX_SupplierPayments_TenantId_PurchaseOrderId_CreatedAtUtc` ON `SupplierPayments` (`TenantId`, `PurchaseOrderId`, `CreatedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126201554_SupplierPayments')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126201554_SupplierPayments', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126202344_SupplierRevisionConcurrency')
BEGIN
    ALTER TABLE `Suppliers` ADD `Revision` bigint NOT NULL DEFAULT 0;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126202344_SupplierRevisionConcurrency')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126202344_SupplierRevisionConcurrency', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126203059_PurchaseDueDate')
BEGIN
    ALTER TABLE `PurchaseOrders` ADD `DueDate` date NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126203059_PurchaseDueDate')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126203059_PurchaseDueDate', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    ALTER TABLE `Opportunities` ADD `AssignedToUserId` char(36) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    ALTER TABLE `CrmActivities` ADD `AssignedToUserId` char(36) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    CREATE INDEX `IX_Opportunities_AssignedToUserId` ON `Opportunities` (`AssignedToUserId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    CREATE INDEX `IX_Opportunities_TenantId_AssignedToUserId` ON `Opportunities` (`TenantId`, `AssignedToUserId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    CREATE INDEX `IX_CrmActivities_AssignedToUserId` ON `CrmActivities` (`AssignedToUserId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    CREATE INDEX `IX_CrmActivities_TenantId_AssignedToUserId_Kind_DueAtUtc_Comple~` ON `CrmActivities` (`TenantId`, `AssignedToUserId`, `Kind`, `DueAtUtc`, `CompletedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    ALTER TABLE `CrmActivities` ADD CONSTRAINT `FK_CrmActivities_AspNetUsers_AssignedToUserId` FOREIGN KEY (`AssignedToUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    ALTER TABLE `Opportunities` ADD CONSTRAINT `FK_Opportunities_AspNetUsers_AssignedToUserId` FOREIGN KEY (`AssignedToUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE SET NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126204834_CrmAssignment')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126204834_CrmAssignment', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126210629_CustomerSalesTimelineIndex')
BEGIN
    CREATE INDEX `IX_Sales_TenantId_CustomerId_CreatedAtUtc` ON `Sales` (`TenantId`, `CustomerId`, `CreatedAtUtc`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126210629_CustomerSalesTimelineIndex')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126210629_CustomerSalesTimelineIndex', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE TABLE `CustomerTags` (
        `Id` char(36) NOT NULL,
        `Name` varchar(40) NOT NULL,
        `NormalizedName` varchar(40) NOT NULL,
        `Color` int NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CustomerTags_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE TABLE `CustomerTagAssignments` (
        `Id` char(36) NOT NULL,
        `CustomerId` char(36) NOT NULL,
        `CustomerTagId` char(36) NOT NULL,
        `AssignedByUserId` char(36) NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CustomerTagAssignments_AspNetUsers_AssignedByUserId` FOREIGN KEY (`AssignedByUserId`) REFERENCES `AspNetUsers` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CustomerTagAssignments_CustomerTags_CustomerTagId` FOREIGN KEY (`CustomerTagId`) REFERENCES `CustomerTags` (`Id`) ON DELETE RESTRICT,
        CONSTRAINT `FK_CustomerTagAssignments_Customers_CustomerId` FOREIGN KEY (`CustomerId`) REFERENCES `Customers` (`Id`) ON DELETE CASCADE,
        CONSTRAINT `FK_CustomerTagAssignments_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE INDEX `IX_CustomerTagAssignments_AssignedByUserId` ON `CustomerTagAssignments` (`AssignedByUserId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE INDEX `IX_CustomerTagAssignments_CustomerId` ON `CustomerTagAssignments` (`CustomerId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE INDEX `IX_CustomerTagAssignments_CustomerTagId` ON `CustomerTagAssignments` (`CustomerTagId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE UNIQUE INDEX `IX_CustomerTagAssignments_TenantId_CustomerId_CustomerTagId` ON `CustomerTagAssignments` (`TenantId`, `CustomerId`, `CustomerTagId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE INDEX `IX_CustomerTagAssignments_TenantId_CustomerTagId_CustomerId` ON `CustomerTagAssignments` (`TenantId`, `CustomerTagId`, `CustomerId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    CREATE UNIQUE INDEX `IX_CustomerTags_TenantId_NormalizedName` ON `CustomerTags` (`TenantId`, `NormalizedName`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126211234_CustomerTags')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126211234_CustomerTags', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126212327_CustomerSegments')
BEGIN
    CREATE TABLE `CustomerSegments` (
        `Id` char(36) NOT NULL,
        `Name` varchar(100) NOT NULL,
        `NormalizedName` varchar(100) NOT NULL,
        `RequiredTagId` char(36) NULL,
        `MinimumLifetimeValue` decimal(14,2) NULL,
        `MaximumLifetimeValue` decimal(14,2) NULL,
        `MinimumSalesCount` int NULL,
        `PurchasedWithinDays` int NULL,
        `ActiveCustomersOnly` tinyint(1) NOT NULL,
        `Revision` bigint NOT NULL,
        `CreatedAtUtc` datetime NOT NULL,
        `UpdatedAtUtc` datetime NULL,
        `TenantId` char(36) NOT NULL,
        PRIMARY KEY (`Id`),
        CONSTRAINT `FK_CustomerSegments_CustomerTags_RequiredTagId` FOREIGN KEY (`RequiredTagId`) REFERENCES `CustomerTags` (`Id`) ON DELETE SET NULL,
        CONSTRAINT `FK_CustomerSegments_Tenants_TenantId` FOREIGN KEY (`TenantId`) REFERENCES `Tenants` (`Id`) ON DELETE RESTRICT
    );
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126212327_CustomerSegments')
BEGIN
    CREATE INDEX `IX_CustomerSegments_RequiredTagId` ON `CustomerSegments` (`RequiredTagId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126212327_CustomerSegments')
BEGIN
    CREATE UNIQUE INDEX `IX_CustomerSegments_TenantId_NormalizedName` ON `CustomerSegments` (`TenantId`, `NormalizedName`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126212327_CustomerSegments')
BEGIN
    CREATE INDEX `IX_CustomerSegments_TenantId_RequiredTagId` ON `CustomerSegments` (`TenantId`, `RequiredTagId`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126212327_CustomerSegments')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126212327_CustomerSegments', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126213925_ProductCatalogLifecycle')
BEGIN
    ALTER TABLE `Products` ADD `Revision` bigint NOT NULL DEFAULT 0;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126213925_ProductCatalogLifecycle')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126213925_ProductCatalogLifecycle', '10.0.12');
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126214715_CheckoutIdempotency')
BEGIN
    ALTER TABLE `Sales` ADD `IdempotencyKey` char(36) NULL;
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126214715_CheckoutIdempotency')
BEGIN
    CREATE UNIQUE INDEX `IX_Sales_TenantId_IdempotencyKey` ON `Sales` (`TenantId`, `IdempotencyKey`);
END;

IF NOT EXISTS(SELECT * FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20261126214715_CheckoutIdempotency')
BEGIN
    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20261126214715_CheckoutIdempotency', '10.0.12');
END;

COMMIT;

