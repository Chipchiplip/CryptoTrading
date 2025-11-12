import { useCallback, useEffect, useMemo, useState } from "react";
import {
  CheckCircle,
  XCircle,
  AlertTriangle,
  Info,
  Clock,
  RefreshCcw,
  X,
} from "lucide-react";
import { Badge } from "./ui/badge";
import { Button } from "./ui/button";
import { ScrollArea } from "./ui/scroll-area";
import { MarketApi } from "../api/market";
import {
  TradingApi,
  Trade,
  Order,
  PaginatedResponse,
} from "../api/trading";

type NotificationType = "success" | "error" | "warning" | "info";
type NotificationCategory = "market" | "trade" | "news";

type ApiResult<T> =
  | { ok: true; data: T }
  | { ok: false; error: string };

interface NewsArticle {
  title?: string;
  description?: string;
  link?: string;
  pubDate?: string;
  source_id?: string;
}

interface NewsApiResponse {
  results?: NewsArticle[];
}

const NEWS_API_KEY =
  import.meta.env.VITE_NEWSDATA_API_KEY ||
  "pub_8253901717e0482d900c89cc2200e128";
const NEWS_API_URL = `https://newsdata.io/api/1/crypto?apikey=${NEWS_API_KEY}&language=en`;
const MAX_NEWS_ITEMS = 5;
const READ_STORAGE_KEY = "notifications:readIds";

interface Notification {
  id: string;
  type: NotificationType;
  category: NotificationCategory;
  title: string;
  message: string;
  timestamp: Date;
  isRead: boolean;
  action?: string;
  actionLink?: string;
}

const fallbackNotifications: Notification[] = [
  {
    id: "market-fallback",
    type: "info",
    category: "market",
    title: "Waiting for market insight",
    message:
      "Real-time market metrics will appear here once the data service responds.",
    timestamp: new Date(),
    isRead: false,
  },
];

const formatCompactCurrency = (value?: number) => {
  if (!Number.isFinite(value ?? NaN)) return "$0";
  const abs = Math.abs(value!);
  if (abs >= 1e12) return `$${(value! / 1e12).toFixed(2)}T`;
  if (abs >= 1e9) return `$${(value! / 1e9).toFixed(2)}B`;
  if (abs >= 1e6) return `$${(value! / 1e6).toFixed(2)}M`;
  if (abs >= 1e3) return `$${(value! / 1e3).toFixed(2)}K`;
  return `$${(value ?? 0).toLocaleString("en-US", {
    maximumFractionDigits: 2,
  })}`;
};

const formatRelativeTime = (date: Date) => {
  const diff = Date.now() - date.getTime();
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return "just now";
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
};

const buildMarketNotifications = (stats: any): Notification[] => {
  const totalCap =
    Number(stats?.total_market_cap ?? stats?.totalMarketCap) || 0;
  const totalVolume =
    Number(stats?.total_volume ?? stats?.totalVolume) || 0;
  const capChange =
    Number(
      stats?.market_cap_change_percentage_24h ??
        stats?.marketCapChangePercentage24h,
    ) || 0;
  const dominanceBtc =
    Number(stats?.btc_dominance ?? stats?.btcDominance) || 0;
  const dominanceEth =
    Number(stats?.eth_dominance ?? stats?.ethDominance) || 0;
  const activeCryptos =
    Number(
      stats?.active_cryptocurrencies ?? stats?.activeCryptocurrencies,
    ) || 0;
  const collectedAt =
    stats?.collected_at_utc ??
    stats?.collectedAtUtc ??
    new Date().toISOString();
  const timestamp = new Date(collectedAt);

  const notifications: Notification[] = [];

  if (dominanceBtc > 0 || dominanceEth > 0 || activeCryptos > 0) {
    notifications.push({
      id: `market-dominance-${collectedAt}`,
      type: "info",
      category: "market",
      title: "Dominance snapshot",
      message: `BTC dominance ${dominanceBtc.toFixed(
        1,
      )}% | ETH ${dominanceEth.toFixed(
        1,
      )}% | Active assets ${activeCryptos.toLocaleString()}`,
      timestamp,
      isRead: false,
    });
  }

  return notifications;
};

const formatQuantity = (value: number) => {
  if (!Number.isFinite(value)) return "0";
  if (Math.abs(value) >= 1) {
    return value.toLocaleString("en-US", {
      maximumFractionDigits: 2,
    });
  }
  return value.toPrecision(3);
};

const buildTradeNotifications = (
  tradesResponse: PaginatedResponse<Trade>,
  orderLookup: Map<string, Order>,
): Notification[] => {
  const trades = tradesResponse?.data ?? [];
  return trades.map((trade) => {
    const order = orderLookup.get(String(trade.orderId));
    const symbol = order?.symbol ?? trade.symbol;
    const side = order?.side?.toUpperCase();
    const status = order?.status ?? "FILLED";
    const quantity =
      Number(trade.quantity) ||
      Number(order?.filled ?? order?.quantity ?? 0) ||
      0;
    const price = Number(trade.price ?? order?.price ?? 0);
    const total =
      price > 0 && quantity > 0 ? price * quantity : undefined;
    const ts = trade.createdAt
      ? new Date(trade.createdAt)
      : new Date(order?.updatedAt ?? order?.createdAt ?? Date.now());

    const verb =
      side === "SELL"
        ? "Sold"
        : side === "BUY"
          ? "Bought"
          : "Executed";

    const parts = [
      `${verb} ${formatQuantity(quantity)} ${symbol}`,
    ];
    if (price > 0) {
      parts.push(`at ${formatCompactCurrency(price)}`);
    }
    if (total && total > 0) {
      parts.push(`(total ${formatCompactCurrency(total)})`);
    }

    return {
      id: `trade-${trade.id}`,
      type: "success",
      category: "trade",
      title: `${symbol} ${side ?? "trade"} ${status.toLowerCase()}`,
      message: parts.join(" "),
      timestamp: ts,
      isRead: false,
      action: "View orders",
      actionLink: "orders",
    };
  });
};

const buildNewsNotifications = (articles: NewsArticle[]): Notification[] => {
  return articles.slice(0, MAX_NEWS_ITEMS).map((article, idx) => {
    const title = article.title?.trim() || "Market news";
    const description =
      article.description?.trim() || "Latest update from crypto markets.";
    const timestamp = article.pubDate
      ? new Date(article.pubDate)
      : new Date();
    const link = article.link;
    const source = article.source_id
      ? article.source_id.toUpperCase()
      : "NEWS";

    return {
      id: `news-${idx}-${title}`,
      type: "info",
      category: "news",
      title: `${source}: ${title}`,
      message: description,
      timestamp,
      isRead: false,
      action: link ? "Read" : undefined,
      actionLink: link,
    };
  });
};

const fetchCryptoNews = async (): Promise<ApiResult<NewsApiResponse>> => {
  try {
    const response = await fetch(NEWS_API_URL);
    if (!response.ok) {
      return { ok: false, error: `News API error ${response.status}` };
    }
    const data = (await response.json()) as NewsApiResponse;
    return { ok: true, data };
  } catch (error: any) {
    return {
      ok: false,
      error: error?.message || "Failed to load news",
    };
  }
};

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
  const [readIds, setReadIds] = useState<Set<string>>(() => {
    if (typeof window === "undefined") {
      return new Set();
    }
    try {
      const stored = window.localStorage.getItem(READ_STORAGE_KEY);
      if (!stored) return new Set();
      const parsed: unknown = JSON.parse(stored);
      if (Array.isArray(parsed)) {
        return new Set(parsed.filter((id) => typeof id === "string"));
      }
    } catch {
      // ignore corrupted storage
    }
    return new Set();
  });
  const [notifications, setNotifications] = useState<Notification[]>(
    fallbackNotifications,
  );
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const unreadCount = useMemo(
    () => notifications.filter((n) => !n.isRead).length,
    [notifications],
  );

  useEffect(() => {
    if (typeof window === "undefined") return;
    try {
      window.localStorage.setItem(
        READ_STORAGE_KEY,
        JSON.stringify(Array.from(readIds)),
      );
    } catch {
      // ignore storage errors
    }
  }, [readIds]);

  const applyReadState = useCallback(
    (items: Notification[]) =>
      items.map((notification) => ({
        ...notification,
        isRead: readIds.has(notification.id) || notification.isRead,
      })),
    [readIds],
  );

  const refreshNotifications = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [marketRes, tradesRes, ordersRes, newsRes] = await Promise.all([
        MarketApi.getMarketStats(),
        TradingApi.getTrades({ pageSize: 5 }),
        TradingApi.getOrders({ status: ["FILLED"], pageSize: 5 }),
        fetchCryptoNews(),
      ]);

      const next: Notification[] = [];
      const orderLookup = new Map<string, Order>();

      if (ordersRes.ok && ordersRes.data) {
        ordersRes.data.data.forEach((order) => {
          orderLookup.set(String(order.id), order);
        });
      } else if (!ordersRes.ok) {
        setError(
          (prev) =>
            prev ??
            "Unable to load your recently filled orders.",
        );
      }

      if (marketRes.ok && marketRes.data) {
        next.push(...buildMarketNotifications(marketRes.data));
      } else if (!marketRes.ok) {
        setError(
          (prev) =>
            prev ??
            "Unable to load market data. Please try again.",
        );
      }

      if (tradesRes.ok && tradesRes.data) {
        next.push(
          ...buildTradeNotifications(tradesRes.data, orderLookup),
        );
      } else if (!tradesRes.ok) {
        setError(
          (prev) =>
            prev ??
            "Unable to load the latest successful trades.",
        );
      }

      if (newsRes.ok && newsRes.data?.results?.length) {
        next.push(...buildNewsNotifications(newsRes.data.results));
      } else if (!newsRes.ok) {
        setError(
          (prev) =>
            prev ??
            "Unable to load crypto news feed.",
        );
      }

      next.sort(
        (a, b) => b.timestamp.getTime() - a.timestamp.getTime(),
      );

      const finalList =
        next.length > 0 ? applyReadState(next) : applyReadState(fallbackNotifications);
      setNotifications(finalList);
    } catch (err) {
      console.error("[NotificationsPanel] refresh error", err);
      setError("Unable to refresh notifications. Check your connection.");
      setNotifications(applyReadState(fallbackNotifications));
    } finally {
      setLoading(false);
    }
  }, [applyReadState]);

  useEffect(() => {
    if (open) {
      refreshNotifications();
    }
  }, [open, refreshNotifications]);

  const markAllAsRead = () => {
    setReadIds((prev) => {
      const next = new Set(prev);
      notifications.forEach((n) => next.add(n.id));
      return next;
    });
    setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
  };

  const markNotificationAsRead = (id: string) => {
    setReadIds((prev) => {
      if (prev.has(id)) return prev;
      const next = new Set(prev);
      next.add(id);
      return next;
    });
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
    );
  };

  const getIcon = (type: NotificationType) => {
    switch (type) {
      case "success":
        return <CheckCircle className="w-5 h-5 text-emerald-500" />;
      case "error":
        return <XCircle className="w-5 h-5 text-red-500" />;
      case "warning":
        return (
          <AlertTriangle className="w-5 h-5 text-yellow-500" />
        );
      case "info":
      default:
        return <Info className="w-5 h-5 text-blue-500" />;
    }
  };

  const handleAction = (notification: Notification) => {
    markNotificationAsRead(notification.id);
    if (notification.actionLink) {
      if (/^https?:\/\//i.test(notification.actionLink)) {
        window.open(notification.actionLink, "_blank", "noopener,noreferrer");
        return;
      }
      onNavigate(notification.actionLink);
      onOpenChange(false);
    }
  };

  if (!open) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-50 flex justify-end">
      <div
        className="absolute inset-0 bg-black/60"
        onClick={() => onOpenChange(false)}
        aria-label="Close notifications overlay"
      ></div>

      <aside className="relative h-full w-full max-w-md bg-gray-900 border-l border-gray-800 shadow-2xl flex flex-col text-white">
        <div className="flex items-center justify-between px-4 py-4 border-b border-gray-800">
          <div className="flex items-center gap-2">
            <span className="text-lg font-semibold">Notifications</span>
            {unreadCount > 0 && (
              <Badge className="bg-emerald-500 text-white">
                {unreadCount} new
              </Badge>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Button
              size="icon"
              variant="ghost"
              className="h-8 w-8 text-gray-400 hover:text-white"
              onClick={refreshNotifications}
              disabled={loading}
              aria-label="Refresh notifications"
            >
              <RefreshCcw
                className={`w-4 h-4 ${loading ? "animate-spin" : ""}`}
              />
            </Button>
            <Button
              size="icon"
              variant="ghost"
              className="h-8 w-8 text-gray-400 hover:text-white"
              onClick={() => onOpenChange(false)}
              aria-label="Close notifications"
            >
              <X className="w-4 h-4" />
            </Button>
          </div>
        </div>

        {error && (
          <div className="mx-4 mt-4 rounded-lg border border-red-500/40 bg-red-500/10 text-red-200 text-sm p-3">
            {error}
          </div>
        )}

        <div className="flex-1 min-h-0 mt-4 overflow-hidden">
          <ScrollArea
            className="h-full pr-2"
            type="always"
            scrollHideDelay={0}
          >
            <div className="space-y-2 pb-24">
              {loading && (
                <div className="p-4 text-center text-sm text-gray-400">
                  Loading market updates, trades, and news...
                </div>
              )}

              {!loading && notifications.length === 0 && (
                <div className="p-4 text-center text-sm text-gray-400">
                  You're all caught up. We'll post new market intel and
                  completed trade summaries here.
                </div>
              )}

              {!loading &&
                notifications.map((notification) => (
                  <div
                    key={notification.id}
                    className={`p-4 rounded-lg border transition-all duration-200 ${
                      notification.isRead
                        ? "bg-gray-800/40 border-gray-800 opacity-70"
                        : "bg-gray-800 border-gray-700"
                    }`}
                  >
                    <div className="flex items-start gap-3 mb-2">
                      {getIcon(notification.type)}
                      <div className="flex-1">
                        <div className="flex items-start justify-between mb-1">
                          <div>
                            <div className="flex items-center gap-2">
                              <h4 className="text-sm font-medium text-gray-100">
                                {notification.title}
                              </h4>
                              <Badge
                                variant="outline"
                                className="border-gray-700 text-gray-300"
                              >
                                {notification.category === "market"
                                  ? "Market"
                                  : notification.category === "trade"
                                    ? "Trade"
                                    : "News"}
                              </Badge>
                            </div>
                          </div>
                          {!notification.isRead && (
                            <div className="w-2 h-2 rounded-full bg-emerald-500 mt-1 transition-opacity duration-200"></div>
                          )}
                        </div>
                        <p className="text-sm text-gray-400 mb-2">
                          {notification.message}
                        </p>
                        <div className="flex items-center justify-between">
                          <span className="text-xs text-gray-400 flex items-center gap-1">
                            <Clock className="w-3 h-3" />
                            {formatRelativeTime(notification.timestamp)}
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
                    {!notification.isRead && (
                      <button
                        className="text-xs text-emerald-400 hover:text-emerald-300 transition-colors duration-200"
                        onClick={() =>
                          markNotificationAsRead(notification.id)
                        }
                      >
                        Mark as read
                      </button>
                    )}
                  </div>
                ))}
            </div>
          </ScrollArea>
        </div>

        <div className="p-4 border-t border-gray-800 bg-gray-900">
          <Button
            variant="default"
            className="w-full bg-emerald-500 text-black font-semibold tracking-wide hover:bg-emerald-400 disabled:bg-gray-700 disabled:text-gray-300"
            onClick={markAllAsRead}
            disabled={notifications.every((n) => n.isRead)}
          >
            Mark all as read
          </Button>
        </div>
      </aside>
    </div>
  );
}
