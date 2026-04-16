-- ============================================================
-- UserCoachmarkState — per-user coachmark state
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[UserCoachmarkState]
(
    [Id]           INT            NOT NULL IDENTITY(1,1),
    [UserId]       INT            NOT NULL,
    [FeatureKey]   NVARCHAR(200)  NOT NULL,
    [FirstShownAt] DATETIME2(7)   NULL,
    [LastShownAt]  DATETIME2(7)   NULL,
    [Dismissed]    BIT            NOT NULL CONSTRAINT [DF_UserCoachmarkState_Dismissed]   DEFAULT (0),
    [DismissedAt]  DATETIME2(7)   NULL,
    [Completed]    BIT            NOT NULL CONSTRAINT [DF_UserCoachmarkState_Completed]   DEFAULT (0),
    [CompletedAt]  DATETIME2(7)   NULL,
    [CurrentStep]  INT            NOT NULL CONSTRAINT [DF_UserCoachmarkState_CurrentStep] DEFAULT (0),
    [ShowCount]    INT            NOT NULL CONSTRAINT [DF_UserCoachmarkState_ShowCount]   DEFAULT (0),

    CONSTRAINT [PK_UserCoachmarkState]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_UserCoachmarkState_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
        ON DELETE CASCADE,

    -- Één rij per gebruiker per feature/sequence
    CONSTRAINT [UQ_UserCoachmarkState_UserFeature]
        UNIQUE ([UserId], [FeatureKey])
);
GO

CREATE NONCLUSTERED INDEX [IX_UserCoachmarkState_UserId]
    ON [dbo].[UserCoachmarkState] ([UserId]);
GO
