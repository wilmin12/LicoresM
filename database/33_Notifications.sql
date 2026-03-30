-- ============================================================
-- 33_Notifications.sql
-- In-app notification system
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'dbo.LM_NOTIFICATIONS') AND type = 'U')
BEGIN
    CREATE TABLE dbo.LM_NOTIFICATIONS (
        NTF_Id       INT           IDENTITY(1,1) PRIMARY KEY,
        NTF_UserId   INT           NOT NULL,
        NTF_Title    NVARCHAR(100) NOT NULL,
        NTF_Message  NVARCHAR(500) NOT NULL,
        NTF_Type     NVARCHAR(20)  NOT NULL DEFAULT 'INFO',   -- INFO | SUCCESS | WARNING | DANGER
        NTF_IsRead   BIT           NOT NULL DEFAULT 0,
        NTF_Url      NVARCHAR(300) NULL,   -- optional deep-link
        NTF_RefId    INT           NULL,   -- related record id (e.g. AohId)
        NTF_RefType  NVARCHAR(30)  NULL,   -- e.g. 'AANKOOPBON'
        Created_At   DATETIME      NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_NOTIFICATIONS_USER FOREIGN KEY (NTF_UserId) REFERENCES dbo.LM_Users(UserId)
    );

    CREATE INDEX IX_NOTIFICATIONS_USER_UNREAD
        ON dbo.LM_NOTIFICATIONS (NTF_UserId, NTF_IsRead, Created_At DESC);

    PRINT 'LM_NOTIFICATIONS table created.';
END
ELSE
    PRINT 'LM_NOTIFICATIONS already exists — skipped.';
