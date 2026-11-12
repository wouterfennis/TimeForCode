import { useCallback, useEffect, useState } from 'react'
import { authClient } from '../../api/authClient'
import { fetchSession, type SessionState } from './session'

export type Session = {
  state: SessionState
  signOutFailed: boolean
  signOut: () => Promise<void>
}

export function useSession(): Session {
  const [state, setState] = useState<SessionState>({ status: 'loading' })
  const [signOutFailed, setSignOutFailed] = useState(false)

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
    setSignOutFailed(false)
    const { response } = await authClient.POST('/api/v1/Authentication/logout').catch(() => ({ response: undefined }))
    if (response?.ok) {
      setState({ status: 'anonymous' })
    } else {
      setSignOutFailed(true)
    }
  }, [])

  return { state, signOutFailed, signOut }
}
