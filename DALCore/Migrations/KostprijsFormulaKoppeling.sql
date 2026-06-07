CREATE TABLE [dbo].[KostprijsFormulaKoppeling] (
    [Id]           INT           IDENTITY(1,1) NOT NULL,
    [Sleutel]      NVARCHAR(100) NOT NULL,
    [Omschrijving] NVARCHAR(200) NOT NULL,
    [MateriaalId]  INT           NULL,
    CONSTRAINT [PK_KostprijsFormulaKoppeling] PRIMARY KEY ([Id]),
    CONSTRAINT [UQ_FormulaKoppeling_Sleutel]  UNIQUE ([Sleutel]),
    CONSTRAINT [FK_FormulaKoppeling_Materiaal] FOREIGN KEY ([MateriaalId])
        REFERENCES [dbo].[KostprijsMateriaal]([Id]) ON DELETE SET NULL
);

-- Bekende formule-slots (developer voegt hier rijen toe bij nieuwe sleutels)
INSERT INTO [dbo].[KostprijsFormulaKoppeling] ([Sleutel], [Omschrijving])
VALUES ('nacalc_ruwbouw_basis', 'Nacalc basisprijs ruwbouw (€/m²)');
