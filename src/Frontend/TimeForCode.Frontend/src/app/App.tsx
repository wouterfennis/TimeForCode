import { ProjectsPage } from '../features/projects/ProjectsPage'
import { messages } from '../messages/en'

export function App() {
  return (
    <main>
      <h1>{messages.title}</h1>
      <ProjectsPage />
    </main>
  )
}
