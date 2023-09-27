-- Checks Table
CREATE TABLE [dbo].[Checks]
    (
        [BankAccountId]  BIGINT          NOT NULL,
        [CreatedAt]      DATETIME2 (7)   NOT NULL,
        [Memo]           VARCHAR (60)    NOT NULL,
        [VendorId]       BIGINT          NOT NULL,
        [EnteredOn]      DATE            NOT NULL,
        [Id]             BIGINT          IDENTITY (1, 1) NOT NULL,
        [Number]         VARCHAR (20)    NOT NULL,
        [OrganizationId] INT             NOT NULL,
        [TotalAmount]    DECIMAL (12, 2) NOT NULL,
        [TransactionId]  BIGINT          NOT NULL,
        CONSTRAINT [PK_Checks]
            PRIMARY KEY CLUSTERED ([Id])
    );


-- Check Expense Lines Table
CREATE TABLE [dbo].[CheckExpenseLines]
    (
        [Amount]      DECIMAL (12, 2) NOT NULL,
        [AccountId]   BIGINT          NOT NULL,
        [CheckId]     BIGINT          NOT NULL,
        [CreatedAt]   DATETIME2 (7)   NOT NULL,
        [Description] VARCHAR (60)    NOT NULL,
        [EntryId]     BIGINT          NOT NULL,
        [Id]          BIGINT          IDENTITY (1, 1) NOT NULL,
        CONSTRAINT [PK_CheckExpenseLines]
            PRIMARY KEY CLUSTERED ([Id])
    );


-- Vendors Table
CREATE TABLE [dbo].[Vendors]
    (
        [CreatedAt]      DATETIME2 (7)  NOT NULL,
        [Description]    VARCHAR (3000) NULL,
        [Id]             BIGINT            IDENTITY (1, 1) NOT NULL,
        [Name]           VARCHAR (100)  NOT NULL,
        [Number]         VARCHAR (20)   NOT NULL,
        [OrganizationId] INT            NOT NULL,
        CONSTRAINT [PK_Vendors]
            PRIMARY KEY CLUSTERED ([Id])
    );
