import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  compress: false,
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
  rewrites: async () => {
    return [
      {
        source: '/api/:path*',
        destination: 'http://localhost:5282/api/:path*',
      },
    ];
  },
};

export default nextConfig;
