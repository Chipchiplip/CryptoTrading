# 📊 Premium Fintech Charts

Dark-themed, neon-glowing chart components built with React + Recharts.

## 🎨 Features

- **Dark Fintech Theme** - Binance/Coinbase Pro inspired
- **Neon Glow Effects** - Green for NAV, Red/Green for PnL
- **Glassmorphism Tooltips** - Blurred background with transparency
- **Smooth Curves** - Monotone bezier interpolation
- **Responsive** - Auto-adapts to container size
- **Premium Styling** - Ultra-thin grids, custom formatting

## 📦 Components

### 1. ChartCard
Wrapper component with glassmorphism card styling.

```tsx
import { ChartCard } from './components/charts';

<ChartCard 
  title="Net Asset Value (NAV)" 
  subtitle="Last 30 days performance"
  action={<button>View Portfolio</button>}
>
  {/* Chart content */}
</ChartCard>
```

### 2. NavChart
30-day Net Asset Value area chart with green neon glow.

```tsx
import { NavChart, mockNavData } from './components/charts';

<NavChart data={mockNavData} />
```

**Data Format:**
```typescript
interface NavDataPoint {
  date: string;  // "YYYY-MM-DD"
  value: number; // NAV in USD
}
```

### 3. TodayPnlChart
Hourly Profit & Loss line chart with conditional coloring.

```tsx
import { TodayPnlChart, mockPnlData } from './components/charts';

<TodayPnlChart data={mockPnlData} />
```

**Data Format:**
```typescript
interface PnlDataPoint {
  time: string;  // "HH:00"
  pnl: number;   // Profit/Loss in USD (can be negative)
}
```

## 🚀 Full Usage Example

```tsx
import { ChartCard, NavChart, TodayPnlChart, mockNavData, mockPnlData } from './components/charts';

function Dashboard() {
  return (
    <div className="grid lg:grid-cols-3 gap-6">
      {/* NAV Chart */}
      <div className="lg:col-span-2">
        <ChartCard 
          title="Net Asset Value (NAV)" 
          subtitle="Last 30 days performance - Tổng giá trị tài sản ròng (tất cả crypto + USD) theo thời gian"
          action={
            <button className="bg-emerald-500 text-black px-4 py-2 rounded-lg">
              View Portfolio
            </button>
          }
        >
          <NavChart data={mockNavData} />
        </ChartCard>
      </div>

      {/* PnL Chart */}
      <div>
        <ChartCard 
          title="Today's PnL" 
          subtitle="Hourly breakdown - Lợi nhuận/lỗ theo từng giờ trong ngày"
        >
          <TodayPnlChart data={mockPnlData} />
        </ChartCard>
      </div>
    </div>
  );
}
```

## 🎨 Styling Details

### Colors
- **NAV Line**: `#00FFA3` → `#00D884` (Green gradient)
- **PnL Profit**: `#00FFA3` (Neon green)
- **PnL Loss**: `#FF4D4D` (Neon red)
- **Background**: `rgba(13, 17, 23, 0.95)`
- **Grid**: `rgba(255, 255, 255, 0.08)`

### Effects
- **Line Glow**: CSS `filter: blur(3px)` with merge
- **Tooltip**: `backdrop-filter: blur(12px)` glassmorphism
- **Card Border**: Subtle glow on hover
- **Dots**: Highlighted for extreme values (>$50K)

### Typography
- **Font**: Inter, SF Pro, system fonts
- **Title**: 18px, weight 600
- **Subtitle**: 13px, 50% opacity
- **Axis Labels**: 11px, 50% opacity

## 📱 Responsive

All charts use `ResponsiveContainer` and adapt to parent width.

## 🔧 Dependencies

```json
{
  "react": "^18.0.0",
  "recharts": "^2.10.0"
}
```

## 💡 Tips

1. **Custom Data**: Replace `mockNavData` and `mockPnlData` with your API data
2. **Height**: Adjust chart height in `ResponsiveContainer` (default: 280px)
3. **Colors**: Modify gradient colors in component `<defs>` section
4. **Glow Intensity**: Adjust `stdDeviation` in `feGaussianBlur` filter

## 🎯 Performance

- Charts use `animationDuration={1000}` for smooth entry
- Tooltips render on-demand only
- Grid lines optimized with `vertical={false}`

---

**Created with ❤️ for premium fintech UIs**
