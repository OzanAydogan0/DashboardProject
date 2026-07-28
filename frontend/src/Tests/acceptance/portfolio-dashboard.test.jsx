import { expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { Route, Routes, useParams } from 'react-router-dom'
import {
  renderWithRouter,
  screen,
  within,
} from '../utils/test-utils'
import {
  renderWithRouter,
  screen,
} from '../utils/test-utils'
import {
  renderWithRouter,
  screen,
  userEvent,
} from '../utils/test-utils'
import { projects } from '../fixtures/projects'
import { server } from '../mocks/server'
import HomePage from '../../pages/HomePage'

const DASHBOARD_URL = 'http://localhost:5074/dashboard'

const navigableDashboardProject = {
  projectId: 'project-1',
  projectCode: 'PRJ-001',
  projectName: 'ÜBYES Entegre Güvenlik Sistemi',
  projectStatus: 'Aktif',
  manualHealth: 'Kötü',
  plannedProgress: 30,
  actualProgress: 22,
  openRiskCount: 1,
  openIssueCount: 0,
  openActionCount: 1,
  baselineFinishDate: '2026-07-01T00:00:00',
  forecastFinishDate: '2026-07-21T00:00:00',
  latestEvmPeriod: '2026-06',
}

function PortfolioProjectDetailRoute() {
  const { id } = useParams()

  return (
    <div>
      <h1>Proje Detayı</h1>
      <span data-testid="portfolio-project-route-id">{id}</span>
    </div>
  )
}

it('dashboard API verisini proje tablosunda ve toplam proje KPI alanında göstermelidir', async () => {
  server.use(
    http.get(DASHBOARD_URL, () => {
      return HttpResponse.json(projects)
    }),
  )

  renderWithRouter(<HomePage />)

  const table = screen.getByRole('table')

  await within(table).findByText(projects[0].projectCode)

  projects.forEach((project) => {
    const codeCell = within(table).getByText(project.projectCode)
    const row = codeCell.closest('tr')

    expect(row).not.toBeNull()
    expect(
      within(row).getByText(project.projectName),
    ).toBeInTheDocument()
  })

  const totalProjectsTitle = screen.getByText('Toplam Proje')
  const totalProjectsValue = totalProjectsTitle.nextElementSibling

  expect(totalProjectsValue).not.toBeNull()
  expect(totalProjectsValue).toHaveTextContent(
    String(projects.length),
  )

  expect(
    screen.queryByText(
      'Veriler backend servisinden alınamadı. Lütfen bağlantınızı kontrol edin.',
    ),
  ).not.toBeInTheDocument()
})

it('Portföy dashboardundaki proje satırına tıklandığında proje detayına gitmelidir', async () => {
  const user = userEvent.setup()

  server.use(
    http.get('http://localhost:5074/dashboard', () => {
      return HttpResponse.json([navigableDashboardProject])
    }),
  )

  renderWithRouter(
    <Routes>
      <Route path="/" element={<HomePage />} />
      <Route
        path="/projects/:id"
        element={<PortfolioProjectDetailRoute />}
      />
    </Routes>,
    {
      initialEntries: ['/'],
    },
  )

  const projectNameCell = await screen.findByText(
    navigableDashboardProject.projectName,
  )
  const projectRow = projectNameCell.closest('tr')

  expect(projectRow).not.toBeNull()

  await user.click(projectRow)

  expect(
    screen.getByRole('heading', { name: 'Proje Detayı' }),
  ).toBeInTheDocument()

  expect(
    screen.getByTestId('portfolio-project-route-id'),
  ).toHaveTextContent(navigableDashboardProject.projectId)
})

it('Portföy dashboardunda yönergede istenen filtre kontrollerini göstermelidir', async () => {
  server.use(
    http.get('http://localhost:5074/dashboard', () => {
      return HttpResponse.json(projects)
    }),
  )

  const { container } = renderWithRouter(<HomePage />)

  await screen.findByText(projects[0].projectCode)

  const projectSelect = container.querySelector(
    'select[name="projectCode"]',
  )

  const statusSelect = container.querySelector(
    'select[name="status"]',
  )

  const healthSelect = container.querySelector(
    'select[name="health"]',
  )

  expect.soft(projectSelect).toBeInTheDocument()
  expect.soft(projectSelect).toHaveAttribute('multiple')

  expect.soft(statusSelect).toBeInTheDocument()
  expect.soft(healthSelect).toBeInTheDocument()

  expect.soft(
    screen.queryByLabelText(/Proje Yöneticisi/i),
  ).toBeInTheDocument()

  expect.soft(
    screen.queryByLabelText(/Müşteri/i),
  ).toBeInTheDocument()

  expect.soft(
    screen.queryByLabelText(/Rapor Dönemi/i),
  ).toBeInTheDocument()

  expect.soft(
    screen.queryByRole('button', { name: 'Filtrele' }),
  ).toBeInTheDocument()

  expect.soft(
    screen.queryByRole('button', { name: 'Temizle' }),
  ).toBeInTheDocument()
})

it('Portföy dashboardunda kritik kayıt bölümlerini göstermelidir', async () => {
  server.use(
    http.get('http://localhost:5074/dashboard', () => {
      return HttpResponse.json(projects)
    }),
  )

  renderWithRouter(<HomePage />)

  await screen.findByText(projects[0].projectCode)

  expect.soft(
    screen.queryByRole('heading', {
      name: /Kritik Riskler/i,
    }),
  ).toBeInTheDocument()

  expect.soft(
    screen.queryByRole('heading', {
      name: /Geciken Aksiyonlar/i,
    }),
  ).toBeInTheDocument()

  expect.soft(
    screen.queryByRole('heading', {
      name: /Yaklaşan.*Kilometre Taşları|Geciken.*Kilometre Taşları/i,
    }),
  ).toBeInTheDocument()
})