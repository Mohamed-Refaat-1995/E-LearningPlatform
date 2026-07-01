# E-Learning Platform — Entity Relationship Diagram

> Generated from the EF Core model (`AppDbContext.OnModelCreating`).
> `User` uses **TPH** (Table-Per-Hierarchy): `Student`, `Instructor`, `Admin` all live in the `Users` table, discriminated by `Role`.

```mermaid
erDiagram
    USER ||--o{ USERSESSION : "has"
    USER ||..|| STUDENT : "TPH (Role)"
    USER ||..|| INSTRUCTOR : "TPH (Role)"
    USER ||..|| ADMIN : "TPH (Role)"

    CATEGORY ||--o{ COURSE : "categorizes"
    INSTRUCTOR ||--o{ COURSE : "creates"
    INSTRUCTOR ||--o{ COUPON : "issues"

    COURSE ||--o{ SECTION : "contains"
    SECTION ||--o{ LESSON : "contains"
    LESSON ||--o| QUIZ : "has (0..1)"
    QUIZ ||--o{ QUESTION : "contains"
    QUESTION ||--o{ ANSWER : "has options"

    STUDENT ||--o{ ENROLLMENT : "enrolls"
    COURSE ||--o{ ENROLLMENT : "has"
    ENROLLMENT ||--o{ LESSONPROGRESS : "tracks"
    LESSON ||--o{ LESSONPROGRESS : "progress of"

    QUIZ ||--o{ QUIZRESULT : "attempted in"
    STUDENT ||--o{ QUIZRESULT : "takes"
    QUIZRESULT ||--o{ STUDENTANSWER : "records"
    QUESTION ||--o{ STUDENTANSWER : "answered by"
    ANSWER ||--o{ STUDENTANSWER : "selected as (0..1)"

    STUDENT ||--o{ REVIEW : "writes"
    COURSE ||--o{ REVIEW : "receives"

    STUDENT ||--o{ ORDER : "places"
    ORDER ||--o{ ORDERITEM : "contains"
    COURSE ||--o{ ORDERITEM : "sold as"
    ORDER ||--o| PAYMENT : "paid by (0..1)"
    PAYMENT ||--o| INVOICE : "generates (0..1)"

    STUDENT ||--o{ CERTIFICATE : "earns"
    COURSE ||--o{ CERTIFICATE : "issued for"
    COURSE ||--o| COUPON : "may target (0..1)"

    USER {
        int Id PK
        string Email UK
        string FirstName
        string LastName
        string PasswordHash
        int Role "discriminator"
        bool IsActive
    }
    STUDENT {
        int Id PK
    }
    INSTRUCTOR {
        int Id PK
    }
    ADMIN {
        int Id PK
        decimal ProfitPercentage
    }
    USERSESSION {
        int Id PK
        int UserId FK
        string SessionToken UK
        bool IsActive
    }
    CATEGORY {
        int Id PK
        string Name UK
        string Description
    }
    COURSE {
        int Id PK
        string Title
        decimal Price
        int CategoryId FK
        int InstructorId FK
        string Level "enum(string)"
        bool IsPublished
    }
    SECTION {
        int Id PK
        int CourseId FK
        string Title
        int DisplayOrder
    }
    LESSON {
        int Id PK
        int SectionId FK
        string Title
        string ContentType
        string VideoUrl
        int DisplayOrder
    }
    QUIZ {
        int Id PK
        int LessonId FK "1:1"
        string Title
        decimal PassingScore
        bool IsPublished
    }
    QUESTION {
        int Id PK
        int QuizId FK
        string QuestionText
        int QuestionType
        int Points
    }
    ANSWER {
        int Id PK
        int QuestionId FK
        string AnswerText
        bool IsCorrect
    }
    ENROLLMENT {
        int Id PK
        int StudentId FK
        int CourseId FK
        decimal PricePaid
        decimal CompletionPercentage
        bool IsRefunded
    }
    LESSONPROGRESS {
        int Id PK
        int EnrollmentId FK
        int LessonId FK
        bool IsCompleted
        int WatchedSeconds
    }
    QUIZRESULT {
        int Id PK
        int QuizId FK
        int StudentId FK
        decimal Score
        int TimeSpentSeconds
    }
    STUDENTANSWER {
        int Id PK
        int QuizResultId FK
        int QuestionId FK
        int StudentId FK
        int SelectedAnswerId FK "nullable"
        bool IsCorrect
    }
    REVIEW {
        int Id PK
        int CourseId FK
        int StudentId FK
        int Rating
        string Title
    }
    ORDER {
        int Id PK
        int StudentId FK
        string Status "enum(string)"
        decimal TotalAmount
        string Currency "enum(string)"
    }
    ORDERITEM {
        int Id PK
        int OrderId FK
        int CourseId FK
        decimal Price
    }
    PAYMENT {
        int Id PK
        int OrderId FK "1:1"
        decimal Amount
        string Status "enum(string)"
        string Currency "enum(string)"
        string PaymentMethod "enum(string)"
    }
    INVOICE {
        int Id PK
        int PaymentId FK "1:1"
        string InvoiceNumber UK
        decimal Amount
        int Currency "enum(int)"
    }
    CERTIFICATE {
        int Id PK
        int StudentId FK
        int CourseId FK
        string CertificateNumber UK
    }
    COUPON {
        int Id PK
        int InstructorId FK
        int CourseId FK "nullable"
        string Code
        string DiscountType "enum(string)"
        decimal DiscountValue
    }
```

## Unique constraints / indexes
- `User.Email`, `UserSession.SessionToken`, `Category.Name`, `Invoice.InvoiceNumber`, `Certificate.CertificateNumber` — unique
- `Enrollment (StudentId, CourseId)` — unique (one enrollment per student/course)
- `LessonProgress (EnrollmentId, LessonId)` — unique
- `Review (CourseId, StudentId)` — unique (one review per student/course)
- `OrderItem (OrderId, CourseId)` — unique
- `Certificate (StudentId, CourseId)` — unique
- `Coupon.Code` — indexed (**not** unique — see note)

## Notes / known model caveats
- All `Cascade` deletes are globally downgraded to `Restrict` in `OnModelCreating`, so no parent hard-delete cascades to children.
- `Invoice.Currency` is stored as `int` while `Payment/Order` currency are stored as strings (inconsistency).
- `Coupon.Code` index is not unique.
- TPH subtype FKs (`Course.InstructorId`, `Student`-typed FKs, etc.) reference the shared `Users` table; the discriminator is not enforced at the DB level.
