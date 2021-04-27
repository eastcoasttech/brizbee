
-- QBDInventoryItems Table
CREATE TABLE [dbo].[QBDInventoryItems] (
    [Id] BIGINT IDENTITY (1, 1) NOT NULL,
    [Name] VARCHAR(31) NOT NULL,
    [FullName] VARCHAR(159) NOT NULL,
    [ManufacturerPartNumber] VARCHAR(31) NOT NULL,
    [BarCodeValue] VARCHAR(50) NOT NULL,
    [ListId] VARCHAR(20) NOT NULL,
    [PurchaseDescription] VARCHAR(256) NOT NULL,
    [SalesDescription] VARCHAR(256) NOT NULL,
    [QBDInventoryItemSyncId] BIGINT NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventoryItems] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventoryItemSyncId]
    ON [dbo].[QBDInventoryItems] ([QBDInventoryItemSyncId]);
GO


-- QBDInventoryItemSyncs Table
CREATE TABLE [dbo].[QBDInventoryItemSyncs] (
    [Id] BIGINT IDENTITY (1, 1) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [OrganizationId] INT NOT NULL
    CONSTRAINT [PK_dbo.QBDInventoryItemSyncs] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_OrganizationId]
    ON [dbo].[QBDInventoryItemSyncs] ([OrganizationId]);
GO

CREATE NONCLUSTERED INDEX [IX_CreatedByUserId]
    ON [dbo].[QBDInventoryItemSyncs] ([CreatedByUserId]);
GO


-- QBDInventoryConsumptionSyncs Table
CREATE TABLE [dbo].[QBDInventoryConsumptionSyncs] (
    [Id] BIGINT IDENTITY (1, 1) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [OrganizationId] INT NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventoryConsumptionSyncs] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_OrganizationId]
    ON [dbo].[QBDInventoryConsumptionSyncs] ([OrganizationId]);
GO

CREATE NONCLUSTERED INDEX [IX_CreatedByUserId]
    ON [dbo].[QBDInventoryConsumptionSyncs] ([CreatedByUserId]);
GO


-- QBDInventorySites Table
CREATE TABLE [dbo].[QBDInventorySites] (
    [Id] BIGINT IDENTITY (1, 1) NOT NULL,
    [FullName] VARCHAR(159) NOT NULL,
    [ListId] VARCHAR(20) NOT NULL,
    [IsActive] BIT NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventorySites] PRIMARY KEY CLUSTERED ([Id])
);
GO


-- QBDUnitOfMeasureSets Table
CREATE TABLE [dbo].[QBDUnitOfMeasureSets] (
    [Id] BIGINT IDENTITY (1, 1) NOT NULL,
    [Name] VARCHAR(31) NOT NULL,
    [ListId] VARCHAR(20) NOT NULL,
    [UnitOfMeasureType] VARCHAR(255) NOT NULL,
    [IsActive] BIT NOT NULL,
    [BaseUnitName] VARCHAR(31) NOT NULL,
    [BaseUnitAbbreviation] VARCHAR(31) NOT NULL,
    CONSTRAINT [PK_dbo.QBDUnitOfMeasureSets] PRIMARY KEY CLUSTERED ([Id])
);
GO


-- QBDInventoryConsumptions Table
CREATE TABLE [dbo].[QBDInventoryConsumptions] (
    [Id] BIGINT IDENTITY (1, 1) NOT NULL,
    [Quantity] INT NOT NULL,
    [UnitOfMeasure] VARCHAR(31) NOT NULL,
    [CreatedAt] DATETIME2(7) NOT NULL,
    [CreatedByUserId] INT NOT NULL,
    [TaskId] INT NOT NULL,
    [QBDInventoryItemId] BIGINT NOT NULL,
    [QBDInventorySiteId] BIGINT NULL,
    [OrganizationId] INT NOT NULL,
    [QBDInventoryConsumptionSyncId] BIGINT NULL,
    [QBDUnitOfMeasureSetId] BIGINT NULL,
    [Hostname] VARCHAR(30) NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventoryConsumptions] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_OrganizationId]
    ON [dbo].[QBDInventoryConsumptions] ([OrganizationId]);
GO

CREATE NONCLUSTERED INDEX [IX_CreatedByUserId]
    ON [dbo].[QBDInventoryConsumptions] ([CreatedByUserId]);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventoryItemId]
    ON [dbo].[QBDInventoryConsumptions] ([QBDInventoryItemId]);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventorySiteId]
    ON [dbo].[QBDInventoryConsumptions] ([QBDInventorySiteId]);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventoryConsumptionSyncId]
    ON [dbo].[QBDInventoryConsumptions] ([QBDInventoryConsumptionSyncId]);
GO

CREATE NONCLUSTERED INDEX [IX_QBDUnitOfMeasureSetId]
    ON [dbo].[QBDInventoryConsumptions] ([QBDUnitOfMeasureSetId]);
GO
