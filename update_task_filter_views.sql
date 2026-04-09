/* GoldBranchAI - TaskFilterViews table bootstrap
   Use when your database exists but EF migrations history is out of sync.
*/

IF OBJECT_ID(N'dbo.TaskFilterViews', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TaskFilterViews] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [AppUserId] INT NOT NULL,
        [Name] NVARCHAR(80) NOT NULL,
        [StateJson] NVARCHAR(1200) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL,
        CONSTRAINT [PK_TaskFilterViews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TaskFilterViews_Users_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [dbo].[Users] ([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_TaskFilterViews_AppUserId_Name'
      AND i.object_id = OBJECT_ID(N'dbo.TaskFilterViews', N'U')
)
BEGIN
    CREATE UNIQUE INDEX [IX_TaskFilterViews_AppUserId_Name]
    ON [dbo].[TaskFilterViews] ([AppUserId], [Name]);
END
GO

