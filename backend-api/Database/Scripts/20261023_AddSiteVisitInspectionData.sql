/*
  AVA Design Services - customer-specific site inspection responses
  Run against the same database used by backend-api.
  Safe to rerun: adds the nullable column only when it does not exist.
*/
IF COL_LENGTH('dbo.SiteVisits', 'InspectionDataJson') IS NULL
BEGIN
    ALTER TABLE dbo.SiteVisits
        ADD InspectionDataJson NVARCHAR(MAX) NULL;
END;
