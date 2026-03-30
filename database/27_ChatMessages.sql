-- ============================================================
-- Script 27: Chat Messages
-- ============================================================

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LM_ChatMessages'
)
BEGIN
    CREATE TABLE LM_ChatMessages (
        MessageId   INT           IDENTITY(1,1) NOT NULL CONSTRAINT PK_LM_ChatMessages PRIMARY KEY,
        FromUserId  INT           NOT NULL,
        ToUserId    INT           NOT NULL,
        Message     NVARCHAR(1000) NOT NULL,
        SentAt      DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
        IsRead      BIT           NOT NULL DEFAULT 0,
        CONSTRAINT FK_ChatMsg_From FOREIGN KEY (FromUserId) REFERENCES LM_Users(UserId),
        CONSTRAINT FK_ChatMsg_To   FOREIGN KEY (ToUserId)   REFERENCES LM_Users(UserId)
    );

    CREATE INDEX IX_ChatMessages_Thread ON LM_ChatMessages (FromUserId, ToUserId, SentAt);
    CREATE INDEX IX_ChatMessages_Unread ON LM_ChatMessages (ToUserId, IsRead);

    PRINT 'LM_ChatMessages table created.';
END
ELSE PRINT 'LM_ChatMessages already exists.';
GO
