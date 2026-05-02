export interface Payment {
  id: number;
  userId: number;
  courseId: number;
  courseName?: string;
  amount: number;
  currency: string;
  paymentMethod: string;
  stripePaymentIntentId: string;
  status: string;
  paidAt: Date;
  invoice?: Invoice;
}

export interface Invoice {
  id: number;
  invoiceNumber: string;
  paymentId: number;
  amount: number;
  currency: string;
  status: string;
  issuedAt: Date;
  pdfUrl?: string;
}

export interface PaymentRequest {
  courseId: number;
  amount: number;
  stripePaymentMethodId?: string;
}

export interface Certificate {
  id: number;
  studentId: number;
  courseId: number;
  courseName?: string;
  certificateNumber: string;
  issuedAt: Date;
  pdfUrl?: string;
  verificationCode?: string;
}
