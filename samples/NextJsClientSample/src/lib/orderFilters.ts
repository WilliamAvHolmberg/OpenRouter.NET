import type { Order } from './orders.mock';

export type OrderFilters = {
  status?: string[];
  delivered?: boolean;
  customerIds?: string[];
  minAmount?: number;
  maxAmount?: number;
  createdFrom?: string;
  createdTo?: string;
  deliveredFrom?: string;
  deliveredTo?: string;
  text?: string;
  tags?: string[];
};

export function applyOrderFilters(orders: Order[], filters: OrderFilters): Order[] {
  return orders.filter((o) => matches(o, filters));
}

function matches(o: Order, f: OrderFilters): boolean {
  if (f.status && f.status.length > 0 && !f.status.includes(o.status)) return false;
  if (typeof f.delivered === 'boolean' && o.delivered !== f.delivered) return false;
  if (f.customerIds && f.customerIds.length > 0 && !f.customerIds.includes(o.customerId)) return false;
  if (typeof f.minAmount === 'number' && o.amount < f.minAmount) return false;
  if (typeof f.maxAmount === 'number' && o.amount > f.maxAmount) return false;

  if (f.createdFrom && new Date(o.createdAt) < new Date(f.createdFrom)) return false;
  if (f.createdTo && new Date(o.createdAt) > new Date(f.createdTo)) return false;

  if (f.deliveredFrom && (!o.deliveredAt || new Date(o.deliveredAt) < new Date(f.deliveredFrom))) return false;
  if (f.deliveredTo && (!o.deliveredAt || new Date(o.deliveredAt) > new Date(f.deliveredTo))) return false;

  if (f.text) {
    const t = f.text.toLowerCase();
    const hay = `${o.id} ${o.customerName} ${o.notes ?? ''}`.toLowerCase();
    if (!hay.includes(t)) return false;
  }

  if (f.tags && f.tags.length > 0) {
    const set = new Set(o.tags);
    const ok = f.tags.every((t) => set.has(t));
    if (!ok) return false;
  }

  return true;
}


