# E-Learning Platform — Claude Project Context

## Project Overview
Full-stack e-learning platform (CS 599 Final Diploma Project, Cairo University).
- **Backend:** .NET 10, ASP.NET Core Web API, EF Core, SQL Server LocalDB, JWT, Cloudinary
- **Frontend:** Angular 18 standalone components, Tailwind CSS, RxJS
- **Backend dev URL:** `https://localhost:44330` (IIS Express / Visual Studio)
- **Frontend dev URL:** `http://localhost:4200` (Angular CLI)

---

## Project Structure

```
Project/
  Backend/
    ELearningPlatform.API/           — Controllers, Program.cs, appsettings.json
    ELearningPlatform.Application/   — Services, DTOs
    ELearningPlatform.Core/          — Entities, Interfaces, Enums
    ELearningPlatform.Infrastructure/— DbContext, Repositories, UnitOfWork, Cloudinary
  Frontend/
    elearning-platform/
      src/app/
        core/
          guards/      — auth.guard.ts, role.guard.ts, root-redirect.guard.ts
          services/    — auth.service.ts, course.service.ts, instructor.service.ts
          interceptors/— auth.interceptor.ts (adds Bearer token to all requests)
        features/
          auth/        — login, register, forgot-password
          instructor/  — instructor-dashboard, course-builder, create-course, manage-course
          admin/       — admin-dashboard, user/course/payment management, reports
          student-dashboard/
          courses/     — course listing, course detail
        shared/
          models/      — course.model.ts, user.model.ts
          components/  — header, course-row
```

---

## Authentication & Authorization

### JWT Claims (backend generates both forms)
```csharp
new Claim(ClaimTypes.NameIdentifier, userId.ToString()),  // for .NET internals
new Claim(ClaimTypes.Email, email),
new Claim(ClaimTypes.Role, role),   // full URI — required for RequireRole() policies
new Claim("role", role),            // plain string — required for frontend payload.role
new Claim("userId", userId.ToString())  // custom — read by TryGetUserId() in controllers
```

### Backend Role Policies (Program.cs)
```csharp
options.AddPolicy("AdminOnly",      policy => policy.RequireRole("Admin"));
options.AddPolicy("InstructorOnly", policy => policy.RequireRole("Instructor", "Admin"));
options.AddPolicy("StudentOnly",    policy => policy.RequireRole("Student"));
```

### Frontend Role System
```typescript
// shared/models/user.model.ts
export enum UserRole { Student = 1, Instructor = 2, Admin = 3 }

// app.routes.ts — instructor route
{ path: 'instructor', canActivate: [authGuard, roleGuard], data: { roles: [2] }, ... }
{ path: 'admin',      canActivate: [authGuard, roleGuard], data: { roles: [3] }, ... }
```

### Root Route Redirect (`/`)
`rootRedirectGuard` checks localStorage token → validates expiry → reads role → redirects:
- Valid Instructor token → `/instructor`
- Valid Admin token → `/admin`
- Valid Student token → `/dashboard`
- Expired / missing → `/auth/login`

### Controller TryGetUserId pattern
```csharp
private bool TryGetUserId(out int userId) {
    userId = 0;
    return int.TryParse(User.FindFirst("userId")?.Value, out userId);
}
```

---

## Key Backend Files & Patterns

### Program.cs — JSON options (critical, prevents circular reference 500 errors)
```csharp
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
```

### CourseController — Course create/update uses DTO not entity
`[FromBody] CreateCourseRequest` (in `Application/DTOs/Courses/CreateCourseRequest.cs`)
Fields: `Title, Description, ThumbnailUrl, Price, Category, Level`
InstructorId is set from JWT, NOT from request body.

### Section/Lesson endpoints return anonymous projections
`AddSection` and `UpdateSection` return flat anonymous objects, NOT raw `Section` entities.
This prevents `Section → Course → Sections → Section` circular reference.

### Instructor dashboard courses endpoint
`GET /api/instructors/my-courses` — reads instructor ID from JWT (InstructorOnly policy).
Returns clean anonymous object projection. Frontend dashboard uses this, NOT `/{id}/courses`.

### Cloudinary service
- `UploadVideoAsync` → `VideoUploadParams` (MP4, WebM, etc.)
- `UploadFileAsync` → `RawUploadParams` (PDF, ZIP, etc.)
- Resource upload endpoint uses `UploadFileAsync`, video upload uses `UploadVideoAsync`

### Cloudinary credentials (appsettings.json)
```json
"CloudinarySettings": {
  "CloudName": "dncrpvdtk",
  "ApiKey": "565224293972423",
  "ApiSecret": "nOOX3sYRwdmT5SUZqLDuT-idVjc",
  "VideoFolder": "elearning/videos"
}
```

---

## Key Frontend Files & Patterns

### AuthService (`core/services/auth.service.ts`)
- `login()` → saves token, calls `loadStoredUser()` (reads `payload.userId` and `payload.role`)
- `getCurrentUser$()` → BehaviorSubject observable
- `getCurrentUserSnapshot()` → sync read of BehaviorSubject (for guards)
- `hasRole(role)` → checks `currentUser$.value.role`
- `isTokenValid()` → checks JWT `exp` claim against `Date.now()`

### InstructorService (`core/services/instructor.service.ts`)
- `getMyCourses()` → `GET /api/instructors/my-courses` (JWT-authenticated, used by dashboard)
- `getInstructorCourses(id)` → `GET /api/instructors/{id}/courses` (public, not used by dashboard)
- `togglePublish(courseId)` → `PATCH /api/courses/{courseId}/publish`
- `uploadVideo(...)` → `POST .../video` (multipart)
- `uploadResource(...)` → `POST .../resource` (multipart)

### Course Builder (`features/instructor/components/course-builder`)
- `openAddLesson(sectionId)` — blocks if any existing lesson in that section has no video
- `saveCourse()` — validates all lessons have videos → calls `togglePublish` → navigates to `/instructor`
- `submitLesson()` — on success navigates to `/instructor` dashboard

---

## Database

### Seeded data
| Role | ID | Email | Password |
|------|----|-------|----------|
| Admin | 1 | admin@elearning.com | (hashed, check seed) |
| Instructor | 2 | instructor@elearning.com | (hashed) |
| Student | 3 | student@elearning.com | (hashed) |
| Course | 1 | "Complete Web Development Bootcamp" | InstructorId=2 |

### TPH (Table Per Hierarchy) — Users table
Discriminator column: `Role` (int). Student=1, Instructor=2, Admin=3.

### Known test user (registered via API)
Instructor id=1006, email=`instructor.99380@gmail.com`

---

## Known Issues / Remaining Bugs

### Draft courses return 404 from `GET /api/courses/{id}`
`CourseService.GetCourseByIdAsync` filters by `c.IsPublished`. Newly created courses have `IsPublished = false`.
This means `course-builder.component` fails to load a newly created course (`Failed to load course` toast).
**Fix needed:** Either remove `IsPublished` filter for instructor-owned courses, or add an instructor-specific `GET /api/courses/{id}/draft` endpoint.

---

## Common Gotchas

1. **IIS Express locks DLLs** — Always stop the backend in Visual Studio before running `dotnet build`. Use `Ctrl+Shift+B` in Visual Studio instead.

2. **`ReferenceHandler.IgnoreCycles` must be deployed** — Without it, any endpoint returning EF entities with navigation properties will 500. Restart backend after every rebuild.

3. **Never return raw EF entities from API endpoints** — Always project to anonymous objects or DTOs to avoid circular reference exceptions (e.g., `Course → Instructor → CreatedCourses → Course`).

4. **JWT role claim** — Backend must include BOTH `ClaimTypes.Role` (for `RequireRole` policies) AND `"role"` plain claim (for `payload.role` in frontend). If only `ClaimTypes.Role` is present, frontend `loadStoredUser` reads `undefined` and defaults to `Student`, breaking instructor/admin access.

5. **Instructor ID from JWT** — Always use `User.FindFirst("userId")` in controllers, not `ClaimTypes.NameIdentifier`. The `nameid` claim in JWT maps to `NameIdentifier` but the custom `"userId"` claim is more reliable.
