import { useState } from 'react';
import { fetchAiSummary } from '/src/services/api.js';

const RANGES = [
  { hours: 1, label: '1H' },
  { hours: 6, label: '6H' },
  { hours: 24, label: '24H' },
  { hours: 168, label: '7D' },
  { hours: 336, label: '14D' },
];

const LEVELS = ['quiet', 'normal', 'busy', 'suspicious'];

export default function AiSummaryPanel() {
  const [hours, setHours] = useState(24);
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  async function generate() {
    setLoading(true);
    setError(null);
    try {
      setData(await fetchAiSummary(hours));
    } catch (e) {
      setError(e.message);
    } finally {
      setLoading(false);
    }
  }

  const s = data?.summary;
  const level = LEVELS.includes(s?.activityLevel) ? s.activityLevel : 'normal';
  const usedModel = data && data.model !== 'none';

  return (
    <section className="ai-summary">
      <div className="ai-summary__bar">
        <div className="ai-summary__title-row">
          <span className="ai-summary__chip">AI</span>
          <h2 className="ai-summary__title">Alert Summary</h2>
          {s && <span className={`ai-badge ai-badge--${level}`}>{level}</span>}
        </div>

        <div className="ai-summary__controls">
          <div className="ai-range" role="group" aria-label="Time range">
            {RANGES.map(r => (
              <button
                key={r.hours}
                type="button"
                className={`ai-range__btn${hours === r.hours ? ' is-active' : ''}`}
                onClick={() => setHours(r.hours)}
                disabled={loading}
              >
                {r.label}
              </button>
            ))}
          </div>
          <button type="button" className="ai-generate" onClick={generate} disabled={loading}>
            {loading ? 'Analyzing...' : 'Generate'}
          </button>
        </div>
      </div>

      {error && <p className="ai-summary__error">{error}</p>}

      {!s && !error && (
        <p className="ai-summary__hint">
          Pick a time range and generate an AI summary of recent arbitrage alerts.
        </p>
      )}

      {s && (
        <div className="ai-summary__body">
          <h3 className="ai-summary__headline">{s.headline}</h3>
          <p className="ai-summary__text">{s.summary}</p>

          {s.highlights?.length > 0 && (
            <div className="ai-highlights">
              {s.highlights.map((h, i) => (
                <div className="ai-highlight" key={i}>
                  <span className="ai-highlight__symbol">{h.symbol}</span>
                  <span className="ai-highlight__text">{h.insight}</span>
                </div>
              ))}
            </div>
          )}

          {s.warnings?.length > 0 && (
            <ul className="ai-warnings">
              {s.warnings.map((w, i) => <li key={i}>{w}</li>)}
            </ul>
          )}

          <div className="ai-summary__meta">
            <span>{data.alertCount} alerts</span>
            {usedModel && <span>{data.model}</span>}
            {usedModel && <span>{data.inputTokens + data.outputTokens} tokens</span>}
            {data.fromCache && <span>cached</span>}
            <span>{new Date(data.generatedAtUtc).toLocaleTimeString()}</span>
          </div>
        </div>
      )}
    </section>
  );
}