'use client';

import { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import initSqlJs from 'sql.js';
import { generateMockData } from './mockData';

type SQLiteContextType = {
  db: any | null;
  isReady: boolean;
  error: string | null;
};

const SQLiteContext = createContext<SQLiteContextType>({
  db: null,
  isReady: false,
  error: null
});

export function SQLiteProvider({ children }: { children: ReactNode }) {
  const [db, setDb] = useState<any | null>(null);
  const [isReady, setIsReady] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;

    async function initDatabase() {
      try {
        const SQL = await initSqlJs({
          locateFile: (file) => `https://sql.js.org/dist/${file}`
        });

        if (!mounted) return;

        const database = new SQL.Database();

        database.run(`
          CREATE TABLE customers (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            segment TEXT NOT NULL,
            country TEXT NOT NULL,
            lifetime_value REAL NOT NULL
          );
        `);

        database.run(`
          CREATE TABLE products (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            category TEXT NOT NULL,
            price REAL NOT NULL,
            cost REAL NOT NULL
          );
        `);

        database.run(`
          CREATE TABLE orders (
            id TEXT PRIMARY KEY,
            customer_id TEXT NOT NULL,
            product_id TEXT NOT NULL,
            amount REAL NOT NULL,
            quantity INTEGER NOT NULL,
            status TEXT NOT NULL,
            created_at TEXT NOT NULL,
            delivered_at TEXT,
            FOREIGN KEY (customer_id) REFERENCES customers(id),
            FOREIGN KEY (product_id) REFERENCES products(id)
          );
        `);

        const { customers, products, orders } = generateMockData();

        const customerStmt = database.prepare(
          'INSERT INTO customers (id, name, segment, country, lifetime_value) VALUES (?, ?, ?, ?, ?)'
        );
        customers.forEach(c => {
          customerStmt.run([c.id, c.name, c.segment, c.country, c.lifetime_value]);
        });
        customerStmt.free();

        const productStmt = database.prepare(
          'INSERT INTO products (id, name, category, price, cost) VALUES (?, ?, ?, ?, ?)'
        );
        products.forEach(p => {
          productStmt.run([p.id, p.name, p.category, p.price, p.cost]);
        });
        productStmt.free();

        const orderStmt = database.prepare(
          'INSERT INTO orders (id, customer_id, product_id, amount, quantity, status, created_at, delivered_at) VALUES (?, ?, ?, ?, ?, ?, ?, ?)'
        );
        orders.forEach(o => {
          orderStmt.run([o.id, o.customer_id, o.product_id, o.amount, o.quantity, o.status, o.created_at, o.delivered_at]);
        });
        orderStmt.free();

        if (!mounted) return;

        setDb(database);
        setIsReady(true);
      } catch (err) {
        if (!mounted) return;
        setError(String(err));
        console.error('Failed to initialize SQLite:', err);
      }
    }

    initDatabase();

    return () => {
      mounted = false;
      if (db) {
        db.close();
      }
    };
  }, []);

  return (
    <SQLiteContext.Provider value={{ db, isReady, error }}>
      {children}
    </SQLiteContext.Provider>
  );
}

export function useDatabase(): any | null {
  const { db } = useContext(SQLiteContext);
  return db;
}

export function useDatabaseReady() {
  const { isReady, error } = useContext(SQLiteContext);
  return { isReady, error };
}
