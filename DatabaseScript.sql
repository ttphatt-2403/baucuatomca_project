IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Rooms] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Status] int NOT NULL,
    [MaxPlayers] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Rooms] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(450) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Balance] decimal(18,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Rounds] (
    [Id] int NOT NULL IDENTITY,
    [RoomId] int NOT NULL,
    [Dice1] int NULL,
    [Dice2] int NULL,
    [Dice3] int NULL,
    [Status] int NOT NULL,
    [BettingEndsAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Rounds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Rounds_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [RoomPlayers] (
    [RoomId] int NOT NULL,
    [UserId] int NOT NULL,
    [JoinedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RoomPlayers] PRIMARY KEY ([RoomId], [UserId]),
    CONSTRAINT [FK_RoomPlayers_Rooms_RoomId] FOREIGN KEY ([RoomId]) REFERENCES [Rooms] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RoomPlayers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Bets] (
    [Id] int NOT NULL IDENTITY,
    [RoundId] int NOT NULL,
    [UserId] int NOT NULL,
    [Symbol] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [WinAmount] decimal(18,2) NULL,
    CONSTRAINT [PK_Bets] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Bets_Rounds_RoundId] FOREIGN KEY ([RoundId]) REFERENCES [Rounds] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Bets_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Bets_RoundId] ON [Bets] ([RoundId]);
GO

CREATE INDEX [IX_Bets_UserId] ON [Bets] ([UserId]);
GO

CREATE INDEX [IX_RoomPlayers_UserId] ON [RoomPlayers] ([UserId]);
GO

CREATE INDEX [IX_Rounds_RoomId] ON [Rounds] ([RoomId]);
GO

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323124509_InitialCreate', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

EXEC sp_rename N'[Users].[PasswordHash]', N'Email', N'COLUMN';
GO

ALTER TABLE [Users] ADD [FirebaseUid] nvarchar(450) NOT NULL DEFAULT N'';
GO

CREATE UNIQUE INDEX [IX_Users_FirebaseUid] ON [Users] ([FirebaseUid]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323125643_AddFirebaseUid', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [RoomPlayers] ADD [IsReady] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260323134531_AddIsReady', N'8.0.25');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [IsAdmin] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260325031120_AddIsAdmin', N'8.0.25');
GO

COMMIT;
GO

