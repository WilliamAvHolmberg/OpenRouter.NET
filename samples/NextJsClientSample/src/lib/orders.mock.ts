export type Order = {
  id: string;
  status: 'pending' | 'processing' | 'shipped' | 'delivered' | 'cancelled';
  delivered: boolean;
  amount: number;
  createdAt: string; // ISO
  deliveredAt?: string; // ISO
  customerId: string;
  customerName: string;
  tags: string[];
  notes?: string;
};

const CUSTOMER_NAMES = [
  'Acme Corp', 'Globex', 'Initech', 'Umbrella Corp', 'Hooli', 'Pied Piper',
  'Stark Industries', 'Wayne Enterprises', 'Cyberdyne Systems', 'Tyrell Corp',
  'Weyland-Yutani', 'Massive Dynamic', 'Oscorp', 'LexCorp', 'InGen',
  'Abstergo Industries', 'Buy n Large', 'Soylent Corp', 'Nakatomi Trading',
  'Dunder Mifflin', 'Sterling Cooper', 'Paper Street Soap', 'Central Perk Cafe',
  'Krusty Krab', 'Los Pollos Hermanos', 'Bluth Company', 'Prestige Worldwide',
];

const TAGS = [
  'priority', 'b2b', 'upsell', 'fragile', 'promo', 'express', 'gift',
  'wholesale', 'recurring', 'vip', 'international', 'rush', 'bulk',
];

const NOTES = [
  'Gift wrap requested',
  'Handle with care',
  'Deliver before 5pm',
  'Contact before delivery',
  'Leave at front desk',
  'Signature required',
  'Fragile - glass items',
  'Customer requested expedited shipping',
];

function generateOrders(count: number): Order[] {
  const orders: Order[] = [];
  const now = Date.now();

  for (let i = 1; i <= count; i++) {
    const orderId = `ORD-${1000 + i}`;
    const customerId = `CUST-${Math.floor(Math.random() * 100) + 1}`;
    const customerName = CUSTOMER_NAMES[Math.floor(Math.random() * CUSTOMER_NAMES.length)];

    // Random date within last 60 days
    const daysAgo = Math.floor(Math.random() * 60);
    const createdAt = new Date(now - daysAgo * 24 * 3600 * 1000).toISOString();

    // Random status with realistic distribution
    const statusRoll = Math.random();
    let status: Order['status'];
    let delivered = false;
    let deliveredAt: string | undefined;

    if (statusRoll < 0.35) {
      status = 'delivered';
      delivered = true;
      const deliveryDelay = Math.floor(Math.random() * 5) + 2; // 2-7 days
      deliveredAt = new Date(now - (daysAgo - deliveryDelay) * 24 * 3600 * 1000).toISOString();
    } else if (statusRoll < 0.55) {
      status = 'shipped';
      delivered = false;
    } else if (statusRoll < 0.70) {
      status = 'processing';
      delivered = false;
    } else if (statusRoll < 0.85) {
      status = 'pending';
      delivered = false;
    } else {
      status = 'cancelled';
      delivered = false;
    }

    // Random amount between $10 and $500
    const amount = Math.round((Math.random() * 490 + 10) * 100) / 100;

    // Random tags (0-3 tags)
    const numTags = Math.floor(Math.random() * 4);
    const tags: string[] = [];
    for (let t = 0; t < numTags; t++) {
      const tag = TAGS[Math.floor(Math.random() * TAGS.length)];
      if (!tags.includes(tag)) {
        tags.push(tag);
      }
    }

    // Random notes (30% chance)
    const notes = Math.random() < 0.3
      ? NOTES[Math.floor(Math.random() * NOTES.length)]
      : undefined;

    orders.push({
      id: orderId,
      status,
      delivered,
      amount,
      createdAt,
      deliveredAt,
      customerId,
      customerName,
      tags,
      notes,
    });
  }

  // Sort by creation date, newest first
  return orders.sort((a, b) =>
    new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
  );
}

export const ORDERS: Order[] = generateOrders(200);


