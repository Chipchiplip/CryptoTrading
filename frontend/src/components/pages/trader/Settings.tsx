import { useState, useEffect, useCallback, useRef, ChangeEvent } from 'react';
import { User, Shield, Key, Activity, Camera, Copy, CheckCircle2, QrCode, AlertTriangle, Loader2, Users } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../../ui/tabs';
import { Avatar, AvatarFallback, AvatarImage } from '../../ui/avatar';
import { Badge } from '../../ui/badge';
import { Switch } from '../../ui/switch';
import { QRCodeComponent } from '../../ui/qr-code';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';

import { adminApi, Role, Level, UserListDto } from '../../../api/admin';
import { AuthApi, Enable2FAResponse, UserInfo, UserProfileDto, LoginActivityDto } from '../../../api/auth';
import { ApiResult } from '../../../api/http'
import { getUserInfo, setAccessToken, getAccessToken } from '../../../api/http';
import { UserSubscription } from '../../../api/payment';
const commonTimezones = [
  { value: "Etc/GMT+12", label: "(GMT-12:00) International Date Line West" },
  { value: "Pacific/Midway", label: "(GMT-11:00) Midway Island, Samoa" },
  { value: "Pacific/Honolulu", label: "(GMT-10:00) Hawaii" },
  { value: "America/Anchorage", label: "(GMT-09:00) Alaska" },
  { value: "America/Los_Angeles", label: "(GMT-08:00) Pacific Time (US & Canada)" },
  { value: "America/Denver", label: "(GMT-07:00) Mountain Time (US & Canada)" },
  { value: "America/Chicago", label: "(GMT-06:00) Central Time (US & Canada)" },
  { value: "America/New_York", label: "(GMT-05:00) Eastern Time (US & Canada)" },
  { value: "America/Caracas", label: "(GMT-04:00) Caracas" },
  { value: "America/Sao_Paulo", label: "(GMT-03:00) Brazil" },
  { value: "Atlantic/South_Georgia", label: "(GMT-02:00) Mid-Atlantic" },
  { value: "Europe/London", label: "(GMT+00:00) London, Lisbon" },
  { value: "Europe/Berlin", label: "(GMT+01:00) Berlin, Paris, Rome" },
  { value: "Europe/Athens", label: "(GMT+02:00) Athens, Helsinki" },
  { value: "Europe/Moscow", label: "(GMT+03:00) Moscow" },
  { value: "Asia/Dubai", label: "(GMT+04:00) Abu Dhabi, Muscat" },
  { value: "Asia/Karachi", label: "(GMT+05:00) Karachi" },
  { value: "Asia/Dhaka", label: "(GMT+06:00) Dhaka" },
  { value: "Asia/Bangkok", label: "(GMT+07:00) Bangkok, Hanoi, Jakarta" },
  { value: "Asia/Hong_Kong", label: "(GMT+08:00) Beijing, Hong Kong" },
  { value: "Asia/Tokyo", label: "(GMT+09:00) Tokyo, Seoul" },
  { value: "Australia/Sydney", label: "(GMT+10:00) Sydney" },
  { value: "Pacific/Auckland", label: "(GMT+12:00) Auckland" },
];
export default function Settings() {
  const [currentUserInfo, setCurrentUserInfo] = useState(() => getUserInfo());

  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [avatarUrl, setAvatarUrl] = useState<string>('');
  const [phoneNumber, setPhoneNumber] = useState('');
  const [timezone, setTimezone] = useState('');
  const [profileLoading, setProfileLoading] = useState(false);
  const [profileSuccess, setProfileSuccess] = useState('');
  const [profileError, setProfileError] = useState('');
  const [avatarUploadError, setAvatarUploadError] = useState('');
  const [avatarUploading, setAvatarUploading] = useState(false);
  const avatarInputRef = useRef<HTMLInputElement | null>(null);

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordLoading, setPasswordLoading] = useState(false);
  const [passwordSuccess, setPasswordSuccess] = useState('');
  const [passwordError, setPasswordError] = useState('');

  const [twoFAEnabled, setTwoFAEnabled] = useState(currentUserInfo?.twoFactorEnabled || false);
  const [twoFASetup, setTwoFASetup] = useState<Enable2FAResponse | null>(null);
  const [verificationCode, setVerificationCode] = useState('');
  const [disablePassword, setDisablePassword] = useState('');
  const [twoFALoading, setTwoFALoading] = useState(false);
  const [twoFAError, setTwoFAError] = useState('');
  const [twoFASuccess, setTwoFASuccess] = useState('');

  const [adminUsers, setAdminUsers] = useState<UserListDto[]>([]);
  const [adminLoading, setAdminLoading] = useState(false);
  const [adminError, setAdminError] = useState('');
  const [adminRoles, setAdminRoles] = useState<Role[]>([]);
  const [adminLevels, setAdminLevels] = useState<Level[]>([]);
  const [userSubscriptions, setUserSubscriptions] = useState<Map<number, UserSubscription>>(new Map());

  const [loginActivity, setLoginActivity] = useState<LoginActivityDto[]>([]);
  const [activityLoading, setActivityLoading] = useState(false);
  const [activityError, setActivityError] = useState('');

  const [copied, setCopied] = useState(false);
 
  useEffect(() => {
    setProfileLoading(true);
    AuthApi.getProfile()
      .then(result => {
        if (result.ok) {
          setFullName(result.data.fullName);
          setEmail(result.data.email);
          setFullName(result.data.fullName || '');
          setEmail(result.data.email || '');
          setPhoneNumber(result.data.phoneNumber || '');
          setTimezone(result.data.timezone || '');
          setAvatarUrl(result.data.avatarUrl || '');
        } else {
          setProfileError(result.error);
        }
      })
      .finally(() => setProfileLoading(false));
  }, []);

  const handleUpdateProfile = async () => {
    setProfileLoading(true);
    setProfileError('');
    setProfileSuccess('');
   
    const result = await AuthApi.updateProfile({
      fullName,
      email,
      phoneNumber,
      timezone,
      avatarUrl: avatarUrl || undefined,
    });
    if (result.ok) {
    setProfileSuccess('Profile updated successfully!');
    // Cập nhật lại state với dữ liệu trả về
    const newInfo = { ...currentUserInfo, fullName: result.data.fullName };
    setAccessToken(getAccessToken(), newInfo as UserInfo);
    setCurrentUserInfo(newInfo as UserInfo);

    // Cập nhật lại form
    setFullName(result.data.fullName || '');
    setPhoneNumber(result.data.phoneNumber || '');
    setTimezone(result.data.timezone || '');
  } else {
    setProfileError(result.error);
  }

  setProfileLoading(false);
};

  const handleChangePassword = async () => {
    if (newPassword !== confirmPassword) {
      setPasswordError("New passwords do not match.");
      return;
    }
    setPasswordLoading(true);
    setPasswordError('');
    setPasswordSuccess('');

    const result = await AuthApi.changePassword({ currentPassword, newPassword });
    if (result.ok) {
      setPasswordSuccess('Password changed successfully!');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } else {
      setPasswordError(result.error);
    }
    setPasswordLoading(false);
  };

  const handleAvatarButtonClick = () => {
    avatarInputRef.current?.click();
  };

  const handleAvatarFileChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    setAvatarUploadError('');
    setProfileSuccess('');

    const maxSize = 2 * 1024 * 1024; // 2MB
    if (!file.type.startsWith('image/')) {
      setAvatarUploadError('Please select a valid image file.');
      event.target.value = '';
      return;
    }
    if (file.size > maxSize) {
      setAvatarUploadError('Image must be smaller than 2MB.');
      event.target.value = '';
      return;
    }

    setAvatarUploading(true);
    try {
      const uploadInfo = await AuthApi.requestAvatarUploadUrl(file.name);
      if (!uploadInfo.ok) {
        setAvatarUploadError(uploadInfo.error);
        return;
      }

      // ✅ Upload trực tiếp vào presigned URL từ backend
      // Content-Type phải match với file type
      const uploadResponse = await fetch(uploadInfo.data.uploadUrl, {
        method: 'PUT',
        headers: {
          'Content-Type': file.type || 'application/octet-stream',
        },
        body: file,
      });

      if (!uploadResponse.ok) {
        throw new Error('Failed to upload image to Cloudflare R2.');
      }

      const publicUrl =
        uploadInfo.data.publicUrl ||
        (uploadInfo.data.publicUrlBase
          ? `${uploadInfo.data.publicUrlBase}/${uploadInfo.data.uploadId}`
          : undefined);

      if (!publicUrl) {
        throw new Error('Unable to determine public image URL.');
      }

      const updateResult = await AuthApi.updateProfile({
        avatarUrl: publicUrl,
      });

      if (updateResult.ok) {
        const newAvatarUrl = updateResult.data.avatarUrl || publicUrl;
        setAvatarUrl(newAvatarUrl);
        setProfileSuccess('Avatar updated successfully!');
        setProfileError('');
        if (typeof window !== 'undefined') {
          window.dispatchEvent(
            new CustomEvent('profile:avatar-updated', {
              detail: {
                avatarUrl: newAvatarUrl,
                fullName: updateResult.data.fullName,
              },
            })
          );
        }
      } else {
        setAvatarUploadError(updateResult.error);
      }
    } catch (error: any) {
      setAvatarUploadError(error?.message || 'Failed to upload avatar.');
    } finally {
      setAvatarUploading(false);
      event.target.value = '';
    }
  };

  const handleEnable2FA = async () => {
    if (twoFAEnabled) {
      if (!disablePassword.trim()) {
        setTwoFAError('Please enter your current password to disable 2FA.');
        setTwoFASuccess('');
        return;
      }
      setTwoFALoading(true);
      setTwoFAError('');
      setTwoFASuccess('');
      try {
        const result = await AuthApi.disable2FA({ password: disablePassword });
        if (result.ok) {
          setTwoFAEnabled(false);
          setTwoFASetup(null);
          setTwoFASuccess('2FA has been disabled successfully!');
          setDisablePassword('');
          const userInfo = getUserInfo();
          if (userInfo) setAccessToken(getAccessToken(), { ...userInfo, twoFactorEnabled: false });
        } else {
          setTwoFAError(result.error);
        }
      } catch (err: any) { setTwoFAError(err.message || 'Failed to disable 2FA'); }
      finally { setTwoFALoading(false); }
      return;
    }

    setTwoFALoading(true);
    setTwoFAError('');
    setTwoFASuccess('');
    try {
      const result = await AuthApi.enable2FA();
      if (result.ok) {
        setTwoFASetup(result.data);
      } else {
        setTwoFAError(result.error);
      }
    } catch (err: any) { setTwoFAError(err.message || 'Failed to enable 2FA'); }
    finally { setTwoFALoading(false); }
  };

  const handleVerify2FA = async () => {
    if (!verificationCode.trim()) {
      setTwoFAError('Please enter verification code');
      return;
    }
    setTwoFALoading(true);
    setTwoFAError('');
    setTwoFASuccess('');
    try {
      const result = await AuthApi.verify2FA({ code: verificationCode });
      if (result.ok) {
        setTwoFAEnabled(true);
        setTwoFASetup(null);
        setVerificationCode('');
        setTwoFASuccess('2FA has been enabled successfully!');
        const userInfo = getUserInfo();
        if (userInfo) setAccessToken(getAccessToken(), { ...userInfo, twoFactorEnabled: true });
      } else {
        setTwoFAError(result.error);
      }
    } catch (err: any) { setTwoFAError(err.message || 'Failed to verify 2FA code'); }
    finally { setTwoFALoading(false); }
  };

  const handleCopySecret = () => {
    if (twoFASetup?.secret) {
      navigator.clipboard.writeText(twoFASetup.secret);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const loadAdminUsers = useCallback(async () => {
    setAdminLoading(true);
    setAdminError('');
    try {
        const [usersResult, rolesResult, levelsResult] = await Promise.all([
            adminApi.getUsers(),
            adminApi.getRoles(),
            adminApi.getLevels()
        ]);

        if (usersResult.ok) {
            const responseData = (usersResult.data as any).data;
            if (responseData && Array.isArray(responseData.users)) {
                setAdminUsers(responseData.users);
                // Load subscriptions for all users
                await loadSubscriptionsForUsers(responseData.users);
            } else {
                setAdminError("Cấu trúc dữ liệu người dùng không hợp lệ.");
                setAdminUsers([]);
            }
        } else {
            setAdminError(usersResult.error);
        }

        if (rolesResult.ok) {
            setAdminRoles(rolesResult.data.data.roles);
        } else {
            setAdminError(prev => prev + " | " + rolesResult.error);
        }

        if (levelsResult.ok) {
            setAdminLevels(levelsResult.data.data.levels);
        } else {
            setAdminError(prev => prev + " | " + levelsResult.error);
        }

    } catch (e: any) {
        setAdminError(e.message || 'Không thể tải dữ liệu admin');
    } finally {
        setAdminLoading(false);
    }
}, []);

  const loadSubscriptionsForUsers = async (usersList: UserListDto[]) => {
    const subscriptionPromises = usersList.map(async (user) => {
      try {
        const subResult = await adminApi.getUserSubscription(user.id);
        if (subResult.ok && subResult.data.data) {
          return { userId: user.id, subscription: subResult.data.data as UserSubscription };
        }
        // Default to Free plan if fetch fails
        return { userId: user.id, subscription: { planType: 0, status: 'free', isActive: true, currentPeriodStart: new Date().toISOString(), currentPeriodEnd: new Date().toISOString() } as UserSubscription };
      } catch (err) {
        // Default to Free plan if fetch fails
        return { userId: user.id, subscription: { planType: 0, status: 'free', isActive: true, currentPeriodStart: new Date().toISOString(), currentPeriodEnd: new Date().toISOString() } as UserSubscription };
      }
    });

    const subscriptionResults = await Promise.all(subscriptionPromises);
    const subscriptionsMap = new Map<number, UserSubscription>();
    subscriptionResults.forEach(({ userId, subscription }) => {
      subscriptionsMap.set(userId, subscription);
    });
    setUserSubscriptions(subscriptionsMap);
  };

  const getPlanName = (planType: number | undefined): string => {
    const planNames: Record<number, string> = {
      0: 'Free',
      1: 'Pro',
      2: 'Premium'
    };
    return planNames[planType ?? 0] || 'Free';
  };

  const loadLoginActivity = useCallback(async () => {
    setActivityLoading(true);
    setActivityError('');
    try {
      const result = await AuthApi.getLoginActivity();
      if (result.ok) {
        setLoginActivity(result.data);
      } else {
        setActivityError(result.error);
      }
    } catch (e: any) {
      setActivityError(e.message || 'Failed to load activity');
    } finally {
      setActivityLoading(false);
    }
  }, []);

  const onTabChange = (value: string) => {
    if (value === 'admin' && adminUsers.length === 0) {
      loadAdminUsers();
    }
    if (value === 'activity' && loginActivity.length === 0) {
      loadLoginActivity();
    }
  };

  const handleAdminUpdate = async (userId: number, action: 'role' | 'level' | 'status', value: string | number | boolean) => {
        if (userId === currentUserInfo?.id && (action === 'role' || action === 'status')) {
            alert(`Bạn không thể thay đổi ${action} của chính mình.`);
            loadAdminUsers();
            return;
        }

        let result: ApiResult<any>;
        try {
            if (action === 'role') {
                result = await adminApi.updateUserRole(userId, Number(value));
            } else if (action === 'level') {
                result = await adminApi.updateUserLevel(userId, Number(value));
            } else {
                result = await adminApi.updateUserStatus(userId, value === 'true' || value === true);
            }

            if (result.ok) {
                await loadAdminUsers();
            } else {
                alert(`Cập nhật thất bại: ${result.error}`);
                loadAdminUsers();
            }
        } catch (e: any) {
            alert(`Đã xảy ra lỗi: ${e.message}`);
            loadAdminUsers();
        }
    };
 
  const handleCopyKey = (key: string) => {
    navigator.clipboard.writeText(key);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  
  const apiKeys = [ { id: 'API-001', name: 'Trading Bot', key: 'sk_live_...abc123', created: '2025-01-01', lastUsed: '2 hours ago', status: 'Active' } ];

  return (
    <div className="p-4 lg:p-8">
      <div className="mb-6">
        <h1 className="text-3xl mb-2">Settings</h1>
        <p className="text-gray-400">Manage your account settings and preferences</p>
      </div>

      <Tabs defaultValue="profile" className="space-y-6" onValueChange={onTabChange}>
        <TabsList className="bg-gray-900 border border-gray-800">
          <TabsTrigger value="profile" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            <User className="w-4 h-4 mr-2" />
            Profile
          </TabsTrigger>
          <TabsTrigger value="security" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            <Shield className="w-4 h-4 mr-2" />
            Security
          </TabsTrigger>
          {currentUserInfo?.role === 'Admin' && (
            <TabsTrigger value="admin" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
              <Users className="w-4 h-4 mr-2" />
              Admin
            </TabsTrigger>
          )}
          {currentUserInfo?.role === 'Admin' && (
            <TabsTrigger value="api" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
              <Key className="w-4 h-4 mr-2" />
              API Keys
            </TabsTrigger>
          )}
          <TabsTrigger value="activity" className="data-[state=active]:bg-emerald-500 data-[state=active]:text-black">
            <Activity className="w-4 h-4 mr-2" />
            Login Activity
          </TabsTrigger>
        </TabsList>

        <TabsContent value="profile">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Profile Information</h2>
           
            {profileError && (
              <div className="p-4 bg-red-500/10 border border-red-500/20 text-red-500 rounded-lg mb-4 flex items-center gap-2">
                <AlertTriangle className="w-5 h-5" /> {profileError}
              </div>
            )}
            {profileSuccess && (
              <div className="p-4 bg-emerald-500/10 border border-emerald-500/20 text-emerald-500 rounded-lg mb-4 flex items-center gap-2">
                <CheckCircle2 className="w-5 h-5" /> {profileSuccess}
              </div>
            )}
           
            <div className="space-y-6">
              <div className="flex items-center gap-6">
                <Avatar className="w-24 h-24">
                  <AvatarImage src={avatarUrl || `https://api.dicebear.com/7.x/avataaars/svg?seed=${email || 'default'}`} />
                  <AvatarFallback>{fullName ? fullName.substring(0, 2).toUpperCase() : 'TR'}</AvatarFallback>
                </Avatar>
                <div>
                  <input
                    ref={avatarInputRef}
                    type="file"
                    accept="image/png,image/jpeg,image/gif"
                    className="hidden"
                    onChange={handleAvatarFileChange}
                  />
                  <Button
                    variant="outline"
                    className="border-gray-700 mb-2"
                    onClick={handleAvatarButtonClick}
                    disabled={avatarUploading}
                  >
                    {avatarUploading ? (
                      <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    ) : (
                      <Camera className="w-4 h-4 mr-2" />
                    )}
                    {avatarUploading ? 'Uploading...' : 'Change Photo'}
                  </Button>
                  <p className="text-sm text-gray-400">JPG, PNG or GIF (max. 2MB)</p>
                  {avatarUploadError && (
                    <p className="text-sm text-red-500 mt-2">{avatarUploadError}</p>
                  )}
                </div>
              </div>

              <div className="grid md:grid-cols-2 gap-6">
                <div>
                  <Label htmlFor="fullName">Full Name</Label>
                  <Input
                    id="fullName"
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    className="bg-gray-800 border-gray-700"
                    disabled={profileLoading}
                  />
                </div>
                {/* Phone Number Input */}
                  <div>
                    <Label htmlFor="phoneNumber">Phone Number</Label>
                    <Input
                      id="phoneNumber"
                      value={phoneNumber}
                      onChange={(e) => setPhoneNumber(e.target.value)}
                      className="bg-gray-800 border-gray-700"
                      disabled={profileLoading}
                    />
                  </div>
                  {/* Timezone Input */}
                <div>
                  <Label htmlFor="timezone">Timezone</Label>
                <Select
                  value={timezone}
                  onValueChange={(value: string) => setTimezone(value)}
                  disabled={profileLoading}
                >
                  <SelectTrigger id="timezone" className="bg-gray-800 border-gray-700">
                    <SelectValue placeholder="Chọn múi giờ của bạn" />
                  </SelectTrigger>
                  <SelectContent className="bg-gray-800 border-gray-700 text-white max-h-96">
                    {commonTimezones.map((tz) => (
                      <SelectItem key={tz.value} value={tz.value}>
                        {tz.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                </div>
                <div>
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="bg-gray-800 border-gray-700"
                    disabled={profileLoading}
                  />
                </div>
              </div>

              <Button onClick={handleUpdateProfile} disabled={profileLoading} className="bg-emerald-500 text-black hover:bg-emerald-600">
                {profileLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Save Changes'}
              </Button>
            </div>
          </Card>
        </TabsContent>

        <TabsContent value="security">
          <div className="space-y-6">
            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-6">Two-Factor Authentication (2FA)</h2>
           
              {twoFAError && (
                <div className="p-4 bg-red-500/10 border border-red-500/20 rounded-lg mb-4 flex items-center gap-2">
                  <AlertTriangle className="w-5 h-5 text-red-500" /> {twoFAError}
                </div>
              )}
              {twoFASuccess && (
                <div className="p-4 bg-emerald-500/10 border border-emerald-500/20 rounded-lg mb-4 flex items-center gap-2">
                  <CheckCircle2 className="w-5 h-5 text-emerald-500" /> {twoFASuccess}
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
                  disabled={twoFALoading}
                />
              </div>

              {twoFAEnabled && !twoFASetup && (
                <div className="mb-4">
                  <Label htmlFor="disable-password" className="text-gray-300">
                    Enter Password to Disable
                  </Label>
                  <Input
                    id="disable-password"
                    type="password"
                    value={disablePassword}
                    onChange={(e) => {
                      setDisablePassword(e.target.value);
                      if (twoFAError) setTwoFAError('');
                    }}
                    placeholder="Your current password"
                    className="bg-gray-800 border-gray-700 mt-2"
                  />
                </div>
              )}
       
              {twoFASetup && !twoFAEnabled && (
              <div className="space-y-6">
                <div className="p-4 bg-blue-500/10 border border-blue-500/20 rounded-lg">
                  <h3 className="text-lg font-semibold mb-4 flex items-center gap-2">
                    <QrCode className="w-5 h-5" />
                    Setup Two-Factor Authentication
                  </h3>

                  <div className="grid md:grid-cols-2 gap-6">
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
                          disabled={twoFALoading || !verificationCode.trim()}
                          className="w-full bg-emerald-500 text-black hover:bg-emerald-600"
                        >
                          {twoFALoading ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Verify & Enable 2FA'}
                        </Button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}  
              {twoFAEnabled && !twoFASetup && (
                <div className="p-4 bg-emerald-500/10 border border-emerald-500/20 rounded-lg">
                  <div className="text-emerald-500 mb-2 flex items-center gap-2">
                    <CheckCircle2 className="w-5 h-5" />
                    Your account is protected with 2FA
                  </div>
                </div>
              )}
            </Card>
       
            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-6">Change Password</h2>
             
              {passwordError && (
                <div className="p-4 bg-red-500/10 border border-red-500/20 text-red-500 rounded-lg mb-4 flex items-center gap-2">
                  <AlertTriangle className="w-5 h-5" /> {passwordError}
                </div>
              )}
              {passwordSuccess && (
                <div className="p-4 bg-emerald-500/10 border border-emerald-500/20 text-emerald-500 rounded-lg mb-4 flex items-center gap-2">
                  <CheckCircle2 className="w-5 h-5" /> {passwordSuccess}
                </div>
              )}
             
              <div className="space-y-4">
                <div>
                  <Label htmlFor="currentPassword">Current Password</Label>
                  <Input
                    id="currentPassword"
                    type="password"
                    className="bg-gray-800 border-gray-700"
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    disabled={passwordLoading}
                  />
                </div>
                <div>
                  <Label htmlFor="newPassword">New Password</Label>
                  <Input
                    id="newPassword"
                    type="password"
                    className="bg-gray-800 border-gray-700"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    disabled={passwordLoading}
                  />
                </div>
                <div>
                  <Label htmlFor="confirmPassword">Confirm New Password</Label>
                  <Input
                    id="confirmPassword"
                    type="password"
                    className="bg-gray-800 border-gray-700"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    disabled={passwordLoading}
                  />
                </div>
                <Button onClick={handleChangePassword} disabled={passwordLoading} className="bg-emerald-500 text-black hover:bg-emerald-600">
                  {passwordLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Update Password'}
                </Button>
              </div>
            </Card>
          </div>
        </TabsContent>

        {currentUserInfo?.role === 'Admin' && (
          <TabsContent value="admin">
            <Card className="bg-gray-900 border-gray-800 p-6">
              <h2 className="text-xl mb-6">Admin - User Management</h2>
              {adminError && (
                <div className="p-4 bg-red-500/10 border border-red-500/20 text-red-500 rounded-lg mb-4">{adminError}</div>
              )}
              {adminLoading ? (
                <div className="flex justify-center items-center h-40">
                  <Loader2 className="w-8 h-8 animate-spin text-emerald-500" />
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <Table>
                    <TableHeader>
                      <TableRow className="border-gray-800 hover:bg-gray-900">
                        <TableHead className="text-white">User</TableHead>
                        <TableHead className="text-white">Role</TableHead>
                        <TableHead className="text-white">Subscription</TableHead>
                        <TableHead className="text-white">Status</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                  {adminUsers.map(user => (
                    <TableRow key={user.id} className="border-gray-800">
                      <TableCell>
                        <div className="font-medium">{user.fullName}</div>
                        <div className="text-sm text-gray-400">{user.email}</div>
                      </TableCell>
                      <TableCell>
                        <Select
                          value={String(adminRoles.find(r => r.name === user.role)?.id ?? "")}
                          onValueChange={(value: string) => handleAdminUpdate(user.id, 'role', value)}
                          disabled={user.id === currentUserInfo?.id}
                        >
                          <SelectTrigger className="bg-gray-800 border-gray-700 w-32">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent className="bg-gray-800 border-gray-700 text-white">
                            {adminRoles.map(role => (
                              <SelectItem key={role.id} value={String(role.id)}>
                                {role.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </TableCell>
                      <TableCell>
                        <Badge className={
                          userSubscriptions.get(user.id)?.planType === 0 
                            ? 'bg-gray-500/10 text-gray-400' 
                            : userSubscriptions.get(user.id)?.planType === 1
                            ? 'bg-blue-500/10 text-blue-400'
                            : 'bg-purple-500/10 text-purple-400'
                        }>
                          {getPlanName(userSubscriptions.get(user.id)?.planType)}
                        </Badge>
                      </TableCell>
                      <TableCell>
                        <Select
                          value={String(user.isActive)}
                          onValueChange={(value: string) => handleAdminUpdate(user.id, 'status', value)}
                          disabled={user.id === currentUserInfo?.id}
                        >
                          <SelectTrigger className="bg-gray-800 border-gray-700 w-32">
                            <SelectValue />
                          </SelectTrigger>
                          <SelectContent className="bg-gray-800 border-gray-700 text-white">
                            <SelectItem value="true">Active</SelectItem>
                            <SelectItem value="false">Locked</SelectItem>
                          </SelectContent>
                        </Select>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
                  </Table>
                </div>
              )}
            </Card>
          </TabsContent>
        )}

        {currentUserInfo?.role === 'Admin' && (
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
                    >
                      <Copy className="w-4 h-4" />
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
        )}

        <TabsContent value="activity">
          <Card className="bg-gray-900 border-gray-800 p-6">
            <h2 className="text-xl mb-6">Login Activity</h2>
            
            {activityError && (
              <div className="p-4 bg-red-500/10 border border-red-500/20 text-red-500 rounded-lg mb-4 flex items-center gap-2">
                <AlertTriangle className="w-5 h-5" /> {activityError}
              </div>
            )}

            {activityLoading ? (
              <div className="flex justify-center items-center h-40">
                <Loader2 className="w-8 h-8 animate-spin text-emerald-500" />
              </div>
            ) : loginActivity.length === 0 ? (
              <div className="text-center py-8 text-gray-400">
                No login activity found.
              </div>
            ) : (
              <div className="space-y-4">
                {loginActivity.map((activity, index) => (
                  <div key={activity.id} className="p-4 bg-gray-800 rounded-lg">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center gap-2 mb-2">
                          <div className="text-white">{activity.userAgent || 'Unknown Device'}</div>
                          {index === 0 && (
                            <Badge className="bg-emerald-500/10 text-emerald-500">
                              Current Session
                            </Badge>
                          )}
                        </div>
                        <div className="text-sm text-gray-400 space-y-1">
                          <div>IP: {activity.ip || 'Unknown'}</div>
                          <div>{new Date(activity.createdAt).toLocaleString('en-US', { 
                            year: 'numeric',
                            month: 'short',
                            day: 'numeric',
                            hour: '2-digit',
                            minute: '2-digit'
                          })}</div>
                          <div>
                            {activity.success ? (
                              <span className="text-emerald-500">Login successful</span>
                            ) : (
                              <span className="text-red-500">Login failed</span>
                            )}
                          </div>
                        </div>
                      </div>
                      {index !== 0 && (
                        <Button size="sm" variant="ghost" className="text-red-500 hover:text-red-400">
                          Revoke
                        </Button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
            
            <Button variant="outline" className="w-full mt-6 border-red-500 text-red-500 hover:bg-red-500/10">
              Log Out All Other Sessions
            </Button>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
