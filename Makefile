# GAAStat Development Commands

.PHONY: help start-db stop-db migrate-db scaffold-models start-backend start-frontend build-backend clean install

help: ## Show this help message
	@echo "GAAStat Development Commands:"
	@echo "=============================="
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

start-db: ## Start PostgreSQL database with Docker Compose
	docker compose up -d

stop-db: ## Stop PostgreSQL database
	docker compose down

migrate-db: ## Apply Flyway migrations to database
	docker compose up flyway

scaffold-models: ## Generate EF Core models from database
	cd backend && dotnet ef dbcontext scaffold "Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;" Npgsql.EntityFrameworkCore.PostgreSQL -o src/GAAStat.Dal/Models/application --force --data-annotations --project src/GAAStat.Dal --startup-project src/GAAStat.Api

start-backend: ## Start .NET API development server
	cd backend && dotnet run --project src/GAAStat.Api

start-frontend: ## Start React development server
	cd frontend && npm run dev

build-backend: ## Build .NET solution
	cd backend && dotnet build

test-backend: ## Run backend tests
	cd backend && dotnet test

install: ## Install all dependencies
	cd frontend && npm install

clean: ## Clean build artifacts
	cd backend && dotnet clean
	cd frontend && rm -rf node_modules dist

setup: start-db migrate-db install ## Initial project setup

dev: ## Start full development environment
	@echo "Starting GAAStat Development Environment..."
	@echo "1. Starting database..."
	@make start-db
	@echo "2. Applying migrations..."
	@sleep 5
	@make migrate-db
	@echo "3. Database ready! You can now:"
	@echo "   - Run 'make start-backend' to start the API"
	@echo "   - Run 'make start-frontend' to start the frontend"
	@echo "   - Run 'make scaffold-models' to generate EF models"