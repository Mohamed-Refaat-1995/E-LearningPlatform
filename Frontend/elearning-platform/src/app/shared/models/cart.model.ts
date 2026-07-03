export interface CartItem {
  courseId: number;
  title: string;
  thumbnailUrl?: string;
  price: number;
  instructorName?: string;
  discountedPrice?: number;
  couponCode?: string;
}
