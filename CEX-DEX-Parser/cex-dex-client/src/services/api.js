const BASE = 'http://localhost:5258';

export async function fetchPrices() {
  const res = await fetch(`${BASE}/api/prices`);
  if (!res.ok) throw new Error('No price data available');
  return res.json();
}

export async function fetchPriceBySymbol(symbol) {
  const encoded = encodeURIComponent(symbol);
  const res = await fetch(`${BASE}/api/prices/${encoded}`);
  if (!res.ok) throw new Error(`Could not fetch prices for ${symbol}`);
  return res.json();
}

export async function fetchAlertHistory() {
  const res = await fetch(`${BASE}/api/alerts/history`);
  if (!res.ok) throw new Error('No alert history');
  return res.json();
}
