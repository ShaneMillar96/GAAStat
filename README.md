
# GAAStat

A comprehensive GAA (Gaelic Athletic Association) statistics and analytics platform built with .NET and React.

## 🏑 Features

- **Team Management**: Create and manage hurling and football teams
- **Player Profiles**: Track detailed player information and statistics
- **Match Recording**: Record match results and player performances
- **Analytics Dashboard**: Analyze team and player performance metrics
- **Database-First Approach**: PostgreSQL with Flyway migrations and EF Core scaffolding

## 🚀 Tech Stack

### Backend
- **.NET 9.0** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API
- **Entity Framework Core 9** - ORM with PostgreSQL provider
- **PostgreSQL 16** - Primary database
- **Flyway** - Database migrations
- **xUnit** - Testing framework

### Frontend
- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Fast build tool and dev server
- **Tailwind CSS** - Utility-first styling
- **Axios** - HTTP client

### Infrastructure
- **Docker Compose** - Container orchestration
- **PostgreSQL Container** - Database instance
- **Flyway Container** - Migration runner

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

## 🐳 Docker Commands

```bash
# Start database
make start-db

# Stop database
make stop-db

# Apply migrations
make migrate-db
```

## 🗄️ Database

The application uses PostgreSQL with Flyway for migrations. The initial schema includes:

- **users** - User authentication and profiles
- **teams** - GAA teams (hurling/football)
- **players** - Player profiles linked to teams
- **matches** - Match information between teams
- **player_stats** - Individual player performance in matches

### Database Schema

```sql
-- Key tables
CREATE TABLE teams (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    sport VARCHAR(20) NOT NULL CHECK (sport IN ('hurling', 'football')),
    county VARCHAR(50),
    division VARCHAR(50)
);

CREATE TABLE players (
    id SERIAL PRIMARY KEY,
    team_id INT NOT NULL REFERENCES teams(id),
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    jersey_number INT,
    position VARCHAR(30)
);
```

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

## 📝 Entity Framework Core Commands

### Scaffold Models from Database
```bash
dotnet ef dbcontext scaffold "Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;" Npgsql.EntityFrameworkCore.PostgreSQL -o Models/application --force --data-annotations
```

### Add Migration (if using Code-First)
```bash
dotnet ef migrations add InitialCreate --project src/GAAStat.Dal --startup-project src/GAAStat.Api
```

### Update Database (if using Code-First)
```bash
dotnet ef database update --project src/GAAStat.Dal --startup-project src/GAAStat.Api
```

## 🏗️ Project Structure

```
GAAStat/
├── backend/                    # .NET Backend
│   ├── GAAStat.sln            # Solution file
│   ├── src/
│   │   ├── GAAStat.Api/       # Web API project
│   │   ├── GAAStat.Dal/       # Data Access Layer
│   │   └── GAAStat.Services/  # Business Logic
│   └── test/
│       ├── GAAStat.Api.Tests/
│       └── GAAStat.Services.Tests/
├── frontend/                   # React Frontend
│   ├── src/
│   ├── package.json
│   └── vite.config.ts
├── database/                   # Database assets
│   ├── flyway.conf
│   └── migrations/
│       └── V1.0__initial_schema.sql
├── docker-compose.yml         # Docker services
└── Makefile                   # Development commands
```

## 🌐 API Endpoints

The API will be available at `http://localhost:5000` with the following endpoints:

- `GET /api/teams` - List all teams
- `GET /api/teams/{id}` - Get team by ID
- `POST /api/teams` - Create new team
- `PUT /api/teams/{id}` - Update team
- `DELETE /api/teams/{id}` - Delete team

(Additional endpoints for players, matches, and statistics)

## 🧪 Testing

```bash
# Run all tests
make test-backend

# Run specific test project
dotnet test backend/test/GAAStat.Api.Tests/

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 🚢 Deployment

### Environment Variables

Set these environment variables in production:

```bash
DATABASE_CONNECTION_STRING="Host=prod-host;Database=gaastat;Username=user;Password=password;"
ASPNETCORE_ENVIRONMENT=Production
```

### Build for Production

```bash
# Backend
cd backend && dotnet publish -c Release

# Frontend
cd frontend && npm run build
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For support, please open an issue on GitHub or contact the development team.

---

**Built with ❤️ for the GAA community**