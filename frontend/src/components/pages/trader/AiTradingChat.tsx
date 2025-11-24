import { useEffect, useMemo, useRef, useState } from 'react';
import { Bot, Loader2, Send, User, AlertTriangle } from 'lucide-react';
import { AiApi, AiChatMessageRequest, AiChatBotSuggestion, AiTradeSuggestionDto } from '../../../api/ai';
import { getUserInfo } from '../../../api/http';
import { Button } from '../../ui/button';
import { Textarea } from '../../ui/textarea';
import { MarketApi } from '../../../api/market';
import { TradingApi } from '../../../api/trading';

type ChatRole = 'user' | 'ai';

interface ChatMessage {
  id: string;
  role: ChatRole;
  text: string;
  timestamp: string;
  tradeSuggestion?: AiTradeSuggestionDto;
  bots?: AiChatBotSuggestion[];
  error?: boolean;
}

type StatusBanner = { type: 'success' | 'error'; text: string } | null;

const createId = () =>
  typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
    ? crypto.randomUUID()
    : `${Date.now()}-${Math.random()}`;

const AiTradingChat = () => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [sessionId, setSessionId] = useState<string | undefined>(undefined);
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [placingOrderId, setPlacingOrderId] = useState<string | null>(null);
  const [status, setStatus] = useState<StatusBanner>(null);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const userInfo = useMemo(() => getUserInfo(), []);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const appendMessage = (msg: ChatMessage) => {
    setMessages((prev) => [...prev, msg]);
  };

  const handleSend = async () => {
    if (!userInfo) {
      setStatus({ type: 'error', text: 'Vui lòng đăng nhập lại để sử dụng AI Chat.' });
      return;
    }

    const trimmed = input.trim();
    if (!trimmed) return;

    const userMessage: ChatMessage = {
      id: createId(),
      role: 'user',
      text: trimmed,
      timestamp: new Date().toISOString(),
    };

    appendMessage(userMessage);
    setInput('');
    setLoading(true);
    setStatus(null);

    const payload: AiChatMessageRequest = {
      userId: userInfo.id,
      sessionId,
      message: trimmed,
    };

    const result = await AiApi.chat(payload);
    setLoading(false);

    if (!result.ok) {
      appendMessage({
        id: createId(),
        role: 'ai',
        text: result.error || 'Hệ thống AI đang bận, vui lòng thử lại sau.',
        timestamp: new Date().toISOString(),
        error: true,
      });
      setStatus({ type: 'error', text: result.error || 'Không thể gửi tin nhắn đến AI.' });
      return;
    }

    const aiMessage: ChatMessage = {
      id: createId(),
      role: 'ai',
      text: result.data.reply,
      timestamp: new Date().toISOString(),
      tradeSuggestion: result.data.tradeSuggestion ?? undefined,
      bots: result.data.bots,
    };
    setSessionId(result.data.sessionId);
    appendMessage(aiMessage);
  };

  const normalizeSymbol = (symbol: string) => {
    if (!symbol) return symbol;
    if (symbol.includes('/')) return symbol.toUpperCase();
    if (symbol.includes('-')) return symbol.toUpperCase().replace('-', '/');
    if (symbol.toUpperCase().endsWith('USDT')) {
      const base = symbol.slice(0, -4);
      return `${base.toUpperCase()}/USDT`;
    }
    return symbol.toUpperCase();
  };

  const extractBaseAsset = (symbol: string) => {
    if (!symbol) return '';
    if (symbol.includes('/')) return symbol.split('/')[0];
    if (symbol.includes('-')) return symbol.split('-')[0];
    const quotes = ['USDT', 'USD', 'USDC', 'BTC', 'ETH'];
    for (const quote of quotes) {
      if (symbol.toUpperCase().endsWith(quote)) {
        return symbol.slice(0, -quote.length);
      }
    }
    return symbol;
  };

  const handlePlaceOrder = async (suggestion: AiTradeSuggestionDto, messageId: string) => {
    try {
      setPlacingOrderId(messageId);
      setStatus(null);

      const normalizedSymbol = normalizeSymbol(suggestion.symbol);
      const baseAsset = extractBaseAsset(normalizedSymbol);

      const priceResult = await MarketApi.getCryptocurrency(baseAsset);
      if (!priceResult.ok || !priceResult.data || !priceResult.data.currentPrice) {
        throw new Error('Không thể lấy giá thị trường hiện tại.');
      }

      const price = priceResult.data.currentPrice;
      const quantity = Number((suggestion.amountUsdt / price).toFixed(6));
      if (!quantity || quantity <= 0) {
        throw new Error('Số lượng giao dịch không hợp lệ.');
      }

      const orderResult = await TradingApi.placeOrder({
        symbol: normalizedSymbol,
        side: suggestion.decision === 'SELL' ? 'SELL' : 'BUY',
        type: 'MARKET',
        quantity,
      });

      if (!orderResult.ok) {
        throw new Error(orderResult.error);
      }

      setStatus({
        type: 'success',
        text: `Đã đặt lệnh ${suggestion.decision} ${normalizedSymbol} thành công.`,
      });
    } catch (error: any) {
      setStatus({
        type: 'error',
        text: error?.message || 'Đặt lệnh thất bại. Vui lòng thử lại.',
      });
    } finally {
      setPlacingOrderId(null);
    }
  };

  const handleApplyBotSuggestion = async (bot: AiChatBotSuggestion) => {
    if (!userInfo) return;
    setStatus(null);
    const result = await AiApi.applyBotSuggestion(bot.suggestionId, userInfo.id);
    if (!result.ok) {
      setStatus({ type: 'error', text: result.error });
      return;
    }
    setStatus({
      type: 'success',
      text: `Đã tạo bot "${bot.name}". Bot đang ở trạng thái bản nháp, hãy vào trang Bots và nhấn "Bật" để kích hoạt.`,
    });
  };

  return (
    <div className="min-h-screen bg-black text-white">
      <div className="max-w-5xl mx-auto px-4 py-8 space-y-6">
        <div>
          <p className="text-sm text-emerald-400 uppercase tracking-widest mb-1">AI Trading</p>
          <h1 className="text-3xl font-semibold">Trò chuyện với AI Trading Assistant</h1>
          <p className="text-gray-400 mt-2">
            AI sẽ đọc kế hoạch giao dịch, danh mục và thị trường trước khi trả lời, đồng thời cung cấp gợi ý lệnh
            (nếu có signal rõ ràng).
          </p>
        </div>

        {status && (
          <div
            className={`rounded-xl border px-4 py-3 flex items-center gap-3 ${
              status.type === 'success' ? 'border-emerald-500/50 bg-emerald-500/10 text-emerald-200' : 'border-red-500/50 bg-red-500/10 text-red-200'
            }`}
          >
            <AlertTriangle className="w-4 h-4" />
            <span>{status.text}</span>
          </div>
        )}

        <div className="bg-gray-950 border border-gray-800 rounded-2xl p-4 space-y-4">
          <div>
            <label className="text-sm text-gray-400 block mb-2">Tin nhắn</label>
            <Textarea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Ví dụ: “Dạo này BTC sao rồi?”, “Nên mua coin nào?”, “Scalp BTC được không?” – khi cần lên plan hoặc tạo bot thì hãy gửi giúp mình vốn, risk mode, cặp và timeframe nhé."
              className="bg-gray-900 border-gray-800 min-h-[80px]"
            />
          </div>
          <div className="flex items-center justify-between">
            <p className="text-sm text-gray-500">AI chỉ trả về lệnh spot và tuân thủ giới hạn vốn đã cấu hình.</p>
            <Button
              onClick={handleSend}
              disabled={loading || !input.trim()}
              className="bg-emerald-500 text-black hover:bg-emerald-400"
            >
              {loading ? <Loader2 className="w-4 h-4 animate-spin mr-2" /> : <Send className="w-4 h-4 mr-2" />}
              Gửi
            </Button>
          </div>
        </div>

        <div className="bg-gray-950 border border-gray-800 rounded-2xl p-4 h-[60vh] overflow-y-auto">
          {messages.length === 0 && (
            <div className="h-full flex flex-col items-center justify-center text-center text-gray-500 space-y-2">
              <Bot className="w-12 h-12 text-emerald-500" />
              <p>Bạn chưa có cuộc trò chuyện nào. Gửi câu hỏi để bắt đầu!</p>
            </div>
          )}

          <div className="space-y-4">
            {messages.map((msg) => (
              <div key={msg.id} className="flex flex-col gap-2">
                <div
                  className={`flex items-start gap-3 ${
                    msg.role === 'user' ? 'justify-end text-right' : 'text-left'
                  }`}
                >
                  {msg.role === 'ai' && (
                    <div className="w-9 h-9 rounded-full bg-emerald-500/20 flex items-center justify-center text-emerald-400">
                      <Bot className="w-5 h-5" />
                    </div>
                  )}

                  <div
                    className={`max-w-[80%] rounded-2xl px-4 py-3 ${
                      msg.role === 'user'
                        ? 'bg-emerald-500 text-black ml-auto'
                        : msg.error
                        ? 'bg-red-500/20 border border-red-500/40 text-red-100'
                        : 'bg-gray-900 border border-gray-800 text-gray-100'
                    }`}
                  >
                    <p className="whitespace-pre-line text-sm">{msg.text}</p>
                    <p className="text-xs mt-2 opacity-70">
                      {new Date(msg.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </p>
                  </div>

                  {msg.role === 'user' && (
                    <div className="w-9 h-9 rounded-full bg-gray-800 flex items-center justify-center text-gray-200 ml-3">
                      <User className="w-5 h-5" />
                    </div>
                  )}
                </div>

                {msg.tradeSuggestion && msg.tradeSuggestion.decision !== 'NO_TRADE' && (
                  <div className="ml-0 md:ml-12 bg-emerald-500/10 border border-emerald-500/40 rounded-2xl p-4 text-sm text-emerald-100 space-y-2">
                    <div className="flex items-center justify-between">
                      <div className="font-semibold uppercase tracking-wide text-emerald-300">
                        Gợi ý: {msg.tradeSuggestion.decision} {normalizeSymbol(msg.tradeSuggestion.symbol)}
                      </div>
                      <div className="text-xs text-gray-400">
                        Tự tin {(msg.tradeSuggestion.confidence * 100).toFixed(0)}%
                      </div>
                    </div>
                    <div className="grid sm:grid-cols-3 gap-3 text-gray-200">
                      <div>
                        <p className="text-xs text-gray-400">Vốn đề xuất</p>
                        <p className="text-base font-semibold">${msg.tradeSuggestion.amountUsdt.toLocaleString()}</p>
                      </div>
                      <div>
                        <p className="text-xs text-gray-400">Kỳ vọng</p>
                        <p className="text-base font-semibold">
                          {msg.tradeSuggestion.expectedReturnPct
                            ? `${msg.tradeSuggestion.expectedReturnPct.toFixed(1)}%`
                            : 'N/A'}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-gray-400">Thời gian nắm giữ</p>
                        <p className="text-base font-semibold capitalize">{msg.tradeSuggestion.timeHorizon}</p>
                      </div>
                    </div>
                    <Button
                      variant="secondary"
                      disabled={placingOrderId === msg.id}
                      onClick={() => handlePlaceOrder(msg.tradeSuggestion!, msg.id)}
                      className="bg-emerald-500 text-black hover:bg-emerald-400 mt-2"
                    >
                      {placingOrderId === msg.id ? (
                        <>
                          <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                          Đang đặt lệnh...
                        </>
                      ) : (
                        'Đặt lệnh theo gợi ý này'
                      )}
                    </Button>
                  </div>
                )}
                {msg.bots && msg.bots.length > 0 && (
                  <div className="ml-0 md:ml-12 space-y-3">
                    {msg.bots.map((bot) => (
                      <div
                        key={bot.suggestionId}
                        className="bg-gray-900/80 border border-gray-800 rounded-2xl p-4 text-sm space-y-2"
                      >
                        <div className="flex items-center justify-between">
                          <div>
                            <p className="text-base font-semibold text-white">{bot.name}</p>
                            <p className="text-xs text-gray-400">
                              {bot.strategyType} • {bot.riskMode} • {bot.timeHorizon}
                            </p>
                          </div>
                          <span className="text-emerald-400 text-xs">Symbols: {bot.symbols.join(', ')}</span>
                        </div>
                        <div className="grid sm:grid-cols-2 gap-3">
                          <div>
                            <p className="text-xs text-gray-500">Vốn mỗi lệnh</p>
                            <p className="text-lg font-semibold">${bot.maxCapitalPerTrade.toLocaleString()}</p>
                          </div>
                          <div>
                            <p className="text-xs text-gray-500">Tối đa mỗi ngày</p>
                            <p className="text-lg font-semibold">${bot.maxDailyExposure.toLocaleString()}</p>
                          </div>
                        </div>
                        {bot.riskNote && <p className="text-xs text-amber-300">{bot.riskNote}</p>}
                        <Button
                          variant="outline"
                          className="border-emerald-500 text-emerald-300 hover:bg-emerald-500/10"
                          onClick={() => handleApplyBotSuggestion(bot)}
                        >
                          Tạo bot từ gợi ý này
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            ))}
            <div ref={messagesEndRef} />
          </div>
        </div>
      </div>
    </div>
  );
};

export default AiTradingChat;

