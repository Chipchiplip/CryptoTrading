import React, { useState } from 'react';

interface CoinIconProps {
  image?: string | null;
  symbol: string;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeClasses = {
  sm: { container: 'w-6 h-6', text: 'text-xs' },
  md: { container: 'w-8 h-8', text: 'text-xs' },
  lg: { container: 'w-10 h-10', text: 'text-sm' },
};

export function CoinIcon({ image, symbol, size = 'md', className = '' }: CoinIconProps) {
  const [imageError, setImageError] = useState(false);
  const sizeClass = sizeClasses[size];
  const displaySymbol = (symbol || '').toUpperCase();

  // Style giống Binance: nền teal/green đậm, text trắng
  if (image && !imageError) {
    return (
      <img
        src={image}
        alt={symbol}
        className={`${sizeClass.container} object-cover rounded-full flex-shrink-0 ${className}`}
        loading="lazy"
        onError={() => setImageError(true)}
      />
    );
  }

  // Hiển thị full symbol (BTC, ETH, etc.) hoặc chỉ chữ cái đầu nếu quá dài
  const displayText = displaySymbol.length <= 4 ? displaySymbol : displaySymbol.charAt(0);
  
  return (
    <div
      className={`${sizeClass.container} bg-teal-600 rounded-full flex items-center justify-center flex-shrink-0 ${className}`}
      style={{ backgroundColor: '#0d9488' }} // teal-600 - giống Binance
    >
      <span className={`text-white font-bold ${sizeClass.text}`}>{displayText}</span>
    </div>
  );
}

