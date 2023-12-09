
-- ShowCustomerNumber column

ALTER TABLE [dbo].[Organizations]
	ADD [ShowCustomerNumber] BIT NOT NULL;

ALTER TABLE [dbo].[Organizations]
	ADD CONSTRAINT [DF_Organizations_ShowCustomerNumber]
	DEFAULT 1 FOR [ShowCustomerNumber];
	

-- ShowProjectNumber column

ALTER TABLE [dbo].[Organizations]
	ADD [ShowProjectNumber] BIT NOT NULL;

ALTER TABLE [dbo].[Organizations]
	ADD CONSTRAINT [DF_Organizations_ShowProjectNumber]
	DEFAULT 1 FOR [ShowProjectNumber];


-- ShowTaskNumber column

ALTER TABLE [dbo].[Organizations]
	ADD [ShowTaskNumber] BIT NOT NULL;

ALTER TABLE [dbo].[Organizations]
	ADD CONSTRAINT [DF_Organizations_ShowTaskNumber]
	DEFAULT 1 FOR [ShowTaskNumber];


-- SortCustomersByColumn columns

ALTER TABLE [dbo].[Organizations]
	ADD [SortCustomersByColumn] CHAR (6) NOT NULL;

ALTER TABLE [dbo].[Organizations]
	ADD CONSTRAINT [DF_Organizations_SortCustomersByColumn]
	DEFAULT 'Number' FOR [SortCustomersByColumn];


-- SortProjectsByColumn column
	
ALTER TABLE [dbo].[Organizations]
	ADD [SortProjectsByColumn] CHAR (6) NOT NULL;

ALTER TABLE [dbo].[Organizations]
	ADD CONSTRAINT [DF_Organizations_SortProjectsByColumn]
	DEFAULT 'Number' FOR [SortProjectsByColumn];
	

-- SortTasksByColumn column

ALTER TABLE [dbo].[Organizations]
	ADD [SortTasksByColumn] CHAR (6) NOT NULL;

ALTER TABLE [dbo].[Organizations]
	ADD CONSTRAINT [DF_Organizations_SortTasksByColumn]
	DEFAULT 'Number' FOR [SortTasksByColumn];
