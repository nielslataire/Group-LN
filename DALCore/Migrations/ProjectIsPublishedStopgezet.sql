-- Add IsPublished column to Project table
ALTER TABLE [dbo].[Project] ADD [IsPublished] BIT NOT NULL DEFAULT 0;

-- Set existing projects as published when they are not in draft (Ontwerp = 3)
UPDATE [dbo].[Project] SET [IsPublished] = 1 WHERE [StatusID] <> 3 OR [StatusID] IS NULL;

-- Add new "Stopgezet" status to ProjectStatus table
INSERT INTO [dbo].[ProjectStatus] ([StatusID], [StatusName]) VALUES (6, 'Stopgezet');
