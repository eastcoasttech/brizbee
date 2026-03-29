CREATE NONCLUSTERED INDEX [IX_OrganizationIdCreatedAtObjectId] ON [dbo].[PunchAudits]
(
    [OrganizationId],
    [CreatedAt],
    [ObjectId]
) ON [PRIMARY]
GO
