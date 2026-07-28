import { describe, expect, it, vi } from 'vitest'
import { expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { Route, Routes, useParams } from 'react-router-dom'
import {
  renderWithRouter,
  screen,
  userEvent,
} from '../utils/test-utils'
import { projects } from '../fixtures/projects'
import { server } from '../mocks/server'
import ProjectsPage from '../../pages/ProjectsPage'

const PROJECTS_URL = 'http://localhost:5074/projects'

function ProjectDetailRoute() {
  const { id } = useParams()

  return (
    <div>
      <h1>Proje Detayı</h1>
      <span data-testid="project-route-id">{id}</span>
    </div>
  )
}

const PROJECTS_URL = 'http://localhost:5074/projects'

const PROJECT_ID = 'project-1'

const PROJECT_DASHBOARD_URL =
  `http://localhost:5074/projects/${PROJECT_ID}/dashboard`

const PROJECT_EVM_URL =
  `http://localhost:5074/dashboard/projects/${PROJECT_ID}/evm`

const projectList = [
  {
    projectId: PROJECT_ID,
    projectCode: 'PRJ-001',
    projectName: 'Detay Test Projesi',
    projectStatus: 'Devam Ediyor',
    manualHealth: 'Orta',
    budgetStatus: 'Dengeli',
    plannedProgress: 70,
    actualProgress: 60,
    baselineFinishDate: '2026-07-01T00:00:00',
    forecastFinishDate: '2026-07-21T00:00:00',
  },
]

const projectDashboard = {
  projectId: PROJECT_ID,
  projectCode: 'PRJ-001',
  projectName: 'Detay Test Projesi',
  customerName: 'Test Müşterisi',
  projectManager: 'Test Yöneticisi',
  projectStatus: 'Devam Ediyor',
  manualHealth: 'Orta',
  plannedProgress: 70,
  actualProgress: 60,
  openRiskCount: 2,
  openIssueCount: 1,
  openActionCount: 3,
  startDate: '2026-01-01T00:00:00',
  baselineFinishDate: '2026-07-01T00:00:00',
  forecastFinishDate: '2026-07-21T00:00:00',
  executiveSummary: 'Proje detay ekranı için test özeti.',
  milestones: [],
  risks: [],
  issues: [],
  actions: [],
  reports: [],
}

const evmData = {
  bac: 1000000,
  pv: 700000,
  ev: 600000,
  ac: 650000,
  sv: -100000,
  cv: -50000,
  spi: 0.86,
  cpi: 0.92,
  eac: 1086956.52,
  vac: -86956.52,
}

function ProjectRouteResult() {
  const { id } = useParams()

  return (
    <div>
      <h1>Proje Detayı Route</h1>
      <span data-testid="project-route-id">{id}</span>
    </div>
  )
}

function renderProjectList(projects = projectList) {
  server.use(
    http.get(PROJECTS_URL, () => {
      return HttpResponse.json(projects)
    }),
  )

  return renderWithRouter(
    <Routes>
      <Route path="/projects" element={<ProjectsPage />} />
      <Route
        path="/projects/:id"
        element={<ProjectRouteResult />}
      />
    </Routes>,
    {
      initialEntries: ['/projects'],
    },
  )
}

function renderProjectDetail({
  dashboardResponse = projectDashboard,
  evmResponse = evmData,
  dashboardStatus = 200,
  evmStatus = 200,
} = {}) {
  server.use(
    http.get(PROJECT_DASHBOARD_URL, () => {
      return HttpResponse.json(
        dashboardResponse,
        { status: dashboardStatus },
      )
    }),

    http.get(PROJECT_EVM_URL, () => {
      return HttpResponse.json(
        evmResponse,
        { status: evmStatus },
      )
    }),
  )

  return renderWithRouter(
    <Routes>
      <Route
        path="/projects/:id"
        element={<ProjectDetailPage />}
      />
      <Route
        path="/projects"
        element={<h1>Projeler Sayfası</h1>}
      />
    </Routes>,
    {
      initialEntries: [`/projects/${PROJECT_ID}`],
    },
  )
}

function getProjectRow(projectName) {
  const projectNameCell = screen.getByText(projectName)
  const row = projectNameCell.closest('tr')

  if (!row) {
    throw new Error(`${projectName} proje satırı bulunamadı.`)
  }

  return row
}

it('proje satırına tıklandığında projectId ile detay sayfasına yönlendirmelidir', async () => {
  const user = userEvent.setup()
  const selectedProject = projects[0]

  server.use(
    http.get(PROJECTS_URL, () => {
      return HttpResponse.json(projects)
    }),
  )

  renderWithRouter(
    <Routes>
      <Route path="/projects" element={<ProjectsPage />} />
      <Route
        path="/projects/:id"
        element={<ProjectDetailRoute />}
      />
    </Routes>,
    {
      initialEntries: ['/projects'],
    },
  )

  const projectNameCell = await screen.findByText(
    selectedProject.projectName,
  )
  const projectRow = projectNameCell.closest('tr')

  expect(projectRow).not.toBeNull()

  await user.click(projectRow)

  expect(
    screen.getByRole('heading', { name: 'Proje Detayı' }),
  ).toBeInTheDocument()

  expect(
    screen.getByTestId('project-route-id'),
  ).toHaveTextContent(String(selectedProject.projectId))
})

describe('Proje navigasyonu ve detay ekranı', () => {
  it('proje satırına tıklandığında projectId ile detay sayfasına gitmelidir', async () => {
    const user = userEvent.setup()

    renderProjectList()

    const projectName = await screen.findByText(
      projectList[0].projectName,
    )

    const projectRow = projectName.closest('tr')

    expect(projectRow).not.toBeNull()

    await user.click(projectRow)

    expect(
      screen.getByRole('heading', {
        name: 'Proje Detayı Route',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByTestId('project-route-id'),
    ).toHaveTextContent(PROJECT_ID)
  })

  it('projectId bulunmadığında projectCode ile detay sayfasına gitmelidir', async () => {
    const user = userEvent.setup()

    const projectWithoutId = {
      ...projectList[0],
      id: undefined,
      projectId: undefined,
      projectCode: 'PRJ-FALLBACK',
      projectName: 'Kod ile Yönlenen Proje',
    }

    renderProjectList([projectWithoutId])

    const projectName = await screen.findByText(
      projectWithoutId.projectName,
    )

    const projectRow = projectName.closest('tr')

    expect(projectRow).not.toBeNull()

    await user.click(projectRow)

    expect(
      screen.getByTestId('project-route-id'),
    ).toHaveTextContent(projectWithoutId.projectCode)
  })

  it('proje detayı yüklenirken loading mesajını göstermelidir', async () => {
    let completeDashboardRequest

    server.use(
      http.get(PROJECT_DASHBOARD_URL, async () => {
        await new Promise((resolve) => {
          completeDashboardRequest = resolve
        })

        return HttpResponse.json(projectDashboard)
      }),

      http.get(PROJECT_EVM_URL, () => {
        return HttpResponse.json(evmData)
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
      screen.getByText('Proje detayları yükleniyor...'),
    ).toBeInTheDocument()

    await vi.waitFor(() => {
      expect(completeDashboardRequest).toBeTypeOf('function')
    })

    completeDashboardRequest()

    expect(
      await screen.findByRole('heading', {
        name: projectDashboard.projectName,
      }),
    ).toBeInTheDocument()
  })

  it('proje detay ekranında dashboard ve EVM verilerini göstermelidir', async () => {
    renderProjectDetail()

    expect(
      await screen.findByRole('heading', {
        name: projectDashboard.projectName,
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByText(projectDashboard.projectCode),
    ).toBeInTheDocument()

    expect(
      screen.getByText(projectDashboard.customerName),
    ).toBeInTheDocument()

    expect(
      screen.getByText(projectDashboard.projectManager),
    ).toBeInTheDocument()

    expect(
      screen.getByText(
        `● Sağlık: ${projectDashboard.manualHealth}`,
      ),
    ).toBeInTheDocument()

    const spiHeading = screen.getByRole('heading', {
      name: 'SPI (Zaman Performansı)',
    })

    const cpiHeading = screen.getByRole('heading', {
      name: 'CPI (Maliyet Performansı)',
    })

    expect(spiHeading.nextElementSibling).toHaveTextContent(
      String(evmData.spi),
    )

    expect(cpiHeading.nextElementSibling).toHaveTextContent(
      String(evmData.cpi),
    )
  })

  it('dashboard isteği hata verdiğinde Türkçe hata mesajını göstermelidir', async () => {
    const consoleErrorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => {})

    try {
      renderProjectDetail({
        dashboardResponse: {
          message: 'Sunucu hatası',
        },
        dashboardStatus: 500,
      })

      expect(
        await screen.findByText(
          'Proje detayları veritabanından çekilemedi.',
        ),
      ).toBeInTheDocument()

      expect(
        screen.getByRole('button', {
          name: '← Projelere Dön',
        }),
      ).toBeInTheDocument()
    } finally {
      consoleErrorSpy.mockRestore()
    }
  })

  it('EVM isteği hata verse bile proje detay ekranını açık tutmalıdır', async () => {
    const consoleWarnSpy = vi
      .spyOn(console, 'warn')
      .mockImplementation(() => {})

    try {
      renderProjectDetail({
        evmResponse: {
          message: 'EVM verisi bulunamadı',
        },
        evmStatus: 500,
      })

      expect(
        await screen.findByRole('heading', {
          name: projectDashboard.projectName,
        }),
      ).toBeInTheDocument()

      expect(
        screen.queryByText(
          'Proje detayları veritabanından çekilemedi.',
        ),
      ).not.toBeInTheDocument()

      expect(
        screen.getByRole('heading', {
          name: 'SPI (Zaman Performansı)',
        }).nextElementSibling,
      ).toHaveTextContent('-')

      expect(
        screen.getByRole('heading', {
          name: 'CPI (Maliyet Performansı)',
        }).nextElementSibling,
      ).toHaveTextContent('-')
    } finally {
      consoleWarnSpy.mockRestore()
    }
  })

  it('Projelere Dön butonuyla proje listesine dönmelidir', async () => {
    const user = userEvent.setup()

    renderProjectDetail()

    await screen.findByRole('heading', {
      name: projectDashboard.projectName,
    })

    await user.click(
      screen.getByRole('button', {
        name: '← Projelere Dön',
      }),
    )

    expect(
      screen.getByRole('heading', {
        name: 'Projeler Sayfası',
      }),
    ).toBeInTheDocument()
  })

  it('proje detayındaki temel sekmeleri göstermelidir', async () => {
    renderProjectDetail()

    await screen.findByRole('heading', {
      name: projectDashboard.projectName,
    })

    expect(
      screen.getByRole('button', {
        name: 'Genel Özet',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: 'Kilometre Taşları (0)',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: 'Riskler (0)',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: 'Sorunlar (0)',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: 'Aksiyonlar (0)',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: 'EVM',
      }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', {
        name: 'PİR Dönemleri (0)',
      }),
    ).toBeInTheDocument()
  })

  it('proje genel özetinde bitiş sapmasını gün olarak göstermelidir', async () => {
    renderProjectDetail()

    await screen.findByRole('heading', {
      name: projectDashboard.projectName,
    })

    const finishDeviation = screen.queryByText(
      /^(\+)?20\s*gün$/i,
    )

    expect(finishDeviation).toBeInTheDocument()
  })
})