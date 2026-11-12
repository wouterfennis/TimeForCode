import { useCallback, useEffect, useRef, useState } from 'react'
import { authClient } from '../../api/authClient'
import { fetchSession, type SessionState } from './session'

export type Session = {
  state: SessionState
  signOutFailed: boolean
  signingOut: boolean
  signOut: () => Promise<void>
}

export function useSession(): Session {
  const [state, setState] = useState<SessionState>({ status: 'loading' })
  const [signOutFailed, setSignOutFailed] = useState(false)
  const [signingOut, setSigningOut] = useState(false)
  const signingOutRef = useRef(false) // blocks a second click before the state update renders

  useEffect(() => {
    const abort = new AbortController()
    fetchSession(abort.signal)
      .then((session) => {
        if (!abort.signal.aborted) setState(session)
      })
      .catch(() => {
        if (!abort.signal.aborted) setState({ status: 'anonymous' })
      })
    return () => abort.abort()
  }, [])

  // Logout revokes the refresh token and expires both token cookies (#104).
  const signOut = useCallback(async () => {
    if (signingOutRef.current) return
    signingOutRef.current = true
    setSigningOut(true)
    setSignOutFailed(false)
    const { response } = await authClient.POST('/api/v1/Authentication/logout').catch(() => ({ response: undefined }))
    if (response?.ok) {
      setState({ status: 'anonymous' })
    } else {
      setSignOutFailed(true)
    }
    signingOutRef.current = false
    setSigningOut(false)
  }, [])

  return { state, signOutFailed, signingOut, signOut }
}
