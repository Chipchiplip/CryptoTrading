import React from 'react';
import {
    ResponsiveContainer,
    LineChart,
    Line,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip,
    ReferenceLine
} from 'recharts';

interface PnlDataPoint {
    time: string;
    pnl: number;
}

interface TodayPnlChartProps {
    data: PnlDataPoint[];
}

/**
 * Format large numbers with K/M suffix (supports negative)
 */
const formatCompactNumber = (value: number): string => {
    const abs = Math.abs(value);
    const sign = value < 0 ? '-' : '';

    if (abs >= 1000000) return `${sign}$${(abs / 1000000).toFixed(1)}M`;
    if (abs >= 1000) return `${sign}$${(abs / 1000).toFixed(0)}K`;
    return `${sign}$${abs.toFixed(0)}`;
};

/**
 * Custom Tooltip with glassmorphism
 */
const CustomTooltip = ({ active, payload, label }: any) => {
    if (!active || !payload || !payload.length) return null;

    const value = payload[0].value;
    const isProfit = value >= 0;

    const tooltipStyle: React.CSSProperties = {
        background: 'rgba(13, 17, 23, 0.92)',
        backdropFilter: 'blur(12px)',
        border: `1px solid ${isProfit ? 'rgba(0, 255, 163, 0.2)' : 'rgba(255, 77, 77, 0.2)'}`,
        borderRadius: '12px',
        padding: '12px 16px',
        boxShadow: isProfit
            ? '0 8px 32px rgba(0, 0, 0, 0.6), 0 0 20px rgba(0, 255, 163, 0.15)'
            : '0 8px 32px rgba(0, 0, 0, 0.6), 0 0 20px rgba(255, 77, 77, 0.15)',
    };

    const labelStyle: React.CSSProperties = {
        fontSize: '12px',
        color: 'rgba(255, 255, 255, 0.6)',
        marginBottom: '4px',
        fontWeight: 500,
    };

    const valueStyle: React.CSSProperties = {
        fontSize: '16px',
        color: isProfit ? '#00FFA3' : '#FF4D4D',
        fontWeight: 700,
        letterSpacing: '-0.02em',
        textShadow: isProfit
            ? '0 0 10px rgba(0, 255, 163, 0.4)'
            : '0 0 10px rgba(255, 77, 77, 0.4)',
    };

    return (
        <div style={tooltipStyle}>
            <div style={labelStyle}>{label}</div>
            <div style={valueStyle}>
                {value >= 0 ? '+' : ''}
                {new Intl.NumberFormat('en-US', {
                    style: 'currency',
                    currency: 'USD',
                    minimumFractionDigits: 2,
                    maximumFractionDigits: 2
                }).format(value)}
            </div>
        </div>
    );
};

/**
 * Custom Dot for highlighting extreme points
 */
const CustomDot = (props: any) => {
    const { cx, cy, payload } = props;

    // Highlight dots with extreme values (spikes/dips)
    const isExtreme = Math.abs(payload.pnl) > 50000;

    if (!isExtreme) return null;

    const isNegative = payload.pnl < 0;

    return (
        <g>
            {/* Outer glow */}
            <circle
                cx={cx}
                cy={cy}
                r={8}
                fill={isNegative ? 'rgba(255, 77, 77, 0.2)' : 'rgba(0, 255, 163, 0.2)'}
                filter="blur(4px)"
            />
            {/* Inner dot */}
            <circle
                cx={cx}
                cy={cy}
                r={4}
                fill={isNegative ? '#FF4D4D' : '#00FFA3'}
                stroke={isNegative ? '#FF6B6B' : '#00FFB3'}
                strokeWidth={2}
            />
        </g>
    );
};

/**
 * Premium Today's PnL Chart
 * Features: Red neon glow, zero-line emphasis, spike highlighting
 */
export const TodayPnlChart: React.FC<TodayPnlChartProps> = ({ data }) => {
    // Determine overall trend
    const totalPnl = data.reduce((sum, d) => sum + d.pnl, 0);
    const isProfit = totalPnl >= 0;

    const wrapperStyle: React.CSSProperties = {
        position: 'relative',
        width: '100%',
    };

    return (
        <div style={wrapperStyle}>
            <ResponsiveContainer width="100%" height={280}>
                <LineChart
                    data={data}
                    margin={{ top: 10, right: 10, left: 0, bottom: 0 }}
                >
                    {/* Gradient & Glow Definitions */}
                    <defs>
                        <filter id="pnlGlow" x="-50%" y="-50%" width="200%" height="200%">
                            <feGaussianBlur stdDeviation="3" result="coloredBlur" />
                            <feMerge>
                                <feMergeNode in="coloredBlur" />
                                <feMergeNode in="SourceGraphic" />
                            </feMerge>
                        </filter>
                    </defs>

                    {/* Ultra-thin grid */}
                    <CartesianGrid
                        strokeDasharray="3 3"
                        stroke="rgba(255, 255, 255, 0.08)"
                        vertical={false}
                    />

                    {/* Emphasized Zero Line */}
                    <ReferenceLine
                        y={0}
                        stroke="rgba(255, 255, 255, 0.25)"
                        strokeWidth={1.5}
                        strokeDasharray="5 5"
                        label={{
                            value: 'Break Even',
                            fill: 'rgba(255, 255, 255, 0.4)',
                            fontSize: 10,
                            position: 'insideTopRight'
                        }}
                    />

                    {/* X-Axis */}
                    <XAxis
                        dataKey="time"
                        stroke="rgba(255, 255, 255, 0.3)"
                        tick={{ fill: 'rgba(255, 255, 255, 0.5)', fontSize: 11 }}
                        axisLine={{ stroke: 'rgba(255, 255, 255, 0.1)' }}
                        tickLine={false}
                    />

                    {/* Y-Axis */}
                    <YAxis
                        tickFormatter={formatCompactNumber}
                        stroke="rgba(255, 255, 255, 0.3)"
                        tick={{ fill: 'rgba(255, 255, 255, 0.5)', fontSize: 11 }}
                        axisLine={false}
                        tickLine={false}
                        width={60}
                    />

                    {/* Custom Tooltip */}
                    <Tooltip content={<CustomTooltip />} cursor={false} />

                    {/* PnL Line with conditional color */}
                    <Line
                        type="monotone"
                        dataKey="pnl"
                        stroke={isProfit ? '#00FFA3' : '#FF4D4D'}
                        strokeWidth={2.5}
                        dot={<CustomDot />}
                        activeDot={{
                            r: 6,
                            fill: isProfit ? '#00FFA3' : '#FF4D4D',
                            stroke: '#fff',
                            strokeWidth: 2
                        }}
                        filter="url(#pnlGlow)"
                        animationDuration={1000}
                        animationEasing="ease-out"
                    />
                </LineChart>
            </ResponsiveContainer>
        </div>
    );
};

export default TodayPnlChart;
