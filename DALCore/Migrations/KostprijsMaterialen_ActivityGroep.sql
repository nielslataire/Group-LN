-- Migration: koppel KostprijsMateriaal aan ActivityGroup ipv KostprijsCategorie
ALTER TABLE [dbo].[KostprijsMateriaal]
    DROP CONSTRAINT [FK_KostprijsMateriaal_Categorie];

ALTER TABLE [dbo].[KostprijsMateriaal]
    ADD CONSTRAINT [FK_KostprijsMateriaal_ActivityGroep]
    FOREIGN KEY ([CategorieId]) REFERENCES [dbo].[ActivityGroup]([GroupID]);
