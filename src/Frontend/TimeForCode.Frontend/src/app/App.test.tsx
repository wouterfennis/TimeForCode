import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { axeViolations } from '../../tests/axe'
import { App } from './App'

describe('App', () => {
  it('shows the placeholder page in the main landmark', () => {
    render(<App />)

    const main = screen.getByRole('main')
    expect(main).toContainElement(screen.getByRole('heading', { level: 1, name: 'TimeForCode' }))
  })

  it('has no automated accessibility violations', async () => {
    const { container } = render(<App />)

    expect(await axeViolations(container)).toEqual([])
  })
})
