export const projectDetail = {
  projectId: 1,
  projectCode: 'PRJ-001',
  projectName: 'Detay Test Projesi',
  customerName: 'Test Müşterisi',
  projectManager: 'Test Yöneticisi',
  projectStatus: 'Aktif',
  manualHealth: 'İyi',
  plannedProgress: 60,
  actualProgress: 55,
  startDate: '2026-01-15T00:00:00',
  baselineFinishDate: '2026-10-01T00:00:00',
  forecastFinishDate: '2026-10-20T00:00:00',
  executiveSummary: 'Test projesi için yayımlanmış yönetici özeti.',
  openRiskCount: 1,
  openIssueCount: 1,
  openActionCount: 1,

  milestones: [
    {
      id: 11,
      name: 'Analiz Tamamlandı',
      owner: 'Test Kullanıcısı',
      plannedDate: '2026-03-01T00:00:00',
      forecastDate: '2026-03-05T00:00:00',
      actualDate: '2026-03-04T00:00:00',
      critical: true,
      status: 'Tamamlandı',
    },
  ],

  risks: [
    {
      id: 21,
      title: 'Takvim gecikmesi riski',
      category: 'Takvim',
      probability: 4,
      impact: 5,
      owner: 'Risk Sorumlusu',
      status: 'Açık',
    },
  ],

  issues: [
    {
      id: 31,
      title: 'Kaynak eksikliği',
      priority: 'Yüksek',
      dueDate: '2026-05-15T00:00:00',
      owner: 'Sorun Sorumlusu',
      status: 'Açık',
    },
  ],

  actions: [
    {
      id: 41,
      description: 'Ek kaynak planı hazırlanacak',
      sourceType: 'Risk',
      owner: 'Aksiyon Sorumlusu',
      dueDate: '2026-05-20T00:00:00',
      progress: 40,
      status: 'Devam Ediyor',
    },
  ],

  reports: [
    {
      id: 51,
      period: '2026-04',
      reportDate: '2026-04-30T00:00:00',
      status: 'Yayımlandı',
      executiveSummary: 'Nisan dönemi test raporu yönetici özeti.',
    },
  ],
}

export const projectEvm = {
  bac: 1000000,
  pv: 600000,
  ev: 550000,
  ac: 580000,
  sv: -50000,
  cv: -30000,
  spi: 0.92,
  cpi: 0.95,
  eac: 1052632,
  vac: -52632,
}

export const projectEvmList = [
  projectEvm,
]

export const emptyProjectDetail = {
  ...projectDetail,
  executiveSummary: '',
  openRiskCount: 0,
  openIssueCount: 0,
  openActionCount: 0,
  milestones: [],
  risks: [],
  issues: [],
  actions: [],
  reports: [],
}