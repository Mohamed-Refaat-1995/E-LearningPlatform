# E-Learning Platform — Frontend CLAUDE.md

See full project context in the parent CLAUDE.md:
`../../CLAUDE.md`

## Quick Reference — Frontend

- **Angular 18** standalone components, Tailwind CSS, RxJS
- **API base URL:** `https://localhost:44330/api` (environment.ts)
- **Working directory:** `Frontend/elearning-platform/`

## Key services
| Service | Location | Purpose |
|---------|----------|---------|
| `AuthService` | `core/services/auth.service.ts` | Login, JWT decode, role checks |
| `InstructorService` | `core/services/instructor.service.ts` | Sections, lessons, videos, my-courses |
| `CourseService` | `core/services/course.service.ts` | Public course CRUD |

## Role enum
```typescript
export enum UserRole { Student = 1, Instructor = 2, Admin = 3 }
```

## Routes
- `/` → rootRedirectGuard (redirects by role or to login)
- `/auth/login` → LoginComponent
- `/instructor` → InstructorDashboardComponent (roleGuard: roles=[2])
- `/instructor/create-course` → CreateCourseComponent
- `/instructor/course-builder/:courseId` → CourseBuilderComponent
- `/admin` → AdminDashboardComponent (roleGuard: roles=[3])
- `/dashboard` → Student dashboard

## Known remaining bug
Draft courses (isPublished=false) return 404 from `GET /api/courses/:id`.
Course builder's `loadCourseAndSections()` will show "Failed to load course" for newly created courses.
Fix: backend `GetCourseByIdAsync` needs to not filter by `IsPublished` for instructor-owned courses.
