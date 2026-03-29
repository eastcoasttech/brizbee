-- Account Balance Function
CREATE OR ALTER FUNCTION [udf_AccountBalance]
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
