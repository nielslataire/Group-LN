-- ============================================================
-- Migration 002: Automated Issue Notification Infrastructure
-- Run once against the cpmRunning database.
-- After running, execute EF Core Power Tools reverse engineering
-- to generate the DAL entities, then delete:
--   DALCore/Models/IssueNotificationSchedule.cs
--   DALCore/Models/IssueNotificationRun.cs
--   DALCore/Models/ConstructionIssueManual.cs
--   DALCore/Models/cpmRunningContextNotifications.cs
-- ============================================================

-- 1. DoNotAutoNotify flag on ConstructionIssue
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'ConstructionIssue') AND name = N'DoNotAutoNotify'
)
BEGIN
    ALTER TABLE ConstructionIssue
        ADD DoNotAutoNotify BIT NOT NULL DEFAULT 0;
    PRINT 'Added ConstructionIssue.DoNotAutoNotify';
END;

-- 2. IssueNotificationSchedule
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'IssueNotificationSchedule')
BEGIN
    CREATE TABLE IssueNotificationSchedule (
        Id                      INT IDENTITY(1,1) NOT NULL,
        SupplierId              INT          NULL,   -- NULL = applies to all companies
        ProjectId               INT          NULL,   -- NULL = applies to all projects
        ReminderFrequencyDays   INT          NOT NULL DEFAULT 7,
            -- 0 = disabled, 7 = weekly, 14 = biweekly, 30 = monthly
        SendEveningUpdate       BIT          NOT NULL DEFAULT 1,
        EveningUpdateHour       INT          NOT NULL DEFAULT 18,
        IsActive                BIT          NOT NULL DEFAULT 1,
        NextReminderRun         DATETIME2(7) NULL,   -- when next reminder should fire
        LastEveningRun          DATE         NULL,   -- date of last evening update
        Notes                   NVARCHAR(500) NULL,
        CreatedDate             DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate            DATETIME2(7) NULL,
        CONSTRAINT PK_IssueNotificationSchedule PRIMARY KEY (Id)
    );

    -- Default global schedule (weekly Monday reminders + evening updates at 18:00)
    -- NextReminderRun = next Monday at 08:00 UTC
    INSERT INTO IssueNotificationSchedule
        (SupplierId, ProjectId, ReminderFrequencyDays, SendEveningUpdate,
         EveningUpdateHour, IsActive, NextReminderRun, Notes)
    VALUES
        (NULL, NULL, 7, 1, 18, 1,
         DATEADD(hour, 8,
             CAST(
                 DATEADD(day,
                     CASE DATEPART(weekday, GETUTCDATE())
                         WHEN 1 THEN 1  -- Sunday  -> Monday (+1)
                         WHEN 2 THEN 7  -- Monday  -> next Monday (+7)
                         WHEN 3 THEN 6  -- Tuesday -> Monday (+6)
                         WHEN 4 THEN 5  -- Wednesday
                         WHEN 5 THEN 4  -- Thursday
                         WHEN 6 THEN 3  -- Friday
                         WHEN 7 THEN 2  -- Saturday
                     END,
                     CAST(GETUTCDATE() AS DATE)
                 ) AS DATETIME2
             )
         ),
         N'Globale standaardinstellingen – wekelijks maandag');

    PRINT 'Created IssueNotificationSchedule with default record';
END;

-- 3. IssueNotificationRun  (audit log of every scheduler execution)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'IssueNotificationRun')
BEGIN
    CREATE TABLE IssueNotificationRun (
        Id              INT IDENTITY(1,1) NOT NULL,
        ScheduleId      INT           NULL,
        RunType         INT           NOT NULL,   -- 0=Reminder, 1=EveningUpdate, 2=Manual
        TriggeredBy     NVARCHAR(50)  NOT NULL DEFAULT N'scheduler',
        StartedDate     DATETIME2(7)  NOT NULL,
        FinishedDate    DATETIME2(7)  NULL,
        Status          INT           NOT NULL DEFAULT 0,  -- 0=Running,1=Success,2=Error
        ErrorMessage    NVARCHAR(MAX) NULL,
        EmailsSent      INT           NOT NULL DEFAULT 0,
        CONSTRAINT PK_IssueNotificationRun PRIMARY KEY (Id),
        CONSTRAINT FK_IssueNotificationRun_Schedule
            FOREIGN KEY (ScheduleId) REFERENCES IssueNotificationSchedule (Id)
    );

    CREATE NONCLUSTERED INDEX IX_IssueNotificationRun_StartedDate
        ON IssueNotificationRun (StartedDate DESC);

    PRINT 'Created IssueNotificationRun';
END;

-- 4. ReminderHour column on IssueNotificationSchedule (UTC, same pattern as EveningUpdateHour)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'IssueNotificationSchedule') AND name = N'ReminderHour'
)
BEGIN
    ALTER TABLE IssueNotificationSchedule
        ADD ReminderHour INT NOT NULL DEFAULT 8;
    PRINT 'Added IssueNotificationSchedule.ReminderHour';
END;

-- 5. ReminderDayOfWeek column on IssueNotificationSchedule
--    0=Sunday, 1=Monday, 2=Tuesday, 3=Wednesday, 4=Thursday, 5=Friday, 6=Saturday (matches .NET DayOfWeek)
--    NULL = no specific day restriction (fire on any day when NextReminderRun is due)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'IssueNotificationSchedule') AND name = N'ReminderDayOfWeek'
)
BEGIN
    ALTER TABLE IssueNotificationSchedule
        ADD ReminderDayOfWeek INT NULL;
    PRINT 'Added IssueNotificationSchedule.ReminderDayOfWeek';
END;
