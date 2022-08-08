-- Accounts Table
CREATE TABLE [dbo].[Accounts]
    (
        [CreatedAt]      DATETIME2 (7) NOT NULL,
        [Description]    VARCHAR (120) NOT NULL,
        [Id]             BIGINT        IDENTITY (1, 1) NOT NULL,
        [Name]           VARCHAR (40)  NOT NULL,
        [Number]         VARCHAR (20)  NOT NULL,
        [OrganizationId] INT           NOT NULL,
        [Type]           VARCHAR (30)  NOT NULL,
        CONSTRAINT [PK_Accounts]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Accounts_Name]
    ON [dbo].[Accounts] ([Name]);

-- Transactions Table
CREATE TABLE [dbo].[Transactions]
    (
        [CreatedAt]      DATETIME2 (7) NOT NULL,
        [Description]    VARCHAR (60)  NOT NULL,
        [EnteredOn]      DATE          NOT NULL,
        [Id]             BIGINT        IDENTITY (1, 1) NOT NULL,
        [OrganizationId] INT           NOT NULL,
        CONSTRAINT [PK_Transactions]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Transactions_EnteredOn]
    ON [dbo].[Transactions] ([EnteredOn]);

-- Entries Table
CREATE TABLE [dbo].[Entries]
    (
        [Amount]        DECIMAL (10, 2) NOT NULL,
        [CreatedAt]     DATETIME2 (7)   NOT NULL,
        [AccountId]     BIGINT          NOT NULL,
        [TransactionId] BIGINT          NOT NULL,
        [Id]            BIGINT          IDENTITY (1, 1) NOT NULL,
        CONSTRAINT [PK_Entries]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Entries_TransactionId]
    ON [dbo].[Entries] ([TransactionId]);
