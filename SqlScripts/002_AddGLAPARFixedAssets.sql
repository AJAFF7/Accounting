-- Migration: Add GL, AP, AR, and Fixed Assets Tables
-- Date: 2026-03-14
-- Description: Creates GeneralLedger, AccountsPayable, AccountsReceivable, and FixedAssets tables

-- Create GeneralLedger table
CREATE TABLE IF NOT EXISTS "Accounting"."GeneralLedger" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "TransactionDate" timestamp with time zone NOT NULL,
    "AccountIdRef" uuid NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "Debit" decimal(18,2) NOT NULL DEFAULT 0,
    "Credit" decimal(18,2) NOT NULL DEFAULT 0,
    "Balance" decimal(18,2) NOT NULL DEFAULT 0,
    "ReferenceNumber" character varying(100),
    "JournalEntryIdRef" uuid,
    "TransactionIdRef" uuid,
    "FiscalYear" integer NOT NULL,
    "FiscalPeriod" integer NOT NULL,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_GeneralLedger" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_GeneralLedger_ChartOfAccounts" 
        FOREIGN KEY ("AccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE RESTRICT,
    CONSTRAINT "FK_GeneralLedger_JournalEntries" 
        FOREIGN KEY ("JournalEntryIdRef") 
        REFERENCES "Accounting"."JournalEntries" ("Id") 
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_GeneralLedger_TransactionDate" ON "Accounting"."GeneralLedger" ("TransactionDate");
CREATE INDEX IF NOT EXISTS "IX_GeneralLedger_AccountIdRef" ON "Accounting"."GeneralLedger" ("AccountIdRef");
CREATE INDEX IF NOT EXISTS "IX_GeneralLedger_ReferenceNumber" ON "Accounting"."GeneralLedger" ("ReferenceNumber");
CREATE INDEX IF NOT EXISTS "IX_GeneralLedger_JournalEntryIdRef" ON "Accounting"."GeneralLedger" ("JournalEntryIdRef");
CREATE INDEX IF NOT EXISTS "IX_GeneralLedger_TransactionIdRef" ON "Accounting"."GeneralLedger" ("TransactionIdRef");
CREATE INDEX IF NOT EXISTS "IX_GeneralLedger_FiscalYear" ON "Accounting"."GeneralLedger" ("FiscalYear");
CREATE INDEX IF NOT EXISTS "IX_GeneralLedger_FiscalPeriod" ON "Accounting"."GeneralLedger" ("FiscalPeriod");

-- Create AccountsPayable table
CREATE TABLE IF NOT EXISTS "Accounting"."AccountsPayable" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "VendorName" character varying(250) NOT NULL,
    "VendorCode" character varying(100),
    "InvoiceNumber" character varying(100) NOT NULL,
    "InvoiceDate" timestamp with time zone NOT NULL,
    "DueDate" timestamp with time zone NOT NULL,
    "TotalAmount" decimal(18,2) NOT NULL,
    "AmountPaid" decimal(18,2) NOT NULL DEFAULT 0,
    "AmountDue" decimal(18,2) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "PaymentTerms" character varying(100),
    "Description" character varying(1000),
    "PurchaseOrderNumber" character varying(100),
    "PaymentDate" timestamp with time zone,
    "PaymentReference" character varying(100),
    "ExpenseAccountIdRef" uuid,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_AccountsPayable" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountsPayable_ChartOfAccounts" 
        FOREIGN KEY ("ExpenseAccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_AccountsPayable_VendorName" ON "Accounting"."AccountsPayable" ("VendorName");
CREATE INDEX IF NOT EXISTS "IX_AccountsPayable_InvoiceNumber" ON "Accounting"."AccountsPayable" ("InvoiceNumber");
CREATE INDEX IF NOT EXISTS "IX_AccountsPayable_InvoiceDate" ON "Accounting"."AccountsPayable" ("InvoiceDate");
CREATE INDEX IF NOT EXISTS "IX_AccountsPayable_DueDate" ON "Accounting"."AccountsPayable" ("DueDate");
CREATE INDEX IF NOT EXISTS "IX_AccountsPayable_Status" ON "Accounting"."AccountsPayable" ("Status");

-- Create AccountsPayablePayments table
CREATE TABLE IF NOT EXISTS "Accounting"."AccountsPayablePayments" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "AccountsPayableIdRef" uuid NOT NULL,
    "PaymentDate" timestamp with time zone NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "PaymentMethod" character varying(100),
    "ReferenceNumber" character varying(100),
    "Notes" character varying(500),
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_AccountsPayablePayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountsPayablePayments_AccountsPayable" 
        FOREIGN KEY ("AccountsPayableIdRef") 
        REFERENCES "Accounting"."AccountsPayable" ("Id") 
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AccountsPayablePayments_AccountsPayableIdRef" ON "Accounting"."AccountsPayablePayments" ("AccountsPayableIdRef");

-- Create AccountsReceivable table
CREATE TABLE IF NOT EXISTS "Accounting"."AccountsReceivable" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "CustomerName" character varying(250) NOT NULL,
    "CustomerCode" character varying(100),
    "InvoiceNumber" character varying(100) NOT NULL,
    "InvoiceDate" timestamp with time zone NOT NULL,
    "DueDate" timestamp with time zone NOT NULL,
    "TotalAmount" decimal(18,2) NOT NULL,
    "AmountReceived" decimal(18,2) NOT NULL DEFAULT 0,
    "AmountDue" decimal(18,2) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "PaymentTerms" character varying(100),
    "Description" character varying(1000),
    "SalesOrderNumber" character varying(100),
    "PaymentDate" timestamp with time zone,
    "PaymentReference" character varying(100),
    "RevenueAccountIdRef" uuid,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_AccountsReceivable" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountsReceivable_ChartOfAccounts" 
        FOREIGN KEY ("RevenueAccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_AccountsReceivable_CustomerName" ON "Accounting"."AccountsReceivable" ("CustomerName");
CREATE INDEX IF NOT EXISTS "IX_AccountsReceivable_InvoiceNumber" ON "Accounting"."AccountsReceivable" ("InvoiceNumber");
CREATE INDEX IF NOT EXISTS "IX_AccountsReceivable_InvoiceDate" ON "Accounting"."AccountsReceivable" ("InvoiceDate");
CREATE INDEX IF NOT EXISTS "IX_AccountsReceivable_DueDate" ON "Accounting"."AccountsReceivable" ("DueDate");
CREATE INDEX IF NOT EXISTS "IX_AccountsReceivable_Status" ON "Accounting"."AccountsReceivable" ("Status");

-- Create AccountsReceivablePayments table
CREATE TABLE IF NOT EXISTS "Accounting"."AccountsReceivablePayments" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "AccountsReceivableIdRef" uuid NOT NULL,
    "PaymentDate" timestamp with time zone NOT NULL,
    "Amount" decimal(18,2) NOT NULL,
    "PaymentMethod" character varying(100),
    "ReferenceNumber" character varying(100),
    "Notes" character varying(500),
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_AccountsReceivablePayments" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AccountsReceivablePayments_AccountsReceivable" 
        FOREIGN KEY ("AccountsReceivableIdRef") 
        REFERENCES "Accounting"."AccountsReceivable" ("Id") 
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_AccountsReceivablePayments_AccountsReceivableIdRef" ON "Accounting"."AccountsReceivablePayments" ("AccountsReceivableIdRef");

-- Create FixedAssets table
CREATE TABLE IF NOT EXISTS "Accounting"."FixedAssets" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "AssetName" character varying(250) NOT NULL,
    "AssetNumber" character varying(100) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "Description" character varying(1000),
    "PurchaseDate" timestamp with time zone NOT NULL,
    "PurchaseCost" decimal(18,2) NOT NULL,
    "SalvageValue" decimal(18,2) NOT NULL DEFAULT 0,
    "UsefulLifeYears" integer NOT NULL,
    "DepreciationMethod" character varying(50) NOT NULL,
    "AccumulatedDepreciation" decimal(18,2) NOT NULL DEFAULT 0,
    "CurrentValue" decimal(18,2) NOT NULL,
    "Location" character varying(250),
    "SerialNumber" character varying(100),
    "Manufacturer" character varying(250),
    "Model" character varying(100),
    "Status" character varying(50) NOT NULL DEFAULT 'Active',
    "DisposalDate" timestamp with time zone,
    "DisposalAmount" decimal(18,2),
    "DisposalNotes" character varying(500),
    "AssetAccountIdRef" uuid,
    "DepreciationAccountIdRef" uuid,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_FixedAssets" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FixedAssets_AssetAccount" 
        FOREIGN KEY ("AssetAccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE SET NULL,
    CONSTRAINT "FK_FixedAssets_DepreciationAccount" 
        FOREIGN KEY ("DepreciationAccountIdRef") 
        REFERENCES "Accounting"."ChartOfAccounts" ("Id") 
        ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_FixedAssets_AssetName" ON "Accounting"."FixedAssets" ("AssetName") WHERE "IsDeleted" = FALSE;
CREATE INDEX IF NOT EXISTS "IX_FixedAssets_AssetNumber" ON "Accounting"."FixedAssets" ("AssetNumber");
CREATE INDEX IF NOT EXISTS "IX_FixedAssets_Category" ON "Accounting"."FixedAssets" ("Category");
CREATE INDEX IF NOT EXISTS "IX_FixedAssets_PurchaseDate" ON "Accounting"."FixedAssets" ("PurchaseDate");
CREATE INDEX IF NOT EXISTS "IX_FixedAssets_Status" ON "Accounting"."FixedAssets" ("Status");

-- Create FixedAssetDepreciation table
CREATE TABLE IF NOT EXISTS "Accounting"."FixedAssetDepreciation" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "FixedAssetIdRef" uuid NOT NULL,
    "DepreciationDate" timestamp with time zone NOT NULL,
    "FiscalYear" integer NOT NULL,
    "FiscalPeriod" integer NOT NULL,
    "DepreciationAmount" decimal(18,2) NOT NULL,
    "AccumulatedDepreciation" decimal(18,2) NOT NULL,
    "BookValue" decimal(18,2) NOT NULL,
    "Notes" character varying(500),
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL DEFAULT false,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_FixedAssetDepreciation" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_FixedAssetDepreciation_FixedAssets" 
        FOREIGN KEY ("FixedAssetIdRef") 
        REFERENCES "Accounting"."FixedAssets" ("Id") 
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_FixedAssetDepreciation_FixedAssetIdRef" ON "Accounting"."FixedAssetDepreciation" ("FixedAssetIdRef");
CREATE INDEX IF NOT EXISTS "IX_FixedAssetDepreciation_DepreciationDate" ON "Accounting"."FixedAssetDepreciation" ("DepreciationDate");
