'use client';

import { useState, useMemo } from 'react';
import type { Order } from '../../lib/orders.mock';

const ITEMS_PER_PAGE = 10;

const getStatusColors = (status: Order['status']) => {
  switch (status) {
    case 'delivered':
      return 'bg-emerald-100 text-emerald-700 border-emerald-300 shadow-emerald-100/50';
    case 'shipped':
      return 'bg-blue-100 text-blue-700 border-blue-300 shadow-blue-100/50';
    case 'processing':
      return 'bg-amber-100 text-amber-700 border-amber-300 shadow-amber-100/50';
    case 'pending':
      return 'bg-purple-100 text-purple-700 border-purple-300 shadow-purple-100/50';
    case 'cancelled':
      return 'bg-red-100 text-red-700 border-red-300 shadow-red-100/50';
    default:
      return 'bg-slate-100 text-slate-700 border-slate-300 shadow-slate-100/50';
  }
};

export function OrdersTable({ orders, filterAnimation }: { orders: Order[]; filterAnimation: boolean }) {
  const [currentPage, setCurrentPage] = useState(1);

  const totalPages = Math.ceil(orders.length / ITEMS_PER_PAGE);

  const paginatedOrders = useMemo(() => {
    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
    return orders.slice(startIndex, startIndex + ITEMS_PER_PAGE);
  }, [orders, currentPage]);

  const startItem = orders.length === 0 ? 0 : (currentPage - 1) * ITEMS_PER_PAGE + 1;
  const endItem = Math.min(currentPage * ITEMS_PER_PAGE, orders.length);

  // Reset to page 1 when orders change
  useMemo(() => {
    setCurrentPage(1);
  }, [orders]);

  const handlePrevious = () => {
    setCurrentPage((prev) => Math.max(1, prev - 1));
  };

  const handleNext = () => {
    setCurrentPage((prev) => Math.min(totalPages, prev + 1));
  };

  return (
    <div className="flex flex-col h-full">
      <div className="flex-1 overflow-hidden">
        <div className="rounded-lg overflow-hidden border border-slate-200 bg-white">
          <table className="min-w-full text-sm">
            <thead className="bg-slate-50 text-slate-700 text-xs sticky top-0 border-b border-slate-200">
              <tr>
                <th className="px-4 py-3 text-left font-semibold">Order</th>
                <th className="px-4 py-3 text-left font-semibold">Customer</th>
                <th className="px-4 py-3 text-left font-semibold">Status</th>
                <th className="px-4 py-3 text-right font-semibold">Amount</th>
                <th className="px-4 py-3 text-left font-semibold">Created</th>
                <th className="px-4 py-3 text-left font-semibold">Delivered</th>
                <th className="px-4 py-3 text-left font-semibold">Tags</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {paginatedOrders.map((o, idx) => (
                <tr
                  key={o.id}
                  className={`
                    group hover:bg-slate-50
                    transition-colors duration-150
                    ${filterAnimation ? 'animate-fade-in-up' : ''}
                  `}
                  style={{
                    animationDelay: filterAnimation ? `${idx * 50}ms` : '0ms'
                  }}
                >
                  <td className="px-4 py-3 font-medium text-slate-900">
                    {o.id}
                  </td>
                  <td className="px-4 py-3 text-slate-700">{o.customerName}</td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center rounded-md border px-2.5 py-0.5 text-xs font-medium capitalize ${getStatusColors(o.status)}`}>
                      {o.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right font-medium text-slate-900">
                    ${o.amount.toFixed(2)}
                  </td>
                  <td className="px-4 py-3 text-slate-600 text-xs">{new Date(o.createdAt).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-slate-600 text-xs">
                    {o.deliveredAt ? new Date(o.deliveredAt).toLocaleDateString() : '-'}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1 flex-wrap">
                      {o.tags.map((t) => (
                        <span key={t} className="inline-flex items-center rounded-md bg-slate-100 px-2 py-0.5 text-xs text-slate-600 border border-slate-200">
                          {t}
                        </span>
                      ))}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Pagination Controls - Fixed at bottom */}
      {orders.length > 0 && (
        <div className="flex-shrink-0 mt-4 flex items-center justify-between border-t border-slate-200 bg-white px-4 py-3 rounded-lg">
          <div className="flex flex-1 justify-between sm:hidden">
            <button
              onClick={handlePrevious}
              disabled={currentPage === 1}
              className="relative inline-flex items-center rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Previous
            </button>
            <button
              onClick={handleNext}
              disabled={currentPage === totalPages}
              className="relative ml-3 inline-flex items-center rounded-md border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
            </button>
          </div>
          <div className="hidden sm:flex sm:flex-1 sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-slate-700">
                Showing <span className="font-medium">{startItem}</span> to{' '}
                <span className="font-medium">{endItem}</span> of{' '}
                <span className="font-medium">{orders.length}</span> results
              </p>
            </div>
            <div>
              <nav className="isolate inline-flex -space-x-px rounded-md shadow-sm" aria-label="Pagination">
                <button
                  onClick={handlePrevious}
                  disabled={currentPage === 1}
                  className="relative inline-flex items-center rounded-l-md px-2 py-2 text-slate-400 ring-1 ring-inset ring-slate-300 hover:bg-slate-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <span className="sr-only">Previous</span>
                  <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fillRule="evenodd" d="M12.79 5.23a.75.75 0 01-.02 1.06L8.832 10l3.938 3.71a.75.75 0 11-1.04 1.08l-4.5-4.25a.75.75 0 010-1.08l4.5-4.25a.75.75 0 011.06.02z" clipRule="evenodd" />
                  </svg>
                </button>

                <span className="relative inline-flex items-center px-4 py-2 text-sm font-semibold text-slate-900 ring-1 ring-inset ring-slate-300">
                  {currentPage} / {totalPages}
                </span>

                <button
                  onClick={handleNext}
                  disabled={currentPage === totalPages}
                  className="relative inline-flex items-center rounded-r-md px-2 py-2 text-slate-400 ring-1 ring-inset ring-slate-300 hover:bg-slate-50 focus:z-20 focus:outline-offset-0 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <span className="sr-only">Next</span>
                  <svg className="h-5 w-5" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                    <path fillRule="evenodd" d="M7.21 14.77a.75.75 0 01.02-1.06L11.168 10 7.23 6.29a.75.75 0 111.04-1.08l4.5 4.25a.75.75 0 010 1.08l-4.5 4.25a.75.75 0 01-1.06-.02z" clipRule="evenodd" />
                  </svg>
                </button>
              </nav>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}


