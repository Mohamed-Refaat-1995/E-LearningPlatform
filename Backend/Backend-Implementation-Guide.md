# E-Learning Platform Backend - Implementation Guide

## Project Structure Complete

The backend solution has been created with the following complete structure:

```
Backend/
├── ELearningPlatform.Core/              (Domain Layer)
│   ├── Entities/                        (All 15+ entity models)
│   ├── Interfaces/                      (IRepository, IUnitOfWork, Service interfaces)
│   └── Enums/                           (UserRole enum)
│
├── ELearningPlatform.Infrastructure/    (Data & External Services)
│   ├── DbContext/                       (AppDbContext with all configurations)
│   ├── Repositories/                    (Generic Repository<T> implementation)
│   ├── UnitOfWork/                      (UnitOfWork pattern implementation)
│   ├── AWS/                             (S3 service implementation)
│   └── Stripe/                          (Payment service implementation)
│
├── ELearningPlatform.Application/       (Business Logic)
│   ├── Services/                        (All business logic services)
│   ├── DTOs/                            (Request/Response objects)
│   ├── Validators/                      (FluentValidation validators)
│   └── MappingProfiles/                 (AutoMapper profiles)
│
└── ELearningPlatform.API/              (Controllers & Middleware)
    ├── Controllers/                     (All REST endpoints)
    ├── Middleware/                      (Error handling, auth)
    └── Extensions/                      (DI setup, configuration)
```

## Database Setup

### Connection String Configuration
Add to `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=ELearningPlatform;User Id=sa;Password=your-password;TrustServerCertificate=true;"
  },
  "JwtSettings": {
    "SecretKey": "your-very-long-secret-key-at-least-32-characters",
    "Issuer": "YourAppName",
    "Audience": "YourAppUsers",
    "ExpirationMinutes": 60
  },
  "AwsSettings": {
    "AccessKey": "your-aws-access-key",
    "SecretKey": "your-aws-secret-key",
    "BucketName": "your-bucket-name",
    "Region": "us-east-1"
  },
  "StripeSettings": {
    "SecretKey": "sk_test_your-stripe-secret-key",
    "PublishableKey": "pk_test_your-stripe-publishable-key",
    "WebhookSecret": "whsec_your-webhook-secret"
  }
}
```

### Create Database Migrations
```bash
cd Backend/ELearningPlatform.API
dotnet ef migrations add Initial -p ../ELearningPlatform.Infrastructure -c AppDbContext
dotnet ef database update
```

## Services Implementation

### Completed Services

1. **AuthService** ✓
   - User registration with password hashing
   - Login with JWT token generation
   - Refresh token mechanism
   - Token validation and claims extraction

2. **CourseService** ✓
   - CRUD operations for courses
   - Search and filtering by category, level, price
   - Rating management and calculations
   - Review management

### Services To Complete (Follow Similar Patterns)

**EnrollmentService:**
- Enroll student in course
- Track lesson progress
- Calculate completion percentage
- Get student's enrolled courses

**QuizService:**
- Create and manage quizzes
- Submit quiz answers
- Auto-grade quizzes
- Track quiz results and scores

**PaymentService:**
- Create payment records
- Process Stripe payments
- Generate invoices
- Handle refunds

**UserService:**
- Manage user profiles
- Password changes
- Progress tracking
- Recommended courses

**AwsS3Service:**
- Upload videos to S3
- Generate pre-signed URLs
- Delete files
- Stream video content

**CertificateService:**
- Generate certificates on course completion
- Create PDF certificates
- Track issued certificates

## API Endpoints Structure

### Authentication Endpoints
```
POST   /api/auth/register
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
```

### Course Endpoints
```
GET    /api/courses
GET    /api/courses/{id}
GET    /api/courses/search
GET    /api/courses/filter
POST   /api/courses (Instructor only)
PUT    /api/courses/{id} (Instructor only)
DELETE /api/courses/{id} (Admin only)
GET    /api/courses/{id}/reviews
POST   /api/courses/{id}/reviews
```

### Enrollment Endpoints
```
GET    /api/enrollments
GET    /api/enrollments/{id}
POST   /api/enrollments
POST   /api/courses/{courseId}/enroll
GET    /api/enrollments/{id}/progress
PUT    /api/enrollments/{id}/progress
```

### Quiz Endpoints
```
GET    /api/quizzes
GET    /api/quizzes/{id}
POST   /api/quizzes (Instructor only)
PUT    /api/quizzes/{id} (Instructor only)
DELETE /api/quizzes/{id} (Admin only)
POST   /api/quizzes/{id}/submit
GET    /api/quizzes/{id}/results
```

### Payment Endpoints
```
POST   /api/payments
POST   /api/payments/webhook
GET    /api/payments/{id}
GET    /api/invoices
GET    /api/invoices/{id}
POST   /api/payments/{id}/refund
```

### User Endpoints
```
GET    /api/users/profile
PUT    /api/users/profile
PUT    /api/users/password
GET    /api/users/{id}/progress
GET    /api/users/{id}/certificates
GET    /api/users/recommendations
```

### Video Endpoints
```
POST   /api/videos/upload
GET    /api/videos/{id}/signed-url
DELETE /api/videos/{id}
```

## Service Dependencies Injection

Register all services in `Program.cs`:
```csharp
// Database
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// UnitOfWork
services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<ICourseService, CourseService>();
services.AddScoped<IEnrollmentService, EnrollmentService>();
services.AddScoped<IQuizService, QuizService>();
services.AddScoped<IPaymentService, PaymentService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IAwsS3Service, AwsS3Service>();
services.AddScoped<ICertificateService, CertificateService>();

// AutoMapper
services.AddAutoMapper(typeof(MappingProfile));

// Validation
services.AddFluentValidation();

// JWT Authentication
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        // Configure JWT options
    });
```

## Testing the API

### Using Postman/Insomnia

1. **Register a new user:**
   ```
   POST /api/auth/register
   {
     "firstName": "John",
     "lastName": "Doe",
     "email": "john@example.com",
     "password": "SecurePassword123!",
     "confirmPassword": "SecurePassword123!",
     "role": 1
   }
   ```

2. **Login:**
   ```
   POST /api/auth/login
   {
     "email": "john@example.com",
     "password": "SecurePassword123!"
   }
   ```

3. **Use returned JWT token in subsequent requests:**
   ```
   Authorization: Bearer {token}
   ```

## Seeded Data

The database includes sample data:
- **Admin User:** admin@elearning.com / Admin@123
- **Instructor:** instructor@elearning.com / Instructor@123
- **Student:** student@elearning.com / Student@123

## Next Steps

1. Implement remaining services following the patterns established
2. Create FluentValidation validators for all DTOs
3. Create AutoMapper mapping profiles
4. Implement error handling middleware
5. Add Swagger/OpenAPI documentation
6. Create unit tests for services
7. Setup CI/CD pipeline with GitHub Actions
8. Configure CORS for Angular frontend
9. Implement refresh token storage (Redis or database)
10. Add logging with Serilog

## Notes

- All passwords are hashed using BCrypt.Net
- JWT tokens expire in 60 minutes (configurable)
- Soft delete implemented with IsDeleted flag
- All entities have CreatedAt and UpdatedAt timestamps
- Use IConfiguration for all sensitive settings
- Store AWS and Stripe keys in environment variables or Azure Key Vault
