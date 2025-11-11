'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

export function Navigation() {
  const pathname = usePathname();

  const links: Array<{ href: string; label: string; badge?: string }> = [
    { href: '/', label: 'Home' },
    { href: '/chat', label: 'Full Chat' },
    { href: '/stateless-chat', label: 'Stateless Chat', badge: 'NEW' },
    { href: '/orders', label: 'Orders' },
    { href: '/dashboard', label: 'Dashboard' },
    { href: '/generate-object', label: 'Generate Object' },
    { href: '/translations', label: 'Translations' }
  ];

  return (
    <nav className="bg-white/80 backdrop-blur-lg border-b border-gray-200 sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-6">
        <div className="flex items-center justify-between h-16">
          <div className="flex items-center gap-3">
            <div className="text-2xl">🚀</div>
            <span className="font-semibold text-gray-900 text-lg">OpenRouter.NET</span>
          </div>

          <div className="flex gap-2">
            {links.map((link) => {
              const isActive = pathname === link.href;
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  className={`px-4 py-2 rounded-lg font-medium transition-all duration-200 relative ${
                    isActive
                      ? 'bg-blue-500 text-white shadow-sm'
                      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
                  }`}
                >
                  {link.label}
                  {link.badge && (
                    <span className="absolute -top-1 -right-1 px-1.5 py-0.5 text-[10px] font-bold bg-purple-500 text-white rounded-full">
                      {link.badge}
                    </span>
                  )}
                </Link>
              );
            })}
          </div>
        </div>
      </div>
    </nav>
  );
}
