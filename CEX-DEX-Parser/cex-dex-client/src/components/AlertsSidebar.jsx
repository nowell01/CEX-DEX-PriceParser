import { useState } from 'react';
import AlertItem from './AlertItem';
import './AlertsSidebar.css';

export default function AlertsSidebar({ alerts }) {
  const [open, setOpen] = useState(true);

  return (
    <aside className={`alerts-sidebar ${open ? 'open' : 'collapsed'}`}>
      <div className="sidebar-header">
        <div className="sidebar-title-row">
          <span className="sidebar-title">Alerts</span>
          {alerts.length > 0 && (
            <span className="alert-count">{alerts.length > 99 ? '99+' : alerts.length}</span>
          )}
        </div>
        <button
          className="sidebar-toggle"
          onClick={() => setOpen(o => !o)}
          title={open ? 'Collapse sidebar' : 'Expand sidebar'}
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            {open
              ? <polyline points="15 18 9 12 15 6" />
              : <polyline points="9 18 15 12 9 6" />
            }
          </svg>
        </button>
      </div>

      {open && (
        <div className="alerts-list">
          {alerts.length === 0 ? (
            <div className="alerts-empty">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
                <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9" />
                <path d="M13.73 21a2 2 0 01-3.46 0" />
              </svg>
              <p>No alerts yet</p>
              <span>Alerts appear here when arbitrage is detected</span>
            </div>
          ) : (
            alerts.map((alert, i) => (
              <AlertItem key={alert.id ?? i} alert={alert} isNew={i === 0} />
            ))
          )}
        </div>
      )}
    </aside>
  );
}
