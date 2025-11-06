import { useState } from 'react';
import { Search, Plus, MoreVertical, Lock, Unlock, Eye, Trash2, Mail, Phone } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '../ui/dropdown-menu';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../ui/dialog';
import { Label } from '../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';

interface User {
  id: string;
  name: string;
  email: string;
  phone: string;
  status: 'active' | 'locked' | 'pending';
  role: string;
  totalOrders: number;
  totalTrades: number;
  walletBalance: string;
  joinedDate: string;
  lastActive: string;
}

const mockUsers: User[] = [
  { id: '1', name: 'John Doe', email: 'john@example.com', phone: '+1 234 567 8900', status: 'active', role: 'Premium', totalOrders: 145, totalTrades: 892, walletBalance: '$45,230', joinedDate: '2023-01-15', lastActive: '2m ago' },
  { id: '2', name: 'Sarah Chen', email: 'sarah@example.com', phone: '+1 234 567 8901', status: 'active', role: 'VIP', totalOrders: 289, totalTrades: 1247, walletBalance: '$128,450', joinedDate: '2022-11-03', lastActive: '5m ago' },
  { id: '3', name: 'Mike Johnson', email: 'mike@example.com', phone: '+1 234 567 8902', status: 'locked', role: 'Basic', totalOrders: 23, totalTrades: 67, walletBalance: '$8,920', joinedDate: '2024-03-20', lastActive: '2d ago' },
  { id: '4', name: 'Emma Wilson', email: 'emma@example.com', phone: '+1 234 567 8903', status: 'active', role: 'Premium', totalOrders: 198, totalTrades: 456, walletBalance: '$67,890', joinedDate: '2023-05-12', lastActive: '1h ago' },
  { id: '5', name: 'Alex Rivera', email: 'alex@example.com', phone: '+1 234 567 8904', status: 'pending', role: 'Basic', totalOrders: 0, totalTrades: 0, walletBalance: '$0', joinedDate: '2024-10-25', lastActive: 'Never' },
  { id: '6', name: 'Lisa Anderson', email: 'lisa@example.com', phone: '+1 234 567 8905', status: 'active', role: 'VIP', totalOrders: 342, totalTrades: 2103, walletBalance: '$234,120', joinedDate: '2022-08-09', lastActive: '15m ago' },
];

interface UsersProps {
  onSelectUser: (userId: string) => void;
}

export default function Users({ onSelectUser }: UsersProps) {
  const [users, setUsers] = useState<User[]>(mockUsers);
  const [searchQuery, setSearchQuery] = useState('');
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [newUser, setNewUser] = useState({ name: '', email: '', phone: '', role: 'Basic' });

  const filteredUsers = users.filter(user => 
    user.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
    user.email.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const toggleUserStatus = (userId: string) => {
    setUsers(users.map(user => {
      if (user.id === userId) {
        return { ...user, status: user.status === 'locked' ? 'active' : 'locked' } as User;
      }
      return user;
    }));
  };

  const deleteUser = (userId: string) => {
    setUsers(users.filter(user => user.id !== userId));
  };

  const addUser = () => {
    const user: User = {
      id: String(users.length + 1),
      name: newUser.name,
      email: newUser.email,
      phone: newUser.phone,
      status: 'pending',
      role: newUser.role,
      totalOrders: 0,
      totalTrades: 0,
      walletBalance: '$0',
      joinedDate: new Date().toISOString().split('T')[0],
      lastActive: 'Never',
    };
    setUsers([...users, user]);
    setShowAddDialog(false);
    setNewUser({ name: '', email: '', phone: '', role: 'Basic' });
  };

  return (
    <>
      {/* Action Bar */}
      <div className="flex items-center justify-between mb-6">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
          <Input
            placeholder="Search users..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-10 bg-gray-900 border-gray-800"
          />
        </div>
        <Button onClick={() => setShowAddDialog(true)} className="bg-emerald-500 hover:bg-emerald-600 text-black">
          <Plus className="w-4 h-4 mr-2" />
          Add User
        </Button>
      </div>

      {/* Users Table */}
      <Card className="bg-gray-900 border-gray-800">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="border-b border-gray-800">
              <tr className="text-gray-400 text-sm">
                <th className="text-left p-4">User</th>
                <th className="text-left p-4">Contact</th>
                <th className="text-left p-4">Status</th>
                <th className="text-left p-4">Role</th>
                <th className="text-left p-4">Orders</th>
                <th className="text-left p-4">Wallet Balance</th>
                <th className="text-left p-4">Last Active</th>
                <th className="text-right p-4">Actions</th>
              </tr>
            </thead>
            <tbody>
              {filteredUsers.map((user) => (
                <tr key={user.id} className="border-b border-gray-800 hover:bg-gray-800/50 transition-colors">
                  <td className="p-4">
                    <div>
                      <div className="text-sm">{user.name}</div>
                      <div className="text-xs text-gray-400">Joined {user.joinedDate}</div>
                    </div>
                  </td>
                  <td className="p-4">
                    <div className="space-y-1">
                      <div className="flex items-center gap-2 text-xs text-gray-400">
                        <Mail className="w-3 h-3" />
                        {user.email}
                      </div>
                      <div className="flex items-center gap-2 text-xs text-gray-400">
                        <Phone className="w-3 h-3" />
                        {user.phone}
                      </div>
                    </div>
                  </td>
                  <td className="p-4">
                    <Badge
                      variant={user.status === 'active' ? 'default' : 'secondary'}
                      className={
                        user.status === 'active'
                          ? 'bg-emerald-500/10 text-emerald-500 border-0'
                          : user.status === 'locked'
                          ? 'bg-red-500/10 text-red-500 border-0'
                          : 'bg-yellow-500/10 text-yellow-500 border-0'
                      }
                    >
                      {user.status}
                    </Badge>
                  </td>
                  <td className="p-4">
                    <Badge variant="outline" className="border-gray-700">
                      {user.role}
                    </Badge>
                  </td>
                  <td className="p-4">
                    <div>
                      <div className="text-sm">{user.totalOrders} orders</div>
                      <div className="text-xs text-gray-400">{user.totalTrades} trades</div>
                    </div>
                  </td>
                  <td className="p-4">
                    <span className="text-emerald-500">{user.walletBalance}</span>
                  </td>
                  <td className="p-4">
                    <span className="text-sm text-gray-400">{user.lastActive}</span>
                  </td>
                  <td className="p-4">
                    <div className="flex items-center justify-end gap-2">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => onSelectUser(user.id)}
                        className="hover:bg-gray-800"
                      >
                        <Eye className="w-4 h-4" />
                      </Button>
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button size="sm" variant="ghost" className="hover:bg-gray-800">
                            <MoreVertical className="w-4 h-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="bg-gray-900 border-gray-800">
                          <DropdownMenuItem
                            onClick={() => toggleUserStatus(user.id)}
                            className="cursor-pointer"
                          >
                            {user.status === 'locked' ? (
                              <>
                                <Unlock className="w-4 h-4 mr-2" />
                                Unlock User
                              </>
                            ) : (
                              <>
                                <Lock className="w-4 h-4 mr-2" />
                                Lock User
                              </>
                            )}
                          </DropdownMenuItem>
                          <DropdownMenuItem
                            onClick={() => deleteUser(user.id)}
                            className="cursor-pointer text-red-500"
                          >
                            <Trash2 className="w-4 h-4 mr-2" />
                            Delete User
                          </DropdownMenuItem>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Add User Dialog */}
      <Dialog open={showAddDialog} onOpenChange={setShowAddDialog}>
        <DialogContent className="bg-gray-900 border-gray-800">
          <DialogHeader>
            <DialogTitle>Add New User</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <Label htmlFor="name">Full Name</Label>
              <Input
                id="name"
                value={newUser.name}
                onChange={(e) => setNewUser({ ...newUser, name: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="John Doe"
              />
            </div>
            <div>
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                value={newUser.email}
                onChange={(e) => setNewUser({ ...newUser, email: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="john@example.com"
              />
            </div>
            <div>
              <Label htmlFor="phone">Phone</Label>
              <Input
                id="phone"
                value={newUser.phone}
                onChange={(e) => setNewUser({ ...newUser, phone: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="+1 234 567 8900"
              />
            </div>
            <div>
              <Label htmlFor="role">Role</Label>
              <Select value={newUser.role} onValueChange={(value) => setNewUser({ ...newUser, role: value })}>
                <SelectTrigger className="bg-gray-800 border-gray-700 mt-2">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-gray-900 border-gray-800">
                  <SelectItem value="Basic">Basic</SelectItem>
                  <SelectItem value="Premium">Premium</SelectItem>
                  <SelectItem value="VIP">VIP</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowAddDialog(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button onClick={addUser} className="bg-emerald-500 hover:bg-emerald-600 text-black">
              Add User
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
