
import { useEffect, useState } from 'react'
import './App.css'

const API = '/api/tickets'

function App() {
  const [tickets, setTickets] = useState([])
  const [statusFilter, setStatusFilter] = useState('')
  const [priorityFilter, setPriorityFilter] = useState('')
  const [categoryFilter, setCategoryFilter] = useState('')
  const [selected, setSelected] = useState(null)
  const [assignee, setAssignee] = useState('')
  const [newComment, setNewComment] = useState('')
  const [issue, setIssue] = useState('')
  const [category, setCategory] = useState('Software')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function loadTickets() {
    setLoading(true)
    setError('')

    try {
      const response = await fetch(API)
      if (!response.ok) throw new Error('Failed to load tickets')
      setTickets(await response.json())
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadTickets()
  }, [])

  
  const filteredTickets = tickets.filter(ticket => {
    const matchesStatus =
      !statusFilter || ticket.status === statusFilter

    const matchesPriority =
      !priorityFilter || ticket.priority === priorityFilter

    const matchesCategory =
      !categoryFilter || ticket.category === categoryFilter

    return matchesStatus && matchesPriority && matchesCategory
  })


  async function createTicket(event) {
    event.preventDefault()
    setError('')

    try {
      const response = await fetch(API, {
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
      const response = await fetch(`${API}/${encodeURIComponent(id)}`)
      if (!response.ok) throw new Error('Failed to load ticket')
      const ticket = await response.json()
      setSelected(ticket)
      setAssignee(ticket.assignedTo || '')
    } catch (err) {
      setError(err.message)
    }
  }

  async function updateStatus(id, status) {
    setError('')

    try {
      const response = await fetch(
        `${API}/${encodeURIComponent(id)}/status`,
        {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ status })
        }
      )

      if (!response.ok) throw new Error('Failed to update status')

      await loadTickets()
      await viewTicket(id)
    } catch (err) {
      setError(err.message)
    }
  }

  
async function updatePriority(id, priority) {
  setError('')

  try {
    const response = await fetch(
      `${API}/${encodeURIComponent(id)}/priority`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ priority })
      }
    )

    if (!response.ok) throw new Error('Failed to update priority')

    await loadTickets()
    await viewTicket(id)
  } catch (err) {
    setError(err.message)
  }
}

async function saveAssignee(id) {
  setError('')

  try {
    const response = await fetch(
      `${API}/${encodeURIComponent(id)}/assignee`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ assignedTo: assignee })
      }
    )

    if (!response.ok) throw new Error('Failed to assign ticket')

    await loadTickets()
    await viewTicket(id)
  } catch (err) {
    setError(err.message)
  }
}

async function addComment(id) {
  if (!newComment.trim()) return

  setError('')

  try {
    const response = await fetch(
      `${API}/${encodeURIComponent(id)}/comments`,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          content: newComment.trim()
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

  return (
    <main className="dashboard">
      <header>
        <h1>AI Support Agent</h1>
        <p>Support Ticket Management Dashboard</p>
      </header>

      <section className="stats">
        <div className="stat">
          <h2>{tickets.length}</h2>
          <p>Total Tickets</p>
        </div>
        <div className="stat">
          <h2>{tickets.filter(t => t.status === 'Open').length}</h2>
          <p>Open</p>
        </div>
        <div className="stat">
          <h2>{tickets.filter(t => t.status === 'Resolved').length}</h2>
          <p>Resolved</p>
        </div>
      </section>

      {error && <p className="error">{error}</p>}

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

      <section className="panel">
        <div className="section-heading">
          <h2>Support Tickets</h2>
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
            }}
          >
            Clear Filters
          </button>
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
      </section>

      
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
              <h3>Issue</h3>
              <p className="ticket-issue">{selected.issue}</p>

              <h3>Category</h3>
              <p>{selected.category}</p>
         
              <h3>Priority</h3>
              <select
                value={selected.priority}
                onChange={(e) => updatePriority(selected.id, e.target.value)}
              >
                <option>Low</option>
                <option>Normal</option>
                <option>High</option>
              </select>

              <h3>Assigned To</h3>
              <div className="assignee-form">
                <input
                  value={assignee}
                  onChange={(e) => setAssignee(e.target.value)}
                  placeholder="Enter technician name"
                />
                <button
                  type="button"
                  onClick={() => saveAssignee(selected.id)}
                  disabled={!assignee.trim()}
                >
                  Save
                </button>
              </div>

              <h3>Status</h3>
              <select
                value={selected.status}
                onChange={(e) =>
                  updateStatus(selected.id, e.target.value)
                }
              >
                <option>Open</option>
                <option>In Progress</option>
                <option>Resolved</option>
              </select>

              <h3>Comments</h3>
              {selected.comments?.length ? (
                selected.comments.map((comment, index) => (
                  <div className="ticket-comment" key={index}>
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
                  placeholder="Write a comment..."
                  rows={4}
                />

                <button
                  type="button"
                  onClick={() => addComment(selected.id)}
                  disabled={!newComment.trim()}
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
