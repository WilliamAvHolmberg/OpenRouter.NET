export type DashboardOrder = {
  id: string;
  customer_id: string;
  product_id: string;
  amount: number;
  quantity: number;
  status: 'completed' | 'pending' | 'cancelled';
  created_at: string;
  delivered_at: string | null;
};

export type Customer = {
  id: string;
  name: string;
  segment: 'enterprise' | 'smb' | 'individual';
  country: string;
  lifetime_value: number;
};

export type Product = {
  id: string;
  name: string;
  category: string;
  price: number;
  cost: number;
};

const CUSTOMER_NAMES = [
  'Acme Corp', 'Globex Industries', 'Initech', 'Umbrella Corp', 'Hooli',
  'Stark Industries', 'Wayne Enterprises', 'Cyberdyne Systems', 'Tyrell Corp',
  'Weyland-Yutani', 'Massive Dynamic', 'Oscorp', 'LexCorp', 'InGen',
  'Soylent Corp', 'Nakatomi Trading', 'Sterling Cooper', 'Dunder Mifflin',
  'Prestige Worldwide', 'Bluth Company', 'Los Pollos Hermanos', 'Vehement Capital'
];

const COUNTRIES = [
  'United States', 'United Kingdom', 'Canada', 'Germany', 'France',
  'Australia', 'Japan', 'Singapore', 'Netherlands', 'Sweden'
];

const PRODUCT_NAMES = [
  'Premium Widget', 'Standard Widget', 'Deluxe Widget', 'Enterprise Suite',
  'Professional License', 'Starter Pack', 'Advanced Analytics', 'Basic Plan',
  'Pro Dashboard', 'Team Collaboration', 'Cloud Storage', 'API Access',
  'Security Module', 'Integration Pack', 'Custom Reports', 'Mobile App',
  'Support Package', 'Training Bundle', 'Consulting Hours', 'Managed Service'
];

const CATEGORIES = [
  'Software', 'Hardware', 'Services', 'Subscriptions', 'Training',
  'Support', 'Consulting', 'Integration'
];

export function generateCustomers(count: number): Customer[] {
  const customers: Customer[] = [];
  const segments: Customer['segment'][] = ['enterprise', 'smb', 'individual'];
  
  for (let i = 1; i <= count; i++) {
    const segment = segments[Math.floor(Math.random() * segments.length)];
    const baseValue = segment === 'enterprise' ? 100000 : segment === 'smb' ? 25000 : 5000;
    
    customers.push({
      id: `CUST-${String(i).padStart(4, '0')}`,
      name: CUSTOMER_NAMES[i % CUSTOMER_NAMES.length] + (i > CUSTOMER_NAMES.length ? ` ${Math.floor(i / CUSTOMER_NAMES.length)}` : ''),
      segment,
      country: COUNTRIES[Math.floor(Math.random() * COUNTRIES.length)],
      lifetime_value: Math.round((baseValue + Math.random() * baseValue) * 100) / 100
    });
  }
  
  return customers;
}

export function generateProducts(count: number): Product[] {
  const products: Product[] = [];
  
  for (let i = 1; i <= count; i++) {
    const price = Math.round((50 + Math.random() * 950) * 100) / 100;
    const cost = Math.round(price * (0.3 + Math.random() * 0.3) * 100) / 100;
    
    products.push({
      id: `PROD-${String(i).padStart(4, '0')}`,
      name: PRODUCT_NAMES[i % PRODUCT_NAMES.length] + (i > PRODUCT_NAMES.length ? ` v${Math.floor(i / PRODUCT_NAMES.length)}` : ''),
      category: CATEGORIES[Math.floor(Math.random() * CATEGORIES.length)],
      price,
      cost
    });
  }
  
  return products;
}

export function generateOrders(count: number, customers: Customer[], products: Product[]): DashboardOrder[] {
  const orders: DashboardOrder[] = [];
  const now = Date.now();
  const statuses: DashboardOrder['status'][] = ['completed', 'pending', 'cancelled'];
  
  for (let i = 1; i <= count; i++) {
    const customer = customers[Math.floor(Math.random() * customers.length)];
    const product = products[Math.floor(Math.random() * products.length)];
    
    const daysAgo = Math.floor(Math.random() * 365);
    const createdAt = new Date(now - daysAgo * 24 * 3600 * 1000);
    
    const statusRoll = Math.random();
    const status: DashboardOrder['status'] = 
      statusRoll < 0.7 ? 'completed' : 
      statusRoll < 0.9 ? 'pending' : 
      'cancelled';
    
    const quantity = Math.floor(Math.random() * 5) + 1;
    const amount = Math.round(product.price * quantity * 100) / 100;
    
    const deliveredAt = status === 'completed' 
      ? new Date(createdAt.getTime() + (2 + Math.floor(Math.random() * 7)) * 24 * 3600 * 1000).toISOString()
      : null;
    
    orders.push({
      id: `ORD-${String(i).padStart(5, '0')}`,
      customer_id: customer.id,
      product_id: product.id,
      amount,
      quantity,
      status,
      created_at: createdAt.toISOString(),
      delivered_at: deliveredAt
    });
  }
  
  return orders.sort((a, b) => 
    new Date(b.created_at).getTime() - new Date(a.created_at).getTime()
  );
}

export function generateMockData() {
  const customers = generateCustomers(50);
  const products = generateProducts(30);
  const orders = generateOrders(500, customers, products);
  
  return { customers, products, orders };
}
