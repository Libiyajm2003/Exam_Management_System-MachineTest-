-- Database objects for Exam Management (ADO.NET + stored procedures)
-- Run against your target database (e.g., USE ExamManagementDb;)

-- Table type for passing ID lists
IF NOT EXISTS (SELECT 1 FROM sys.types WHERE is_table_type = 1 AND name = 'IntList')
    CREATE TYPE dbo.IntList AS TABLE (Id INT PRIMARY KEY);
GO

-- SubjectMst
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SubjectMst]') AND type = 'U')
BEGIN
    CREATE TABLE dbo.SubjectMst
    (
        SubjectID   INT IDENTITY(1,1) PRIMARY KEY,
        SubjectName NVARCHAR(200) NOT NULL
    );
END
GO

-- StudentMst
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[StudentMst]') AND type = 'U')
BEGIN
    CREATE TABLE dbo.StudentMst
    (
        StudentID   INT IDENTITY(1,1) PRIMARY KEY,
        StudentName NVARCHAR(250) NOT NULL,
        Mail        NVARCHAR(250) NOT NULL UNIQUE
    );
END
GO

-- ExamMaster
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExamMaster]') AND type = 'U')
BEGIN
    CREATE TABLE dbo.ExamMaster
    (
        MasterID   INT IDENTITY(1,1) PRIMARY KEY,
        StudentID  INT NOT NULL,
        ExamYear   INT NOT NULL,
        TotalMark  DECIMAL(10,2) NOT NULL DEFAULT 0,
        PassOrFail NVARCHAR(10) NOT NULL DEFAULT 'FAIL',
        CreateTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ExamMaster_Student FOREIGN KEY (StudentID) REFERENCES dbo.StudentMst(StudentID),
        CONSTRAINT UQ_ExamMaster_StudentYear UNIQUE (StudentID, ExamYear)
    );
END
GO

-- ExamDtls
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ExamDtls]') AND type = 'U')
BEGIN
    CREATE TABLE dbo.ExamDtls
    (
        DtlsID    INT IDENTITY(1,1) PRIMARY KEY,
        MasterID  INT NOT NULL,
        SubjectID INT NOT NULL,
        Marks     DECIMAL(5,2) NOT NULL CHECK (Marks >= 0 AND Marks <= 100),
        CONSTRAINT FK_ExamDtls_Master FOREIGN KEY (MasterID) REFERENCES dbo.ExamMaster(MasterID) ON DELETE CASCADE,
        CONSTRAINT FK_ExamDtls_Subject FOREIGN KEY (SubjectID) REFERENCES dbo.SubjectMst(SubjectID)
    );
END
GO

-- Seed data
IF NOT EXISTS (SELECT 1 FROM dbo.StudentMst)
BEGIN
    INSERT INTO dbo.StudentMst (StudentName, Mail)
    VALUES ('Alice Johnson', 'alice@example.com'),
           ('Bob Williams', 'bob@example.com');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.SubjectMst)
BEGIN
    INSERT INTO dbo.SubjectMst (SubjectName)
    VALUES ('Mathematics'), ('Science'), ('English');
END
GO

-- Stored procedures

CREATE OR ALTER PROCEDURE dbo.usp_Students_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StudentID, StudentName, Mail
    FROM dbo.StudentMst
    ORDER BY StudentName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Subjects_GetAll
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SubjectID, SubjectName
    FROM dbo.SubjectMst
    ORDER BY SubjectName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Student_GetById
    @StudentID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT StudentID
    FROM dbo.StudentMst
    WHERE StudentID = @StudentID;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Exam_CheckExists
    @StudentID INT,
    @ExamYear INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) AS Cnt
    FROM dbo.ExamMaster
    WHERE StudentID = @StudentID AND ExamYear = @ExamYear;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Subjects_ValidateIds
    @Ids dbo.IntList READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(*) AS MissingCount
    FROM @Ids i
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SubjectMst s WHERE s.SubjectID = i.Id);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Exam_Insert
    @StudentID INT,
    @ExamYear INT,
    @TotalMark DECIMAL(10,2),
    @PassOrFail NVARCHAR(10),
    @NewMasterID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ExamMaster (StudentID, ExamYear, TotalMark, PassOrFail)
    VALUES (@StudentID, @ExamYear, @TotalMark, @PassOrFail);

    SET @NewMasterID = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_ExamDetail_Insert
    @MasterID INT,
    @SubjectID INT,
    @Marks DECIMAL(5,2)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.ExamDtls (MasterID, SubjectID, Marks)
    VALUES (@MasterID, @SubjectID, @Marks);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Exams_WithDetails_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT em.MasterID, em.StudentID, em.ExamYear, em.TotalMark, em.PassOrFail, em.CreateTime,
           s.StudentName, s.Mail
    FROM dbo.ExamMaster em
    INNER JOIN dbo.StudentMst s ON s.StudentID = em.StudentID
    ORDER BY em.CreateTime DESC;

    SELECT d.DtlsID, d.MasterID, d.SubjectID, d.Marks, subj.SubjectName
    FROM dbo.ExamDtls d
    INNER JOIN dbo.SubjectMst subj ON subj.SubjectID = d.SubjectID
    ORDER BY d.MasterID, d.DtlsID;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Exams_WithDetails_ByIds
    @Ids dbo.IntList READONLY
AS
BEGIN
    SET NOCOUNT ON;

    SELECT em.MasterID, em.StudentID, em.ExamYear, em.TotalMark, em.PassOrFail, em.CreateTime,
           s.StudentName, s.Mail
    FROM dbo.ExamMaster em
    INNER JOIN dbo.StudentMst s ON s.StudentID = em.StudentID
    WHERE em.MasterID IN (SELECT Id FROM @Ids)
    ORDER BY em.CreateTime DESC;

    SELECT d.DtlsID, d.MasterID, d.SubjectID, d.Marks, subj.SubjectName
    FROM dbo.ExamDtls d
    INNER JOIN dbo.SubjectMst subj ON subj.SubjectID = d.SubjectID
    WHERE d.MasterID IN (SELECT Id FROM @Ids)
    ORDER BY d.MasterID, d.DtlsID;
END
GO



