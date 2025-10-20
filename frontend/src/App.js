import React from "react";
import CoinGeckoClone from "./components/CoinGeckoClone";

function App() {
  return (
    <div style={{ 
      background: '#f8f9fa', 
      minHeight: '100vh',
      fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    }}>
      <CoinGeckoClone />
    </div>
  );
}

export default App;
