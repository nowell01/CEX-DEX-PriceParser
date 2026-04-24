import PairCard from './PairCard';
import './Dashboard.css';

export default function Dashboard({ pairs, loading, error, onRefresh }) {
  if (loading) {
    return (
      <div className="dashboard">
        <div className="dashboard-header">
          <h2 className="dashboard-title">Live Price Comparison</h2>
        </div>
        <div className="dashboard-state">
          <div className="skeleton-grid">
            <div className="skeleton-card" />
            <div className="skeleton-card" />
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="dashboard">
        <div className="dashboard-header">
          <h2 className="dashboard-title">Live Price Comparison</h2>
        </div>
        <div className="dashboard-state">
          <div className="error-box">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="12" />
              <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
            <p>{error}</p>
            <button className="retry-btn" onClick={onRefresh}>Retry</button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="dashboard">
      <div className="dashboard-header">
        <h2 className="dashboard-title">Live Price Comparison</h2>
        <span className="dashboard-subtitle">{pairs.length} pair{pairs.length !== 1 ? 's' : ''} · auto-refreshes every 30s</span>
      </div>
      <div className="pairs-grid">
        {pairs.map(pair => (
          <PairCard key={pair.symbol} pair={pair} />
        ))}
      </div>
    </div>
  );
}
