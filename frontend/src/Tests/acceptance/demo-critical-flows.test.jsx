import { expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import {
  renderWithRouter,
  screen,
  within,
} from '../utils/test-utils'
import { server } from '../mocks/server'
import HomePage from '../../pages/HomePage'

const DASHBOARD_URL = 'http://localhost:5074/dashboard'

const demoProjects = [
  {
    projectId: 1,
    projectCode: 'PRJ-001',
    projectName: 'ÜBYES Entegre Güvenlik Sistemi',
    projectStatus: 'Devam Ediyor',
    manualHealth: 'Kötü',
    plannedProgress: 30,
    actualProgress: 22,
    openRiskCount: 3,
    openIssueCount: 1,
    openActionCount: 2,
    baselineFinishDate: '2026-06-01T00:00:00',
    forecastFinishDate: '2026-07-15T00:00:00',
    latestEvmPeriod: '2026-06',
  },
  {
    projectId: 2,
    projectCode: 'PRJ-002',
    projectName: 'ERİSİS PSIM Ürünleştirme',
    projectStatus: 'Devam Ediyor',
    manualHealth: 'Orta',
    plannedProgress: 56,
    actualProgress: 49,
    openRiskCount: 2,
    openIssueCount: 0,
    openActionCount: 1,
    baselineFinishDate: '2026-08-01T00:00:00',
    forecastFinishDate: '2026-08-20T00:00:00',
    latestEvmPeriod: '2026-06',
  },
  {
    projectId: 3,
    projectCode: 'PRJ-003',
    projectName: 'AKSİ FPV İHA Tespit Sistemi',
    projectStatus: 'Devam Ediyor',
    manualHealth: 'İyi',
    plannedProgress: 44,
    actualProgress: 46,
    openRiskCount: 0,
    openIssueCount: 0,
    openActionCount: 1,
    baselineFinishDate: '2026-09-01T00:00:00',
    forecastFinishDate: '2026-08-25T00:00:00',
    latestEvmPeriod: '2026-06',
  },
]

it('üç demo proje dashboard üzerinde hata oluşturmadan görüntülenmelidir', async () => {
  server.use(
    http.get(DASHBOARD_URL, () => {
      return HttpResponse.json(demoProjects)
    }),
  )

  renderWithRouter(<HomePage />)

  const table = screen.getByRole('table')

  await within(table).findByText('PRJ-001')

  demoProjects.forEach((project) => {
    const codeCell = within(table).getByText(project.projectCode)
    const row = codeCell.closest('tr')

    expect(row).not.toBeNull()
    expect(within(row).getByText(project.projectName)).toBeInTheDocument()
  })

  expect(
    screen.getByRole('heading', { name: 'Sağlık Dağılımı' }),
  ).toBeInTheDocument()

  expect(
    screen.getByRole('heading', { name: 'Proje Listesi' }),
  ).toBeInTheDocument()

  expect(screen.getByText('Toplam Proje').nextElementSibling).toHaveTextContent(
    '3',
  )

  expect(
    screen.queryByText(
      'Veriler backend servisinden alınamadı. Lütfen bağlantınızı kontrol edin.',
    ),
  ).not.toBeInTheDocument()
})