-- AI Sağlayıcı Tercihleri - AppUser tablosuna yeni kolonlar ekleme
-- GoldBranch AI - 2026-04-15

-- Mevcut veritabanına bu script'i çalıştırarak yeni alanları ekleyebilirsiniz.
-- Eğer EnsureDeleted + EnsureCreated kullanıyorsanız bu script'e gerek yoktur.

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'PreferredAiProvider')
BEGIN
    ALTER TABLE [Users] ADD [PreferredAiProvider] nvarchar(max) NOT NULL DEFAULT 'default';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'CustomAiApiKey')
BEGIN
    ALTER TABLE [Users] ADD [CustomAiApiKey] nvarchar(max) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'Users') AND name = 'CustomAiModel')
BEGIN
    ALTER TABLE [Users] ADD [CustomAiModel] nvarchar(max) NULL;
END
GO

PRINT 'AI Provider tercihleri başarıyla eklendi!';
