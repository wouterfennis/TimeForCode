import type { ProjectSummary } from './useProjects'

export function ProjectListItem({ project }: { project: ProjectSummary }) {
  return (
    <li>
      <a href={project.githubUrl}>{project.fullName}</a>
      {project.description ? <p>{project.description}</p> : null}
      {project.language ? <p>{project.language}</p> : null}
    </li>
  )
}
