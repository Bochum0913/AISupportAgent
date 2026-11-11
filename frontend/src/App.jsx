
import { useEffect, useState } from 'react'
import './App.css'
import AIChat from './AIChat.jsx'

const API = '/api/tickets'
let csrfToken = null

async function refreshCsrfToken() {
  // Antiforgery request tokens depend on the authenticated identity.
  const response = await fetch('/api/auth/csrf', { credentials: 'same-origin', cache: 'no-store' })
  if (!response.ok) throw new Error('Could not initialize CSRF protection')
  csrfToken = (await response.json()).token
}

async function apiFetch(url, options = {}) {
  if (!csrfToken) await refreshCsrfToken()

  const send = () => fetch(url, {
    ...options,
    credentials: 'same-origin',
    headers: { ...options.headers, 'X-CSRF-TOKEN': csrfToken }
  })
  let response = await send()

  // A rejected CSRF request has not executed the action, so it is safe
  // to obtain a fresh identity-bound token and retry that request once.
  if (response.status === 400 && !['GET', 'HEAD'].includes((options.method || 'GET').toUpperCase())) {
    const body = await response.clone().json().catch(() => null)
    if (body?.message === 'Invalid or missing CSRF token.') {
      await refreshCsrfToken()
      response = await send()
    }
  }
  return response
}


function App() {
  const [user, setUser] = useState(null)
  const [authReady, setAuthReady] = useState(false)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [authMode, setAuthMode] = useState('login')
  const [authError, setAuthError] = useState('')
  const [tickets, setTickets] = useState([])
  const [statusFilter, setStatusFilter] = useState('')
  const [priorityFilter, setPriorityFilter] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [showUnassignedOnly, setShowUnassignedOnly] = useState(false)
  const [selected, setSelected] = useState(null)
  const [history, setHistory] = useState([])
  const [historyError, setHistoryError] = useState('')
  const [drawerError, setDrawerError] = useState('')
  const [assignee, setAssignee] = useState('')
  const [agents, setAgents] = useState([])
  const [accounts, setAccounts] = useState([])
  const [accountError, setAccountError] = useState('')
  const [resolutionNote, setResolutionNote] = useState('')
  const [newComment, setNewComment] = useState('')
  const [isInternalComment, setIsInternalComment] = useState(false)
  const [issue, setIssue] = useState('')
  const [category, setCategory] = useState('Software')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function loadTickets() {
    setLoading(true)
    setError('')

    try {
      const response = await apiFetch(API)
      if (!response.ok) throw new Error('Failed to load tickets')
      setTickets(await response.json())
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    apiFetch('/api/auth/me').then(async r => {
      if (r.ok) setUser(await r.json())
    }).catch(() => {}).finally(() => setAuthReady(true))
  }, [])
  useEffect(() => {
    if (!user) return
    loadTickets()
    if (user.roles.includes('Admin')) {
      refreshStaffDirectory()
    }
  }, [user])

  async function refreshStaffDirectory() {
    try {
      const [agentsResponse, usersResponse] = await Promise.all([
        apiFetch(`${API}/agents`), apiFetch('/api/auth/users')
      ])
      if (!agentsResponse.ok || !usersResponse.ok) throw new Error('Cannot load user directory')
      setAgents(await agentsResponse.json())
      setAccounts(await usersResponse.json())
    } catch (err) { setAccountError(err.message) }
  }

  async function setAccountRole(accountId, role) {
    setAccountError('')
    try {
      const response = await apiFetch(`/api/auth/users/${encodeURIComponent(accountId)}/role`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role })
      })
      if (!response.ok) {
        const body = await response.json().catch(() => ({}))
        throw new Error(body.message || 'Could not update account role')
      }
      await refreshStaffDirectory()
    } catch (err) { setAccountError(err.message) }
  }

  async function submitAuth(e) {
    e.preventDefault()
    setAuthError('')
    try {
      const response = await apiFetch(`/api/auth/${authMode}`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
      })
      if (!response.ok) {
        const result = await response.json().catch(() => ({}))
        throw new Error(result.message || (result.errors || []).join('; ') || 'Authentication failed')
      }
      // Authentication changes the identity bound to the CSRF token.
      csrfToken = null
      const me = await apiFetch('/api/auth/me')
      if (!me.ok) throw new Error('Could not load current user')
      setUser(await me.json()); setPassword('')
    } catch (err) { setAuthError(err.message) }
  }
  async function logout() {
    const response = await apiFetch('/api/auth/logout', { method: 'POST' })
    if (!response.ok) {
      setError('Failed to log out; please retry')
      return
    }
    csrfToken = null
    setUser(null); setTickets([]); setSelected(null)
    setAgents([]); setAccounts([]); setShowUnassignedOnly(false)
  }

  
  const filteredTickets = tickets.filter(ticket => {
    const matchesStatus =
      !statusFilter || ticket.status === statusFilter

    const matchesPriority =
      !priorityFilter || ticket.priority === priorityFilter

    const matchesCategory =
      !categoryFilter || ticket.category === categoryFilter

    return matchesStatus && matchesPriority && matchesCategory &&
      (!showUnassignedOnly || (!ticket.assignedTo && ticket.status !== 'Closed'))
  })


  async function createTicket(event) {
    event.preventDefault()
    setError('')

    try {
      const response = await apiFetch(API, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ issue, category })
      })

      if (!response.ok) throw new Error('Failed to create ticket')

      setIssue('')
      await loadTickets()
    } catch (err) {
      setError(err.message)
    }
  }

  async function viewTicket(id) {
    setError('')

    try {
      const response = await apiFetch(`${API}/${encodeURIComponent(id)}`)
      if (!response.ok) throw new Error('Failed to load ticket')
      const ticket = await response.json()
      setSelected(ticket)
      setDrawerError('')
      setAssignee(ticket.assignedTo || '')
      setIsInternalComment(false)
      setResolutionNote('')
      setHistory([])
      setHistoryError('')
      try {
        const historyResponse = await apiFetch(`${API}/${encodeURIComponent(id)}/history`)
        if (!historyResponse.ok) throw new Error('Could not load ticket history')
        setHistory(await historyResponse.json())
      } catch (historyErr) {
        setHistoryError(historyErr.message)
      }
    } catch (err) {
      setError(err.message)
    }
  }

  async function updateStatus(id, status) {
    setError('')
  setDrawerError('')

    try {
      const response = await apiFetch(
        `${API}/${encodeURIComponent(id)}/status`,
        {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ status, version: selected.version, resolutionNote: status === 'Resolved' ? resolutionNote : null })
        }
      )

      if (response.status === 409) {
          await loadTickets()
          await viewTicket(id)
          setDrawerError('Another user changed this ticket. The latest version has been loaded; review it before trying again.')
          return
        }
        if (!response.ok) {
          const body = await response.json().catch(() => ({}))
          throw new Error(body.message || 'Failed to update status')
        }

      await loadTickets()
      await viewTicket(id)
    } catch (err) {
      setError(err.message)
    }
  }

  
async function updatePriority(id, priority) {
  setError('')
  setDrawerError('')

  try {
    const response = await apiFetch(
      `${API}/${encodeURIComponent(id)}/priority`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ priority, version: selected.version })
      }
    )

    if (response.status === 409) {
          await loadTickets()
          await viewTicket(id)
          setDrawerError('Another user changed this ticket. The latest version has been loaded; review it before trying again.')
          return
        }
        if (!response.ok) {
          const body = await response.json().catch(() => ({}))
          throw new Error(body.message || 'Failed to update priority')
        }

    await loadTickets()
    await viewTicket(id)
  } catch (err) {
    setError(err.message)
  }
}

async function saveAssignee(id) {
  setError('')
  setDrawerError('')

  try {
    const response = await apiFetch(
      `${API}/${encodeURIComponent(id)}/assignee`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ assignedTo: assignee, version: selected.version })
      }
    )

    if (response.status === 409) {
          await loadTickets()
          await viewTicket(id)
          setDrawerError('Another user changed this ticket. The latest version has been loaded; review it before trying again.')
          return
        }
        if (!response.ok) {
          const body = await response.json().catch(() => ({}))
          throw new Error(body.message || 'Failed to assign ticket')
        }

    await loadTickets()
    await viewTicket(id)
  } catch (err) {
    setError(err.message)
  }
}

async function confirmResolution(id, resolved) {
  setDrawerError('')
  try {
    const response = await apiFetch(`${API}/${encodeURIComponent(id)}/confirmation`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ version: selected.version, resolved })
    })
    if (!response.ok) {
      const body = await response.json().catch(() => ({}))
      throw new Error(body.message || 'Could not confirm resolution')
    }
    await loadTickets()
    await viewTicket(id)
  } catch (err) {
    setDrawerError(err.message)
    await viewTicket(id)
    setDrawerError(err.message)
  }
}

async function addComment(id) {
  if (!newComment.trim()) return

  setError('')

  try {
    const response = await apiFetch(
      `${API}/${encodeURIComponent(id)}/comments`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content: newComment.trim(),
          isInternal: isInternalComment && !!user?.roles?.some(r => r === 'Agent' || r === 'Admin')
        })
      }
    )

    if (!response.ok) {
      throw new Error('Failed to add comment')
    }

    setNewComment('')
    await viewTicket(id)
  } catch (err) {
    setError(err.message)
  }
}

  if (!authReady) return <main className="dashboard"><p>Loading...</p></main>
  if (!user) return (
    <main className="auth-page">
      <form className="auth-card" onSubmit={submitAuth}>
        <h1>AI Support Agent</h1>
        <p>{authMode === 'login' ? 'Sign in to your account' : 'Create a Customer account'}</p>
        <label>Email<input type="email" value={email} onChange={e => setEmail(e.target.value)} required /></label>
        <label>Password<input type="password" value={password} onChange={e => setPassword(e.target.value)} required minLength={10} /></label>
        {authError && <p className="error">{authError}</p>}
        <button type="submit">{authMode === 'login' ? 'Sign in' : 'Register'}</button>
        <button type="button" className="secondary-button" onClick={() => { setAuthMode(authMode === 'login' ? 'register' : 'login'); setAuthError('') }}>
          {authMode === 'login' ? 'Create an account' : 'Already registered? Sign in'}
        </button>
      </form>
    </main>
  )

  const isAdmin = user.roles.includes('Admin')
  const isAgent = user.roles.includes('Agent')
  const isCustomer = user.roles.includes('Customer') && !isAdmin && !isAgent
  const isStaff = isAdmin || isAgent

  // The admin has a staff directory; other roles do not receive that directory.
  // Never display raw Identity GUIDs to users when a display name is unavailable.
  const formatAssignee = (value) => {
    if (!value || value === 'Unassigned') return 'Unassigned'
    const knownAgent = agents.find(a => a.id === value)
    if (knownAgent?.email) return knownAgent.email
    if (user.id === value && user.email) return user.email
    const isIdentityId = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
    return isIdentityId ? 'Assigned support agent' : value
  }

  const formatHistoryValue = (fieldName, value) => {
    if (fieldName === 'AssignedTo') return formatAssignee(value)
    return value || '—'
  }
  const canEditTicket = isAdmin || (isAgent && selected?.assignedTo === user.id)
  const allowedNextStatus = selected?.status === 'Open' || selected?.status === 'Reopened'
    ? 'In Progress' : selected?.status === 'In Progress' ? 'Resolved' : null
  return (
    <main className="dashboard">
      <header>
        <h1>AI Support Agent</h1>
        <p>{isAdmin ? 'Admin · Ticket Assignment & Oversight' : isAgent ? 'Agent · My Assigned Tickets' : 'Customer · AI Self-Service & Tickets'}</p>
        <div className="account-bar"><span>{user.email} ({user.roles.join(', ')})</span><button onClick={logout}>Log out</button></div>
      </header>

      {isAdmin && <section className="panel">
        <div className="section-heading"><h2>User Role Management</h2><button onClick={refreshStaffDirectory}>Refresh Users</button></div>
        <p>New users register as Customers. Assign the Agent role here before assigning tickets.</p>
        {accountError && <p className="error">{accountError}</p>}
        <div className="account-list">
          {accounts.map(account => <div className="account-item" key={account.id}>
            <span>{account.email} · {account.roles?.join(', ')}</span>
            {!account.roles?.includes('Admin') && <button type="button"
              disabled={account.roles?.includes('Agent')}
              onClick={() => setAccountRole(account.id, 'Agent')}>
              {account.roles?.includes('Agent') ? 'Agent' : 'Make Agent'}
            </button>}
          </div>)}
        </div>
      </section>}

      <section className="stats">
        {isCustomer ? <>
          <div className="stat">
            <h2>{tickets.length}</h2>
            <p>My Tickets</p>
          </div>
          <div className="stat">
            <h2>{tickets.filter(t => t.status !== 'Closed').length}</h2>
            <p>Active Tickets</p>
          </div>
          <div className="stat">
            <h2>{tickets.filter(t => t.status === 'Closed').length}</h2>
            <p>Closed Tickets</p>
          </div>
        </> : isAgent ? <>
          <div className="stat">
            <h2>{tickets.length}</h2>
            <p>My Assigned Tickets</p>
          </div>
          <div className="stat">
            <h2>{tickets.filter(t => ['Open', 'Reopened', 'In Progress'].includes(t.status)).length}</h2>
            <p>Needs Action</p>
          </div>
          <div className="stat">
            <h2>{tickets.filter(t => t.status === 'Resolved').length}</h2>
            <p>Awaiting Customer Confirmation</p>
          </div>
        </> : <>
          <div className="stat">
            <h2>{tickets.length}</h2>
            <p>All Tickets</p>
          </div>
          <div className="stat">
            <h2>{tickets.filter(t => !t.assignedTo && t.status !== 'Closed').length}</h2>
            <p>Unassigned Tickets</p>
          </div>
          <div className="stat">
            <h2>{tickets.filter(t => t.status !== 'Closed').length}</h2>
            <p>Not Closed</p>
          </div>
        </>}
      </section>

      {error && <p className="error">{error}</p>}

      {isCustomer && (
      <section className="panel">
        <h2>Create Ticket</h2>

        <form onSubmit={createTicket}>
          <input
            value={issue}
            onChange={e => setIssue(e.target.value)}
            placeholder="Describe the issue..."
            required
          />

          <select
            value={category}
            onChange={e => setCategory(e.target.value)}
          >
            {['Network', 'Email', 'Account', 'Hardware',
              'Software', 'Other'].map(c => (
              <option key={c}>{c}</option>
            ))}
          </select>

          <button type="submit">Create Ticket</button>
        </form>
      </section>
      )}

      <section className="panel">
        <div className="section-heading">
          <h2>{isAdmin ? 'All Support Tickets' : isAgent ? 'My Assigned Tickets' : 'My Support Tickets'}</h2>
          <button onClick={loadTickets}>Refresh</button>
        </div>

        
        <div className="filters">
          <select
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
            aria-label="Filter by status"
          >
            <option value="">All Statuses</option>
            <option value="Open">Open</option>
            <option value="In Progress">In Progress</option>
            <option value="Resolved">Resolved</option>
            <option value="Reopened">Reopened</option>
            <option value="Closed">Closed</option>
          </select>

          <select
            value={priorityFilter}
            onChange={e => setPriorityFilter(e.target.value)}
            aria-label="Filter by priority"
          >
            <option value="">All Priorities</option>
            <option value="Low">Low</option>
            <option value="Normal">Normal</option>
            <option value="High">High</option>
          </select>

          <select
            value={categoryFilter}
            onChange={e => setCategoryFilter(e.target.value)}
            aria-label="Filter by category"
          >
            <option value="">All Categories</option>
            {['Network', 'Email', 'Account', 'Hardware',
              'Software', 'Other'].map(category => (
              <option key={category} value={category}>
                {category}
              </option>
            ))}
          </select>

          <button
            type="button"
            onClick={() => {
              setStatusFilter('')
              setPriorityFilter('')
              setCategoryFilter('')
              setShowUnassignedOnly(false)
            }}
          >
            Clear Filters
          </button>
          {isAdmin && <button
            type="button"
            className={showUnassignedOnly ? '' : 'secondary-button'}
            aria-pressed={showUnassignedOnly}
            onClick={() => setShowUnassignedOnly(value => !value)}
          >
            {showUnassignedOnly ? 'Show All Tickets' : 'Unassigned Only'}
          </button>}
        </div>


        {loading ? <p>Loading...</p> : (
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Issue</th>
                <th>Category</th>
                <th>Priority</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {filteredTickets.map(ticket => (
                <tr
                  key={ticket.id}
                  onClick={() => viewTicket(ticket.id)}
                >
                  <td>{ticket.id}</td>
                  <td>{ticket.issue}</td>
                  <td>{ticket.category}</td>
                  <td>{ticket.priority}</td>
                  <td>{ticket.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {!loading && filteredTickets.length === 0 && <p>No tickets match the current filters.</p>}
      </section>

      
      {isCustomer && <AIChat key={user.id} apiFetch={apiFetch} onTicketCreated={loadTickets} />}

      {selected && (
        <div className="drawer-overlay" onClick={() => setSelected(null)}>
          <aside
            className="ticket-drawer"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="drawer-header">
              <div>
                <h2>Ticket Details</h2>
                <p>{selected.id}</p>
              </div>
              <button onClick={() => setSelected(null)}>Close</button>
            </div>

            <div className="drawer-content">
              {drawerError && <p className="error" role="alert">{drawerError}</p>}
              <h3>Issue</h3>
              <p className="ticket-issue">{selected.issue}</p>

              <h3>Category</h3>
              <p>{selected.category}</p>
         
              <h3>Priority</h3>
              <select disabled={!canEditTicket}
                value={selected.priority}
                onChange={(e) => updatePriority(selected.id, e.target.value)}
              >
                <option>Low</option><option>Normal</option><option>High</option>
              </select>

              <h3>Assigned To</h3>
              <p>{formatAssignee(selected.assignedTo)}</p>
              {isAdmin && <div className="assignee-form">
                <select value={assignee} onChange={e => setAssignee(e.target.value)} aria-label="Assign to Agent">
                  <option value="">Select an Agent</option>
                  {agents.map(a => <option key={a.id} value={a.id}>{a.email}</option>)}
                </select>
                <button type="button" onClick={() => saveAssignee(selected.id)} disabled={!assignee || assignee === selected.assignedTo}>Assign</button>
              </div>}
              {isAdmin && agents.length === 0 && <p>No Agent accounts found. Create a test Agent via the Admin role API first.</p>}

              <h3>Status</h3>
              <p><strong>{selected.status}</strong></p>
              {canEditTicket && allowedNextStatus && <div className="workflow-actions">
                {allowedNextStatus === 'Resolved' && <textarea
                  value={resolutionNote}
                  onChange={e => setResolutionNote(e.target.value)}
                  maxLength={4000}
                  placeholder="Required: describe the resolution for the customer"
                  rows={3}
                />}
                <button type="button" disabled={allowedNextStatus === 'Resolved' && !resolutionNote.trim()}
                  onClick={() => updateStatus(selected.id, allowedNextStatus)}>
                  Move to {allowedNextStatus}
                </button>
              </div>}
              {isCustomer && selected.status === 'Resolved' && <div className="workflow-actions">
                <p>Did the agent resolve your issue?</p>
                <button type="button" onClick={() => confirmResolution(selected.id, true)}>Yes, close ticket</button>
                <button type="button" className="secondary-button" onClick={() => confirmResolution(selected.id, false)}>No, reopen ticket</button>
              </div>}

              <h3>Activity History</h3>
              {historyError && <p className="error">{historyError}</p>}
              {history.length ? (
                <ol className="audit-history">
                  {history.map(entry => (
                    <li key={entry.id}>
                      <strong>{entry.fieldName}</strong>: {formatHistoryValue(entry.fieldName, entry.oldValue)} → {formatHistoryValue(entry.fieldName, entry.newValue)}
                      <small>{entry.actorLabel} · {new Date(entry.occurredAtUtc + (entry.occurredAtUtc.endsWith('Z') ? '' : 'Z')).toLocaleString()}</small>
                    </li>
                  ))}
                </ol>
              ) : (!historyError && <p>No recorded changes yet.</p>)}

              <h3>Comments</h3>
              {selected.comments?.length ? (
                selected.comments.map((comment, index) => (
                  <div className={`ticket-comment ${comment.isInternal ? 'internal-comment' : ''}`} key={comment.id ?? index}>
                    <div className="comment-meta">
                      <strong>{comment.authorLabel || 'Legacy'}</strong>
                      <span> · {new Date(comment.createdAt).toLocaleString()}</span>
                      {comment.isInternal && <strong className="internal-label"> · Internal note</strong>}
                    </div>
                    <p>{comment.content}</p>
                  </div>
                ))
              ) : (
                <p>No comments yet.</p>
              )}
              
              <div className="comment-form">
                <textarea
                  value={newComment}
                  onChange={(e) => setNewComment(e.target.value)}
                  placeholder={canEditTicket && isInternalComment ? 'Write an internal note (staff only)...' : 'Write a public reply...'}
                  rows={4}
                  maxLength={4000}
                />

                {canEditTicket && <label className="internal-toggle">
                  <input type="checkbox" checked={isInternalComment} onChange={e => setIsInternalComment(e.target.checked)} />
                  Internal note · Only visible to support staff
                </label>}
                <button
                  type="button"
                  onClick={() => addComment(selected.id)}
                  disabled={!newComment.trim() || (isStaff && !canEditTicket)}
                >
                  Add Comment
                </button>
              </div>

            </div>
          </aside>
        </div>
      )}

    </main>
  )
}

export default App
