import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  compress: false,
  rewrites: async () => {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5282/api/:path*',
      },
    ];
  },
  turbopack: {
    resolveAlias: {
      'fs': './empty-module.ts',
      'path': './empty-module.ts',
      'crypto': './empty-module.ts',
    },
  },
  webpack: (config, { isServer }) => {
    if (!isServer) {
      config.resolve.fallback = {
        ...config.resolve.fallback,
        fs: false,
        path: false,
        crypto: false,
      };
    }
    return config;
  },
};

export default nextConfig;
