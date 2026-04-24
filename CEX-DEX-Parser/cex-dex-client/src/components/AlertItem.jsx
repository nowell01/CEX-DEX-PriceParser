import { useEffect, useState } from 'react';
import './AlertItem.css';

function timeAgo(ts) {
  const diff = (Date.now() - new Date(ts).getTime()) / 1000;
  if (diff < 60)   return `${Math.floor(diff)}s ago`;
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
  return new Date(ts).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

function formatPrice(n) {
  if (n >= 1000) return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return n.toFixed(4);
}

export default function AlertItem({ alert, isNew }) {
  const [highlight, setHighlight] = useState(isNew);

  useEffect(() => {
    if (!isNew) return;
    const t = setTimeout(() => setHighlight(false), 3000);
    return () => clearTimeout(t);
  }, [isNew]);

  const {
    symbol, highExchange, lowExchange,
    highPrice, lowPrice, spreadPercent, triggeredAt,
  } = alert;

  return (
    <div className={`alert-item ${highlight ? 'alert-new' : ''}`}>
      <div className="alert-top">
        <span className="alert-symbol">{symbol}</span>
        <span className="alert-spread">{spreadPercent?.toFixed(3)}%</span>
      </div>

      <div className="alert-route">
        <span className="alert-exchange high">{highExchange}</span>
        <svg className="alert-arrow" viewBox="0 0 32 10" fill="none">
          <line x1="0" y1="5" x2="28" y2="5" stroke="currentColor" strokeWidth="1.5" />
          <polyline points="22,1 28,5 22,9" stroke="currentColor" strokeWidth="1.5" fill="none" />
        </svg>
        <span className="alert-exchange low">{lowExchange}</span>
      </div>

      <div className="alert-prices">
        <span className="alert-price-val">${formatPrice(highPrice)}</span>
        <span className="alert-price-sep">→</span>
        <span className="alert-price-val">${formatPrice(lowPrice)}</span>
      </div>

      <div className="alert-footer">
        <span className="alert-time">{timeAgo(triggeredAt)}</span>
        <span className="alert-type-tag">Arbitrage</span>
      </div>
    </div>
  );
}
