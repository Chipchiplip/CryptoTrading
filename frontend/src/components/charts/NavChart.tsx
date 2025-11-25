import React from 'react';
import {
    ResponsiveContainer,
    AreaChart,
    Area,
    XAxis,
    YAxis,
    CartesianGrid,
    Tooltip
} from 'recharts';

interface NavDataPoint {
    date: string;
    value: number;
}

interface NavChartProps {
    data: NavDataPoint[];
}

/**
 * Format large numbers with K/M suffix
 */
const formatCompactNumber = (value: number): string => {
    if (value >= 1000000) return `$${(value / 1000000).toFixed(1)}M`;
    if (value >= 1000) return `$${(value / 1000).toFixed(0)}K`;
    return `$${value.toFixed(0)}`;
};

/**
 * Format date for X-axis (MM-DD)
 */
const formatDate = (dateStr: string): string => {
    const date = new Date(dateStr);
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const day = date.getDate().toString().padStart(2, '0');
    return `${month}-${day}`;
};

/**
 * Custom Tooltip with glassmorphism effect
 */
const CustomTooltip = ({ active, payload, label }: any) => {
    if (!active || !payload || !payload.length) return null;

    const value = payload[0].value;
    const date = new Date(label);
    const formattedDate = date.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    });

    const tooltipStyle: React.CSSProperties = {
        background: 'rgba(13, 17, 23, 0.92)',
        backdropFilter: 'blur(12px)',
        border: '1px solid rgba(0, 255, 163, 0.2)',
        borderRadius: '12px',
        padding: '12px 16px',
        boxShadow: '0 8px 32px rgba(0, 0, 0, 0.6), 0 0 20px rgba(0, 255, 163, 0.15)',
    };

    const labelStyle: React.CSSProperties = {
        fontSize: '12px',
        color: 'rgba(255, 255, 255, 0.6)',
        marginBottom: '4px',
        fontWeight: 500,
    };

    const valueStyle: React.CSSProperties = {
        fontSize: '16px',
        color: '#00FFA3',
        fontWeight: 700,
        letterSpacing: '-0.02em',
        textShadow: '0 0 10px rgba(0, 255, 163, 0.4)',
    };

    return (
        <div style={tooltipStyle}>
            <div style={labelStyle}>{formattedDate}</div>
            <div style={valueStyle}>
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
 * Premium NAV Chart Component
 * Features: Neon green glow, smooth curves, area gradient
 */
export const NavChart: React.FC<NavChartProps> = ({ data }) => {
    const wrapperStyle: React.CSSProperties = {
        position: 'relative',
        width: '100%',
    };

    return (
        <div style={wrapperStyle}>
            <ResponsiveContainer width="100%" height={280}>
                <AreaChart
                    data={data}
                    margin={{ top: 10, right: 10, left: 0, bottom: 0 }}
                >
                    {/* Gradient Definitions */}
                    <defs>
                        <linearGradient id="navGradient" x1="0" y1="0" x2="0" y2="1">
                            <stop offset="0%" stopColor="#00FFA3" stopOpacity={0.22} />
                            <stop offset="50%" stopColor="#00D884" stopOpacity={0.12} />
                            <stop offset="100%" stopColor="#00D884" stopOpacity={0.02} />
                        </linearGradient>

                        {/* Glow filter for line */}
                        <filter id="navGlow" x="-50%" y="-50%" width="200%" height="200%">
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

                    {/* X-Axis */}
                    <XAxis
                        dataKey="date"
                        tickFormatter={formatDate}
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

                    {/* Area with gradient fill */}
                    <Area
                        type="monotone"
                        dataKey="value"
                        stroke="#00FFA3"
                        strokeWidth={2.5}
                        fill="url(#navGradient)"
                        filter="url(#navGlow)"
                        animationDuration={1000}
                        animationEasing="ease-out"
                    />
                </AreaChart>
            </ResponsiveContainer>
        </div>
    );
};

export default NavChart;
