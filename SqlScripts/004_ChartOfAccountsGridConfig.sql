-- Grid Configuration: Chart of Accounts
-- This script sets up the grid configuration for Chart of Accounts in the Admin schema

-- Create Menu for Chart of Accounts if it doesn't exist (without module reference for now)
INSERT INTO "Admin"."Menus" (
    "Id", "DisplayName", "Page", "Icon", 
    "Sort", "ShowInHomePage", "CreatedByIdRef", "CreatedDate", "IsDeleted"
)
SELECT 
    gen_random_uuid(),
    'Chart of Accounts',
    '/Accounting/chart-of-accounts',
    'mdi-account-balance',
    1,
    true,
    '00000000-0000-0000-0000-000000000001'::uuid,
    NOW(),
    false
WHERE NOT EXISTS (SELECT 1 FROM "Admin"."Menus" WHERE "Page" = '/Accounting/chart-of-accounts');

-- Create Grid definition if it doesn't exist
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

-- Update existing grid to set ViewName if it's missing
UPDATE "Admin"."Grids" 
SET "ViewName" = 'ChartOfAccounts'
WHERE "Name" = 'ChartOfAccounts' AND ("ViewName" IS NULL OR "ViewName" = '');

-- Insert Column definitions if they don't exist
INSERT INTO "Admin"."Columns" (
    "Id", "GridIdRef", "DbName", "Title", "DataType", "ColumnType",
    "Width", "Sortable", "Filterable", "Editable", "Visible", "Sort",
    "CreatedByIdRef", "CreatedDate", "IsDeleted"
)
SELECT * FROM (VALUES
    -- Code Column
    (gen_random_uuid()::uuid, (SELECT "Id" FROM "Admin"."Grids" WHERE "Name" = 'ChartOfAccounts' LIMIT 1), 'Code', 'Code', 'String', 'String', '150px', true, true, true, true, 1, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    -- Name Column
    (gen_random_uuid()::uuid, (SELECT "Id" FROM "Admin"."Grids" WHERE "Name" = 'ChartOfAccounts' LIMIT 1), 'Name', 'Name', 'String', 'String', '250px', true, true, true, true, 2, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    -- Type Column with Template
    (gen_random_uuid()::uuid, (SELECT "Id" FROM "Admin"."Grids" WHERE "Name" = 'ChartOfAccounts' LIMIT 1), 'Type', 'Type', 'String', 'Template', '150px', true, true, true, true, 3, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    -- Description Column
    (gen_random_uuid()::uuid, (SELECT "Id" FROM "Admin"."Grids" WHERE "Name" = 'ChartOfAccounts' LIMIT 1), 'Description', 'Description', 'String', 'String', '300px', true, true, true, true, 4, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    -- IsActive Column (Status) with Template
    (gen_random_uuid()::uuid, (SELECT "Id" FROM "Admin"."Grids" WHERE "Name" = 'ChartOfAccounts' LIMIT 1), 'IsActive', 'Status', 'Boolean', 'Template', '120px', true, true, true, true, 5, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false)
) AS cols ("Id", "GridIdRef", "DbName", "Title", "DataType", "ColumnType", "Width", "Sortable", "Filterable", "Editable", "Visible", "Sort", "CreatedByIdRef", "CreatedDate", "IsDeleted")
WHERE NOT EXISTS (
    SELECT 1 FROM "Admin"."Columns" 
    WHERE "GridIdRef" = (SELECT "Id" FROM "Admin"."Grids" WHERE "Name" = 'ChartOfAccounts' LIMIT 1)
    AND "DbName" = cols."DbName"
);
