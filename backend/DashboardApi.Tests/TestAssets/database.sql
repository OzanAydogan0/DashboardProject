-- SQLite ayarlarını etkinleştirelim
PRAGMA foreign_keys = OFF; -- Oluşturma esnasında FK çakışmalarını önlemek için geçici olarak kapatıyoruz

-- VARSA ESKİ YAPILARI SİLELİM (Temiz başlangıç için)
DROP VIEW IF EXISTS vw_evm;
DROP VIEW IF EXISTS vw_pir;
DROP VIEW IF EXISTS vw_risk;
DROP VIEW IF EXISTS vw_dashboard;

DROP TABLE IF EXISTS audit_logs;
DROP TABLE IF EXISTS management_decisions;
DROP TABLE IF EXISTS evm_records;
DROP TABLE IF EXISTS actions;
DROP TABLE IF EXISTS issues;
DROP TABLE IF EXISTS risks;
DROP TABLE IF EXISTS milestones;
DROP TABLE IF EXISTS pir_reports;
DROP TABLE IF EXISTS project_users;
DROP TABLE IF EXISTS projects;
DROP TABLE IF EXISTS customers;
DROP TABLE IF EXISTS programs;
DROP TABLE IF EXISTS users;

PRAGMA foreign_keys = ON; -- Yabancı anahtarları tekrar etkinleştiriyoruz


CREATE TABLE users (
    user_id TEXT NOT NULL,
    email TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    full_name TEXT NOT NULL,
    user_role TEXT NOT NULL,
    user_status TEXT DEFAULT 'Aktif' NOT NULL,
    last_login_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_users PRIMARY KEY (user_id),
    CONSTRAINT uq_users_email UNIQUE (email),
    CONSTRAINT ck_users_user_role CHECK (user_role IN ('Sistem Yöneticisi','Proje Yöneticisi','Üst Yönetim İzleyicisi')),
    CONSTRAINT ck_users_user_status CHECK (user_status IN ('Aktif','Pasif'))
);

CREATE TABLE programs (
    program_id TEXT NOT NULL,
    program_name TEXT NULL,
    program_description TEXT,
    program_status TEXT DEFAULT 'Aktif' NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_programs PRIMARY KEY (program_id),
    CONSTRAINT uq_programs_program_name UNIQUE (program_name),
    CONSTRAINT ck_programs_program_status CHECK (program_status IN ('Aktif','Pasif'))
);

CREATE TABLE customers (
    customer_id TEXT NOT NULL,
    customer_name TEXT NULL,
    customer_type TEXT NOT NULL,
    customer_status TEXT DEFAULT 'Aktif' NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_customers PRIMARY KEY (customer_id),
    CONSTRAINT ck_customers_customer_status CHECK (customer_status IN ('Aktif','Pasif'))
);

CREATE TABLE projects (
    project_id TEXT NOT NULL,
    project_code TEXT NOT NULL,
    project_name TEXT NOT NULL,
    program_id TEXT NOT NULL,
    customer_id TEXT NOT NULL,
    project_manager_user_id TEXT NOT NULL,
    start_date DATE NOT NULL,
    baseline_finish_date DATE NOT NULL,
    forecast_finish_date DATE NOT NULL,
    actual_finish_date DATE,
    project_status TEXT DEFAULT 'Taslak' NOT NULL,
    manual_health TEXT DEFAULT 'Gri' NOT NULL,
    planned_progress NUMERIC DEFAULT 0 NOT NULL,
    actual_progress NUMERIC DEFAULT 0 NOT NULL,
    bac NUMERIC DEFAULT 0 NOT NULL,
    currency TEXT DEFAULT 'TRY' NOT NULL,
    reporting_frequency TEXT DEFAULT 'Aylık' NOT NULL,
    confidentiality TEXT DEFAULT 'Şirket İçi' NOT NULL,
    project_description TEXT,
    is_active INTEGER DEFAULT 1 NOT NULL,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_projects PRIMARY KEY (project_id),
    CONSTRAINT uq_projects_project_code UNIQUE (project_code),
    CONSTRAINT fk_projects_program_id FOREIGN KEY (program_id) REFERENCES programs (program_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_projects_customer_id FOREIGN KEY (customer_id) REFERENCES customers (customer_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_projects_project_manager_user_id FOREIGN KEY (project_manager_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_projects_project_status CHECK (project_status IN ('Taslak','Aktif','Beklemede','Tamamlandı','Pasif')),
    CONSTRAINT ck_projects_manual_health CHECK (manual_health IN ('Yeşil','Sarı','Kırmızı','Gri')),
    CONSTRAINT ck_projects_planned_progress CHECK (planned_progress BETWEEN 0 AND 100),
    CONSTRAINT ck_projects_actual_progress CHECK (actual_progress BETWEEN 0 AND 100),
    CONSTRAINT ck_projects_bac CHECK (bac >= 0),
    CONSTRAINT ck_projects_currency CHECK (currency IN ('TRY','USD','EUR')),
    CONSTRAINT ck_projects_reporting_frequency CHECK (reporting_frequency IN ('Haftalık','Aylık','Üç Aylık')),
    CONSTRAINT ck_projects_confidentiality CHECK (confidentiality IN ('Şirket İçi','Hizmete Özel','Gizli')),
    CONSTRAINT fk_projects_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_projects_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_projects_finish_dates CHECK (baseline_finish_date >= start_date AND forecast_finish_date >= start_date AND (actual_finish_date IS NULL OR actual_finish_date >= start_date))
);

CREATE TABLE project_users (
    project_user_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    user_id TEXT NOT NULL,
    assigned_by_user_id TEXT NOT NULL,
    assignment_status TEXT DEFAULT 'Aktif' NOT NULL,
    assigned_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_project_users PRIMARY KEY (project_user_id),
    CONSTRAINT uq_project_users_project_user UNIQUE (project_id, user_id),
    CONSTRAINT fk_project_users_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_project_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_project_users_assigned_by_user_id FOREIGN KEY (assigned_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_project_users_assignment_status CHECK (assignment_status IN ('Aktif','Pasif'))
);

CREATE TABLE pir_reports (
    pir_report_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    period TEXT NOT NULL,
    report_date DATE DEFAULT CURRENT_DATE NOT NULL,
    executive_summary TEXT NOT NULL,
    completed_work TEXT NOT NULL,
    delays TEXT,
    next_period_plan TEXT NOT NULL,
    management_expectations TEXT,
    manual_health TEXT DEFAULT 'Gri' NOT NULL,
    report_status TEXT DEFAULT 'Taslak' NOT NULL,
    published_by_user_id TEXT,
    published_at DATETIME,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_pir_reports PRIMARY KEY (pir_report_id),
    CONSTRAINT uq_pir_reports_project_period UNIQUE (project_id, period),
    CONSTRAINT fk_pir_reports_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT ck_pir_reports_period CHECK (length(period) = 7 AND substr(period, 1, 4) GLOB '[0-9][0-9][0-9][0-9]' AND substr(period, 5, 1) = '-' AND substr(period, 6, 2) BETWEEN '01' AND '12'),
    CONSTRAINT ck_pir_reports_manual_health CHECK (manual_health IN ('Yeşil','Sarı','Kırmızı','Gri')),
    CONSTRAINT ck_pir_reports_report_status CHECK (report_status IN ('Taslak','Yayımlandı')),
    CONSTRAINT fk_pir_reports_published_by_user_id FOREIGN KEY (published_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_pir_reports_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_pir_reports_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_pir_reports_publish_consistency CHECK ((report_status = 'Taslak' AND published_at IS NULL) OR (report_status = 'Yayımlandı' AND published_at IS NOT NULL AND published_by_user_id IS NOT NULL))
);

CREATE TABLE milestones (
    milestone_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    milestone_name TEXT NOT NULL,
    planned_date DATE NOT NULL,
    forecast_date DATE NOT NULL,
    actual_date DATE,
    milestone_status TEXT DEFAULT 'Planlandı' NOT NULL,
    critical INTEGER DEFAULT 0 NOT NULL,
    milestone_owner_user_id TEXT NOT NULL,
    acceptance_criteria TEXT NOT NULL,
    milestone_description TEXT,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_milestones PRIMARY KEY (milestone_id),
    CONSTRAINT fk_milestones_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT ck_milestones_milestone_status CHECK (milestone_status IN ('Planlandı','Devam Ediyor','Tamamlandı','Gecikti','İptal')),
    CONSTRAINT fk_milestones_milestone_owner_user_id FOREIGN KEY (milestone_owner_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_milestones_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_milestones_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_milestones_date_order CHECK (forecast_date >= planned_date OR actual_date IS NOT NULL)
   

);

CREATE TABLE risks (
    risk_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    risk_title TEXT NOT NULL,
    risk_category TEXT NOT NULL,
    risk_probability INTEGER NOT NULL,
    risk_impact INTEGER NOT NULL,
    risk_score INTEGER DEFAULT 0 NOT NULL, 
    risk_owner_user_id TEXT NOT NULL,
    risk_mitigation TEXT NOT NULL,
    risk_due_date DATE NOT NULL,
    risk_status TEXT DEFAULT 'Açık' NOT NULL,
    opened_date DATE DEFAULT CURRENT_DATE NOT NULL,
    closed_date DATE,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_risks PRIMARY KEY (risk_id),
    CONSTRAINT fk_risks_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT ck_risks_risk_probability CHECK (risk_probability BETWEEN 1 AND 5),
    CONSTRAINT ck_risks_risk_impact CHECK (risk_impact BETWEEN 1 AND 5),
    CONSTRAINT fk_risks_risk_owner_user_id FOREIGN KEY (risk_owner_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_risks_risk_status CHECK (risk_status IN ('Açık','İzleniyor','Azaltıldı','Kapalı')),
    CONSTRAINT fk_risks_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_risks_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_risks_close_date CHECK (closed_date IS NULL OR closed_date >= opened_date)
);

CREATE TABLE issues (
    issue_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    risk_id TEXT,
    issue_title TEXT NOT NULL,
    issue_priority TEXT NOT NULL,
    issue_owner_user_id TEXT NOT NULL,
    issue_due_date DATE NOT NULL,
    issue_status TEXT DEFAULT 'Açık' NOT NULL,
    issue_impact TEXT NOT NULL,
    root_cause TEXT,
    issue_resolution TEXT,
    opened_date DATE DEFAULT CURRENT_DATE NOT NULL,
    closed_date DATE,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_issues PRIMARY KEY (issue_id),
    CONSTRAINT fk_issues_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_issues_risk_id FOREIGN KEY (risk_id) REFERENCES risks (risk_id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT ck_issues_issue_priority CHECK (issue_priority IN ('Düşük','Orta','Yüksek','Kritik')),
    CONSTRAINT fk_issues_issue_owner_user_id FOREIGN KEY (issue_owner_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_issues_issue_status CHECK (issue_status IN ('Açık','Devam Ediyor','Çözüldü','Kapalı')),
    CONSTRAINT ck_issues_issue_impact CHECK (issue_impact IN ('Düşük','Orta','Yüksek','Kritik')),
    CONSTRAINT fk_issues_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_issues_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_issues_close_date CHECK (closed_date IS NULL OR closed_date >= opened_date)
);

CREATE TABLE actions (
    action_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    risk_id TEXT,
    issue_id TEXT,
    action_description TEXT NOT NULL,
    source_type TEXT NOT NULL,
    source_reference TEXT,
    action_owner_user_id TEXT NOT NULL,
    action_due_date DATE NOT NULL,
    action_status TEXT DEFAULT 'Açık' NOT NULL,
    action_progress NUMERIC DEFAULT 0 NOT NULL,
    action_priority TEXT NOT NULL,
    completed_date DATE,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_actions PRIMARY KEY (action_id),
    CONSTRAINT fk_actions_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_actions_risk_id FOREIGN KEY (risk_id) REFERENCES risks (risk_id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT fk_actions_issue_id FOREIGN KEY (issue_id) REFERENCES issues (issue_id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT ck_actions_source_type CHECK (source_type IN ('Risk','Sorun','Kilometre Taşı','PIR','Yönetim Kararı','Diğer')),
    CONSTRAINT fk_actions_action_owner_user_id FOREIGN KEY (action_owner_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_actions_action_status CHECK (action_status IN ('Açık','Devam Ediyor','Tamamlandı','İptal')),
    CONSTRAINT ck_actions_action_progress CHECK (action_progress BETWEEN 0 AND 100),
    CONSTRAINT ck_actions_action_priority CHECK (action_priority IN ('Düşük','Orta','Yüksek','Kritik')),
    CONSTRAINT fk_actions_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_actions_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_actions_completed_date CHECK (completed_date IS NULL OR completed_date >= action_due_date OR action_status = 'Tamamlandı')
);

CREATE TABLE evm_records (
    evm_record_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    period TEXT NOT NULL,
    bac NUMERIC NOT NULL,
    pv NUMERIC NOT NULL,
    ev NUMERIC NOT NULL,
    ac NUMERIC NOT NULL,
    sv NUMERIC,
    cv NUMERIC,
    spi NUMERIC,
    cpi NUMERIC,
    eac NUMERIC,
    vac NUMERIC,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_evm_records PRIMARY KEY (evm_record_id),
    CONSTRAINT uq_evm_records_project_period UNIQUE (project_id, period),
    CONSTRAINT fk_evm_records_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT ck_evm_records_period CHECK (length(period) = 7 AND substr(period, 1, 4) GLOB '[0-9][0-9][0-9][0-9]' AND substr(period, 5, 1) = '-' AND substr(period, 6, 2) BETWEEN '01' AND '12'),
    CONSTRAINT ck_evm_records_bac CHECK (bac >= 0),
    CONSTRAINT ck_evm_records_pv CHECK (pv >= 0),
    CONSTRAINT ck_evm_records_ev CHECK (ev >= 0),
    CONSTRAINT ck_evm_records_ac CHECK (ac >= 0),
    CONSTRAINT fk_evm_records_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_evm_records_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_evm_records_spi CHECK (spi IS NULL OR spi >= 0),
    CONSTRAINT ck_evm_records_cpi CHECK (cpi IS NULL OR cpi >= 0)
);

CREATE TABLE management_decisions (
    management_decision_id TEXT NOT NULL,
    project_id TEXT NOT NULL,
    decision_title TEXT NOT NULL,
    decision TEXT NOT NULL,
    decision_owner_user_id TEXT NOT NULL,
    decision_due_date DATE NOT NULL,
    decision_status TEXT DEFAULT 'Açık' NOT NULL,
    decision_impact TEXT NOT NULL,
    if_delayed TEXT,
    recommendation TEXT,
    decision_date DATE DEFAULT CURRENT_DATE NOT NULL,
    created_by_user_id TEXT NOT NULL,
    updated_by_user_id TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    CONSTRAINT pk_management_decisions PRIMARY KEY (management_decision_id),
    CONSTRAINT fk_management_decisions_project_id FOREIGN KEY (project_id) REFERENCES projects (project_id) ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_management_decisions_decision_owner_user_id FOREIGN KEY (decision_owner_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT ck_management_decisions_decision_status CHECK (decision_status IN ('Açık','Beklemede','Uygulanıyor','Tamamlandı','İptal')),
    CONSTRAINT ck_management_decisions_decision_impact CHECK (decision_impact IN ('Düşük','Orta','Yüksek','Kritik')),
    CONSTRAINT fk_management_decisions_created_by_user_id FOREIGN KEY (created_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT fk_management_decisions_updated_by_user_id FOREIGN KEY (updated_by_user_id) REFERENCES users (user_id) ON DELETE RESTRICT ON UPDATE CASCADE
);

CREATE TABLE audit_logs (
    audit_log_id TEXT NOT NULL,
    user_id TEXT,
    entity_name TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    action_type TEXT NOT NULL,
    old_values TEXT, 
    new_values TEXT, 
    changed_at DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    ip_address TEXT, 
    CONSTRAINT pk_audit_logs PRIMARY KEY (audit_log_id),
    CONSTRAINT fk_audit_logs_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT ck_audit_logs_action_type CHECK (action_type IN ('INSERT','UPDATE','DELETE'))
);

CREATE TRIGGER trg_users_updated_at AFTER UPDATE ON users FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE users SET updated_at = CURRENT_TIMESTAMP WHERE user_id = OLD.user_id; END;

CREATE TRIGGER trg_programs_updated_at AFTER UPDATE ON programs FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE programs SET updated_at = CURRENT_TIMESTAMP WHERE program_id = OLD.program_id; END;

CREATE TRIGGER trg_customers_updated_at AFTER UPDATE ON customers FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE customers SET updated_at = CURRENT_TIMESTAMP WHERE customer_id = OLD.customer_id; END;

CREATE TRIGGER trg_projects_updated_at AFTER UPDATE ON projects FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE projects SET updated_at = CURRENT_TIMESTAMP WHERE project_id = OLD.project_id; END;

CREATE TRIGGER trg_project_users_updated_at AFTER UPDATE ON project_users FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE project_users SET updated_at = CURRENT_TIMESTAMP WHERE project_user_id = OLD.project_user_id; END;

CREATE TRIGGER trg_pir_reports_updated_at AFTER UPDATE ON pir_reports FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE pir_reports SET updated_at = CURRENT_TIMESTAMP WHERE pir_report_id = OLD.pir_report_id; END;

CREATE TRIGGER trg_milestones_updated_at AFTER UPDATE ON milestones FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE milestones SET updated_at = CURRENT_TIMESTAMP WHERE milestone_id = OLD.milestone_id; END;

CREATE TRIGGER trg_issues_updated_at AFTER UPDATE ON issues FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE issues SET updated_at = CURRENT_TIMESTAMP WHERE issue_id = OLD.issue_id; END;

CREATE TRIGGER trg_actions_updated_at AFTER UPDATE ON actions FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE actions SET updated_at = CURRENT_TIMESTAMP WHERE action_id = OLD.action_id; END;

CREATE TRIGGER trg_evm_records_updated_at AFTER UPDATE ON evm_records FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE evm_records SET updated_at = CURRENT_TIMESTAMP WHERE evm_record_id = OLD.evm_record_id; END;

CREATE TRIGGER trg_management_decisions_updated_at AFTER UPDATE ON management_decisions FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE management_decisions SET updated_at = CURRENT_TIMESTAMP WHERE management_decision_id = OLD.management_decision_id; END;

CREATE TRIGGER trg_risks_updated_at AFTER UPDATE ON risks FOR EACH ROW WHEN NEW.updated_at <= OLD.updated_at
BEGIN UPDATE risks SET updated_at = CURRENT_TIMESTAMP WHERE risk_id = OLD.risk_id; END;

CREATE TRIGGER trg_risks_calc_score_insert AFTER INSERT ON risks
BEGIN
    UPDATE risks SET risk_score = NEW.risk_probability * NEW.risk_impact WHERE risk_id = NEW.risk_id;
END;

CREATE TRIGGER trg_risks_calc_score_update AFTER UPDATE OF risk_probability, risk_impact ON risks FOR EACH ROW
BEGIN
    UPDATE risks SET risk_score = NEW.risk_probability * NEW.risk_impact WHERE risk_id = NEW.risk_id;
END;

CREATE INDEX idx_projects_program_id ON projects (program_id);
CREATE INDEX idx_projects_customer_id ON projects (customer_id);
CREATE INDEX idx_projects_project_manager_user_id ON projects (project_manager_user_id);
CREATE INDEX idx_projects_status_active ON projects (project_status, is_active);
CREATE INDEX idx_projects_forecast_finish_date ON projects (forecast_finish_date) WHERE is_active = 1;
CREATE INDEX idx_project_users_user_id_active ON project_users (user_id, project_id) WHERE assignment_status = 'Aktif';
CREATE INDEX idx_pir_reports_project_report_date ON pir_reports (project_id, report_date DESC);
CREATE INDEX idx_pir_reports_status ON pir_reports (report_status, report_date DESC);
CREATE INDEX idx_milestones_project_dates ON milestones (project_id, forecast_date);
CREATE INDEX idx_milestones_critical_open ON milestones (project_id, forecast_date) WHERE critical = 1 AND milestone_status <> 'Tamamlandı';
CREATE INDEX idx_risks_project_status_score ON risks (project_id, risk_status, risk_score DESC);
CREATE INDEX idx_risks_owner_due ON risks (risk_owner_user_id, risk_due_date) WHERE risk_status <> 'Kapalı';
CREATE INDEX idx_issues_project_status_priority ON issues (project_id, issue_status, issue_priority);
CREATE INDEX idx_issues_owner_due ON issues (issue_owner_user_id, issue_due_date) WHERE issue_status <> 'Kapalı';
CREATE INDEX idx_issues_risk_id ON issues (risk_id);
CREATE INDEX idx_actions_project_status_due ON actions (project_id, action_status, action_due_date);
CREATE INDEX idx_actions_owner_due ON actions (action_owner_user_id, action_due_date) WHERE action_status <> 'Tamamlandı';
CREATE INDEX idx_actions_risk_id ON actions (risk_id);
CREATE INDEX idx_actions_issue_id ON actions (issue_id);
CREATE INDEX idx_evm_records_project_period_desc ON evm_records (project_id, period DESC);
CREATE INDEX idx_management_decisions_project_status_due ON management_decisions (project_id, decision_status, decision_due_date);
CREATE INDEX idx_audit_logs_entity ON audit_logs (entity_name, entity_id, changed_at DESC);
CREATE INDEX idx_audit_logs_user_changed_at ON audit_logs (user_id, changed_at DESC);
CREATE INDEX idx_audit_logs_changed_at ON audit_logs (changed_at); 

CREATE VIEW vw_dashboard AS
SELECT
    p.project_id,
    p.project_code,
    p.project_name,
    p.project_status,
    p.manual_health,
    p.planned_progress,
    p.actual_progress,
    p.baseline_finish_date,
    p.forecast_finish_date,
    p.bac,
    p.currency,
    COUNT(DISTINCT CASE WHEN r.risk_status <> 'Kapalı' THEN r.risk_id END) AS open_risk_count,
    COUNT(DISTINCT CASE WHEN i.issue_status <> 'Kapalı' THEN i.issue_id END) AS open_issue_count,
    COUNT(DISTINCT CASE WHEN a.action_status <> 'Tamamlandı' THEN a.action_id END) AS open_action_count,
    COUNT(DISTINCT CASE WHEN m.milestone_status <> 'Tamamlandı' THEN m.milestone_id END) AS open_milestone_count,
    MAX(e.period) AS latest_evm_period
FROM projects p
LEFT JOIN risks r ON r.project_id = p.project_id
LEFT JOIN issues i ON i.project_id = p.project_id
LEFT JOIN actions a ON a.project_id = p.project_id
LEFT JOIN milestones m ON m.project_id = p.project_id
LEFT JOIN evm_records e ON e.project_id = p.project_id
WHERE p.is_active = 1
GROUP BY p.project_id;

CREATE VIEW vw_risk AS
SELECT
    r.risk_id,
    r.project_id,
    p.project_code,
    p.project_name,
    r.risk_title,
    r.risk_category,
    r.risk_probability,
    r.risk_impact,
    r.risk_score,
    r.risk_status,
    r.risk_due_date,
    r.risk_owner_user_id,
    u.full_name AS risk_owner_full_name,
    CASE
        WHEN r.risk_score BETWEEN 1 AND 4 THEN 'Yeşil'
        WHEN r.risk_score BETWEEN 5 AND 15 THEN 'Sarı'
        ELSE 'Kırmızı'
    END AS risk_health
FROM risks r
JOIN projects p ON p.project_id = r.project_id
JOIN users u ON u.user_id = r.risk_owner_user_id;

CREATE VIEW vw_pir AS
SELECT
    pr.pir_report_id,
    pr.project_id,
    p.project_code,
    p.project_name,
    pr.period,
    pr.report_date,
    pr.executive_summary,
    pr.completed_work,
    pr.delays,
    pr.next_period_plan,
    pr.management_expectations,
    pr.manual_health,
    pr.report_status,
    pr.published_at,
    pr.published_by_user_id
FROM pir_reports pr
JOIN projects p ON p.project_id = pr.project_id;

CREATE VIEW vw_evm AS
SELECT
    e.evm_record_id,
    e.project_id,
    p.project_code,
    p.project_name,
    e.period,
    e.bac,
    e.pv,
    e.ev,
    e.ac,
    (e.ev - e.pv) AS sv,
    (e.ev - e.ac) AS cv,
    CASE WHEN e.pv = 0 THEN NULL ELSE ROUND((1.0 * e.ev) / e.pv, 4) END AS spi,
    CASE WHEN e.ac = 0 THEN NULL ELSE ROUND((1.0 * e.ev) / e.ac, 4) END AS cpi,
    CASE WHEN e.ac = 0 OR e.ev = 0 THEN NULL ELSE ROUND((1.0 * e.bac * e.ac) / e.ev, 2) END AS eac,
    CASE WHEN e.ac = 0 OR e.ev = 0 THEN NULL ELSE ROUND(e.bac - ((1.0 * e.bac * e.ac) / e.ev), 2) END AS vac
FROM evm_records e
JOIN projects p ON p.project_id = e.project_id;


-- ==========================================
-- 5. SENTETİK TEST VERİLERİNİN DOLDURULMASI
-- ==========================================

-- A. KULLANICILAR
-- Bu hesaplar yalnızca fixture ilişkileri içindir; oturum açma testleri çalışma
-- anında kendi kullanıcılarını oluşturur. BCrypt biçimli değer sentetik bir
-- işaretçidir ve bilinen bir parolaya karşılık gelmez.
INSERT INTO users (user_id, email, password_hash, full_name, user_role, user_status) VALUES
('USR-ADMIN', 'fixture.admin@example.test', '$2b$04$abcdefghijklmnopqrstuuABCDEFGHIJKLMNOPQRSTUVWXYZ01234', 'Test Sistem Yöneticisi', 'Sistem Yöneticisi', 'Aktif'),
('USR-PM1', 'fixture.pm.alfa@example.test', '$2b$04$abcdefghijklmnopqrstuuABCDEFGHIJKLMNOPQRSTUVWXYZ01234', 'Test Proje Sorumlusu Alfa', 'Proje Yöneticisi', 'Aktif'),
('USR-PM2', 'fixture.pm.beta@example.test', '$2b$04$abcdefghijklmnopqrstuuABCDEFGHIJKLMNOPQRSTUVWXYZ01234', 'Test Proje Sorumlusu Beta', 'Proje Yöneticisi', 'Aktif'),
('USR-YONETIM', 'fixture.izleyici@example.test', '$2b$04$abcdefghijklmnopqrstuuABCDEFGHIJKLMNOPQRSTUVWXYZ01234', 'Test Yönetim İzleyicisi', 'Üst Yönetim İzleyicisi', 'Aktif');

-- B. PROGRAMLAR
INSERT INTO programs (program_id, program_name, program_description, program_status) VALUES
('PRG-001', 'Örnek Program Alfa', 'Tamamen sentetik program verisi Alfa.', 'Aktif'),
('PRG-002', 'Örnek Program Beta', 'Tamamen sentetik program verisi Beta.', 'Aktif'),
('PRG-003', 'Örnek Program Gama', 'Tamamen sentetik program verisi Gama.', 'Aktif');

-- C. MÜŞTERİLER
INSERT INTO customers (customer_id, customer_name, customer_type, customer_status) VALUES
('CST-101', 'Test Müşterisi Alfa', 'Özel', 'Aktif'),
('CST-202', 'Test Müşterisi Beta', 'Özel', 'Aktif'),
('CST-303', 'Test Müşterisi Gama', 'İç Müşteri', 'Aktif');

-- D. PROJELER
INSERT INTO projects (
    project_id, project_code, project_name, program_id, customer_id, project_manager_user_id,
    start_date, baseline_finish_date, forecast_finish_date, actual_finish_date, project_status,
    manual_health, planned_progress, actual_progress, bac, currency, reporting_frequency,
    confidentiality, project_description, is_active, created_by_user_id, updated_by_user_id
) VALUES
('PRJ-001', 'PRJ-001', 'Örnek Proje Alfa', 'PRG-001', 'CST-101', 'USR-PM1', '2026-01-01', '2026-12-31', '2026-12-31', NULL, 'Aktif', 'Kırmızı', 50.00, 38.00, 250000.00, 'USD', 'Aylık', 'Şirket İçi', 'Alfa projesi için tamamen sentetik test açıklaması.', 1, 'USR-ADMIN', 'USR-ADMIN'),
('PRJ-002', 'PRJ-002', 'Örnek Proje Beta', 'PRG-002', 'CST-202', 'USR-PM2', '2026-03-01', '2026-09-30', '2026-10-15', NULL, 'Aktif', 'Sarı', 70.00, 62.00, 800000.00, 'TRY', 'Aylık', 'Şirket İçi', 'Beta projesi için tamamen sentetik test açıklaması.', 1, 'USR-ADMIN', 'USR-ADMIN'),
('PRJ-003', 'PRJ-003', 'Örnek Proje Gama', 'PRG-003', 'CST-303', 'USR-PM1', '2026-02-15', '2027-02-15', '2027-02-15', NULL, 'Aktif', 'Yeşil', 40.00, 42.00, 450000.00, 'EUR', 'Aylık', 'Hizmete Özel', 'Gama projesi için tamamen sentetik test açıklaması.', 1, 'USR-ADMIN', 'USR-ADMIN');

-- E. PROJE KULLANICILARI
INSERT OR IGNORE INTO project_users (project_user_id, project_id, user_id, assigned_by_user_id, assignment_status) VALUES
('PU-001', 'PRJ-001', 'USR-PM1', 'USR-ADMIN', 'Aktif'),
('PU-002', 'PRJ-001', 'USR-YONETIM', 'USR-ADMIN', 'Aktif'),
('PU-003', 'PRJ-002', 'USR-PM2', 'USR-ADMIN', 'Aktif'),
('PU-004', 'PRJ-003', 'USR-PM1', 'USR-ADMIN', 'Aktif');

-- F. PIR DURUM RAPORLARI
INSERT INTO pir_reports (
    pir_report_id, project_id, period, report_date, executive_summary, completed_work,
    delays, next_period_plan, management_expectations, manual_health, report_status,
    published_by_user_id, published_at, created_by_user_id, updated_by_user_id
) VALUES
('PIR-01', 'PRJ-001', '2026-06', '2026-06-30', 'Sentetik test ortamı hazırlığı nedeniyle plan sapması bulunmaktadır.', 'Alfa kapsamındaki örnek tasarım çalışmaları tamamlandı.', 'Test ortamı hazırlığında kontrollü bir gecikme kaydedildi.', 'Bir sonraki dönemde örnek entegrasyon kontrolleri yürütülecek.', 'Test kapsamındaki onay adımının tamamlanması beklenmektedir.', 'Kırmızı', 'Yayımlandı', 'USR-YONETIM', '2026-07-01 14:00:00', 'USR-PM1', 'USR-PM1'),
('PIR-02', 'PRJ-002', '2026-06', '2026-06-30', 'Sentetik Beta projesi genel olarak örnek takvime uygundur.', 'Örnek arayüz ve servis kontrolleri tamamlandı.', 'Bir test adımında kısa süreli sapma gözlemlendi.', 'Bir sonraki dönemde sentetik kabul senaryoları çalıştırılacak.', 'Ek bir test yönetim kararı beklenmemektedir.', 'Sarı', 'Yayımlandı', 'USR-YONETIM', '2026-07-01 15:30:00', 'USR-PM2', 'USR-PM2');

-- G. KİLOMETRE TAŞLARI
INSERT INTO milestones (
    milestone_id, project_id, milestone_name, planned_date, forecast_date, actual_date,
    milestone_status, critical, milestone_owner_user_id, acceptance_criteria, milestone_description,
    created_by_user_id, updated_by_user_id
) VALUES
('MS-101', 'PRJ-001', 'Örnek Tasarım Onayı', '2026-04-15', '2026-04-15', '2026-04-15', 'Tamamlandı', 1, 'USR-PM1', 'Sentetik onay kontrol listesinin tamamlanması.', 'Alfa projesi örnek tasarım adımı.', 'USR-ADMIN', 'USR-ADMIN'),
('MS-102', 'PRJ-001', 'Örnek Kabul Testi', '2026-08-01', '2026-08-15', NULL, 'Devam Ediyor', 1, 'USR-PM1', 'Sentetik test senaryolarının başarıyla tamamlanması.', 'Alfa projesi örnek doğrulama adımı.', 'USR-ADMIN', 'USR-ADMIN'),
('MS-201', 'PRJ-002', 'Örnek Veri Modeli Onayı', '2026-04-01', '2026-04-05', '2026-04-05', 'Tamamlandı', 0, 'USR-PM2', 'Sentetik veri modelinin doğrulanması.', 'Beta projesi örnek veri modeli adımı.', 'USR-ADMIN', 'USR-ADMIN');

-- H. RİSKLER
INSERT INTO risks (
    risk_id, project_id, risk_title, risk_category, risk_probability, risk_impact,
    risk_owner_user_id, risk_mitigation, risk_due_date, risk_status, opened_date,
    closed_date, created_by_user_id, updated_by_user_id
) VALUES
('RSK-001', 'PRJ-001', 'Test Ortamı Hazırlık Gecikmesi', 'Teknik', 4, 5, 'USR-PM1', 'Alternatif sentetik test ortamı hazırlanacak.', '2026-08-30', 'Açık', '2026-02-10', NULL, 'USR-PM1', 'USR-PM1'),
('RSK-002', 'PRJ-002', 'Örnek Kaynak Planı Sapması', 'Planlama', 3, 4, 'USR-PM2', 'Sentetik kaynak planı yeniden gözden geçirilecek.', '2026-09-01', 'İzleniyor', '2026-03-15', NULL, 'USR-PM2', 'USR-PM2');

-- I. SORUNLAR
INSERT INTO issues (
    issue_id, project_id, issue_title, issue_priority, issue_owner_user_id, issue_due_date,
    issue_status, issue_impact, root_cause, issue_resolution, opened_date, closed_date,
    created_by_user_id, updated_by_user_id
) VALUES
('ISS-001', 'PRJ-001', 'Test Ortamı Kapasite Sorunu', 'Kritik', 'USR-PM1', '2026-07-20', 'Açık', 'Kritik', 'Sentetik kapasite değeri beklenen aralığın dışındadır.', NULL, '2026-07-15', NULL, 'USR-PM1', 'USR-PM1'),
('ISS-002', 'PRJ-002', 'Örnek Arabirim Uyumsuzluğu', 'Orta', 'USR-PM2', '2026-05-30', 'Kapalı', 'Orta', 'Sentetik arabirim sürümleri farklı seçilmiştir.', 'Test sürümleri eşitlenerek doğrulama tamamlandı.', '2026-05-10', '2026-05-28', 'USR-PM2', 'USR-PM2');

-- J. AKSİYONLAR
INSERT INTO actions (
    action_id, project_id, action_description, source_type, source_reference,
    action_owner_user_id, action_due_date, action_status, action_progress,
    action_priority, completed_date, created_by_user_id, updated_by_user_id
) VALUES
('ACT-101', 'PRJ-001', 'Örnek test ortamı hazırlık adımlarının takip edilmesi.', 'PIR', 'PIR-01', 'USR-PM1', '2026-07-10', 'Devam Ediyor', 40.00, 'Yüksek', NULL, 'USR-PM1', 'USR-PM1'),
('ACT-201', 'PRJ-002', 'Sentetik arabirim uyumluluk kontrollerinin yapılması.', 'Sorun', 'ISS-002', 'USR-PM2', '2026-05-25', 'Tamamlandı', 100.00, 'Orta', '2026-05-24', 'USR-PM2', 'USR-PM2');

-- K. KAZANILMIŞ DEĞER ANALİZİ
INSERT INTO evm_records (
    evm_record_id, project_id, period, bac, pv, ev, ac, sv, cv, spi, cpi, eac, vac,
    created_by_user_id, updated_by_user_id
) VALUES
('EVM-001', 'PRJ-001', '2026-06', 250000.00, 125000.00, 95000.00, 110000.00, -30000.00, -15000.00, 0.7600, 0.8636, 289489.00, -39489.00, 'USR-ADMIN', 'USR-ADMIN'),
('EVM-002', 'PRJ-002', '2026-06', 800000.00, 560000.00, 496000.00, 510000.00, -64000.00, -14000.00, 0.8857, 0.9725, 822622.00, -22622.00, 'USR-ADMIN', 'USR-ADMIN');

-- L. YÖNETİM KARARLARI
INSERT INTO management_decisions (
    management_decision_id, project_id, decision_title, decision, decision_owner_user_id,
    decision_due_date, decision_status, decision_impact, if_delayed, recommendation,
    decision_date, created_by_user_id, updated_by_user_id
) VALUES
('DEC-001', 'PRJ-001', 'Ek Test Ortamının Hazırlanması', 'Sentetik doğrulamalar için ek test ortamı hazırlanmasına karar verildi.', 'USR-PM1', '2026-07-22', 'Uygulanıyor', 'Yüksek', 'Doğrulama takvimi örnek olarak iki hafta sapabilir.', 'İkinci bir sentetik test ortamı etkinleştirilmelidir.', '2026-07-16', 'USR-YONETIM', 'USR-YONETIM');

-- M. DENETİM LOGLARI
INSERT INTO audit_logs (audit_log_id, user_id, entity_name, entity_id, action_type, old_values, new_values, changed_at, ip_address) VALUES
('LOG-001', 'USR-ADMIN', 'users', 'USR-PM1', 'INSERT', NULL, '{"user_id":"USR-PM1","email":"fixture.pm.alfa@example.test","full_name":"Test Proje Sorumlusu Alfa"}', '2026-07-17 10:00:00', '127.0.0.1');
