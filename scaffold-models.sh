#!/bin/bash

# GAAStat - Database Model Scaffolding Script
# This script generates Entity Framework Core models from the PostgreSQL database

echo "🔧 Scaffolding Entity Framework Models from Database..."
echo "======================================================"

# Connection string
CONNECTION_STRING="Host=localhost;Database=gaastat-dev;Username=gaastat;Password=password1;"

# Navigate to backend directory
cd backend

echo "📦 Running Entity Framework scaffolding..."

# Run the scaffold command
dotnet ef dbcontext scaffold \
  "$CONNECTION_STRING" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  -o src/GAAStat.Dal/Models/application \
  --force \
  --data-annotations \
  --project src/GAAStat.Dal \
  --startup-project src/GAAStat.Api \
  --context-dir src/GAAStat.Dal/Contexts \
  --context GAAStatDbContext

echo "✅ Models scaffolded successfully!"
echo "📁 Generated files are located in: src/GAAStat.Dal/Models/application/"
echo "🔍 Database context updated at: src/GAAStat.Dal/Contexts/GAAStatDbContext.cs"

echo ""
echo "🚀 Next Steps:"
echo "1. Review the generated models in src/GAAStat.Dal/Models/application/"
echo "2. Start the API server: make start-backend"
echo "3. Start the frontend: make start-frontend"