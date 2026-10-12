-- ============================================================
-- HelpFeedback — "Vraag stellen aan support" en artikel-feedback
-- uit het gl-v2 Helpcentrum (design-handoff punt 41).
-- Uitvoeren op de CPM-database vóór deployment.
-- ============================================================

CREATE TABLE [dbo].[HelpFeedback]
(
    [Id]          INT            NOT NULL IDENTITY(1,1),
    [UserId]      INT            NOT NULL,
    -- 'SupportQuestion' | 'ArticleHelpful' | 'ArticleNotHelpful'
    [Kind]        NVARCHAR(30)   NOT NULL,
    [ArticleSlug] NVARCHAR(200)  NULL,
    [ScreenId]    NVARCHAR(20)   NULL,
    [Message]     NVARCHAR(MAX) NULL,
    [CreatedAt]   DATETIME2(7)   NOT NULL CONSTRAINT [DF_HelpFeedback_CreatedAt] DEFAULT (getdate()),

    CONSTRAINT [PK_HelpFeedback]
        PRIMARY KEY CLUSTERED ([Id]),

    CONSTRAINT [FK_HelpFeedback_Users]
        FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
        ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_HelpFeedback_UserId]
    ON [dbo].[HelpFeedback] ([UserId]);
GO

CREATE NONCLUSTERED INDEX [IX_HelpFeedback_ArticleSlug]
    ON [dbo].[HelpFeedback] ([ArticleSlug]);
GO
