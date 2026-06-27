export interface Course {
  id: number;
  title: string;
  description: string;
  thumbnailUrl?: string;
  price: number;
  category: string;
  level: string;
  instructorId: number;
  instructorName?: string;
  totalStudents: number;
  averageRating: number;
  totalReviews: number;
  isPublished: boolean;
  publishedAt: Date;
  createdAt: Date;
  updatedAt: Date;
  sections?: Section[];
  reviews?: Review[];
}

export interface Section {
  id: number;
  title: string;
  description?: string;
  courseId: number;
  displayOrder: number;
  lessons?: Lesson[];
  isExpanded?: boolean; // UI state only
}

export interface Lesson {
  id: number;
  title: string;
  description?: string;
  sectionId: number;
  displayOrder: number;
  durationMinutes: number;
  isPreview: boolean;
  contents?: LessonContent[];
  isExpanded?: boolean; // UI state only
}

export interface LessonContent {
  id: number;
  lessonId: number;
  contentType: string; // 'Video' | 'Text' | 'Resource'
  videoUrl?: string;
  videoPublicId?: string;
  textContent?: string;
  resourceUrl?: string;
}

export interface CreateCourseRequest {
  title: string;
  description: string;
  category: string;
  level: string;
  price: number;
  thumbnailUrl?: string;
}

export interface SectionRequest {
  title: string;
  description?: string;
}

export interface LessonRequest {
  title: string;
  description?: string;
  durationMinutes: number;
  isPreview: boolean;
}

export interface Review {
  id: number;
  courseId: number;
  studentId: number;
  studentName?: string;
  rating: number;
  title: string;
  content: string;
  helpfulCount: number;
  createdAt: Date;
}

export interface CreateReviewRequest {
  rating: number;
  title: string;
  content: string;
}

export interface Coupon {
  id: number;
  code: string;
  discountType: 'Percentage' | 'Fixed';
  discountValue: number;
  maxDiscountAmount?: number;
  maxUses?: number;
  usedCount: number;
  expiryDate?: Date;
  isActive: boolean;
  instructorId: number;
  courseId?: number;
  createdAt: Date;
}

export interface CreateCouponRequest {
  code: string;
  discountType: 'Percentage' | 'Fixed';
  discountValue: number;
  maxDiscountAmount?: number;
  maxUses?: number;
  expiryDate?: string;
  courseId?: number;
}

export interface InstructorEarnings {
  instructorId: number;
  totalRevenue: number;
  totalStudents: number;
  totalCourses: number;
  courses: CourseEarning[];
}

export interface CourseEarning {
  courseId: number;
  courseTitle: string;
  totalStudents: number;
  pricePerSeat: number;
  totalRevenue: number;
  enrollmentCount: number;
}

export interface EnrolledStudent {
  id: number;
  studentId: number;
  studentName?: string;
  studentEmail?: string;
  courseId: number;
  pricePaid: number;
  enrolledAt: Date;
  completionPercentage: number;
  completedAt?: Date;
}
