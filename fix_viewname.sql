-- Fix ViewName column issue for Chart of Accounts
-- Run this SQL script manually to fix the immediate error

-- Add ViewName column to Grids table if it doesn't exist
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'Admin' 
        AND table_name = 'Grids' 
        AND column_name = 'ViewName'
    ) THEN
        ALTER TABLE "Admin"."Grids" ADD COLUMN "ViewName" character varying(250);
    END IF;
END $$;

-- Update existing ChartOfAccounts grid to set ViewName
UPDATE "Admin"."Grids" 
SET "ViewName" = 'ChartOfAccounts'
WHERE "Name" = 'ChartOfAccounts' 
AND ("ViewName" IS NULL OR "ViewName" = '');

-- Create the grid configuration if it doesn't exist
INSERT INTO "Admin"."Grids" (
    "Id", "MenuIdRef", "Name", "ViewName", 
    "PageSize", "Sortable", "Filterable", "Groupable", "Editable",
    "AutoBind", "CreatedByIdRef", "CreatedDate", "IsDeleted"
)
SELECT 
    gen_random_uuid(),
    (SELECT "Id" FROM "Admin"."Menus" WHERE "Page" = '/Accounting/chart-of-accounts' LIMIT 1),
    'ChartOfAccounts',
    'ChartOfAccounts',
    25,
    true,
    true,
    false,
    true,
    true,
    '00000000-0000-0000-0000-000000000001'::uuid,
    NOW(),
    false
WHERE NOT EXISTS (SELECT 1 FROM "Admin"."Grids" WHERE "Name" = 'ChartOfAccounts');
