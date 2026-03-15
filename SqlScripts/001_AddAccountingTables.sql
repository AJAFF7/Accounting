-- Migration: Add Accounting Tables
-- Date: 2026-03-14
-- Description: Creates ChartOfAccounts, JournalEntries, and Transactions tables

-- Create Accounting schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS "Accounting";

-- Create ChartOfAccounts table
CREATE TABLE IF NOT EXISTS "Accounting"."ChartOfAccounts" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "Name" character varying(250) NOT NULL,
    "Type" character varying(50) NOT NULL,
    "Code" character varying(50),
    "Description" character varying(1000),
    "IsActive" boolean NOT NULL DEFAULT true,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_ChartOfAccounts" PRIMARY KEY ("Id")
);

-- Create indexes for ChartOfAccounts
CREATE INDEX IF NOT EXISTS "IX_ChartOfAccounts_Name" ON "Accounting"."ChartOfAccounts" ("Name") WHERE "IsDeleted" = FALSE;
CREATE INDEX IF NOT EXISTS "IX_ChartOfAccounts_Type" ON "Accounting"."ChartOfAccounts" ("Type");
CREATE INDEX IF NOT EXISTS "IX_ChartOfAccounts_Code" ON "Accounting"."ChartOfAccounts" ("Code");

-- Create JournalEntries table
CREATE TABLE IF NOT EXISTS "Accounting"."JournalEntries" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "Date" timestamp with time zone NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "DebitAccountIdRef" uuid NOT NULL,
    "CreditAccountIdRef" uuid NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "ReferenceNumber" character varying(100),
    "IsPosted" boolean NOT NULL DEFAULT false,
    "PostedDate" timestamp with time zone,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_JournalEntries" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_JournalEntries_ChartOfAccounts_Debit" 
        FOREIGN KEY ("DebitAccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE RESTRICT,
    CONSTRAINT "FK_JournalEntries_ChartOfAccounts_Credit" 
        FOREIGN KEY ("CreditAccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE RESTRICT
);

-- Create indexes for JournalEntries
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_Date" ON "Accounting"."JournalEntries" ("Date");
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_DebitAccountIdRef" ON "Accounting"."JournalEntries" ("DebitAccountIdRef");
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_CreditAccountIdRef" ON "Accounting"."JournalEntries" ("CreditAccountIdRef");
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_ReferenceNumber" ON "Accounting"."JournalEntries" ("ReferenceNumber");
CREATE INDEX IF NOT EXISTS "IX_JournalEntries_IsPosted" ON "Accounting"."JournalEntries" ("IsPosted");

-- Create Transactions table
CREATE TABLE IF NOT EXISTS "Accounting"."Transactions" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "JournalEntryIdRef" uuid NOT NULL,
    "AccountIdRef" uuid NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "Balance" decimal(18,2) NOT NULL DEFAULT 0,
    "Type" character varying(50) NOT NULL,
    "TransactionDate" timestamp with time zone NOT NULL,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_Transactions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Transactions_JournalEntries" 
        FOREIGN KEY ("JournalEntryIdRef") 
        REFERENCES "Accounting"."JournalEntries" ("Id") 
        ON DELETE CASCADE,
    CONSTRAINT "FK_Transactions_ChartOfAccounts" 
        FOREIGN KEY ("AccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE RESTRICT
);

-- Create indexes for Transactions
CREATE INDEX IF NOT EXISTS "IX_Transactions_JournalEntryIdRef" ON "Accounting"."Transactions" ("JournalEntryIdRef");
CREATE INDEX IF NOT EXISTS "IX_Transactions_AccountIdRef" ON "Accounting"."Transactions" ("AccountIdRef");
CREATE INDEX IF NOT EXISTS "IX_Transactions_Type" ON "Accounting"."Transactions" ("Type");
CREATE INDEX IF NOT EXISTS "IX_Transactions_TransactionDate" ON "Accounting"."Transactions" ("TransactionDate");

-- Insert sample Chart of Accounts
INSERT INTO "Accounting"."ChartOfAccounts" ("Id", "Name", "Type", "Code", "IsActive", "CreatedByIdRef", "CreatedDate", "IsDeleted")
VALUES 
    (gen_random_uuid(), 'Cash', 'Asset', '1001', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Accounts Receivable', 'Asset', '1002', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Inventory', 'Asset', '1003', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Accounts Payable', 'Liability', '2001', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Owner Equity', 'Equity', '3001', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Sales Revenue', 'Revenue', '4001', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Cost of Goods Sold', 'Expense', '5001', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Rent Expense', 'Expense', '5002', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false),
    (gen_random_uuid(), 'Utilities Expense', 'Expense', '5003', true, '00000000-0000-0000-0000-000000000000', CURRENT_TIMESTAMP, false)
ON CONFLICT DO NOTHING;
