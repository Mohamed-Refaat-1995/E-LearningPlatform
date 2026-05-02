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
}

export interface LessonContent {
  id: number;
  lessonId: number;
  contentType: string;
  videoUrl?: string;
  videoS3Key?: string;
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
