# eLearning Platform — Page Workflows

This document describes the runtime workflow of every routed page in the Angular app, including which guards run, what data is fetched, the user actions on the page, and where each action navigates next. All API calls go to `environment.apiUrl` (default `https://localhost:44330/api`) and tokens are attached automatically by [auth.interceptor.ts](src/app/core/interceptors/auth.interceptor.ts).

---

## 1. Routing & Guards Overview

Top-level routes are declared in [app.routes.ts](src/app/app.routes.ts). Every feature route is **lazy-loaded** via `loadChildren` / `loadComponent`, so the chunk only downloads when the user navigates there.

| Path | Guard(s) | Required Role |
|---|---|---|
| `/` | — | Public |
| `/auth/**` | — | Public |
| `/courses/**` | — | Public |
| `/dashboard/**` | `authGuard` | Any authenticated |
| `/quiz/**` | `authGuard` | Any authenticated |
| `/profile` | `authGuard` | Any authenticated |
| `/instructor/**` | `authGuard` + `roleGuard` | Instructor (role `2`) |
| `/admin/**` | `authGuard` + `roleGuard` | Admin (role `3`) |
| `**` | — | Redirects to `/` |

**Guard behavior:**
- [authGuard](src/app/core/guards/auth.guard.ts) — checks `AuthService.getToken()` is present and `isTokenValid()` (JWT exp claim). If not, redirects to `/auth/login` with `returnUrl` in query params.
- [roleGuard](src/app/core/guards/role.guard.ts) — reads required roles from `route.data.roles` and calls `AuthService.hasRole()`. On failure, redirects to `/unauthorized`.

**Authentication state** is held in two `BehaviorSubject`s inside [AuthService](src/app/core/services/auth.service.ts). On app boot, `loadStoredUser()` decodes the JWT from `localStorage.auth_token`, hydrates `currentUser$`, and flips `_isAuthenticated$` to true. Logout clears both tokens and resets the subjects.

---

## 2. Public Pages

### 2.1 Home — `/`
**Component:** [home.component.ts](src/app/features/home/components/home/home.component.ts)

**On load:**
1. Subscribes to `AuthService.getCurrentUser$()` — used to switch the greeting between "Welcome back, {firstName}" (logged in) and "Welcome to eLearn" (guest).
2. Calls four `CourseService` endpoints in parallel:
   - `getCategories()` → category nav bar
   - `getRecommended(10)` → "Recommended for you" / "Top picks" row
   - `getPopular(10)` → "Most popular" row
   - `getTopRated(10)` → "Top rated" row
3. Each row uses the [course-row](src/app/shared/components/course-row/course-row.component.ts) component, which renders a horizontally scrolling list of [course-card](src/app/shared/components/course-card/course-card.component.ts) items. While the request is pending each row shows a 5-card skeleton.

**User actions:**
- Click a category chip → `/courses?category={c}`
- Click a course card → `/courses/{id}`
- Click "Sign up to get personal recommendations" (guests only) → `/auth/register`
- Click "Add occupation and interests" (logged in) → `/profile`
- Header search submit → `/courses?q={term}`

---

### 2.2 Course List — `/courses`
**Component:** [course-list.component.ts](src/app/features/courses/components/course-list/course-list.component.ts)

**On load:** builds the filter form (search, category, level, minPrice, maxPrice) and calls `CourseService.getAllCourses()`. The hardcoded `categories` and `levels` arrays populate the dropdowns.

**Reactive filtering:**
- `searchTerm` field has a `debounceTime(300)` and triggers `CourseService.searchCourses(term)`.
- All other filter fields trigger `debounceTime(500)` → `CourseService.filterCourses(...)`.
- When all filters are empty, the original `courses` array is restored locally without a request.

**Pagination** is client-side: `paginatedCourses` slices the filtered array with `currentPage` × `pageSize` (default 12).

**User actions:**
- Click course → `/courses/{id}`
- Click "Enroll" button (stopPropagation) → if no token, `/auth/login?returnUrl=/courses/{id}`; otherwise `/courses/{id}`.

---

### 2.3 Course Detail — `/courses/:id`
**Component:** [course-detail.component.ts](src/app/features/courses/components/course-detail/course-detail.component.ts)

**On load (reacts to `route.params`):**
1. `CourseService.getCourseById(id)` — loads title, description, instructor, sections, etc.
2. If a token exists, `EnrollmentService.getEnrollments()` — sets `isEnrolled` if the user is already enrolled.
3. `CourseService.getCourseReviews(id)` — populates the reviews tab.

**User actions:**
- **Enroll** → if no token, redirect to login with `returnUrl=/courses/{id}`. Otherwise `EnrollmentService.enrollCourse(id)` → on success, navigates to `/dashboard/my-courses/{id}`.
- **Write a review** → opens an inline form (rating 1–5, title ≥ 5 chars, content ≥ 10 chars). Submit calls `CourseService.addReview(id, payload)` then reloads the detail. Unauthenticated users are redirected to login.

---

## 3. Auth Pages

All three components share the same outer container; they use Reactive Forms with field-level validation and submit through [AuthService](src/app/core/services/auth.service.ts).

### 3.1 Login — `/auth/login`
**Component:** [login.component.ts](src/app/features/auth/components/login/login.component.ts)

- Form: `email` (required, email format), `password` (required, ≥ 6 chars).
- Submit → `AuthService.login({ email, password })`. On success, `AuthService.tap()` stores the access + refresh tokens in `localStorage` and rehydrates `currentUser$`.
- Navigates to `route.snapshot.queryParams.returnUrl` if present, else `/dashboard`.
- Error path: surfaces `error.error.message` or a default message.

### 3.2 Register — `/auth/register`
**Component:** [register.component.ts](src/app/features/auth/components/register/register.component.ts)

- Form fields: firstName, lastName, email, password (must match `/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$/`), confirmPassword, userType (`student` | `instructor`), agreeTerms (required true).
- Cross-field validator `passwordMatchValidator` enforces password === confirmPassword.
- Submit → `AuthService.register(...)` with `role` translated from `userType` (`student` → 1, `instructor` → 2). On success, shows a "Registration successful! Redirecting..." flash and after 2 s navigates to `/auth/login`.

### 3.3 Forgot Password — `/auth/forgot-password`
**Component:** [forgot-password.component.ts](src/app/features/auth/components/forgot-password/forgot-password.component.ts)

Two-step in-page flow tracked by `currentStep` (`'email' | 'reset'`):

1. **Email step:** user submits email → `AuthService.requestPasswordReset(email)`. On success, sets `currentStep = 'reset'`.
2. **Reset step:** form requires `code` (≥ 6 chars), new password (regex above), confirm. Submit → `AuthService.resetPassword({ email, code, newPassword })`. On success, after 2 s navigates to `/auth/login`.
3. **Back** button on the reset step resets the reset form and returns to step 1.

---

## 4. Student Pages (require auth)

### 4.1 Student Dashboard — `/dashboard`
**Component:** [student-dashboard.component.ts](src/app/features/student-dashboard/components/dashboard/student-dashboard.component.ts)

**On load (3 parallel calls):**
- `EnrollmentService.getEnrollments()` → enrolled course list
- `UserService.getRecommendedCourses()` → "Recommended for you"
- `UserService.getUserProgress()` → aggregated progress stats

**User actions:**
- Continue learning a course → `/dashboard/my-courses/{courseId}`
- View a recommended course → `/courses/{courseId}`
- Progress percent is computed as `completedLessons / totalLessons` per enrollment.

### 4.2 Enrolled Course Player — `/dashboard/my-courses/:courseId`
**Component:** [enrolled-course.component.ts](src/app/features/student-dashboard/components/enrolled-course/enrolled-course.component.ts)

**On load:**
1. `CourseService.getCourseById(courseId)` → loads sections + lessons.
2. Auto-selects the first section and its first lesson.
3. `EnrollmentService.getEnrollments()` → finds the matching enrollment to track progress.

**User actions:**
- Click any section / lesson → updates `selectedSection` / `selectedLesson`.
- **Mark complete** → `EnrollmentService.updateLessonProgress(enrollmentId, { lessonId, isCompleted: true, watchedSeconds })`, then refetches enrollments.
- **Go to quiz** → `/quiz/{quizId}`
- **Download certificate** (when `enrollment.certificateId` exists) → `/dashboard/certificate/{certificateId}` *(target route not yet wired in `app.routes.ts`)*.

The lesson progress bar is rendered by the small [lesson-progress.component.ts](src/app/features/student-dashboard/components/enrolled-course/lesson-progress.component.ts) child component (Input: `progress` object with `watchedSeconds` / `durationSeconds`).

### 4.3 Quiz Taking — `/quiz/:id`
**Component:** [quiz-taking.component.ts](src/app/features/quiz/components/quiz-taking/quiz-taking.component.ts)

**On load:** `QuizService.getQuiz(id)` → builds a `FormArray` of answer groups (one per question, with `questionId`, `selectedAnswerId`, `textAnswer`). Then `startTimer()` runs an RxJS `interval(1000)` countdown from `quiz.timeLimit * 60` seconds.

**User actions:**
- `currentQuestionIndex` drives the rendered question. Navigation via **Previous**, **Next**, or click on the question-number sidebar.
- Submit (or timer hits 0) → `QuizService.submitQuiz(id, { answers })` → navigates to `/quiz/result/{response.result.id}`.

Cleanup in `ngOnDestroy`: clears the timer interval and completes the `destroy$` Subject.

### 4.4 Quiz Result — `/quiz/result/:id`
**Component:** [quiz-result.component.ts](src/app/features/quiz/components/quiz-result/quiz-result.component.ts)

**On load:** `QuizService.getQuizResult(resultId)` → renders score, percentage, per-question feedback. `getGradeColor` / `getGradeLabel` map the percentage to a color and label (Excellent / Very Good / Good / Satisfactory / Needs Improvement).

**User actions:**
- **Retake quiz** → `/quiz/{quizResult.quizId}`
- **Back to dashboard** → `/dashboard`

### 4.5 Profile — `/profile`
**Component:** [profile.component.ts](src/app/features/user-profile/components/profile/profile.component.ts)

Two forms on the same page, switched by `activeTab` / `showPasswordForm`:

- **Profile form** — firstName, lastName, email (disabled), phoneNumber, bio (≤ 500 chars). Loaded with `UserService.getProfile()` (`patchValue`) and saved via `UserService.updateProfile(...)`.
- **Password form** — currentPassword, newPassword (regex), confirmPassword + `passwordMatchValidator`. Saved via `UserService.changePassword(...)`.

Both forms show a 3-second auto-dismissing success banner; errors persist until next submit.

---

## 5. Instructor Pages (require role 2)

Currently scaffolded as placeholders — they render shell layouts and a "coming soon" panel.

### 5.1 Instructor Dashboard — `/instructor`
**Component:** [instructor-dashboard.component.ts](src/app/features/instructor/components/instructor-dashboard/instructor-dashboard.component.ts)

Four KPI tiles (Total Courses, Total Students, Total Revenue, Avg. Rating — all hardcoded to 0 today) and a "My Courses" panel.

**User actions:**
- Click **Create Course** → `/instructor/create-course`

### 5.2 Create Course — `/instructor/create-course`
**Component:** [create-course.component.ts](src/app/features/instructor/components/create-course/create-course.component.ts)

Placeholder page; the form is not implemented yet. Back link returns to `/instructor`. When implemented it will call `CourseService.createCourse(request)`.

### 5.3 Manage Course — `/instructor/manage/:courseId`
**Component:** [manage-course.component.ts](src/app/features/instructor/components/manage-course/manage-course.component.ts)

Reads `:courseId` from route params (placeholder for now). Will eventually load the course via `CourseService.getCourseById(id)` and let the instructor edit sections / lessons via `CourseService.updateCourse(...)`.

---

## 6. Admin Pages (require role 3)

Same placeholder pattern as instructor — fully wired routes, minimal UI.

### 6.1 Admin Dashboard — `/admin`
**Component:** [admin-dashboard.component.ts](src/app/features/admin/components/admin-dashboard/admin-dashboard.component.ts)

Four KPI tiles (Total Users, Total Courses, Total Revenue, Pending Approvals) and four navigation cards linking to the four admin sub-pages.

### 6.2 User Management — `/admin/users`
[user-management.component.ts](src/app/features/admin/components/user-management/user-management.component.ts) — placeholder. Will use `UserService` admin endpoints.

### 6.3 Course Management — `/admin/courses`
[course-management.component.ts](src/app/features/admin/components/course-management/course-management.component.ts) — placeholder. Will approve / moderate courses.

### 6.4 Payment Management — `/admin/payments`
[payment-management.component.ts](src/app/features/admin/components/payment-management/payment-management.component.ts) — placeholder. Will use `PaymentService` to list transactions and process refunds.

### 6.5 Reports — `/admin/reports`
[reports.component.ts](src/app/features/admin/components/reports/reports.component.ts) — placeholder grid of 4 stat panels (User Statistics, Revenue Statistics, Course Performance, Student Progress).

---

## 7. Cross-Cutting Concerns

### Shared Header — `app-header`
[header.component.ts](src/app/shared/components/header/header.component.ts) is embedded in pages that need it (currently only Home; auth/dashboard pages have their own chrome). It subscribes to `currentUser$` and `isAuthenticated$`, renders:

- Logo → `/`
- Explore link → `/courses`
- Search bar (submits to `/courses?q=...`)
- "Teach" link if `user.role === 2`
- "My learning" link if authenticated
- Logout button (clears tokens, navigates to `/`) or Log in / Sign up buttons for guests.

### HTTP layer
- [environment.ts](src/environments/environment.ts) sets `apiUrl` and the Stripe publishable key.
- [auth.interceptor.ts](src/app/core/interceptors/auth.interceptor.ts) attaches `Authorization: Bearer {token}` to every outgoing request when `auth_token` is in localStorage.

### Services
| Service | Endpoint prefix | Used by |
|---|---|---|
| `AuthService` | `/auth` | login, register, forgot-password, header, guards |
| `CourseService` | `/courses` | home, course-list, course-detail, enrolled-course |
| `EnrollmentService` | (enrollments) | course-detail, student-dashboard, enrolled-course |
| `UserService` | (user/profile) | profile, student-dashboard |
| `QuizService` | (quiz) | quiz-taking, quiz-result |
| `PaymentService` | (payments) | reserved for admin payments + checkout |

### Cleanup pattern
Every long-lived feature component uses a `private destroy$ = new Subject<void>()` and `takeUntil(this.destroy$)` on subscriptions, then `destroy$.next()` + `complete()` in `ngOnDestroy`. The quiz-taking page additionally clears its `setInterval` timer.

---

## 8. End-to-End User Journeys

**New visitor → enrolled student:**
1. Land on `/` → click a course card → `/courses/{id}`
2. Click **Enroll** → redirected to `/auth/login?returnUrl=/courses/{id}`
3. Don't have an account → click **Sign up** → `/auth/register` → submit → back to `/auth/login`
4. Log in → `returnUrl` brings the user back to `/courses/{id}`
5. Click **Enroll** again → `EnrollmentService.enrollCourse(id)` → `/dashboard/my-courses/{id}`
6. Play lessons; click **Mark complete** as each is finished.
7. Reach the section that has a quiz → click **Go to quiz** → `/quiz/{quizId}` → answer → submit → `/quiz/result/{resultId}`.
8. When all lessons completed and `enrollment.certificateId` is set → **Download certificate**.

**Instructor publishing flow (planned):**
1. Register with `userType = 'instructor'` → log in.
2. Header shows **Teach** link → `/instructor`.
3. Click **Create Course** → `/instructor/create-course` → fill form → POST `/courses`.
4. From dashboard, click an existing course → `/instructor/manage/{id}` → edit sections, publish.

**Admin moderation flow (planned):**
1. Log in with role 3 → navigate to `/admin`.
2. Drill into Users / Courses / Payments / Reports for moderation actions.
