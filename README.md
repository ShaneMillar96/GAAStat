# GAAStat

A comprehensive GAA (Gaelic Athletic Association) statistics and analytics platform built with .NET and React, designed to transform complex match data from CSV/Excel files into intuitive, actionable insights.

## 📊 Project Overview

GAAStat revolutionizes how GAA teams and analysts work with match statistics by providing a modern web-based platform that ingests, processes, and visualizes match data. Instead of wrestling with confusing spreadsheets scattered across multiple tabs, users can upload their CSV/Excel files and immediately access clean, organized statistics through an intuitive dashboard.

The platform serves coaches, analysts, and team management who need to:
- Convert raw match data into meaningful insights
- Track player and team performance over time
- Access historical trends and comparisons
- Generate reports for strategic decision-making
- Visualize complex statistics in an easy-to-understand format

## 🎯 Problem Definition

### The Challenge
Current GAA statistics management relies heavily on Excel spreadsheets that present several critical issues:

- **Data Fragmentation**: Statistics are scattered across multiple sheets within a single file, making holistic analysis difficult
- **Poor Usability**: Complex layouts and inconsistent formatting make data interpretation time-consuming and error-prone
- **Limited Analysis**: No built-in capability for historical trends, comparisons, or advanced analytics
- **Manual Processes**: Data entry and validation are manual, increasing the likelihood of errors
- **Accessibility**: Files are difficult to share and collaborate on, limiting team-wide insights
- **Scalability**: As seasons progress, managing multiple files becomes increasingly cumbersome

### The Solution
GAAStat addresses these challenges through:

- **Streamlined Data Ingestion**: Simple CSV/Excel upload functionality that automatically parses and validates match data
- **Centralized Database**: All match statistics stored in a structured PostgreSQL database optimized for analytics queries
- **Intuitive Web Interface**: Clean, modern dashboard presenting statistics in easy-to-read formats
- **Match-by-Match Analysis**: Each uploaded file represents a match, allowing users to drill down into specific game performance
- **Comprehensive Analytics**: Automated calculation of key metrics including top scorers, turnovers, shots, total scores, and performance trends
- **Historical Insights**: Track performance over time with season and career statistics
- **Collaborative Platform**: Web-based access enables team-wide data sharing and analysis

## 🎯 MoSCoW Priorities

### Must Have 🔴
- **CSV/Excel File Upload**: Core functionality to upload and process match data files
- **Data Parsing & Validation**: Robust parsing engine that handles various file formats and validates data integrity
- **Match Statistics Display**: Present uploaded match data in a clean, organized format
- **Player Performance Metrics**: Individual player stats including points, goals, assists, turnovers, fouls
- **Team Performance Metrics**: Team-level statistics and match summaries
- **Basic Dashboard**: Key performance indicators and summary statistics
- **Database Storage**: Secure, structured storage of all processed match data

### Should Have 🟡
- **Historical Trend Analysis**: Performance tracking across multiple matches and seasons
- **Advanced Filtering & Search**: Filter matches by date, opponent, competition, player, etc.
- **Performance Comparisons**: Compare player/team performance across different matches
- **Data Export**: Export processed data in various formats (PDF reports, CSV, Excel)
- **User Authentication**: Secure login system to protect team data
- **Responsive Design**: Mobile-friendly interface for on-the-go access
- **Data Visualization Charts**: Graphs and charts for better data representation

### Could Have 🔵
- **Custom Report Generation**: Create tailored reports for specific analysis needs
- **Team/Player Rankings**: Automated ranking systems based on performance metrics
- **Season-over-Season Comparisons**: Multi-season analysis and trend identification
- **Advanced Analytics**: Statistical modeling and predictive insights
- **Bulk File Processing**: Upload and process multiple match files simultaneously
- **Data Import Templates**: Standardized templates for consistent data formatting
- **Performance Alerts**: Notifications for significant performance changes or milestones

### Won't Have ⚪ (Initial Release)
- **Live Match Streaming**: Real-time match updates and live statistics
- **Social Media Integration**: Sharing statistics directly to social platforms
- **Video Analysis Integration**: Linking statistics with match footage
- **Mobile App**: Native mobile applications (web-responsive initially)
- **Multi-Language Support**: International language options
- **API for Third-Party Integrations**: External system connectivity

## 🏗️ Technical Architecture

### Data Flow Architecture
```
CSV/Excel Upload → Data Parser → Validation Layer → ETL Pipeline → PostgreSQL Database → REST API → React Frontend → Analytics Dashboard
```

### Core Components

#### Data Ingestion Layer
- **File Upload Service**: Handles CSV/Excel file uploads with format validation
- **Data Parser**: Processes various file formats and extracts statistical data
- **Validation Engine**: Ensures data integrity and consistency before storage
- **ETL Pipeline**: Transforms raw data into structured database records

#### Data Storage Layer
- **PostgreSQL Database**: Optimized schema for match statistics and analytics queries
- **Entity Framework Core**: ORM for database operations and migrations
- **Flyway Migrations**: Version-controlled database schema management

#### API Layer
- **ASP.NET Core Web API**: RESTful endpoints for data operations
- **File Processing Endpoints**: Handle file uploads and processing status
- **Statistics Endpoints**: Serve processed data to frontend applications
- **Authentication & Authorization**: Secure API access controls

#### Frontend Layer
- **React 18 with TypeScript**: Modern, type-safe user interface
- **Data Visualization**: Charts and graphs using specialized libraries
- **Responsive Design**: Tailwind CSS for consistent, mobile-friendly styling
- **State Management**: Efficient data flow and caching strategies

#### Analytics Engine
- **Statistical Calculations**: Automated computation of performance metrics
- **Trend Analysis**: Historical performance tracking and comparison
- **Report Generation**: Dynamic creation of statistical reports

### Database Schema Design

The database is optimized for analytical queries with the following key relationships:

- **Matches**: Central entity representing each uploaded CSV/Excel file
- **Teams**: GAA teams participating in matches
- **Players**: Individual athletes linked to teams
- **Player Statistics**: Detailed performance metrics per match per player
- **Computed Metrics**: Pre-calculated analytics for improved query performance

## 🚀 Tech Stack

### Backend Infrastructure
- **.NET 9.0** - Latest .NET framework for optimal performance
- **ASP.NET Core Web API** - RESTful API architecture
- **Entity Framework Core 9** - Database ORM with PostgreSQL provider
- **PostgreSQL 16** - Primary database optimized for analytics
- **Flyway** - Database migration management
- **xUnit** - Comprehensive testing framework

### Frontend Technologies
- **React 18** - Modern UI library with concurrent features
- **TypeScript** - Type safety for robust development
- **Vite** - Fast build tool and development server
- **Tailwind CSS** - Utility-first styling framework
- **Axios** - HTTP client for API communication
- **Data Visualization Libraries** - Charts and graphs for statistics display

### Data Processing
- **CSV Parser Libraries** - Handle various CSV formats and encodings
- **Excel Processing Libraries** - Support for .xlsx and .xls file formats
- **Data Validation Libraries** - Ensure data quality and consistency
- **Background Job Processing** - Asynchronous file processing capabilities

### Infrastructure & DevOps
- **Docker Compose** - Container orchestration for development
- **PostgreSQL Container** - Containerized database instance
- **Flyway Container** - Automated migration execution
- **Make** - Build automation and development commands

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 18+](https://nodejs.org/)
- [Docker](https://www.docker.com/get-started)
- [Docker Compose](https://docs.docker.com/compose/)

## 🛠️ Quick Start

### 1. Clone and Setup
```bash
git clone <repository-url>
cd GAAStat
make setup
```

This will:
- Start the PostgreSQL database
- Apply Flyway migrations
- Install frontend dependencies

### 2. Generate Database Models
```bash
make scaffold-models
```

This generates EF Core models from the database schema.

### 3. Start Development Servers

#### Backend API
```bash
make start-backend
```
API will be available at: `http://localhost:5000`

#### Frontend
```bash
make start-frontend
```
Frontend will be available at: `http://localhost:5173`

## 📤 File Upload Workflow

### Supported File Formats
- **CSV Files**: Comma-separated values with standard GAA statistics columns
- **Excel Files**: .xlsx and .xls formats with structured match data

### Data Requirements
Each uploaded file should contain:
- Match information (teams, date, venue)
- Player roster for both teams
- Individual player statistics (points, goals, assists, turnovers, fouls, etc.)
- Match summary data

### Upload Process
1. Navigate to the file upload interface
2. Select your CSV/Excel match file
3. System validates file format and structure
4. Data is parsed and processed
5. Match statistics are stored in the database
6. Success confirmation with match summary

## 🐳 Docker Commands

```bash
# Start database
make start-db

# Stop database
make stop-db

# Apply migrations
make migrate-db
```

## 🗄️ Database Schema

The application uses PostgreSQL with Flyway for migrations. The schema is optimized for analytical queries:

### Core Tables
```sql
-- Matches: Each uploaded CSV/Excel file represents a match
CREATE TABLE matches (
    id SERIAL PRIMARY KEY,
    home_team_id INT NOT NULL,
    away_team_id INT NOT NULL,
    match_date TIMESTAMP NOT NULL,
    venue VARCHAR(100),
    competition VARCHAR(100),
    home_score INT DEFAULT 0,
    away_score INT DEFAULT 0,
    file_name VARCHAR(255), -- Original uploaded file name
    status VARCHAR(20) DEFAULT 'completed'
);

-- Player statistics: Detailed performance metrics per match
CREATE TABLE player_stats (
    id SERIAL PRIMARY KEY,
    match_id INT NOT NULL,
    player_id INT NOT NULL,
    minutes_played INT DEFAULT 0,
    points_scored INT DEFAULT 0,
    goals_scored INT DEFAULT 0,
    assists INT DEFAULT 0,
    turnovers INT DEFAULT 0,
    fouls_committed INT DEFAULT 0,
    fouls_drawn INT DEFAULT 0,
    shots_attempted INT DEFAULT 0,
    shots_on_target INT DEFAULT 0
);
```

## 📊 Analytics Features

### Match-Level Analytics
- **Top Scorers**: Highest point and goal scorers per match
- **Performance Metrics**: Team and individual KPIs
- **Turnover Analysis**: Ball retention and possession statistics
- **Shooting Accuracy**: Shot conversion rates and efficiency

### Historical Analytics
- **Season Trends**: Performance tracking over multiple matches
- **Player Development**: Individual improvement over time
- **Head-to-Head**: Historical matchup analysis
- **Competition Analysis**: Performance across different tournaments

### Dashboard Metrics
- Total matches processed
- Overall team performance
- Key player statistics
- Recent match summaries
- Performance trends and insights

## 🔧 Development Commands

```bash
# Show all available commands
make help

# Build backend
make build-backend

# Run backend tests
make test-backend

# Clean build artifacts
make clean

# Full development environment setup
make dev
```

## 🧪 Testing

### Backend Testing
```bash
# Run all tests
make test-backend

# Run specific test project
dotnet test backend/test/GAAStat.Api.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### File Processing Testing
- Test with various CSV formats
- Validate Excel file compatibility
- Error handling for malformed data
- Performance testing with large files

## 🌐 API Endpoints

The API will be available at `http://localhost:5000` with the following endpoints:

### File Management
- `POST /api/files/upload` - Upload CSV/Excel match files
- `GET /api/files/status/{id}` - Check file processing status
- `GET /api/files/history` - List uploaded files

### Match Statistics
- `GET /api/matches` - List all processed matches
- `GET /api/matches/{id}` - Get specific match statistics
- `GET /api/matches/{id}/players` - Get player statistics for a match

### Analytics
- `GET /api/analytics/dashboard` - Dashboard summary statistics
- `GET /api/analytics/players/top-scorers` - Top scoring players
- `GET /api/analytics/teams/{id}/trends` - Team performance trends

## 🚢 Deployment

### Environment Variables
Set these environment variables in production:

```bash
DATABASE_CONNECTION_STRING="Host=prod-host;Database=gaastat;Username=user;Password=password;"
ASPNETCORE_ENVIRONMENT=Production
FILE_UPLOAD_PATH="/var/uploads"
MAX_FILE_SIZE_MB=50
```

### Build for Production
```bash
# Backend
cd backend && dotnet publish -c Release

# Frontend
cd frontend && npm run build
```

## 🏗️ Project Structure

```
GAAStat/
├── backend/                    # .NET Backend
│   ├── GAAStat.sln            # Solution file
│   ├── src/
│   │   ├── GAAStat.Api/       # Web API project
│   │   ├── GAAStat.Dal/       # Data Access Layer
│   │   └── GAAStat.Services/  # Business Logic & File Processing
│   └── test/
│       ├── GAAStat.Api.Tests/
│       └── GAAStat.Services.Tests/
├── frontend/                   # React Frontend
│   ├── src/
│   │   ├── components/        # React components
│   │   ├── services/          # API services
│   │   ├── types/             # TypeScript definitions
│   │   └── utils/             # Utility functions
│   ├── package.json
│   └── vite.config.ts
├── database/                   # Database assets
│   ├── flyway.conf
│   └── migrations/
│       └── V1.0__initial_schema_setup.sql
├── docker-compose.yml         # Docker services
├── Makefile                   # Development commands
└── docs/                      # Documentation
    ├── api-specification.md
    ├── file-format-guide.md
    └── deployment-guide.md
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/file-processing-enhancement`)
3. Commit your changes (`git commit -m 'Add CSV parsing improvements'`)
4. Push to the branch (`git push origin feature/file-processing-enhancement`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For support, please open an issue on GitHub or contact the development team.

---

**Built with ❤️ for the GAA community - transforming match data into winning insights**