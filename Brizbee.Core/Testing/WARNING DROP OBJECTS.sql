
DROP FUNCTION IF EXISTS [dbo].[tvf_DatesInRange];

-- Drop Tables
DROP TABLE IF EXISTS [dbo].[CalculatedWithholdings];
DROP TABLE IF EXISTS [dbo].[AvailableWithholdings];

DROP TABLE IF EXISTS [dbo].[CalculatedTaxations];
DROP TABLE IF EXISTS [dbo].[AvailableTaxations];

DROP TABLE IF EXISTS [dbo].[CalculatedDeductions];
DROP TABLE IF EXISTS [dbo].[AvailableDeductions];

DROP TABLE IF EXISTS [dbo].[Paychecks];

DROP TABLE IF EXISTS [dbo].[CheckExpenseLines];
DROP TABLE IF EXISTS [dbo].[Checks];
DROP TABLE IF EXISTS [dbo].[Deposits];
DROP TABLE IF EXISTS [dbo].[Payments];
DROP TABLE IF EXISTS [dbo].[LineItems];
DROP TABLE IF EXISTS [dbo].[Invoices];

DROP TABLE IF EXISTS [dbo].[Entries];
DROP TABLE IF EXISTS [dbo].[Transactions];
DROP TABLE IF EXISTS [dbo].[Accounts];
DROP TABLE IF EXISTS [dbo].[Vendors];

DROP TABLE IF EXISTS [dbo].[Punches];
DROP TABLE IF EXISTS [dbo].[PunchAudits];

DROP TABLE IF EXISTS [dbo].[TimesheetEntries];
DROP TABLE IF EXISTS [dbo].[TimeCardAudits];

DROP TABLE IF EXISTS [dbo].[Tasks];
DROP TABLE IF EXISTS [dbo].[Jobs];
DROP TABLE IF EXISTS [dbo].[Customers];

DROP TABLE IF EXISTS [dbo].[QBDInventoryConsumptionSyncs];
DROP TABLE IF EXISTS [dbo].[QBDInventoryConsumptions];

DROP TABLE IF EXISTS [dbo].[QBDInventoryItemSyncs];
DROP TABLE IF EXISTS [dbo].[QBDInventoryItems];

DROP TABLE IF EXISTS [dbo].[QBDInventorySites];
DROP TABLE IF EXISTS [dbo].[QBDUnitOfMeasureSets];

DROP TABLE IF EXISTS [dbo].[QuickBooksDesktopExports];
DROP TABLE IF EXISTS [dbo].[QuickBooksOnlineExports];

DROP TABLE IF EXISTS [dbo].[Rates];

DROP TABLE IF EXISTS [dbo].[TaskTemplates];
DROP TABLE IF EXISTS [dbo].[PopulateTemplates];

DROP TABLE IF EXISTS [dbo].[Commits];
DROP TABLE IF EXISTS [dbo].[Users];
DROP TABLE IF EXISTS [dbo].[Organizations];

DROP TABLE IF EXISTS [dbo].[SchemaVersions];
