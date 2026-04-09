-- =============================================================================
-- 007_CoordinationProject.sql
-- Coordinatieprojecten: projecttype, contracttype, schijven, uurtarieven
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Kolommen toevoegen aan Project
-- -----------------------------------------------------------------------------
ALTER TABLE Project
    ADD IsCoordinationProject bit NOT NULL DEFAULT 0;

ALTER TABLE Project
    ADD CoordinationIssuerCompanyId int NULL;

ALTER TABLE Project
    ADD ContractType int NULL;
    -- 1 = Schijven, 2 = Regie, 3 = Gemengd

ALTER TABLE Project
    ADD ProjectDistanceKm decimal(10, 2) NULL;

ALTER TABLE Project
    ADD CONSTRAINT FK_Project_CoordinationIssuerCompany
        FOREIGN KEY (CoordinationIssuerCompanyId)
        REFERENCES IssuerCompany (Id);

-- -----------------------------------------------------------------------------
-- 2. Km-tarief op IssuerCompany
-- -----------------------------------------------------------------------------
ALTER TABLE IssuerCompany
    ADD RatePerKm decimal(10, 2) NULL;

-- -----------------------------------------------------------------------------
-- 3. Schijven per project (bij contracttype = Schijven of Gemengd)
-- -----------------------------------------------------------------------------
CREATE TABLE ProjectContractSlice
(
    Id          int             IDENTITY(1,1)   NOT NULL,
    ProjectId   int             NOT NULL,
    Description nvarchar(255)   NULL,
    Percentage  decimal(5, 2)   NOT NULL,
    SortOrder   int             NOT NULL CONSTRAINT DF_ProjectContractSlice_SortOrder DEFAULT 0,

    CONSTRAINT PK_ProjectContractSlice PRIMARY KEY (Id),
    CONSTRAINT FK_ProjectContractSlice_Project
        FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId) ON DELETE CASCADE
);

-- -----------------------------------------------------------------------------
-- 4. Uurtarieven per persoon op project (bij contracttype = Regie of Gemengd)
-- -----------------------------------------------------------------------------
CREATE TABLE ProjectHourlyRate
(
    Id          int             IDENTITY(1,1)   NOT NULL,
    ProjectId   int             NOT NULL,
    UserId      nvarchar(128)   NOT NULL,
    HourlyRate  decimal(10, 2)  NOT NULL,

    CONSTRAINT PK_ProjectHourlyRate PRIMARY KEY (Id),
    CONSTRAINT FK_ProjectHourlyRate_Project
        FOREIGN KEY (ProjectId) REFERENCES Project (ProjectId) ON DELETE CASCADE,
    CONSTRAINT FK_ProjectHourlyRate_Users
        FOREIGN KEY (UserId) REFERENCES Users (UserID),
    CONSTRAINT UQ_ProjectHourlyRate_ProjectUser
        UNIQUE (ProjectId, UserId)
);

-- -----------------------------------------------------------------------------
-- 5. Standaard uurtarieven per facturatiebedrijf (in instellingen)
-- -----------------------------------------------------------------------------
CREATE TABLE IssuerCompanyUserRate
(
    Id              int             IDENTITY(1,1)   NOT NULL,
    IssuerCompanyId int             NOT NULL,
    UserId          nvarchar(128)   NOT NULL,
    HourlyRate      decimal(10, 2)  NOT NULL,

    CONSTRAINT PK_IssuerCompanyUserRate PRIMARY KEY (Id),
    CONSTRAINT FK_IssuerCompanyUserRate_IssuerCompany
        FOREIGN KEY (IssuerCompanyId) REFERENCES IssuerCompany (Id) ON DELETE CASCADE,
    CONSTRAINT FK_IssuerCompanyUserRate_Users
        FOREIGN KEY (UserId) REFERENCES Users (UserID),
    CONSTRAINT UQ_IssuerCompanyUserRate_CompanyUser
        UNIQUE (IssuerCompanyId, UserId)
);
