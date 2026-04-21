-- Add InvoiceId FK to ProjectContractSlice
ALTER TABLE [dbo].[ProjectContractSlice]
    ADD [InvoiceId] INT NULL
        CONSTRAINT [FK_ProjectContractSlice_Invoices]
        REFERENCES [dbo].[Invoices]([Id]);
