import { useState } from 'react';
import { Search, CheckCircle, XCircle, DollarSign, AlertTriangle } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../ui/dialog';
import { Textarea } from '../ui/textarea';
import { Label } from '../ui/label';

interface Withdrawal {
  id: string;
  withdrawalId: string;
  user: string;
  userId: string;
  amount: string;
  coin: string;
  method: string;
  destination: string;
  status: 'pending' | 'approved' | 'rejected' | 'paid';
  timestamp: string;
  notes?: string;
}

const mockWithdrawals: Withdrawal[] = [
  { id: '1', withdrawalId: 'WTH-001', user: 'John Doe', userId: 'USR-123', amount: '$5,000', coin: 'USDT', method: 'Bank Transfer', destination: 'Bank: ***1234', status: 'pending', timestamp: '2024-10-28 14:23' },
  { id: '2', withdrawalId: 'WTH-002', user: 'Sarah Chen', userId: 'USR-124', amount: '$12,450', coin: 'USDT', method: 'Bank Transfer', destination: 'Bank: ***5678', status: 'pending', timestamp: '2024-10-28 14:15' },
  { id: '3', withdrawalId: 'WTH-003', user: 'Mike Johnson', userId: 'USR-125', amount: '0.15 BTC', coin: 'BTC', method: 'Crypto', destination: '1A1zP...DivfNa', status: 'approved', timestamp: '2024-10-28 14:10' },
  { id: '4', withdrawalId: 'WTH-004', user: 'Emma Wilson', userId: 'USR-126', amount: '$23,100', coin: 'USDT', method: 'Bank Transfer', destination: 'Bank: ***9012', status: 'paid', timestamp: '2024-10-28 13:45', notes: 'Payment completed' },
  { id: '5', withdrawalId: 'WTH-005', user: 'Alex Rivera', userId: 'USR-127', amount: '$15,600', coin: 'USDT', method: 'Bank Transfer', destination: 'Bank: ***3456', status: 'rejected', timestamp: '2024-10-28 13:30', notes: 'Insufficient funds' },
];

export default function WithdrawalsApprovals() {
  const [withdrawals, setWithdrawals] = useState<Withdrawal[]>(mockWithdrawals);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedWithdrawal, setSelectedWithdrawal] = useState<Withdrawal | null>(null);
  const [showDialog, setShowDialog] = useState(false);
  const [action, setAction] = useState<'approve' | 'reject' | 'paid' | null>(null);
  const [notes, setNotes] = useState('');

  const filteredWithdrawals = withdrawals.filter(withdrawal =>
    withdrawal.withdrawalId.toLowerCase().includes(searchQuery.toLowerCase()) ||
    withdrawal.user.toLowerCase().includes(searchQuery.toLowerCase()) ||
    withdrawal.userId.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleAction = (withdrawal: Withdrawal, actionType: 'approve' | 'reject' | 'paid') => {
    setSelectedWithdrawal(withdrawal);
    setAction(actionType);
    setShowDialog(true);
    setNotes('');
  };

  const confirmAction = () => {
    if (selectedWithdrawal && action) {
      const newStatus = action === 'approve' ? 'approved' : action === 'reject' ? 'rejected' : 'paid';
      setWithdrawals(withdrawals.map(w => 
        w.id === selectedWithdrawal.id 
          ? { ...w, status: newStatus, notes }
          : w
      ));
      setShowDialog(false);
      setSelectedWithdrawal(null);
      setAction(null);
      setNotes('');
    }
  };

  const stats = {
    pending: withdrawals.filter(w => w.status === 'pending').length,
    approved: withdrawals.filter(w => w.status === 'approved').length,
    paid: withdrawals.filter(w => w.status === 'paid').length,
    totalPending: withdrawals
      .filter(w => w.status === 'pending')
      .reduce((acc, w) => {
        const amount = w.amount.replace(/[^0-9.]/g, '');
        return acc + (w.coin === 'USDT' ? parseFloat(amount) : 0);
      }, 0),
  };

  return (
    <>
      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Pending Withdrawals</div>
          <div className="text-3xl text-yellow-500">{stats.pending}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Approved (Not Paid)</div>
          <div className="text-3xl text-blue-500">{stats.approved}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Paid Today</div>
          <div className="text-3xl text-emerald-500">{stats.paid}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Pending Amount</div>
          <div className="text-3xl">${(stats.totalPending / 1000).toFixed(1)}K</div>
        </Card>
      </div>

      {/* Search Bar */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
          <Input
            placeholder="Search by withdrawal ID, user..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10 bg-gray-800 border-gray-700"
          />
        </div>
      </Card>

      {/* Withdrawals Table */}
      <Card className="bg-gray-900 border-gray-800">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-800">
              <tr className="text-gray-400 text-sm">
                <th className="text-left p-4">Withdrawal ID</th>
                <th className="text-left p-4">User</th>
                <th className="text-left p-4">Amount</th>
                <th className="text-left p-4">Coin</th>
                <th className="text-left p-4">Method</th>
                <th className="text-left p-4">Destination</th>
                <th className="text-left p-4">Status</th>
                <th className="text-left p-4">Timestamp</th>
                <th className="text-right p-4">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredWithdrawals.map((withdrawal) => (
                <tr key={withdrawal.id} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                  <td className="p-4">
                    <span className="font-mono text-sm">{withdrawal.withdrawalId}</span>
                  </td>
                  <td className="p-4">
                    <div>
                      <div className="text-sm">{withdrawal.user}</div>
                      <div className="text-xs text-gray-400">{withdrawal.userId}</div>
                    </div>
                  </td>
                  <td className="p-4">
                    <span className="text-lg">{withdrawal.amount}</span>
                  </td>
                  <td className="p-4">
                    <Badge variant="outline" className="border-gray-700">
                      {withdrawal.coin}
                    </Badge>
                  </td>
                  <td className="p-4 text-gray-400">{withdrawal.method}</td>
                  <td className="p-4">
                    <span className="font-mono text-xs text-gray-400">{withdrawal.destination}</span>
                  </td>
                  <td className="p-4">
                    <Badge
                      className={
                        withdrawal.status === 'paid' ? 'bg-emerald-500/10 text-emerald-500 border-0' :
                        withdrawal.status === 'approved' ? 'bg-blue-500/10 text-blue-500 border-0' :
                        withdrawal.status === 'pending' ? 'bg-yellow-500/10 text-yellow-500 border-0' :
                        'bg-red-500/10 text-red-500 border-0'
                      }
                    >
                      {withdrawal.status}
                    </Badge>
                  </td>
                  <td className="p-4 text-gray-400 text-sm">{withdrawal.timestamp}</td>
                  <td className="p-4">
                    {withdrawal.status === 'pending' && (
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          size="sm"
                          onClick={() => handleAction(withdrawal, 'approve')}
                          className="bg-emerald-500 hover:bg-emerald-600 text-black"
                        >
                          <CheckCircle className="w-4 h-4 mr-1" />
                          Approve
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleAction(withdrawal, 'reject')}
                          className="border-red-500 text-red-500 hover:bg-red-500/10"
                        >
                          <XCircle className="w-4 h-4 mr-1" />
                          Reject
                        </Button>
                      </div>
                    )}
                    {withdrawal.status === 'approved' && (
                      <Button
                        size="sm"
                        onClick={() => handleAction(withdrawal, 'paid')}
                        className="bg-blue-500 hover:bg-blue-600 text-white"
                      >
                        <DollarSign className="w-4 h-4 mr-1" />
                        Mark as PAID
                      </Button>
                    )}
                    {(withdrawal.status === 'paid' || withdrawal.status === 'rejected') && withdrawal.notes && (
                      <span className="text-xs text-gray-400 italic">{withdrawal.notes}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Confirmation Dialog */}
      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="bg-gray-900 border-gray-800">
          <DialogHeader>
            <DialogTitle>
              {action === 'approve' ? 'Approve Withdrawal' : action === 'reject' ? 'Reject Withdrawal' : 'Mark as PAID'}
            </DialogTitle>
          </DialogHeader>
          {selectedWithdrawal && (
            <div className="space-y-4 py-4">
              <div className="p-4 bg-gray-800 rounded-lg space-y-2">
                <div className="flex justify-between">
                  <span className="text-gray-400">Withdrawal ID:</span>
                  <span className="font-mono">{selectedWithdrawal.withdrawalId}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">User:</span>
                  <span>{selectedWithdrawal.user}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Amount:</span>
                  <span className="text-lg text-red-500">{selectedWithdrawal.amount}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Destination:</span>
                  <span className="font-mono text-sm">{selectedWithdrawal.destination}</span>
                </div>
              </div>
              <div>
                <Label htmlFor="notes">Notes (Optional)</Label>
                <Textarea
                  id="notes"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  className="bg-gray-800 border-gray-700 mt-2"
                  placeholder={
                    action === 'paid' ? 'e.g., Transaction ID: TXN123456' :
                    action === 'reject' ? 'e.g., Insufficient funds' :
                    'e.g., Verified and approved'
                  }
                  rows={3}
                />
              </div>
              {action === 'paid' && (
                <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 rounded-lg text-sm text-emerald-500">
                  <AlertTriangle className="w-4 h-4 inline mr-2" />
                  Confirm that payment has been sent to the user.
                </div>
              )}
              {action === 'approve' && (
                <div className="p-3 bg-blue-500/10 border border-blue-500/20 rounded-lg text-sm text-blue-500">
                  The withdrawal will be approved and ready for payment processing.
                </div>
              )}
            </div>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button
              onClick={confirmAction}
              className={
                action === 'paid' ? 'bg-blue-500 hover:bg-blue-600 text-white' :
                action === 'approve' ? 'bg-emerald-500 hover:bg-emerald-600 text-black' : 
                'bg-red-500 hover:bg-red-600 text-white'
              }
            >
              {action === 'approve' ? 'Approve Withdrawal' : action === 'reject' ? 'Reject Withdrawal' : 'Confirm Payment'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
