import { useState, useEffect } from 'react';
import { Check, Crown, Zap, Star, Loader2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { PaymentApi, type SubscriptionPlan, type BillingHistoryItem } from '../../../api/payment';

const planIcons: Record<string, typeof Star> = {
  Free: Star,
  Plus: Zap,
  Pro: Zap,
  Premium: Crown,
};

export default function Subscription() {
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [currentPlanType, setCurrentPlanType] = useState<number>(0);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [billingHistory, setBillingHistory] = useState<BillingHistoryItem[]>([]);

  useEffect(() => {
    loadData();

    // Check for payment status from URL params
    const urlParams = new URLSearchParams(window.location.search);
    const status = urlParams.get('status');
    const plan = urlParams.get('plan');

    if (status === 'success' && plan) {
      setError(null);
      // Reload subscription data
      loadSubscription();
      // Clean URL
      window.history.replaceState({}, '', window.location.pathname);
    } else if (status === 'failed') {
      setError('Payment failed. Please try again.');
      window.history.replaceState({}, '', window.location.pathname);
    }
  }, []);

  const loadData = async () => {
    setLoading(true);
    setError(null);

    try {
      const [plansRes, subscriptionRes, billingRes] = await Promise.all([
        PaymentApi.getPlans(),
        PaymentApi.getSubscription(),
        PaymentApi.getBillingHistory(),
      ]);

      if (!plansRes.ok) {
        setError(plansRes.error);
        setLoading(false);
        return;
      }

      if (subscriptionRes.ok) {
        setCurrentPlanType(subscriptionRes.data.planType);
      }

      if (billingRes.ok) {
        setBillingHistory(billingRes.data.billingHistory);
      }

      setPlans(plansRes.data.plans);
    } catch (e: any) {
      setError(e?.message || 'Failed to load subscription data');
    } finally {
      setLoading(false);
    }
  };

  const loadSubscription = async () => {
    const [subscriptionRes, billingRes] = await Promise.all([
      PaymentApi.getSubscription(),
      PaymentApi.getBillingHistory(),
    ]);
    if (subscriptionRes.ok) {
      setCurrentPlanType(subscriptionRes.data.planType);
    }
    if (billingRes.ok) {
      setBillingHistory(billingRes.data.billingHistory);
    }
  };

  const handleUpgrade = async (planType: number) => {
    if (planType === currentPlanType) return;

    setProcessing(planType);
    setError(null);

    try {
      const res = await PaymentApi.createSubscriptionCheckout({ planType });

      if (!res.ok) {
        setError(res.error);
        setProcessing(null);
        return;
      }

      // Free plan doesn't need payment
      if (res.data.message) {
        setError(null);
        await loadSubscription();
        setProcessing(null);
        return;
      }

      // Redirect to VNPay
      if (res.data.paymentUrl) {
        window.location.href = res.data.paymentUrl;
      } else {
        setError('Payment URL not received');
        setProcessing(null);
      }
    } catch (e: any) {
      setError(e?.message || 'Failed to create checkout');
      setProcessing(null);
    }
  };

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });
    } catch {
      return dateString;
    }
  };

  const getStatusBadgeClass = (status: string) => {
    switch (status.toLowerCase()) {
      case 'success':
        return 'bg-emerald-500/10 text-emerald-500';
      case 'pending':
        return 'bg-yellow-500/10 text-yellow-500';
      case 'failed':
        return 'bg-red-500/10 text-red-500';
      default:
        return 'bg-gray-500/10 text-gray-500';
    }
  };

  if (loading) {
    return (
      <div className="p-4 lg:p-8 flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <Loader2 className="w-8 h-8 text-emerald-500 animate-spin mx-auto mb-4" />
          <p className="text-gray-400">Loading subscription plans...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-8 text-center">
        <h1 className="text-4xl mb-2">Choose Your Plan</h1>
        <p className="text-gray-400">Upgrade to unlock more features and better rates</p>
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error}
        </div>
      )}

      {/* Plans */}
      <div className="grid md:grid-cols-3 gap-6 mb-12">
        {plans.map((plan) => {
          const Icon = planIcons[plan.name] || Star;
          const isCurrent = plan.id === currentPlanType;
          const isPopular = plan.id === 1; // Plus/Pro plan
          const isProcessing = processing === plan.id;

          return (
            <Card
              key={plan.id}
              className={`bg-gray-900 border-gray-800 p-8 relative ${
                isPopular ? 'border-emerald-500' : ''
              }`}
            >
              {isPopular && (
                <Badge className="absolute -top-3 left-1/2 transform -translate-x-1/2 bg-emerald-500 text-black">
                  Most Popular
                </Badge>
              )}
              {isCurrent && (
                <Badge className="absolute -top-3 left-1/2 transform -translate-x-1/2 bg-blue-500 text-white">
                  Current Plan
                </Badge>
              )}

              <div className="text-center mb-6">
                <div className="w-16 h-16 bg-emerald-500/10 rounded-full flex items-center justify-center mx-auto mb-4">
                  <Icon className="w-8 h-8 text-emerald-500" />
                </div>
                <h3 className="text-2xl mb-2">{plan.name}</h3>
                <div className="text-4xl text-white mb-1">
                  ${plan.price}
                  <span className="text-xl text-gray-400">/{plan.period}</span>
                </div>
                {plan.priceVnd > 0 && (
                  <div className="text-sm text-gray-400 mt-1">
                    ~{plan.priceVnd.toLocaleString('vi-VN')} VND
                  </div>
                )}
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
                  isCurrent
                    ? 'bg-gray-700 text-gray-300 cursor-not-allowed'
                    : 'bg-emerald-500 text-black hover:bg-emerald-600'
                }`}
                disabled={isCurrent || isProcessing}
                onClick={() => handleUpgrade(plan.id)}
              >
                {isProcessing ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Processing...
                  </>
                ) : isCurrent ? (
                  'Current Plan'
                ) : (
                  'Upgrade Now'
                )}
              </Button>
            </Card>
          );
        })}
      </div>

      {/* Billing History */}
      <Card className="bg-gray-900 border-gray-800 p-6">
        <h2 className="text-xl mb-6">Billing History</h2>
        {billingHistory.length === 0 ? (
          <div className="text-center py-8 text-gray-400">
            <p>No billing history found.</p>
            <p className="text-sm mt-2">Your subscription payments will appear here.</p>
          </div>
        ) : (
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
                    <td className="py-4 text-emerald-500 font-mono text-sm">{invoice.invoiceId}</td>
                    <td className="py-4 text-gray-300">{formatDate(invoice.date)}</td>
                    <td className="py-4 text-white">{invoice.plan}</td>
                    <td className="py-4 text-right text-white">
                      {invoice.currency === 'VND' ? (
                        <div>
                          <div>{invoice.amount.toLocaleString('vi-VN')} VND</div>
                          <div className="text-xs text-gray-400">~${invoice.amountUsd.toFixed(2)}</div>
                        </div>
                      ) : (
                        `$${invoice.amountUsd.toFixed(2)}`
                      )}
                    </td>
                    <td className="py-4">
                      <Badge className={getStatusBadgeClass(invoice.status)}>
                        {invoice.status.charAt(0).toUpperCase() + invoice.status.slice(1)}
                      </Badge>
                    </td>
                    <td className="py-4 text-right">
                      {invoice.status === 'success' && (
                        <Button size="sm" variant="ghost" className="text-emerald-500">
                          Download
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
