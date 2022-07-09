UPDATE
    [dbo].[Punches]
SET
    [TaskId] = @DestinationTaskId
WHERE
    [TaskId] = @SourceTaskId;
