-- Existing SQLite databases için tek seferlik Sorun -> Aksiyon ilişki geçişi.
PRAGMA foreign_keys = ON;

BEGIN IMMEDIATE;

ALTER TABLE actions
ADD COLUMN issue_id TEXT
REFERENCES issues (issue_id)
ON DELETE SET NULL
ON UPDATE CASCADE;

CREATE INDEX idx_actions_issue_id ON actions (issue_id);

-- Eski aksiyonlarda geçerli bir Sorun kaynak referansı varsa yeni FK alanını doldur.
UPDATE actions
SET issue_id = source_reference
WHERE source_type = 'Sorun'
  AND EXISTS (
      SELECT 1
      FROM issues
      WHERE issues.issue_id = actions.source_reference
        AND issues.project_id = actions.project_id
  );

COMMIT;
