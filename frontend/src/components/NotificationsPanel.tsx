import {
  CheckCircle,
  XCircle,
  AlertTriangle,
  Info,
  Clock,
} from "lucide-react";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from "./ui/sheet";
import { Badge } from "./ui/badge";
import { Button } from "./ui/button";
import { ScrollArea } from "./ui/scroll-area";

interface Notification {
  id: string;
  type: "success" | "error" | "warning" | "info";
  title: string;
  message: string;
  timestamp: string;
  isRead: boolean;
  action?: string;
  actionLink?: string;
}

const mockNotifications: Notification[] = [
  {
    id: "1",
    type: "warning",
    title: "Pending Deposit Approval",
    message:
      "User John Doe submitted a deposit of $5,000 requiring approval",
    timestamp: "2 minutes ago",
    isRead: false,
    action: "Review",
    actionLink: "deposits-approvals",
  },
  {
    id: "2",
    type: "warning",
    title: "Withdrawal Request",
    message: "Sarah Chen requested withdrawal of $12,450",
    timestamp: "5 minutes ago",
    isRead: false,
    action: "Review",
    actionLink: "withdrawals-approvals",
  },
  {
    id: "3",
    type: "error",
    title: "Price Feed Error",
    message: "Kraken API connection timeout",
    timestamp: "15 minutes ago",
    isRead: false,
    action: "View",
    actionLink: "price-feeds",
  },
  {
    id: "4",
    type: "success",
    title: "Deposit Approved",
    message:
      "Deposit #DEP-1234 has been processed successfully",
    timestamp: "1 hour ago",
    isRead: true,
  },
  {
    id: "5",
    type: "info",
    title: "New User Registration",
    message: "Alex Rivera has registered a new account",
    timestamp: "2 hours ago",
    isRead: true,
    action: "View",
    actionLink: "users",
  },
  {
    id: "6",
    type: "success",
    title: "Withdrawal Completed",
    message: "Withdrawal #WTH-5678 marked as PAID",
    timestamp: "3 hours ago",
    isRead: true,
  },
  {
    id: "7",
    type: "warning",
    title: "Rate Limit Warning",
    message:
      "CoinMarketCap API approaching rate limit (85% used)",
    timestamp: "4 hours ago",
    isRead: true,
    action: "View",
    actionLink: "price-feeds",
  },
];

interface NotificationsPanelProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onNavigate: (page: string) => void;
}

export default function NotificationsPanel({
  open,
  onOpenChange,
  onNavigate,
}: NotificationsPanelProps) {
  const unreadCount = mockNotifications.filter(
    (n) => !n.isRead,
  ).length;

  const getIcon = (type: Notification["type"]) => {
    switch (type) {
      case "success":
        return (
          <CheckCircle className="w-5 h-5 text-emerald-500" />
        );
      case "error":
        return <XCircle className="w-5 h-5 text-red-500" />;
      case "warning":
        return (
          <AlertTriangle className="w-5 h-5 text-yellow-500" />
        );
      case "info":
        return <Info className="w-5 h-5 text-blue-500" />;
    }
  };

  const handleAction = (notification: Notification) => {
    if (notification.actionLink) {
      onNavigate(notification.actionLink);
      onOpenChange(false);
    }
  };

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="bg-gray-900 border-gray-800 w-96 text-white">
        <SheetHeader>
          <SheetTitle className="flex items-center justify-between text-white">
            <span>Notifications</span>
            {unreadCount > 0 && (
              <Badge className="bg-emerald-500 text-white">
                {unreadCount} new
              </Badge>
            )}
          </SheetTitle>
        </SheetHeader>

        <ScrollArea className="h-[calc(100vh-100px)] mt-6">
          <div className="space-y-2">
            {mockNotifications.map((notification) => (
              <div
                key={notification.id}
                className={`p-4 rounded-lg border transition-colors ${
                  notification.isRead
                    ? "bg-gray-800/50 border-gray-800"
                    : "bg-gray-800 border-gray-700"
                }`}
              >
                <div className="flex items-start gap-3 mb-2">
                  {getIcon(notification.type)}
                  <div className="flex-1">
                    <div className="flex items-start justify-between mb-1">
                      <h4 className="text-sm font-medium text-gray-300">
                        {notification.title}
                      </h4>
                      {!notification.isRead && (
                        <div className="w-2 h-2 rounded-full bg-emerald-500 mt-1"></div>
                      )}
                    </div>
                    <p className="text-sm text-gray-400 mb-2">
                      {notification.message}
                    </p>
                    <div className="flex items-center justify-between">
                      <span className="text-xs text-gray-400 flex items-center gap-1">
                        <Clock className="w-3 h-3" />
                        {notification.timestamp}
                      </span>
                      {notification.action && (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() =>
                            handleAction(notification)
                          }
                          className="text-emerald-500 hover:text-emerald-400 h-auto py-1 px-2"
                        >
                          {notification.action}
                        </Button>
                      )}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </ScrollArea>

        <div className="absolute bottom-0 left-0 right-0 p-4 border-t border-gray-800 bg-gray-900">
          <Button
            variant="outline"
            className="w-full border-gray-700 text-gray-200 hover:text-white"
          >
            Mark all as read
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}