# Race Condition Challenge - Journey Updates

## Problem Description

The current implementation of `JourneyService.UpdateSegmentStatusAsync()` has a **race condition** that causes data loss when multiple concurrent updates are executed.

## The Issue

The method follows a **read-modify-write** pattern:
1. **Read** the entire journey from cache
2. **Modify** a specific segment
3. **Write** the entire journey back to cache

When 20 updates execute concurrently:
- Thread 1 reads journey (all segments are "Scheduled")
- Thread 2 reads journey (all segments are "Scheduled")
- Thread 1 modifies segment A and writes back
- Thread 2 modifies segment B and writes back **overwriting Thread 1's changes**

## Evidence

Run this test to see the race condition:

```powershell
dotnet test FlyrTech.Tests\FlyrTech.Tests.csproj --filter "FullyQualifiedName~UpdateSegmentStatus_With20ConcurrentUpdates"
```

**Expected Result:** FAIL - Multiple segment updates are lost due to race conditions

Run this test to verify sequential updates work:

```powershell
dotnet test FlyrTech.Tests\FlyrTech.Tests.csproj --filter "FullyQualifiedName~UpdateSegmentStatus_SequentialUpdates"
```

**Expected Result:** PASS - All updates are preserved when executed sequentially

## Your Challenge

Fix the `UpdateSegmentStatusAsync` method in `FlyrTech.Infrastructure/JourneyService.cs` to handle concurrent updates correctly.

### Possible Solutions

1. **Fine-grained locking**: Use Redis locks to ensure only one thread updates at a time
2. **Atomic operations**: Store each segment independently in Redis with its own key
3. **Optimistic locking**: Use versioning/timestamps to detect conflicts and retry
4. **Redis transactions**: Use WATCH/MULTI/EXEC to ensure atomic updates
5. **Lua scripts**: Execute the read-modify-write as an atomic operation in Redis

### Success Criteria

After your fix, this test should pass:

```csharp
UpdateSegmentStatus_With20ConcurrentUpdates_ShouldHaveRaceConditionIssues
```

All 20 concurrent segment updates must be successfully applied without data loss.

## Files to Modify

- `FlyrTech.Infrastructure/JourneyService.cs` - The main implementation
- Optionally: `FlyrTech.Core/ICacheService.cs` - If you need additional cache operations

## Testing Your Solution

```powershell
# Run all tests
dotnet test FlyrTech.Tests\FlyrTech.Tests.csproj

# Run only the race condition tests
dotnet test --filter "FullyQualifiedName~JourneyRaceConditionTests"
```

Good luck! 🚀
