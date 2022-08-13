-- Accounts Table
CREATE TABLE [dbo].[Accounts]
    (
        [CreatedAt]      DATETIME2 (7) NOT NULL,
        [Description]    VARCHAR (120) NOT NULL,
        [Id]             BIGINT        IDENTITY (1, 1) NOT NULL,
        [Name]           VARCHAR (40)  NOT NULL,
        [Number]         INT           NOT NULL,
        [OrganizationId] INT           NOT NULL,
        [Type]           VARCHAR (30)  NOT NULL,
        CONSTRAINT [PK_Accounts]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Accounts_Name]
    ON [dbo].[Accounts] ([Name]);

ALTER TABLE [dbo].[Accounts]
ADD
    [NormalBalance] AS (CASE
                            WHEN [Type] = 'Bank' THEN
                                'DEBIT'
                            WHEN [Type] = 'Accounts Receivable' THEN
                                'DEBIT'
                            WHEN [Type] = 'Other Current Asset' THEN
                                'DEBIT'
                            WHEN [Type] = 'Fixed Asset' THEN
                                'DEBIT'
                            WHEN [Type] = 'Other Asset' THEN
                                'DEBIT'
                            WHEN [Type] = 'Expense' THEN
                                'DEBIT'
                            WHEN [Type] = 'Other Expense' THEN
                                'DEBIT'
                            WHEN [Type] = 'Accounts Payable' THEN
                                'CREDIT'
                            WHEN [Type] = 'Credit Card' THEN
                                'CREDIT'
                            WHEN [Type] = 'Other Current Liability' THEN
                                'CREDIT'
                            WHEN [Type] = 'Long Term Liability' THEN
                                'CREDIT'
                            WHEN [Type] = 'Equity' THEN
                                'CREDIT'
                            WHEN [Type] = 'Income' THEN
                                'CREDIT'
                            WHEN [Type] = 'Cost of Goods Sold' THEN
                                'CREDIT'
                            WHEN [Type] = 'Other Income' THEN
                                'CREDIT'
                            ELSE
                                'CREDIT'
                        END
                       ) PERSISTED;


-- Transactions Table
CREATE TABLE [dbo].[Transactions]
    (
        [CreatedAt]       DATETIME2 (7) NOT NULL,
        [Description]     VARCHAR (60)  NOT NULL,
        [ReferenceNumber] VARCHAR (20)  NOT NULL,
        [EnteredOn]       DATE          NOT NULL,
        [Id]              BIGINT        IDENTITY (1, 1) NOT NULL,
        [OrganizationId]  INT           NOT NULL,
        CONSTRAINT [PK_Transactions]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Transactions_OrganizationIdEnteredOn]
    ON [dbo].[Transactions] ([OrganizationId], [EnteredOn])
    INCLUDE ([ReferenceNumber], [Description]);


-- Entries Table
CREATE TABLE [dbo].[Entries]
    (
        [Amount]        DECIMAL (12, 2) NOT NULL,
        [CreatedAt]     DATETIME2 (7)   NOT NULL,
        [Description]   VARCHAR (60)    NOT NULL,
        [AccountId]     BIGINT          NOT NULL,
        [TransactionId] BIGINT          NOT NULL,
        [Type]          CHAR (1)        NOT NULL,
        [Id]            BIGINT          IDENTITY (1, 1) NOT NULL,
        CONSTRAINT [PK_Entries]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Entries_TransactionId]
    ON [dbo].[Entries] ([TransactionId])
    INCLUDE ([AccountId], [Type], [Amount]);

CREATE NONCLUSTERED INDEX [IX_Entries_AccountId]
    ON [dbo].[Entries] ([AccountId])
    INCLUDE ([TransactionId], [Type], [Amount]);


-- Invoices Table
CREATE TABLE [dbo].[Invoices]
    (
        [CreatedAt]      DATETIME2 (7)   NOT NULL,
        [CustomerId]     INT             NOT NULL,
        [Id]             BIGINT          IDENTITY (1, 1) NOT NULL,
        [Number]         VARCHAR (20)    NOT NULL,
        [OrganizationId] INT             NOT NULL,
        [TotalAmount]    DECIMAL (12, 2) NOT NULL,
        [EnteredOn]      DATE            NOT NULL,
        [TransactionId]  BIGINT          NOT NULL,
        CONSTRAINT [PK_Invoices]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Invoices_OrganizationId]
    ON [dbo].[Invoices] ([OrganizationId])
    INCLUDE ([CustomerId], [TransactionId], [Number], [TotalAmount], [EnteredOn]);

CREATE NONCLUSTERED INDEX [IX_Invoices_CustomerId]
    ON [dbo].[Invoices] ([CustomerId])
    INCLUDE ([OrganizationId], [TransactionId], [Number], [TotalAmount], [EnteredOn]);

CREATE NONCLUSTERED INDEX [IX_Invoices_TransactionId]
    ON [dbo].[Invoices] ([TransactionId])
    INCLUDE ([OrganizationId], [CustomerId], [Number], [TotalAmount], [EnteredOn]);


-- LineItems Table
CREATE TABLE [dbo].[LineItems]
    (
        [CreatedAt]   DATETIME2 (7)   NOT NULL,
        [Description] VARCHAR (120)   NOT NULL,
        [InvoiceId]   BIGINT          NOT NULL,
        [Id]          BIGINT          IDENTITY (1, 1) NOT NULL,
        [Quantity]    DECIMAL (12, 2) NOT NULL,
        [TotalAmount] DECIMAL (12, 2) NOT NULL,
        [UnitAmount]  DECIMAL (12, 2) NOT NULL,
        CONSTRAINT [PK_LineItems]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_LineItems_InvoiceId]
    ON [dbo].[LineItems] ([InvoiceId])
    INCLUDE ([Description], [Quantity], [TotalAmount], [UnitAmount]);


-- Payments Table
CREATE TABLE [dbo].[Payments]
    (
        [Amount]          DECIMAL (12, 2) NOT NULL,
        [CreatedAt]       DATETIME2 (7)   NOT NULL,
        [EnteredOn]       DATE            NOT NULL,
        [InvoiceId]       BIGINT          NOT NULL,
        [Id]              BIGINT          IDENTITY (1, 1) NOT NULL,
        [ReferenceNumber] VARCHAR (20)    NOT NULL,
        [TransactionId]   BIGINT          NOT NULL,
        CONSTRAINT [PK_Payments]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Payments_InvoiceId]
    ON [dbo].[Payments] ([InvoiceId])
    INCLUDE ([Amount], [EnteredOn], [ReferenceNumber], [TransactionId]);

CREATE NONCLUSTERED INDEX [IX_Payments_TransactionId]
    ON [dbo].[Payments] ([TransactionId])
    INCLUDE ([Amount], [EnteredOn], [ReferenceNumber], [InvoiceId]);


-- Deposits Table
CREATE TABLE [dbo].[Deposits]
    (
        [Amount]          DECIMAL (12, 2) NOT NULL,
        [BankAccountId]   BIGINT          NOT NULL,
        [CreatedAt]       DATETIME2 (7)   NOT NULL,
        [EnteredOn]       DATE            NOT NULL,
        [Id]              BIGINT          IDENTITY (1, 1) NOT NULL,
        [ReferenceNumber] VARCHAR (20)    NOT NULL,
        [TransactionId]   BIGINT          NOT NULL,
        CONSTRAINT [PK_Deposits]
            PRIMARY KEY CLUSTERED ([Id])
    );

CREATE NONCLUSTERED INDEX [IX_Deposits_BankAccountId]
    ON [dbo].[Deposits] ([BankAccountId])
    INCLUDE ([Amount], [EnteredOn], [ReferenceNumber], [TransactionId]);
