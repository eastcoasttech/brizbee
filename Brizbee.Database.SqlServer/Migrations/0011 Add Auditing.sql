

-- Punch Audits Table
CREATE TABLE [dbo].[PunchAudits] (
    [Id]                BIGINT IDENTITY (1, 1) NOT NULL CONSTRAINT [PK_PunchAudits_Id] PRIMARY KEY,
    [CreatedAt]         DATETIME2 (7) NOT NULL,
    [OrganizationId]    INT NOT NULL,
    [UserId]            INT NOT NULL,
    [ObjectId]          INT NOT NULL,
    [Action]            VARCHAR(10) NOT NULL,
    [Before]            NVARCHAR(MAX) CONSTRAINT [CK_PunchAudits_Before] CHECK (ISJSON([Before])=1) NOT NULL,
    [After]             NVARCHAR(MAX) CONSTRAINT [CK_PunchAudits_After] CHECK (ISJSON([After])=1) NOT NULL
);
GO

-- Time Card Audits Table
CREATE TABLE [dbo].[TimeCardAudits] (
    [Id]                BIGINT IDENTITY (1, 1) NOT NULL CONSTRAINT [PK_TimeCardAudits_Id] PRIMARY KEY,
    [CreatedAt]         DATETIME2 (7) NOT NULL,
    [OrganizationId]    INT NOT NULL,
    [UserId]            INT NOT NULL,
    [ObjectId]          INT NOT NULL,
    [Action]            VARCHAR(10) NOT NULL,
    [Before]            NVARCHAR(MAX) CONSTRAINT [CK_TimeCardAudits_Before] CHECK (ISJSON([Before])=1) NOT NULL,
    [After]             NVARCHAR(MAX) CONSTRAINT [CK_TimeCardAudits_After] CHECK (ISJSON([After])=1) NOT NULL
);
GO
