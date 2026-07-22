import { useState, useEffect } from 'react';
import { projectService } from '../services/projectService';
import './ReportsPage.css';

function ReportsPage() {
  const [reportType, setReportType] = useState('pir');
  const [projectId, setProjectId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  const [projects, setProjects] = useState([]);
  const [isProjectsLoading, setIsProjectsLoading] = useState(true);

  const [contentSelection, setContentSelection] = useState({
    executiveSummary: true,
    progress: true,
    milestones: true,
    budgetEVM: true,
    risks: true,
    actions: true,
    issues: true,
    resources: true,
  });

  const [outputFormats, setOutputFormats] = useState({
    pdf: true,
    excel: false,
  });

  useEffect(() => {
    let isMounted = true;
    const fetchProjects = async () => {
      try {
        const data = await projectService.getProjects();
        if (isMounted) setProjects(Array.isArray(data) ? data : []);
      } catch (err) {
        console.error('Projeler çekilirken hata:', err);
      } finally {
        if (isMounted) setIsProjectsLoading(false);
      }
    };
    fetchProjects();
    return () => { isMounted = false; };
  }, []);

  const handleContentChange = (key) => {
    setContentSelection((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const handleFormatChange = (key) => {
    setOutputFormats((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const handleGenerateReport = async (e) => {
    e.preventDefault();
    if (!projectId) {
      setError("Lütfen rapor oluşturmak için bir proje seçin.");
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const format = outputFormats.excel ? 'excel' : 'pdf';
      const blob = await projectService.generateReport(projectId, format);

      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      
      const selectedProject = projects.find(p => p.projectId === projectId || p.id === projectId);
      const pName = selectedProject ? (selectedProject.projectCode || selectedProject.projectName) : 'PIR';
      
      a.download = format === 'excel' ? `${pName}_Raporu.xlsx` : `${pName}_Raporu.pdf`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);

    } catch (err) {
      console.error('Hata:', err);
      setError(err.message || err.response?.data?.message || 'Rapor indirilirken bir hata oluştu.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="report-generator-card">
      <h2>Rapor Oluştur</h2>
      
      <div className="rg-container">
        {/* SOL KOLON - FORM */}
        <form className="rg-form-section" onSubmit={handleGenerateReport}>
          
          <div className="form-group">
            <label>Rapor Tipi</label>
            <select 
              className="form-control" 
              value={reportType} 
              onChange={(e) => setReportType(e.target.value)}
            >
              <option value="pir">Proje İlerleme Raporu (PİR)</option>
              <option value="status">Statü Raporu</option>
            </select>
          </div>

          <div className="form-group">
            <label>Proje</label>
            <select 
              className="form-control" 
              value={projectId} 
              onChange={(e) => setProjectId(e.target.value)} 
              disabled={isProjectsLoading}
            >
              <option value="">{isProjectsLoading ? 'Projeler yükleniyor...' : 'Proje Seçiniz...'}</option>
              {projects.map((project) => {
                const pId = project.id || project.projectId || project.projectCode;
                return (
                  <option key={pId} value={pId}>
                    {project.projectCode} - {project.projectName}
                  </option>
                );
              })}
            </select>
          </div>

          <div className="form-group">
            <label>Rapor Dönemi</label>
            <div className="rg-date-range">
              <input 
                type="date" 
                value={startDate} 
                onChange={(e) => setStartDate(e.target.value)} 
              />
              <span className="rg-date-separator">-</span>
              <input 
                type="date" 
                value={endDate} 
                onChange={(e) => setEndDate(e.target.value)} 
              />
            </div>
          </div>

          <div className="form-group">
            <label>İçerik Seçimi</label>
            <div className="rg-checkbox-grid">
              {[
                { key: 'executiveSummary', label: 'Yönetici Özeti' },
                { key: 'risks', label: 'Riskler' },
                { key: 'progress', label: 'İlerleme Durumu' },
                { key: 'actions', label: 'Aksiyonlar' },
                { key: 'milestones', label: 'Kilometre Taşları' },
                { key: 'issues', label: 'Sorunlar' },
                { key: 'budgetEVM', label: 'Bütçe (EVM)' },
                { key: 'resources', label: 'Kaynaklar' },
              ].map((item) => (
                <label key={item.key} className="rg-custom-checkbox">
                  <input
                    type="checkbox"
                    checked={contentSelection[item.key]}
                    onChange={() => handleContentChange(item.key)}
                  />
                  <span>{item.label}</span>
                </label>
              ))}
            </div>
          </div>

          {error && (
            <div className="rg-error-message">
              {error}
            </div>
          )}

          <div className="rg-actions">
            <button 
              type="submit" 
              className="rg-submit-btn" 
              disabled={isLoading || isProjectsLoading}
            >
              {isLoading ? 'Oluşturuluyor...' : 'Raporu Oluştur'}
            </button>
          </div>
        </form>

        {/* SAĞ KOLON - ÇIKTI FORMATI & GRAFİK */}
        <div className="rg-preview-section">
          <div className="form-group">
            <label>Çıktı Formatı</label>
            <div className="rg-format-options">
              <label className="rg-custom-checkbox">
                <input
                  type="checkbox"
                  checked={outputFormats.pdf}
                  onChange={() => handleFormatChange('pdf')}
                />
                <span>PDF</span>
              </label>
              <label className="rg-custom-checkbox">
                <input
                  type="checkbox"
                  checked={outputFormats.excel}
                  onChange={() => handleFormatChange('excel')}
                />
                <span>Excel</span>
              </label>
            </div>
          </div>

          <div className="rg-illustration-wrapper">
            <div className="rg-mock-document">
              <div className="rg-mock-header"></div>
              <div className="rg-mock-chart-row">
                <div className="rg-mock-circle-chart"></div>
                <div className="rg-mock-lines">
                  <div className="rg-mock-line"></div>
                  <div className="rg-mock-line w-75"></div>
                </div>
              </div>
              <div className="rg-mock-pie-chart"></div>
              <div className="rg-mock-lines" style={{ marginTop: '12px' }}>
                <div className="rg-mock-line"></div>
                <div className="rg-mock-line w-50"></div>
                <div className="rg-mock-line w-75"></div>
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>
  );
}

export default ReportsPage;