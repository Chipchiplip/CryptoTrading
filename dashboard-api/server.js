require('dotenv').config();
const express = require('express');
const mysql = require('mysql2/promise');
const cors = require('cors');

const app = express();
app.use(cors());
app.use(express.json());

// MySQL connection pool
const pool = mysql.createPool({
  host: process.env.DB_HOST || 'localhost',
  port: process.env.DB_PORT || 3306,
  user: process.env.DB_USER || 'root',
  password: process.env.DB_PASS || '',
  database: process.env.DB_NAME || 'crypto_trading',
  waitForConnections: true,
  connectionLimit: 10,
  queueLimit: 0
});

// Helper: Get 0 if null
const zeroIfNull = (val) => val ?? 0;

// Helper: Format date to YYYY-MM-DD
const formatDate = (date) => {
  const d = new Date(date);
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

// =====================================================================
// GET /dashboard/summary?userId=8
// =====================================================================
app.get('/dashboard/summary', async (req, res) => {
  try {
    const userId = parseInt(req.query.userId) || 0;
    if (!userId) {
      return res.status(400).json({ error: 'userId parameter is required' });
    }

    // 1. NAV Total (from V_PortfolioNav view or calculated)
    const [navRows] = await pool.execute(`
      SELECT 
        p.UserId,
        SUM(p.QtyCoin * COALESCE(cp.PriceUsd, 0)) AS PortfolioUsd
      FROM (
        SELECT 
          o.UserId,
          t.CryptocurrencyId,
          ROUND(SUM(CASE 
            WHEN o.Side = 'BUY' THEN t.QuantityCoin
            WHEN o.Side = 'SELL' THEN -t.QuantityCoin
            ELSE 0 
          END), 18) AS QtyCoin
        FROM Trades t
        JOIN Orders o ON o.Id = t.OrderId
        WHERE o.UserId = ?
        GROUP BY o.UserId, t.CryptocurrencyId
        HAVING QtyCoin <> 0
      ) p
      LEFT JOIN (
        SELECT CryptocurrencyId, PriceUsd
        FROM CryptoPrices
        WHERE (CryptocurrencyId, CollectedAtUtc) IN (
          SELECT CryptocurrencyId, MAX(CollectedAtUtc) AS MaxTs
          FROM CryptoPrices
          GROUP BY CryptocurrencyId
        )
      ) cp ON cp.CryptocurrencyId = p.CryptocurrencyId
      WHERE p.UserId = ?
      GROUP BY p.UserId
    `, [userId, userId]);

    const navTotal = zeroIfNull(navRows[0]?.PortfolioUsd);

    // 2. Today's PnL (from Trades)
    const [pnlRows] = await pool.execute(`
      SELECT 
        SUM(CASE 
          WHEN o.Side = 'SELL' THEN (t.PriceUsd * t.QuantityCoin - t.FeeUsd)
          ELSE -(t.PriceUsd * t.QuantityCoin + t.FeeUsd)
        END) AS TodayPnl
      FROM Trades t
      JOIN Orders o ON o.Id = t.OrderId
      WHERE o.UserId = ?
        AND DATE(t.CreatedAt) = CURRENT_DATE()
    `, [userId]);

    const todayPnl = zeroIfNull(pnlRows[0]?.TodayPnl);

    // 3. Available Balance (USD from V_WalletBalances)
    const [balanceRows] = await pool.execute(`
      SELECT 
        COALESCE(SUM(m.Amount), 0) AS Balance
      FROM Wallets w
      LEFT JOIN WalletMovements m ON m.WalletId = w.Id
      WHERE w.UserId = ?
        AND w.AssetType = 'FIAT'
        AND w.CurrencyCode = 'USD'
      GROUP BY w.Id
      LIMIT 1
    `, [userId]);

    const availableBalance = zeroIfNull(balanceRows[0]?.Balance);

    // 4. Open Orders Count
    const [orderRows] = await pool.execute(`
      SELECT COUNT(*) AS OpenOrders
      FROM Orders
      WHERE UserId = ?
        AND Status IN ('NEW', 'PARTIAL')
    `, [userId]);

    const openOrdersCount = zeroIfNull(orderRows[0]?.OpenOrders);

    res.json({
      navTotal: parseFloat(navTotal) || 0,
      todayPnl: parseFloat(todayPnl) || 0,
      availableBalance: parseFloat(availableBalance) || 0,
      openOrdersCount: parseInt(openOrdersCount) || 0
    });
  } catch (error) {
    console.error('Error in /dashboard/summary:', error);
    res.status(500).json({ error: error.message });
  }
});

// =====================================================================
// GET /dashboard/nav?userId=8&days=30
// =====================================================================
app.get('/dashboard/nav', async (req, res) => {
  try {
    const userId = parseInt(req.query.userId) || 0;
    const days = parseInt(req.query.days) || 30;
    if (!userId) {
      return res.status(400).json({ error: 'userId parameter is required' });
    }

    // Get all positions (current holdings by coin)
    const [positions] = await pool.execute(`
      SELECT 
        t.CryptocurrencyId,
        ROUND(SUM(CASE 
          WHEN o.Side = 'BUY' THEN t.QuantityCoin
          WHEN o.Side = 'SELL' THEN -t.QuantityCoin
          ELSE 0 
        END), 18) AS QtyCoin
      FROM Trades t
      JOIN Orders o ON o.Id = t.OrderId
      WHERE o.UserId = ?
      GROUP BY t.CryptocurrencyId
      HAVING QtyCoin <> 0
    `, [userId]);

    const cryptoIds = positions.map(p => p.CryptocurrencyId);
    if (cryptoIds.length === 0) {
      return res.json([]);
    }

    // Get historical prices for each day (last 30 days)
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(startDate.getDate() - days);

    const navData = [];
    const dateArray = [];

    // Generate date array
    for (let d = new Date(startDate); d <= endDate; d.setDate(d.getDate() + 1)) {
      dateArray.push(new Date(d));
    }

    // For each date, get the latest price before or on that date
    for (const date of dateArray) {
      const dateStr = formatDate(date);
      
      let totalNav = 0;
      
      for (const pos of positions) {
        // Get latest price on or before this date
        const [priceRows] = await pool.execute(`
          SELECT PriceUsd
          FROM CryptoPrices
          WHERE CryptocurrencyId = ?
            AND DATE(CollectedAtUtc) <= ?
          ORDER BY CollectedAtUtc DESC
          LIMIT 1
        `, [pos.CryptocurrencyId, dateStr]);

        const price = zeroIfNull(priceRows[0]?.PriceUsd);
        totalNav += parseFloat(pos.QtyCoin) * parseFloat(price);
      }

      navData.push({
        t: dateStr,
        v: parseFloat(totalNav) || 0
      });
    }

    res.json(navData);
  } catch (error) {
    console.error('Error in /dashboard/nav:', error);
    res.status(500).json({ error: error.message });
  }
});

// =====================================================================
// GET /dashboard/pnl?userId=8&granularity=hourly&date=today
// =====================================================================
app.get('/dashboard/pnl', async (req, res) => {
  try {
    const userId = parseInt(req.query.userId) || 0;
    const granularity = req.query.granularity || 'hourly';
    let dateParam = req.query.date || 'today';

    if (!userId) {
      return res.status(400).json({ error: 'userId parameter is required' });
    }

    if (dateParam === 'today') {
      dateParam = formatDate(new Date());
    }

    let dateCondition = '';
    let groupBy = '';
    let formatFunc = '';

    if (granularity === 'hourly') {
      dateCondition = `DATE(t.CreatedAt) = ?`;
      groupBy = 'HOUR(t.CreatedAt)';
      formatFunc = "LPAD(HOUR(t.CreatedAt), 2, '0')";
    } else if (granularity === 'daily') {
      dateCondition = `DATE(t.CreatedAt) >= ? AND DATE(t.CreatedAt) <= ?`;
      groupBy = 'DATE(t.CreatedAt)';
      formatFunc = "DATE_FORMAT(t.CreatedAt, '%Y-%m-%d')";
    } else {
      return res.status(400).json({ error: 'Invalid granularity. Use hourly or daily' });
    }

    // Query PnL by hour for the specified date
    const query = `
      SELECT 
        ${formatFunc} AS h,
        SUM(CASE 
          WHEN o.Side = 'SELL' THEN (t.PriceUsd * t.QuantityCoin - t.FeeUsd)
          ELSE -(t.PriceUsd * t.QuantityCoin + t.FeeUsd)
        END) AS v
      FROM Trades t
      JOIN Orders o ON o.Id = t.OrderId
      WHERE o.UserId = ?
        AND ${granularity === 'hourly' ? dateCondition : `DATE(t.CreatedAt) >= ? AND DATE(t.CreatedAt) <= ?`}
      GROUP BY ${groupBy}
      ORDER BY ${groupBy}
    `;

    const params = granularity === 'hourly' 
      ? [userId, dateParam]
      : [userId, dateParam, dateParam];

    const [rows] = await pool.execute(query, params);

    const pnlData = rows.map(row => ({
      h: String(row.h).padStart(2, '0'),
      v: parseFloat(zeroIfNull(row.v)) || 0
    }));

    // Fill missing hours with 0 (for hourly only)
    if (granularity === 'hourly') {
      const filledData = [];
      for (let hour = 0; hour < 24; hour++) {
        const hourStr = String(hour).padStart(2, '0');
        const existing = pnlData.find(d => d.h === hourStr);
        filledData.push({
          h: hourStr,
          v: existing ? existing.v : 0
        });
      }
      res.json(filledData);
    } else {
      res.json(pnlData);
    }
  } catch (error) {
    console.error('Error in /dashboard/pnl:', error);
    res.status(500).json({ error: error.message });
  }
});

// Health check
app.get('/health', async (req, res) => {
  try {
    const [rows] = await pool.execute('SELECT 1 as ok');
    res.json({ status: 'ok', database: 'connected' });
  } catch (error) {
    res.status(500).json({ status: 'error', database: 'disconnected', error: error.message });
  }
});

const PORT = process.env.PORT || 4000;
app.listen(PORT, () => {
  console.log(`Dashboard API server running on port ${PORT}`);
  console.log(`Endpoints:`);
  console.log(`  GET /dashboard/summary?userId=8`);
  console.log(`  GET /dashboard/nav?userId=8&days=30`);
  console.log(`  GET /dashboard/pnl?userId=8&granularity=hourly&date=today`);
  console.log(`  GET /health`);
});

