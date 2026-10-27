import { messages } from '../../messages/en'
import { ProjectListItem } from './ProjectListItem'
import { useProjects } from './useProjects'

export function ProjectsPage() {
  const state = useProjects()

  if (state.status === 'loading') {
    return <output>{messages.projects.loading}</output>
  }
  if (state.status === 'error') {
    return <p role="alert">{messages.projects.error}</p>
  }
  if (state.projects.length === 0) {
    return <p>{messages.projects.empty}</p>
  }
  const isPartial = state.totalCount > state.projects.length
  return (
    <>
      <ul aria-label={messages.projects.listLabel}>
        {state.projects.map((project) => (
          <ProjectListItem key={project.id} project={project} />
        ))}
      </ul>
      {isPartial ? <p>{messages.projects.partial(state.projects.length, state.totalCount)}</p> : null}
    </>
  )
}
