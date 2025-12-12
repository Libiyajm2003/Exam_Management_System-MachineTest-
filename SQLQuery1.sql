
CREATE DATABASE ExamManagementDb;

USE ExamManagementDb;

-- SubjectMst
CREATE TABLE SubjectMst
(
    SubjectID   INT IDENTITY(1,1) PRIMARY KEY,
    SubjectName NVARCHAR(200) NOT NULL
);

-- StudentMst
CREATE TABLE StudentMst
(
    StudentID   INT IDENTITY(1,1) PRIMARY KEY,
    StudentName NVARCHAR(250) NOT NULL CHECK (LEN(StudentName) >= 5),
    Mail        NVARCHAR(250) NOT NULL UNIQUE
);

-- ExamMaster
CREATE TABLE ExamMaster
(
    MasterID   INT IDENTITY(1,1) PRIMARY KEY,
    StudentID  INT NOT NULL,
    ExamYear   INT NOT NULL,
    TotalMark  DECIMAL(10,2) NOT NULL DEFAULT 0,
    PassOrFail NVARCHAR(10) NOT NULL DEFAULT 'FAIL',
    CreateTime DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT FK_ExamMaster_Student FOREIGN KEY (StudentID)
        REFERENCES StudentMst(StudentID),

    CONSTRAINT UQ_ExamMaster_StudentYear UNIQUE (StudentID, ExamYear)
);

-- ExamDtls
CREATE TABLE ExamDtls
(
    DtlsID    INT IDENTITY(1,1) PRIMARY KEY,
    MasterID  INT NOT NULL,
    SubjectID INT NOT NULL,
    Marks     DECIMAL(5,2) NOT NULL CHECK (Marks >= 0 AND Marks <= 100),

    CONSTRAINT FK_ExamDtls_Master FOREIGN KEY (MasterID)
        REFERENCES ExamMaster(MasterID) ON DELETE CASCADE,

    CONSTRAINT FK_ExamDtls_Subject FOREIGN KEY (SubjectID)
        REFERENCES SubjectMst(SubjectID)
);


INSERT INTO StudentMst (StudentName, Mail)
VALUES ('Libiya', 'Libiya@gmail.com'),
       ('Gladis', 'Gladis@gmail.com');

INSERT INTO SubjectMst (SubjectName)
VALUES ('Computer'),
       ('Science'),
       ('English');

-- Students_GetAll
CREATE OR ALTER PROCEDURE Students_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT StudentID, StudentName, Mail
    FROM StudentMst
    ORDER BY StudentName;
END;

-- Subjects_GetAll
CREATE OR ALTER PROCEDURE Subjects_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SubjectID, SubjectName
    FROM SubjectMst
    ORDER BY SubjectName;
END;

-- Student_GetById
CREATE OR ALTER PROCEDURE Student_GetById
    @StudentID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT StudentID, StudentName, Mail
    FROM StudentMst
    WHERE StudentID = @StudentID;
END;

-- Student_Insert
CREATE OR ALTER PROCEDURE Student_Insert
    @StudentName NVARCHAR(250),
    @Mail NVARCHAR(250),
    @NewStudentID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Check if email already exists
    IF EXISTS (SELECT 1 FROM StudentMst WHERE Mail = @Mail)
    BEGIN
        RAISERROR('Email already exists. Email must be unique.', 16, 1);
        RETURN;
    END

    INSERT INTO StudentMst (StudentName, Mail)
    VALUES (@StudentName, @Mail);

    SET @NewStudentID = SCOPE_IDENTITY();
END;

-- Exam_CheckExists
CREATE OR ALTER PROCEDURE Exam_CheckExists
    @StudentID INT,
    @ExamYear INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(1) AS Cnt
    FROM ExamMaster
    WHERE StudentID = @StudentID
      AND ExamYear = @ExamYear;
END;

-- Exam_Insert (creates master only, TotalMark & PassOrFail computed later)
CREATE OR ALTER PROCEDURE Exam_Insert
    @StudentID INT,
    @ExamYear INT,
    @NewMasterID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ExamMaster (StudentID, ExamYear)
    VALUES (@StudentID, @ExamYear);

    SET @NewMasterID = SCOPE_IDENTITY();
END;

-- ExamDetail_Insert
CREATE OR ALTER PROCEDURE ExamDetail_Insert
    @MasterID INT,
    @SubjectID INT,
    @Marks DECIMAL(5,2)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO ExamDtls (MasterID, SubjectID, Marks)
    VALUES (@MasterID, @SubjectID, @Marks);
END;

-- FinalizeExamResult (computes TotalMark and PassOrFail)
CREATE OR ALTER PROCEDURE FinalizeExamResult
    @MasterID INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM ExamMaster WHERE MasterID = @MasterID)
    BEGIN
        RAISERROR('MasterID %d not found.', 16, 1, @MasterID);
        RETURN;
    END

    DECLARE @Total DECIMAL(10,2);
    DECLARE @MinMark DECIMAL(5,2);

    SELECT
        @Total = ISNULL(SUM(Marks),0),
        @MinMark = MIN(Marks)
    FROM ExamDtls
    WHERE MasterID = @MasterID;

    DECLARE @Status NVARCHAR(10) = 'FAIL';
    IF @MinMark IS NOT NULL AND @MinMark >= 25
        SET @Status = 'PASS';

    UPDATE ExamMaster
    SET TotalMark = @Total,
        PassOrFail = @Status
    WHERE MasterID = @MasterID;
END;

-- Exams_WithDetails_GetAll
CREATE OR ALTER PROCEDURE Exams_WithDetails_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT em.MasterID, em.StudentID, em.ExamYear, em.TotalMark, em.PassOrFail, em.CreateTime,
           s.StudentName, s.Mail
    FROM ExamMaster em
    INNER JOIN StudentMst s ON s.StudentID = em.StudentID
    ORDER BY em.CreateTime DESC;

    SELECT d.DtlsID, d.MasterID, d.SubjectID, d.Marks, subj.SubjectName
    FROM ExamDtls d
    INNER JOIN SubjectMst subj ON subj.SubjectID = d.SubjectID
    ORDER BY d.MasterID, d.DtlsID;
END;

-- Exams_GetByMasterId (retrieve single master + details)
CREATE OR ALTER PROCEDURE Exams_GetByMasterId
    @MasterID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT em.MasterID, em.StudentID, em.ExamYear, em.TotalMark, em.PassOrFail, em.CreateTime,
           s.StudentName, s.Mail
    FROM ExamMaster em
    INNER JOIN StudentMst s ON s.StudentID = em.StudentID
    WHERE em.MasterID = @MasterID;

    SELECT d.DtlsID, d.MasterID, d.SubjectID, d.Marks, subj.SubjectName
    FROM ExamDtls d
    INNER JOIN SubjectMst subj ON subj.SubjectID = d.SubjectID
    WHERE d.MasterID = @MasterID
    ORDER BY d.DtlsID;
END;


