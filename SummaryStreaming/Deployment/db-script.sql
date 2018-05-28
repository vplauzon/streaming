--DROP PROC dbo.updateSummaries
--DROP TABLE [dbo].WidgetSummary

CREATE TABLE [dbo].WidgetSummary
(
	[WidgetId] INT NOT NULL PRIMARY KEY, 
    [WidgetCount] INT NOT NULL
)
GO

CREATE PROC dbo.updateSummaries @jsonPayload AS VARCHAR(MAX)
AS
BEGIN
	MERGE dbo.WidgetSummary AS target
	USING
	(
		SELECT *
		FROM OPENJSON(@jsonPayload)
		WITH (
			WidgetId INT '$.widgetid',
			WidgetCount INT '$.count'
		)
	) AS source
	ON (target.WidgetId = source.WidgetId)
	WHEN MATCHED THEN
		UPDATE SET WidgetCount = source.WidgetCount+target.WidgetCount
	WHEN NOT MATCHED THEN
		INSERT (WidgetId, WidgetCount)
		VALUES (source.WidgetId, source.WidgetCount);
END
GO
