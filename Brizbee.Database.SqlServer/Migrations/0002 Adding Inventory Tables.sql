
-- Commits Table
CREATE TABLE [dbo].[QBDInventoryItems] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [QuickBooksExportedAt] datetime2(7) NULL,
    [Guid] uniqueidentifier NOT NULL,
    [InAt] datetime2(7) NOT NULL,
    [OrganizationId] int NOT NULL,
    [OutAt] datetime2(7) NOT NULL,
    [PunchCount] int NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_dbo.QBDInventoryItems] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO