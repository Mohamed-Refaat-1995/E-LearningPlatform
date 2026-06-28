export interface CourseCompletionStatus {
  completionPercentage: number;
  allLessonsCompleted: boolean;
  allQuizzesPassed: boolean;
  isCourseComplete: boolean;
  isCertificateEligible: boolean;
  hasCertificate: boolean;
  certificateId?: number;
}

export interface CertificateVerification {
  isValid: boolean;
  studentName?: string;
  courseTitle?: string;
  certificateNumber?: string;
  issuedAt?: Date;
  verificationCode?: string;
  message?: string;
}
