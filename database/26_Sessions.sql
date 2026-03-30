-- ============================================================
-- Script 26: User Sessions Tracking
-- Creates LM_Sessions for online-user panel
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LM_Sessions'
)
BEGIN
    CREATE TABLE LM_Sessions (
        SessionId   INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_LM_Sessions PRIMARY KEY,
        SessionKey  NVARCHAR(50)  NOT NULL,            -- GUID generated at login
        UserId      INT           NOT NULL,
        LoginAt     DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        LastSeenAt  DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        LogoutAt    DATETIME2     NULL,
        IpAddress   NVARCHAR(50)  NULL,
        UserAgent   NVARCHAR(300) NULL,
        IsActive    BIT           NOT NULL DEFAULT 1,
        CONSTRAINT FK_LM_Sessions_User FOREIGN KEY (UserId) REFERENCES LM_Users(UserId)
    );

    CREATE INDEX IX_LM_Sessions_SessionKey ON LM_Sessions (SessionKey);
    CREATE INDEX IX_LM_Sessions_Active     ON LM_Sessions (IsActive, LastSeenAt);

    PRINT 'LM_Sessions table created.';
END
ELSE
BEGIN
    PRINT 'LM_Sessions table already exists.';
END
GO
