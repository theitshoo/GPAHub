# GPAHub — Database ERD

Target: SQL Server · EF Core 10 code-first · migrations in `GPAHub.Infrastructure/Persistence/Migrations/`.

---

## Entity-Relationship Diagram

```mermaid
erDiagram
    STUDENTS ||--o{ SEMESTERS : owns
    STUDENTS ||--o{ COURSES : owns
    STUDENTS ||--o{ GRADESCALES : owns
    STUDENTS ||--o{ SUBSCRIPTIONS : has
    STUDENTS ||--o| REFRESHTOKENS : "sessions (1..n)"
    STUDENTS ||--o{ GPARECORDS : saves
    STUDENTS ||--o{ TARGETPLANS : saves

    GRADESCALES ||--|{ GRADEDEFINITIONS : contains
    SUBSCRIPTIONS ||--o{ PAYMENTS : "paid by"
    COURSES }o--o| SEMESTERS : "grouped in (SET NULL via app detach)"
    GPARECORDS ||--|{ GPARECORDCROURLINES : snapshots
    TARGETPLANS ||--|{ TARGETPLANUPCOMINGCOURSES : lists
    PLANS ||..o{ PAYMENTS : "reference data"

    STUDENTS {
        uniqueidentifier Id PK
        nvarchar Name
        nvarchar Email UK
        nvarchar PasswordHash NULL
        decimal CurrentGpa NULL
        decimal CompletedCreditHours NULL
        rowversion Version
    }
    GRADESCALES {
        uniqueidentifier Id PK
        uniqueidentifier StudentId FK "NULL = system default"
        nvarchar Name
        bit IsActive
        bit EnforceFullCoverage
        rowversion Version
    }
    GRADEDEFINITIONS {
        uniqueidentifier Id PK
        uniqueidentifier GradeScaleId FK
        nvarchar Name
        int MinMark
        int MaxMark
        decimal Points
        rowversion Version
    }
    SEMESTERS {
        uniqueidentifier Id PK
        uniqueidentifier StudentId FK
        nvarchar Name
        rowversion Version
    }
    COURSES {
        uniqueidentifier Id PK
        uniqueidentifier StudentId FK
        uniqueidentifier SemesterId FK "NULL allowed"
        nvarchar Name
        nvarchar Code NULL
        decimal CreditHours "> 0"
        int InputType
        int NumericMark NULL
        nvarchar LetterGrade NULL
        rowversion Version
    }
    SUBSCRIPTIONS {
        uniqueidentifier Id PK
        uniqueidentifier StudentId FK
        int Type "Free=1 Premium=2"
        int Status "Active=1 Expired=2"
        datetimeoffset StartDate
        datetimeoffset EndDate NULL "lifetime"
        rowversion Version
    }
    PAYMENTS {
        uniqueidentifier Id PK
        uniqueidentifier SubscriptionId FK "NULL until activated"
        decimal Amount ">= 0"
        char Currency "3"
        int Status "Pending/Completed/Failed"
        nvarchar ExternalReference UK "idempotency key"
        datetimeoffset OccurredAtUtc
        rowversion Version
    }
    PLANS {
        uniqueidentifier Id PK
        nvarchar Name UK
        nvarchar Features ";-joined flags"
    }
    GPARECORDS {
        uniqueidentifier Id PK
        uniqueidentifier StudentId FK
        int CalculationType
        decimal SemesterGpa
        decimal CumulativeGpa NULL
        decimal TotalCreditHours
        decimal TotalQualityPoints
        datetimeoffset CreatedAtUtc
    }
    GPARECORDCROURLINES {
        uniqueidentifier Id PK
        uniqueidentifier GpaRecordId FK
        nvarchar CourseName
        nvarchar CourseCode NULL
        decimal CreditHours
        nvarchar GradeName
        decimal GpaPoints
        decimal QualityPoints "computed"
    }
    TARGETPLANS {
        uniqueidentifier Id PK
        uniqueidentifier StudentId FK
        decimal TargetGpa
        decimal CurrentGpa
        decimal CompletedCreditHours
        decimal RequiredAverageGpa
        bit IsAchievable
        decimal MaxReachableGpa NULL
        datetimeoffset CreatedAtUtc
    }
    TARGETPLANUPCOMINGCOURSES {
        uniqueidentifier Id PK
        uniqueidentifier TargetPlanId FK
        nvarchar Name
        decimal CreditHours
    }
    REFRESHTOKENS {
        uniqueidentifier Id PK
        uniqueidentifier StudentId FK
        nvarchar TokenHash UK "SHA-256"
        datetimeoffset CreatedAtUtc
        datetimeoffset ExpiresAtUtc
        datetimeoffset RevokedAtUtc NULL
    }
```

---

## Relationships & Delete Behavior

| Relationship | Cardinality | On delete | Notes |
|--------------|-------------|-----------|-------|
| Student → Semester | 1 : 0..* | **Cascade** | |
| Student → Course | 1 : 0..* | **Cascade** | |
| Student → GradeScale | 1 : 0..* (+ system defaults with NULL) | Cascade (owned rows only) | |
| GradeScale → GradeDefinition | 1 : 0..* | **Cascade** | |
| Student → Semester → Course | indirect path | **NoAction on Course.SemesterId** | Avoids SQL Server multiple-cascade-path error; service detaches explicitly (DR-014) |
| Student → Subscription | 1 : 0..* | Cascade | |
| Subscription → Payment | 1 : 0..* | Cascade | `SubscriptionId` nullable: pre-payment records exist without a subscription |
| Student → GpaRecord / TargetPlan | 1 : 0..* | Cascade | Child snapshot lines cascade with parents |
| Student → RefreshToken | 1 : 0..* | Cascade | |

## Indexes

| Table | Index | Purpose |
|-------|-------|---------|
| Students | UNIQUE `Email` | Account identity |
| GradeScales | UNIQUE `(StudentId, Name)` WHERE `StudentId IS NOT NULL` | Scale-name uniqueness per student (system defaults exempt) |
| GradeScales | UNIQUE `StudentId` WHERE `[IsActive] = 1 AND [StudentId] IS NOT NULL` | **One active scale per student**, enforced by the database even if a future bug tries to bypass it |
| GradeScales | `StudentId` filtered `IS NOT NULL` | Active-scale lookup |
| GradeDefinitions | `(GradeScaleId, Name)` UNIQUE | No duplicate grade names inside a scale |
| Courses | `StudentId`, `SemesterId` | Ownership + semester filtering |
| Semesters | `StudentId` | Ownership scans |
| Subscriptions | `StudentId` | Latest-subscription lookup |
| Payments | `ExternalReference` UNIQUE | Webhook idempotency |
| GpaRecords / TargetPlans | `(StudentId, CreatedAtUtc)` | Paged history queries |

## Check Constraints

| Constraint | Rule |
|------------|------|
| `CK_GradeDefinitions_MarkRange` | `MinMark <= MaxMark AND MinMark >= 0 AND MaxMark <= 100` |
| `CK_GradeDefinitions_Points` | `Points >= 0` |
| `CK_Courses_CreditHours` | `CreditHours > 0` |
| `CK_Courses_MarkRange` | `NumericMark IS NULL OR (NumericMark BETWEEN 0 AND 100)` |
| `CK_Payments_Amount` | `Amount >= 0` |
| `CK_Subscriptions_DateRange` | `EndDate IS NULL OR EndDate >= StartDate` |

## Concurrency & Precision

- Every mutable table carries a shadow `rowversion Version` column. Concurrent updates raise `DbUpdateConcurrencyException`, mapped by middleware to **HTTP 409 `concurrency_conflict`**.
- All GPA/hours/points columns use `decimal(18,6)`; credit hours use `(5,2)`; money uses `(18,2)` — full internal precision is preserved and rounding happens only at the API edge (`MidpointRounding.AwayFromZero`, DR-009).
- Insert-only tables (`GpaRecords`, `TargetPlans` children) are never updated after creation; history is append-only.

## Migrations

```bash
dotnet ef migrations add <Name> --project GPAHub.Infrastructure --output-dir Persistence/Migrations
dotnet ef database update --project GPAHub.Infrastructure
```

Applied automatically at application startup. Current set: `InitialCreate`, `ProductionHardening`.
