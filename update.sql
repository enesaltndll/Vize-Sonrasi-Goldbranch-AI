BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [AvatarUrl] nvarchar(max) NULL;
GO

ALTER TABLE [Users] ADD [ExperiencePoints] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Tasks] ADD [DependsOnTaskId] int NULL;
GO

CREATE TABLE [TaskComments] (
    [Id] int NOT NULL IDENTITY,
    [TodoTaskId] int NOT NULL,
    [AppUserId] int NOT NULL,
    [CommentText] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_TaskComments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TaskComments_Tasks_TodoTaskId] FOREIGN KEY ([TodoTaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TaskComments_Users_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [UserBadges] (
    [Id] int NOT NULL IDENTITY,
    [AppUserId] int NOT NULL,
    [BadgeName] nvarchar(max) NOT NULL,
    [IconUrl] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [EarnedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserBadges] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserBadges_Users_AppUserId] FOREIGN KEY ([AppUserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Tasks_DependsOnTaskId] ON [Tasks] ([DependsOnTaskId]);
GO

CREATE INDEX [IX_TaskComments_AppUserId] ON [TaskComments] ([AppUserId]);
GO

CREATE INDEX [IX_TaskComments_TodoTaskId] ON [TaskComments] ([TodoTaskId]);
GO

CREATE INDEX [IX_UserBadges_AppUserId] ON [UserBadges] ([AppUserId]);
GO

ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Tasks_DependsOnTaskId] FOREIGN KEY ([DependsOnTaskId]) REFERENCES [Tasks] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260406220921_AddGrandFinaleFeatures', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260406222541_GrandFinaleUpdate', N'8.0.25');
GO

COMMIT;
GO

