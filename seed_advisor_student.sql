-- ============================================================
-- URMS: Seed Academic Advisor + Student (linked together)
-- Password for BOTH accounts: P@ssword123
-- Run this in SSMS after the app has started at least once
-- ============================================================

-- Pre-computed ASP.NET Identity V3 hash for "P@ssword123"
DECLARE @PasswordHash NVARCHAR(MAX) = N'AQAAAAEAAYagAAAAELjNj/4+e9FaxRCX6VTsNPq4AT6kPKehzdBGoLa8cwJOXerIklfyG2Q1dBiDM6spSA==';

-- ============================================================
-- Step 1: Create the Advisor user
-- ============================================================
DECLARE @AdvisorId NVARCHAR(450) = NEWID();
DECLARE @AdvisorEmail NVARCHAR(256) = N'dr.ahmed@urms.edu.eg';

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE NormalizedEmail = UPPER(@AdvisorEmail))
BEGIN
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        FirstNameAr, LastNameAr, FirstNameEn, LastNameEn,
        UserType, IsApproved, IsActive, CreatedAt
    )
    VALUES (
        @AdvisorId,
        @AdvisorEmail, UPPER(@AdvisorEmail),
        @AdvisorEmail, UPPER(@AdvisorEmail),
        1,                          -- EmailConfirmed
        @PasswordHash,              -- P@ssword123
        UPPER(NEWID()),             -- SecurityStamp
        NEWID(),                    -- ConcurrencyStamp
        0, 0, 1, 0,                -- PhoneConfirmed, 2FA, LockoutEnabled, FailedCount
        N'د. أحمد', N'محمود',       -- Arabic name
        N'Dr. Ahmed', N'Mahmoud',   -- English name
        2,                          -- UserType = AcademicAdvisor
        1, 1,                       -- IsApproved, IsActive
        GETUTCDATE()
    );

    -- Assign "AcademicAdvisor" role
    DECLARE @AdvisorRoleId NVARCHAR(450);
    SELECT @AdvisorRoleId = Id FROM AspNetRoles WHERE NormalizedName = N'ACADEMICADVISOR';

    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@AdvisorId, @AdvisorRoleId);

    -- Create AcademicAdvisor entity record
    INSERT INTO AcademicAdvisors (UserId, AdvisorCode, CreatedAt)
    VALUES (@AdvisorId, N'ADV-002', GETUTCDATE());

    PRINT N'Advisor created: dr.ahmed@urms.edu.eg / P@ssword123';
END
ELSE
BEGIN
    SELECT @AdvisorId = Id FROM AspNetUsers WHERE NormalizedEmail = UPPER(@AdvisorEmail);
    PRINT N'Advisor already exists, skipping...';
END


-- ============================================================
-- Step 2: Create the Student user (linked to the Advisor)
-- ============================================================
DECLARE @StudentId NVARCHAR(450) = NEWID();
DECLARE @StudentEmail NVARCHAR(256) = N'omar.student@urms.edu.eg';

IF NOT EXISTS (SELECT 1 FROM AspNetUsers WHERE NormalizedEmail = UPPER(@StudentEmail))
BEGIN
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        FirstNameAr, LastNameAr, FirstNameEn, LastNameEn,
        UserType, IsApproved, IsActive, CreatedAt
    )
    VALUES (
        @StudentId,
        @StudentEmail, UPPER(@StudentEmail),
        @StudentEmail, UPPER(@StudentEmail),
        1,
        @PasswordHash,              -- P@ssword123
        UPPER(NEWID()),
        NEWID(),
        0, 0, 1, 0,
        N'عمر', N'خالد',            -- Arabic name
        N'Omar', N'Khaled',         -- English name
        1,                          -- UserType = Student
        1, 1,                       -- IsApproved, IsActive
        GETUTCDATE()
    );

    -- Assign "Student" role
    DECLARE @StudentRoleId NVARCHAR(450);
    SELECT @StudentRoleId = Id FROM AspNetRoles WHERE NormalizedName = N'STUDENT';

    INSERT INTO AspNetUserRoles (UserId, RoleId)
    VALUES (@StudentId, @StudentRoleId);

    -- Create Student entity (linked to Advisor!)
    INSERT INTO Students (UserId, UniversityCode, NationalId, AcademicAdvisorId, CreatedAt)
    VALUES (@StudentId, N'20230055', N'30001011234567', @AdvisorId, GETUTCDATE());

    -- Pre-registration lookup
    INSERT INTO AdvisorStudentAssignments (UniversityCode, AdvisorId, AssignedAt, CreatedAt)
    VALUES (N'20230055', @AdvisorId, GETUTCDATE(), GETUTCDATE());

    PRINT N'Student created: omar.student@urms.edu.eg / P@ssword123';
END
ELSE
    PRINT N'Student already exists, skipping...';


-- ============================================================
-- Step 3: Verify
-- ============================================================
PRINT N'';
PRINT N'========== CREATED ACCOUNTS ==========';

SELECT u.Email, u.FirstNameAr + N' ' + u.LastNameAr AS [NameAr],
       CASE u.UserType WHEN 2 THEN N'AcademicAdvisor' WHEN 1 THEN N'Student' END AS [Role]
FROM AspNetUsers u
WHERE u.NormalizedEmail IN (UPPER(N'dr.ahmed@urms.edu.eg'), UPPER(N'omar.student@urms.edu.eg'));

PRINT N'========== ADVISOR-STUDENT LINK ==========';

SELECT s.UniversityCode, su.Email AS [StudentEmail],
       au.Email AS [AdvisorEmail], au.FirstNameAr + N' ' + au.LastNameAr AS [AdvisorName]
FROM Students s
INNER JOIN AspNetUsers su ON su.Id = s.UserId
INNER JOIN AspNetUsers au ON au.Id = s.AcademicAdvisorId
WHERE su.NormalizedEmail = UPPER(N'omar.student@urms.edu.eg');
