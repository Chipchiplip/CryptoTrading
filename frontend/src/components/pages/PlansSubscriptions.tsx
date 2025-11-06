import { useState } from 'react';
import { Plus, Edit, Trash2, CheckCircle } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../ui/dialog';
import { Label } from '../ui/label';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Switch } from '../ui/switch';

interface Plan {
  id: string;
  name: string;
  price: string;
  billingCycle: 'monthly' | 'yearly';
  description: string;
  features: string[];
  isActive: boolean;
  subscribersCount: number;
}

const mockPlans: Plan[] = [
  {
    id: '1',
    name: 'Basic',
    price: '$0',
    billingCycle: 'monthly',
    description: 'Perfect for getting started',
    features: ['Basic trading', 'Standard fees', 'Email support', '5 coins access'],
    isActive: true,
    subscribersCount: 1245,
  },
  {
    id: '2',
    name: 'Premium',
    price: '$29.99',
    billingCycle: 'monthly',
    description: 'For serious traders',
    features: ['Advanced trading tools', 'Reduced fees (0.5%)', 'Priority support', 'All coins access', 'Advanced analytics'],
    isActive: true,
    subscribersCount: 432,
  },
  {
    id: '3',
    name: 'VIP',
    price: '$99.99',
    billingCycle: 'monthly',
    description: 'Ultimate trading experience',
    features: ['Pro trading features', 'Lowest fees (0.1%)', '24/7 dedicated support', 'All coins + new listings', 'API access', 'Premium analytics', 'Personal account manager'],
    isActive: true,
    subscribersCount: 87,
  },
  {
    id: '4',
    name: 'Enterprise',
    price: '$499',
    billingCycle: 'monthly',
    description: 'Custom solution for institutions',
    features: ['White-label solution', 'Custom fees', 'Dedicated infrastructure', 'Full API access', 'Custom integrations', 'SLA guarantee'],
    isActive: false,
    subscribersCount: 12,
  },
];

export default function PlansSubscriptions() {
  const [plans, setPlans] = useState<Plan[]>(mockPlans);
  const [showDialog, setShowDialog] = useState(false);
  const [editingPlan, setEditingPlan] = useState<Plan | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    price: '',
    billingCycle: 'monthly' as 'monthly' | 'yearly',
    description: '',
    features: [''],
    isActive: true,
  });

  const handleEdit = (plan: Plan) => {
    setEditingPlan(plan);
    setFormData({
      name: plan.name,
      price: plan.price,
      billingCycle: plan.billingCycle,
      description: plan.description,
      features: plan.features,
      isActive: plan.isActive,
    });
    setShowDialog(true);
  };

  const handleDelete = (planId: string) => {
    setPlans(plans.filter(p => p.id !== planId));
  };

  const handleSave = () => {
    if (editingPlan) {
      setPlans(plans.map(p => p.id === editingPlan.id ? { ...p, ...formData } : p));
    } else {
      const newPlan: Plan = {
        id: String(plans.length + 1),
        name: formData.name,
        price: formData.price,
        billingCycle: formData.billingCycle,
        description: formData.description,
        features: formData.features.filter(f => f.trim() !== ''),
        isActive: formData.isActive,
        subscribersCount: 0,
      };
      setPlans([...plans, newPlan]);
    }
    setShowDialog(false);
    setEditingPlan(null);
    setFormData({ name: '', price: '', billingCycle: 'monthly', description: '', features: [''], isActive: true });
  };

  const updateFeature = (index: number, value: string) => {
    const newFeatures = [...formData.features];
    newFeatures[index] = value;
    setFormData({ ...formData, features: newFeatures });
  };

  const addFeature = () => {
    setFormData({ ...formData, features: [...formData.features, ''] });
  };

  const removeFeature = (index: number) => {
    setFormData({ ...formData, features: formData.features.filter((_, i) => i !== index) });
  };

  const totalSubscribers = plans.reduce((acc, plan) => acc + plan.subscribersCount, 0);
  const totalRevenue = plans.reduce((acc, plan) => {
    const price = parseFloat(plan.price.replace(/[$,]/g, '')) || 0;
    return acc + (price * plan.subscribersCount);
  }, 0);

  return (
    <>
      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Plans</div>
          <div className="text-3xl">{plans.length}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Subscribers</div>
          <div className="text-3xl">{totalSubscribers}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Monthly Revenue</div>
          <div className="text-3xl text-emerald-500">${(totalRevenue / 1000).toFixed(1)}K</div>
        </Card>
      </div>

      {/* Action Bar */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <p className="text-gray-400">Manage subscription plans and pricing</p>
        </div>
        <Button onClick={() => { setEditingPlan(null); setFormData({ name: '', price: '', billingCycle: 'monthly', description: '', features: [''], isActive: true }); setShowDialog(true); }} className="bg-emerald-500 hover:bg-emerald-600 text-black">
          <Plus className="w-4 h-4 mr-2" />
          Add Plan
        </Button>
      </div>

      {/* Plans Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {plans.map((plan) => (
          <Card key={plan.id} className={`bg-gray-900 border-gray-800 p-6 ${!plan.isActive ? 'opacity-60' : ''}`}>
            <div className="flex items-start justify-between mb-4">
              <div>
                <h3 className="text-xl mb-1">{plan.name}</h3>
                <Badge variant={plan.isActive ? 'default' : 'secondary'} className={plan.isActive ? 'bg-emerald-500/10 text-emerald-500 border-0' : 'bg-gray-700 text-gray-400 border-0'}>
                  {plan.isActive ? 'Active' : 'Inactive'}
                </Badge>
              </div>
              <div className="flex gap-1">
                <Button size="sm" variant="ghost" onClick={() => handleEdit(plan)} className="hover:bg-gray-800">
                  <Edit className="w-4 h-4" />
                </Button>
                <Button size="sm" variant="ghost" onClick={() => handleDelete(plan.id)} className="hover:bg-gray-800 text-red-500">
                  <Trash2 className="w-4 h-4" />
                </Button>
              </div>
            </div>

            <div className="mb-4">
              <div className="text-3xl mb-1">{plan.price}</div>
              <div className="text-sm text-gray-400">per {plan.billingCycle === 'monthly' ? 'month' : 'year'}</div>
            </div>

            <p className="text-sm text-gray-400 mb-4">{plan.description}</p>

            <div className="space-y-2 mb-4">
              {plan.features.map((feature, index) => (
                <div key={index} className="flex items-start gap-2 text-sm">
                  <CheckCircle className="w-4 h-4 text-emerald-500 mt-0.5 flex-shrink-0" />
                  <span>{feature}</span>
                </div>
              ))}
            </div>

            <div className="pt-4 border-t border-gray-800">
              <div className="text-sm text-gray-400">
                {plan.subscribersCount} subscribers
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Add/Edit Plan Dialog */}
      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="bg-gray-900 border-gray-800 max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingPlan ? 'Edit Plan' : 'Add New Plan'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="name">Plan Name</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="bg-gray-800 border-gray-700 mt-2"
                  placeholder="e.g. Premium"
                />
              </div>
              <div>
                <Label htmlFor="price">Price</Label>
                <Input
                  id="price"
                  value={formData.price}
                  onChange={(e) => setFormData({ ...formData, price: e.target.value })}
                  className="bg-gray-800 border-gray-700 mt-2"
                  placeholder="e.g. $29.99"
                />
              </div>
            </div>

            <div>
              <Label>Billing Cycle</Label>
              <div className="flex gap-4 mt-2">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    checked={formData.billingCycle === 'monthly'}
                    onChange={() => setFormData({ ...formData, billingCycle: 'monthly' })}
                    className="text-emerald-500"
                  />
                  <span>Monthly</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    checked={formData.billingCycle === 'yearly'}
                    onChange={() => setFormData({ ...formData, billingCycle: 'yearly' })}
                    className="text-emerald-500"
                  />
                  <span>Yearly</span>
                </label>
              </div>
            </div>

            <div>
              <Label htmlFor="description">Description</Label>
              <Textarea
                id="description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="Brief description of the plan"
                rows={2}
              />
            </div>

            <div>
              <div className="flex items-center justify-between mb-2">
                <Label>Features</Label>
                <Button size="sm" variant="outline" onClick={addFeature} className="border-gray-700">
                  <Plus className="w-4 h-4 mr-1" />
                  Add Feature
                </Button>
              </div>
              <div className="space-y-2">
                {formData.features.map((feature, index) => (
                  <div key={index} className="flex gap-2">
                    <Input
                      value={feature}
                      onChange={(e) => updateFeature(index, e.target.value)}
                      className="bg-gray-800 border-gray-700"
                      placeholder="e.g. Advanced trading tools"
                    />
                    {formData.features.length > 1 && (
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => removeFeature(index)}
                        className="text-red-500"
                      >
                        <Trash2 className="w-4 h-4" />
                      </Button>
                    )}
                  </div>
                ))}
              </div>
            </div>

            <div className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
              <div>
                <Label>Active Status</Label>
                <p className="text-sm text-gray-400 mt-1">Make this plan available to users</p>
              </div>
              <Switch
                checked={formData.isActive}
                onCheckedChange={(checked) => setFormData({ ...formData, isActive: checked })}
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button onClick={handleSave} className="bg-emerald-500 hover:bg-emerald-600 text-black">
              {editingPlan ? 'Save Changes' : 'Create Plan'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
