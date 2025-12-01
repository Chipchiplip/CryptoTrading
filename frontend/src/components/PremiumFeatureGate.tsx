import { Lock } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Button } from './ui/button';
import { Card } from './ui/card';

interface PremiumFeatureGateProps {
  featureName: string;
  description: string;
  helperText?: string;
}

export function PremiumFeatureGate({ featureName, description, helperText }: PremiumFeatureGateProps) {
  const navigate = useNavigate();

  return (
    <div className="p-4 lg:p-8 flex items-center justify-center min-h-[60vh]">
      <Card className="bg-gray-900 border-emerald-500/30 max-w-2xl w-full p-10 text-center space-y-6">
        <div className="mx-auto w-20 h-20 rounded-full bg-emerald-500/10 flex items-center justify-center">
          <Lock className="w-10 h-10 text-emerald-400" />
        </div>
        <div>
          <p className="text-emerald-400 uppercase tracking-widest text-sm mb-2">Premium Feature</p>
          <h1 className="text-3xl font-semibold text-white mb-4">{featureName} bị khóa</h1>
          <p className="text-gray-300">{description}</p>
          {helperText && <p className="text-gray-400 text-sm mt-3">{helperText}</p>}
        </div>
        <Button
          className="bg-emerald-500 text-black hover:bg-emerald-600 px-8 py-6 text-lg"
          onClick={() => navigate('/subscription')}
        >
          Nâng cấp Premium ngay
        </Button>
      </Card>
    </div>
  );
}

