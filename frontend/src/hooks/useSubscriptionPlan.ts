import { useCallback, useEffect, useState } from 'react';
import { PaymentApi } from '../api/payment';

interface SubscriptionState {
  planType: number;
  status: string;
}

const DEFAULT_SUBSCRIPTION: SubscriptionState = {
  planType: 0,
  status: 'free',
};

export function useSubscriptionPlan() {
  const [subscription, setSubscription] = useState<SubscriptionState>(DEFAULT_SUBSCRIPTION);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setError(null);
    const res = await PaymentApi.getSubscription();
    if (res.ok) {
      setSubscription({
        planType: res.data.planType,
        status: res.data.status,
      });
    } else {
      setError(res.error);
      setSubscription(DEFAULT_SUBSCRIPTION);
    }
    setLoading(false);
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return {
    planType: subscription.planType,
    status: subscription.status,
    loading,
    error,
    refresh,
    isPremium: subscription.planType === 2,
  };
}

