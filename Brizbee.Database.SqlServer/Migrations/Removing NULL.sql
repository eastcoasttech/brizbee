
-- Customers Table

ALTER TABLE [dbo].[Customers]
	ALTER COLUMN [Description]					NVARCHAR (3000) NOT NULL;

ALTER TABLE [dbo].[Customers]
	ADD CONSTRAINT [DF_Customers_Description]
	DEFAULT '' FOR [Description];

-- Jobs Table

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [Description]					NVARCHAR (3000) NOT NULL;
	
ALTER TABLE [dbo].[Jobs]
	ADD CONSTRAINT [DF_Jobs_Description]
	DEFAULT '' FOR [Description];

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [CustomerWorkOrder]			NVARCHAR (50) NOT NULL;
	
ALTER TABLE [dbo].[Jobs]
	ADD CONSTRAINT [DF_Jobs_CustomerWorkOrder]
	DEFAULT '' FOR [CustomerWorkOrder];

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [CustomerPurchaseOrder]		NVARCHAR (50) NOT NULL;
	
ALTER TABLE [dbo].[Jobs]
	ADD CONSTRAINT [DF_Jobs_CustomerPurchaseOrder]
	DEFAULT '' FOR [CustomerPurchaseOrder];

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [InvoiceNumber]				NVARCHAR (50) NOT NULL;
	
ALTER TABLE [dbo].[Jobs]
	ADD CONSTRAINT [DF_Jobs_InvoiceNumber]
	DEFAULT '' FOR [InvoiceNumber];

ALTER TABLE [dbo].[Jobs]
	ALTER COLUMN [QuoteNumber]					NVARCHAR (50) NOT NULL;
	
ALTER TABLE [dbo].[Jobs]
	ADD CONSTRAINT [DF_Jobs_QuoteNumber]
	DEFAULT '' FOR [QuoteNumber];
