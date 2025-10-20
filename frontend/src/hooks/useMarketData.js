import { useEffect, useState } from "react";
import axios from "axios";
import * as signalR from "@microsoft/signalr";

export default function useMarketData() {
  const [coins, setCoins] = useState([]);
  const [marketStats, setMarketStats] = useState({
    totalMarketCap: 0,
    totalVolume: 0,
    btcDominance: 57.3,
    ethDominance: 12.5
  });
  const [connection, setConnection] = useState(null);

  useEffect(() => {
    // Initial data load
    console.log("Fetching crypto data from backend...");
    console.log("useEffect called, setting up data fetch...");
    axios.get("http://localhost:5000/api/market/cryptocurrencies")
      .then(res => {
        console.log("Received crypto data:", res.data);
        console.log("Response status:", res.status);
        console.log("Response headers:", res.headers);
        
        // Ensure all coins have required properties
        const safeCoins = res.data.map(coin => {
          console.log("Processing coin:", coin);
          console.log("Coin current_price:", coin.current_price);
          console.log("Coin market_cap:", coin.market_cap);
          return {
            id: coin.id || '',
            symbol: coin.symbol || '',
            name: coin.name || '',
            currentPrice: coin.current_price || 0,
            marketCap: coin.market_cap || 0,
            priceChangePercentage1hInCurrency: coin.price_change_percentage_1h_in_currency || 0,
            priceChangePercentage24h: coin.price_change_percentage_24h || 0,
            priceChangePercentage7dInCurrency: coin.price_change_percentage_7d_in_currency || 0,
            totalVolume: coin.total_volume || 0,
            circulatingSupply: coin.circulating_supply || 0,
            lastUpdated: coin.last_updated || new Date()
          };
        });
        console.log("Processed coins:", safeCoins);
        setCoins(safeCoins);
        // Calculate market stats as fallback
        const totalMarketCap = safeCoins.reduce((sum, coin) => sum + (coin.marketCap || 0), 0);
        const totalVolume = safeCoins.reduce((sum, coin) => sum + (coin.totalVolume || 0), 0);
        console.log("Calculated market stats:", { totalMarketCap, totalVolume });
        setMarketStats(prev => ({
          ...prev,
          totalMarketCap,
          totalVolume
        }));
      })
      .catch(err => {
        console.error("Error loading crypto data:", err);
        console.error("Error details:", err.response);
        setCoins([]);
      });

    // Load market stats
    axios.get("http://localhost:5000/api/market/stats")
      .then(res => {
        setMarketStats(prev => ({
          ...prev,
          totalMarketCap: res.data.total_market_cap || prev.totalMarketCap,
          totalVolume: res.data.total_volume || prev.totalVolume,
          btcDominance: 57.3,
          ethDominance: 12.5
        }));
      })
      .catch(err => {
        console.error("Error loading market stats:", err);
        // Keep fallback stats
      });

    // Setup SignalR connection
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5000/marketHub")
      .withAutomaticReconnect()
      .build();

    newConnection.start()
      .then(() => {
        console.log("SignalR Connected");
      })
      .catch(err => {
        console.error("SignalR Connection Error: ", err);
        // Connection will retry automatically
      });

    newConnection.on("ReceivePriceUpdate", (updatedCoin) => {
      setCoins(prevCoins => {
        const index = prevCoins.findIndex(coin => coin.id === updatedCoin.id);
        if (index !== -1) {
          const newCoins = [...prevCoins];
          newCoins[index] = { ...newCoins[index], ...updatedCoin };
          return newCoins;
        }
        return prevCoins;
      });
    });

    newConnection.onclose((error) => {
      if (error) {
        console.log("SignalR connection closed due to error:", error);
      } else {
        console.log("SignalR connection closed");
      }
    });

    setConnection(newConnection);

    return () => {
      if (newConnection) {
        newConnection.stop();
      }
    };
  }, []);

  return { coins, marketStats, connection };
}
