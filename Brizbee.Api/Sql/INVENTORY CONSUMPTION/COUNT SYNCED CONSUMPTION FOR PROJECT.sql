SELECT
    COUNT( * )
FROM [dbo].[QBDInventoryConsumptions] AS [C]
INNER JOIN [dbo].[Tasks] AS [T] ON
    [T].[Id] = [C].[TaskId]
WHERE
    [T].[JobId] = @ProjectId
    AND [C].[QBDInventoryConsumptionSyncId] IS NOT NULL;
