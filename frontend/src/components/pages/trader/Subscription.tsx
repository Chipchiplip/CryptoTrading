import { useState, useEffect } from 'react';
import { Check, Crown, Star, Loader2, AlertTriangle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '../../ui/dialog';
import { PaymentApi, type SubscriptionPlan, type BillingHistoryItem } from '../../../api/payment';

const planIcons: Record<string, typeof Star> = {
  Free: Star,
  Premium: Crown,
};

export default function Subscription() {
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [currentPlanType, setCurrentPlanType] = useState<number>(0);
  const [loading, setLoading] = useState(true);
  const [processing, setProcessing] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [billingHistory, setBillingHistory] = useState<BillingHistoryItem[]>([]);
  const [confirmDialogOpen, setConfirmDialogOpen] = useState(false);
  const [pendingPlanType, setPendingPlanType] = useState<number | null>(null);
  const [insufficientBalanceDialogOpen, setInsufficientBalanceDialogOpen] = useState(false);
  const [insufficientBalanceError, setInsufficientBalanceError] = useState<{ required: number; available: number } | null>(null);

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

    // Prevent downgrading while Premium subscription is active
    if (currentPlanType === 2 && planType < currentPlanType) {
      setError('Bạn đang ở gói Premium. Vui lòng hủy subscription hiện tại, hệ thống sẽ tự chuyển về Free khi hết hạn.');
      return;
    }

    // Show confirmation dialog for plan changes
    setPendingPlanType(planType);
    setConfirmDialogOpen(true);
  };

  const confirmPlanChange = async () => {
    if (pendingPlanType === null) return;

    const planType = pendingPlanType;
    setConfirmDialogOpen(false);
    setProcessing(planType);
    setError(null);

    try {
      const res = await PaymentApi.createSubscriptionCheckout({ planType });

      if (!res.ok) {
        // Try to parse error message - it might be a string or an object
        let errorMessage = res.error;
        let hasInsufficientBalance = false;
        let balanceError: { required: number; available: number } | null = null;
        
        try {
          // If error is a string that looks like JSON, try to parse it
          if (typeof res.error === 'string' && res.error.startsWith('{')) {
            const errorData = JSON.parse(res.error);
            if (errorData.required !== undefined && errorData.available !== undefined) {
              hasInsufficientBalance = true;
              balanceError = {
                required: Number(errorData.required),
                available: Number(errorData.available)
              };
            } else if (errorData.message) {
              errorMessage = errorData.message;
            }
          } else if (typeof res.error === 'string' && res.error.includes('Insufficient')) {
            hasInsufficientBalance = true;
          }
        } catch {
          // If parsing fails, use the original error
          errorMessage = res.error;
        }
        
        // Show modal dialog for insufficient balance, otherwise show regular error
        if (hasInsufficientBalance && balanceError) {
          setInsufficientBalanceError(balanceError);
          setInsufficientBalanceDialogOpen(true);
        } else {
          setError(errorMessage);
        }
        setProcessing(null);
        return;
      }

      // Success - reload subscription data
      if (res.data.message) {
        setError(null);
        await loadSubscription();
        setProcessing(null);
        // Show success message briefly
        setTimeout(() => {
          setError(null);
        }, 3000);
      } else {
        setError('Unexpected response from server');
        setProcessing(null);
      }
    } catch (e: any) {
      setError(e?.message || 'Failed to create subscription');
      setProcessing(null);
    }
  };

  const getPlanName = (planType: number) => {
    const planNames: Record<number, string> = { 0: 'Free', 2: 'Premium' };
    return planNames[planType] || 'Unknown';
  };

  const getPlanChangeType = (from: number, to: number) => {
    if (to < from) return { type: 'downgrade', label: 'Downgrade' };
    if (to > from) return { type: 'upgrade', label: 'Upgrade' };
    return { type: 'change', label: 'Change' };
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

  const marketingFeatures: Record<number, string[]> = {
    0: [
      'Trade up to 10 spot orders per day (0.2% fee)',
      'Basic watchlist & summary-only portfolio view',
      'No AI Chat, AI bots or live recommendations',
      'Email support during business hours',
      'Perfect for testing the platform with a small balance'
    ],
    2: [
      'Unlimited trades and bot automation at 0.05% fee',
      'Unlimited watchlists plus smart alerts',
      'Full AI suite: AI chat, live recommendations, custom bots',
      'Detailed portfolio analytics (PnL, NAV, per-bot metrics)',
      'API access and professional tooling for power users'
    ]
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-8 text-center">
        <h1 className="text-4xl mb-2">Choose Your Plan</h1>
        <p className="text-gray-400">Free keeps the bare essentials; Premium unlocks AI chat, automated bots, unlimited watchlists, and deep portfolio analytics.</p>
      </div>

      {error && !insufficientBalanceDialogOpen && (
        <div className="mb-6 p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error}
        </div>
      )}

      {/* Plans */}
      <div className="grid md:grid-cols-2 gap-6 mb-12 max-w-4xl mx-auto">
        {plans.map((plan) => {
          const Icon = planIcons[plan.name] || Star;
          const isCurrent = plan.id === currentPlanType;
          const isPopular = plan.id === 2; // Premium plan
          const isProcessing = processing === plan.id;

          const isDowngradeLocked = currentPlanType === 2 && plan.id === 0;

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
                {(marketingFeatures[plan.id] ?? plan.features).map((feature, index) => (
                  <li key={index} className="flex items-start gap-2">
                    <Check className="w-5 h-5 text-emerald-500 flex-shrink-0 mt-0.5" />
                    <span className="text-gray-300">{feature}</span>
                  </li>
                ))}
              </ul>

              {isDowngradeLocked && (
                <div className="text-sm text-yellow-400 mb-4 bg-yellow-500/10 border border-yellow-500/30 rounded p-2 text-center">
                  Đang có gói Premium hoạt động. Hủy subscription để quay về Free sau khi hết hạn.
                </div>
              )}

              <Button
                className={`w-full ${
                  isCurrent
                    ? 'bg-gray-700 text-gray-300 cursor-not-allowed'
                    : isDowngradeLocked
                      ? 'bg-gray-800 text-gray-500 cursor-not-allowed'
                      : 'bg-emerald-500 text-black hover:bg-emerald-600'
                }`}
                disabled={isCurrent || isProcessing || isDowngradeLocked}
                onClick={() => handleUpgrade(plan.id)}
              >
                {isProcessing ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Processing...
                  </>
                ) : isCurrent ? (
                  'Current Plan'
                ) : isDowngradeLocked ? (
                  'Locked'
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

      {/* Confirmation Dialog */}
      <Dialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen}>
        <DialogContent className="bg-gray-900 border-gray-800 text-white">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-yellow-500" />
              Confirm Plan Change
            </DialogTitle>
            <DialogDescription className="text-gray-400">
              {pendingPlanType !== null && (
                <>
                  You are about to {getPlanChangeType(currentPlanType, pendingPlanType).type} from{' '}
                  <span className="font-semibold text-white">{getPlanName(currentPlanType)}</span> to{' '}
                  <span className="font-semibold text-emerald-500">{getPlanName(pendingPlanType)}</span> plan.
                  {pendingPlanType < currentPlanType && (
                    <div className="mt-2 p-3 bg-blue-500/10 border border-blue-500/50 rounded text-blue-400 text-sm">
                      <strong>Note:</strong> Downgrading to a lower plan is free and will take effect immediately.
                    </div>
                  )}
                  {pendingPlanType > currentPlanType && (
                    <div className="mt-2 p-3 bg-yellow-500/10 border border-yellow-500/50 rounded text-yellow-400 text-sm">
                      <strong>Note:</strong> Upgrading will charge your wallet. Please ensure you have sufficient balance.
                    </div>
                  )}
                </>
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => {
                setConfirmDialogOpen(false);
                setPendingPlanType(null);
              }}
              className="text-gray-400 hover:text-white"
            >
              Cancel
            </Button>
            <Button
              onClick={confirmPlanChange}
              className="bg-emerald-500 text-black hover:bg-emerald-600"
            >
              Confirm
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Insufficient Balance Dialog */}
      <Dialog open={insufficientBalanceDialogOpen} onOpenChange={setInsufficientBalanceDialogOpen}>
        <DialogContent className="bg-gray-900 border-gray-800 text-white">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-red-500" />
              Insufficient Balance
            </DialogTitle>
            <DialogDescription className="text-gray-400">
              {insufficientBalanceError && (
                <div className="mt-4 space-y-3">
                  <p className="text-white">
                    You don't have enough balance to upgrade to this plan.
                  </p>
                  <div className="p-4 bg-red-500/10 border border-red-500/50 rounded-lg space-y-2">
                    <div className="flex justify-between items-center">
                      <span className="text-gray-400">Required:</span>
                      <span className="text-white font-semibold">
                        ${insufficientBalanceError.required.toFixed(2)}
                      </span>
                    </div>
                    <div className="flex justify-between items-center">
                      <span className="text-gray-400">Available:</span>
                      <span className="text-red-400 font-semibold">
                        ${insufficientBalanceError.available.toFixed(2)}
                      </span>
                    </div>
                    <div className="flex justify-between items-center pt-2 border-t border-red-500/30">
                      <span className="text-gray-400">Shortfall:</span>
                      <span className="text-red-400 font-semibold">
                        ${(insufficientBalanceError.required - insufficientBalanceError.available).toFixed(2)}
                      </span>
                    </div>
                  </div>
                  <p className="text-sm text-gray-400 mt-4">
                    Please deposit more funds to your wallet before upgrading.
                  </p>
                </div>
              )}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => {
                setInsufficientBalanceDialogOpen(false);
                setInsufficientBalanceError(null);
              }}
              className="text-gray-400 hover:text-white"
            >
              Close
            </Button>
            <Button
              onClick={() => {
                setInsufficientBalanceDialogOpen(false);
                setInsufficientBalanceError(null);
                // Navigate to deposit page if needed
                window.location.href = '/deposit';
              }}
              className="bg-emerald-500 text-black hover:bg-emerald-600"
            >
              Go to Deposit
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
