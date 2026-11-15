import { useEffect, useState } from 'react'
import { messages } from '../../messages/en'
import { fetchSession, signInUrl, signOut, type SessionResult } from './session'

type State = SessionResult | { status: 'loading' }

export function SessionControls() {
  const [state, setState] = useState<State>({ status: 'loading' })
  const [signOutFailed, setSignOutFailed] = useState(false)

  useEffect(() => {
    const abort = new AbortController()
    void fetchSession(abort.signal).then((result) => {
      if (!abort.signal.aborted) setState(result)
    })
    return () => abort.abort()
  }, [])

  async function handleSignOut() {
    try {
      await signOut()
      setSignOutFailed(false)
      setState(await fetchSession())
    } catch {
      setSignOutFailed(true)
    }
  }

  if (state.status === 'loading') {
    return <output>{messages.session.loading}</output>
  }
  if (state.status === 'error') {
    return <p role="alert">{messages.session.error}</p>
  }
  if (state.status === 'signed-out') {
    return <a href={signInUrl(`${window.location.pathname}${window.location.search}`)}>{messages.session.signIn}</a>
  }
  return (
    <div>
      <p>{messages.session.signedInAs(state.session.login ?? state.session.userId)}</p>
      {state.session.isNewUser ? <output>{messages.session.welcome}</output> : null}
      <button type="button" onClick={handleSignOut}>
        {messages.session.signOut}
      </button>
      {signOutFailed ? <p role="alert">{messages.session.signOutFailed}</p> : null}
    </div>
  )
}
