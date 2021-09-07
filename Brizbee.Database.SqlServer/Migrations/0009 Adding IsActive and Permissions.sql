
-- IsActive

ALTER TABLE [dbo].[Users]
	ADD [IsActive] BIT NULL;

ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_IsActive]
	DEFAULT 1 FOR [IsActive];
	

-- View Punches Permission

ALTER TABLE [dbo].[Users]
	ADD [CanViewPunches] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewPunches]
	DEFAULT 0 FOR [CanViewPunches];
	

-- Create Punches Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreatePunches] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreatePunches]
	DEFAULT 0 FOR [CanCreatePunches];
	

-- Modify Punches Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyPunches] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyPunches]
	DEFAULT 0 FOR [CanModifyPunches];


-- Delete Punches Permission

ALTER TABLE [dbo].[Users]
	ADD [CanDeletePunches] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeletePunches]
	DEFAULT 0 FOR [CanDeletePunches];
	

-- Split and Populate Punches Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanSplitAndPopulatePunches] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanSplitAndPopulatePunches]
	DEFAULT 0 FOR [CanSplitAndPopulatePunches];


-- View Reports Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewReports] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewReports]
	DEFAULT 0 FOR [CanViewReports];


-- View Locks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewLocks] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewLocks]
	DEFAULT 0 FOR [CanViewLocks];


-- Create Locks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanCreateLocks] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateLocks]
	DEFAULT 0 FOR [CanCreateLocks];


-- Undo Locks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanUndoLocks] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanUndoLocks]
	DEFAULT 0 FOR [CanUndoLocks];

	
-- View Timecards Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewTimecards] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewTimecards]
	DEFAULT 0 FOR [CanViewTimecards];

	
-- Create Timecards Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateTimecards] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateTimecards]
	DEFAULT 0 FOR [CanCreateTimecards];

	
-- Modify Timecards Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyTimecards] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyTimecards]
	DEFAULT 0 FOR [CanModifyTimecards];

	
-- Delete Timecards Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteTimecards] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteTimecards]
	DEFAULT 0 FOR [CanDeleteTimecards];
	
	
-- View Users Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewUsers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewUsers]
	DEFAULT 0 FOR [CanViewUsers];
	
	
-- Create Users Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateUsers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateUsers]
	DEFAULT 0 FOR [CanCreateUsers];
	
	
-- Modify Users Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyUsers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyUsers]
	DEFAULT 0 FOR [CanModifyUsers];
	
	
-- Delete Users Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteUsers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteUsers]
	DEFAULT 0 FOR [CanDeleteUsers];
	
	
-- View Inventory Items Permission

ALTER TABLE [dbo].[Users]
	ADD [CanViewInventoryItems] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewInventoryItems]
	DEFAULT 0 FOR [CanViewInventoryItems];
	
	
-- Sync Inventory Items Permission

ALTER TABLE [dbo].[Users]
	ADD [CanSyncInventoryItems] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanSyncInventoryItems]
	DEFAULT 0 FOR [CanSyncInventoryItems];
	
	
-- View Inventory Consumptions Permission

ALTER TABLE [dbo].[Users]
	ADD [CanViewInventoryConsumptions] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewInventoryConsumptions]
	DEFAULT 0 FOR [CanViewInventoryConsumptions];
	
	
-- Sync Inventory Consumptions Permission

ALTER TABLE [dbo].[Users]
	ADD [CanSyncInventoryConsumptions] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanSyncInventoryConsumptions]
	DEFAULT 0 FOR [CanSyncInventoryConsumptions];
	
	
-- Delete Inventory Consumptions Permission

ALTER TABLE [dbo].[Users]
	ADD [CanDeleteInventoryConsumptions] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteInventoryConsumptions]
	DEFAULT 0 FOR [CanDeleteInventoryConsumptions];

	
-- View Rates Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewRates] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewRates]
	DEFAULT 0 FOR [CanViewRates];

	
-- Create Rates Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateRates] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateRates]
	DEFAULT 0 FOR [CanCreateRates];

	
-- Modify Rates Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyRates] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyRates]
	DEFAULT 0 FOR [CanModifyRates];

	
-- Delete Rates Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteRates] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteRates]
	DEFAULT 0 FOR [CanDeleteRates];


-- View Organization Details Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewOrganizationDetails] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewOrganizationDetails]
	DEFAULT 0 FOR [CanViewOrganizationDetails];


-- Modify Organization Details Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyOrganizationDetails] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyOrganizationDetails]
	DEFAULT 0 FOR [CanModifyOrganizationDetails];


-- View Customers Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewCustomers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewCustomers]
	DEFAULT 0 FOR [CanViewCustomers];

	
-- Create Customers Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateCustomers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateCustomers]
	DEFAULT 0 FOR [CanCreateCustomers];

	
-- Modify Customers Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyCustomers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyCustomers]
	DEFAULT 0 FOR [CanModifyCustomers];

	
-- Delete Customers Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteCustomers] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteCustomers]
	DEFAULT 0 FOR [CanDeleteCustomers];


-- View Projects Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewProjects] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewProjects]
	DEFAULT 0 FOR [CanViewProjects];

	
-- Create Projects Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateProjects] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateProjects]
	DEFAULT 0 FOR [CanCreateProjects];

	
-- Modify Projects Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyProjects] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyProjects]
	DEFAULT 0 FOR [CanModifyProjects];

	
-- Delete Projects Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteProjects] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteProjects]
	DEFAULT 0 FOR [CanDeleteProjects];


-- View Tasks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewTasks] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewTasks]
	DEFAULT 0 FOR [CanViewTasks];

	
-- Create Tasks Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateTasks] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateTasks]
	DEFAULT 0 FOR [CanCreateTasks];

	
-- Modify Tasks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyTasks] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyTasks]
	DEFAULT 0 FOR [CanModifyTasks];

	
-- Delete Tasks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteTasks] BIT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteTasks]
	DEFAULT 0 FOR [CanDeleteTasks];
GO


-- Migrate the role to the new permissions
UPDATE [dbo].[Users]
	SET
		[CanViewPunches] = 1,
		[CanCreatePunches] = 1,
		[CanModifyPunches] = 1,
		[CanDeletePunches] = 1,
		[CanSplitAndPopulatePunches] = 1,
		[CanViewReports] = 1,
		[CanViewLocks] = 1,
		[CanCreateLocks] = 1,
		[CanUndoLocks] = 1,
		[CanViewTimecards] = 1,
		[CanCreateTimecards] = 1,
		[CanModifyTimecards] = 1,
		[CanDeleteTimecards] = 1,
		[CanViewUsers] = 1,
		[CanCreateUsers] = 1,
		[CanModifyUsers] = 1,
		[CanDeleteUsers] = 1,
		[CanViewInventoryItems] = 1,
		[CanSyncInventoryItems] = 1,
		[CanViewInventoryConsumptions] = 1,
		[CanSyncInventoryConsumptions] = 1,
		[CanDeleteInventoryConsumptions] = 1,
		[CanViewRates] = 1,
		[CanCreateRates] = 1,
		[CanModifyRates] = 1,
		[CanDeleteRates] = 1,
		[CanViewOrganizationDetails] = 1,
		[CanModifyOrganizationDetails] = 1,
		[CanViewCustomers] = 1,
		[CanCreateCustomers] = 1,
		[CanModifyCustomers] = 1,
		[CanDeleteCustomers] = 1,
		[CanViewProjects] = 1,
		[CanCreateProjects] = 1,
		[CanModifyProjects] = 1,
		[CanDeleteProjects] = 1,
		[CanViewTasks] = 1,
		[CanCreateTasks] = 1,
		[CanModifyTasks] = 1,
		[CanDeleteTasks] = 1
WHERE [Role] = 'Administrator'
GO

UPDATE [dbo].[Users]
	SET
		[CanViewPunches] = 0,
		[CanCreatePunches] = 0,
		[CanModifyPunches] = 0,
		[CanDeletePunches] = 0,
		[CanSplitAndPopulatePunches] = 0,
		[CanViewReports] = 0,
		[CanViewLocks] = 0,
		[CanCreateLocks] = 0,
		[CanUndoLocks] = 0,
		[CanViewTimecards] = 0,
		[CanCreateTimecards] = 0,
		[CanModifyTimecards] = 0,
		[CanDeleteTimecards] = 0,
		[CanViewUsers] = 0,
		[CanCreateUsers] = 0,
		[CanModifyUsers] = 0,
		[CanDeleteUsers] = 0,
		[CanViewInventoryItems] = 0,
		[CanSyncInventoryItems] = 0,
		[CanViewInventoryConsumptions] = 0,
		[CanSyncInventoryConsumptions] = 0,
		[CanDeleteInventoryConsumptions] = 0,
		[CanViewRates] = 0,
		[CanCreateRates] = 0,
		[CanModifyRates] = 0,
		[CanDeleteRates] = 0,
		[CanViewOrganizationDetails] = 0,
		[CanModifyOrganizationDetails] = 0,
		[CanViewCustomers] = 0,
		[CanCreateCustomers] = 0,
		[CanModifyCustomers] = 0,
		[CanDeleteCustomers] = 0,
		[CanViewProjects] = 0,
		[CanCreateProjects] = 0,
		[CanModifyProjects] = 0,
		[CanDeleteProjects] = 0,
		[CanViewTasks] = 0,
		[CanCreateTasks] = 0,
		[CanModifyTasks] = 0,
		[CanDeleteTasks] = 0
WHERE [Role] = 'Standard'
GO


-- Migrate IsActive
UPDATE [dbo].[Users]
	SET
		[IsActive] = 1
WHERE [IsDeleted] = 0
GO

UPDATE [dbo].[Users]
	SET
		[IsActive] = 0
WHERE [IsDeleted] = 1
GO
