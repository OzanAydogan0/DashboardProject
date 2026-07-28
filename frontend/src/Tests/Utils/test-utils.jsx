import { render } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { AlertProvider } from '../../components/AlertProvider'
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
      <AlertProvider>
        {ui}
      </AlertProvider>
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