DROP FUNCTION IF EXISTS [tvf_DatesInRange];
GO

CREATE FUNCTION [tvf_DatesInRange]
    (
        @MinDate DATETIME,
        @MaxDate DATETIME
    )
RETURNS TABLE
AS
    RETURN
    (
        WITH [GetDates] AS
        (
            SELECT
                DATEADD( DAY, 1, @MinDate ) AS [TheDate]
            UNION ALL
            SELECT
                DATEADD( DAY, 1, [TheDate] )
            FROM [GetDates]
            WHERE
                [TheDate] < @MaxDate
        )
        SELECT
            [TheDate]
        FROM [GetDates]
    );
GO
