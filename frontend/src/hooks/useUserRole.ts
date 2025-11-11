import { useState, useEffect } from 'react';
import { getUserInfo } from '../api/http';
import { UserInfo } from '../api/auth';

/**
 * Hook to get current user role and admin status
 * Reads user info from localStorage
 */
export function useUserRole() {
  const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
  const [isAdmin, setIsAdmin] = useState(false);

  useEffect(() => {
    const info = getUserInfo();
    setUserInfo(info);
    setIsAdmin(info?.role === 'Admin');
  }, []);

  return {
    userInfo,
    role: userInfo?.role || 'User',
    isAdmin,
  };
}



