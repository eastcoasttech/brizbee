-- Index to prevent multiple open punches
CREATE UNIQUE NONCLUSTERED INDEX [IX_UserIdOutAt]
    ON [dbo].[Punches] ([UserId], [OutAt])
    WHERE [OutAt] IS NULL;