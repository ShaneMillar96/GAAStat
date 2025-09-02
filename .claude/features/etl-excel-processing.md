# Feature: Excel ETL Data Processing System

## Executive Summary

The Excel ETL (Extract, Transform, Load) feature enables GAAStat users to upload comprehensive match data via Excel files (specifically .xlsx format) containing 32 sheets of statistical information. The system automatically extracts, transforms, and loads all data into the PostgreSQL database using sophisticated mapping logic that handles complex data structures including match statistics, player performance, specialized analytics, and positional analysis.

This feature transforms the manual data entry process into an automated, bulk processing system that can handle extensive GAA match statistics while maintaining data integrity and providing comprehensive error handling and progress tracking.

## Problem Statement

Currently, GAAStat requires manual data entry for match statistics, which is time-consuming and error-prone when dealing with comprehensive statistical data. Users have rich Excel files containing detailed match analysis (32 sheets with 200+ metrics per match) but no efficient way to import this data into the application's database. The complex data structures, varying sheet formats, and intricate relationships between different data types require a sophisticated ETL solution.

## Objectives & Success Metrics

### Primary Objectives
- **Automated Data Import**: Enable one-click upload and processing of complex Excel files
- **Data Integrity**: Ensure 100% accuracy in data transformation and mapping
- **User Experience**: Provide clear progress feedback and comprehensive error reporting
- **Performance**: Process large Excel files efficiently (32 sheets with thousands of data points)
- **Scalability**: Support multiple concurrent uploads and varying file sizes

### Success Metrics
- Upload success rate: >99%
- Data validation accuracy: 100% (no corrupted data)
- Processing time: <2 minutes for typical match file
- User satisfaction: Clear progress indicators and error reporting
- Data completeness: All 32 sheet types successfully processed

## Functional Requirements

### Core Functionality
1. **File Upload Interface**
   - Single-click file selection for .xlsx files
   - File size validation (max 50MB configurable)
   - File type validation (only .xlsx allowed)
   - Drag-and-drop support

2. **Automated Sheet Detection**
   - Identify all 32 expected sheet types
   - Handle sheet name variations and formatting
   - Report missing or unexpected sheets

3. **Data Extraction**
   - Parse match team statistics (8 sheets with 235+ metrics each)
   - Extract player statistics (8 sheets with 80+ fields per player)
   - Process specialized analytics (kickouts, shots, scoreable frees)
   - Handle positional analysis and aggregated data

4. **Data Transformation**
   - Score parsing ("2-06" → goals=2, points=6)
   - Percentage conversions and NULL handling
   - Player name resolution and validation
   - Data type conversions and validation

5. **Data Loading**
   - Bulk insert operations for performance
   - Transactional integrity (all-or-nothing)
   - Relationship mapping and foreign key validation
   - Duplicate detection and handling

6. **Progress Tracking**
   - Real-time progress updates
   - Stage-by-stage completion indicators
   - Detailed logging of each processing step

7. **Error Handling**
   - Comprehensive validation reporting
   - Data quality issue identification
   - Recovery suggestions and partial success handling

### User Stories

**As a GAA statistician, I want to:**
- Upload a comprehensive Excel file and have all match data automatically imported
- See clear progress indicators during the import process
- Receive detailed feedback about any data quality issues
- Have confidence that all data relationships are preserved correctly

**As a system administrator, I want to:**
- Monitor ETL job performance and success rates
- Access detailed logs for troubleshooting failed imports
- Configure processing parameters and validation rules
- Ensure system performance isn't impacted by large uploads

## Technical Architecture

### Technology Research

**EPPlus Library Analysis:**
Based on research of EPPlus documentation, this Excel processing library provides excellent capabilities for our requirements:

- **Data Extraction**: `LoadFromDataTable`, `LoadFromCollection`, and `ToDataTable` methods for efficient data handling
- **Sheet Processing**: Support for reading multiple worksheets and cell ranges
- **Data Types**: Robust handling of various data types with conversion capabilities
- **Performance**: Bulk operations and async support for large files
- **Validation**: Built-in data validation features

**Key Integration Patterns Identified:**
- Use `ExcelPackage` for file handling with proper disposal patterns
- Leverage `ExcelWorksheet.Cells` for range-based data extraction
- Implement `ToDataTable()` for efficient data transformation
- Use async methods (`LoadFromTextAsync`, `LoadFromDataReaderAsync`) for large operations

### Components

#### 1. API Layer Components
- **ETLController**: HTTP endpoints for file upload and progress tracking
- **FileUploadMiddleware**: Stream processing and validation
- **ProgressHub**: SignalR hub for real-time progress updates

#### 2. Service Layer Components
- **IExcelProcessingService**: Core ETL orchestration service
- **IExcelParsingService**: Excel file reading and sheet detection
- **IDataTransformationService**: Data transformation and validation logic
- **IDataMappingService**: Excel-to-database mapping coordination
- **IProgressTrackingService**: Progress reporting and user feedback

#### 3. Data Processing Components
- **SheetProcessor**: Base class for processing individual sheet types
- **MatchStatsProcessor**: Handles team statistics sheets
- **PlayerStatsProcessor**: Processes player performance data
- **SpecializedAnalyticsProcessor**: Manages kickouts, shots, and frees
- **ValidationEngine**: Comprehensive data validation framework

#### 4. Infrastructure Components
- **ETLJobTracker**: Job status and progress persistence
- **BulkDataInserter**: Optimized database insertion logic
- **ErrorReporter**: Error aggregation and user notification

### Data Model

#### ETL Tracking Tables
```sql
CREATE TABLE etl_jobs (
    job_id SERIAL PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    file_size_bytes BIGINT NOT NULL,
    status VARCHAR(50) NOT NULL, -- pending, processing, completed, failed
    started_at TIMESTAMP,
    completed_at TIMESTAMP,
    error_summary TEXT,
    created_by VARCHAR(100)
);

CREATE TABLE etl_job_progress (
    progress_id SERIAL PRIMARY KEY,
    job_id INTEGER REFERENCES etl_jobs(job_id),
    stage VARCHAR(100) NOT NULL,
    total_steps INTEGER,
    completed_steps INTEGER,
    status VARCHAR(50),
    message TEXT,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE etl_validation_errors (
    error_id SERIAL PRIMARY KEY,
    job_id INTEGER REFERENCES etl_jobs(job_id),
    sheet_name VARCHAR(100),
    row_number INTEGER,
    column_name VARCHAR(100),
    error_type VARCHAR(50),
    error_message TEXT,
    suggested_fix TEXT
);
```

### API Design

#### Endpoints

**POST /api/etl/upload**
```csharp
[HttpPost("upload")]
public async Task<IActionResult> UploadExcelFile([FromForm] IFormFile file)
{
    var validationResult = await _fileValidationService.ValidateUploadAsync(file);
    if (!validationResult.IsValid)
        return BadRequest(ApiResponse.Error("Invalid file", validationResult.Errors));
    
    var jobId = await _excelProcessingService.StartProcessingAsync(file);
    return Accepted(ApiResponse.Success(new { JobId = jobId }));
}
```

**GET /api/etl/progress/{jobId}**
```csharp
[HttpGet("progress/{jobId}")]
public async Task<IActionResult> GetProgress(int jobId)
{
    var progress = await _progressTrackingService.GetProgressAsync(jobId);
    return Ok(ApiResponse.Success(progress));
}
```

**GET /api/etl/jobs**
```csharp
[HttpGet("jobs")]
public async Task<IActionResult> GetRecentJobs([FromQuery] int pageSize = 20)
{
    var jobs = await _etlJobService.GetRecentJobsAsync(pageSize);
    return Ok(ApiResponse.Success(jobs));
}
```

### Integration Points

#### Database Integration
- Direct integration with existing GAAStat PostgreSQL schema
- Utilizes current EF Core models and contexts
- Maintains referential integrity with existing data

#### Existing Services Integration
- Leverages current player and team management services
- Integrates with match result calculation logic
- Uses established validation patterns

#### Frontend Integration
- Progressive web app upload component
- Real-time progress updates via SignalR
- Integration with existing match viewing workflows

## Implementation Phases

### Phase 1: Core ETL Infrastructure (MVP)
**Duration**: 2-3 weeks

**Components**:
1. Basic file upload API endpoint
2. Excel parsing service with EPPlus integration
3. Simple data transformation for match headers
4. Basic progress tracking
5. Error handling framework

**Deliverables**:
- Upload single Excel file
- Parse and validate basic match information
- Insert match headers into database
- Basic progress feedback

**Acceptance Criteria**:
- Successfully upload and validate .xlsx files
- Extract match metadata (date, teams, scores)
- Create match records with proper relationships
- Report basic validation errors

### Phase 2: Comprehensive Sheet Processing
**Duration**: 3-4 weeks

**Components**:
1. All 32 sheet type processors
2. Complete data transformation logic
3. Advanced validation engine
4. Bulk data insertion optimization
5. Detailed progress tracking

**Deliverables**:
- Process all sheet types (team stats, player stats, analytics)
- Handle complex data transformations
- Validate data relationships and integrity
- Optimized database insertion

**Acceptance Criteria**:
- Process 235+ team metrics per match
- Handle 80+ player fields across multiple matches
- Validate score consistency and player name resolution
- Complete processing within 2-minute target

### Phase 3: Advanced Features & Optimization
**Duration**: 2-3 weeks

**Components**:
1. Concurrent upload support
2. Advanced error recovery
3. Data quality reporting
4. Performance monitoring
5. Admin management interface

**Deliverables**:
- Multiple simultaneous uploads
- Comprehensive error reporting and recovery
- Performance dashboards
- Data quality metrics and reporting

**Acceptance Criteria**:
- Support 5+ concurrent uploads
- Provide detailed data quality reports
- Achieve 99%+ success rate
- Comprehensive admin monitoring tools

## Risk Assessment

### Technical Risks

**High Priority**:
1. **Data Mapping Complexity**: 32 different sheet structures with varying formats
   - *Mitigation*: Comprehensive mapping documentation and flexible parsing logic
   - *Contingency*: Phased approach with incremental sheet type support

2. **Performance at Scale**: Large Excel files with thousands of data points
   - *Mitigation*: Async processing, bulk operations, and progress chunking
   - *Contingency*: File size limits and background processing

3. **Data Validation Complexity**: Complex relationships and business rules
   - *Mitigation*: Comprehensive validation framework with detailed error reporting
   - *Contingency*: Manual validation tools for edge cases

**Medium Priority**:
4. **Memory Management**: Large file processing memory usage
   - *Mitigation*: Streaming approaches and proper disposal patterns
   - *Contingency*: File chunking and background processing

5. **Database Performance**: Bulk inserts affecting system performance
   - *Mitigation*: Optimized bulk operations and off-peak processing
   - *Contingency*: Queue-based processing with rate limiting

### Business Risks

6. **User Adoption**: Complexity of data validation feedback
   - *Mitigation*: Clear, actionable error messages and data quality guides
   - *Contingency*: Training materials and support documentation

7. **Data Quality**: Inconsistent Excel file formats
   - *Mitigation*: Flexible parsing logic and comprehensive validation
   - *Contingency*: Manual review workflows for problematic files

## Testing Strategy

### Unit Testing
- **Service Layer**: 90%+ coverage of transformation and validation logic
- **Data Processing**: Test each sheet processor with sample data
- **Error Handling**: Comprehensive exception testing
- **Validation Rules**: Test all data validation scenarios

**Key Test Cases**:
```csharp
[Test]
public async Task ProcessMatchStatsSheet_ValidData_ReturnsSuccess()
[Test]
public async Task TransformPlayerStats_InvalidScore_ReturnsValidationError()
[Test]
public async Task ValidatePlayerName_FuzzyMatch_ResolvesCorrectly()
```

### Integration Testing
- **Database Operations**: Test complete ETL pipeline with sample Excel files
- **API Endpoints**: Test file upload and progress tracking workflows
- **Progress Updates**: Verify real-time progress reporting
- **Error Recovery**: Test partial failure scenarios and recovery

### User Acceptance Testing
- **End-to-End Workflows**: Complete upload and validation cycles
- **Error Scenarios**: Test with intentionally problematic Excel files
- **Performance Testing**: Test with maximum file sizes and complexity
- **UI/UX Testing**: Validate progress indicators and error reporting

### Performance Testing
- **Load Testing**: Multiple concurrent uploads
- **Stress Testing**: Maximum file sizes (50MB limit)
- **Memory Testing**: Long-running processes and memory cleanup
- **Database Impact**: Bulk operations effect on system performance

## Recommended Agent Assignments

Based on the technical requirements and system complexity:

### **backend-developer**: Core Service Implementation
- Excel parsing service development using EPPlus
- Data transformation and validation logic
- Database integration and bulk operations
- Error handling and logging systems
- Performance optimization and async processing

### **api-developer**: API Layer Development
- Upload endpoints with file validation
- Progress tracking and status endpoints
- SignalR hub for real-time updates
- Authentication and authorization integration
- API documentation and testing

### **database-engineer**: Data Architecture & ETL Optimization
- ETL tracking table design and implementation
- Bulk insertion optimization strategies  
- Database performance monitoring during ETL
- Migration scripts for ETL infrastructure
- Data validation and consistency checks

### **frontend-developer**: User Interface Components
- File upload component with progress indicators
- ETL job management dashboard
- Error reporting and validation feedback UI
- Real-time progress updates integration
- Responsive design for various devices

### **devops-engineer**: Infrastructure & Deployment
- File storage and processing infrastructure
- Background job processing configuration
- Monitoring and alerting for ETL processes
- Performance metrics and logging infrastructure
- Deployment automation for ETL components

## Timeline Estimate

### Total Duration: 7-10 weeks

**Phase 1: Core Infrastructure (Weeks 1-3)**
- Week 1: Project setup, EPPlus integration, basic API
- Week 2: Core parsing logic, match header processing
- Week 3: Basic progress tracking, error handling, testing

**Phase 2: Full Sheet Processing (Weeks 4-7)**
- Week 4: Team statistics and player statistics processors
- Week 5: Specialized analytics (kickouts, shots, frees)
- Week 6: Data validation, bulk operations, optimization
- Week 7: Integration testing, bug fixes, performance tuning

**Phase 3: Advanced Features (Weeks 8-10)**
- Week 8: Concurrent processing, advanced error recovery
- Week 9: Admin interface, monitoring, documentation
- Week 10: Final testing, deployment, user training

**Milestones**:
- Week 3: MVP demo with basic match processing
- Week 7: Full feature demo with all sheet types
- Week 10: Production-ready system with monitoring

## Open Questions

### Technical Decisions Required
1. **File Storage Strategy**: Local storage vs. cloud storage for uploaded files during processing
2. **Processing Architecture**: Synchronous vs. asynchronous processing for user experience
3. **Error Recovery**: Automatic retry mechanisms vs. manual intervention requirements
4. **Data Validation Strictness**: Fail-fast vs. best-effort processing with warnings

### Business Requirements Clarification
5. **User Permissions**: Who can upload files and access ETL functionality?
6. **Data Retention**: How long to retain ETL job history and uploaded files?
7. **Concurrent Upload Limits**: Maximum simultaneous uploads per user/system?
8. **File Size Limits**: Actual practical limits based on infrastructure capacity?

### Integration Considerations
9. **Existing Data Handling**: How to handle conflicts with existing match data?
10. **Database Migration**: Impact on existing data during ETL table additions?
11. **Performance Impact**: Acceptable system performance degradation during processing?
12. **Monitoring Requirements**: What metrics and alerts are needed for production?

This comprehensive feature plan provides a solid foundation for implementing a robust, scalable ETL system that will transform the GAAStat data import process while maintaining high standards of data integrity and user experience.