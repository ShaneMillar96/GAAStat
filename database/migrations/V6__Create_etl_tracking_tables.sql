-- GAA Statistics Database - ETL Tracking Tables Migration
-- Creates ETL job tracking, progress monitoring, and error reporting tables
-- Purpose: Support Excel file ETL processing with comprehensive tracking and error handling
-- Author: Database Engineer
-- Date: 2025-09-02

-- ETL Jobs - Main job tracking table
CREATE TABLE etl_jobs (
    job_id SERIAL PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    status VARCHAR(50) NOT NULL, -- pending, processing, completed, failed
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    error_summary TEXT,
    created_by VARCHAR(100),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ETL Job Progress - Stage-by-stage progress tracking
CREATE TABLE etl_job_progress (
    progress_id SERIAL PRIMARY KEY,
    job_id INTEGER NOT NULL,
    stage VARCHAR(100) NOT NULL,
    total_steps INTEGER,
    completed_steps INTEGER,
    status VARCHAR(50),
    message TEXT,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (job_id) REFERENCES etl_jobs(job_id) ON DELETE CASCADE
);

-- ETL Validation Errors - Detailed error tracking for data validation issues
CREATE TABLE etl_validation_errors (
    error_id SERIAL PRIMARY KEY,
    job_id INTEGER NOT NULL,
    sheet_name VARCHAR(100),
    row_number INTEGER,
    column_name VARCHAR(100),
    error_type VARCHAR(50),
    error_message TEXT,
    suggested_fix TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (job_id) REFERENCES etl_jobs(job_id) ON DELETE CASCADE
);

-- Indexes for performance optimization
CREATE INDEX idx_etl_jobs_status ON etl_jobs(status);
CREATE INDEX idx_etl_jobs_created_at ON etl_jobs(created_at);
CREATE INDEX idx_etl_jobs_created_by ON etl_jobs(created_by);
CREATE INDEX idx_etl_job_progress_job_id ON etl_job_progress(job_id);
CREATE INDEX idx_etl_job_progress_updated_at ON etl_job_progress(updated_at);
CREATE INDEX idx_etl_validation_errors_job_id ON etl_validation_errors(job_id);
CREATE INDEX idx_etl_validation_errors_error_type ON etl_validation_errors(error_type);

-- Check constraints for data integrity
ALTER TABLE etl_jobs ADD CONSTRAINT chk_etl_jobs_status 
    CHECK (status IN ('pending', 'processing', 'completed', 'failed'));
ALTER TABLE etl_jobs ADD CONSTRAINT chk_etl_jobs_file_size 
    CHECK (file_size_bytes > 0);
ALTER TABLE etl_jobs ADD CONSTRAINT chk_etl_jobs_completion_dates 
    CHECK (completed_at IS NULL OR started_at IS NULL OR completed_at >= started_at);

ALTER TABLE etl_job_progress ADD CONSTRAINT chk_etl_progress_steps 
    CHECK (total_steps IS NULL OR completed_steps IS NULL OR completed_steps <= total_steps);
ALTER TABLE etl_job_progress ADD CONSTRAINT chk_etl_progress_completed_steps_positive 
    CHECK (completed_steps IS NULL OR completed_steps >= 0);

ALTER TABLE etl_validation_errors ADD CONSTRAINT chk_etl_errors_row_number_positive 
    CHECK (row_number IS NULL OR row_number > 0);

-- Table comments for documentation
COMMENT ON TABLE etl_jobs IS 'Master table tracking Excel ETL job execution status and metadata';
COMMENT ON TABLE etl_job_progress IS 'Stage-by-stage progress tracking for ETL jobs with detailed status updates';
COMMENT ON TABLE etl_validation_errors IS 'Detailed error tracking for data validation issues during ETL processing';

-- Column comments for clarity
COMMENT ON COLUMN etl_jobs.job_id IS 'Unique identifier for ETL job';
COMMENT ON COLUMN etl_jobs.file_name IS 'Original name of uploaded Excel file';
COMMENT ON COLUMN etl_jobs.file_size_bytes IS 'Size of uploaded file in bytes';
COMMENT ON COLUMN etl_jobs.status IS 'Current job status: pending, processing, completed, failed';
COMMENT ON COLUMN etl_jobs.started_at IS 'Timestamp when job processing began';
COMMENT ON COLUMN etl_jobs.completed_at IS 'Timestamp when job finished (success or failure)';
COMMENT ON COLUMN etl_jobs.error_summary IS 'High-level summary of errors if job failed';
COMMENT ON COLUMN etl_jobs.created_by IS 'User or system that initiated the ETL job';

COMMENT ON COLUMN etl_job_progress.progress_id IS 'Unique identifier for progress entry';
COMMENT ON COLUMN etl_job_progress.job_id IS 'Reference to parent ETL job';
COMMENT ON COLUMN etl_job_progress.stage IS 'Current processing stage (e.g., "Parsing Excel", "Validating Data")';
COMMENT ON COLUMN etl_job_progress.total_steps IS 'Total number of steps for this stage';
COMMENT ON COLUMN etl_job_progress.completed_steps IS 'Number of steps completed in this stage';
COMMENT ON COLUMN etl_job_progress.status IS 'Status of current stage';
COMMENT ON COLUMN etl_job_progress.message IS 'Detailed progress message for user feedback';

COMMENT ON COLUMN etl_validation_errors.error_id IS 'Unique identifier for validation error';
COMMENT ON COLUMN etl_validation_errors.job_id IS 'Reference to ETL job that encountered this error';
COMMENT ON COLUMN etl_validation_errors.sheet_name IS 'Excel sheet name where error occurred';
COMMENT ON COLUMN etl_validation_errors.row_number IS 'Row number in sheet where error was found';
COMMENT ON COLUMN etl_validation_errors.column_name IS 'Column name where error occurred';
COMMENT ON COLUMN etl_validation_errors.error_type IS 'Category of validation error';
COMMENT ON COLUMN etl_validation_errors.error_message IS 'Detailed error description';
COMMENT ON COLUMN etl_validation_errors.suggested_fix IS 'Recommended action to resolve the error';