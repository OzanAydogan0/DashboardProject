-- Existing SQLite databases için tek seferlik Risk -> Sorun/Aksiyon ilişki geçişi.
PRAGMA foreign_keys = ON;

BEGIN IMMEDIATE;

ALTER TABLE issues
ADD COLUMN risk_id TEXT
REFERENCES risks (risk_id)
ON DELETE SET NULL
ON UPDATE CASCADE;

ALTER TABLE actions
ADD COLUMN risk_id TEXT
REFERENCES risks (risk_id)
ON DELETE SET NULL
ON UPDATE CASCADE;

CREATE INDEX idx_issues_risk_id ON issues (risk_id);
CREATE INDEX idx_actions_risk_id ON actions (risk_id);

-- Eski aksiyonlarda geçerli bir Risk kaynak referansı varsa yeni FK alanını doldur.
UPDATE actions
SET risk_id = source_reference
WHERE source_type = 'Risk'
  AND EXISTS (
      SELECT 1
      FROM risks
      WHERE risks.risk_id = actions.source_reference
        AND risks.project_id = actions.project_id
  );

COMMIT;
