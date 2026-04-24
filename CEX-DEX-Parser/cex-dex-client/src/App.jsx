import { useState, useEffect, useRef, useCallback } from 'react';
import { fetchPrices, fetchAlertHistory } from './services/api';
import { createHubConnection } from './services/signalr';
import Header from './components/Header';
import Dashboard from './components/Dashboard';
import AlertsSidebar from './components/AlertsSidebar';
import './App.css';

export default function App() {
  const [pairs, setPairs] = useState([]);
  const [alerts, setAlerts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [connected, setConnected] = useState(false);
  const [lastUpdated, setLastUpdated] = useState(null);
  const [refreshing, setRefreshing] = useState(false);
  const connRef = useRef(null);

  const loadPrices = useCallback(async (manual = false) => {
    if (manual) setRefreshing(true);
    try {
      setError(null);
      const data = await fetchPrices();
      setPairs(data);
      setLastUpdated(new Date());
    } catch {
      setError('Unable to reach the API. Make sure the .NET server is running on port 5258.');
    } finally {
      setLoading(false);
      if (manual) setRefreshing(false);
    }
  }, []);

  const loadAlerts = useCallback(async () => {
    try {
      const data = await fetchAlertHistory();
      setAlerts(data);
    } catch {
      /* empty history is fine */
    }
  }, []);

  useEffect(() => {
    loadPrices();
    loadAlerts();
    const iv = setInterval(() => loadPrices(), 30_000);
    return () => clearInterval(iv);
  }, [loadPrices, loadAlerts]);

  useEffect(() => {
    const conn = createHubConnection();
    connRef.current = conn;

    conn.start()
      .then(() => setConnected(true))
      .catch(() => setConnected(false));

    conn.on('AlertTriggered', (alert) => {
      setAlerts(prev => [alert, ...prev]);
    });

    conn.onclose(() => setConnected(false));
    conn.onreconnecting(() => setConnected(false));
    conn.onreconnected(() => setConnected(true));

    return () => { conn.stop(); };
  }, []);

  return (
    <div className="app-shell">
      <Header
        connected={connected}
        lastUpdated={lastUpdated}
        refreshing={refreshing}
        onRefresh={() => loadPrices(true)}
      />
      <div className="app-body">
        <Dashboard
          pairs={pairs}
          loading={loading}
          error={error}
          onRefresh={() => loadPrices(true)}
        />
        <AlertsSidebar alerts={alerts} />
      </div>
    </div>
  );
}
