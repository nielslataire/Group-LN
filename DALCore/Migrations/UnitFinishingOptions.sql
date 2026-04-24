-- UnitFinishingOption tabel aanmaken
CREATE TABLE [dbo].[UnitFinishingOption] (
    [Id]        INT           IDENTITY(1,1) NOT NULL,
    [UnitId]    INT           NOT NULL,
    [Name]      NVARCHAR(100) NOT NULL,
    [SortOrder] INT           NOT NULL CONSTRAINT [DF_UnitFinishingOption_SortOrder] DEFAULT 0,
    [IsDefault] BIT           NOT NULL CONSTRAINT [DF_UnitFinishingOption_IsDefault] DEFAULT 0,
    CONSTRAINT [PK_UnitFinishingOption] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UnitFinishingOption_Units] FOREIGN KEY ([UnitId])
        REFERENCES [dbo].[Units] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_UnitFinishingOption_UnitId] ON [dbo].[UnitFinishingOption] ([UnitId]);

-- FK kolom toevoegen aan bestaande UnitConstructionValue rijen
ALTER TABLE [dbo].[UnitConstructionValue]
    ADD [FinishingOptionId] INT NULL;

ALTER TABLE [dbo].[UnitConstructionValue]
    ADD CONSTRAINT [FK_UnitConstructionValue_FinishingOption]
    FOREIGN KEY ([FinishingOptionId]) REFERENCES [dbo].[UnitFinishingOption] ([Id])
    ON DELETE NO ACTION;
