UPDATE
    [dbo].[QBDInventoryConsumptions]
SET
    [TaskId] = @DestinationTaskId
WHERE
    [TaskId] = @SourceTaskId
    AND [QBDInventoryConsumptionSyncId] IS NULL;
