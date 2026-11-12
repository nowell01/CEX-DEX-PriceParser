const BASE = 'https://cex-dex-parser-h7b3f0gwbyfah7ft.canadacentral-01.azurewebsites.net';
//const BASE = 'https://localhost:5258';

export async function fetchPrices() {
    const res = await fetch(`${BASE}/api/prices`, { credentials: 'include' });
  if (!res.ok) throw new Error('No price data available');
  return res.json();
}

export async function fetchPriceBySymbol(symbol) {
  const encoded = encodeURIComponent(symbol);
    const res = await fetch(`${BASE}/api/prices/${encoded}`, { credentials: 'include' });
  if (!res.ok) throw new Error(`Could not fetch prices for ${symbol}`);
  return res.json();
}

export async function fetchAlertHistory() {
    const res = await fetch(`${BASE}/api/alerts/history`, { credentials: 'include' });
  if (!res.ok) throw new Error('No alert history');
  return res.json();
}

export async function fetchAiSummary(hours = 24) {
    const res = await fetch(`${BASE}/api/insights/summary?hours=${hours}`);
    if (res.status === 429) throw new Error('Too many requests. Try again in a minute.');
    if (!res.ok) throw new Error('AI summary is unavailable right now.');
    return res.json();
}
