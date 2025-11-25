import React from 'react';

interface ChartCardProps {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  action?: React.ReactNode;
  className?: string;
}

/**
 * Premium dark-themed card wrapper for charts
 * Features: glassmorphism, subtle glow, rounded corners
 */
export const ChartCard: React.FC<ChartCardProps> = ({
  title,
  subtitle,
  children,
  action,
  className = ''
}) => {
  const cardStyle: React.CSSProperties = {
    background: 'rgba(13, 17, 23, 0.95)',
    border: '1px solid rgba(255, 255, 255, 0.06)',
    borderRadius: '20px',
    padding: '24px',
    backdropFilter: 'blur(10px)',
    boxShadow: '0 4px 24px rgba(0, 0, 0, 0.4), 0 0 1px rgba(0, 255, 163, 0.1)',
    transition: 'all 0.3s ease',
  };

  const headerStyle: React.CSSProperties = {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: '20px',
  };

  const titleStyle: React.CSSProperties = {
    fontSize: '18px',
    fontWeight: 600,
    color: '#ffffff',
    margin: '0 0 4px 0',
    fontFamily: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    letterSpacing: '-0.02em',
  };

  const subtitleStyle: React.CSSProperties = {
    fontSize: '13px',
    color: 'rgba(255, 255, 255, 0.5)',
    margin: 0,
    fontWeight: 400,
  };

  const contentStyle: React.CSSProperties = {
    position: 'relative',
    minHeight: '250px',
  };

  return (
    <div style={cardStyle} className={className}>
      {/* Header */}
      <div style={headerStyle}>
        <div>
          <h3 style={titleStyle}>{title}</h3>
          {subtitle && <p style={subtitleStyle}>{subtitle}</p>}
        </div>
        {action && <div style={{ flexShrink: 0 }}>{action}</div>}
      </div>

      {/* Chart Content */}
      <div style={contentStyle}>
        {children}
      </div>
    </div>
  );
};

export default ChartCard;
