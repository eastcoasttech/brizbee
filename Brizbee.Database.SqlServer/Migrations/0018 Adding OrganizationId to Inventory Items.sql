
ALTER TABLE [dbo].[QBDInventoryItems]
	ADD [OrganizationId] INT NULL;

ALTER TABLE [dbo].[QBDInventoryItems]
    ADD CONSTRAINT [FK_OrganizationId] FOREIGN KEY ([OrganizationId])
		REFERENCES [dbo].[Organizations] ([Id])
		ON DELETE CASCADE
		ON UPDATE CASCADE;
