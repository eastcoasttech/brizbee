
-- Adjust column length to allow indexing
ALTER TABLE [dbo].[Users] ALTER COLUMN [PasswordSalt] CHAR(128) NULL;


ALTER TABLE [dbo].[Users] ALTER COLUMN [PasswordHash] CHAR(128) NULL;


ALTER TABLE [dbo].[Users] ALTER COLUMN [Pin] VARCHAR(12) NOT NULL;


-- Indexes to improve performance of users and authentication endpoints
CREATE NONCLUSTERED INDEX [IX_OrganizationIdIsDeletedName]
    ON [dbo].[Users] ([OrganizationId], [IsDeleted], [Name]);


CREATE NONCLUSTERED INDEX [IX_IsDeletedIsActiveEmailAddressPasswordSaltPasswordHash]
    ON [dbo].[Users] ([IsDeleted], [IsActive], [EmailAddress], [Id], [PasswordSalt], [PasswordHash]);
    

CREATE NONCLUSTERED INDEX [IX_IsDeletedIsActiveOrganizationIdPinId]
    ON [dbo].[Users] ([IsDeleted], [IsActive], [OrganizationId], [Pin], [Id]);
