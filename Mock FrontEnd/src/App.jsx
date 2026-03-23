import { useState } from 'react'
import './App.css'

// ── API helper ────────────────────────────────────────────────────────────────
async function apiFetch(method, path, body, token) {
  const opts = {
    method,
    credentials: 'include',   // sends HttpOnly cookies (aff_sid)
    headers: { 'Content-Type': 'application/json' },
  }
  if (token) opts.headers['Authorization'] = `Bearer ${token}`
  if (body)  opts.body = JSON.stringify(body)

  try {
    const res  = await fetch(path, opts)
    const data = await res.json().catch(() => ({}))
    return { ok: res.ok, status: res.status, data }
  } catch (e) {
    return { ok: false, status: 0, data: { error: e.message } }
  }
}

// ── Small components ──────────────────────────────────────────────────────────
function Field({ label, type = 'text', value, onChange, placeholder }) {
  return (
    <div className="field">
      <label>{label}</label>
      <input
        type={type} value={value}
        placeholder={placeholder}
        onChange={e => onChange(e.target.value)}
      />
    </div>
  )
}

function StatCard({ value, label, highlight }) {
  return (
    <div className={`stat-card${highlight ? ' highlight' : ''}`}>
      <div className="stat-val">{value}</div>
      <div className="stat-lbl">{label}</div>
    </div>
  )
}

function InfoRow({ k, v }) {
  return (
    <div className="info-row">
      <span className="ik">{k}</span>
      <span className="iv">{v}</span>
    </div>
  )
}

function LogEntry({ method, url, status, data }) {
  const ok = status >= 200 && status < 300
  return (
    <div className="log-entry">
      <div>
        <span className={`method ${method === 'GET' ? 'get' : 'post'}`}>{method}</span>
        <span className="log-url">{url}</span>
      </div>
      <div className={`log-status ${ok ? 'ok' : 'err'}`}>
        HTTP {status || '—'} {ok ? '✓' : '✗'}
      </div>
      <pre className="log-data">{JSON.stringify(data, null, 2)}</pre>
    </div>
  )
}

// ── Main App ──────────────────────────────────────────────────────────────────
const DEV_ACCOUNTS = [
  { email: 'alice@dev.com', info: 'ALICE001 · 10 clicks · 3 conversions' },
  { email: 'bob@dev.com',   info: 'BOB00001 · 4 clicks · 0 conversions'  },
  { email: 'carol@dev.com', info: 'CAROL001 · new account'               },
]

const TABS = ['register', 'login', 'dashboard', 'click', 'convert']
const TAB_LABELS = { register: 'Register', login: 'Login', dashboard: 'Dashboard', click: 'Track Click', convert: 'Convert' }

export default function App() {
  const [token,  setToken]  = useState(() => localStorage.getItem('ct_token'))
  const [affId,  setAffId]  = useState(() => localStorage.getItem('ct_affid'))
  const [tab,    setTab]    = useState('register')
  const [log,    setLog]    = useState([])
  const [dash,   setDash]   = useState(null)

  // Form state
  const [regForm,   setReg]   = useState({ name: 'Test User', email: 'test@example.com', password: 'Test123!' })
  const [loginForm, setLogin] = useState({ email: 'alice@dev.com', password: 'DevPass123!' })
  const [clickCode, setClick] = useState('ALICE001')
  const [convForm,  setConv]  = useState({ sessionId: '', conversionType: 'Registration', userId: '' })

  // ── Helpers ────────────────────────────────────────────────────────────────
  const doApi = async (method, path, body) => {
    const r = await apiFetch(method, path, body, token)
    setLog(prev => [{ method, url: path, status: r.status, data: r.data, id: Date.now() }, ...prev])
    return r
  }

  const saveAuth = ({ token: t, affiliateId: id }) => {
    setToken(t); setAffId(id)
    localStorage.setItem('ct_token', t)
    localStorage.setItem('ct_affid', id)
  }

  const logout = () => {
    setToken(null); setAffId(null)
    localStorage.removeItem('ct_token')
    localStorage.removeItem('ct_affid')
    setLog(prev => [{ method: '—', url: '/logout', status: 200, data: { message: 'Logged out' }, id: Date.now() }, ...prev])
  }

  const quickLogin = (email) => {
    setLogin({ email, password: 'DevPass123!' })
    setTab('login')
  }

  // ── Actions ────────────────────────────────────────────────────────────────
  const register = async () => {
    const r = await doApi('POST', '/api/auth/register', regForm)
    if (r.ok) saveAuth(r.data)
  }

  const login = async () => {
    const r = await doApi('POST', '/api/auth/login', loginForm)
    if (r.ok) saveAuth(r.data)
  }

  const loadDash = async () => {
    const r = await doApi('GET', '/api/affiliate/dashboard')
    if (r.ok) setDash(r.data)
  }

  const recordClick = async () => {
    await doApi('GET', `/api/tracking/click?affiliateCode=${encodeURIComponent(clickCode)}`)
  }

  const clearSession = () => {
    document.cookie = 'aff_sid=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;'
    setLog(prev => [{ method: '—', url: 'aff_sid cookie', status: 200, data: { message: 'Session cookie cleared.' }, id: Date.now() }, ...prev])
  }

  const recordConversion = async () => {
    const body = { sessionId: convForm.sessionId, conversionType: convForm.conversionType }
    if (convForm.userId) body.userId = parseInt(convForm.userId)
    await doApi('POST', '/api/tracking/convert', body)
  }

  // ── Render ─────────────────────────────────────────────────────────────────
  return (
    <div className="app">

      {/* Header */}
      <header className="app-header">
        <span className="logo">⚡ CopyTrade Market — Test Console</span>
        <span className={`badge ${token ? 'ok' : ''}`}>
          {token ? `Logged in · affiliate #${affId}` : 'Not logged in'}
        </span>
      </header>

      <div className="layout">

        {/* Sidebar */}
        <nav>
          {TABS.map(t => (
            <button key={t} className={tab === t ? 'active' : ''} onClick={() => setTab(t)}>
              {TAB_LABELS[t]}
            </button>
          ))}
          <div className="sep" />
          <button className="danger" onClick={logout}>Logout</button>
        </nav>

        {/* Main */}
        <main>

          {/* Register */}
          {tab === 'register' && (
            <section>
              <h2>Register</h2>
              <div className="dev-box">
                <div className="dev-title">Dev Accounts (auto-seeded · password: DevPass123!)</div>
                {DEV_ACCOUNTS.map(a => (
                  <div key={a.email} className="dev-row" onClick={() => quickLogin(a.email)}>
                    <code>{a.email}</code><span>{a.info}</span>
                  </div>
                ))}
              </div>
              <Field label="Name"     value={regForm.name}     onChange={v => setReg({ ...regForm, name: v })} />
              <Field label="Email"    type="email" value={regForm.email}    onChange={v => setReg({ ...regForm, email: v })} />
              <Field label="Password" type="password" value={regForm.password} onChange={v => setReg({ ...regForm, password: v })} />
              <button className="btn green" onClick={register}>Register</button>
              <p className="hint">
                If an <code>aff_sid</code> cookie is present (from a click), a Registration
                conversion is auto-recorded. <span className="tag">Observer Pattern</span>
              </p>
            </section>
          )}

          {/* Login */}
          {tab === 'login' && (
            <section>
              <h2>Login</h2>
              <Field label="Email"    type="email"    value={loginForm.email}    onChange={v => setLogin({ ...loginForm, email: v })} />
              <Field label="Password" type="password" value={loginForm.password} onChange={v => setLogin({ ...loginForm, password: v })} />
              <button className="btn blue" onClick={login}>Login</button>
            </section>
          )}

          {/* Dashboard */}
          {tab === 'dashboard' && (
            <section>
              <h2>
                Dashboard
                <button className="btn blue sm" onClick={loadDash}>Refresh</button>
              </h2>
              {dash ? (
                <>
                  <div className="stat-grid">
                    <StatCard value={dash.totalClicks}     label="Total Clicks" />
                    <StatCard value={dash.uniqueClicks}    label="Unique Clicks" />
                    <StatCard value={dash.last7DayClicks}  label="Last 7 Days" />
                    <StatCard value={dash.convertedClicks} label="Clicks w/ Conversion" highlight />
                  </div>
                  <InfoRow k="Name"          v={dash.affiliateName} />
                  <InfoRow k="Referral Code" v={dash.uniqueCode} />
                  <InfoRow k="Cached Count"  v={dash.cachedClickCount} />
                </>
              ) : (
                <p className="hint">Login first, then click Refresh.</p>
              )}
            </section>
          )}

          {/* Track Click */}
          {tab === 'click' && (
            <section>
              <h2>Track Click</h2>
              <Field label="Affiliate Code" value={clickCode} onChange={setClick} />
              <div style={{ display: 'flex', gap: 8 }}>
                <button className="btn blue" onClick={recordClick}>Record Click</button>
                <button className="btn" style={{ background: '#21262d', color: '#f85149' }} onClick={clearSession}>Clear Session</button>
              </div>
              <p className="hint">
                Sets an <code>aff_sid</code> cookie. A subsequent Register call will
                include the cookie and auto-record a Registration conversion.
                Use <strong>Clear Session</strong> to reset and test the flow again.
              </p>
            </section>
          )}

          {/* Convert */}
          {tab === 'convert' && (
            <section>
              <h2>Record Conversion</h2>
              <Field
                label="Session ID (raw aff_sid value — get from Swagger or DevTools Network tab)"
                value={convForm.sessionId}
                onChange={v => setConv({ ...convForm, sessionId: v })}
                placeholder="e.g. a3f8c12d4e…"
              />
              <div className="field">
                <label>Conversion Type</label>
                <select value={convForm.conversionType} onChange={e => setConv({ ...convForm, conversionType: e.target.value })}>
                  <option>Registration</option>
                  <option>Deposit</option>
                </select>
              </div>
              <Field
                label="User ID (optional)"
                type="number"
                value={convForm.userId}
                onChange={v => setConv({ ...convForm, userId: v })}
                placeholder="e.g. 42"
              />
              <button className="btn green" onClick={recordConversion}>Record Conversion</button>
              <p className="hint">
                Registration conversions are auto-recorded on Register (Observer Pattern).
                Use this tab to manually test Deposit conversions.
              </p>
            </section>
          )}

        </main>

        {/* Log panel */}
        <aside>
          <div className="log-hdr">
            <span>Response Log</span>
            <button onClick={() => setLog([])}>Clear</button>
          </div>
          <div className="log-body">
            {log.map(e => <LogEntry key={e.id} {...e} />)}
          </div>
        </aside>

      </div>
    </div>
  )
}
