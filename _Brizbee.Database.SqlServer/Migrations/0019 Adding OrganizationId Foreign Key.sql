ALTER TABLE [dbo].[QBDInventoryItems]
ADD
    CONSTRAINT [FK_QBDInventoryItems_OrganizationId]
    FOREIGN KEY ([OrganizationId])
    REFERENCES [dbo].[Organizations] ([Id]) ON DELETE CASCADE ON UPDATE CASCADE;
