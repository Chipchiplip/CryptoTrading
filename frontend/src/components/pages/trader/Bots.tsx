import { useEffect, useMemo, useState } from 'react';
import { RefreshCw, Loader2, Play, Square, Trash2 } from 'lucide-react';
import { BotApi, BotSummary } from '../../../api/bots';
import { Button } from '../../ui/button';
import { Badge } from '../../ui/badge';

type StatusFilter = 'RUNNING' | 'STOPPED' | 'ALL';

const statusOptions: { label: string; value: StatusFilter }[] = [
  { label: 'Đang chạy', value: 'RUNNING' },
  { label: 'Đã dừng', value: 'STOPPED' },
  { label: 'Tất cả', value: 'ALL' },
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
      return 'Đang chạy';
    case 'STARTING':
      return 'Đang khởi động';
    case 'STOPPED':
      return 'Đã dừng';
    case 'PAUSED':
      return 'Tạm dừng';
    case 'ERROR':
      return 'Lỗi';
    case 'DRAFT':
      return 'Bản nháp';
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

const TraderBots = () => {
  const [bots, setBots] = useState<BotSummary[]>([]);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('RUNNING');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [actionBanner, setActionBanner] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [actionLoading, setActionLoading] = useState<string | null>(null);
  const pageSize = 20;

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
    fetchBots(1, statusFilter);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [statusFilter]);

  const activeCount = useMemo(
    () => bots.filter((bot) => bot.status === 'RUNNING').length,
    [bots]
  );

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
        action === 'start' ? 'đã được bật' : action === 'stop' ? 'đã dừng' : 'đã xóa';
      setActionBanner({ type: 'success', message: `Bot "${bot.name}" ${actionVerb}.` });
      await fetchBots(page, statusFilter);
    }
    setActionLoading(null);
  };

  const canPrev = page > 1;
  const canNext = page < totalPages;

  return (
    <div className="max-w-6xl mx-auto px-4 py-8 space-y-6">
      <div>
        <p className="text-sm text-emerald-400 uppercase tracking-widest mb-1">Bots</p>
        <h1 className="text-3xl font-semibold">Danh sách bot đang hoạt động</h1>
        <p className="text-gray-400 mt-2">
          Xem nhanh các bot bạn đã tạo từ AI hoặc thủ công, theo dõi trạng thái hoạt động và tín hiệu gần nhất.
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

      <div className="bg-gray-950 border border-gray-800 rounded-2xl p-4 flex flex-wrap gap-4 items-center justify-between">
        <div className="flex gap-3 items-center">
          <div className="text-sm text-gray-400">Trạng thái</div>
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
            Đang chạy: <span className="text-white font-medium">{activeCount}</span>
          </div>
        </div>
        <div className="flex gap-2">
          <Button
            variant="outline"
            className="border-gray-700 text-gray-200 hover:bg-gray-900"
            onClick={() => fetchBots(Math.max(page - 1, 1))}
            disabled={!canPrev || loading}
          >
            Trang trước
          </Button>
          <Button
            variant="outline"
            className="border-gray-700 text-gray-200 hover:bg-gray-900"
            onClick={() => fetchBots(Math.min(page + 1, totalPages))}
            disabled={!canNext || loading}
          >
            Trang sau
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
                <th className="px-6 py-3 font-semibold">Chiến lược</th>
                <th className="px-6 py-3 font-semibold">Trạng thái</th>
                <th className="px-6 py-3 font-semibold">Tín hiệu gần nhất</th>
                <th className="px-6 py-3 font-semibold">P&L</th>
                <th className="px-6 py-3 font-semibold">Lần chạy cuối</th>
                <th className="px-6 py-3 font-semibold text-center">Hành động</th>
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
                    Chưa có bot nào cho bộ lọc này. Vào AI Chat để tạo bot mới nhé!
                  </td>
                </tr>
              ) : (
                bots.map((bot) => {
                  const normalizedStatus = normalizeStatus(bot.status);
                  return (
                  <tr key={bot.id} className="hover:bg-gray-900/40 transition-colors">
                    <td className="px-6 py-4">
                      <div className="font-semibold text-white">{bot.name}</div>
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
                            {bot.runtime.lastExecutionAt ? `Lúc ${formatDate(bot.runtime.lastExecutionAt)}` : '—'}
                          </div>
                        </>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <div className={bot.runtime?.unrealizedPnl ?? 0 >= 0 ? 'text-emerald-400' : 'text-red-400'}>
                        {formatPnl(bot.runtime?.unrealizedPnl)}
                      </div>
                      <div className="text-xs text-gray-500">Realized: {formatPnl(bot.runtime?.realizedPnl)}</div>
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
                                Bật
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
                                Dừng
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
                                Xóa
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
            Trang {page}/{totalPages}
          </span>
          <span>Tổng {bots.length} bot trong trang này</span>
        </div>
      </div>
    </div>
  );
};

export default TraderBots;

