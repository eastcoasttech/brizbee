
-- Drop Stored Procedures
DROP PROCEDURE [dbo].[usp_ReadingsGapsForConnections];
DROP PROCEDURE [dbo].[usp_PopulateFlagsForReadingsGaps];
DROP PROCEDURE [dbo].[usp_ActiveAndInactiveMeterCount];
DROP PROCEDURE [dbo].[usp_ConsumptionForEAIExpanded];
DROP PROCEDURE [dbo].[usp_ConsumptionForEAISummarized];
DROP PROCEDURE [dbo].[usp_MetersAndChannels];
DROP PROCEDURE [dbo].[spSiloGetConsumptionPerMeterChannelAndTimeOfUsePeriod];
DROP PROCEDURE [dbo].[spSiloGetConsumptionPerDayAndTimeOfUsePeriod];
DROP PROCEDURE [dbo].[spSiloGetConsumptionPerDayAndTimeOfUsePeriodForLoadType];
DROP PROCEDURE [dbo].[spSiloGetConsumptionPerDayAndTimeOfUsePeriodForMeter];
DROP PROCEDURE [dbo].[spSiloGetConsumptionPerMonthAndTimeOfUsePeriodWithParametersForLoadType];
DROP PROCEDURE [dbo].[spSiloGetIntervalConsumptionPerMeterChannel];
DROP PROCEDURE [dbo].[spSiloGetIntervalConsumption];
DROP PROCEDURE [dbo].[spSiloGetQuarterHourlyIntervalConsumptionPerMeterChannel];
DROP PROCEDURE [dbo].[spSiloGetIntervalConsumptionWithParametersForMeter];
DROP PROCEDURE [dbo].[spSiloGetIntervalConsumptionWithParameters];


-- Drop Views
DROP VIEW [dbo].[vw_ReadingsDailyEnergyActiveImportWithoutCast];
DROP VIEW [dbo].[vw_ReadingsHourlyPowerActiveInterval];
DROP VIEW [dbo].[vw_TagsActiveLoadType];
DROP VIEW [dbo].[vw_TagsActiveLoadName];
DROP VIEW [dbo].[vw_IQ2_Users];
DROP VIEW [dbo].[vw_IQ2_Buildings];
DROP VIEW [dbo].[vw_IQ2_Readings];
DROP VIEW [dbo].[vw_IQ2_LoadNames];
DROP VIEW [dbo].[vw_IQ2_LoadTypes];
DROP VIEW [dbo].[vw_IQ2_Meters];


-- Drop Functions
DROP FUNCTION [dbo].[GapsForDateRange];
DROP FUNCTION [dbo].[MetersAndChannels];


-- Drop Tables
DROP TABLE [dbo].[SchemaVersions];
DROP TABLE [dbo].[StagedFamily5Readings];
DROP TABLE [dbo].[StagedFamily5NReadings];
DROP TABLE [dbo].[StagedRealTimeReadings];
DROP TABLE [dbo].[Journals];
DROP TABLE [dbo].[Assignments];
DROP TABLE [dbo].[Commands];
DROP TABLE [dbo].[Deployments];
DROP TABLE [dbo].[Bridges];
DROP TABLE [dbo].[CacheObjects];
DROP TABLE [dbo].[Errors];
DROP TABLE [dbo].[Flags];
DROP TABLE [dbo].[Imports];
DROP TABLE [dbo].[MeterPoints];
DROP TABLE [dbo].[Notifications];
DROP TABLE [dbo].[Tags];
DROP TABLE [dbo].[Readings];
DROP TABLE [dbo].[Queries];
DROP TABLE [dbo].[Meters];
DROP TABLE [dbo].[Transponders];
DROP TABLE [dbo].[VirtualMeters];
DROP TABLE [dbo].[Devices];
DROP TABLE [dbo].[Connections];
DROP TABLE [dbo].[Buildings];
DROP TABLE [dbo].[Users];