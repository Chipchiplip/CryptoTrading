import { useState } from 'react';
import { Search, CheckCircle, XCircle, Eye, Download } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../ui/dialog';
import { Textarea } from '../ui/textarea';
import { Label } from '../ui/label';

interface Deposit {
  id: string;
  depositId: string;
  user: string;
  userId: string;
  amount: string;
  coin: string;
  method: string;
  txHash?: string;
  proof?: string;
  status: 'pending' | 'approved' | 'rejected';
  timestamp: string;
  notes?: string;
}

const mockDeposits: Deposit[] = [
  { id: '1', depositId: 'DEP-001', user: 'John Doe', userId: 'USR-123', amount: '$5,000', coin: 'USDT', method: 'Bank Transfer', proof: 'receipt_001.pdf', status: 'pending', timestamp: '2024-10-28 14:23' },
  { id: '2', depositId: 'DEP-002', user: 'Sarah Chen', userId: 'USR-124', amount: '$12,450', coin: 'USDT', method: 'Bank Transfer', proof: 'receipt_002.pdf', status: 'pending', timestamp: '2024-10-28 14:15' },
  { id: '3', depositId: 'DEP-003', user: 'Mike Johnson', userId: 'USR-125', amount: '$8,920', coin: 'USDT', method: 'Crypto Transfer', txHash: '0x1234...5678', status: 'pending', timestamp: '2024-10-28 14:10' },
  { id: '4', depositId: 'DEP-004', user: 'Emma Wilson', userId: 'USR-126', amount: '$23,100', coin: 'USDT', method: 'Bank Transfer', proof: 'receipt_004.pdf', status: 'approved', timestamp: '2024-10-28 13:45', notes: 'Verified by bank' },
  { id: '5', depositId: 'DEP-005', user: 'Alex Rivera', userId: 'USR-127', amount: '$15,600', coin: 'USDT', method: 'Bank Transfer', proof: 'receipt_005.pdf', status: 'rejected', timestamp: '2024-10-28 13:30', notes: 'Invalid receipt' },
];

export default function DepositsApprovals() {
  const [deposits, setDeposits] = useState<Deposit[]>(mockDeposits);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedDeposit, setSelectedDeposit] = useState<Deposit | null>(null);
  const [showDialog, setShowDialog] = useState(false);
  const [action, setAction] = useState<'approve' | 'reject' | null>(null);
  const [notes, setNotes] = useState('');

  const filteredDeposits = deposits.filter(deposit =>
    deposit.depositId.toLowerCase().includes(searchQuery.toLowerCase()) ||
    deposit.user.toLowerCase().includes(searchQuery.toLowerCase()) ||
    deposit.userId.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleAction = (deposit: Deposit, actionType: 'approve' | 'reject') => {
    setSelectedDeposit(deposit);
    setAction(actionType);
    setShowDialog(true);
    setNotes('');
  };

  const confirmAction = () => {
    if (selectedDeposit && action) {
      setDeposits(deposits.map(d => 
        d.id === selectedDeposit.id 
          ? { ...d, status: action === 'approve' ? 'approved' : 'rejected', notes }
          : d
      ));
      setShowDialog(false);
      setSelectedDeposit(null);
      setAction(null);
      setNotes('');
    }
  };

  const stats = {
    pending: deposits.filter(d => d.status === 'pending').length,
    approved: deposits.filter(d => d.status === 'approved').length,
    rejected: deposits.filter(d => d.status === 'rejected').length,
    totalPending: deposits
      .filter(d => d.status === 'pending')
      .reduce((acc, d) => acc + parseFloat(d.amount.replace(/[$,]/g, '')), 0),
  };

  return (
    <>
      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-6">
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Pending Approvals</div>
          <div className="text-3xl text-yellow-500">{stats.pending}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Total Pending Amount</div>
          <div className="text-3xl">${(stats.totalPending / 1000).toFixed(1)}K</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Approved Today</div>
          <div className="text-3xl text-emerald-500">{stats.approved}</div>
        </Card>
        <Card className="bg-gray-900 border-gray-800 p-6">
          <div className="text-gray-400 text-sm mb-2">Rejected Today</div>
          <div className="text-3xl text-red-500">{stats.rejected}</div>
        </Card>
      </div>

      {/* Search Bar */}
      <Card className="bg-gray-900 border-gray-800 p-6 mb-6">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
          <Input
            placeholder="Search by deposit ID, user..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10 bg-gray-800 border-gray-700"
          />
        </div>
      </Card>

      {/* Deposits Table */}
      <Card className="bg-gray-900 border-gray-800">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-800">
              <tr className="text-gray-400 text-sm">
                <th className="text-left p-4">Deposit ID</th>
                <th className="text-left p-4">User</th>
                <th className="text-left p-4">Amount</th>
                <th className="text-left p-4">Coin</th>
                <th className="text-left p-4">Method</th>
                <th className="text-left p-4">Proof</th>
                <th className="text-left p-4">Status</th>
                <th className="text-left p-4">Timestamp</th>
                <th className="text-right p-4">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredDeposits.map((deposit) => (
                <tr key={deposit.id} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                  <td className="p-4">
                    <span className="font-mono text-sm">{deposit.depositId}</span>
                  </td>
                  <td className="p-4">
                    <div>
                      <div className="text-sm">{deposit.user}</div>
                      <div className="text-xs text-gray-400">{deposit.userId}</div>
                    </div>
                  </td>
                  <td className="p-4">
                    <span className="text-lg">{deposit.amount}</span>
                  </td>
                  <td className="p-4">
                    <Badge variant="outline" className="border-gray-700">
                      {deposit.coin}
                    </Badge>
                  </td>
                  <td className="p-4 text-gray-400">{deposit.method}</td>
                  <td className="p-4">
                    {deposit.proof && (
                      <Button size="sm" variant="ghost" className="text-blue-500 hover:text-blue-400">
                        <Download className="w-4 h-4 mr-1" />
                        View
                      </Button>
                    )}
                    {deposit.txHash && (
                      <span className="font-mono text-xs text-gray-400">{deposit.txHash}</span>
                    )}
                  </td>
                  <td className="p-4">
                    <Badge
                      className={
                        deposit.status === 'approved' ? 'bg-emerald-500/10 text-emerald-500 border-0' :
                        deposit.status === 'pending' ? 'bg-yellow-500/10 text-yellow-500 border-0' :
                        'bg-red-500/10 text-red-500 border-0'
                      }
                    >
                      {deposit.status}
                    </Badge>
                  </td>
                  <td className="p-4 text-gray-400 text-sm">{deposit.timestamp}</td>
                  <td className="p-4">
                    {deposit.status === 'pending' && (
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          size="sm"
                          onClick={() => handleAction(deposit, 'approve')}
                          className="bg-emerald-500 hover:bg-emerald-600 text-black"
                        >
                          <CheckCircle className="w-4 h-4 mr-1" />
                          Approve
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          onClick={() => handleAction(deposit, 'reject')}
                          className="border-red-500 text-red-500 hover:bg-red-500/10"
                        >
                          <XCircle className="w-4 h-4 mr-1" />
                          Reject
                        </Button>
                      </div>
                    )}
                    {deposit.status !== 'pending' && deposit.notes && (
                      <span className="text-xs text-gray-400 italic">{deposit.notes}</span>
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
              {action === 'approve' ? 'Approve Deposit' : 'Reject Deposit'}
            </DialogTitle>
          </DialogHeader>
          {selectedDeposit && (
            <div className="space-y-4 py-4">
              <div className="p-4 bg-gray-800 rounded-lg space-y-2">
                <div className="flex justify-between">
                  <span className="text-gray-400">Deposit ID:</span>
                  <span className="font-mono">{selectedDeposit.depositId}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">User:</span>
                  <span>{selectedDeposit.user}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Amount:</span>
                  <span className="text-lg text-emerald-500">{selectedDeposit.amount}</span>
                </div>
              </div>
              <div>
                <Label htmlFor="notes">Notes (Optional)</Label>
                <Textarea
                  id="notes"
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  className="bg-gray-800 border-gray-700 mt-2"
                  placeholder={action === 'approve' ? 'e.g., Verified by bank' : 'e.g., Invalid receipt'}
                  rows={3}
                />
              </div>
              {action === 'approve' && (
                <div className="p-3 bg-emerald-500/10 border border-emerald-500/20 rounded-lg text-sm text-emerald-500">
                  The amount will be credited to user's wallet after approval.
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
              className={action === 'approve' ? 'bg-emerald-500 hover:bg-emerald-600 text-black' : 'bg-red-500 hover:bg-red-600 text-white'}
            >
              {action === 'approve' ? 'Approve & Credit Wallet' : 'Reject Deposit'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
