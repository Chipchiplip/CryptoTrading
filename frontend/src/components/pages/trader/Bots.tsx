import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { RefreshCw, Loader2, Play, Square, Trash2, TrendingUp, TrendingDown, DollarSign } from 'lucide-react';
import { BotApi, BotSummary } from '../../../api/bots';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';
import { useSubscriptionPlan } from '../../../hooks/useSubscriptionPlan';
import { PremiumFeatureGate } from '../../PremiumFeatureGate';

type StatusFilter = 'RUNNING' | 'STOPPED' | 'ALL';

const statusOptions: { label: string; value: StatusFilter }[] = [
  { label: 'Running', value: 'RUNNING' },
  { label: 'Stopped', value: 'STOPPED' },
  { label: 'All', value: 'ALL' },
];

const statusClasses: Record<string, string> = {
  RUNNING: 'bg-emerald-500/20 text-emerald-200',
  STARTING: 'bg-amber-500/20 text-amber-200',
  STOPPED: 'bg-gray-600/20 text-gray-200',
  ERROR: 'bg-red-500/20 text-red-200',
  PAUSED: 'bg-blue-500/20 text-blue-200',
  DRAFT: 'bg-gray-700/40 text-gray-200',
};

const normalizeStatus = (status: string) => status?.toUpperCase?.() ?? status;

const statusLabel = (status: string) => {
  switch (normalizeStatus(status)) {
    case 'RUNNING':
      return 'Running';
    case 'STARTING':
      return 'Starting';
    case 'STOPPED':
      return 'Stopped';
    case 'PAUSED':
      return 'Paused';
    case 'ERROR':
      return 'Error';
    case 'DRAFT':
      return 'Draft';
    default:
      return status;
  }
};

const formatDate = (value?: string) => {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString();
};

const formatPair = (bot: BotSummary) => `${bot.baseAsset}/${bot.quoteAsset}`;

const formatPnl = (value?: number) => {
  if (value === undefined || value === null) return '—';
  const formatted = value.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return `${value >= 0 ? '+' : ''}${formatted} USDT`;
};

const getTotalPnl = (bot: BotSummary) => {
  const realized = bot.runtime?.realizedPnl ?? 0;
  const unrealized = bot.runtime?.unrealizedPnl ?? 0;
  return realized + unrealized;
};

const formatTotalPnl = (bot: BotSummary) => {
  const total = getTotalPnl(bot);
  return formatPnl(total);
};

const TraderBots = () => {
  const navigate = useNavigate();
  const [bots, setBots] = useState<BotSummary[]>([]);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('RUNNING');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [actionBanner, setActionBanner] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const pageSize = 20;
  const { isPremium, loading: planLoading } = useSubscriptionPlan();

  const fetchBots = async (targetPage = page, filter = statusFilter) => {
    setLoading(true);
    setError(null);
    // Giữ lại banner hành động khi chỉ làm mới dữ liệu
    const status = filter === 'ALL' ? undefined : filter;
    const result = await BotApi.getBots({ status, page: targetPage, pageSize });

    if (!result.ok) {
      setError(result.error);
      setBots([]);
    } else {
      setBots(result.data.data);
      setTotalPages(result.data.totalPages);
      setPage(result.data.page);
    }
    setLoading(false);
  };

  useEffect(() => {
    if (!isPremium) {
      setBots([]);
      return;
    }
    fetchBots(1, statusFilter);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter, isPremium]);

  const activeCount = useMemo(
    () => bots.filter((bot) => normalizeStatus(bot.status) === 'RUNNING').length,
    [bots]
  );

  const totalPnl = useMemo(() => {
    return bots.reduce((sum, bot) => {
      const realized = bot.runtime?.realizedPnl ?? 0;
      const unrealized = bot.runtime?.unrealizedPnl ?? 0;
      return sum + realized + unrealized;
    }, 0);
  }, [bots]);

  const totalFees = useMemo(() => {
    return bots.reduce((sum, bot) => sum + (bot.runtime?.totalFees ?? 0), 0);
  }, [bots]);

  const handleRefresh = () => fetchBots();
  const handleChangeStatus = (value: StatusFilter) => setStatusFilter(value);

  const handleBotAction = async (bot: BotSummary, action: 'start' | 'stop' | 'delete') => {
    const key = `${action}-${bot.id}`;
    setActionLoading(key);
    setActionBanner(null);
    let result;
    if (action === 'start') {
      result = await BotApi.startBot(bot.id);
    } else if (action === 'stop') {
      result = await BotApi.stopBot(bot.id);
    } else {
      result = await BotApi.deleteBot(bot.id);
    }

    if (!result.ok) {
      setActionBanner({ type: 'error', message: result.error });
    } else {
      const actionVerb =
        action === 'start' ? 'has been started' : action === 'stop' ? 'has been stopped' : 'has been deleted';
      setActionBanner({ type: 'success', message: `Bot "${bot.name}" ${actionVerb}.` });
      await fetchBots(page, statusFilter);
    }
    setActionLoading(null);
  };

  const canPrev = page > 1;
  const canNext = page < totalPages;

  if (planLoading) {
    return (
      <div className="min-h-screen bg-black text-white flex items-center justify-center">
        <Loader2 className="w-10 h-10 text-emerald-500 animate-spin" />
      </div>
    );
  }

  if (!isPremium) {
    return (
      <PremiumFeatureGate
        featureName="AI Bots"
        description="Tính năng bot tự động chỉ dành cho gói Premium để giao dịch liên tục và nhận phân tích nâng cao."
        helperText="Nâng cấp Premium để tạo, bật/tắt và quản lý bot AI với chiến lược độc quyền."
      />
    );
  }

  return (
    <div className="max-w-6xl mx-auto px-4 py-8 space-y-6">
      <div>
        <p className="text-sm text-emerald-400 uppercase tracking-widest mb-1">Bots</p>
        <h1 className="text-3xl font-semibold">Active Bots List</h1>
        <p className="text-gray-400 mt-2">
          Quick view of bots you've created from AI or manually, track activity status and latest signals.
        </p>
      </div>

      {actionBanner && (
        <div
          className={`border rounded-xl px-4 py-3 text-sm ${
            actionBanner.type === 'success'
              ? 'border-emerald-500/40 bg-emerald-500/10 text-emerald-100'
              : 'border-red-500/40 bg-red-500/10 text-red-100'
          }`}
        >
          {actionBanner.message}
        </div>
      )}

      {/* Summary Cards */}
      {bots.length > 0 && (
        <div className="grid md:grid-cols-3 gap-4">
          <div className="bg-gray-950 border border-gray-800 rounded-2xl p-6">
            <div className="flex items-center gap-2 mb-2">
              <DollarSign className="w-5 h-5 text-emerald-500" />
              <div className="text-gray-400 text-sm">Total P&L (All Bots)</div>
            </div>
            <div className={`text-2xl font-bold ${totalPnl >= 0 ? 'text-emerald-500' : 'text-red-500'}`}>
              {totalPnl >= 0 ? '+' : ''}{totalPnl.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} USDT
            </div>
            <div className="text-xs text-gray-500 mt-1">
              {bots.length} bot{bots.length !== 1 ? 's' : ''}
            </div>
          </div>

          <div className="bg-gray-950 border border-gray-800 rounded-2xl p-6">
            <div className="flex items-center gap-2 mb-2">
              <TrendingUp className="w-5 h-5 text-blue-500" />
              <div className="text-gray-400 text-sm">Active Bots</div>
            </div>
            <div className="text-2xl font-bold text-white">
              {activeCount}
            </div>
            <div className="text-xs text-gray-500 mt-1">
              {bots.length - activeCount} stopped
            </div>
          </div>

          <div className="bg-gray-950 border border-gray-800 rounded-2xl p-6">
            <div className="flex items-center gap-2 mb-2">
              <DollarSign className="w-5 h-5 text-yellow-500" />
              <div className="text-gray-400 text-sm">Total Fees</div>
            </div>
            <div className="text-2xl font-bold text-white">
              {totalFees.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} USDT
            </div>
            <div className="text-xs text-gray-500 mt-1">
              Across all bots
            </div>
          </div>
        </div>
      )}

      <div className="bg-gray-950 border border-gray-800 rounded-2xl p-4 flex flex-wrap gap-4 items-center justify-between">
        <div className="flex gap-3 items-center">
          <div className="text-sm text-gray-400">Status</div>
          <div className="flex gap-2">
            {statusOptions.map((option) => (
              <button
                key={option.value}
                onClick={() => handleChangeStatus(option.value)}
                className={`px-3 py-1.5 rounded-lg text-sm transition-colors ${
                  statusFilter === option.value
                    ? 'bg-emerald-500 text-black'
                    : 'bg-gray-900 text-gray-300 hover:bg-gray-800'
                }`}
              >
                {option.label}
              </button>
            ))}
          </div>
          <div className="text-sm text-gray-500">
            Running: <span className="text-white font-medium">{activeCount}</span>
          </div>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            className="border-gray-700 text-gray-200 hover:bg-gray-900"
            onClick={() => fetchBots(Math.max(page - 1, 1))}
            disabled={!canPrev || loading}
          >
            Previous
          </Button>
          <Button
            variant="outline"
            className="border-gray-700 text-gray-200 hover:bg-gray-900"
            onClick={() => fetchBots(Math.min(page + 1, totalPages))}
            disabled={!canNext || loading}
          >
            Next
          </Button>
          <Button onClick={handleRefresh} disabled={loading} className="bg-emerald-500 text-black hover:bg-emerald-400">
            {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <RefreshCw className="w-4 h-4" />}
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-500/10 border border-red-500/40 text-red-200 rounded-2xl p-4 text-sm">
          {error}
        </div>
      )}

      <div className="bg-gray-950 border border-gray-800 rounded-2xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-gray-900 text-gray-400 uppercase text-xs tracking-wider">
              <tr>
                <th className="px-6 py-3 font-semibold">Bot</th>
                <th className="px-6 py-3 font-semibold">Strategy</th>
                <th className="px-6 py-3 font-semibold">Status</th>
                <th className="px-6 py-3 font-semibold">Latest Signal</th>
                <th className="px-6 py-3 font-semibold">P&L</th>
                <th className="px-6 py-3 font-semibold">Last Run</th>
                <th className="px-6 py-3 font-semibold text-center">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-900">
              {loading ? (
                <tr>
                  <td colSpan={7} className="text-center py-10 text-gray-400">
                    <div className="flex justify-center">
                      <Loader2 className="w-5 h-5 animate-spin" />
                    </div>
                  </td>
                </tr>
              ) : bots.length === 0 ? (
                <tr>
                  <td colSpan={7} className="text-center py-10 text-gray-500">
                    No bots found for this filter. Go to AI Chat to create a new bot!
                  </td>
                </tr>
              ) : (
                bots.map((bot) => {
                  const normalizedStatus = normalizeStatus(bot.status);
                  return (
                  <tr key={bot.id} className="hover:bg-gray-900/40 transition-colors">
                    <td className="px-6 py-4">
                      <div 
                        className="font-semibold text-white cursor-pointer hover:text-emerald-400 transition-colors"
                        onClick={() => navigate(`/bot-detail?id=${bot.id}`)}
                      >
                        {bot.name}
                      </div>
                      <div className="text-xs text-gray-500 mt-1">{formatPair(bot)}</div>
                    </td>
                    <td className="px-6 py-4">
                      <div className="text-gray-200">{bot.strategy?.displayName || bot.strategy?.key}</div>
                      <div className="text-xs text-gray-500">v{bot.strategy?.version}</div>
                    </td>
                    <td className="px-6 py-4">
                      <Badge className={`${statusClasses[normalizedStatus] ?? 'bg-gray-700'} border-0`}>
                        {statusLabel(bot.status)}
                      </Badge>
                    </td>
                    <td className="px-6 py-4 text-gray-200">
                      {bot.runtime?.lastSignal ? (
                        <>
                          <div>{bot.runtime.lastSignal}</div>
                          <div className="text-xs text-gray-500 mt-1">
                            {bot.runtime.lastExecutionAt ? `At ${formatDate(bot.runtime.lastExecutionAt)}` : '—'}
                          </div>
                        </>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <div className="space-y-1">
                        {/* Total P&L - Most prominent */}
                        <div className={`flex items-center gap-1 font-semibold text-lg ${
                          getTotalPnl(bot) >= 0 ? 'text-emerald-400' : 'text-red-400'
                        }`}>
                          {getTotalPnl(bot) >= 0 ? (
                            <TrendingUp className="w-4 h-4" />
                          ) : (
                            <TrendingDown className="w-4 h-4" />
                          )}
                          {formatTotalPnl(bot)}
                        </div>
                        {/* Breakdown */}
                        <div className="text-xs space-y-0.5">
                          <div className="text-gray-400">
                            Realized: <span className={bot.runtime?.realizedPnl ?? 0 >= 0 ? 'text-emerald-400' : 'text-red-400'}>
                              {formatPnl(bot.runtime?.realizedPnl)}
                            </span>
                          </div>
                          <div className="text-gray-400">
                            Unrealized: <span className={bot.runtime?.unrealizedPnl ?? 0 >= 0 ? 'text-emerald-400' : 'text-red-400'}>
                              {formatPnl(bot.runtime?.unrealizedPnl)}
                            </span>
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-gray-400">{formatDate(bot.runtime?.lastExecutionAt)}</td>
                    <td className="px-6 py-4">
                      <div className="flex flex-wrap gap-2 justify-center">
                        {['DRAFT', 'STOPPED'].includes(normalizedStatus) && (
                          <Button
                            size="sm"
                            className="bg-emerald-500 text-black hover:bg-emerald-400"
                            onClick={() => handleBotAction(bot, 'start')}
                            disabled={actionLoading === `start-${bot.id}`}
                          >
                            {actionLoading === `start-${bot.id}` ? (
                              <Loader2 className="w-4 h-4 animate-spin" />
                            ) : (
                              <>
                                <Play className="w-4 h-4 mr-1" />
                                Start
                              </>
                            )}
                          </Button>
                        )}
                        {['RUNNING', 'STARTING'].includes(normalizedStatus) && (
                          <Button
                            size="sm"
                            variant="outline"
                            className="border-amber-500/60 text-amber-200 hover:bg-amber-500/10"
                            onClick={() => handleBotAction(bot, 'stop')}
                            disabled={actionLoading === `stop-${bot.id}`}
                          >
                            {actionLoading === `stop-${bot.id}` ? (
                              <Loader2 className="w-4 h-4 animate-spin" />
                            ) : (
                              <>
                                <Square className="w-4 h-4 mr-1" />
                                Stop
                              </>
                            )}
                          </Button>
                        )}
                        {['DRAFT', 'STOPPED'].includes(normalizedStatus) && (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="text-red-300 hover:bg-red-500/10"
                            onClick={() => handleBotAction(bot, 'delete')}
                            disabled={actionLoading === `delete-${bot.id}`}
                          >
                            {actionLoading === `delete-${bot.id}` ? (
                              <Loader2 className="w-4 h-4 animate-spin" />
                            ) : (
                              <>
                                <Trash2 className="w-4 h-4 mr-1" />
                                Delete
                              </>
                            )}
                          </Button>
                        )}
                        {!['DRAFT', 'STOPPED', 'RUNNING', 'STARTING'].includes(normalizedStatus) && (
                          <span className="text-xs text-gray-500">—</span>
                        )}
                      </div>
                    </td>
                  </tr>
                )})
              )}
            </tbody>
          </table>
        </div>
        <div className="border-t border-gray-900 px-6 py-3 text-xs text-gray-500 flex justify-between">
          <span>
            Page {page}/{totalPages}
          </span>
          <span>Total {bots.length} bots on this page</span>
        </div>
      </div>
    </div>
  );
};

export default TraderBots;
