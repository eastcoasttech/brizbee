

-- PopulateTemplates Table
CREATE TABLE [dbo].[PopulateTemplates] (
    [Id]                INT IDENTITY (1, 1) NOT NULL CONSTRAINT [PK_PopulateTemplates_Id] PRIMARY KEY,
    [CreatedAt]         DATETIME2 (7) NOT NULL,
    [OrganizationId]    INT NOT NULL,
    [Name]              NVARCHAR(100) NOT NULL,
    [Template]          NVARCHAR(MAX) CONSTRAINT [CK_PopulateTemplates_Template] CHECK (ISJSON([Template])=1) NOT NULL
);
GO
