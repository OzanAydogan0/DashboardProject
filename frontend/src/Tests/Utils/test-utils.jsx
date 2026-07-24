import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'

function renderWithRouter(
  ui,
  {
    route = '/',
    initialEntries = [route],
    ...renderOptions
  } = {},
) {
  const user = userEvent.setup()

  const result = render(
    <MemoryRouter initialEntries={initialEntries}>
      {ui}
    </MemoryRouter>,
    renderOptions,
  )

  return {
    user,
    ...result,
  }
}

export * from '@testing-library/react'
export { renderWithRouter, userEvent }