import './PairCard.css';

const EXCHANGE_COLORS = {
  Binance:     '#F0B90B',
  Coinbase:    '#5B8FED',
  KuCoin:      '#23AF91',
  Uniswap:     '#FF007A',
  PancakeSwap: '#1FC7D4',
  Hyperliquid: '#9B30FF',
};

function getExchangeColor(name) {
  return EXCHANGE_COLORS[name] ?? '#888';
}

function getSpreadClass(pct) {
  if (pct < 0.5) return 'low';
  if (pct < 1.0) return 'mid';
  if (pct < 2.0) return 'high';
  return 'extreme';
}

function formatPrice(n) {
  if (n >= 1000) return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  if (n >= 1)    return n.toFixed(4);
  return n.toFixed(6);
}

function formatVolume(n) {
  if (n >= 1_000_000_000) return `$${(n / 1_000_000_000).toFixed(2)}B`;
  if (n >= 1_000_000)     return `$${(n / 1_000_000).toFixed(2)}M`;
  if (n >= 1_000)         return `$${(n / 1_000).toFixed(1)}K`;
  return `$${n.toFixed(0)}`;
}

function formatTs(ts) {
  const d = new Date(ts);
  return d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

export default function PairCard({ pair }) {
  const { symbol, spread, spreadPercent, highestExchange, lowestExchange, prices = [] } = pair;
  const spreadClass = getSpreadClass(spreadPercent);

  const sorted = [...prices].sort((a, b) => b.price - a.price);

  return (
    <div className="pair-card">
      {/* Card header */}
      <div className="pair-card-header">
        <div className="pair-symbol">
          <span className="symbol-base">{symbol?.split('/')[0]}</span>
          <span className="symbol-slash">/</span>
          <span className="symbol-quote">{symbol?.split('/')[1]}</span>
        </div>

        <div className={`spread-badge spread-${spreadClass}`}>
          <span className="spread-label">Spread</span>
          <span className="spread-value">{spreadPercent?.toFixed(3)}%</span>
        </div>
      </div>

      {/* Best/Worst exchange row */}
      <div className="exchange-summary">
        <div className="exchange-tag">
          <span className="tag-label">Highest</span>
          <span
            className="tag-exchange"
            style={{ '--ex-color': getExchangeColor(highestExchange) }}
          >
            {highestExchange ?? '—'}
          </span>
        </div>
        <div className="spread-arrow">
          <svg viewBox="0 0 40 12" fill="none">
            <line x1="0" y1="6" x2="36" y2="6" stroke="currentColor" strokeWidth="1.5" strokeDasharray="3 2" />
            <polyline points="30,1 36,6 30,11" stroke="currentColor" strokeWidth="1.5" fill="none" />
          </svg>
          <span className="spread-raw">${spread?.toFixed(6)}</span>
        </div>
        <div className="exchange-tag">
          <span className="tag-label">Lowest</span>
          <span
            className="tag-exchange"
            style={{ '--ex-color': getExchangeColor(lowestExchange) }}
          >
            {lowestExchange ?? '—'}
          </span>
        </div>
      </div>

      {/* Price table */}
      <div className="price-table-wrap">
        <table className="price-table">
          <thead>
            <tr>
              <th>Exchange</th>
              <th>Price</th>
              <th>24h Volume</th>
              <th>Updated</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((ep) => (
              <tr key={ep.exchange} className="price-row">
                <td>
                  <span
                    className="ex-name-cell"
                    style={{ '--ex-color': getExchangeColor(ep.exchange) }}
                  >
                    <span className="ex-dot" />
                    {ep.exchange}
                  </span>
                </td>
                <td className="price-cell">${formatPrice(ep.price)}</td>
                <td className="vol-cell">{formatVolume(ep.volume24h)}</td>
                <td className="ts-cell">{formatTs(ep.timestamp)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
