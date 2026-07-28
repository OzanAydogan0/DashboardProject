import { describe, expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { projects } from '../fixtures/projects'
import { server } from '../mocks/server'
import {
  renderWithRouter,
  screen,
} from '../utils/test-utils'
import ProjectsPage from '../../pages/ProjectsPage'
import ReportsPage from '../../pages/ReportsPage'

const API_BASE_URL = 'http://localhost:5074'

const adminUser = {
  userId: 1,
  email: 'admin@pir.local',
  role: 'Sistem Yöneticisi',
  userRole: 'Sistem Yöneticisi',
}

const projectReports = [
  {
    reportId: 10,
    pirReportId: 10,
    period: '2026-06',
    status: 'Yayımlandı',
  },
]

function useProjectsPageHandlers() {
  server.use(
    http.get(`${API_BASE_URL}/projects`, () => {
      return HttpResponse.json(projects)
    }),

    http.get(`${API_BASE_URL}/programs`, () => {
      return HttpResponse.json([])
    }),

    http.get(`${API_BASE_URL}/customers`, () => {
      return HttpResponse.json([])
    }),

    http.get(`${API_BASE_URL}/users`, () => {
      return HttpResponse.json([])
    }),
  )
}

function useReportsPageHandlers() {
  server.use(
    http.get(`${API_BASE_URL}/projects`, () => {
      return HttpResponse.json(projects)
    }),

    http.get(`${API_BASE_URL}/projects/:projectId/reports`, () => {
      return HttpResponse.json(projectReports)
    }),
  )
}

describe('Import and Export', () => {
  it('Excel içe aktarma akışında gerekli temel kontrolleri göstermelidir', async () => {
    localStorage.setItem('user', JSON.stringify(adminUser))
    useProjectsPageHandlers()

    renderWithRouter(<ProjectsPage />)

    await screen.findByText(projects[0].projectName)

    expect(
      screen.getByRole('button', {
        name: /Excel.*İçe Aktar|İçe Aktar.*Excel/i,
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: /Şablon İndir/i,
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: /Ön İzleme/i,
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: /^İçe Aktar$/i,
      }),
    ).toBeInTheDocument()
  })

  it('Raporlar ekranında proje ve dönem seçilerek Türkçe PDF oluşturulabilmelidir', async () => {
    useReportsPageHandlers()

    renderWithRouter(<ReportsPage />)

    await screen.findByRole('heading', {
      name: /Rapor Oluştur/i,
    })

    const comboboxes = screen.getAllByRole('combobox')

    expect(comboboxes).toHaveLength(3)

    const reportTypeSelect = comboboxes[0]
    const projectSelect = comboboxes[1]
    const periodSelect = comboboxes[2]

    expect(reportTypeSelect).toBeInTheDocument()
    expect(projectSelect).toBeInTheDocument()
    expect(periodSelect).toBeInTheDocument()

    expect(
      screen.getByRole('checkbox', {
        name: /PDF/i,
      }),
    ).toBeChecked()

    expect(
      screen.getByRole('button', {
        name: /Raporu Oluştur|PDF Oluştur/i,
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: /PDF İndir/i,
      }),
    ).toBeInTheDocument()
  })

  it('Projeler için Excel dışa aktarma kontrolü göstermelidir', async () => {
    localStorage.setItem('user', JSON.stringify(adminUser))
    useProjectsPageHandlers()

    renderWithRouter(<ProjectsPage />)

    await screen.findByText(projects[0].projectName)

    const accessibleName =
      /Excel.*Dışa Aktar|Dışa Aktar.*Excel/i

    const exportButton = screen.queryByRole('button', {
      name: accessibleName,
    })

    const exportLink = screen.queryByRole('link', {
      name: accessibleName,
    })

    expect(exportButton ?? exportLink).not.toBeNull()
  })
})