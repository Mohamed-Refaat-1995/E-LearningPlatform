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
  // Flat content fields as actually returned by GET .../lessons (no nested "contents" array server-side)
  contentType?: string;
  videoUrl?: string;
  videoPublicId?: string;
  textContent?: string;
  resourceUrl?: string;
  contents?: LessonContent[]; // populated client-side only, right after a fresh upload in this session
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

export interface Category {
  id: number;
  name: string;
  description?: string;
  iconUrl?: string;
}

export interface CreateCourseRequest {
  title: string;
  description: string;
  categoryId: number;
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
  instructorReply?: string;
  repliedAt?: Date;
  reactionCounts?: { [emoji: string]: number };
  myReaction?: string;
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

export interface PublicInstructor {
  id: number;
  fullName: string;
  bio?: string;
  profileImageUrl?: string;
  courseCount: number;
  totalStudents: number;
  averageRating: number;
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
