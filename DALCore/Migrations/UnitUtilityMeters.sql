-- EAN-codes gas/elektriciteit en watermeternummer toevoegen aan een eenheid
ALTER TABLE [dbo].[Units]
    ADD [EanGas] NVARCHAR(50) NULL;

ALTER TABLE [dbo].[Units]
    ADD [EanElektriciteit] NVARCHAR(50) NULL;

ALTER TABLE [dbo].[Units]
    ADD [WatermeterNummer] NVARCHAR(50) NULL;
