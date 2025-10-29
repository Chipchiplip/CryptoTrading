import { TrendingUp, Shield, Zap, Globe, ArrowRight, CheckCircle2 } from 'lucide-react';
import { Button } from '../../ui/button';
import { Card } from '../../ui/card';

interface HomeProps {
  onNavigate?: (page: string) => void;
}

export default function Home({ onNavigate }: HomeProps) {
  const features = [
    {
      icon: Shield,
      title: 'Secure Trading',
      description: 'Bank-level security with multi-factor authentication and cold storage'
    },
    {
      icon: Zap,
      title: 'Lightning Fast',
      description: 'Execute trades in milliseconds with our high-performance engine'
    },
    {
      icon: Globe,
      title: 'Global Access',
      description: 'Trade 24/7 from anywhere in the world with our mobile app'
    },
    {
      icon: TrendingUp,
      title: 'Advanced Tools',
      description: 'Professional trading tools and real-time market analytics'
    }
  ];

  const cryptoStats = [
    { label: 'Total Users', value: '2.5M+', change: '+12%' },
    { label: '24h Volume', value: '$4.2B', change: '+8%' },
    { label: 'Markets', value: '350+', change: '+5%' },
    { label: 'Countries', value: '180+', change: 'Stable' }
  ];

  const popularCoins = [
    { symbol: 'BTC', name: 'Bitcoin', price: '$50,234', change: '+2.34%', positive: true },
    { symbol: 'ETH', name: 'Ethereum', price: '$2,845', change: '+1.82%', positive: true },
    { symbol: 'SOL', name: 'Solana', price: '$98.45', change: '-0.45%', positive: false },
    { symbol: 'BNB', name: 'BNB', price: '$312.89', change: '+3.12%', positive: true }
  ];

  return (
    <div className="bg-black text-white">
      {/* Hero Section */}
      <section className="relative overflow-hidden">
        <div className="absolute inset-0 bg-gradient-to-br from-emerald-500/10 via-transparent to-transparent"></div>
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20 md:py-32 relative">
          <div className="grid md:grid-cols-2 gap-12 items-center">
            <div>
              <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-emerald-500/10 text-emerald-500 text-sm mb-6">
                <span className="w-2 h-2 bg-emerald-500 rounded-full animate-pulse"></span>
                Trusted by millions worldwide
              </div>
              <h1 className="text-5xl md:text-6xl mb-6">
                Trade Crypto with
                <span className="text-emerald-500"> Confidence</span>
              </h1>
              <p className="text-xl text-gray-400 mb-8">
                Join the world's leading cryptocurrency exchange. Buy, sell, and trade Bitcoin, Ethereum, and 350+ altcoins with ease.
              </p>
              <div className="flex flex-col sm:flex-row gap-4">
                <Button
                  size="lg"
                  className="bg-emerald-500 text-black hover:bg-emerald-600"
                  onClick={() => onNavigate?.('register')}
                >
                  Get Started Free
                  <ArrowRight className="ml-2 w-5 h-5" />
                </Button>
                <Button
                  size="lg"
                  variant="outline"
                  className="border-gray-700 hover:bg-gray-900"
                  onClick={() => onNavigate?.('markets')}
                >
                  Explore Markets
                </Button>
                <Button
                  size="lg"
                  variant="outline"
                  className="border-emerald-500 text-emerald-500 hover:bg-emerald-500/10"
                  onClick={() => onNavigate?.('trader-dashboard')}
                >
                  Demo Trader
                </Button>
              </div>
            </div>
            <div className="relative">
              <Card className="bg-gray-900 border-gray-800 p-6">
                <div className="space-y-4">
                  <div className="flex items-center justify-between mb-6">
                    <h3>Trending Coins</h3>
                    <span className="text-emerald-500 text-sm">Live</span>
                  </div>
                  {popularCoins.map((coin) => (
                    <div key={coin.symbol} className="flex items-center justify-between p-3 rounded-lg hover:bg-gray-800 transition-colors">
                      <div className="flex items-center gap-3">
                        <div className="w-10 h-10 bg-emerald-500/10 rounded-full flex items-center justify-center">
                          <span className="text-emerald-500">{coin.symbol}</span>
                        </div>
                        <div>
                          <div className="text-white">{coin.name}</div>
                          <div className="text-sm text-gray-400">{coin.symbol}</div>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-white">{coin.price}</div>
                        <div className={coin.positive ? 'text-emerald-500 text-sm' : 'text-red-500 text-sm'}>
                          {coin.change}
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </Card>
            </div>
          </div>
        </div>
      </section>

      {/* Stats Section */}
      <section className="bg-gray-900/50 py-12 border-y border-gray-800">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-8">
            {cryptoStats.map((stat, index) => (
              <div key={index} className="text-center">
                <div className="text-3xl md:text-4xl text-emerald-500 mb-2">{stat.value}</div>
                <div className="text-gray-400 mb-1">{stat.label}</div>
                <div className="text-sm text-gray-500">{stat.change}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="text-center mb-16">
            <h2 className="text-4xl mb-4">Why Choose CryptoTrade?</h2>
            <p className="text-xl text-gray-400 max-w-2xl mx-auto">
              Everything you need to trade cryptocurrencies safely and efficiently
            </p>
          </div>
          <div className="grid md:grid-cols-2 lg:grid-cols-4 gap-8">
            {features.map((feature, index) => (
              <Card key={index} className="bg-gray-900 border-gray-800 p-6 hover:border-emerald-500/50 transition-colors">
                <div className="w-12 h-12 bg-emerald-500/10 rounded-lg flex items-center justify-center mb-4">
                  <feature.icon className="w-6 h-6 text-emerald-500" />
                </div>
                <h3 className="mb-2">{feature.title}</h3>
                <p className="text-gray-400 text-sm">{feature.description}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      {/* Benefits Section */}
      <section className="py-20 bg-gray-900/50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="grid md:grid-cols-2 gap-12 items-center">
            <div>
              <h2 className="text-4xl mb-6">Start Trading in Minutes</h2>
              <div className="space-y-4">
                {[
                  'Create your free account in 30 seconds',
                  'Complete quick verification process',
                  'Deposit funds via bank transfer or card',
                  'Start trading with as little as $10'
                ].map((item, index) => (
                  <div key={index} className="flex items-start gap-3">
                    <CheckCircle2 className="w-6 h-6 text-emerald-500 flex-shrink-0 mt-0.5" />
                    <span className="text-gray-300">{item}</span>
                  </div>
                ))}
              </div>
              <Button
                size="lg"
                className="bg-emerald-500 text-black hover:bg-emerald-600 mt-8"
                onClick={() => onNavigate?.('register')}
              >
                Create Free Account
              </Button>
            </div>
            <Card className="bg-gray-900 border-gray-800 p-8">
              <div className="space-y-6">
                <div>
                  <div className="text-gray-400 mb-2">Total Balance</div>
                  <div className="text-4xl text-white">$12,458.32</div>
                  <div className="text-emerald-500 text-sm mt-1">+$1,234 (11.2%) this month</div>
                </div>
                <div className="grid grid-cols-2 gap-4 pt-6 border-t border-gray-800">
                  <div>
                    <div className="text-gray-400 text-sm mb-1">24h Profit</div>
                    <div className="text-emerald-500">+$342.89</div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-sm mb-1">Win Rate</div>
                    <div className="text-white">68.4%</div>
                  </div>
                </div>
                <div className="pt-4">
                  <div className="h-32 bg-gradient-to-t from-emerald-500/20 to-transparent rounded-lg"></div>
                </div>
              </div>
            </Card>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-20">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-4xl md:text-5xl mb-6">
            Ready to Start Your Crypto Journey?
          </h2>
          <p className="text-xl text-gray-400 mb-8">
            Join millions of users trading on CryptoTrade today
          </p>
          <div className="flex flex-col sm:flex-row gap-4 justify-center">
            <Button
              size="lg"
              className="bg-emerald-500 text-black hover:bg-emerald-600"
              onClick={() => onNavigate?.('register')}
            >
              Sign Up Now
              <ArrowRight className="ml-2 w-5 h-5" />
            </Button>
            <Button
              size="lg"
              variant="outline"
              className="border-gray-700 hover:bg-gray-900"
              onClick={() => onNavigate?.('markets')}
            >
              View Markets
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}
