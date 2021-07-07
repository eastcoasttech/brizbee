
-- Commits Table
CREATE TABLE [dbo].[Commits] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [QuickBooksExportedAt] datetime2(7) NULL,
    [Guid] uniqueidentifier NOT NULL,
    [InAt] datetime2(7) NOT NULL,
    [OrganizationId] int NOT NULL,
    [OutAt] datetime2(7) NOT NULL,
    [PunchCount] int NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_dbo.Commits] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- Customers Table
CREATE TABLE [dbo].[Customers] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Name] nvarchar(max) NOT NULL,
    [Number] nvarchar(max) NOT NULL,
    [OrganizationId] int NOT NULL,
    CONSTRAINT [PK_dbo.Customers] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- Jobs Table
CREATE TABLE [dbo].[Jobs] (
    [Id]                            INT IDENTITY (1, 1) NOT NULL,
    [CreatedAt]                     DATETIME2(7) NOT NULL,
    [CustomerId]                    INT NOT NULL,
    [Description]                   NVARCHAR(MAX) NULL,
    [Name]                          NVARCHAR(MAX) NOT NULL,
    [Number]                        NVARCHAR(MAX) NOT NULL,
    [QuickBooksCustomerJob]         NVARCHAR(MAX) NULL,
    [Status]                        NVARCHAR (20) NULL,
    [CustomerWorkOrder]             NVARCHAR (50) NULL,
    [CustomerPurchaseOrder]         NVARCHAR (50) NULL,
    [InvoiceNumber]                 NVARCHAR (50) NULL,
    [QuoteNumber]                   NVARCHAR (50) NULL,
    CONSTRAINT [PK_dbo.Jobs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- Organizations Table
CREATE TABLE [dbo].[Organizations] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [MinutesFormat] nvarchar(max) NULL,
    [Name] nvarchar(max) NOT NULL,
    [Code] nvarchar(8) NOT NULL,
    [StripeCustomerId] nvarchar(max) NOT NULL,
    [StripeSourceCardBrand] nvarchar(max) NULL,
    [StripeSourceCardExpirationMonth] nvarchar(max) NULL,
    [StripeSourceCardExpirationYear] nvarchar(max) NULL,
    [StripeSourceCardLast4] nvarchar(max) NULL,
    [StripeSourceId] nvarchar(max) NULL,
    [StripeSubscriptionId] nvarchar(max) NOT NULL,
    [Groups] VARCHAR (200) NULL,
    [PlanId] int NOT NULL DEFAULT (0),
    CONSTRAINT [PK_dbo.Organizations] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- Punches Table
CREATE TABLE [dbo].[Punches] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CommitId] int NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [Guid] uniqueidentifier NOT NULL,
    [InAt] datetime2(7) NOT NULL,
    [InAtTimeZone] nvarchar(max) NULL,
    [LatitudeForInAt] nvarchar(20) NULL,
    [LongitudeForInAt] nvarchar(20) NULL,
    [LatitudeForOutAt] nvarchar(20) NULL,
    [LongitudeForOutAt] nvarchar(20) NULL,
    [OutAt] datetime2(7) NULL,
    [OutAtTimeZone] nvarchar(max) NULL,
    [SourceForInAt] nvarchar(max) NULL,
    [SourceForOutAt] nvarchar(max) NULL,
    [TaskId] int NOT NULL,
    [UserId] int NOT NULL,
    [InAtSourceHardware] nvarchar(12) NULL,
    [InAtSourceHostname] nvarchar(30) NULL,
    [InAtSourceIpAddress] nvarchar(30) NULL,
    [InAtSourceOperatingSystem] nvarchar(30) NULL,
    [InAtSourceOperatingSystemVersion] nvarchar(20) NULL,
    [InAtSourceBrowser] nvarchar(30) NULL,
    [InAtSourceBrowserVersion] nvarchar(20) NULL,
    [InAtSourcePhoneNumber] nvarchar(30) NULL,
    [OutAtSourceHardware] nvarchar(12) NULL,
    [OutAtSourceHostname] nvarchar(30) NULL,
    [OutAtSourceIpAddress] nvarchar(30) NULL,
    [OutAtSourceOperatingSystem] nvarchar(30) NULL,
    [OutAtSourceOperatingSystemVersion] nvarchar(20) NULL,
    [OutAtSourceBrowser] nvarchar(30) NULL,
    [OutAtSourceBrowserVersion] nvarchar(20) NULL,
    [OutAtSourcePhoneNumber] nvarchar(30) NULL,
    [ServiceRateId] int NULL,
    [PayrollRateId] int NULL,
    CONSTRAINT [PK_dbo.Punches] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- QuickBooksDesktopExports Table
CREATE TABLE [dbo].[QuickBooksDesktopExports] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CommitId] int NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [InAt] datetime2(7) NULL,
    [OutAt] datetime2(7) NULL,
    [QBProductName] nvarchar(100) NULL,
    [QBMajorVersion] nvarchar(10) NULL,
    [QBMinorVersion] nvarchar(10) NULL,
    [QBCountry] nvarchar(10) NULL,
    [QBSupportedQBXMLVersions] nvarchar(100) NULL,
    [UserId] int NOT NULL,
    [Log] nvarchar(max) NULL,
    [TxnIDs]                        VARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.QuickBooksDesktopExports] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- QuickBooksOnlineExports Table
CREATE TABLE [dbo].[QuickBooksOnlineExports] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CommitId] int NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [InAt] datetime2(7) NULL,
    [OutAt] datetime2(7) NULL,
    [CreatedByUserId] int NOT NULL,
    [CreatedTimeActivitiesIds] nvarchar(max) NULL,
    [ReversedAt] datetime2(7) NULL,
    [ReversedByUserId] int NULL,
    CONSTRAINT [PK_dbo.QuickBooksOnlineExports] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- Rates Table
CREATE TABLE [dbo].[Rates] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [IsDeleted] [bit] NOT NULL,
    [Name] nvarchar(40) NULL,
    [OrganizationId] int NOT NULL,
    [ParentRateId] int NULL,
    [QBDPayrollItem] nvarchar(max) NULL,
    [QBDServiceItem] nvarchar(max) NULL,
    [QBOPayrollItem] nvarchar(max) NULL,
    [QBOServiceItem] nvarchar(max) NULL,
    [Type] nvarchar(20) NULL,
    CONSTRAINT [PK_dbo.Rates] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- Tasks Table
CREATE TABLE [dbo].[Tasks] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [JobId] int NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Number] nvarchar(max) NOT NULL,
    [QuickBooksPayrollItem] nvarchar(max) NULL,
    [QuickBooksServiceItem] nvarchar(max) NULL,
    [BaseServiceRateId] int NULL,
    [BasePayrollRateId] int NULL,
    [Group] VARCHAR(20) NULL,
    [Order] INT NOT NULL,
    CONSTRAINT [PK_dbo.Tasks] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- TaskTemplates Table
CREATE TABLE [dbo].[TaskTemplates] (
    [Id]                INT IDENTITY (1, 1) NOT NULL,
    [CreatedAt]         DATETIME2 (7) NOT NULL,
    [OrganizationId]    INT NOT NULL,
    [Name]              NVARCHAR(MAX) NULL,
    [Template]          NVARCHAR(MAX) CONSTRAINT [CT_TaskTemplates_Template] CHECK (ISJSON([Template])=1) NOT NULL,
    CONSTRAINT [PK_dbo.TaskTemplates] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- TimesheetEntries
CREATE TABLE [dbo].[TimesheetEntries] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [EnteredAt] datetime2(7) NOT NULL,
    [Minutes] int NOT NULL,
    [Notes] nvarchar(max) NULL,
    [TaskId] int NOT NULL,
    [UserId] int NOT NULL,
    CONSTRAINT [PK_dbo.TimesheetEntries] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO


-- Users Table
CREATE TABLE [dbo].[Users] (
    [Id] int IDENTITY (1, 1) NOT NULL,
    [CreatedAt] datetime2(7) NOT NULL,
    [EmailAddress] nvarchar(254) NULL,
    [IsDeleted] [bit] NOT NULL,
    [Name] nvarchar(128) NOT NULL,
    [OrganizationId] int NOT NULL,
    [PasswordSalt] nvarchar(max) NULL,
    [PasswordHash] nvarchar(max) NULL,
    [Pin] nvarchar(max) NOT NULL,
    [QuickBooksEmployee] nvarchar(max) NULL,
    [Role] nvarchar(128) NOT NULL,
    [TimeZone] nvarchar(max) NOT NULL,
    [UsesMobileClock] [bit] NOT NULL,
    [UsesTouchToneClock] [bit] NOT NULL,
    [UsesWebClock] [bit] NOT NULL,
    [UsesTimesheets] [bit] NOT NULL,
    [RequiresLocation] [bit] NOT NULL DEFAULT (0),
    [RequiresPhoto] [bit] NOT NULL DEFAULT (0),
    [QBOGivenName] nvarchar(max) NULL,
    [QBOMiddleName] nvarchar(max) NULL,
    [QBOFamilyName] nvarchar(max) NULL,
    CONSTRAINT [PK_dbo.Users] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO






ALTER TABLE [dbo].[Commits] WITH CHECK ADD CONSTRAINT [FK_dbo.Commits_dbo.Organizations_OrganizationId] FOREIGN KEY ([OrganizationId])
REFERENCES [dbo].[Organizations] ([Id])
GO
ALTER TABLE [dbo].[Commits] CHECK CONSTRAINT [FK_dbo.Commits_dbo.Organizations_OrganizationId]
GO
ALTER TABLE [dbo].[Commits] WITH CHECK ADD CONSTRAINT [FK_dbo.Commits_dbo.Users_UserId] FOREIGN KEY ([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Commits] CHECK CONSTRAINT [FK_dbo.Commits_dbo.Users_UserId]
GO
ALTER TABLE [dbo].[Customers] WITH CHECK ADD CONSTRAINT [FK_dbo.Customers_dbo.Organizations_OrganizationId] FOREIGN KEY ([OrganizationId])
REFERENCES [dbo].[Organizations] ([Id])
GO
ALTER TABLE [dbo].[Customers] CHECK CONSTRAINT [FK_dbo.Customers_dbo.Organizations_OrganizationId]
GO
ALTER TABLE [dbo].[Jobs] WITH CHECK ADD CONSTRAINT [FK_dbo.Jobs_dbo.Customers_CustomerId] FOREIGN KEY ([CustomerId])
REFERENCES [dbo].[Customers] ([Id])
GO
ALTER TABLE [dbo].[Jobs] CHECK CONSTRAINT [FK_dbo.Jobs_dbo.Customers_CustomerId]
GO
ALTER TABLE [dbo].[Punches] WITH CHECK ADD CONSTRAINT [FK_dbo.Punches_dbo.Commits_CommitId] FOREIGN KEY ([CommitId])
REFERENCES [dbo].[Commits] ([Id])
GO
ALTER TABLE [dbo].[Punches] CHECK CONSTRAINT [FK_dbo.Punches_dbo.Commits_CommitId]
GO
ALTER TABLE [dbo].[Punches] WITH CHECK ADD CONSTRAINT [FK_dbo.Punches_dbo.Rates_PayrollRateId] FOREIGN KEY ([PayrollRateId])
REFERENCES [dbo].[Rates] ([Id])
GO
ALTER TABLE [dbo].[Punches] CHECK CONSTRAINT [FK_dbo.Punches_dbo.Rates_PayrollRateId]
GO
ALTER TABLE [dbo].[Punches] WITH CHECK ADD CONSTRAINT [FK_dbo.Punches_dbo.Rates_ServiceRateId] FOREIGN KEY ([ServiceRateId])
REFERENCES [dbo].[Rates] ([Id])
GO
ALTER TABLE [dbo].[Punches] CHECK CONSTRAINT [FK_dbo.Punches_dbo.Rates_ServiceRateId]
GO
ALTER TABLE [dbo].[Punches] WITH CHECK ADD CONSTRAINT [FK_dbo.Punches_dbo.Tasks_TaskId] FOREIGN KEY ([TaskId])
REFERENCES [dbo].[Tasks] ([Id])
GO
ALTER TABLE [dbo].[Punches] CHECK CONSTRAINT [FK_dbo.Punches_dbo.Tasks_TaskId]
GO
ALTER TABLE [dbo].[Punches] WITH CHECK ADD CONSTRAINT [FK_dbo.Punches_dbo.Users_UserId] FOREIGN KEY ([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[Punches] CHECK CONSTRAINT [FK_dbo.Punches_dbo.Users_UserId]
GO
ALTER TABLE [dbo].[QuickBooksDesktopExports] WITH CHECK ADD CONSTRAINT [FK_dbo.QuickBooksDesktopExports_dbo.Commits_CommitId] FOREIGN KEY ([CommitId])
REFERENCES [dbo].[Commits] ([Id])
GO
ALTER TABLE [dbo].[QuickBooksDesktopExports] CHECK CONSTRAINT [FK_dbo.QuickBooksDesktopExports_dbo.Commits_CommitId]
GO
ALTER TABLE [dbo].[QuickBooksDesktopExports] WITH CHECK ADD CONSTRAINT [FK_dbo.QuickBooksDesktopExports_dbo.Users_UserId] FOREIGN KEY ([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[QuickBooksDesktopExports] CHECK CONSTRAINT [FK_dbo.QuickBooksDesktopExports_dbo.Users_UserId]
GO
ALTER TABLE [dbo].[QuickBooksOnlineExports] WITH CHECK ADD CONSTRAINT [FK_dbo.QuickBooksOnlineExports_dbo.Commits_CommitId] FOREIGN KEY ([CommitId])
REFERENCES [dbo].[Commits] ([Id])
GO
ALTER TABLE [dbo].[QuickBooksOnlineExports] CHECK CONSTRAINT [FK_dbo.QuickBooksOnlineExports_dbo.Commits_CommitId]
GO
ALTER TABLE [dbo].[QuickBooksOnlineExports] WITH CHECK ADD CONSTRAINT [FK_dbo.QuickBooksOnlineExports_dbo.Users_ReversedByUserId] FOREIGN KEY ([ReversedByUserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[QuickBooksOnlineExports] CHECK CONSTRAINT [FK_dbo.QuickBooksOnlineExports_dbo.Users_ReversedByUserId]
GO
ALTER TABLE [dbo].[QuickBooksOnlineExports] WITH CHECK ADD CONSTRAINT [FK_dbo.QuickBooksOnlineExports_dbo.Users_UserId] FOREIGN KEY ([CreatedByUserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[QuickBooksOnlineExports] CHECK CONSTRAINT [FK_dbo.QuickBooksOnlineExports_dbo.Users_UserId]
GO
ALTER TABLE [dbo].[Rates] WITH CHECK ADD CONSTRAINT [FK_dbo.Rates_dbo.Organizations_OrganizationId] FOREIGN KEY ([OrganizationId])
REFERENCES [dbo].[Organizations] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Rates] CHECK CONSTRAINT [FK_dbo.Rates_dbo.Organizations_OrganizationId]
GO
ALTER TABLE [dbo].[Rates] WITH CHECK ADD CONSTRAINT [FK_dbo.Rates_dbo.Rates_ParentRateId] FOREIGN KEY ([ParentRateId])
REFERENCES [dbo].[Rates] ([Id])
GO
ALTER TABLE [dbo].[Rates] CHECK CONSTRAINT [FK_dbo.Rates_dbo.Rates_ParentRateId]
GO
ALTER TABLE [dbo].[Tasks] WITH CHECK ADD CONSTRAINT [FK_dbo.Tasks_dbo.Jobs_JobId] FOREIGN KEY ([JobId])
REFERENCES [dbo].[Jobs] ([Id])
GO
ALTER TABLE [dbo].[Tasks] CHECK CONSTRAINT [FK_dbo.Tasks_dbo.Jobs_JobId]
GO
ALTER TABLE [dbo].[Tasks] WITH CHECK ADD CONSTRAINT [FK_dbo.Tasks_dbo.Rates_BasePayrollRateId] FOREIGN KEY ([BasePayrollRateId])
REFERENCES [dbo].[Rates] ([Id])
GO
ALTER TABLE [dbo].[Tasks] CHECK CONSTRAINT [FK_dbo.Tasks_dbo.Rates_BasePayrollRateId]
GO
ALTER TABLE [dbo].[Tasks] WITH CHECK ADD CONSTRAINT [FK_dbo.Tasks_dbo.Rates_BaseServiceRateId] FOREIGN KEY ([BaseServiceRateId])
REFERENCES [dbo].[Rates] ([Id])
GO
ALTER TABLE [dbo].[Tasks] CHECK CONSTRAINT [FK_dbo.Tasks_dbo.Rates_BaseServiceRateId]
GO
ALTER TABLE [dbo].[TaskTemplates] WITH CHECK ADD CONSTRAINT [FK_dbo.TaskTemplates_dbo.Organizations_OrganizationId] FOREIGN KEY ([OrganizationId])
REFERENCES [dbo].[Organizations] ([Id])
GO
ALTER TABLE [dbo].[TaskTemplates] CHECK CONSTRAINT [FK_dbo.TaskTemplates_dbo.Organizations_OrganizationId]
GO
ALTER TABLE [dbo].[TimesheetEntries] WITH CHECK ADD CONSTRAINT [FK_dbo.TimesheetEntries_dbo.Tasks_TaskId] FOREIGN KEY ([TaskId])
REFERENCES [dbo].[Tasks] ([Id])
GO
ALTER TABLE [dbo].[TimesheetEntries] CHECK CONSTRAINT [FK_dbo.TimesheetEntries_dbo.Tasks_TaskId]
GO
ALTER TABLE [dbo].[TimesheetEntries] WITH CHECK ADD CONSTRAINT [FK_dbo.TimesheetEntries_dbo.Users_UserId] FOREIGN KEY ([UserId])
REFERENCES [dbo].[Users] ([Id])
GO
ALTER TABLE [dbo].[TimesheetEntries] CHECK CONSTRAINT [FK_dbo.TimesheetEntries_dbo.Users_UserId]
GO
ALTER TABLE [dbo].[Users] WITH CHECK ADD CONSTRAINT [FK_dbo.Users_dbo.Organizations_OrganizationId] FOREIGN KEY ([OrganizationId])
REFERENCES [dbo].[Organizations] ([Id])
GO
ALTER TABLE [dbo].[Users] CHECK CONSTRAINT [FK_dbo.Users_dbo.Organizations_OrganizationId]
GO