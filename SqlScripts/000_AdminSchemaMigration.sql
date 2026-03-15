-- Complete Admin schema migration to bypass SoftMax.Core bug
-- This script creates all tables from migration 20240903115945_M-001

-- Create Admin schema
CREATE SCHEMA IF NOT EXISTS "Admin";

-- Create BackgroundJobs table
CREATE TABLE IF NOT EXISTS "Admin"."BackgroundJobs" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "Name" character varying(150) NOT NULL,
    "Active" boolean NOT NULL,
    "Query" text NOT NULL,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    "Schedules" jsonb,
    CONSTRAINT "PK_BackgroundJobs" PRIMARY KEY ("Id")
);

-- Drop EmailServers if it exists (in case of partial migration)
DROP TABLE IF EXISTS "Admin"."EmailServers";

-- Create EmailServers table
CREATE TABLE "Admin"."EmailServers" (
    "Id" uuid NOT NULL DEFAULT (gen_random_uuid()),
    "EmailAddress" character varying(100) NOT NULL,
    "DisplayName" character varying(250) NOT NULL,
    "UserName" character varying(250) NOT NULL,
    "Password" character varying(250) NOT NULL,
    "Smtp" character varying(250) NOT NULL,
    "Port" integer NOT NULL,
    "Active" boolean NOT NULL,
    "CreatedByIdRef" uuid NOT NULL,
    "ModifiedByIdRef" uuid,
    "CreatedDate" timestamp with time zone NOT NULL,
    "ModifiedDate" timestamp with time zone,
    "IsDeleted" boolean NOT NULL,
    "DeletedByIdRef" uuid,
    "DeleteDate" timestamp with time zone,
    CONSTRAINT "PK_EmailServers" PRIMARY KEY ("Id")
);

-- Create __MigrationsHistory table to mark migration as complete
CREATE TABLE IF NOT EXISTS "Admin"."__MigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___MigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Insert migration record to skip buggy migration
INSERT INTO "Admin"."__MigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20240903115945_M-001', '10.0.5')
ON CONFLICT ("MigrationId") DO NOTHING;
