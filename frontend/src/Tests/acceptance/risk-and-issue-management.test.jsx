import { expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { Route, Routes } from 'react-router-dom'
import {
  renderWithRouter,
  screen,
  userEvent,
  within,
} from '../utils/test-utils'
import { server } from '../mocks/server'
import ProjectDetailPage from '../../pages/ProjectDetailPage'

const PROJECT_ID = '1'

const PROJECT_DASHBOARD_URL =
  `http://localhost:5074/projects/${PROJECT_ID}/dashboard`

const PROJECT_EVM_URL =
  `http://localhost:5074/dashboard/projects/${PROJECT_ID}/evm`

const risks = [
  {
    id: 101,
    title: 'Tedarik gecikmesi',
    category: 'Tedarik',
    probability: 4,
    impact: 5,
    owner: 'Ayşe Yılmaz',
    status: 'Açık',
  },
  {
    id: 102,
    title: 'Kaynak yetersizliği',
    category: 'Kaynak',
    probability: 2,
    impact: 3,
    owner: 'Mehmet Demir',
    status: 'İzleniyor',
  },
]

const projectDashboard = {
  projectId: 1,
  projectCode: 'PRJ-001',
  projectName: 'Risk Yönetimi Test Projesi',
  customerName: 'Test Müşterisi',
  projectManager: 'Test Yöneticisi',
  projectStatus: 'Devam Ediyor',
  manualHealth: 'Orta',
  plannedProgress: 60,
  actualProgress: 50,
  milestones: [],
  risks,
  issues: [],
  actions: [],
  reports: [],
}

function getRiskRow(title) {
  const titleCell = screen.getByRole('cell', { name: title })
  const row = titleCell.closest('tr')

  if (!row) {
    throw new Error(`${title} risk satırı bulunamadı.`)
  }

  return row
}

it('risk puanını olasılık ile etki değerlerinin çarpımı olarak göstermelidir', async () => {
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
      name: `Riskler (${risks.length})`,
    }),
  )

  expect(
    screen.getByRole('heading', { name: 'Proje Riskleri' }),
  ).toBeInTheDocument()

  const table = screen.getByRole('table')

  expect(
    within(table).getByRole('columnheader', {
      name: 'Risk Başlığı',
    }),
  ).toBeInTheDocument()

  expect(
    within(table).getByRole('columnheader', {
      name: 'Olasılık (1-5)',
    }),
  ).toBeInTheDocument()

  expect(
    within(table).getByRole('columnheader', {
      name: 'Etki (1-5)',
    }),
  ).toBeInTheDocument()

  expect(
    within(table).getByRole('columnheader', {
      name: 'Risk Puanı',
    }),
  ).toBeInTheDocument()

  risks.forEach((risk) => {
    const row = getRiskRow(risk.title)
    const expectedScore = risk.probability * risk.impact
    const cells = within(row).getAllByRole('cell')

    expect(cells[0]).toHaveTextContent(risk.title)
    expect(cells[1]).toHaveTextContent(risk.category)
    expect(cells[2]).toHaveTextContent(String(risk.probability))
    expect(cells[3]).toHaveTextContent(String(risk.impact))
    expect(cells[4]).toHaveTextContent(String(expectedScore))
    expect(cells[5]).toHaveTextContent(risk.owner)
    expect(cells[6]).toHaveTextContent(risk.status)
  })
})