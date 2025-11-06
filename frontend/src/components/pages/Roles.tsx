import { useState } from 'react';
import { Shield, Plus, Edit, Trash2, Users } from 'lucide-react';
import { Card } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../ui/dialog';
import { Label } from '../ui/label';
import { Input } from '../ui/input';
import { Checkbox } from '../ui/checkbox';

interface Role {
  id: string;
  name: string;
  description: string;
  userCount: number;
  permissions: string[];
  color: string;
}

const allPermissions = [
  { id: 'view_dashboard', label: 'View Dashboard', category: 'Dashboard' },
  { id: 'view_users', label: 'View Users', category: 'Users' },
  { id: 'create_users', label: 'Create Users', category: 'Users' },
  { id: 'edit_users', label: 'Edit Users', category: 'Users' },
  { id: 'delete_users', label: 'Delete Users', category: 'Users' },
  { id: 'lock_users', label: 'Lock/Unlock Users', category: 'Users' },
  { id: 'view_trades', label: 'View Trades', category: 'Trading' },
  { id: 'execute_trades', label: 'Execute Trades', category: 'Trading' },
  { id: 'cancel_trades', label: 'Cancel Trades', category: 'Trading' },
  { id: 'view_wallets', label: 'View Wallets', category: 'Wallets' },
  { id: 'manage_wallets', label: 'Manage Wallets', category: 'Wallets' },
  { id: 'view_coins', label: 'View Coins Catalog', category: 'Coins' },
  { id: 'manage_coins', label: 'Manage Coins', category: 'Coins' },
  { id: 'view_price_feeds', label: 'View Price Feeds', category: 'System' },
  { id: 'manage_price_feeds', label: 'Manage Price Feeds', category: 'System' },
  { id: 'view_reports', label: 'View Reports', category: 'Analytics' },
  { id: 'export_data', label: 'Export Data', category: 'Analytics' },
];

const mockRoles: Role[] = [
  {
    id: '1',
    name: 'Super Admin',
    description: 'Full system access with all permissions',
    userCount: 3,
    permissions: allPermissions.map(p => p.id),
    color: 'red',
  },
  {
    id: '2',
    name: 'Admin',
    description: 'Administrative access to most features',
    userCount: 8,
    permissions: ['view_dashboard', 'view_users', 'edit_users', 'lock_users', 'view_trades', 'view_wallets', 'view_coins', 'manage_coins'],
    color: 'purple',
  },
  {
    id: '3',
    name: 'Support',
    description: 'Customer support with limited access',
    userCount: 15,
    permissions: ['view_dashboard', 'view_users', 'view_trades', 'view_wallets'],
    color: 'blue',
  },
  {
    id: '4',
    name: 'Analyst',
    description: 'Read-only access for analytics',
    userCount: 5,
    permissions: ['view_dashboard', 'view_reports', 'export_data', 'view_trades', 'view_price_feeds'],
    color: 'emerald',
  },
];

export default function Roles() {
  const [roles, setRoles] = useState<Role[]>(mockRoles);
  const [showDialog, setShowDialog] = useState(false);
  const [editingRole, setEditingRole] = useState<Role | null>(null);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    permissions: [] as string[],
  });

  const handleEdit = (role: Role) => {
    setEditingRole(role);
    setFormData({
      name: role.name,
      description: role.description,
      permissions: role.permissions,
    });
    setShowDialog(true);
  };

  const handleDelete = (roleId: string) => {
    setRoles(roles.filter(r => r.id !== roleId));
  };

  const handleSave = () => {
    if (editingRole) {
      setRoles(roles.map(r => r.id === editingRole.id ? { ...r, ...formData } : r));
    } else {
      const newRole: Role = {
        id: String(roles.length + 1),
        name: formData.name,
        description: formData.description,
        userCount: 0,
        permissions: formData.permissions,
        color: 'gray',
      };
      setRoles([...roles, newRole]);
    }
    setShowDialog(false);
    setEditingRole(null);
    setFormData({ name: '', description: '', permissions: [] });
  };

  const togglePermission = (permissionId: string) => {
    setFormData(prev => ({
      ...prev,
      permissions: prev.permissions.includes(permissionId)
        ? prev.permissions.filter(p => p !== permissionId)
        : [...prev.permissions, permissionId]
    }));
  };

  const groupedPermissions = allPermissions.reduce((acc, perm) => {
    if (!acc[perm.category]) acc[perm.category] = [];
    acc[perm.category].push(perm);
    return acc;
  }, {} as Record<string, typeof allPermissions>);

  return (
    <>
      {/* Action Bar */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <p className="text-gray-400">Manage roles and permissions for your team</p>
        </div>
        <Button onClick={() => { setEditingRole(null); setFormData({ name: '', description: '', permissions: [] }); setShowDialog(true); }} className="bg-emerald-500 hover:bg-emerald-600 text-black">
          <Plus className="w-4 h-4 mr-2" />
          Add Role
        </Button>
      </div>

      {/* Roles Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {roles.map((role) => (
          <Card key={role.id} className="bg-gray-900 border-gray-800 p-6">
            <div className="flex items-start justify-between mb-4">
              <div className="flex items-center gap-3">
                <div className={`w-12 h-12 rounded-lg bg-${role.color}-500/10 flex items-center justify-center`}>
                  <Shield className={`w-6 h-6 text-${role.color}-500`} />
                </div>
                <div>
                  <h3 className="text-lg mb-1">{role.name}</h3>
                  <div className="flex items-center gap-2 text-sm text-gray-400">
                    <Users className="w-4 h-4" />
                    {role.userCount} users
                  </div>
                </div>
              </div>
              <div className="flex gap-1">
                <Button size="sm" variant="ghost" onClick={() => handleEdit(role)} className="hover:bg-gray-800">
                  <Edit className="w-4 h-4" />
                </Button>
                <Button size="sm" variant="ghost" onClick={() => handleDelete(role.id)} className="hover:bg-gray-800 text-red-500">
                  <Trash2 className="w-4 h-4" />
                </Button>
              </div>
            </div>
            
            <p className="text-sm text-gray-400 mb-4">{role.description}</p>
            
            <div>
              <div className="text-sm mb-2">Permissions ({role.permissions.length})</div>
              <div className="flex flex-wrap gap-2">
                {role.permissions.slice(0, 3).map((permId) => {
                  const perm = allPermissions.find(p => p.id === permId);
                  return (
                    <Badge key={permId} variant="outline" className="border-gray-700 text-xs">
                      {perm?.label}
                    </Badge>
                  );
                })}
                {role.permissions.length > 3 && (
                  <Badge variant="outline" className="border-gray-700 text-xs">
                    +{role.permissions.length - 3} more
                  </Badge>
                )}
              </div>
            </div>
          </Card>
        ))}
      </div>

      {/* Add/Edit Role Dialog */}
      <Dialog open={showDialog} onOpenChange={setShowDialog}>
        <DialogContent className="bg-gray-900 border-gray-800 max-w-2xl max-h-[80vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingRole ? 'Edit Role' : 'Add New Role'}</DialogTitle>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <Label htmlFor="name">Role Name</Label>
              <Input
                id="name"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="e.g. Content Manager"
              />
            </div>
            <div>
              <Label htmlFor="description">Description</Label>
              <Input
                id="description"
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="bg-gray-800 border-gray-700 mt-2"
                placeholder="Brief description of the role"
              />
            </div>
            <div>
              <Label>Permissions</Label>
              <div className="mt-3 space-y-4">
                {Object.entries(groupedPermissions).map(([category, perms]) => (
                  <div key={category} className="border border-gray-800 rounded-lg p-4">
                    <div className="text-sm mb-3 text-emerald-500">{category}</div>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      {perms.map((perm) => (
                        <div key={perm.id} className="flex items-center space-x-2">
                          <Checkbox
                            id={perm.id}
                            checked={formData.permissions.includes(perm.id)}
                            onCheckedChange={() => togglePermission(perm.id)}
                          />
                          <label
                            htmlFor={perm.id}
                            className="text-sm cursor-pointer"
                          >
                            {perm.label}
                          </label>
                        </div>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDialog(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button onClick={handleSave} className="bg-emerald-500 hover:bg-emerald-600 text-black">
              {editingRole ? 'Save Changes' : 'Create Role'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
