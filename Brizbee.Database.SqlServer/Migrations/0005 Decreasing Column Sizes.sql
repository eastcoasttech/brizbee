

-- Number Columns
ALTER TABLE [dbo].[Customers]
	ALTER COLUMN [Number] NVARCHAR (10) NOT NULL;

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [Number] NVARCHAR (10) NOT NULL;

ALTER TABLE [dbo].[Tasks]
	ALTER COLUMN [Number] NVARCHAR (10) NOT NULL;


-- Description Columns
ALTER TABLE [dbo].[Customers]
	ALTER COLUMN [Description] NVARCHAR (3000) NULL;

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [Description] NVARCHAR (3000) NULL;

	
-- Name Columns
ALTER TABLE [dbo].[Customers]
	ALTER COLUMN [Name] NVARCHAR (100) NOT NULL;

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [Name] NVARCHAR (100) NOT NULL;

ALTER TABLE [dbo].[Tasks]
	ALTER COLUMN [Name] NVARCHAR (100) NOT NULL;


-- QuickBooksCustomerJob Column
ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [QuickBooksCustomerJob] NVARCHAR (159) NULL;

	
-- Organization Name Column
ALTER TABLE [dbo].[Organizations]
	ALTER COLUMN [Name] NVARCHAR (100) NOT NULL;
	
	
-- Organization MinutesFormat Column
ALTER TABLE [dbo].[Organizations]
	ALTER COLUMN [MinutesFormat] CHAR (7) NULL;


-- Rate Columns
ALTER TABLE [dbo].[Rates]
	ALTER COLUMN [QBDPayrollItem] NVARCHAR (31) NULL;

ALTER TABLE [dbo].[Rates]
	ALTER COLUMN [QBDServiceItem] NVARCHAR (31) NULL;

ALTER TABLE [dbo].[Rates]
	ALTER COLUMN [QBOPayrollItem] NVARCHAR (31) NULL;

ALTER TABLE [dbo].[Rates]
	ALTER COLUMN [QBOServiceItem] NVARCHAR (31) NULL;
