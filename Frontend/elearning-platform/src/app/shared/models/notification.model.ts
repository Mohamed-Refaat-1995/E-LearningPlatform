export interface Notification {
  id: number;
  title: string;
  message: string;
  type: 'CourseUpdate' | 'FreeCourse' | string;
  courseId?: number;
  courseTitle?: string;
  isRead: boolean;
  createdAt: Date;
}
