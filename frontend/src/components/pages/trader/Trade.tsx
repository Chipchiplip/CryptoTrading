import React, { useState, useEffect, useLayoutEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { TrendingUp, TrendingDown, Info, Loader2, Search, ChevronDown } from 'lucide-react';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
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
  const [showCoinSelector, setShowCoinSelector] = useState(false);
  const [coinSearchQuery, setCoinSearchQuery] = useState('');
  
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
  const hasFittedContentRef = useRef<boolean>(false); // Track if we've fitted content initially
  const lastSymbolRef = useRef<string | null>(null); // Track last symbol to reset fit on symbol change
  const lastTimeframeRef = useRef<Timeframe | null>(null); // Track last timeframe to reset fit on timeframe change
  
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
    let resizeObserverInstance: ResizeObserver | null = null;
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
        // Note: In lightweight-charts v5, zoom and scroll are enabled by default
        // But we need to ensure proper configuration
        const chart = createChart(container, {
          layout: {
            background: { type: ColorType.Solid, color: '#000000' },
            textColor: '#ffffff',
          },
          grid: {
            vertLines: { color: '#1a1a1a' },
            horzLines: { color: '#1a1a1a' },
          },
          // Use autoSize to maintain proper aspect ratio
          autoSize: true,
          width: finalWidth,
          height: finalHeight,
          timeScale: {
            timeVisible: true,
            secondsVisible: false,
            // Enable pan and zoom
            fixLeftEdge: false,
            fixRightEdge: false,
            allowShiftVisibleRangeOnWhitespaceReplacement: true,
          },
          rightPriceScale: {
            borderColor: '#1a1a1a',
          },
          // Enable crosshair for better interaction
          crosshair: {
            vertLine: {
              visible: true,
              width: 1,
              color: '#758696',
              style: 0,
            },
            horzLine: {
              visible: true,
              width: 1,
              color: '#758696',
              style: 0,
            },
          },
        });

        console.log('[Chart] Chart created, adding series...');
        console.log('[Chart] Chart instance:', chart);
        console.log('[Chart] Container children:', container.children.length);

        // Add candlestick series - Use CandlestickSeries class for version 5.x
        // In v5.x, you need to import CandlestickSeries and use it
        let candlestickSeries: ISeriesApi<'Candlestick'> | null = null;
        
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
          // Don't throw - let chart render even if series creation fails
          // The chart will still be created and can be used
          console.warn('[Chart] Continuing despite series creation error - chart will still render');
        }

        // Always save chart reference, even if series creation failed
        chartRef.current = chart;
        if (candlestickSeries) {
          seriesRef.current = candlestickSeries;
        }

        console.log('[Chart] Chart initialized successfully');
        console.log('[Chart] Chart object:', chart);
        console.log('[Chart] Series object:', candlestickSeries);
        console.log('[Chart] seriesRef.current is now set:', !!seriesRef.current);
        console.log('[Chart] chartRef.current is now set:', !!chartRef.current);

        // Ensure canvas can receive events for zoom/scroll
        // Use setTimeout to ensure canvas is created
        setTimeout(() => {
          const canvas = container.querySelector('canvas');
          if (canvas) {
            console.log('[Chart] Canvas found after initialization, configuring for zoom/scroll');
            canvas.style.pointerEvents = 'auto';
            canvas.style.touchAction = 'pan-x pan-y';
            canvas.style.userSelect = 'none'; // Prevent text selection
            canvas.style.webkitUserSelect = 'none';
            // Ensure canvas is not blocked
            canvas.style.position = 'relative';
            canvas.style.zIndex = '1';
            
            // Also check for any overlay divs that might block interaction
            const overlays = container.querySelectorAll('div[style*="position: absolute"]');
            overlays.forEach((overlay) => {
              const el = overlay as HTMLElement;
              if (el.style.pointerEvents !== 'none') {
                // Only set to none if it's not already a loading overlay
                if (!el.classList.contains('pointer-events-none')) {
                  // Check if it's blocking the canvas
                  const rect = el.getBoundingClientRect();
                  const canvasRect = canvas.getBoundingClientRect();
                  if (rect.top <= canvasRect.top && rect.bottom >= canvasRect.bottom &&
                      rect.left <= canvasRect.left && rect.right >= canvasRect.right) {
                    console.warn('[Chart] Found potential blocking overlay, setting pointer-events-none');
                    el.style.pointerEvents = 'none';
                  }
                }
              }
            });
          } else {
            console.warn('[Chart] Canvas not found after initialization');
          }
        }, 200);

        // Set data if already available
        if (candles.length > 0 && candlestickSeries) {
          console.log('[Chart] Setting initial candles data:', candles.length);
          try {
            const formattedData = candles.map(c => ({
              time: c.time as any,
              open: Number(c.open) || 0,
              high: Number(c.high) || 0,
              low: Number(c.low) || 0,
              close: Number(c.close) || 0,
            })).filter(c => c.time > 0 && c.open > 0 && c.high > 0 && c.low > 0 && c.close > 0);
            
            if (formattedData.length > 0) {
              console.log('[Chart] Formatted data sample:', formattedData[0]);
              candlestickSeries.setData(formattedData);
              chart.timeScale().fitContent();
              console.log('[Chart] Initial candles data set successfully');
            }
          } catch (error) {
            console.error('[Chart] Error setting initial candles:', error);
          }
        }

        // Handle resize - use ResizeObserver for better performance
        resizeObserverInstance = new ResizeObserver((entries) => {
          if (chartRef.current && entries.length > 0) {
            const entry = entries[0];
            const newWidth = entry.contentRect.width;
            const newHeight = entry.contentRect.height;
            
            if (newWidth > 0 && newHeight > 0) {
              chartRef.current.applyOptions({ 
                width: newWidth,
                height: newHeight 
              });
            }
          }
        });

        resizeObserverInstance.observe(container);

        // Also handle window resize for fallback
        resizeHandler = () => {
          if (chartContainerRef.current && chartRef.current) {
            const rect = chartContainerRef.current.getBoundingClientRect();
            const newWidth = rect.width || chartContainerRef.current.clientWidth || 800;
            const newHeight = rect.height || chartContainerRef.current.clientHeight || 400;
            
            if (newWidth > 0 && newHeight > 0) {
              chartRef.current.applyOptions({ 
                width: newWidth,
                height: newHeight 
              });
            }
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
      
      if (resizeObserverInstance && chartContainerRef.current) {
        resizeObserverInstance.unobserve(chartContainerRef.current);
        resizeObserverInstance.disconnect();
        resizeObserverInstance = null;
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
    
    // Auto refresh every 30 seconds (reduced frequency to prevent jitter)
    const intervalId = setInterval(fetchCandles, 30000);
    return () => clearInterval(intervalId);
  }, [selectedCoin?.symbol, timeframe]); // Removed intervalMap from dependencies

  // Update chart when candles change - preserve zoom state
  useEffect(() => {
    if (!seriesRef.current || !chartRef.current || candles.length === 0) {
      return;
    }

    const currentSymbol = selectedCoin?.symbol || '';
    const symbolChanged = lastSymbolRef.current !== currentSymbol;
    const timeframeChanged = lastTimeframeRef.current !== timeframe;
    
    // Reset fit flag if symbol or timeframe changed
    if (symbolChanged || timeframeChanged) {
      hasFittedContentRef.current = false;
      lastSymbolRef.current = currentSymbol;
      lastTimeframeRef.current = timeframe;
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
      
      // Update data without resetting zoom
      seriesRef.current.setData(formattedData);
      
      // Only fit content on first load or when symbol/timeframe changes
      if (chartRef.current && (!hasFittedContentRef.current || symbolChanged || timeframeChanged)) {
        chartRef.current.timeScale().fitContent();
        hasFittedContentRef.current = true;
        console.log('[Chart] Fitted content (first time, symbol change, or timeframe change)');
      } else {
        console.log('[Chart] Preserved zoom state during update');
      }
      
      console.log('[Chart] Chart data updated successfully');
    } catch (error) {
      console.error('[Chart] Error updating chart data:', error);
    }
  }, [candles, selectedCoin?.symbol, timeframe]); // Added selectedCoin?.symbol and timeframe to detect changes

  // SignalR MarketHub - realtime price updates for charts
  useEffect(() => {
    let retryCount = 0;
    const MAX_RETRIES = 3;
    let isMounted = true;
    let connection: signalR.HubConnection | null = null;

    const connectSignalR = async () => {
      // Clean up existing connection first
      if (signalRConnectionRef.current) {
        try {
          await signalRConnectionRef.current.stop();
        } catch (err) {
          console.warn('Error stopping existing SignalR connection:', err);
        }
        signalRConnectionRef.current = null;
      }

      if (!selectedCoin?.symbol || !isMounted) return;

      try {
        connection = new signalR.HubConnectionBuilder()
          .withUrl('/marketHub')
          .withAutomaticReconnect({
            nextRetryDelayInMilliseconds: (retryContext) => {
              // Exponential backoff: 2s, 4s, 8s, max 30s
              return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
            }
          })
          .configureLogging(signalR.LogLevel.Warning)
          .build();

        // Handle connection errors gracefully
        connection.onclose((error) => {
          if (error && isMounted) {
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
          if (!isMounted) return;
          if (update.symbol === selectedCoin?.symbol && seriesRef.current) {
            setCandles(prev => {
              if (prev.length === 0) return prev;
              const lastCandle = prev[prev.length - 1];
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
                
                // Update chart
                if (seriesRef.current) {
                  try {
                    seriesRef.current.update(updated);
                  } catch (err) {
                    console.warn('Error updating chart:', err);
                  }
                }
                
                // Update state
                const newCandles = [...prev];
                newCandles[newCandles.length - 1] = updated;
                return newCandles;
              }
              return prev;
            });
          }
        });

        // Start connection with timeout
        const timeout = setTimeout(() => {
          if (isMounted) {
            connection?.stop().catch(() => {});
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
          
          if (!isMounted) {
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
        if (!isMounted) return;
        
        console.warn('SignalR connection failed, using polling only:', err?.message || err);
        
        // Retry with exponential backoff
        if (retryCount < MAX_RETRIES) {
          retryCount++;
          const delay = 2000 * Math.pow(2, retryCount - 1);
          setTimeout(() => {
            if (isMounted) {
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
      isMounted = false;
      if (signalRConnectionRef.current) {
        signalRConnectionRef.current.stop()
          .catch((err) => {
            console.warn('Error stopping SignalR connection during cleanup:', err);
          })
          .finally(() => {
            signalRConnectionRef.current = null;
          });
      }
    };
  }, [selectedCoin?.symbol]); // Removed 'candles' from dependencies

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
        console.log('[OrderBook] Fetching order book for:', selectedPair);
        const res = await TradingApi.getOrderBook(selectedPair);
        console.log('[OrderBook] Response:', res);
        if (res.ok && res.data) {
          console.log('[OrderBook] Setting order book data:', res.data);
          setOrderBook(res.data);
        } else {
          console.warn('[OrderBook] API response not OK');
          setOrderBook(null);
        }
      } catch (e: any) {
        console.error('[OrderBook] Error fetching order book:', e);
        setOrderBook(null);
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
    <div className="p-4 lg:p-8 bg-[#1a1f2e] min-h-screen">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-3xl font-bold mb-2 text-white">Trade</h1>
        <p className="text-gray-300">Execute market or limit orders</p>
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
          <Card className="bg-[#243447] border-gray-700/50 p-6 shadow-lg">
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
                    <span className="text-gray-300 text-sm">1 ngày</span>
                  </div>
                </div>
              </div>
              {/* Modern Coin Selector Button */}
              <button
                onClick={() => setShowCoinSelector(true)}
                className="flex items-center gap-3 px-4 py-2.5 bg-[#2d4156] hover:bg-[#354a62] border border-gray-600 rounded-lg transition-all duration-200 hover:border-emerald-500/70 group shadow-md"
              >
                <div className="flex items-center gap-2">
                  <CoinIcon 
                    symbol={selectedCoin?.symbol || 'BTC'} 
                    image={selectedCoin?.image} 
                    size="sm" 
                  />
                  <span className="text-white font-medium">
                    {selectedPair}
                  </span>
                </div>
                <ChevronDown className="w-4 h-4 text-gray-400 group-hover:text-emerald-500 transition-colors" />
              </button>
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
                    ? 'bg-emerald-500 text-white hover:bg-emerald-600 shadow-lg shadow-emerald-500/30' 
                    : 'border-gray-600 bg-[#2d4156] text-gray-200 hover:bg-[#354a62] hover:text-white'
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
              ref={chartContainerRef}
              style={{ 
                minHeight: '400px', 
                minWidth: '100%', 
                position: 'relative', 
                width: '100%', 
                height: '400px',
                maxHeight: '400px',
                // Ensure container can receive mouse events
                overflow: 'hidden', // Changed from 'visible' to prevent overflow
                pointerEvents: 'auto',
                touchAction: 'pan-x pan-y',
                userSelect: 'none', // Prevent text selection
                WebkitUserSelect: 'none',
              }}
            >
              {loadingChart && candles.length === 0 && (
                <div className="flex items-center justify-center h-full absolute inset-0 bg-black/50 z-10 pointer-events-none">
                  <div className="flex flex-col items-center gap-2 pointer-events-none">
                    <Loader2 className="w-8 h-8 text-[#f2c94c] animate-spin" />
                    <span className="text-gray-400 text-sm">Loading chart data...</span>
                  </div>
                </div>
              )}
              {!loadingChart && candles.length === 0 && !chartRef.current && (
                <div className="flex items-center justify-center h-full absolute inset-0 text-gray-400 z-10 pointer-events-none">
                  <div className="text-center pointer-events-none">
                    <p>No chart data available</p>
                    <p className="text-xs text-gray-500 mt-2">Chart will appear when data is loaded</p>
                  </div>
                </div>
              )}
            </div>

            <div className="mt-4 text-xs text-gray-400">
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
          <Card className="bg-[#243447] border-gray-700/50 p-6 shadow-lg">
            <div className="flex gap-2 mb-4 border-b border-gray-800 pb-2">
                <button
                  type="button"
                  onClick={() => setSide('buy')}
                  className={`flex-1 py-2 px-4 rounded-t-md transition-all ${
                    side === 'buy' 
                      ? 'border-b-2 border-emerald-500 text-emerald-400 bg-[#2d4156] font-semibold' 
                      : 'text-gray-300 hover:text-white hover:bg-[#2d4156]/50'
                  }`}
                >
                  Mua {selectedPair.split('/')[0]}
                </button>
                <button
                  type="button"
                  onClick={() => setSide('sell')}
                  className={`flex-1 py-2 px-4 rounded-t-md transition-all ${
                    side === 'sell' 
                      ? 'border-b-2 border-red-500 text-red-400 bg-[#2d4156] font-semibold' 
                      : 'text-gray-300 hover:text-white hover:bg-[#2d4156]/50'
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
                    ? 'bg-emerald-500 text-white hover:bg-emerald-600 shadow-lg shadow-emerald-500/30' 
                    : 'border-gray-600 bg-[#2d4156] text-gray-200 hover:bg-[#354a62] hover:text-white hover:border-gray-500'
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
                    ? 'bg-emerald-500 text-white hover:bg-emerald-600 shadow-lg shadow-emerald-500/30' 
                    : 'border-gray-600 bg-[#2d4156] text-gray-200 hover:bg-[#354a62] hover:text-white hover:border-gray-500'
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
                  <Label className="text-gray-200 mb-2 block">Bạn mua</Label>
                  <div className="flex gap-2">
                  <Input
                    type="number"
                      placeholder="0"
                      value={buyAmount}
                      onChange={(e) => handleBuyAmountChange(e.target.value)}
                      className="bg-[#2d4156] border-gray-600 text-white placeholder:text-gray-400 flex-1 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-600 bg-[#2d4156] text-gray-200 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[0]}
                    </div>
                  </div>
                  <p className="text-sm text-gray-500 mt-2">
                    1 {selectedPair.split('/')[0]} ≈ {selectedPair.split('/')[1]} {formatPrice(currentPrice)}
                  </p>
                </div>

                {/* You Use */}
            <div>
                  <Label className="text-gray-200 mb-2 block">Bạn sử dụng{orderType === 'LIMIT' ? ' (dự kiến)' : ''}</Label>
                  <div className="flex gap-2">
              <Input
                type="number"
                      placeholder="10 - 50,000"
                      value={useAmount}
                      onChange={(e) => handleUseAmountChange(e.target.value)}
                      className="bg-[#2d4156] border-gray-600 text-white placeholder:text-gray-400 flex-1 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-600 bg-[#2d4156] text-gray-200 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[1]}
                    </div>
              </div>
            </div>

                {/* Buy Button */}
                <Button
                  onClick={handleSubmit}
                  disabled={!buyAmount || !useAmount || parseFloat(buyAmount) <= 0 || parseFloat(useAmount) <= 0}
                  className="w-full text-white font-bold py-6 text-lg shadow-lg disabled:opacity-50 disabled:cursor-not-allowed transition-all transform hover:scale-[1.02] active:scale-[0.98]"
                  style={{ 
                    backgroundColor: '#00bc7d',
                  }}
                  onMouseEnter={(e: React.MouseEvent<HTMLButtonElement>) => {
                    if (!e.currentTarget.disabled) {
                      e.currentTarget.style.backgroundColor = '#00a66a';
                    }
                  }}
                  onMouseLeave={(e: React.MouseEvent<HTMLButtonElement>) => {
                    if (!e.currentTarget.disabled) {
                      e.currentTarget.style.backgroundColor = '#00bc7d';
                    }
                  }}
                  onMouseDown={(e: React.MouseEvent<HTMLButtonElement>) => {
                    if (!e.currentTarget.disabled) {
                      e.currentTarget.style.backgroundColor = '#009966';
                    }
                  }}
                  onMouseUp={(e: React.MouseEvent<HTMLButtonElement>) => {
                    if (!e.currentTarget.disabled) {
                      e.currentTarget.style.backgroundColor = '#00bc7d';
                    }
                  }}
                >
                  <TrendingUp className="w-5 h-5 mr-2 inline" />
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
                  <Label className="text-gray-200 mb-2 block">Bạn bán</Label>
                  <div className="flex gap-2">
                    <Input
                      type="number"
                      placeholder="0"
                      value={sellAmount}
                      onChange={(e) => handleSellAmountChange(e.target.value)}
                      className="bg-[#2d4156] border-gray-600 text-white placeholder:text-gray-400 flex-1 focus:border-red-500 focus:ring-1 focus:ring-red-500"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-600 bg-[#2d4156] text-gray-200 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[0]}
                    </div>
                  </div>
                  <p className="text-sm text-gray-500 mt-2">
                    1 {selectedPair.split('/')[0]} ≈ {selectedPair.split('/')[1]} {formatPrice(currentPrice)}
                  </p>
                </div>

                {/* You Receive */}
                <div>
                  <Label className="text-gray-200 mb-2 block">Bạn nhận{orderType === 'LIMIT' ? ' (dự kiến)' : ''}</Label>
                  <div className="flex gap-2">
                    <Input
                      type="number"
                      placeholder="0"
                      value={receiveAmount}
                      onChange={(e) => handleReceiveAmountChange(e.target.value)}
                      className="bg-[#2d4156] border-gray-600 text-white placeholder:text-gray-400 flex-1 focus:border-red-500 focus:ring-1 focus:ring-red-500"
                    />
                    <div className="flex items-center justify-center px-3 py-2 border border-gray-600 bg-[#2d4156] text-gray-200 rounded-md w-20 text-sm font-medium cursor-default">
                      {selectedPair.split('/')[1]}
                    </div>
                  </div>
                </div>

                {/* Sell Button */}
                <Button
                  onClick={handleSubmit}
                  disabled={!sellAmount || !receiveAmount || parseFloat(sellAmount) <= 0 || parseFloat(receiveAmount) <= 0}
                  className="w-full bg-red-500 hover:bg-red-600 active:bg-red-700 text-white font-bold py-6 text-lg shadow-lg shadow-red-500/30 disabled:opacity-50 disabled:cursor-not-allowed transition-all transform hover:scale-[1.02] active:scale-[0.98]"
                >
                  <TrendingDown className="w-5 h-5 mr-2 inline" />
                  Bán {selectedPair.split('/')[0]}
                </Button>
              </div>
            )}
        </Card>

          {/* Order Book Preview */}
          <Card className="bg-[#243447] border-gray-700/50 p-6 shadow-lg">
            <h3 className="mb-4 text-white">Order Book Preview</h3>
            <div className="space-y-3">
              <div>
                <div className="text-sm text-gray-200 mb-2 font-medium">Sell Orders</div>
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
                <div className="text-sm text-gray-200 mb-2 font-medium">Buy Orders</div>
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
          <Card className="bg-[#243447] border-gray-700/50 p-6 shadow-lg">
            <h3 className="mb-4 text-white">Account Balance</h3>
            {balances ? (
              <div className="space-y-3">
                <div className="flex justify-between">
                  <span className="text-gray-200">Total Balance</span>
                  <span className="text-emerald-500">${(balances?.totalBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-200">Available</span>
                  <span className="text-emerald-500">${(balances?.availableBalance || 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-200">Locked</span>
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
        <DialogContent 
          className="!bg-black !border-gray-600 text-white shadow-2xl"
          style={{ backgroundColor: '#0C121E' }}
        >
          <DialogHeader>
            <DialogTitle>Confirm Order</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-3 p-4 bg-gray-900/50 rounded-lg border border-gray-600/50">
              <div className="flex justify-between">
                <span className="text-gray-300">Pair</span>
                <span className="text-white">{selectedPair}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-300">Side</span>
                <Badge className={side === 'buy' ? "bg-emerald-500/10 text-emerald-500" : "bg-red-500/10 text-red-500"}>
                  {side === 'buy' ? 'BUY' : 'SELL'}
                </Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-300">Type</span>
                <Badge className="bg-blue-500/10 text-blue-500">{orderType}</Badge>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-300">Amount</span>
                <span className="text-white">
                  {side === 'buy' ? buyAmount : sellAmount} {selectedPair.split('/')[0]}
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-300">Price</span>
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
          <DialogFooter className="gap-3">
            <Button 
              onClick={() => setShowPreview(false)} 
              className="flex-1 font-medium py-6 text-lg transition-all text-white"
              style={{ 
                backgroundColor: '#c93e43',
              }}
              onMouseEnter={(e: React.MouseEvent<HTMLButtonElement>) => {
                e.currentTarget.style.backgroundColor = '#b5363a';
              }}
              onMouseLeave={(e: React.MouseEvent<HTMLButtonElement>) => {
                e.currentTarget.style.backgroundColor = '#c93e43';
              }}
            >
              Hủy
            </Button>
            <Button
              onClick={confirmOrder}
              className="flex-1 font-bold py-6 text-lg shadow-lg transition-all transform hover:scale-[1.02] active:scale-[0.98] text-white"
              style={side === 'buy' 
                ? { backgroundColor: '#00ac72' }
                : { backgroundColor: '#ef4444' }
              }
              onMouseEnter={(e: React.MouseEvent<HTMLButtonElement>) => {
                if (side === 'buy') {
                  e.currentTarget.style.backgroundColor = '#009966';
                } else {
                  e.currentTarget.style.backgroundColor = '#dc2626';
                }
              }}
              onMouseLeave={(e: React.MouseEvent<HTMLButtonElement>) => {
                if (side === 'buy') {
                  e.currentTarget.style.backgroundColor = '#00ac72';
                } else {
                  e.currentTarget.style.backgroundColor = '#ef4444';
                }
              }}
            >
              {side === 'buy' ? (
                <>
                  <TrendingUp className="w-5 h-5 mr-2 inline" />
                  Xác nhận Mua
                </>
              ) : (
                <>
                  <TrendingDown className="w-5 h-5 mr-2 inline" />
                  Xác nhận Bán
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Modern Coin Selector Dialog */}
      <Dialog open={showCoinSelector} onOpenChange={setShowCoinSelector}>
        <DialogContent className="bg-[#1a1d24] border-gray-800 text-white max-w-2xl max-h-[80vh]">
          <DialogHeader>
            <DialogTitle className="text-2xl font-bold">Chọn cặp giao dịch</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            {/* Search Bar */}
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-5 h-5 text-gray-400" />
              <Input
                type="text"
                placeholder="Tìm kiếm coin (BTC, ETH, ...)"
                value={coinSearchQuery}
                onChange={(e) => setCoinSearchQuery(e.target.value)}
                className="bg-gray-800 border-gray-700 text-white pl-10 h-12"
              />
            </div>

            {/* Coin List */}
            <div className="max-h-[500px] overflow-y-auto space-y-2 pr-2 custom-scrollbar">
              {pairs
                .filter((coin) => {
                  const query = coinSearchQuery.toLowerCase();
                  return (
                    coin.symbol.toLowerCase().includes(query) ||
                    coin.name.toLowerCase().includes(query)
                  );
                })
                .slice(0, 50)
                .map((coin) => {
                  const pair = `${coin.symbol}/USDT`;
                  const isSelected = selectedPair === pair;
                  const priceChange = coin.priceChangePercentage24h || 0;
                  const isPositive = priceChange >= 0;

                  return (
                    <button
                      key={coin.id}
                      onClick={() => {
                        setSelectedPair(pair);
                        setSearchParams({ pair });
                        setShowCoinSelector(false);
                        setCoinSearchQuery('');
                      }}
                      className={`w-full flex items-center gap-4 p-4 rounded-lg border transition-all duration-200 hover:bg-gray-800/50 ${
                        isSelected
                          ? 'bg-emerald-500/10 border-emerald-500/50 shadow-lg shadow-emerald-500/10'
                          : 'bg-gray-800/30 border-gray-700 hover:border-gray-600'
                      }`}
                    >
                      <CoinIcon
                        symbol={coin.symbol}
                        image={coin.image}
                        size="md"
                      />
                      <div className="flex-1 text-left">
                        <div className="flex items-center gap-2">
                          <span className="font-semibold text-white">
                            {coin.symbol}
                          </span>
                          <span className="text-gray-400 text-sm">
                            {coin.name}
                          </span>
                        </div>
                        <div className="flex items-center gap-2 mt-1">
                          <span className="text-white font-medium">
                            {formatPrice(coin.currentPrice)}
                          </span>
                          <Badge
                            className={
                              isPositive
                                ? 'bg-emerald-500/10 text-emerald-500'
                                : 'bg-red-500/10 text-red-500'
                            }
                          >
                            {isPositive ? (
                              <TrendingUp className="w-3 h-3 mr-1" />
                            ) : (
                              <TrendingDown className="w-3 h-3 mr-1" />
                            )}
                            {isPositive ? '+' : ''}
                            {priceChange.toFixed(2)}%
                          </Badge>
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-white font-semibold">
                          {pair}
                        </div>
                        {isSelected && (
                          <div className="text-emerald-500 text-xs mt-1">
                            Đã chọn
                          </div>
                        )}
                      </div>
                    </button>
                  );
                })}
              {pairs.filter((coin) => {
                const query = coinSearchQuery.toLowerCase();
                return (
                  coin.symbol.toLowerCase().includes(query) ||
                  coin.name.toLowerCase().includes(query)
                );
              }).length === 0 && (
                <div className="text-center py-8 text-gray-400">
                  Không tìm thấy coin nào
                </div>
              )}
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
