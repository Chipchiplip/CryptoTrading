import React, { useState, useEffect, useLayoutEffect, useRef } from 'react';
import { createChart, ColorType, IChartApi, ISeriesApi, CandlestickSeries } from 'lightweight-charts';
import { MarketApi, CandlestickData, Crypto } from '../../../api/market';
import { Card } from '../../ui/card';
import { Button } from '../../ui/button';
import { Input } from '../../ui/input';
import { Label } from '../../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
import { Loader2 } from 'lucide-react';

export default function ChartTest() {
  const [symbol, setSymbol] = useState('BTCUSDT');
  const [interval, setInterval] = useState('1h');
  const [candles, setCandles] = useState<CandlestickData[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [rawData, setRawData] = useState<any>(null);
  const [marketInfo, setMarketInfo] = useState<Crypto | null>(null);
  const [cryptoList, setCryptoList] = useState<Crypto[]>([]);

  const chartContainerRef = useRef<HTMLDivElement>(null);
  const chartRef = useRef<IChartApi | null>(null);
  const seriesRef = useRef<ISeriesApi<'Candlestick'> | null>(null);

  // Fetch crypto list
  useEffect(() => {
    const fetchCryptos = async () => {
      const res = await MarketApi.getCryptocurrencies();
      if (res.ok) {
        setCryptoList(res.data);
      }
    };
    fetchCryptos();
  }, []);

  // Initialize chart - only once on mount
  useLayoutEffect(() => {
    if (!chartContainerRef.current) {
      console.log('[ChartTest] Container ref is null');
      return;
    }

    const container = chartContainerRef.current;
    let resizeHandler: (() => void) | null = null;

    // Wait for container to have proper size
    const initChart = () => {
      if (!chartContainerRef.current) {
        return; // Container removed
      }
      
      // Don't reinitialize if chart already exists
      if (chartRef.current) {
        console.log('[ChartTest] Chart already initialized, skipping');
        return;
      }

      const rect = container.getBoundingClientRect();
      const width = rect.width || container.clientWidth;
      const height = rect.height || container.clientHeight || 400;

      if (width === 0 || height === 0) {
        setTimeout(initChart, 100);
        return;
      }

      const finalWidth = width > 0 ? width : 800;
      const finalHeight = height > 0 ? height : 400;

      console.log('[ChartTest] Initializing chart with size:', finalWidth, 'x', finalHeight);

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

        console.log('[ChartTest] Chart created, adding series...');
        console.log('[ChartTest] Chart instance:', chart);
        console.log('[ChartTest] Container children:', container.children.length);

        // Add candlestick series - Use CandlestickSeries class for version 5.x
        // In v5.x, you need to import CandlestickSeries and use it
        let candlestickSeries: ISeriesApi<'Candlestick'>;
        
        try {
          console.log('[ChartTest] CandlestickSeries:', CandlestickSeries);
          candlestickSeries = chart.addSeries(CandlestickSeries, {
            upColor: '#22c55e',
            downColor: '#ef4444',
            borderVisible: false,
            wickUpColor: '#22c55e',
            wickDownColor: '#ef4444',
          }) as ISeriesApi<'Candlestick'>;
          
          console.log('[ChartTest] Series created successfully:', candlestickSeries);
        } catch (err: any) {
          console.error('[ChartTest] Error creating series:', err);
          console.error('[ChartTest] Error message:', err?.message);
          console.error('[ChartTest] Error stack:', err?.stack);
          throw err;
        }

        chartRef.current = chart;
        seriesRef.current = candlestickSeries;

        console.log('[ChartTest] Chart initialized successfully');
        console.log('[ChartTest] Chart object:', chart);
        console.log('[ChartTest] Series object:', candlestickSeries);

        // Handle resize
        resizeHandler = () => {
          if (chartContainerRef.current && chartRef.current) {
            const newWidth = chartContainerRef.current.clientWidth || 800;
            chartRef.current.applyOptions({ width: newWidth });
          }
        };

        window.addEventListener('resize', resizeHandler);
      } catch (error) {
        console.error('[ChartTest] Error initializing chart:', error);
        console.error('[ChartTest] Error details:', error);
      }
    };

    // Small delay to ensure container is ready
    setTimeout(initChart, 100);

    return () => {
      console.log('[ChartTest] Cleanup: removing chart');
      
      if (resizeHandler) {
        window.removeEventListener('resize', resizeHandler);
        resizeHandler = null;
      }
      
      if (chartRef.current) {
        try {
          // Simply call remove() - lightweight-charts handles cleanup internally
          chartRef.current.remove();
        } catch (error) {
          console.warn('[ChartTest] Error removing chart (this is usually safe to ignore):', error);
        }
        
        chartRef.current = null;
        seriesRef.current = null;
      }
    };
  }, []); // Only run once on mount

  // Update chart when candles change
  useEffect(() => {
    if (!seriesRef.current || candles.length === 0) {
      return;
    }

    console.log('[ChartTest] Updating chart with', candles.length, 'candles');
    
    try {
      const formattedData = candles.map(c => ({
        time: c.time as any,
        open: Number(c.open) || 0,
        high: Number(c.high) || 0,
        low: Number(c.low) || 0,
        close: Number(c.close) || 0,
      })).filter(c => c.time > 0 && c.open > 0 && c.high > 0 && c.low > 0 && c.close > 0);

      console.log('[ChartTest] Formatted data sample:', formattedData[0]);
      
      seriesRef.current.setData(formattedData);
      
      if (chartRef.current) {
        chartRef.current.timeScale().fitContent();
      }
      
      console.log('[ChartTest] Chart data updated successfully');
    } catch (error) {
      console.error('[ChartTest] Error updating chart data:', error);
    }
  }, [candles]);

  // Fetch candles data
  const fetchCandles = async () => {
    setLoading(true);
    setError(null);
    setRawData(null);

    try {
      const res = await MarketApi.getCandles(symbol, interval);
      
      // Store raw response
      setRawData(res);

      if (res.ok && res.data && res.data.length > 0) {
        const formattedData = res.data.map((candle: CandlestickData) => ({
          time: typeof candle.time === 'number' 
            ? candle.time 
            : Math.floor(new Date(candle.time as any).getTime() / 1000),
          open: Number(candle.open) || 0,
          high: Number(candle.high) || 0,
          low: Number(candle.low) || 0,
          close: Number(candle.close) || 0,
        })).filter(c => c.time > 0 && c.open > 0);

        setCandles(formattedData);
        console.log('✅ Chart data loaded:', formattedData.length, 'candles');
      } else {
        setError(res.error || 'No data received');
        setCandles([]);
      }

      // Fetch market info
      const baseSymbol = symbol.replace('USDT', '').replace('USD', '');
      const marketRes = await MarketApi.getCryptocurrency(baseSymbol);
      if (marketRes.ok && marketRes.data) {
        setMarketInfo(marketRes.data);
      } else {
        console.log('[ChartTest] Failed to fetch market info:', marketRes.error);
        setMarketInfo(null);
      }
    } catch (e: any) {
      setError(e.message || 'Failed to fetch data');
      setCandles([]);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6 space-y-6 bg-black min-h-screen">
      <div className="max-w-7xl mx-auto">
        <h1 className="text-3xl font-bold text-white mb-6">Chart Test Page</h1>

        {/* Controls */}
        <Card className="p-4 mb-6 bg-gray-900 border-gray-800">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <Label htmlFor="symbol" className="text-white">Symbol</Label>
              <Input
                id="symbol"
                value={symbol}
                onChange={(e) => setSymbol(e.target.value.toUpperCase())}
                placeholder="BTCUSDT"
                className="bg-gray-800 border-gray-700 text-white"
              />
            </div>
            <div>
              <Label htmlFor="interval" className="text-white">Interval</Label>
              <Select value={interval} onValueChange={setInterval}>
                <SelectTrigger className="bg-gray-800 border-gray-700 text-white">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="1m">1 Minute</SelectItem>
                  <SelectItem value="5m">5 Minutes</SelectItem>
                  <SelectItem value="15m">15 Minutes</SelectItem>
                  <SelectItem value="1h">1 Hour</SelectItem>
                  <SelectItem value="4h">4 Hours</SelectItem>
                  <SelectItem value="1d">1 Day</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-end">
              <Button 
                onClick={fetchCandles} 
                disabled={loading}
                className="w-full bg-emerald-500 hover:bg-emerald-600"
              >
                {loading ? (
                  <>
                    <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                    Loading...
                  </>
                ) : (
                  'Fetch Data'
                )}
              </Button>
            </div>
          </div>
        </Card>

        {/* Error */}
        {error && (
          <Card className="p-4 mb-6 bg-red-900/20 border-red-800">
            <p className="text-red-400">Error: {error}</p>
          </Card>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Chart */}
          <div className="lg:col-span-2">
            <Card className="p-4 bg-gray-900 border-gray-800">
              <h2 className="text-xl font-semibold text-white mb-4">Candlestick Chart</h2>
              <div 
                ref={(el) => {
                  chartContainerRef.current = el;
                  if (el) {
                    console.log('[ChartTest] Container ref set, size:', el.clientWidth, 'x', el.clientHeight);
                  }
                }}
                className="w-full bg-black"
                style={{ height: '400px', minHeight: '400px', width: '100%', position: 'relative' }}
              >
                {candles.length === 0 && !loading && (
                  <div className="flex items-center justify-center h-full text-gray-400">
                    <p>Click "Fetch Data" to load chart</p>
                  </div>
                )}
              </div>
              {candles.length > 0 && (
                <p className="text-sm text-gray-400 mt-2">
                  Loaded {candles.length} candles
                </p>
              )}
            </Card>
          </div>

          {/* Market Info */}
          <div className="space-y-6">
            <Card className="p-4 bg-gray-900 border-gray-800">
              <h2 className="text-xl font-semibold text-white mb-4">Market Details</h2>
              {marketInfo ? (
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-gray-400">Symbol:</span>
                    <span className="text-white font-semibold">{marketInfo.symbol || 'N/A'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Name:</span>
                    <span className="text-white">{marketInfo.name || 'N/A'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Price:</span>
                    <span className="text-white font-semibold">
                      ${marketInfo.currentPrice ? marketInfo.currentPrice.toLocaleString('vi-VN') : 'N/A'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">24h Change:</span>
                    <span className={marketInfo.priceChangePercentage24h >= 0 ? 'text-green-400' : 'text-red-400'}>
                      {marketInfo.priceChangePercentage24h !== undefined && marketInfo.priceChangePercentage24h !== null
                        ? `${marketInfo.priceChangePercentage24h >= 0 ? '+' : ''}${marketInfo.priceChangePercentage24h.toFixed(2)}%`
                        : 'N/A'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Market Cap:</span>
                    <span className="text-white">
                      ${marketInfo.marketCap ? (marketInfo.marketCap / 1e9).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + 'B' : 'N/A'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Volume:</span>
                    <span className="text-white">
                      ${marketInfo.totalVolume ? (marketInfo.totalVolume / 1e9).toLocaleString('vi-VN', { maximumFractionDigits: 2 }) + 'B' : 'N/A'}
                    </span>
                  </div>
                </div>
              ) : (
                <p className="text-gray-400 text-sm">No market data loaded</p>
              )}
            </Card>

            {/* Data Stats */}
            {candles.length > 0 && (
              <Card className="p-4 bg-gray-900 border-gray-800">
                <h2 className="text-xl font-semibold text-white mb-4">Data Stats</h2>
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-gray-400">Total Candles:</span>
                    <span className="text-white">{candles.length}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">First Candle:</span>
                    <span className="text-white text-xs">
                      {candles[0]?.time ? new Date(candles[0].time * 1000).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : 'N/A'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Last Candle:</span>
                    <span className="text-white text-xs">
                      {candles[candles.length - 1]?.time ? new Date(candles[candles.length - 1].time * 1000).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : 'N/A'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Highest:</span>
                    <span className="text-green-400">
                      ${candles.length > 0 ? Math.max(...candles.map(c => c.high || 0)).toLocaleString('vi-VN') : 'N/A'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Lowest:</span>
                    <span className="text-red-400">
                      ${candles.length > 0 ? Math.min(...candles.map(c => c.low || 0)).toLocaleString('vi-VN') : 'N/A'}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-gray-400">Current:</span>
                    <span className="text-white font-semibold">
                      ${candles[candles.length - 1]?.close ? candles[candles.length - 1].close.toLocaleString('vi-VN') : 'N/A'}
                    </span>
                  </div>
                </div>
              </Card>
            )}
          </div>
        </div>

        {/* Raw Data */}
        {rawData && (
          <Card className="p-4 bg-gray-900 border-gray-800 mt-6">
            <h2 className="text-xl font-semibold text-white mb-4">Raw API Response</h2>
            <div className="bg-black p-4 rounded overflow-auto max-h-96">
              <pre className="text-xs text-gray-300">
                {JSON.stringify(rawData, null, 2)}
              </pre>
            </div>
          </Card>
        )}

        {/* Sample Candles */}
        {candles.length > 0 && (
          <Card className="p-4 bg-gray-900 border-gray-800 mt-6">
            <h2 className="text-xl font-semibold text-white mb-4">Sample Candles (First 5)</h2>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-700">
                    <th className="text-left p-2 text-gray-400">Time</th>
                    <th className="text-right p-2 text-gray-400">Open</th>
                    <th className="text-right p-2 text-gray-400">High</th>
                    <th className="text-right p-2 text-gray-400">Low</th>
                    <th className="text-right p-2 text-gray-400">Close</th>
                  </tr>
                </thead>
                <tbody>
                  {candles.slice(0, 5).map((candle, idx) => (
                    <tr key={idx} className="border-b border-gray-800">
                      <td className="p-2 text-gray-300 text-xs">
                        {candle.time ? new Date(candle.time * 1000).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : 'N/A'}
                      </td>
                      <td className="p-2 text-right text-white">${candle.open ? candle.open.toLocaleString('vi-VN') : 'N/A'}</td>
                      <td className="p-2 text-right text-green-400">${candle.high ? candle.high.toLocaleString('vi-VN') : 'N/A'}</td>
                      <td className="p-2 text-right text-red-400">${candle.low ? candle.low.toLocaleString('vi-VN') : 'N/A'}</td>
                      <td className="p-2 text-right text-white">${candle.close ? candle.close.toLocaleString('vi-VN') : 'N/A'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        )}
      </div>
    </div>
  );
}

