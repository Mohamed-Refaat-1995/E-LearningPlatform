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

// Step 1 — create order
export interface CreateOrderRequest {
  courseIds: number[];
}

export interface Order {
  id: number;
  userId: number;
  status: string;
  totalAmount: number;
  currency: string;
  placedAt: Date;
  items: OrderItem[];
}

export interface OrderItem {
  id: number;
  orderId: number;
  courseId: number;
  price: number;
}

// Step 2 — create payment against an order
export interface PaymentRequest {
  orderId: number;
  amount: number;
  paymentMethod: string;
}

// Response to creating a payment: the Stripe client secret is only present when
// the order total is greater than zero (free courses skip Stripe entirely).
export interface PaymentWithClientSecret {
  payment: Payment;
  clientSecret: string | null;
}

// Step 3 — process / confirm payment (this triggers enrollment on backend)
export interface ProcessPaymentRequest {
  stripePaymentIntentId?: string;
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
