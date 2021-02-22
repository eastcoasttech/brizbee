
-- QBDInventoryItems Table
CREATE TABLE [dbo].[QBDInventoryItems] (
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [Name] VARCHAR(31) NOT NULL,
    [FullName] VARCHAR(159) NOT NULL,
    [ManufacturerPartNumber] VARCHAR(31) NOT NULL,
    [BarCodeValue] VARCHAR(50) NOT NULL,
    [ListId] VARCHAR(20) NOT NULL,
    [PurchaseDescription] VARCHAR(256) NOT NULL,
    [SalesDescription] VARCHAR(256) NOT NULL,
    [QBDInventoryItemSyncId] INT NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventoryItems] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventoryItemSyncId]
    ON [dbo].[QBDInventoryItems]([QBDInventoryItemSyncId] ASC);
GO


-- QBDInventoryItemSyncs Table
CREATE TABLE [dbo].[QBDInventoryItemSyncs] (
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [OrganizationId] INT NOT NULL
    CONSTRAINT [PK_dbo.QBDInventoryItemSyncs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_OrganizationId]
    ON [dbo].[QBDInventoryItemSyncs]([OrganizationId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_CreatedByUserId]
    ON [dbo].[QBDInventoryItemSyncs]([CreatedByUserId] ASC);
GO


-- QBDInventoryAdjustmentSyncs Table
CREATE TABLE [dbo].[QBDInventoryAdjustmentSyncs] (
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [OrganizationId] INT NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventoryAdjustmentSyncs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_OrganizationId]
    ON [dbo].[QBDInventoryAdjustmentSyncs]([OrganizationId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_CreatedByUserId]
    ON [dbo].[QBDInventoryAdjustmentSyncs]([CreatedByUserId] ASC);
GO


-- QBDInventorySites Table
CREATE TABLE [dbo].[QBDInventorySites] (
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [FullName] VARCHAR(159) NOT NULL,
    [ListId] VARCHAR(20) NOT NULL,
    [IsActive] BIT NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventorySites] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- QBDUnitOfMeasureSets Table
CREATE TABLE [dbo].[QBDUnitOfMeasureSets] (
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [Name] VARCHAR(31) NOT NULL,
    [ListId] VARCHAR(20) NOT NULL,
    [UnitOfMeasureType] VARCHAR(255) NOT NULL,
    [IsActive] BIT NOT NULL,
    [BaseUnitName] VARCHAR(31) NOT NULL,
    [BaseUnitAbbreviation] VARCHAR(31) NOT NULL,
    CONSTRAINT [PK_dbo.QBDUnitOfMeasureSets] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- InventoryAdjustments Table
CREATE TABLE [dbo].[InventoryAdjustments] (
    [Id] INT IDENTITY (1, 1) NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitOfMeasure] VARCHAR(31) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [QBDInventoryItemId] INT NOT NULL,
    [QBDInventorySiteId] INT NOT NULL,
    [OrganizationId] INT NOT NULL,
    [QBDInventoryAdjustmentSyncId] INT NULL,
    [QBDUnitOfMeasureSetId] INT NOT NULL,
    CONSTRAINT [PK_dbo.InventoryAdjustments] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_OrganizationId]
    ON [dbo].[InventoryAdjustments]([OrganizationId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_CreatedByUserId]
    ON [dbo].[InventoryAdjustments]([CreatedByUserId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventoryItemId]
    ON [dbo].[InventoryAdjustments]([QBDInventoryItemId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventorySiteId]
    ON [dbo].[InventoryAdjustments]([QBDInventorySiteId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventoryAdjustmentSyncId]
    ON [dbo].[InventoryAdjustments]([QBDInventoryAdjustmentSyncId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_QBDUnitOfMeasureSetId]
    ON [dbo].[InventoryAdjustments]([QBDUnitOfMeasureSetId] ASC);
GO