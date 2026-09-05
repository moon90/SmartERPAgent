#!/usr/bin/env bash
# =====================================================================================
# Smart ERP Agent - Database Seeder Utility Script
# Populates demo tenants (ACME_CORP, GLOBAL_LOGISTICS, BIOTECH_MED) and sample inventory
# =====================================================================================

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "=========================================================="
echo "🚀 Smart ERP Agent: Seeding Live SQL Server Database"
echo "=========================================================="

# Check if SQL Server container is running
if docker ps | grep -q smarterp-sql; then
    echo "✓ Found running SQL Server container 'smarterp-sql'."
    echo "Executing SQL seed script via Docker sqlcmd..."
    docker exec -i smarterp-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Dev@123456" -C -i /dev/stdin < "$SCRIPT_DIR/seed-data.sql"
    echo "✓ SQL script executed."
else
    echo "ℹ SQL Server container not running directly as 'smarterp-sql'. Using .NET API seeder runner..."
    dotnet run --project "$BACKEND_DIR/src/SmartErpAgent.Api/SmartErpAgent.Api.csproj" -- --seed
fi

echo "----------------------------------------------------------"
echo "📊 Current Database Summary:"
docker exec -i smarterp-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "Dev@123456" -C -Q \
    "SELECT 'Tenants' AS [Table], count(*) AS [Count] FROM SmartErpAgentDb.dbo.Tenants UNION ALL SELECT 'InventoryItems', count(*) FROM SmartErpAgentDb.dbo.InventoryItems UNION ALL SELECT 'Invoices', count(*) FROM SmartErpAgentDb.dbo.Invoices;" 2>/dev/null || true

echo "=========================================================="
echo "🎉 Seeding complete!"
echo "=========================================================="
