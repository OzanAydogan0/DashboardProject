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

const reports = [
  {
    id: 101,
    period: '2026-05',
    reportDate: '2026-05-31T00:00:00',
    status: 'Yayımlandı',
    executiveSummary:
      'Mayıs döneminde temel tasarım faaliyetleri tamamlandı ve pilot hazırlıkları başlatıldı.',
  },
  {
    id: 102,
    period: '2026-06',
    reportDate: '2026-06-30T00:00:00',
    status: 'Taslak',
    executiveSummary:
      'Haziran döneminde pilot kurulum çalışmaları devam etti.',
  },
]

const projectDashboard = {
  projectId: 1,
  projectCode: 'PRJ-001',
  projectName: 'PİR Raporlama Test Projesi',
  customerName: 'Test Müşterisi',
  projectManager: 'Test Yöneticisi',
  projectStatus: 'Devam Ediyor',
  manualHealth: 'Orta',
  plannedProgress: 55,
  actualProgress: 50,
  milestones: [],
  risks: [],
  issues: [],
  actions: [],
  reports,
}

function formatDate(dateString) {
  return new Date(dateString).toLocaleDateString('tr-TR')
}

function getReportRow(period) {
  const periodCell = screen.getByRole('cell', { name: period })
  const row = periodCell.closest('tr')

  if (!row) {
    throw new Error(`${period} PİR dönemi satırı bulunamadı.`)
  }

  return row
}

it('mevcut PİR dönemlerini tarih, durum ve yönetici özetiyle göstermelidir', async () => {
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
      name: `PİR Dönemleri (${reports.length})`,
    }),
  )

  expect(
    screen.getByRole('heading', {
      name: 'Aylık PİR Rapor Dönemleri',
    }),
  ).toBeInTheDocument()

  const table = screen.getByRole('table')

  expect(
    within(table).getByRole('columnheader', { name: 'Dönem' }),
  ).toBeInTheDocument()

  expect(
    within(table).getByRole('columnheader', {
      name: 'Rapor Tarihi',
    }),
  ).toBeInTheDocument()

  expect(
    within(table).getByRole('columnheader', { name: 'Durum' }),
  ).toBeInTheDocument()

  expect(
    within(table).getByRole('columnheader', {
      name: 'Yönetici Özeti',
    }),
  ).toBeInTheDocument()

  reports.forEach((report) => {
    const row = getReportRow(report.period)
    const expectedSummary =
      `${report.executiveSummary.slice(0, 60)}...`

    expect(
      within(row).getByText(formatDate(report.reportDate)),
    ).toBeInTheDocument()

    expect(
      within(row).getByText(report.status),
    ).toBeInTheDocument()

    expect(
      within(row).getByText(expectedSummary),
    ).toBeInTheDocument()
  })
})