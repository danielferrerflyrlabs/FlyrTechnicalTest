# FlyrTech - Technical Challenge

A .NET 8 solution with an intentional race condition for you to analyze and fix.

## 📋 Challenge Overview

This is a **3-5 hour technical assessment** with two main components.

**💡 IMPORTANT:** We care more about **how you think** about the problem and your solution design than the implementation itself. We want to see your analytical process, how you evaluate trade-offs, and how you communicate your design decisions.

### Part 1: Technical Analysis (3-4 hours)
Complete the `TECHNICAL_ANALYSIS.md` document with your in-depth analysis of the race condition problem and proposed solutions.

### Part 2: Implementation (45-60 minutes)
Implement one of your proposed solutions to fix the race condition in `JourneyService.UpdateSegmentStatusAsync()`.

## 🚀 Getting Started

**1. Fork this repository to your own GitHub account:**
- Click the "Fork" button at the top right of this repository
- Clone your forked repository to your local machine:
```powershell
git clone https://github.com/YOUR_USERNAME/FlyrTechnicalTest.git
cd FlyrTechnicalTest
```

**2. Read the full challenge details:**
See [RACE_CONDITION_CHALLENGE.md](RACE_CONDITION_CHALLENGE.md) for the complete problem description.

**2. Start Redis:**
```powershell
docker run -d -p 6379:6379 --name redis-flyrtech redis:latest
```

**3. Verify the problem exists:**
```powershell
dotnet test --filter "FullyQualifiedName~UpdateSegmentStatus_With20ConcurrentUpdates"
```
**Expected:** ❌ TEST FAILS - Updates are lost due to race condition

**4. Complete your technical analysis:**
Fill out `TECHNICAL_ANALYSIS.md` with your analysis and proposed solutions.

**5. Implement your solution:**
Fix the code to make all tests pass.

**6. Verify your solution works:**
```powershell
dotnet test
```
**Expected:** ✅ ALL 23 tests pass

**7. Commit and push your changes:**
```powershell
git add .
git commit -m "Fix race condition in JourneyService"
git push origin main
```

## 📊 Deliverables

**Submit your work by sending us the link to your forked repository.**

Your repository should include:
1. **TECHNICAL_ANALYSIS.md** - Completed with your detailed analysis
2. **Code changes** - Your implementation to fix the race condition
3. **All tests passing** - Including the concurrent updates test
4. **Clean commit history** - Well-organized commits with clear messages

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

## 📚 Additional Documentation

- **[RACE_CONDITION_CHALLENGE.md](RACE_CONDITION_CHALLENGE.md)** - Detailed problem description and success criteria
- **[TECHNICAL_ANALYSIS.md](TECHNICAL_ANALYSIS.md)** - Template for your technical analysis (to be completed)

## Key Features

✅ Real Redis integration (not mocked)  
✅ Race condition demonstration for learning  
✅ Clean architecture & DI  
✅ Journey data auto-initialization  
✅ 23 tests (22 pass, 1 intentional fail before fix)  
✅ Swagger documentation  
✅ Async/await patterns  

## License

MIT
