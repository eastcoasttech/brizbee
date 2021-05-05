
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
    [QBDUnitOfMeasureSetListId] VARCHAR(20) NULL,
    [QBDUnitOfMeasureSetFullName] VARCHAR(31) NULL,
    [QBDUnitOfMeasureSetId] BIGINT NULL,
    [PurchaseCost] DECIMAL(10,2) NOT NULL,
    [SalesPrice] DECIMAL(10,2) NOT NULL,
    [QBDCOGSAccountListId] VARCHAR(20) NULL,
    [QBDCOGSAccountFullName] VARCHAR(31) NULL,
    CONSTRAINT [PK_dbo.QBDInventoryItems] PRIMARY KEY CLUSTERED ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_QBDInventoryItemSyncId]
    ON [dbo].[QBDInventoryItems] ([QBDInventoryItemSyncId]);
GO


-- QBDInventoryItemSyncs Table
CREATE TABLE [dbo].[QBDInventoryItemSyncs] (
    [Id]                            BIGINT IDENTITY (1, 1) NOT NULL,
    [CreatedAt]                     DATETIME2(7) NOT NULL,
    [CreatedByUserId]               INT NOT NULL,
    [OrganizationId]                INT NOT NULL,
    [HostProductName]               VARCHAR (50) NOT NULL,
    [HostMajorVersion]              VARCHAR (10) NOT NULL,
    [HostMinorVersion]              VARCHAR (10) NOT NULL,
    [HostCountry]                   VARCHAR (10) NOT NULL,
    [HostSupportedQBXMLVersion]     VARCHAR (100) NOT NULL,
    [Hostname]                      VARCHAR (15) NOT NULL,
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
    [Id]                            BIGINT IDENTITY (1, 1) NOT NULL,
    [CreatedAt]                     DATETIME2 (7) NOT NULL,
    [CreatedByUserId]               INT NOT NULL,
    [OrganizationId]                INT NOT NULL,
    [RecordingMethod]               VARCHAR (25) NOT NULL,
    [ValueMethod]                   VARCHAR (25) NOT NULL,
    [HostProductName]               VARCHAR (50) NOT NULL,
    [HostMajorVersion]              VARCHAR (10) NOT NULL,
    [HostMinorVersion]              VARCHAR (10) NOT NULL,
    [HostCountry]                   VARCHAR (10) NOT NULL,
    [HostSupportedQBXMLVersion]     VARCHAR (100) NOT NULL,
    [ConsumptionsCount]             INT NOT NULL,
    [Hostname]                      VARCHAR (15) NOT NULL,
    [TxnIDs]                        VARCHAR (MAX) NOT NULL,
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
    [Id]                            BIGINT IDENTITY (1, 1) NOT NULL,
    [Name]                          VARCHAR(31) NOT NULL,
    [ListId]                        VARCHAR(20) NOT NULL,
    [UnitOfMeasureType]             VARCHAR(255) NOT NULL,
    [IsActive]                      BIT NOT NULL,
    [UnitNamesAndAbbreviations]     NVARCHAR(MAX) CONSTRAINT [CT_UnitNamesAndAbbreviations] CHECK (ISJSON([UnitNamesAndAbbreviations])=1) NOT NULL,
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
