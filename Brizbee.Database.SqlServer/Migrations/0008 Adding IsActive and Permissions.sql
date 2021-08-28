
-- IsActive

ALTER TABLE [dbo].[Users]
	ADD [IsActive] BIT NOT NULL;

ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_IsActive]
	DEFAULT 1 FOR [IsActive];
	

-- View Punches Permission

ALTER TABLE [dbo].[Users]
	ADD [CanViewPunches] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewPunches]
	DEFAULT 0 FOR [CanViewPunches];
	

-- Create Punches Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreatePunches] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreatePunches]
	DEFAULT 0 FOR [CanCreatePunches];
	

-- Modify Punches Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyPunches] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyPunches]
	DEFAULT 0 FOR [CanModifyPunches];


-- Delete Punches Permission

ALTER TABLE [dbo].[Users]
	ADD [CanDeletePunches] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeletePunches]
	DEFAULT 0 FOR [CanDeletePunches];
	

-- Split and Populate Punches Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanSplitAndPopulatePunches] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanSplitAndPopulatePunches]
	DEFAULT 0 FOR [CanSplitAndPopulatePunches];


-- View Reports Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewReports] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewReports]
	DEFAULT 0 FOR [CanViewReports];


-- View Locks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewLocks] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewLocks]
	DEFAULT 0 FOR [CanViewLocks];


-- Create Locks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanCreateLocks] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateLocks]
	DEFAULT 0 FOR [CanCreateLocks];


-- Undo Locks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanUndoLocks] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanUndoLocks]
	DEFAULT 0 FOR [CanUndoLocks];

	
-- View Timecards Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewTimecards] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewTimecards]
	DEFAULT 0 FOR [CanViewTimecards];

	
-- Create Timecards Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateTimecards] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateTimecards]
	DEFAULT 0 FOR [CanCreateTimecards];

	
-- Modify Timecards Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyTimecards] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyTimecards]
	DEFAULT 0 FOR [CanModifyTimecards];

	
-- Delete Timecards Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteTimecards] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteTimecards]
	DEFAULT 0 FOR [CanDeleteTimecards];
	
	
-- View Users Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewUsers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewUsers]
	DEFAULT 0 FOR [CanViewUsers];
	
	
-- Create Users Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateUsers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateUsers]
	DEFAULT 0 FOR [CanCreateUsers];
	
	
-- Modify Users Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyUsers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyUsers]
	DEFAULT 0 FOR [CanModifyUsers];
	
	
-- Delete Users Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteUsers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteUsers]
	DEFAULT 0 FOR [CanDeleteUsers];
	
	
-- View Inventory Items Permission

ALTER TABLE [dbo].[Users]
	ADD [CanViewInventoryItems] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewInventoryItems]
	DEFAULT 0 FOR [CanViewInventoryItems];
	
	
-- Sync Inventory Items Permission

ALTER TABLE [dbo].[Users]
	ADD [CanSyncInventoryItems] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanSyncInventoryItems]
	DEFAULT 0 FOR [CanSyncInventoryItems];
	
	
-- View Inventory Consumptions Permission

ALTER TABLE [dbo].[Users]
	ADD [CanViewInventoryConsumptions] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewInventoryConsumptions]
	DEFAULT 0 FOR [CanViewInventoryConsumptions];
	
	
-- Sync Inventory Consumptions Permission

ALTER TABLE [dbo].[Users]
	ADD [CanSyncInventoryConsumptions] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanSyncInventoryConsumptions]
	DEFAULT 0 FOR [CanSyncInventoryConsumptions];
	
	
-- Delete Inventory Consumptions Permission

ALTER TABLE [dbo].[Users]
	ADD [CanDeleteInventoryConsumptions] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteInventoryConsumptions]
	DEFAULT 0 FOR [CanDeleteInventoryConsumptions];

	
-- View Rates Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewRates] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewRates]
	DEFAULT 0 FOR [CanViewRates];

	
-- Create Rates Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateRates] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateRates]
	DEFAULT 0 FOR [CanCreateRates];

	
-- Modify Rates Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyRates] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyRates]
	DEFAULT 0 FOR [CanModifyRates];

	
-- Delete Rates Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteRates] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteRates]
	DEFAULT 0 FOR [CanDeleteRates];


-- View Organization Details Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewOrganizationDetails] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewOrganizationDetails]
	DEFAULT 0 FOR [CanViewOrganizationDetails];


-- Modify Organization Details Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyOrganizationDetails] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyOrganizationDetails]
	DEFAULT 0 FOR [CanModifyOrganizationDetails];


-- View Customers Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewCustomers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewCustomers]
	DEFAULT 0 FOR [CanViewCustomers];

	
-- Create Customers Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateCustomers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateCustomers]
	DEFAULT 0 FOR [CanCreateCustomers];

	
-- Modify Customers Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyCustomers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyCustomers]
	DEFAULT 0 FOR [CanModifyCustomers];

	
-- Delete Customers Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteCustomers] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteCustomers]
	DEFAULT 0 FOR [CanDeleteCustomers];


-- View Projects Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewProjects] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewProjects]
	DEFAULT 0 FOR [CanViewProjects];

	
-- Create Projects Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateProjects] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateProjects]
	DEFAULT 0 FOR [CanCreateProjects];

	
-- Modify Projects Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyProjects] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyProjects]
	DEFAULT 0 FOR [CanModifyProjects];

	
-- Delete Projects Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteProjects] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteProjects]
	DEFAULT 0 FOR [CanDeleteProjects];


-- View Tasks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanViewTasks] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanViewTasks]
	DEFAULT 0 FOR [CanViewTasks];

	
-- Create Tasks Permission

ALTER TABLE [dbo].[Users]
	ADD [CanCreateTasks] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanCreateTasks]
	DEFAULT 0 FOR [CanCreateTasks];

	
-- Modify Tasks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanModifyTasks] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanModifyTasks]
	DEFAULT 0 FOR [CanModifyTasks];

	
-- Delete Tasks Permission
	
ALTER TABLE [dbo].[Users]
	ADD [CanDeleteTasks] BIT NOT NULL;
	
ALTER TABLE [dbo].[Users]
	ADD CONSTRAINT [DF_Users_CanDeleteTasks]
	DEFAULT 0 FOR [CanDeleteTasks];


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
