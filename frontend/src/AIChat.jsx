import { useEffect, useRef, useState } from 'react'
import './AIChat.css'

const opening = [{ role: 'assistant', content: 'Hi! Describe your IT issue. I can suggest troubleshooting steps, or you can create a ticket directly from the dashboard.' }]

export default function AIChat({ apiFetch, onTicketCreated }) {
  const [open, setOpen] = useState(false)
  const [messages, setMessages] = useState(opening)
  const [draft, setDraft] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [phase, setPhase] = useState('chat')
  const [firstIssue, setFirstIssue] = useState('')
  const [ticketCategory, setTicketCategory] = useState('Other')
  const bottom = useRef(null)

  useEffect(() => { if (open) bottom.current?.scrollIntoView({ behavior: 'smooth' }) }, [open, messages, phase, busy])

  async function send(e) {
    e.preventDefault()
    const question = draft.trim()
    if (!question || busy) return
    setDraft(''); setError(''); setBusy(true); setPhase('chat')
    const prior = messages.filter(m => m.role === 'user' || (m.role === 'assistant' && m.fromApi))
      .slice(-8).map(m => ({ role: m.role, content: m.content }))
    setMessages(prev => [...prev, { role: 'user', content: question }])
    if (!firstIssue) setFirstIssue(question)
    try {
      const response = await apiFetch('/api/self-service/ask', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ message: question, history: prior })
      })
      const body = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(body.message || `AI request failed (HTTP ${response.status}).`)
      setMessages(prev => [...prev, { role: 'assistant', content: body.answer, fromApi: true }])
      setPhase('resolved')
    } catch (err) {
      setError(err.message || 'Unable to contact AI. You can create a ticket directly.')
    } finally { setBusy(false) }
  }

  function chooseResolved(answer) {
    if (answer) {
      setMessages(prev => [...prev, { role: 'assistant', content: 'Glad that helped! You can ask another question anytime.' }])
      setPhase('chat'); setFirstIssue('')
    } else {
      setMessages(prev => [...prev, { role: 'assistant', content: 'Would you like to create a support ticket?' }])
      setPhase('offerTicket')
    }
  }

  async function createTicket() {
    if (busy) return
    const issue = firstIssue.trim()
    if (!issue) { setError('Please describe the issue first.'); return }
    setBusy(true); setError('')
    try {
      const response = await apiFetch('/api/tickets', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ issue: issue.slice(0, 2000), category: ticketCategory })
      })
      const data = await response.json().catch(() => ({}))
      if (!response.ok) throw new Error(data.message || `Ticket creation failed (HTTP ${response.status}).`)
      setMessages(prev => [...prev, { role: 'assistant', content: 'Your support ticket has been created. You can view it in the dashboard.' }])
      setPhase('chat'); setFirstIssue('')
      onTicketCreated?.()
    } catch (err) { setError(err.message || 'Could not create ticket.') }
    finally { setBusy(false) }
  }

  function reset() { if (busy) return; setMessages(opening); setDraft(''); setError(''); setPhase('chat'); setFirstIssue(''); setTicketCategory('Other') }

  return <div className="ai-chat-root">
    {open && <section className="ai-chat-window" role="dialog" aria-label="AI support chat">
      <div className="ai-chat-header">
        <div><strong>Ask AI</strong><small>IT self-service troubleshooting</small></div>
        <div className="ai-chat-header-actions">
          <button type="button" onClick={reset} disabled={busy} title="New conversation">↻</button>
          <button type="button" onClick={() => setOpen(false)} aria-label="Close chat">×</button>
        </div>
      </div>
      <div className="ai-chat-history" role="log" aria-live="polite">
        {messages.map((m, i) => <div key={i} className={`ai-chat-message ${m.role}`}>{m.content}</div>)}
        {busy && <div className="ai-chat-message assistant">Working…</div>}
        {phase === 'resolved' && !busy && <div className="ai-chat-choices" aria-label="Resolution confirmation">
          <p>Did this resolve your issue?</p>
          <button type="button" onClick={() => chooseResolved(true)}>Yes</button>
          <button type="button" onClick={() => chooseResolved(false)}>No</button>
        </div>}
        {phase === 'offerTicket' && !busy && <div className="ai-chat-choices" aria-label="Ticket confirmation">
          <p>Create a support ticket for your issue?</p>
          <label>Category <select value={ticketCategory} onChange={e => setTicketCategory(e.target.value)}>
            {['Network', 'Email', 'Account', 'Hardware', 'Software', 'Other'].map(c => <option key={c}>{c}</option>)}
          </select></label>
          <button type="button" onClick={createTicket}>Yes, create ticket</button>
          <button type="button" onClick={() => { setPhase('chat'); setFirstIssue(''); setMessages(prev => [...prev, { role: 'assistant', content: 'No ticket was created. You can continue chatting or use the Create Ticket form anytime.' }]) }}>No, thanks</button>
        </div>}
        <div ref={bottom} />
      </div>
      {error && <div className="ai-chat-error" role="alert">{error}</div>}
      <form className="ai-chat-form" onSubmit={send}>
        <input aria-label="Your IT question" value={draft} onChange={e => setDraft(e.target.value)}
          placeholder="Describe an IT issue…" maxLength={2000} disabled={busy || phase === 'offerTicket'} />
        <button type="submit" disabled={busy || !draft.trim() || phase === 'offerTicket'}>Send</button>
      </form>
      <p className="ai-chat-footnote">AI can make mistakes. Never share passwords or verification codes.</p>
    </section>}
    <button type="button" className="ai-chat-launcher" onClick={() => setOpen(o => !o)} aria-expanded={open}>
      {open ? '× Close' : '✦ Ask AI'}
    </button>
  </div>
}
