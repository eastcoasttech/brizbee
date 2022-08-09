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
GO

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

CREATE NONCLUSTERED INDEX [IX_Transactions_OrganizationId]
    ON [dbo].[Transactions] ([OrganizationId], [EnteredOn])
    INCLUDE ([ReferenceNumber], [Description]);
GO


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
GO

CREATE NONCLUSTERED INDEX [IX_Entries_AccountId]
    ON [dbo].[Entries] ([AccountId])
    INCLUDE ([TransactionId], [Type], [Amount]);
GO


-- Account Balance Function
DROP FUNCTION IF EXISTS [udf_AccountBalance];
GO

CREATE FUNCTION [udf_AccountBalance]
    (
        @MinDate   DATETIME,
        @MaxDate   DATETIME,
        @AccountId BIGINT
    )
RETURNS DECIMAL (12, 2)
AS
    BEGIN
        DECLARE @CreditSum DECIMAL (12, 2);
        DECLARE @DebitSum DECIMAL (12, 2);
        DECLARE @Balance DECIMAL (12, 2);
        DECLARE @NormalBalance CHAR (6);

        -- Sum all of the credits.
        SELECT
            @CreditSum = SUM( [E].[Amount] )
        FROM [dbo].[Entries] AS [E]
        INNER JOIN [dbo].[Transactions] AS [T] ON
            [T].[Id] = [E].[TransactionId]
        WHERE
            [E].[AccountId] = @AccountId
            AND [T].[EnteredOn] BETWEEN @MinDate AND @MaxDate
            AND [E].[Type] = 'C';

        -- Sum all of the debits.
        SELECT
            @DebitSum = SUM( [E].[Amount] )
        FROM [dbo].[Entries] AS [E]
        INNER JOIN [dbo].[Transactions] AS [T] ON
            [T].[Id] = [E].[TransactionId]
        WHERE
            [E].[AccountId] = @AccountId
            AND [T].[EnteredOn] BETWEEN @MinDate AND @MaxDate
            AND [E].[Type] = 'D';

        -- Determine the normal balance depending on type of account.
        SELECT
            @NormalBalance = [NormalBalance]
        FROM [dbo].[Accounts]
        WHERE
            [Id] = @AccountId;
            
        -- Remove nulls from calculation.
        IF @CreditSum IS NULL
            SET @CreditSum = 0;
            
        IF @DebitSum IS NULL
            SET @DebitSum = 0;

        -- Direction of subtraction depends on normal balance.
        IF @NormalBalance = 'CREDIT'
            SELECT
                @Balance = @CreditSum - @DebitSum;
        ELSE
            SELECT
                @Balance = @DebitSum - @CreditSum;

        RETURN @Balance;
    END;
GO
