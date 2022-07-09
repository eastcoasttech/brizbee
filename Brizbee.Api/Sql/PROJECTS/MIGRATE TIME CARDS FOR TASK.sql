UPDATE
    [dbo].[TimesheetEntries]
SET
    [TaskId] = @DestinationTaskId
WHERE
    [TaskId] = @SourceTaskId;
