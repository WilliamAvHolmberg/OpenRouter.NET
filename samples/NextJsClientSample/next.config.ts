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
};

export default nextConfig;
