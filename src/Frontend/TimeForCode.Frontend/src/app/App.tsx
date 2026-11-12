import { ProjectsPage } from '../features/projects/ProjectsPage'
import { SessionControl } from '../features/session/SessionControl'
import { messages } from '../messages/en'

export function App() {
  return (
    <>
      <header>
        <SessionControl />
      </header>
      <main>
        <h1>{messages.title}</h1>
        <ProjectsPage />
      </main>
    </>
  )
}
