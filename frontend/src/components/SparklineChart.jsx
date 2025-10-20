import React from 'react';

const SparklineChart = ({ data, width = 100, height = 30, color = '#10b981' }) => {
  if (!data || data.length === 0) {
    return <div style={{ width, height, backgroundColor: '#f3f4f6', borderRadius: '4px' }} />;
  }

  // Normalize data to fit within the chart dimensions
  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;
  
  const points = data.map((value, index) => {
    const x = (index / (data.length - 1)) * (width - 2);
    const y = height - 2 - ((value - min) / range) * (height - 4);
    return `${x},${y}`;
  }).join(' ');

  return (
    <svg width={width} height={height} style={{ display: 'block' }}>
      <polyline
        points={points}
        fill="none"
        stroke={color}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </svg>
  );
};

export default SparklineChart;

