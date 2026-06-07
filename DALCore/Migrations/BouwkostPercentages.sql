-- Migration: BouwkostPercentages
-- Run once against the CPM_MarketData database

CREATE TABLE [dbo].[BouwkostPercentageGroep] (
    [Id]       INT            IDENTITY(1,1) NOT NULL,
    [Naam]     NVARCHAR(100)  NOT NULL,
    [Volgorde] INT            NOT NULL DEFAULT 0,
    CONSTRAINT [PK_BouwkostPercentageGroep] PRIMARY KEY CLUSTERED ([Id])
);

CREATE TABLE [dbo].[BouwkostPercentage] (
    [Id]      INT             IDENTITY(1,1) NOT NULL,
    [GroepId] INT             NOT NULL,
    [Naam]    NVARCHAR(150)   NOT NULL,
    [Percentage] DECIMAL(7,4) NOT NULL DEFAULT 0,
    [Volgorde]   INT          NOT NULL DEFAULT 0,
    CONSTRAINT [PK_BouwkostPercentage] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_BouwkostPercentage_Groep] FOREIGN KEY ([GroepId])
        REFERENCES [dbo].[BouwkostPercentageGroep] ([Id])
);

-- Seed: veelgebruikte groepen
INSERT INTO [dbo].[BouwkostPercentageGroep] ([Naam],[Volgorde]) VALUES
    ('Erelonen',        10),
    ('Veiligheid & coördinatie', 20),
    ('Administratie & aansluitingen', 30),
    ('Financiering',    40),
    ('Diverse kosten',  50);
