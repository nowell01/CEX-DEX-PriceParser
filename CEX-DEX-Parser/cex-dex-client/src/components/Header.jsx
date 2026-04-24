import './Header.css';

function formatTime(date) {
  if (!date) return '—';
  return date.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

export default function Header({ connected, lastUpdated, refreshing, onRefresh }) {
  return (
    <header className="header">
      <div className="header-brand">
        <span className="header-logo">
          <span className="logo-cex">CEX</span>
          <span className="logo-sep">·</span>
          <span className="logo-dex">DEX</span>
        </span>
        <span className="header-tagline">Arbitrage Dashboard</span>
      </div>

      <div className="header-meta">
        <div className="header-updated">
          <span className="meta-label">Updated</span>
          <span className="meta-value">{formatTime(lastUpdated)}</span>
        </div>

        <div className={`status-dot-wrap ${connected ? 'connected' : 'disconnected'}`}>
          <span className="status-dot" />
          <span className="status-label">{connected ? 'Live' : 'Offline'}</span>
        </div>

        <button
          className={`refresh-btn ${refreshing ? 'spinning' : ''}`}
          onClick={onRefresh}
          disabled={refreshing}
          title="Refresh prices"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <polyline points="23 4 23 10 17 10" />
            <polyline points="1 20 1 14 7 14" />
            <path d="M3.51 9a9 9 0 0114.13-3.36L23 10M1 14l5.36 4.36A9 9 0 0020.49 15" />
          </svg>
        </button>
      </div>
    </header>
  );
}
