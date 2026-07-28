import { expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { Route, Routes } from 'react-router-dom'
import {
  renderWithRouter,
  screen,
  within,
  userEvent,
} from '../utils/test-utils'
import { server } from '../mocks/server'
import ProjectDetailPage from '../../pages/ProjectDetailPage'

const PROJECT_ID = '1'

const PROJECT_DASHBOARD_URL =
  `http://localhost:5074/projects/${PROJECT_ID}/dashboard`

const PROJECT_EVM_URL =
  `http://localhost:5074/dashboard/projects/${PROJECT_ID}/evm`

const milestones = [
  {
    id: 101,
    name: 'Tasarım Onayı',
    owner: 'Ayşe Yılmaz',
    plannedDate: '2026-04-10T00:00:00',
    forecastDate: '2026-04-15T00:00:00',
    actualDate: '2026-04-14T00:00:00',
    critical: true,
    status: 'Tamamlandı',
  },
  {
    id: 102,
    name: 'Pilot Kurulum',
    owner: 'Mehmet Demir',
    plannedDate: '2026-06-01T00:00:00',
    forecastDate: '2026-06-12T00:00:00',
    actualDate: null,
    critical: false,
    status: 'Planlandı',
  },
]

const projectDashboard = {
  projectId: 1,
  projectCode: 'PRJ-001',
  projectName: 'Kilometre Taşı Test Projesi',
  customerName: 'Test Müşterisi',
  projectManager: 'Test Yöneticisi',
  projectStatus: 'Devam Ediyor',
  manualHealth: 'Orta',
  plannedProgress: 50,
  actualProgress: 45,
  milestones,
  risks: [],
  issues: [],
  actions: [],
  reports: [],
}

function formatDate(dateString) {
  if (!dateString) {
    return '-'
  }

  return new Date(dateString).toLocaleDateString('tr-TR')
}

function getMilestoneRow(name) {
  const nameCell = screen.getByRole('cell', { name })
  const row = nameCell.closest('tr')

  if (!row) {
    throw new Error(`${name} kilometre taşı satırı bulunamadı.`)
  }

  return row
}

it('kilometre taşı tablo alanlarını doğru göstermelidir', async () => {
  const user = userEvent.setup()

  server.use(
    http.get(PROJECT_DASHBOARD_URL, () => {
      return HttpResponse.json(projectDashboard)
    }),

    http.get(PROJECT_EVM_URL, () => {
      return HttpResponse.json({})
    }),
  )

  renderWithRouter(
    <Routes>
      <Route
        path="/projects/:id"
        element={<ProjectDetailPage />}
      />
    </Routes>,
    {
      initialEntries: [`/projects/${PROJECT_ID}`],
    },
  )

  expect(
    await screen.findByRole('heading', {
      name: projectDashboard.projectName,
    }),
  ).toBeInTheDocument()

  await user.click(
    screen.getByRole('button', {
      name: `Kilometre Taşları (${milestones.length})`,
    }),
  )

  expect(
    screen.getByRole('heading', { name: 'Kilometre Taşları' }),
  ).toBeInTheDocument()

  const table = screen.getByRole('table')

  expect(
    within(table).getByRole('columnheader', { name: 'Adı' }),
  ).toBeInTheDocument()
  expect(
    within(table).getByRole('columnheader', { name: 'Sorumlu' }),
  ).toBeInTheDocument()
  expect(
    within(table).getByRole('columnheader', {
      name: 'Planlanan Tarih',
    }),
  ).toBeInTheDocument()
  expect(
    within(table).getByRole('columnheader', {
      name: 'Tahmini Tarih',
    }),
  ).toBeInTheDocument()
  expect(
    within(table).getByRole('columnheader', {
      name: 'Gerçekleşen Tarih',
    }),
  ).toBeInTheDocument()
  expect(
    within(table).getByRole('columnheader', { name: 'Kritik' }),
  ).toBeInTheDocument()
  expect(
    within(table).getByRole('columnheader', { name: 'Durum' }),
  ).toBeInTheDocument()

  const completedRow = getMilestoneRow('Tasarım Onayı')

  expect(
    within(completedRow).getByText('Ayşe Yılmaz'),
  ).toBeInTheDocument()
  expect(
    within(completedRow).getByText(formatDate(milestones[0].plannedDate)),
  ).toBeInTheDocument()
  expect(
    within(completedRow).getByText(formatDate(milestones[0].forecastDate)),
  ).toBeInTheDocument()
  expect(
    within(completedRow).getByText(formatDate(milestones[0].actualDate)),
  ).toBeInTheDocument()
  expect(
    within(completedRow).getByText('⚠️ Evet'),
  ).toBeInTheDocument()
  expect(
    within(completedRow).getByText('Tamamlandı'),
  ).toBeInTheDocument()

  const plannedRow = getMilestoneRow('Pilot Kurulum')

  expect(
    within(plannedRow).getByText('Mehmet Demir'),
  ).toBeInTheDocument()
  expect(
    within(plannedRow).getByText(formatDate(milestones[1].plannedDate)),
  ).toBeInTheDocument()
  expect(
    within(plannedRow).getByText(formatDate(milestones[1].forecastDate)),
  ).toBeInTheDocument()
  expect(
    within(plannedRow).getByText('-'),
  ).toBeInTheDocument()
  expect(
    within(plannedRow).getByText('Hayır'),
  ).toBeInTheDocument()
  expect(
    within(plannedRow).getByText('Planlandı'),
  ).toBeInTheDocument()
})