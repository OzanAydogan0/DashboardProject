import { describe, expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { Route, Routes } from 'react-router-dom'
import {
  renderWithRouter,
  screen,
  userEvent,
} from '../utils/test-utils'
import { server } from '../mocks/server'
import ProjectDetailPage from '../../pages/ProjectDetailPage'

const PROJECT_ID = '1'

const PROJECT_DASHBOARD_URL =
  `http://localhost:5074/projects/${PROJECT_ID}/dashboard`

const PROJECT_EVM_URL =
  `http://localhost:5074/dashboard/projects/${PROJECT_ID}/evm`

const projectDashboard = {
  projectId: 1,
  projectCode: 'PRJ-001',
  projectName: 'EVM Test Projesi',
  customerName: 'Test Müşterisi',
  projectManager: 'Test Yöneticisi',
  projectStatus: 'Devam Ediyor',
  manualHealth: 'Kötü',
  plannedProgress: 60,
  actualProgress: 45,
  openRiskCount: 2,
  openIssueCount: 1,
  openActionCount: 3,
  startDate: '2026-01-01T00:00:00',
  baselineFinishDate: '2026-08-01T00:00:00',
  forecastFinishDate: '2026-09-15T00:00:00',
  executiveSummary: 'Proje planın gerisinde ilerlemektedir.',
  milestones: [],
  risks: [],
  issues: [],
  actions: [],
  reports: [],
}

const evmData = {
  bac: 1000000,
  pv: 600000,
  ev: 450000,
  ac: 500000,
  sv: -150000,
  cv: -50000,
  spi: 0.75,
  cpi: 0.9,
  eac: 1111111.11,
  vac: -111111.11,
}

function expectLabeledValue(label, value) {
  const labelElement = screen.getByText(label)
  const container = labelElement.parentElement

  expect(container).not.toBeNull()
  expect(container).toHaveTextContent(String(value))
}

const EVM_PROJECT_ID = 'evm-format-project'

const EVM_DASHBOARD_URL =
  `http://localhost:5074/projects/${EVM_PROJECT_ID}/dashboard`

const EVM_DETAILS_URL =
  `http://localhost:5074/dashboard/projects/${EVM_PROJECT_ID}/evm`

const evmProjectDashboard = {
  projectId: EVM_PROJECT_ID,
  projectCode: 'PRJ-EVM-001',
  projectName: 'EVM Biçimlendirme Test Projesi',
  customerName: 'Test Müşterisi',
  projectManager: 'Test Yöneticisi',
  projectStatus: 'Devam Ediyor',
  manualHealth: 'Orta',
  plannedProgress: 90,
  actualProgress: 81,
  startDate: '2026-01-01T00:00:00',
  baselineFinishDate: '2026-08-01T00:00:00',
  forecastFinishDate: '2026-08-20T00:00:00',
  milestones: [],
  risks: [],
  issues: [],
  actions: [],
  reports: [],
}

const formattedEvmData = {
  bac: 1000000,
  pv: 900000,
  ev: 810000,
  ac: 800000,
  sv: -90000,
  cv: 10000,
  spi: 0.9,
  cpi: 1,
  eac: 1000000,
  vac: 0,
}

function renderEvmProjectPage(evmResponse = formattedEvmData) {
  server.use(
    http.get(EVM_DASHBOARD_URL, () => {
      return HttpResponse.json(evmProjectDashboard)
    }),

    http.get(EVM_DETAILS_URL, () => {
      return HttpResponse.json(evmResponse)
    }),
  )

  return renderWithRouter(
    <Routes>
      <Route
        path="/projects/:id"
        element={<ProjectDetailPage />}
      />
    </Routes>,
    {
      initialEntries: [`/projects/${EVM_PROJECT_ID}`],
    },
  )
}

async function waitForEvmProjectToLoad() {
  await screen.findByRole('heading', {
    name: evmProjectDashboard.projectName,
  })
}

async function openEvmTab() {
  const user = userEvent.setup()

  await user.click(
    screen.getByRole('button', {
      name: 'EVM',
    }),
  )

  return user
}

function getSummaryCardValue(headingName) {
  const heading = screen.getByRole('heading', {
    name: headingName,
  })

  const valueElement = heading.nextElementSibling

  if (!valueElement) {
    throw new Error(`${headingName} özet değeri bulunamadı.`)
  }

  return valueElement
}

function getEvmLabeledContainer(label) {
  const labelElement = screen.getByText(label)
  const container = labelElement.parentElement

  if (!container) {
    throw new Error(`${label} EVM alanı bulunamadı.`)
  }

  return container
}

it('proje sağlık bilgisini ve API’den gelen EVM göstergelerini göstermelidir', async () => {
  const user = userEvent.setup()

  server.use(
    http.get(PROJECT_DASHBOARD_URL, () => {
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
    await screen.findByRole('heading', {
      name: projectDashboard.projectName,
    }),
  ).toBeInTheDocument()

  expect(
    screen.getByText(`● Sağlık: ${projectDashboard.manualHealth}`),
  ).toBeInTheDocument()

  expect(
    screen.getByRole('heading', {
      name: 'Planlanan İlerleme',
    }).nextElementSibling,
  ).toHaveTextContent(`%${projectDashboard.plannedProgress}`)

  expect(
    screen.getByRole('heading', {
      name: 'Gerçekleşen İlerleme',
    }).nextElementSibling,
  ).toHaveTextContent(`%${projectDashboard.actualProgress}`)

  expect(
    screen.getByRole('heading', {
      name: 'SPI (Zaman Performansı)',
    }).nextElementSibling,
  ).toHaveTextContent(String(evmData.spi))

  expect(
    screen.getByRole('heading', {
      name: 'CPI (Maliyet Performansı)',
    }).nextElementSibling,
  ).toHaveTextContent(String(evmData.cpi))

  await user.click(
    screen.getByRole('button', { name: 'EVM' }),
  )

  expect(
    screen.getByRole('heading', {
      name: 'EVM Göstergeleri ve Hesaplamaları',
    }),
  ).toBeInTheDocument()

  expectLabeledValue(
    'BAC (Tamamlanma Bütçesi):',
    evmData.bac,
  )
  expectLabeledValue('PV (Planlanan Değer):', evmData.pv)
  expectLabeledValue('EV (Kazanılan Değer):', evmData.ev)
  expectLabeledValue(
    'AC (Gerçekleşen Maliyet):',
    evmData.ac,
  )
  expectLabeledValue('SV (Takvim Sapması):', evmData.sv)
  expectLabeledValue('CV (Maliyet Sapması):', evmData.cv)
  expectLabeledValue(
    'EAC (Tahmini Tamamlanma Maliyeti):',
    evmData.eac,
  )
  expectLabeledValue(
    'VAC (Tamamlanma Sapması):',
    evmData.vac,
  )
})

describe('EVM gösterim kuralları', () => {
  it('SPI ve CPI değerlerini iki ondalık basamakla göstermelidir', async () => {
    renderEvmProjectPage()

    await waitForEvmProjectToLoad()

    const expectedSpi = new Intl.NumberFormat('tr-TR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(formattedEvmData.spi)

    const expectedCpi = new Intl.NumberFormat('tr-TR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(formattedEvmData.cpi)

    expect(
      getSummaryCardValue('SPI (Zaman Performansı)'),
    ).toHaveTextContent(expectedSpi)

    expect(
      getSummaryCardValue('CPI (Maliyet Performansı)'),
    ).toHaveTextContent(expectedCpi)
  })

  it('EVM parasal değerlerini Türkçe binlik ayırıcıyla göstermelidir', async () => {
    renderEvmProjectPage()

    await waitForEvmProjectToLoad()
    await openEvmTab()

    const formatNumber = (value) =>
      new Intl.NumberFormat('tr-TR').format(value)

    expect(
      getEvmLabeledContainer('BAC (Tamamlanma Bütçesi):'),
    ).toHaveTextContent(formatNumber(formattedEvmData.bac))

    expect(
      getEvmLabeledContainer('PV (Planlanan Değer):'),
    ).toHaveTextContent(formatNumber(formattedEvmData.pv))

    expect(
      getEvmLabeledContainer('EV (Kazanılan Değer):'),
    ).toHaveTextContent(formatNumber(formattedEvmData.ev))

    expect(
      getEvmLabeledContainer('AC (Gerçekleşen Maliyet):'),
    ).toHaveTextContent(formatNumber(formattedEvmData.ac))

    expect(
      getEvmLabeledContainer(
        'EAC (Tahmini Tamamlanma Maliyeti):',
      ),
    ).toHaveTextContent(formatNumber(formattedEvmData.eac))

    expect(
      getEvmLabeledContainer('VAC (Tamamlanma Sapması):'),
    ).toHaveTextContent(formatNumber(formattedEvmData.vac))
  })

  it('EVM verisi bulunmadığında sayısal alanlarda Veri yok göstermelidir', async () => {
    renderEvmProjectPage({})

    await waitForEvmProjectToLoad()

    expect(
      getSummaryCardValue('SPI (Zaman Performansı)'),
    ).toHaveTextContent('Veri yok')

    expect(
      getSummaryCardValue('CPI (Maliyet Performansı)'),
    ).toHaveTextContent('Veri yok')

    await openEvmTab()

    expect(
      getEvmLabeledContainer('BAC (Tamamlanma Bütçesi):'),
    ).toHaveTextContent('Veri yok')

    expect(
      getEvmLabeledContainer('PV (Planlanan Değer):'),
    ).toHaveTextContent('Veri yok')

    expect(
      getEvmLabeledContainer('EV (Kazanılan Değer):'),
    ).toHaveTextContent('Veri yok')

    expect(
      getEvmLabeledContainer('AC (Gerçekleşen Maliyet):'),
    ).toHaveTextContent('Veri yok')

    expect(
      getEvmLabeledContainer('SV (Takvim Sapması):'),
    ).toHaveTextContent('Veri yok')

    expect(
      getEvmLabeledContainer('CV (Maliyet Sapması):'),
    ).toHaveTextContent('Veri yok')

    expect(
      getEvmLabeledContainer(
        'EAC (Tahmini Tamamlanma Maliyeti):',
      ),
    ).toHaveTextContent('Veri yok')

    expect(
      getEvmLabeledContainer('VAC (Tamamlanma Sapması):'),
    ).toHaveTextContent('Veri yok')
  })

  it('EVM sekmesinde PV EV ve AC değerlerini karşılaştıran grafik göstermelidir', async () => {
    renderEvmProjectPage()

    await waitForEvmProjectToLoad()
    await openEvmTab()

    const chartHeading = screen.queryByRole('heading', {
      name: /PV.*EV.*AC.*Karşılaştır/i,
    })

    const accessibleChart = screen.queryByRole('img', {
      name: /PV.*EV.*AC/i,
    })

    expect(
      chartHeading ?? accessibleChart,
    ).toBeInTheDocument()
  })
})