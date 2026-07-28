/*import { expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { render, screen } from '../utils/test-utils'
import { server } from '../mocks/server'
import App from '../../App'

const PROJECTS_URL = 'http://localhost:5074/projects'

it('token bulunmadığında korumalı projeler sayfası yerine giriş ekranını göstermelidir', async () => {
  localStorage.removeItem('token')
  localStorage.removeItem('user')

  window.history.pushState({}, '', '/projects')

  server.use(
    http.get(PROJECTS_URL, () => {
      return HttpResponse.json([])
    }),
  )

  render(<App />)

  expect(await screen.findByLabelText('E-posta')).toBeInTheDocument()
  expect(screen.getByLabelText('Şifre')).toBeInTheDocument()
  expect(
    screen.getByRole('button', { name: 'Giriş Yap' }),
  ).toBeInTheDocument()

  expect(
    screen.queryByRole('heading', { name: 'Projeler' }),
  ).not.toBeInTheDocument()
  expect(
    screen.queryByRole('button', { name: 'Çıkış Yap' }),
  ).not.toBeInTheDocument()
})*/