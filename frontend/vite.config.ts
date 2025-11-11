
  import { defineConfig } from 'vite';
  import react from '@vitejs/plugin-react-swc';
  import path from 'path';

  export default defineConfig({
    plugins: [react()],
    resolve: {
      extensions: ['.js', '.jsx', '.ts', '.tsx', '.json'],
      alias: {
        'vaul@1.1.2': 'vaul',
        'sonner@2.0.3': 'sonner',
        'recharts@2.15.2': 'recharts',
        'react-resizable-panels@2.1.7': 'react-resizable-panels',
        'react-hook-form@7.55.0': 'react-hook-form',
        'react-day-picker@8.10.1': 'react-day-picker',
        'next-themes@0.4.6': 'next-themes',
        'lucide-react@0.487.0': 'lucide-react',
        'input-otp@1.4.2': 'input-otp',
        'embla-carousel-react@8.6.0': 'embla-carousel-react',
        'cmdk@1.1.1': 'cmdk',
        'class-variance-authority@0.7.1': 'class-variance-authority',
        '@radix-ui/react-tooltip@1.1.8': '@radix-ui/react-tooltip',
        '@radix-ui/react-toggle@1.1.2': '@radix-ui/react-toggle',
        '@radix-ui/react-toggle-group@1.1.2': '@radix-ui/react-toggle-group',
        '@radix-ui/react-tabs@1.1.3': '@radix-ui/react-tabs',
        '@radix-ui/react-switch@1.1.3': '@radix-ui/react-switch',
        '@radix-ui/react-slot@1.1.2': '@radix-ui/react-slot',
        '@radix-ui/react-slider@1.2.3': '@radix-ui/react-slider',
        '@radix-ui/react-separator@1.1.2': '@radix-ui/react-separator',
        '@radix-ui/react-select@2.1.6': '@radix-ui/react-select',
        '@radix-ui/react-scroll-area@1.2.3': '@radix-ui/react-scroll-area',
        '@radix-ui/react-radio-group@1.2.3': '@radix-ui/react-radio-group',
        '@radix-ui/react-progress@1.1.2': '@radix-ui/react-progress',
        '@radix-ui/react-popover@1.1.6': '@radix-ui/react-popover',
        '@radix-ui/react-navigation-menu@1.2.5': '@radix-ui/react-navigation-menu',
        '@radix-ui/react-menubar@1.1.6': '@radix-ui/react-menubar',
        '@radix-ui/react-label@2.1.2': '@radix-ui/react-label',
        '@radix-ui/react-hover-card@1.1.6': '@radix-ui/react-hover-card',
        '@radix-ui/react-dropdown-menu@2.1.6': '@radix-ui/react-dropdown-menu',
        '@radix-ui/react-dialog@1.1.6': '@radix-ui/react-dialog',
        '@radix-ui/react-context-menu@2.2.6': '@radix-ui/react-context-menu',
        '@radix-ui/react-collapsible@1.1.3': '@radix-ui/react-collapsible',
        '@radix-ui/react-checkbox@1.1.4': '@radix-ui/react-checkbox',
        '@radix-ui/react-avatar@1.1.3': '@radix-ui/react-avatar',
        '@radix-ui/react-aspect-ratio@1.1.2': '@radix-ui/react-aspect-ratio',
        '@radix-ui/react-alert-dialog@1.1.6': '@radix-ui/react-alert-dialog',
        '@radix-ui/react-accordion@1.2.3': '@radix-ui/react-accordion',
        '@': path.resolve(__dirname, './src'),
      },
    },
    build: {
      target: 'esnext',
      outDir: 'build',
    },
  server: {
    port: 3000,
    host: true, // Allow external access
    open: true,
    strictPort: false, // Try next available port if 3000 is busy
    proxy: {
      '/api': {
        target: 'http://localhost:5299',
        changeOrigin: true,
        secure: false,
        timeout: 10000, // 10 second timeout
        configure: (proxy, _options) => {
          proxy.on('error', (err: any, req, res) => {
            console.error('❌ Proxy Error:', {
              url: req?.url,
              method: req?.method,
              error: err?.message || err,
              code: err?.code,
              target: 'http://localhost:5299'
            });
          });
          proxy.on('proxyReq', (proxyReq, req, res) => {
            console.log('🔄 Proxying:', req.method, req.url, '→', 'http://localhost:5299' + req.url);
          });
          proxy.on('proxyRes', (proxyRes, req, res) => {
            console.log('✅ Proxy Response:', req.method, req.url, '→', proxyRes.statusCode);
          });
        },
      },
      '/marketHub': {
        target: process.env.VITE_API_TARGET || 'http://localhost:5299',
        ws: true,
        changeOrigin: true,
        secure: false,
        timeout: 5000,
        configure: (proxy, _options) => {
          proxy.on('error', (err: any, _req, _res) => {
            // Silently ignore WebSocket proxy errors - SignalR will handle reconnection
            if (err?.code !== 'ECONNRESET' && err?.code !== 'ECONNREFUSED') {
              console.warn('WebSocket proxy error:', err?.message || err);
            }
          });
          proxy.on('proxyReqWs', (proxyReq, req, socket) => {
            // Handle WebSocket upgrade errors silently
            socket.on('error', (err: any) => {
              if (err?.code !== 'ECONNRESET' && err?.code !== 'ECONNREFUSED') {
                console.warn('WebSocket socket error:', err?.message || err);
              }
            });
          });
        },
      },
      // TradingHub SignalR for order/trade events
      '/hubs/trading': {
        target: process.env.VITE_API_TARGET || 'http://localhost:5299',
        ws: true,
        changeOrigin: true,
        secure: false,
        configure: (proxy, _options) => {
          proxy.on('error', (err: any, _req, _res) => {
            // Silently ignore WebSocket proxy errors - SignalR will handle reconnection
            if (err?.code !== 'ECONNRESET' && err?.code !== 'ECONNREFUSED') {
              console.warn('WebSocket proxy error:', err?.message || err);
            }
          });
          proxy.on('proxyReqWs', (proxyReq, req, socket) => {
            // Handle WebSocket upgrade errors silently
            socket.on('error', (err: any) => {
              if (err?.code !== 'ECONNRESET' && err?.code !== 'ECONNREFUSED') {
                console.warn('WebSocket socket error:', err?.message || err);
              }
            });
          });
        },
      },
    },
  },
  });