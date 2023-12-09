
-- Merge Projects Permission

ALTER TABLE [dbo].[Users]
ADD
    [CanMergeProjects] BIT NULL CONSTRAINT [DF_Users_CanMergeProjects]
                                DEFAULT 0;
