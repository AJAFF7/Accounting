-- Seed Data: Chart of Accounts
-- Date: 2026-03-14
-- Description: Populates standard Chart of Accounts

-- Insert standard Chart of Accounts if they don't exist
INSERT INTO "Accounting"."ChartOfAccounts" ("Id", "Name", "Type", "Code", "Description", "IsActive", "CreatedByIdRef", "CreatedDate", "IsDeleted")
SELECT * FROM (VALUES
    -- Assets (1000-1999)
    (gen_random_uuid(), 'Cash', 'Asset', '1000', 'Cash on hand and in bank', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Petty Cash', 'Asset', '1010', 'Small cash fund for minor expenses', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Accounts Receivable', 'Asset', '1200', 'Money owed by customers', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Inventory', 'Asset', '1300', 'Goods available for sale', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Prepaid Expenses', 'Asset', '1400', 'Expenses paid in advance', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Office Equipment', 'Asset', '1500', 'Office furniture and equipment', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Accumulated Depreciation - Equipment', 'Asset', '1550', 'Accumulated depreciation on equipment', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Vehicles', 'Asset', '1600', 'Company vehicles', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Buildings', 'Asset', '1700', 'Real estate and buildings', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    
    -- Liabilities (2000-2999)
    (gen_random_uuid(), 'Accounts Payable', 'Liability', '2000', 'Money owed to suppliers', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Salaries Payable', 'Liability', '2100', 'Unpaid employee salaries', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Taxes Payable', 'Liability', '2200', 'Taxes owed to government', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Short-term Loans', 'Liability', '2300', 'Loans due within one year', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Long-term Loans', 'Liability', '2500', 'Loans due after one year', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Credit Card Payable', 'Liability', '2400', 'Credit card balances', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    
    -- Equity (3000-3999)
    (gen_random_uuid(), 'Owner''s Capital', 'Equity', '3000', 'Owner''s investment in business', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Owner''s Drawings', 'Equity', '3100', 'Owner''s withdrawals from business', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Retained Earnings', 'Equity', '3200', 'Accumulated profits', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Common Stock', 'Equity', '3300', 'Shares issued to shareholders', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    
    -- Revenue (4000-4999)
    (gen_random_uuid(), 'Sales Revenue', 'Revenue', '4000', 'Income from sales', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Service Revenue', 'Revenue', '4100', 'Income from services', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Interest Income', 'Revenue', '4200', 'Income from interest', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Other Income', 'Revenue', '4900', 'Miscellaneous income', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    
    -- Expenses (5000-5999)
    (gen_random_uuid(), 'Cost of Goods Sold', 'Expense', '5000', 'Direct costs of products sold', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Salaries Expense', 'Expense', '5100', 'Employee salaries and wages', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Rent Expense', 'Expense', '5200', 'Office or facility rent', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Utilities Expense', 'Expense', '5300', 'Electricity, water, internet', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Office Supplies', 'Expense', '5400', 'Stationery and office materials', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Telephone Expense', 'Expense', '5500', 'Phone and communication costs', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Insurance Expense', 'Expense', '5600', 'Insurance premiums', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Depreciation Expense', 'Expense', '5700', 'Asset depreciation', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Advertising Expense', 'Expense', '5800', 'Marketing and advertising costs', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Travel Expense', 'Expense', '5900', 'Business travel costs', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Maintenance Expense', 'Expense', '6000', 'Repairs and maintenance', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Professional Fees', 'Expense', '6100', 'Legal and consulting fees', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Bank Charges', 'Expense', '6200', 'Banking fees and charges', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Interest Expense', 'Expense', '6300', 'Interest on loans', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false),
    (gen_random_uuid(), 'Miscellaneous Expense', 'Expense', '6900', 'Other operating expenses', true, '00000000-0000-0000-0000-000000000001'::uuid, NOW(), false)
) AS new_accounts ("Id", "Name", "Type", "Code", "Description", "IsActive", "CreatedByIdRef", "CreatedDate", "IsDeleted")
WHERE NOT EXISTS (
    SELECT 1 FROM "Accounting"."ChartOfAccounts" WHERE "Code" = new_accounts."Code"
);
