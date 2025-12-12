-- Add Student_Insert stored procedure
-- Run this script in your ExamManagementDb database

USE ExamManagementDb;
GO

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
GO

