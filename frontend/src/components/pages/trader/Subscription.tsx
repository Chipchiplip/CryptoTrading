import { Check, Crown, Zap, Star } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';

export default function Subscription() {
  const plans = [
    {
      name: 'Free',
      price: 0,
      period: 'month',
      icon: Star,
      features: [
        'Basic trading features',
        '10 trades per day',
        'Email support',
        'Standard trading fees (0.2%)',
      ],
      current: true,
    },
    {
      name: 'Pro',
      price: 29,
      period: 'month',
      icon: Zap,
      features: [
        'All Free features',
        'Unlimited trades',
        'Priority support',
        'Reduced fees (0.1%)',
        'Advanced charts',
        'API access',
      ],
      popular: true,
    },
    {
      name: 'Premium',
      price: 99,
      period: 'month',
      icon: Crown,
      features: [
        'All Pro features',
        '24/7 dedicated support',
        'Lowest fees (0.05%)',
        'Advanced analytics',
        'Custom trading bots',
        'Priority withdrawals',
        'Personal account manager',
      ],
    },
  ];

  const billingHistory = [
    { id: 'INV-001', date: '2025-01-01', plan: 'Free', amount: 0, status: 'Active' },
    { id: 'INV-002', date: '2024-12-01', plan: 'Free', amount: 0, status: 'Completed' },
  ];

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-8 text-center">
        <h1 className="text-4xl mb-2">Choose Your Plan</h1>
        <p className="text-gray-400">Upgrade to unlock more features and better rates</p>
      </div>

      {/* Plans */}
      <div className="grid md:grid-cols-3 gap-6 mb-12">
        {plans.map((plan) => (
          <Card
            key={plan.name}
            className={`bg-gray-900 border-gray-800 p-8 relative ${
              plan.popular ? 'border-emerald-500' : ''
            }`}
          >
            {plan.popular && (
              <Badge className="absolute -top-3 left-1/2 transform -translate-x-1/2 bg-emerald-500 text-black">
                Most Popular
              </Badge>
            )}
            {plan.current && (
              <Badge className="absolute -top-3 left-1/2 transform -translate-x-1/2 bg-blue-500 text-white">
                Current Plan
              </Badge>
            )}

            <div className="text-center mb-6">
              <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto mb-4">
                <plan.icon className="w-8 h-8 text-emerald-500" />
              </div>
              <h3 className="text-2xl mb-2">{plan.name}</h3>
              <div className="text-4xl text-white mb-1">
                ${plan.price}
                <span className="text-xl text-gray-400">/{plan.period}</span>
              </div>
            </div>

            <ul className="space-y-3 mb-8">
              {plan.features.map((feature, index) => (
                <li key={index} className="flex items-start gap-2">
                  <Check className="w-5 h-5 text-emerald-500 flex-shrink-0 mt-0.5" />
                  <span className="text-gray-300">{feature}</span>
                </li>
              ))}
            </ul>

            <Button
              className={`w-full ${
                plan.current
                  ? 'bg-gray-700 text-gray-300 cursor-not-allowed'
                  : 'bg-emerald-500 text-black hover:bg-emerald-600'
              }`}
              disabled={plan.current}
            >
              {plan.current ? 'Current Plan' : 'Upgrade Now'}
            </Button>
          </Card>
        ))}
      </div>

      {/* Billing History */}
      <Card className="bg-gray-900 border-gray-800 p-6">
        <h2 className="text-xl mb-6">Billing History</h2>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="text-left text-gray-400 text-sm border-b border-gray-800">
                <th className="pb-3">Invoice ID</th>
                <th className="pb-3">Date</th>
                <th className="pb-3">Plan</th>
                <th className="pb-3 text-right">Amount</th>
                <th className="pb-3">Status</th>
                <th className="pb-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody>
              {billingHistory.map((invoice) => (
                <tr key={invoice.id} className="border-b border-gray-800">
                  <td className="py-4 text-emerald-500">{invoice.id}</td>
                  <td className="py-4 text-gray-300">{invoice.date}</td>
                  <td className="py-4 text-white">{invoice.plan}</td>
                  <td className="py-4 text-right text-white">${invoice.amount.toFixed(2)}</td>
                  <td className="py-4">
                    <Badge className={invoice.status === 'Active' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-gray-500/10 text-gray-500'}>
                      {invoice.status}
                    </Badge>
                  </td>
                  <td className="py-4 text-right">
                    <Button size="sm" variant="ghost" className="text-emerald-500">
                      Download
                    </Button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}
