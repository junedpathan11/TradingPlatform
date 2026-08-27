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

CREATE TABLE [Trades] (
    [TradeId] int NOT NULL IDENTITY,
    [Symbol] nvarchar(16) NOT NULL,
    [Side] nvarchar(4) NOT NULL,
    [Quantity] decimal(18,2) NOT NULL,
    [Price] decimal(18,5) NOT NULL,
    [TimestampUtc] datetime2(3) NOT NULL,
    [Status] nvarchar(10) NOT NULL,
    CONSTRAINT [PK_Trades] PRIMARY KEY ([TradeId]),
    CONSTRAINT [CK_Trades_Quantity_Positive] CHECK ([Quantity] > 0),
    CONSTRAINT [CK_Trades_Side] CHECK ([Side] IN ('Buy', 'Sell')),
    CONSTRAINT [CK_Trades_Status] CHECK ([Status] IN ('Filled', 'Rejected'))
);
GO

CREATE INDEX [IX_Trades_Symbol] ON [Trades] ([Symbol]);
GO

CREATE INDEX [IX_Trades_TimestampUtc] ON [Trades] ([TimestampUtc]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260827150445_InitialCreate', N'8.0.30');
GO

COMMIT;
GO

