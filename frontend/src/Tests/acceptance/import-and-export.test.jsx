/*import { describe, expect, it } from 'vitest'
import { HttpResponse, http } from 'msw'
import { projects } from '../fixtures/projects'
import { server } from '../mocks/server'
import ProjectsPage from '../../pages/ProjectsPage'
import {
  renderWithRouter,
  screen,
} from '../utils/test-utils'
import ReportsPage from '../../pages/ReportsPage'

describe('Import and Export', () => {
  it('Excel içe aktarma akışında gerekli temel kontrolleri göstermelidir', () => {
    renderWithRouter(<ReportsPage />)

    expect(
      screen.getByRole('button', { name: 'Şablon İndir' }),
    ).toBeInTheDocument()

    expect(
      screen.getByLabelText(/Dosya Seç/i),
    ).toHaveAttribute('type', 'file')

    expect(
      screen.getByRole('button', { name: 'Ön İzleme' }),
    ).toBeInTheDocument()

    expect(
      screen.getByRole('button', { name: 'İçe Aktar' }),
    ).toBeInTheDocument()
  })
    it('Raporlar ekranında proje ve dönem seçilerek Türkçe PDF oluşturulabilmelidir', () => {
    renderWithRouter(<ReportsPage />)

    expect(
        screen.getByRole('combobox', { name: /Proje/i }),
    ).toBeInTheDocument()

    expect(
        screen.getByRole('combobox', { name: /PİR Dönemi/i }),
    ).toBeInTheDocument()

    expect(
        screen.getByRole('button', { name: 'PDF Oluştur' }),
    ).toBeInTheDocument()

    expect(
        screen.getByRole('button', { name: 'PDF İndir' }),
    ).toBeInTheDocument()
    })
    it('Projeler için Excel dışa aktarma kontrolü göstermelidir', async () => {
    server.use(
        http.get('http://localhost:5074/projects', () => {
        return HttpResponse.json(projects)
        }),
    )

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

    expect(exportButton ?? exportLink).toBeInTheDocument()
    })
})*/