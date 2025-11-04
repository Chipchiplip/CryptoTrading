import React, { useState, useEffect, useLayoutEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { TrendingUp, TrendingDown, Info, Loader2 } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Alert, AlertDescription } from '../../ui/alert';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '../../ui/dialog';
import { Badge } from '../../ui/badge';
import { CoinIcon } from '../../ui/CoinIcon';
import { createChart, ColorType, IChartApi, ISeriesApi, CandlestickSeries } from 'lightweight-charts';
import * as signalR from '@microsoft/signalr';
import { TradingApi, OrderBook, TradingBalances } from '../../../api/trading';
import { MarketApi, Crypto, CandlestickData } from '../../../api/market';

interface TradeProps {
  onNavigate?: (page: string) => void;
}

type Timeframe = '1D' | '7D' | '1M' | '3M' | '1Y';

const timeframeDays: Record<Timeframe, number> = {
  '1D': 1,
  '7D': 7,
  '1M': 30,
  '3M': 90,
  '1Y': 365,
};

export default function Trade({ onNavigate }: TradeProps) {
  const [searchParams, setSearchParams] = useSearchParams();
  const urlPair = searchParams.get('pair');
  const [selectedPair, setSelectedPair] = useState(urlPair || 'BTC/USDT');
  const [side, setSide] = useState<'buy' | 'sell'>('buy');
  const [orderType, setOrderType] = useState<'MARKET' | 'LIMIT'>('MARKET');
  const [buyAmount, setBuyAmount] = useState('');
  const [useAmount, setUseAmount] = useState('');
  const [sellAmount, setSellAmount] = useState('');
  const [receiveAmount, setReceiveAmount] = useState('');
  const [limitPrice, setLimitPrice] = useState('');
  const [showPreview, setShowPreview] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingChart, setLoadingChart] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [timeframe, setTimeframe] = useState<Timeframe>('1D');
  
  const [pairs, setPairs] = useState<Crypto[]>([]);
  const [orderBook, setOrderBook] = useState<OrderBook | null>(null);
  const [balances, setBalances] = useState<TradingBalances | null>(null);
  const [selectedCoin, setSelectedCoin] = useState<Crypto | null>(null);
  
  // Chart refs and state
  const chartContainerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null);
  const [candles, setCandles] = useState<CandlestickData[]>([]);
  const signalRConnectionRef = useRef<signalR.HubConnection | null>(null);
  
  // Map timeframe to chart interval
  const intervalMap: Record<Timeframe, string> = {
    '1D': '1h',
    '7D': '4h',
    '1M': '1d',
    '3M': '1d',
    '1Y': '1d'
  };

  // Update selectedPair when URL param changes
  useEffect(() => {
    const urlPair = searchParams.get('pair');
    if (urlPair) {
      setSelectedPair(urlPair);
    }
  }, [searchParams]);

  // Fetch cryptocurrencies and find selected coin
  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);
      
      try {
        const cryptosRes = await MarketApi.getCryptocurrencies();
        if (!cryptosRes.ok) {
          setError(cryptosRes.error);
          setLoading(false);
          return;
        }
        
        // Normalize data
        const normalized = cryptosRes.data.map((coin: any) => ({
          id: coin.id || coin.Id || '',
          symbol: String(coin.symbol || coin.Symbol || '').toUpperCase(),
          name: coin.name || coin.Name || '',
          currentPrice: Number(coin.current_price ?? coin.currentPrice ?? coin.CurrentPrice ?? 0),
          priceChange24h: Number(coin.price_change_24h ?? coin.priceChange24h ?? coin.PriceChange24h ?? 0),
          priceChangePercentage24h: Number(coin.price_change_percentage_24h ?? coin.priceChangePercentage24h ?? coin.PriceChangePercentage24h ?? 0),
          marketCap: Number(coin.market_cap ?? coin.marketCap ?? coin.MarketCap ?? 0),
          totalVolume: Number(coin.total_volume ?? coin.totalVolume ?? coin.TotalVolume ?? 0),
          image: coin.image || coin.Image || coin.image_url || coin.imageUrl || null,
        }));
        
        setPairs(normalized);
        
        // Find selected coin by symbol
        const baseSymbol = selectedPair.split('/')[0].toUpperCase();
        const coin = normalized.find(c => c.symbol.toUpperCase() === baseSymbol);
        if (coin) {
          setSelectedCoin(coin);
        }
        
        // Fetch balances
        const balancesRes = await TradingApi.getBalances();
        if (balancesRes.ok) {
          setBalances(balancesRes.data);
        }
        
        setLoading(false);
      } catch (e: any) {
        setError(e?.message || 'Failed to load trading data');
        setLoading(false);
      }
    };
    
    fetchData();
  }, []);

  // Update selected coin when pair changes
  useEffect(() => {
    if (pairs.length > 0) {
      const baseSymbol = selectedPair.split('/')[0].toUpperCase();
      const coin = pairs.find(c => c.symbol.toUpperCase() === baseSymbol);
      if (coin) {
        setSelectedCoin(coin);
      }
    }
  }, [selectedPair, pairs]);

  // Reset form fields when switching between buy and sell
  useEffect(() => {
    setBuyAmount('');
    setUseAmount('');
    setSellAmount('');
    setReceiveAmount('');
    setLimitPrice('');
  }, [side]);

  // Initialize chart - only once on mount (exact copy from ChartTest)
  useLayoutEffect(() => {
    console.log('[Chart] useLayoutEffect triggered, chartContainerRef.current:', !!chartContainerRef.current);
    
    let resizeHandler: (() => void) | null = null;
    let timeoutId: ReturnType<typeof setTimeout> | null = null;

    // Wait for container to have proper size
    const initChart = () => {
      if (!chartContainerRef.current) {
        console.log('[Chart] Container ref is null in initChart, retrying...');
        setTimeout(initChart, 100);
        return;
      }
      
      // Don't reinitialize if chart already exists
      if (chartRef.current) {
        console.log('[Chart] Chart already initialized, skipping');
        return;
      }

      const container = chartContainerRef.current;
      const rect = container.getBoundingClientRect();
      const width = rect.width || container.clientWidth;
      const height = rect.height || container.clientHeight || 400;

      console.log('[Chart] Container size check - width:', width, 'height:', height);

      if (width === 0 || height === 0) {
        console.log('[Chart] Container has no size, retrying in 100ms...');
        setTimeout(initChart, 100);
        return;
      }

      const finalWidth = width > 0 ? width : 800;
      const finalHeight = height > 0 ? height : 400;

      console.log('[Chart] Initializing chart with size:', finalWidth, 'x', finalHeight);

      try {
        // Don't clear container - let createChart handle it
        // createChart will automatically clear the container

        // Create chart with minimal config first
        const chart = createChart(container, {
          layout: {
            background: { type: ColorType.Solid, color: '#000000' },
            textColor: '#ffffff',
          },
          grid: {
            vertLines: { color: '#1a1a1a' },
            horzLines: { color: '#1a1a1a' },
          },
          width: finalWidth,
          height: finalHeight,
          timeScale: {
            timeVisible: true,
            secondsVisible: false,
          },
          rightPriceScale: {
            borderColor: '#1a1a1a',
          },
        });

        console.log('[Chart] Chart created, adding series...');
        console.log('[Chart] Chart instance:', chart);
        console.log('[Chart] Container children:', container.children.length);

        // Add candlestick series - Use CandlestickSeries class for version 5.x
        // In v5.x, you need to import CandlestickSeries and use it
        let candlestickSeries: ISeriesApi<'Candlestick'>;
        
        try {
          console.log('[Chart] CandlestickSeries:', CandlestickSeries);
          candlestickSeries = chart.addSeries(CandlestickSeries, {
            upColor: '#22c55e',
            downColor: '#ef4444',
            borderVisible: false,
            wickUpColor: '#22c55e',
            wickDownColor: '#ef4444',
          }) as ISeriesApi<'Candlestick'>;
          
          console.log('[Chart] Series created successfully:', candlestickSeries);
        } catch (err: any) {
          console.error('[Chart] Error creating series:', err);
          console.error('[Chart] Error message:', err?.message);
          console.error('[Chart] Error stack:', err?.stack);
          throw err;
        }

        chartRef.current = chart;
        seriesRef.current = candlestickSeries;

        console.log('[Chart] Chart initialized successfully');
        console.log('[Chart] Chart object:', chart);
        console.log('[Chart] Series object:', candlestickSeries);
        console.log('[Chart] seriesRef.current is now set:', !!seriesRef.current);
        console.log('[Chart] chartRef.current is now set:', !!chartRef.current);

        // Set data if already available
        if (candles.length > 0) {
          console.log('[Chart] Setting initial candles data:', candles.length);
          try {
            const formattedData = candles.map(c => ({
              time: c.time as any,
              open: Number(c.open) || 0,
              high: Number(c.high) || 0,
              low: Number(c.low) || 0,
              close: Number(c.close) || 0,
            })).filter(c => c.time > 0 && c.open > 0 && c.high > 0 && c.low > 0 && c.close > 0);
            
            console.log('[Chart] Formatted data sample:', formattedData[0]);
            candlestickSeries.setData(formattedData);
            chart.timeScale().fitContent();
            console.log('[Chart] Initial candles data set successfully');
          } catch (error) {
            console.error('[Chart] Error setting initial candles:', error);
          }
        }

        // Handle resize
        resizeHandler = () => {
          if (chartContainerRef.current && chartRef.current) {
            const newWidth = chartContainerRef.current.clientWidth || 800;
            chartRef.current.applyOptions({ width: newWidth });
          }
        };

        window.addEventListener('resize', resizeHandler);
      } catch (error) {
        console.error('[Chart] Error initializing chart:', error);
        console.error('[Chart] Error details:', error);
      }
    };

    // Start initialization - try immediately and also after delay
    initChart();
    timeoutId = setTimeout(initChart, 200);

    return () => {
      console.log('[Chart] Cleanup: removing chart');
      
      if (timeoutId) {
        clearTimeout(timeoutId);
      }
      
      if (resizeHandler) {
        window.removeEventListener('resize', resizeHandler);
        resizeHandler = null;
      }
      
      if (chartRef.current) {
        try {
          // Simply call remove() - lightweight-charts handles cleanup internally
          chartRef.current.remove();
        } catch (error) {
          console.warn('[Chart] Error removing chart (this is usually safe to ignore):', error);
        }
        
        chartRef.current = null;
        seriesRef.current = null;
      }
    };
  }, []); // Only run once on mount

  // Fetch candles data
  useEffect(() => {
    const fetchCandles = async () => {
      if (!selectedCoin?.symbol) return;
      
      setLoadingChart(true);
      try {
        const symbol = `${selectedCoin.symbol}USDT`;
        const interval = intervalMap[timeframe] || '1m';
        
        const res = await MarketApi.getCandles(symbol, interval);
        console.log(`[Chart] API response for ${symbol} (${interval}):`, res);
        
        if (res.ok && res.data && res.data.length > 0) {
          const formattedData = res.data.map((candle: CandlestickData) => {
            // Ensure time is a Unix timestamp (seconds)
            const time = typeof candle.time === 'number' 
              ? candle.time 
              : Math.floor(new Date(candle.time as any).getTime() / 1000);
            
            return {
              time: time as any, // lightweight-charts expects Unix timestamp
              open: Number(candle.open) || 0,
              high: Number(candle.high) || 0,
              low: Number(candle.low) || 0,
              close: Number(candle.close) || 0,
            };
          }).filter(c => c.time > 0 && c.open > 0 && c.high > 0 && c.low > 0 && c.close > 0);
          
          console.log(`[Chart] Loaded ${formattedData.length} valid candles for ${symbol} (${interval})`);
          console.log('[Chart] Sample candle:', formattedData[0]);
          
          // Always set candles state - useEffect will handle setting to chart
          setCandles(formattedData);
        } else {
          console.warn(`[Chart] No candles data for ${symbol} (${interval}):`, !res.ok ? res.error : 'Empty response');
          setCandles([]);
        }
      } catch (e: any) {
        console.error('Error fetching candles:', e);
        setCandles([]);
      } finally {
        setLoadingChart(false);
      }
    };
    
    fetchCandles();
    
    // Auto refresh every 5 seconds
    const intervalId = setInterval(fetchCandles, 5000);
    return () => clearInterval(intervalId);
  }, [selectedCoin?.symbol, timeframe, intervalMap]);

  // Update chart when candles change (exact copy from ChartTest)
  useEffect(() => {
    if (!seriesRef.current || candles.length === 0) {
      return;
    }

    console.log('[Chart] Updating chart with', candles.length, 'candles');
    
    try {
      const formattedData = candles.map(c => ({
        time: c.time as any,
        open: Number(c.open) || 0,
        high: Number(c.high) || 0,
        low: Number(c.low) || 0,
        close: Number(c.close) || 0,
      })).filter(c => c.time > 0 && c.open > 0 && c.high > 0 && c.low > 0 && c.close > 0);

      console.log('[Chart] Formatted data sample:', formattedData[0]);
      
      seriesRef.current.setData(formattedData);
      
      if (chartRef.current) {
        chartRef.current.timeScale().fitContent();
      }
      
      console.log('[Chart] Chart data updated successfully');
    } catch (error) {
      console.error('[Chart] Error updating chart data:', error);
    }
  }, [candles]);

  // SignalR MarketHub - realtime price updates for charts
  useEffect(() => {
    let retryCount = 0;
    const MAX_RETRIES = 3;
    let isCancelled = false;

    const connectSignalR = async () => {
      if (isCancelled) return;

      try {
        const connection = new signalR.HubConnectionBuilder()
          .withUrl('/marketHub')
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: (retryContext) => {
              // Exponential backoff: 2s, 4s, 8s, max 30s
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
          })
          .build();

        // Handle connection errors gracefully
        connection.onclose((error) => {
          if (error && !isCancelled) {
            console.warn('SignalR connection closed:', error);
          }
        });

        connection.onreconnecting((error) => {
          console.log('SignalR reconnecting...', error?.message);
        });

        connection.onreconnected((connectionId) => {
          console.log('SignalR reconnected:', connectionId);
          retryCount = 0;
        });

        connection.on('ReceivePriceUpdate', (update: any) => {
          if (update.symbol === selectedCoin?.symbol && seriesRef.current && candles.length > 0) {
            const lastCandle = candles[candles.length - 1];
            const now = Math.floor(Date.now() / 1000);
            
            // Check if we should update the current candle or create a new one
            if (lastCandle.time === now || Math.abs(lastCandle.time - now) < 60) {
              // Update current candle
              const updated = {
                ...lastCandle,
                high: Math.max(lastCandle.high, update.currentPrice),
                low: Math.min(lastCandle.low, update.currentPrice),
                close: update.currentPrice,
              };
              seriesRef.current.update(updated);
              setCandles(prev => {
                const newCandles = [...prev];
                newCandles[newCandles.length - 1] = updated;
                return newCandles;
              });
            }
          }
        });

        // Start connection with timeout
        const timeout = setTimeout(() => {
          if (!isCancelled) {
            connection.stop().catch(() => {});
            if (retryCount < MAX_RETRIES) {
              retryCount++;
              setTimeout(connectSignalR, 2000 * retryCount);
            } else {
              console.warn('SignalR: Max retries reached, using polling only');
            }
          }
        }, 5000);

        try {
          await connection.start();
          clearTimeout(timeout);
          
          if (isCancelled) {
            await connection.stop();
            return;
          }

          await connection.invoke('JoinMarketGroup');
          signalRConnectionRef.current = connection;
          retryCount = 0;
          console.log('SignalR MarketHub connected successfully');
        } catch (startError: any) {
          clearTimeout(timeout);
          throw startError;
        }
      } catch (err: any) {
        if (isCancelled) return;
        
        console.warn('SignalR connection failed, using polling only:', err?.message || err);
        
        // Retry with exponential backoff
        if (retryCount < MAX_RETRIES) {
          retryCount++;
          const delay = 2000 * Math.pow(2, retryCount - 1);
          setTimeout(() => {
            if (!isCancelled) {
              connectSignalR();
            }
          }, delay);
        } else {
          console.warn('SignalR: Max retries reached, will use polling only');
        }
      }
    };

    if (selectedCoin?.symbol) {
      connectSignalR();
    }

    return () => {
      isCancelled = true;
      if (signalRConnectionRef.current) {
        signalRConnectionRef.current.stop().catch(() => {
          // Ignore errors on cleanup
        });
        signalRConnectionRef.current = null;
      }
    };
  }, [selectedCoin?.symbol, candles]);

  // TODO: TradingHub SignalR - for order/trade realtime events
  // NOTE: Backend TradingHub is not yet implemented. Uncomment this code when backend adds TradingHub
  /*
  const tradingHubRef = useRef<signalR.HubConnection | null>(null);
  
  useEffect(() => {
    const connectTradingHub = async () => {
      try {
        const token = localStorage.getItem('token');
        const connection = new signalR.HubConnectionBuilder()
          .withUrl('/hubs/trading', {
            accessTokenFactory: () => token || ''
          })
          .withAutomaticReconnect()
          .build();

        // Order placed event
        connection.on('OrderPlaced', (order: any) => {
          console.log('Order placed:', order);
          // Refetch balances when order is placed
          TradingApi.getBalances().then(res => {
            if (res.ok) setBalances(res.data);
          });
        });

        // Order updated event (filled, partial fill, etc.)
        connection.on('OrderUpdated', (order: any) => {
          console.log('Order updated:', order);
          // Refetch balances when order status changes
          TradingApi.getBalances().then(res => {
            if (res.ok) setBalances(res.data);
          });
        });

        // Trade executed event
        connection.on('TradeExecuted', (trade: any) => {
          console.log('Trade executed:', trade);
          // Refetch balances after trade
          TradingApi.getBalances().then(res => {
            if (res.ok) setBalances(res.data);
          });
        });

        // Order rejected event
        connection.on('OrderRejected', (data: any) => {
          console.log('Order rejected:', data);
          setError(data.reason || 'Order was rejected');
        });

        await connection.start();
        await connection.invoke('SubscribeToUserOrders');
        tradingHubRef.current = connection;
      } catch (err) {
        console.warn('TradingHub connection failed:', err);
      }
    };

    connectTradingHub();

    return () => {
      if (tradingHubRef.current) {
        tradingHubRef.current.stop();
        tradingHubRef.current = null;
      }
    };
  }, []);
  */

  // Fetch order book when pair changes
  useEffect(() => {
    const fetchOrderBook = async () => {
      if (!selectedPair) return;
      try {
        const res = await TradingApi.getOrderBook(selectedPair);
        if (res.ok) {
          setOrderBook(res.data);
        }
      } catch (e: any) {
        console.error('Error fetching order book:', e);
      }
    };
    
    fetchOrderBook();
    const interval = setInterval(fetchOrderBook, 3000);
    return () => clearInterval(interval);
  }, [selectedPair]);


  // Format price
  const formatPrice = (value: number | undefined | null) => {
    const numValue = Number(value) || 0;
    if (numValue >= 1000) return `$${numValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
    if (numValue >= 1) return `$${numValue.toFixed(2)}`;
    return `$${numValue.toFixed(4)}`;
  };

  // Calculate conversion
  const currentPrice = selectedCoin?.currentPrice || orderBook?.currentPrice || 0;
  
  // When buyAmount changes, calculate useAmount
  useEffect(() => {
    if (buyAmount && currentPrice > 0) {
      const calculated = (parseFloat(buyAmount) * currentPrice).toFixed(2);
      setUseAmount(calculated);
    } else if (!buyAmount) {
      setUseAmount('');
    }
  }, [buyAmount, currentPrice]);

  // When useAmount changes, calculate buyAmount
  useEffect(() => {
    if (useAmount && currentPrice > 0 && !buyAmount) {
      // Only update if buyAmount is empty to avoid circular updates
    } else if (useAmount && currentPrice > 0) {
      const calculated = (parseFloat(useAmount) / currentPrice).toFixed(8);
      // Only update if user is typing in useAmount field
    }
  }, [useAmount, currentPrice]);

  const handleUseAmountChange = (value: string) => {
    setUseAmount(value);
    if (value && currentPrice > 0) {
      const calculated = (parseFloat(value) / currentPrice).toFixed(8);
      setBuyAmount(calculated);
    } else {
      setBuyAmount('');
    }
  };

  const handleBuyAmountChange = (value: string) => {
    setBuyAmount(value);
    if (value && currentPrice > 0) {
      const calculated = (parseFloat(value) * currentPrice).toFixed(2);
      setUseAmount(calculated);
    } else {
      setUseAmount('');
    }
  };

  const handleSellAmountChange = (value: string) => {
    setSellAmount(value);
    if (value && currentPrice > 0) {
      const calculated = (parseFloat(value) * currentPrice).toFixed(2);
      setReceiveAmount(calculated);
    } else {
      setReceiveAmount('');
    }
  };

  const handleReceiveAmountChange = (value: string) => {
    setReceiveAmount(value);
    if (value && currentPrice > 0) {
      const calculated = (parseFloat(value) / currentPrice).toFixed(8);
      setSellAmount(calculated);
    } else {
      setSellAmount('');
    }
  };

  const handleSubmit = async () => {
    if (side === 'buy' && (!buyAmount || !useAmount)) return;
    if (side === 'sell' && (!sellAmount || !receiveAmount)) return;
    setShowPreview(true);
  };

  const confirmOrder = async () => {
    const amount = side === 'buy' ? buyAmount : sellAmount;
    if (!amount) return;
    if (orderType === 'LIMIT' && !limitPrice) {
      setError('Limit price is required for limit orders');
      return;
    }
    
    setError(null);
    try {
      // Round quantity and price to proper precision
      const quantity = parseFloat(amount);
      const price = orderType === 'LIMIT' ? parseFloat(limitPrice) : undefined;
      
      const res = await TradingApi.placeOrder({
        symbol: selectedPair,
        side: side === 'buy' ? 'BUY' : 'SELL',  // Backend expects uppercase
        type: orderType,  // Already uppercase
        quantity,
        price,
      });
      
      if (!res.ok) {
        setError(res.error);
        setShowPreview(false);
        return;
      }
      
      setShowPreview(false);
      setBuyAmount('');
      setUseAmount('');
      setSellAmount('');
      setReceiveAmount('');
      setLimitPrice('');
      
      // Refetch balances after order is placed
      const balancesRes = await TradingApi.getBalances();
      if (balancesRes.ok) {
        setBalances(balancesRes.data);
      }
      
      onNavigate?.('orders');
    } catch (e: any) {
      setError(e?.message || 'Failed to place order');
      setShowPreview(false);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <Loader2 className="w-8 h-8 text-emerald-500 animate-spin mx-auto mb-4" />
          <p className="text-gray-400">Loading trading data...</p>
        </div>
      </div>
    );
  }

  const priceChange = selectedCoin?.priceChangePercentage24h || orderBook?.priceChangePercentage24h || 0;
  const isPositive = priceChange >= 0;

  return (
    <div className="p-4 lg:p-8 bg-[#0d1117] min-h-screen">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2 text-white">Trade</h1>
        <p className="text-gray-400">Execute market or limit orders</p>
      </div>

      {/* Error Message */}
      {error && (
        <div className="mb-4 p-4 bg-red-500/10 border border-red-500/50 rounded-lg text-red-400">
          {error}
        </div>
      )}

      <div className="grid lg:grid-cols-3 gap-6">
        {/* Left: Chart and Trading Info */}
        <div className="lg:col-span-2 space-y-6">
          {/* Coin Info and Pair Selection */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-4">
                <CoinIcon symbol={selectedCoin?.symbol || 'BTC'} image={selectedCoin?.image} size="lg" />
                <div>
                  <div className="flex items-center gap-2">
                    <h2 className="text-2xl font-bold text-white">
                      {selectedCoin?.name || 'Bitcoin'} ({selectedCoin?.symbol || 'BTC'})
                    </h2>
                    <Badge className="bg-orange-500/10 text-orange-500">HOT</Badge>
                  </div>
                  <div className="flex items-center gap-2 mt-1">
                    <span className="text-white">
                      {selectedPair.split('/')[0]} sang {selectedPair.split('/')[1]}: 1 {selectedPair.split('/')[0]} = {formatPrice(currentPrice)} {selectedPair.split('/')[1]}
                    </span>
                    <Badge className={isPositive ? 'bg-emerald-500/10 text-emerald-500' : 'bg-red-500/10 text-red-500'}>
                      {isPositive ? <TrendingUp className="w-3 h-3 mr-1" /> : <TrendingDown className="w-3 h-3 mr-1" />}
                      {isPositive ? '+' : ''}{priceChange.toFixed(2)}%
                    </Badge>
                    <span className="text-gray-400 text-sm">1 ngày</span>
                  </div>
                </div>
              </div>
              <Select 
                value={selectedPair} 
                onValueChange={(value) => {
                  setSelectedPair(value);
                  setSearchParams({ pair: value });
                }}
              >
                <SelectTrigger className="bg-gray-800 border-gray-700 w-48">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent className="bg-gray-800 border-gray-700 text-white">
                  {pairs.slice(0, 20).map((coin) => (
                    <SelectItem key={coin.id} value={`${coin.symbol}/USDT`}>
                      {coin.symbol}/USDT
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Timeframe Selector */}
            <div className="flex gap-2 mb-4">
              {(['1D', '7D', '1M', '3M', '1Y'] as Timeframe[]).map((tf) => (
                <Button
                  key={tf}
                  type="button"
                  variant={timeframe === tf ? 'default' : 'outline'}
                  onClick={() => setTimeframe(tf)}
                  className={timeframe === tf 
                    ? 'bg-emerald-500 text-black hover:bg-emerald-600' 
                    : 'border-gray-700 text-gray-400 hover:bg-gray-800'
                  }
                  size="sm"
                >
                  {tf === '1D' ? '1 ngày' : tf === '7D' ? '7 ngày' : tf === '1M' ? '1 tháng' : tf === '3M' ? '3 Tháng' : '1Y'}
                </Button>
              ))}
            </div>

            {/* Price Chart */}
            <div 
              className="h-[400px] w-full relative bg-black" 
              ref={(el) => {
                chartContainerRef.current = el;
                if (el) {
                  console.log('[Chart] Container ref set, size:', el.clientWidth, 'x', el.clientHeight);
                }
              }}
              style={{ minHeight: '400px', minWidth: '100%', position: 'relative', width: '100%', height: '400px' }}
            >
              {loadingChart && candles.length === 0 && (
                <div className="flex items-center justify-center h-full absolute inset-0 bg-black/50 z-10 pointer-events-none">
                  <div className="flex flex-col items-center gap-2">
                    <Loader2 className="w-8 h-8 text-[#f2c94c] animate-spin" />
                    <span className="text-gray-400 text-sm">Loading chart data...</span>
                  </div>
                </div>
              )}
              {!loadingChart && candles.length === 0 && !chartRef.current && (
                <div className="flex items-center justify-center h-full absolute inset-0 text-gray-400 z-10 pointer-events-none">
                  <div className="text-center">
                    <p>No chart data available</p>
                    <p className="text-xs text-gray-500 mt-2">Chart will appear when data is loaded</p>
                  </div>
                </div>
              )}
            </div>

            <div className="mt-4 text-xs text-gray-500">
              Lần gần nhất cập nhật trang: {new Date().toLocaleString('vi-VN', { 
                year: 'numeric', 
                month: '2-digit', 
                day: '2-digit', 
                hour: '2-digit', 
                minute: '2-digit',
                timeZone: 'UTC'
              })} (UTC+0)
            </div>
          </Card>
            </div>

        {/* Right: Trading Form */}
        <div className="space-y-6">
          {/* Trading Form */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <div className="flex gap-2 mb-4 border-b border-gray-800 pb-2">
                <button
                  type="button"
                  onClick={() => setSide('buy')}
                  className={`flex-1 py-2 px-4 rounded-t-md transition-all ${
                    side === 'buy' 
                      ? 'border-b-2 border-[#f2c94c] text-[#f2c94c] bg-gray-800/50 font-semibold' 
                      : 'text-gray-400 hover:text-gray-300 hover:bg-gray-800/30'
                  }`}
                >
                  Mua {selectedPair.split('/')[0]}
                </button>
                <button
                  type="button"
                  onClick={() => setSide('sell')}
                  className={`flex-1 py-2 px-4 rounded-t-md transition-all ${
                    side === 'sell' 
                      ? 'border-b-2 border-[#f2c94c] text-[#f2c94c] bg-gray-800/50 font-semibold' 
                      : 'text-gray-400 hover:text-gray-300 hover:bg-gray-800/30'
                  }`}
                >
                  Bán {selectedPair.split('/')[0]}
                </button>
            </div>

            {/* Order Type Selector */}
            <div className="flex gap-2 mb-4">
              <Button
                type="button"
                variant={orderType === 'MARKET' ? 'default' : 'outline'}
                onClick={() => setOrderType('MARKET')}
                className={`flex-1 font-medium ${
                  orderType === 'MARKET' 
                    ? 'bg-emerald-500 text-black hover:bg-emerald-600 shadow-sm' 
                    : 'border-gray-700 text-gray-300 hover:bg-gray-800 hover:text-white hover:border-gray-600'
                }`}
                size="sm"
              >
                Market
              </Button>
              <Button
                type="button"
                variant={orderType === 'LIMIT' ? 'default' : 'outline'}
                onClick={() => setOrderType('LIMIT')}
                className={`flex-1 font-medium ${
                  orderType === 'LIMIT' 
                    ? 'bg-emerald-500 text-black hover:bg-emerald-600 shadow-sm' 
                    : 'border-gray-700 text-gray-300 hover:bg-gray-800 hover:text-white hover:border-gray-600'
                }`}
                size="sm"
              >
                Limit
              </Button>
            </div>

            {side === 'buy' ? (
              <div className="space-y-4">
                {/* Limit Price (only for LIMIT orders) */}
                {orderType === 'LIMIT' && (
                  <div>
                    <Label className="text-gray-400 mb-2 block">Giá limit</Label>
                    <div className="flex gap-2">
                      <Input
                        type="number"
                        placeholder={formatPrice(currentPrice)}
                        value={limitPrice}
                        onChange={(e) => setLimitPrice(e.target.value)}
                        className="bg-gray-800 border-gray-700 text-white flex-1"
                      />
                      <div className="flex items-center justify-center px-3 py-2 border border-gray-700 bg-gray-800/50 text-gray-300 rounded-md w-20 text-sm font-medium cursor-default">
                        {selectedPair.split('/')[1]}
                      </div>
                    </div>
                    <p className="text-sm text-gray-500 mt-2">
                      Giá thị trường hiện tại: {formatPrice(currentPrice)}
                    </p>
                  </div>
                )}

                {/* You Buy */}
                <div>
                  <Label className="text-gray-400 mb-2 block">Bạn mua</Label>
                  <div className="flex gap-2">
                  <Input
                    type="number"
                      placeholder="0"
                      value={buyAmount}
                      onChange={(e) => handleBuyAmountChange(e.target.value)}
                      className="bg-gray-800 border-gray-700 text-white flex-1"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-700 bg-gray-800/50 text-gray-300 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[0]}
                    </div>
                  </div>
                  <p className="text-sm text-gray-500 mt-2">
                    1 {selectedPair.split('/')[0]} ≈ {selectedPair.split('/')[1]} {formatPrice(currentPrice)}
                  </p>
                </div>

                {/* You Use */}
            <div>
                  <Label className="text-gray-400 mb-2 block">Bạn sử dụng{orderType === 'LIMIT' ? ' (dự kiến)' : ''}</Label>
                  <div className="flex gap-2">
              <Input
                type="number"
                      placeholder="10 - 50,000"
                      value={useAmount}
                      onChange={(e) => handleUseAmountChange(e.target.value)}
                      className="bg-gray-800 border-gray-700 text-white flex-1"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-700 bg-gray-800/50 text-gray-300 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[1]}
                    </div>
              </div>
            </div>

                {/* Buy Button */}
            <Button
              onClick={handleSubmit}
              disabled={!buyAmount || !useAmount}
              className="w-full bg-[#f2c94c] text-black hover:bg-[#e5b73d] font-bold py-6 text-lg shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition-all"
            >
              Mua {selectedPair.split('/')[0]}
            </Button>
          </div>
            ) : (
              <div className="space-y-4">
                {/* Limit Price (only for LIMIT orders) */}
                {orderType === 'LIMIT' && (
                  <div>
                    <Label className="text-gray-400 mb-2 block">Giá limit</Label>
                    <div className="flex gap-2">
                      <Input
                        type="number"
                        placeholder={formatPrice(currentPrice)}
                        value={limitPrice}
                        onChange={(e) => setLimitPrice(e.target.value)}
                        className="bg-gray-800 border-gray-700 text-white flex-1"
                      />
                      <div className="flex items-center justify-center px-3 py-2 border border-gray-700 bg-gray-800/50 text-gray-300 rounded-md w-20 text-sm font-medium cursor-default">
                        {selectedPair.split('/')[1]}
                      </div>
                    </div>
                    <p className="text-sm text-gray-500 mt-2">
                      Giá thị trường hiện tại: {formatPrice(currentPrice)}
                    </p>
                  </div>
                )}

                {/* You Sell */}
                <div>
                  <Label className="text-gray-400 mb-2 block">Bạn bán</Label>
                  <div className="flex gap-2">
                    <Input
                      type="number"
                      placeholder="0"
                      value={sellAmount}
                      onChange={(e) => handleSellAmountChange(e.target.value)}
                      className="bg-gray-800 border-gray-700 text-white flex-1"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-700 bg-gray-800/50 text-gray-300 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[0]}
                    </div>
                  </div>
                  <p className="text-sm text-gray-500 mt-2">
                    1 {selectedPair.split('/')[0]} ≈ {selectedPair.split('/')[1]} {formatPrice(currentPrice)}
                  </p>
                </div>

                {/* You Receive */}
                <div>
                  <Label className="text-gray-400 mb-2 block">Bạn nhận{orderType === 'LIMIT' ? ' (dự kiến)' : ''}</Label>
                  <div className="flex gap-2">
                    <Input
                      type="number"
                      placeholder="0"
                      value={receiveAmount}
                      onChange={(e) => handleReceiveAmountChange(e.target.value)}
                      className="bg-gray-800 border-gray-700 text-white flex-1"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-700 bg-gray-800/50 text-gray-300 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[1]}
                    </div>
                  </div>
                </div>

                {/* Sell Button */}
                <Button
                  onClick={handleSubmit}
                  disabled={!sellAmount || !receiveAmount}
                  className="w-full bg-red-500 text-white hover:bg-red-600 font-bold py-6 text-lg shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition-all"
                >
                  Bán {selectedPair.split('/')[0]}
                </Button>
              </div>
            )}
        </Card>

          {/* Order Book Preview */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <h3 className="mb-4 text-white">Order Book Preview</h3>
            <div className="space-y-3">
              <div>
                <div className="text-sm text-gray-400 mb-2">Sell Orders</div>
                {orderBook?.asks?.length ? (
                  orderBook.asks.slice(0, 5).map((ask, i) => (
                    <div key={i} className="flex justify-between text-sm py-1">
                      <span className="text-red-500">{formatPrice(ask.price)}</span>
                      <span className="text-gray-400">{ask.quantity.toFixed(4)}</span>
                    </div>
                  ))
                ) : (
                  <div className="text-gray-400 text-sm py-2">No orders</div>
                )}
              </div>

              <div className="py-2 text-center border-y border-gray-800">
                {orderBook ? (
                  <>
                    <div className="text-xl text-emerald-500">{formatPrice(orderBook.currentPrice)}</div>
                    <div className="text-xs text-gray-400 mt-1">
                      {orderBook.priceChangePercentage24h >= 0 ? '+' : ''}
                      {orderBook.priceChangePercentage24h.toFixed(2)}%
                    </div>
                  </>
                ) : (
                  <Loader2 className="w-5 h-5 text-emerald-500 animate-spin mx-auto" />
                )}
              </div>

              <div>
                <div className="text-sm text-gray-400 mb-2">Buy Orders</div>
                {orderBook?.bids?.length ? (
                  orderBook.bids.slice(0, 5).map((bid, i) => (
                    <div key={i} className="flex justify-between text-sm py-1">
                      <span className="text-emerald-500">{formatPrice(bid.price)}</span>
                      <span className="text-gray-400">{bid.quantity.toFixed(4)}</span>
                    </div>
                  ))
                ) : (
                  <div className="text-gray-400 text-sm py-2">No orders</div>
                )}
              </div>
            </div>
          </Card>

          {/* Account Balance */}
          <Card className="bg-[#1a1d24] border-gray-800 p-6">
            <h3 className="mb-4 text-white">Account Balance</h3>
            {balances ? (
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-400">Total Balance</span>
                  <span className="text-emerald-500">${(balances?.totalBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Available</span>
                  <span className="text-emerald-500">${(balances?.availableBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-400">Locked</span>
                  <span className="text-orange-500">${(balances?.lockedBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
              </div>
            ) : (
              <div className="text-center py-4">
                <Loader2 className="w-5 h-5 text-emerald-500 animate-spin mx-auto" />
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Order Preview Dialog */}
      <Dialog open={showPreview} onOpenChange={setShowPreview}>
        <DialogContent className="bg-[#1a1d24] border-gray-800 text-white">
          <DialogHeader>
            <DialogTitle>Confirm Order</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-3 p-4 bg-gray-800 rounded-lg">
              <div className="flex justify-between">
                <span className="text-gray-400">Pair</span>
                <span className="text-white">{selectedPair}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Side</span>
                <Badge className={side === 'buy' ? "bg-emerald-500/10 text-emerald-500" : "bg-red-500/10 text-red-500"}>
                  {side === 'buy' ? 'BUY' : 'SELL'}
                </Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Type</span>
                <Badge className="bg-blue-500/10 text-blue-500">{orderType}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Amount</span>
                <span className="text-white">
                  {side === 'buy' ? buyAmount : sellAmount} {selectedPair.split('/')[0]}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-400">Price</span>
                <span className="text-white">
                  {orderType === 'LIMIT' ? formatPrice(parseFloat(limitPrice)) : formatPrice(currentPrice)}
                  {orderType === 'MARKET' && <span className="text-xs text-gray-500 ml-1">(market)</span>}
                </span>
              </div>
              <div className="flex justify-between border-t border-gray-700 pt-3">
                <span className="text-white">
                  {side === 'buy' ? 'Total' : 'Receive'}{orderType === 'LIMIT' ? ' (estimated)' : ''}
                </span>
                <span className="text-white text-lg">
                  {side === 'buy' ? (
                    orderType === 'LIMIT' && limitPrice 
                      ? (parseFloat(buyAmount) * parseFloat(limitPrice)).toFixed(2)
                      : useAmount
                  ) : (
                    orderType === 'LIMIT' && limitPrice
                      ? (parseFloat(sellAmount) * parseFloat(limitPrice)).toFixed(2)
                      : receiveAmount
                  )} {selectedPair.split('/')[1]}
                </span>
              </div>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowPreview(false)} className="border-gray-700">
              Cancel
            </Button>
            <Button
              onClick={confirmOrder}
              className="bg-[#f2c94c] text-black hover:bg-[#e5b73d]"
            >
              Confirm {side === 'buy' ? 'Buy' : 'Sell'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
