import { signInUrl } from '../../api/authClient'
import { messages } from '../../messages/en'
import { useSession } from './useSession'

export function SessionControl() {
  const { state, signOutFailed, signOut } = useSession()

  if (state.status === 'loading') {
    return null
  }
  if (state.status === 'anonymous') {
    return <a href={signInUrl(window.location.origin)}>{messages.session.signIn}</a>
  }
  return (
    <div>
      <span>{messages.session.signedInAs(state.login)}</span>{' '}
      <button type="button" onClick={() => void signOut()}>
        {messages.session.signOut}
      </button>
      {signOutFailed ? <p role="alert">{messages.session.signOutFailed}</p> : null}
    </div>
  )
}
