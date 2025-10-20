import { Line } from "react-chartjs-2";
import { useEffect, useState } from "react";
import axios from "axios";

function PriceChart({ coinId, days = 7 }) {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (coinId) {
      setLoading(true);
      axios.get(`/api/crypto/${coinId}/history?days=${days}`)
        .then(res => {
          setHistory(res.data);
          setLoading(false);
        })
        .catch(err => {
          console.error("Error fetching price history:", err);
          setLoading(false);
        });
    }
  }, [coinId, days]);

  if (loading) return <div>Loading chart...</div>;
  if (!history.length) return <div>No data available</div>;

  const data = {
    labels: history.map(h => new Date(h.timestamp).toLocaleDateString()),
    datasets: [{
      label: "Price (USD)",
      data: history.map(h => h.price),
      borderColor: "#1976d2",
      backgroundColor: "rgba(25, 118, 210, 0.1)",
      fill: true,
      tension: 0.4
    }]
  };

  const options = {
    responsive: true,
    plugins: {
      title: {
        display: true,
        text: `Price History - ${days} days`
      },
      legend: {
        display: true
      }
    },
    scales: {
      y: {
        beginAtZero: false,
        ticks: {
          callback: function(value) {
            return '$' + value.toFixed(2);
          }
        }
      }
    }
  };

  return <Line data={data} options={options} />;
}

export default PriceChart;
