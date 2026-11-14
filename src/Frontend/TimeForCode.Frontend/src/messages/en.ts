// All user-facing strings live here, so translations can be added later without touching components.
export const messages = {
  title: 'TimeForCode',
  session: {
    loading: 'Checking your sign-in…',
    error: 'Your sign-in could not be checked. Try again later.',
    signIn: 'Sign in',
    signOut: 'Sign out',
    signOutFailed: 'You could not be signed out. Try again.',
    signedInAs: (name: string) => `Signed in as ${name}`,
    welcome: 'Welcome! Your profile has been created.',
  },
  projects: {
    listLabel: 'Projects',
    loading: 'Loading projects…',
    empty: 'There are no active projects yet.',
    error: 'The projects could not be loaded. Try again later.',
    // The API returns one page (20 by default); paging controls come later.
    partial: (shown: number, total: number) => `Showing ${shown} of ${total} active projects.`,
  },
} as const
