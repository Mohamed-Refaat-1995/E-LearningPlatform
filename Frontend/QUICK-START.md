# Frontend Quick Start Guide

## Project Structure

```
elearning-platform/
├── src/
│   ├── app/
│   │   ├── core/              # Guards, interceptors, services
│   │   ├── shared/            # Shared models, directives, pipes
│   │   ├── features/          # Feature modules (lazy-loaded)
│   │   │   ├── auth/
│   │   │   ├── courses/
│   │   │   ├── student-dashboard/
│   │   │   ├── quiz/
│   │   │   ├── user-profile/
│   │   │   ├── instructor/
│   │   │   └── admin/
│   │   └── app.routes.ts      # Main routing configuration
│   ├── environment.ts         # API configuration
│   └── styles/                # Global styles
├── angular.json
├── tailwind.config.js
└── package.json
```

## Installation & Setup

### 1. Install Dependencies
```bash
cd elearning-platform
npm install
```

### 2. Configure Environment
Edit `src/environment.ts`:
```typescript
export const environment = {
  apiUrl: 'https://localhost:5001/api',
  stripePublishableKey: 'your-stripe-key'
};
```

### 3. Start Development Server
```bash
npm start
# or
ng serve
```

The app will be available at `http://localhost:4200`

### 4. Build for Production
```bash
npm run build:prod
# or
ng build --configuration production
```

## Available Routes

### Public Routes
- `/auth/login` - Login page
- `/auth/register` - Registration page
- `/auth/forgot-password` - Password reset
- `/courses` - Browse all courses
- `/courses/:id` - Course detail

### Protected Routes (requires login)
- `/dashboard` - Student dashboard
- `/dashboard/my-courses/:courseId` - Enrolled course view
- `/quiz/:id` - Take quiz
- `/quiz/result/:id` - Quiz results
- `/profile` - User profile

### Instructor Routes (requires login + instructor role)
- `/instructor` - Instructor dashboard
- `/instructor/create-course` - Create course
- `/instructor/manage/:courseId` - Manage course

### Admin Routes (requires login + admin role)
- `/admin` - Admin dashboard
- `/admin/users` - User management
- `/admin/courses` - Course management
- `/admin/payments` - Payment management
- `/admin/reports` - Reports

## Testing the Application

### 1. Authentication Flow
1. Go to `http://localhost:4200/auth/register`
2. Create a new account (student)
3. Login with created credentials
4. Should redirect to `/dashboard`

### 2. Course Browsing
1. Go to `http://localhost:4200/courses`
2. Use filters (category, level, price)
3. Search for courses
4. Click on a course to see details
5. View reviews and instructor info

### 3. Course Enrollment
1. Browse courses (no login needed)
2. Click on a course
3. Click "Enroll Now" button
4. Should redirect to login if not authenticated
5. After login, enrollment should complete

### 4. Student Dashboard
1. Login as student
2. View "My Courses" section
3. Click "Continue Learning" on a course
4. Navigate through lessons
5. Mark lessons as complete

### 5. Quiz Feature
1. From enrolled course, access quiz
2. Answer questions (multiple choice, true/false, short answer)
3. Timer should count down
4. Submit quiz
5. View results with answer review

### 6. Profile Management
1. Go to `/profile`
2. Edit profile information
3. Change password
4. Verify success message

## Demo Credentials (from backend seeding)

**Student Account:**
- Email: `student@elearning.com`
- Password: `Student@123`

**Instructor Account:** (if seeded)
- Email: `instructor@elearning.com`
- Password: `Instructor@123`

**Admin Account:** (if seeded)
- Email: `admin@elearning.com`
- Password: `Admin@123`

## Common Issues & Solutions

### Issue: CORS Error
**Solution**: Ensure backend is running and CORS is configured for `http://localhost:4200`

### Issue: 401 Unauthorized Errors
**Solution**: Check that token is stored in localStorage and token is not expired. Login again if needed.

### Issue: Modules Not Found
**Solution**: Ensure all feature modules are properly imported in app.routes.ts

### Issue: Styling Not Applied
**Solution**: Ensure Tailwind CSS is built. Run `npm start` which watches for changes.

## Development Tips

### 1. Adding a New Feature
1. Create new folder under `features/`
2. Create `components/` subdirectory
3. Create `.component.ts` and `.component.html` files
4. Create `[feature].routes.ts` file
5. Import in `app.routes.ts`

### 2. Using Services
All services are in `core/services/`:
- `auth.service.ts` - Authentication
- `course.service.ts` - Course management
- `enrollment.service.ts` - Enrollment tracking
- `quiz.service.ts` - Quiz operations
- `user.service.ts` - User profile
- `payment.service.ts` - Payments

### 3. TypeScript Strict Mode
All components use TypeScript strict mode. Ensure proper typing:
```typescript
enrollments: Course[] = []; // explicit type
```

### 4. Reactive Forms
Use FormBuilder for all forms:
```typescript
this.form = this.formBuilder.group({
  email: ['', [Validators.required, Validators.email]],
  password: ['', [Validators.required, Validators.minLength(6)]]
});
```

### 5. RxJS Cleanup
Always unsubscribe in ngOnDestroy:
```typescript
private destroy$ = new Subject<void>();

ngOnDestroy(): void {
  this.destroy$.next();
  this.destroy$.complete();
}

// In subscribe:
this.service.getData()
  .pipe(takeUntil(this.destroy$))
  .subscribe(...);
```

## Build & Deployment

### Build Artifacts
```bash
npm run build:prod
# Creates dist/elearning-platform/ directory
```

### Deploy to Cloud
1. Build production artifacts
2. Upload `dist/elearning-platform/` to hosting
3. Configure server for SPA (route all to index.html)
4. Update `environment.prod.ts` with production API URL

### Docker Deployment
```dockerfile
FROM node:18 AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build:prod

FROM nginx:latest
COPY --from=build /app/dist/elearning-platform/ /usr/share/nginx/html/
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

## Performance Optimization

### 1. Lazy Loading
All feature modules are lazy-loaded in `app.routes.ts`. No additional configuration needed.

### 2. Code Splitting
Angular CLI automatically splits bundles. Check with:
```bash
npm run build:prod -- --stats-json
# View with webpack-bundle-analyzer
```

### 3. Image Optimization
- Use WebP format for course thumbnails
- Implement lazy loading for course images
- Use CDN for static assets

### 4. Caching
- Service workers for offline support (future)
- Browser caching via HTTP headers
- Angular's built-in change detection optimization

## Useful Commands

```bash
# Development server
npm start

# Run tests
npm test

# Lint code
npm run lint

# Build for production
npm run build:prod

# Format code
npx prettier --write src/

# Analyze bundle
npm run build:prod -- --stats-json
```

## Environment Variables

Create `.env` file in root directory:
```
NG_APP_API_URL=https://localhost:5001/api
NG_APP_STRIPE_KEY=your_stripe_key
NG_APP_ENVIRONMENT=development
```

Access in code:
```typescript
const apiUrl = process.env['NG_APP_API_URL'];
```

## Support & Troubleshooting

For more information:
- Angular Docs: https://angular.io/docs
- RxJS Docs: https://rxjs.dev
- Tailwind CSS: https://tailwindcss.com
- TypeScript: https://www.typescriptlang.org

Check `FRONTEND-COMPONENTS-CREATED.md` for detailed component documentation.

---

**Last Updated**: 2026-05-02
**Version**: 1.0.0
