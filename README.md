# FlyrTech - Redis Race Condition Challenge

A .NET 8 solution demonstrating and solving concurrency problems with Redis distributed cache.

## 🎯 Race Condition Challenge

This solution includes an **intentional race condition** to demonstrate data loss in concurrent update scenarios.

### The Problem

The `JourneyService.UpdateSegmentStatusAsync()` method has a critical race condition:

**Current Implementation (Problematic):**
1. **Read** entire journey from Redis
2. **Modify** a specific segment in memory
3. **Write** entire journey back to Redis

**What Goes Wrong with 20 Concurrent Updates:**
- Thread 1 reads journey → modifies SEG-001 → writes back
- Thread 2 reads journey → modifies SEG-002 → writes back **← OVERWRITES Thread 1!**
- Thread 3 reads journey → modifies SEG-003 → writes back **← OVERWRITES Threads 1 & 2!**
- **Result:** Only the last update survives. 17-19 updates are lost! 💥

### Quick Start

**1. Start Redis:**
```powershell
docker run -d -p 6379:6379 --name redis-flyrtech redis:latest
```

**2. Run the failing test:**
```powershell
dotnet test --filter "FullyQualifiedName~UpdateSegmentStatus_With20ConcurrentUpdates"
```
**Expected:** ❌ TEST FAILS - Most updates are lost

**3. Run the sequential test:**
```powershell
dotnet test --filter "FullyQualifiedName~UpdateSegmentStatus_SequentialUpdates"
```
**Expected:** ✅ TEST PASSES - Proves the logic is correct

### Your Challenge

Fix `JourneyService.UpdateSegmentStatusAsync()` in `FlyrTech.Infrastructure/JourneyService.cs` to handle concurrent updates without data loss.

**Success Criteria:**
All 20 concurrent updates must succeed without data loss.

---

## Solution Structure

```
FlyrTech/
├── FlyrTech.Core/                  # Domain layer
│   ├── ICacheService.cs            # Cache interface
│   ├── IJourneyService.cs          # Journey interface
│   └── Models/Journey.cs           # Domain model
├── FlyrTech.Infrastructure/        # Infrastructure
│   ├── RedisCacheService.cs        # Redis implementation
│   └── JourneyService.cs           # ⚠️ Has race condition
├── FlyrTech.Api/                   # Web API
│   ├── Program.cs                  # DI & initialization
│   └── Data/journeys.json          # Sample data
└── FlyrTech.Tests/                 # Tests
    └── JourneyRaceConditionTests.cs # Demonstrates problem
```

## Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Redis** - See installation below

## Redis Installation

### Windows (Docker - Recommended)
```powershell
# Install Docker Desktop: https://www.docker.com/products/docker-desktop
docker run -d -p 6379:6379 --name redis-flyrtech redis:latest

# Verify
docker ps
redis-cli ping  # Should respond: PONG
```

### Windows (WSL2)
```powershell
wsl --install
# Inside WSL:
sudo apt update && sudo apt install redis-server
sudo service redis-server start
redis-cli ping
```

### macOS
```bash
brew install redis
brew services start redis
redis-cli ping
```

### Linux (Ubuntu/Debian)
```bash
sudo apt update && sudo apt install redis-server
sudo systemctl start redis-server
redis-cli ping
```

## Building & Running

**Build:**
```powershell
dotnet build FlyrTech.sln
```

**Run Tests:**
```powershell
dotnet test FlyrTech.Tests/FlyrTech.Tests.csproj
```

**Expected Results:**
- ✅ 22 tests pass
- ❌ 1 test fails (race condition demo)

**Run API:**
```powershell
cd FlyrTech.Api
dotnet run
```
- Swagger UI: `https://localhost:5001/swagger`
- On startup, loads 3 journeys into cache

## API Endpoints

### Journeys

**GET /api/journeys** - List all journey IDs

**GET /api/journeys/{id}** - Get journey details

**PUT /api/journeys/{id}/segments/{segmentId}/status** - Update segment (⚠️ race condition)
```json
{ "status": "Departed" }
```

**PUT /api/journeys/{id}/status** - Update journey status
```json
{ "status": "InProgress" }
```

### Cache

**GET /api/cache/demo** - Demo endpoint with 60s TTL

**GET /api/cache/{key}** - Get value

**POST /api/cache/{key}** - Set value

**DELETE /api/cache/{key}** - Remove value

## Testing Strategy

All tests require **running Redis** on `localhost:6379`.

**Test Categories:**
1. **RedisCacheServiceTests** (21 tests) - Cache operations
2. **CacheEndpointTests** (1 test) - API behavior
3. **JourneyRaceConditionTests** (2 tests):
   - ❌ `UpdateSegmentStatus_With20ConcurrentUpdates` - FAILS (intentional)
   - ✅ `UpdateSegmentStatus_SequentialUpdates` - PASSES

## Configuration

`FlyrTech.Api/appsettings.json`:
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

For remote Redis:
```json
{
  "Redis": {
    "ConnectionString": "host:port,password=secret"
  }
}
```

## Architecture

**Clean Architecture with DI:**
- `ICacheService` → `RedisCacheService` (Singleton)
- `IJourneyService` → `JourneyService` (Singleton)
- `IConnectionMultiplexer` → Redis connection pool (Singleton)

**Data Flow:**
1. API starts → Loads `Data/journeys.json`
2. Initializes Redis cache with 3 journeys
3. Endpoints use `IJourneyService`
4. Service uses `ICacheService` (Redis)

## Troubleshooting

**Redis Connection Error:**
```
RedisConnectionException: It was not possible to connect...
```

**Solutions:**
1. Check Redis is running: `redis-cli ping` → PONG
2. Check port: `netstat -an | findstr "6379"` (Windows) or `grep 6379` (Unix)
3. Docker status: `docker ps` and `docker logs redis-flyrtech`
4. Clear data: `redis-cli FLUSHALL`

**Tests Failing Unexpectedly:**
1. Ensure Redis is running
2. Clear Redis: `redis-cli FLUSHALL`
3. Restart Redis service
4. Check logs

## Key Features

✅ Real Redis integration (not mocked)  
✅ Race condition demonstration for learning  
✅ Clean architecture & DI  
✅ Journey data auto-initialization  
✅ 23 tests (22 pass, 1 intentional fail)  
✅ Swagger documentation  
✅ Async/await patterns  

## License

MIT
