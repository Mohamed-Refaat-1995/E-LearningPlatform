import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CartItem } from '@shared/models/cart.model';
import { ToastService } from './toast.service';

const CART_STORAGE_KEY = 'cart_items';

@Injectable({ providedIn: 'root' })
export class CartService {
  private itemsSubject = new BehaviorSubject<CartItem[]>(this.loadFromStorage());
  items$: Observable<CartItem[]> = this.itemsSubject.asObservable();
  count$: Observable<number> = this.items$.pipe(map(items => items.length));

  constructor(private toast: ToastService) {}

  private loadFromStorage(): CartItem[] {
    try {
      const raw = localStorage.getItem(CART_STORAGE_KEY);
      return raw ? JSON.parse(raw) : [];
    } catch {
      return [];
    }
  }

  private persist(items: CartItem[]): void {
    localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
    this.itemsSubject.next(items);
  }

  getItems(): CartItem[] {
    return this.itemsSubject.value;
  }

  addToCart(item: CartItem): void {
    const items = this.getItems();
    if (items.some(i => i.courseId === item.courseId)) {
      this.toast.info('This course is already in your cart.');
      return;
    }
    this.persist([...items, item]);
    this.toast.success('Added to cart.');
  }

  removeFromCart(courseId: number): void {
    this.persist(this.getItems().filter(i => i.courseId !== courseId));
  }

  updateItem(courseId: number, patch: Partial<CartItem>): void {
    this.persist(this.getItems().map(i => i.courseId === courseId ? { ...i, ...patch } : i));
  }

  clear(): void {
    this.persist([]);
  }

  isInCart(courseId: number): boolean {
    return this.getItems().some(i => i.courseId === courseId);
  }
}
