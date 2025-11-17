-- ============================================================
-- 008_FixTradingBotRuntimeSnapshotVersion.sql
-- Purpose: Fix the Version column type in TradingBotRuntimeSnapshots
--          to match the C# model (DateTime? instead of byte[])
--          The column should be datetime(6) for MySQL concurrency control
-- ============================================================

-- Ensure the Version column is datetime(6) with proper defaults
-- This migration is safe to run multiple times (idempotent)
ALTER TABLE `TradingBotRuntimeSnapshots`
    MODIFY COLUMN `Version` datetime(6) NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6);

