export interface Enrollment {
  id: number;
  studentId: number;
  courseId: number;
  courseName?: string;
  pricePaid: number;
  enrolledAt: Date;
  completedAt?: Date;
  completionPercentage: number;
  lessonProgresses?: LessonProgress[];
}

export interface LessonProgress {
  id: number;
  enrollmentId: number;
  lessonId: number;
  isCompleted: boolean;
  watchedSeconds: number;
  completedAt?: Date;
  lastAccessedAt: Date;
}

export interface EnrollmentRequest {
  courseId: number;
}

export interface LessonProgressRequest {
  lessonId: number;
  watchedSeconds: number;
  isCompleted: boolean;
}

export interface StudentProgress {
  totalEnrollments: number;
  completedCourses: number;
  inProgressCourses: number;
  averageProgress: number;
  enrollments: Enrollment[];
}
