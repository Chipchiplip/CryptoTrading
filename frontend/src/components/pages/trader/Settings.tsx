import { useState, useEffect } from 'react';
import { User, Shield, Key, Activity, Camera, Copy, CheckCircle2, QrCode, AlertTriangle } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';
import { Avatar, AvatarFallback, AvatarImage } from '../../ui/avatar';
import { Badge } from '../../ui/badge';
import { Switch } from '../../ui/switch';
import { QRCodeComponent } from '../../ui/qr-code';
import { AuthApi, Enable2FAResponse } from '../../../api/auth';

export default function Settings() {
  const [twoFAEnabled, setTwoFAEnabled] = useState(false);
  const [emailNotifications, setEmailNotifications] = useState(true);
  const [copied, setCopied] = useState(false);
  const [twoFASetup, setTwoFASetup] = useState<Enable2FAResponse | null>(null);
  const [verificationCode, setVerificationCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const apiKeys = [
    { id: 'API-001', name: 'Trading Bot', key: 'sk_live_...abc123', created: '2025-01-01', lastUsed: '2 hours ago', status: 'Active' },
    { id: 'API-002', name: 'Portfolio Tracker', key: 'sk_live_...def456', created: '2024-12-15', lastUsed: '1 day ago', status: 'Active' },
  ];

  const loginActivity = [
    { id: 1, device: 'Chrome on Windows', location: 'New York, US', ip: '192.168.1.1', time: '2 hours ago', current: true },
    { id: 2, device: 'Safari on iPhone', location: 'New York, US', ip: '192.168.1.2', time: '1 day ago', current: false },
    { id: 3, device: 'Chrome on macOS', location: 'Los Angeles, US', ip: '192.168.1.3', time: '3 days ago', current: false },
  ];

  const handleCopyKey = (key: string) => {
    navigator.clipboard.writeText(key);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleEnable2FA = async () => {
    if (twoFAEnabled) {
      // TODO: Implement disable 2FA
      setTwoFAEnabled(false);
      setTwoFASetup(null);
      return;
    }

    setLoading(true);
    setError('');
    
    try {
      const result = await AuthApi.enable2FA();
      if (result.ok) {
        setTwoFASetup(result.data);
        setSuccess('');
      } else {
        setError(result.error);
      }
    } catch (err) {
      setError('Failed to enable 2FA');
    } finally {
      setLoading(false);
    }
  };

  const handleVerify2FA = async () => {
    if (!verificationCode.trim()) {
      setError('Please enter verification code');
      return;
    }

    setLoading(true);
    setError('');
    
    try {
      const result = await AuthApi.verify2FA({ code: verificationCode });
      if (result.ok) {
        setTwoFAEnabled(true);
        setTwoFASetup(null);
        setVerificationCode('');
        setSuccess('2FA has been enabled successfully!');
      } else {
        setError(result.error);
      }
    } catch (err) {
      setError('Failed to verify 2FA code');
    } finally {
      setLoading(false);
    }
  };

  const handleCopySecret = () => {
    if (twoFASetup?.secret) {
      navigator.clipboard.writeText(twoFASetup.secret);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Settings</h1>
        <p className="text-gray-400">Manage your account settings and preferences</p>
      </div>

      <Tabs defaultValue="profile" className="space-y-6">
        <TabsList className="bg-gray-900 border border-gray-800">
          <TabsTrigger value="profile" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            <User className="w-4 h-4 mr-2" />
            Profile
          </TabsTrigger>
          <TabsTrigger value="security" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            <Shield className="w-4 h-4 mr-2" />
            Security
          </TabsTrigger>
          <TabsTrigger value="api" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            <Key className="w-4 h-4 mr-2" />
            API Keys
          </TabsTrigger>
          <TabsTrigger value="activity" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            <Activity className="w-4 h-4 mr-2" />
            Login Activity
          </TabsTrigger>
        </TabsList>

        {/* Profile Tab */}
        <TabsContent value="profile">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Profile Information</h2>
            <div className="space-y-6">
              <div className="flex items-center gap-6">
                <Avatar className="w-24 h-24">
                  <AvatarImage src="https://api.dicebear.com/7.x/avataaars/svg?seed=trader" />
                  <AvatarFallback>TR</AvatarFallback>
                </Avatar>
                <div>
                  <Button variant="outline" className="border-gray-700 mb-2">
                    <Camera className="w-4 h-4 mr-2" />
                    Change Photo
                  </Button>
                  <p className="text-sm text-gray-400">JPG, PNG or GIF (max. 2MB)</p>
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-6">
                <div>
                  <Label htmlFor="fullName">Full Name</Label>
                  <Input
                    id="fullName"
                    defaultValue="John Trader"
                    className="bg-gray-800 border-gray-700"
                  />
                </div>
                <div>
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    defaultValue="trader@example.com"
                    className="bg-gray-800 border-gray-700"
                  />
                </div>
                <div>
                  <Label htmlFor="phone">Phone Number</Label>
                  <Input
                    id="phone"
                    defaultValue="+1 234 567 8900"
                    className="bg-gray-800 border-gray-700"
                  />
                </div>
                <div>
                  <Label htmlFor="timezone">Timezone</Label>
                  <Input
                    id="timezone"
                    defaultValue="UTC-5 (EST)"
                    className="bg-gray-800 border-gray-700"
                  />
                </div>
              </div>

              <div className="flex items-center justify-between p-4 bg-gray-800 rounded-lg">
                <div>
                  <div className="text-white mb-1">Email Notifications</div>
                  <div className="text-sm text-gray-400">Receive email about your account activity</div>
                </div>
                <Switch
                  checked={emailNotifications}
                  onCheckedChange={setEmailNotifications}
                />
              </div>

              <Button className="bg-emerald-500 text-black hover:bg-emerald-600">
                Save Changes
              </Button>
            </div>
          </Card>
        </TabsContent>

        {/* Security Tab */}
        <TabsContent value="security">
          <div className="space-y-6">
            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-6">Two-Factor Authentication (2FA)</h2>
              
              {/* Error/Success Messages */}
              {error && (
                <div className="p-4 bg-red-500/10 border border-red-500/20 rounded-lg mb-4 flex items-center gap-2">
                  <AlertTriangle className="w-5 h-5 text-red-500" />
                  <span className="text-red-500">{error}</span>
                </div>
              )}
              
              {success && (
                <div className="p-4 bg-emerald-500/10 border border-emerald-500/20 rounded-lg mb-4 flex items-center gap-2">
                  <CheckCircle2 className="w-5 h-5 text-emerald-500" />
                  <span className="text-emerald-500">{success}</span>
                </div>
              )}

              <div className="flex items-center justify-between p-4 bg-gray-800 rounded-lg mb-4">
                <div>
                  <div className="text-white mb-1 flex items-center gap-2">
                    2FA Status
                    <Badge className={twoFAEnabled ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {twoFAEnabled ? 'Enabled' : 'Disabled'}
                    </Badge>
                  </div>
                  <div className="text-sm text-gray-400">Add an extra layer of security to your account</div>
                </div>
                <Switch
                  checked={twoFAEnabled}
                  onCheckedChange={handleEnable2FA}
                  disabled={loading}
                />
              </div>

              {/* 2FA Setup Process */}
              {twoFASetup && !twoFAEnabled && (
                <div className="space-y-6">
                  <div className="p-4 bg-blue-500/10 border border-blue-500/20 rounded-lg">
                    <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                      <QrCode className="w-5 h-5" />
                      Setup Two-Factor Authentication
                    </h3>
                    
                    <div className="grid md:grid-cols-2 gap-6">
                      {/* QR Code */}
                      <div className="text-center">
                        <p className="text-sm text-gray-400 mb-4">
                          Scan this QR code with your authenticator app:
                        </p>
                        <QRCodeComponent 
                          value={twoFASetup.qrCodeUrl} 
                          size={200}
                          className="mb-4"
                        />
                      </div>
                      
                      {/* Manual Entry */}
                      <div>
                        <p className="text-sm text-gray-400 mb-4">
                          Or enter this secret key manually:
                        </p>
                        <div className="flex items-center gap-2 mb-4">
                          <code className="flex-1 px-3 py-2 bg-gray-800 rounded text-gray-300 text-sm break-all">
                            {twoFASetup.secret}
                          </code>
                          <Button
                            size="sm"
                            variant="outline"
                            className="border-gray-700"
                            onClick={handleCopySecret}
                          >
                            {copied ? <CheckCircle2 className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                          </Button>
                        </div>
                        
                        <div className="space-y-4">
                          <div>
                            <Label htmlFor="verificationCode">Enter verification code from your app:</Label>
                            <Input
                              id="verificationCode"
                              value={verificationCode}
                              onChange={(e) => setVerificationCode(e.target.value)}
                              placeholder="000000"
                              className="bg-gray-800 border-gray-700"
                              maxLength={6}
                            />
                          </div>
                          
                          <Button 
                            onClick={handleVerify2FA}
                            disabled={loading || !verificationCode.trim()}
                            className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
                          >
                            {loading ? 'Verifying...' : 'Verify & Enable 2FA'}
                          </Button>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )}

              {/* 2FA Enabled State */}
              {twoFAEnabled && (
                <div className="p-4 bg-emerald-500/10 border border-emerald-500/20 rounded-lg">
                  <div className="text-emerald-500 mb-2 flex items-center gap-2">
                    <CheckCircle2 className="w-5 h-5" />
                    Your account is protected with 2FA
                  </div>
                  <p className="text-sm text-gray-400">
                    Two-factor authentication is active. You'll need to enter a code from your authenticator app when logging in.
                  </p>
                </div>
              )}
            </Card>

            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-6">Change Password</h2>
              <div className="space-y-4">
                <div>
                  <Label htmlFor="currentPassword">Current Password</Label>
                  <Input
                    id="currentPassword"
                    type="password"
                    className="bg-gray-800 border-gray-700"
                  />
                </div>
                <div>
                  <Label htmlFor="newPassword">New Password</Label>
                  <Input
                    id="newPassword"
                    type="password"
                    className="bg-gray-800 border-gray-700"
                  />
                </div>
                <div>
                  <Label htmlFor="confirmPassword">Confirm New Password</Label>
                  <Input
                    id="confirmPassword"
                    type="password"
                    className="bg-gray-800 border-gray-700"
                  />
                </div>
                <Button className="bg-emerald-500 text-black hover:bg-emerald-600">
                  Update Password
                </Button>
              </div>
            </Card>
          </div>
        </TabsContent>

        {/* API Keys Tab */}
        <TabsContent value="api">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-xl">API Keys</h2>
              <Button className="bg-emerald-500 text-black hover:bg-emerald-600">
                Create New Key
              </Button>
            </div>
            <div className="space-y-4">
              {apiKeys.map((key) => (
                <div key={key.id} className="p-4 bg-gray-800 rounded-lg">
                  <div className="flex items-center justify-between mb-3">
                    <div>
                      <div className="text-white mb-1">{key.name}</div>
                      <div className="text-sm text-gray-400">Created on {key.created}</div>
                    </div>
                    <Badge className={key.status === 'Active' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-gray-500/10 text-gray-500'}>
                      {key.status}
                    </Badge>
                  </div>
                  <div className="flex items-center gap-2 mb-2">
                    <code className="flex-1 px-3 py-2 bg-gray-900 rounded text-gray-400 text-sm">
                      {key.key}
                    </code>
                    <Button
                      size="sm"
                      variant="outline"
                      className="border-gray-700"
                      onClick={() => handleCopyKey(key.key)}
                    >
                      {copied ? <CheckCircle2 className="w-4 h-4" /> : <Copy className="w-4 h-4" />}
                    </Button>
                  </div>
                  <div className="flex items-center justify-between text-sm">
                    <span className="text-gray-400">Last used: {key.lastUsed}</span>
                    <Button size="sm" variant="ghost" className="text-red-500 hover:text-red-400">
                      Revoke
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          </Card>
        </TabsContent>

        {/* Login Activity Tab */}
        <TabsContent value="activity">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Login Activity</h2>
            <div className="space-y-4">
              {loginActivity.map((activity) => (
                <div key={activity.id} className="p-4 bg-gray-800 rounded-lg">
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center gap-2 mb-2">
                        <div className="text-white">{activity.device}</div>
                        {activity.current && (
                          <Badge className="bg-emerald-500/10 text-emerald-500">
                            Current Session
                          </Badge>
                        )}
                      </div>
                      <div className="text-sm text-gray-400 space-y-1">
                        <div>{activity.location}</div>
                        <div>IP: {activity.ip}</div>
                        <div>{activity.time}</div>
                      </div>
                    </div>
                    {!activity.current && (
                      <Button size="sm" variant="ghost" className="text-red-500 hover:text-red-400">
                        Revoke
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
            <Button variant="outline" className="w-full mt-6 border-red-500 text-red-500 hover:bg-red-500/10">
              Log Out All Other Sessions
            </Button>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
