import { useState } from 'react';
import { fetchAiSummary } from '/src/services/api.js';

const RANGES = [1, 6, 24, 168];
const LEVEL_COLORS = {
    quiet: '#6b7280',
    normal: '#2563eb',
    busy: '#d97706',
    suspicious: '#dc2626',
};

function rangeLabel(h) {
    return h < 24 ? `${h}h` : `${h / 24}d`;
}

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

    return (
        <section className="ai-summary">
            <header className="ai-summary__header">
                <h2>AI Alert Summary</h2>
                <select value={hours} onChange={e => setHours(Number(e.target.value))} disabled={loading}>
                    {RANGES.map(h => <option key={h} value={h}>{rangeLabel(h)}</option>)}
                </select>
                <button onClick={generate} disabled={loading}>
                    {loading ? 'Analyzing�' : 'Generate'}
                </button>
            </header>

            {error && <p className="ai-summary__error">{error}</p>}

            {s && (
                <div className="ai-summary__body">
                    <span className="ai-summary__badge"
                        style={{ background: LEVEL_COLORS[s.activityLevel] ?? LEVEL_COLORS.normal }}>
                        {s.activityLevel}
                    </span>
                    <h3>{s.headline}</h3>
                    <p>{s.summary}</p>

                    {s.highlights?.length > 0 && (
                        <ul>
                            {s.highlights.map((h, i) => (
                                <li key={i}><strong>{h.symbol}</strong> � {h.insight}</li>
                            ))}
                        </ul>
                    )}

                    {s.warnings?.length > 0 && (
                        <ul className="ai-summary__warnings">
                            {s.warnings.map((w, i) => <li key={i}> ! {w}</li>)}
                        </ul>
                    )}

                    <small className="ai-summary__meta">
                        {data.alertCount} alerts � {data.model} � {data.inputTokens + data.outputTokens} tokens
                        {data.fromCache && ' � cached'} � {new Date(data.generatedAtUtc).toLocaleTimeString()}
                    </small>
                </div>
            )}
        </section>
    );
}