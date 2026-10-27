import { useEffect, useState } from 'react'
import type { components } from '../../api/donation'
import { donationClient } from '../../api/donationClient'

export type ProjectSummary = components['schemas']['ProjectSummaryResponse']

export type ProjectsState =
  | { status: 'loading' }
  | { status: 'error' }
  | { status: 'loaded'; projects: ProjectSummary[]; totalCount: number }

export function useProjects(): ProjectsState {
  const [state, setState] = useState<ProjectsState>({ status: 'loading' })

  useEffect(() => {
    const abort = new AbortController()
    donationClient
      .GET('/api/v1/Project', { signal: abort.signal })
      .then(({ data }) => setState(data ? { status: 'loaded', projects: data.projects, totalCount: Number(data.totalCount) } : { status: 'error' }))
      .catch(() => {
        if (!abort.signal.aborted) setState({ status: 'error' })
      })
    return () => abort.abort()
  }, [])

  return state
}
