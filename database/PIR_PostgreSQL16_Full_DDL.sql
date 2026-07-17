PRAGMA foreign_keys = ON;

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
    program_name TEXT NOT NULL,
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
    customer_name TEXT NOT NULL,
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
    confidentiality TEXT DEFAULT 'Normal' NOT NULL,
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
    CONSTRAINT ck_projects_project_status CHECK (project_status IN ('Taslak','Devam Ediyor','Beklemede','Tamamlandı','Pasif')),
    CONSTRAINT ck_projects_manual_health CHECK (manual_health IN ('Yeşil','Sarı','Kırmızı','Gri')),
    CONSTRAINT ck_projects_planned_progress CHECK (planned_progress BETWEEN 0 AND 100),
    CONSTRAINT ck_projects_actual_progress CHECK (actual_progress BETWEEN 0 AND 100),
    CONSTRAINT ck_projects_bac CHECK (bac >= 0),
    CONSTRAINT ck_projects_currency CHECK (currency IN ('TRY','USD','EUR')),
    CONSTRAINT ck_projects_reporting_frequency CHECK (reporting_frequency IN ('Haftalık','Aylık','Üç Aylık')),
    CONSTRAINT ck_projects_confidentiality CHECK (confidentiality IN ('Genel','Normal','Özel','Gizli')),
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
    CONSTRAINT ck_milestones_date_order CHECK (forecast_date >= planned_date OR actual_date IS NOT NULL),
    CONSTRAINT ck_milestones_actual_date CHECK (actual_date IS NULL OR actual_date >= planned_date)
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
CREATE INDEX idx_actions_project_status_due ON actions (project_id, action_status, action_due_date);
CREATE INDEX idx_actions_owner_due ON actions (action_owner_user_id, action_due_date) WHERE action_status <> 'Tamamlandı';
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
        WHEN r.risk_score BETWEEN 1 AND 5 THEN 'Yeşil'
        WHEN r.risk_score BETWEEN 6 AND 15 THEN 'Sarı'
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
    CASE WHEN e.pv = 0 THEN NULL ELSE ROUND(e.ev / e.pv, 4) END AS spi,
    CASE WHEN e.ac = 0 THEN NULL ELSE ROUND(e.ev / e.ac, 4) END AS cpi,
    CASE WHEN e.ac = 0 OR e.ev = 0 THEN NULL ELSE ROUND(e.bac / (e.ev / e.ac), 2) END AS eac,
    CASE WHEN e.ac = 0 OR e.ev = 0 THEN NULL ELSE ROUND(e.bac - (e.bac / (e.ev / e.ac)), 2) END AS vac
FROM evm_records e
JOIN projects p ON p.project_id = e.project_id;


INSERT INTO users (user_id, email, password_hash, full_name, user_role) 
VALUES ('U-001', 'test@sirket.com', 'hash_sifre_buraya', 'Ahmet Yılmaz', 'Sistem Yöneticisi');

SELECT * FROM users;