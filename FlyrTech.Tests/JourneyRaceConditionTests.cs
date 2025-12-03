using FlyrTech.Core;
using FlyrTech.Core.Models;
using FlyrTech.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace FlyrTech.Tests;

/// <summary>
/// Tests demonstrating race condition issues with concurrent journey updates
/// This test requires a running Redis instance on localhost:6379
/// </summary>
public class JourneyRaceConditionTests
{
    private readonly ICacheService _cacheService;
    private readonly IJourneyService _journeyService;

    public JourneyRaceConditionTests()
    {
        // Connect to Redis for testing
        var redis = ConnectionMultiplexer.Connect("localhost:6379");
        _cacheService = new RedisCacheService(redis);
        _journeyService = new JourneyService(_cacheService);
    }

    [Fact]
    public async Task UpdateSegmentStatus_With20ConcurrentUpdates_ShouldHaveRaceConditionIssues()
    {
        // Arrange
        var journey = CreateTestJourney();
        var journeyService = _journeyService;
        
        // Initialize cache with test journey
        await journeyService.InitializeCacheAsync(new List<Journey> { journey });

        // Define 20 different segment IDs and their target statuses
        var updates = new List<(string segmentId, string status)>
        {
            ("SEG-001", "Departed"),
            ("SEG-002", "Boarding"),
            ("SEG-003", "Delayed"),
            ("SEG-004", "Cancelled"),
            ("SEG-005", "Departed"),
            ("SEG-006", "Boarding"),
            ("SEG-007", "Delayed"),
            ("SEG-008", "Cancelled"),
            ("SEG-009", "Departed"),
            ("SEG-010", "Boarding"),
            ("SEG-011", "Delayed"),
            ("SEG-012", "Cancelled"),
            ("SEG-013", "Departed"),
            ("SEG-014", "Boarding"),
            ("SEG-015", "Delayed"),
            ("SEG-016", "Cancelled"),
            ("SEG-017", "Departed"),
            ("SEG-018", "Boarding"),
            ("SEG-019", "Delayed"),
            ("SEG-020", "Cancelled")
        };

        // Act - Execute 20 concurrent updates
        var tasks = updates.Select(update => 
            journeyService.UpdateSegmentStatusAsync(journey.Id, update.segmentId, update.status)
        ).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Verify all updates were successful
        var results = await Task.WhenAll(tasks);
        var allSuccessful = results.All(r => r);
        Assert.True(allSuccessful, "All update operations should return true");

        // Get the final journey state
        var finalJourney = await journeyService.GetJourneyAsync(journey.Id);
        Assert.NotNull(finalJourney);

        // Verify each segment has its expected status
        var failures = new List<string>();
        foreach (var (segmentId, expectedStatus) in updates)
        {
            var segment = finalJourney.Segments.FirstOrDefault(s => s.SegmentId == segmentId);
            Assert.NotNull(segment);

            if (segment.Status != expectedStatus)
            {
                failures.Add($"Segment {segmentId}: expected '{expectedStatus}', got '{segment.Status}'");
            }
        }

        // This assertion will likely FAIL due to race conditions
        // Some updates will be lost because of the read-modify-write pattern
        Assert.Empty(failures);
        
        // If we get here without failures, print success message
        if (failures.Count == 0)
        {
            // This is unlikely with the current implementation
            Assert.True(true, "All 20 concurrent updates were applied correctly (race condition was fixed!)");
        }
        else
        {
            // Expected failure - document the race condition
            var errorMessage = $"Race condition detected! {failures.Count} out of 20 updates were lost:\n" +
                             string.Join("\n", failures);
            Assert.Fail(errorMessage);
        }
    }

    [Fact]
    public async Task UpdateSegmentStatus_SequentialUpdates_ShouldWorkCorrectly()
    {
        // Arrange
        var journey = CreateTestJourney();
        var journeyService = _journeyService;
        
        await journeyService.InitializeCacheAsync(new List<Journey> { journey });

        // Act - Execute updates sequentially (no race condition)
        for (int i = 1; i <= 20; i++)
        {
            var segmentId = $"SEG-{i:D3}";
            var status = i % 4 == 0 ? "Cancelled" : i % 3 == 0 ? "Delayed" : i % 2 == 0 ? "Boarding" : "Departed";
            await journeyService.UpdateSegmentStatusAsync(journey.Id, segmentId, status);
        }

        // Assert - All updates should be present
        var finalJourney = await journeyService.GetJourneyAsync(journey.Id);
        Assert.NotNull(finalJourney);

        for (int i = 1; i <= 20; i++)
        {
            var segmentId = $"SEG-{i:D3}";
            var expectedStatus = i % 4 == 0 ? "Cancelled" : i % 3 == 0 ? "Delayed" : i % 2 == 0 ? "Boarding" : "Departed";
            var segment = finalJourney.Segments.First(s => s.SegmentId == segmentId);
            Assert.Equal(expectedStatus, segment.Status);
        }
    }

    private Journey CreateTestJourney()
    {
        var segments = new List<Segment>();
        
        // Create 20 segments
        for (int i = 1; i <= 20; i++)
        {
            segments.Add(new Segment
            {
                SegmentId = $"SEG-{i:D3}",
                Origin = "MAD",
                Destination = "BCN",
                DepartureTime = DateTime.UtcNow.AddDays(i),
                ArrivalTime = DateTime.UtcNow.AddDays(i).AddHours(2),
                FlightNumber = $"IB{3000 + i}",
                Carrier = "Iberia",
                Status = "Scheduled",
                Price = 100.00m + i
            });
        }

        return new Journey
        {
            Id = "JRN-TEST-001",
            PassengerName = "Test User",
            PassengerEmail = "test@test.com",
            BookingDate = DateTime.UtcNow,
            Status = "Confirmed",
            TotalPrice = segments.Sum(s => s.Price),
            Segments = segments,
            Metadata = new Dictionary<string, string>
            {
                { "testRun", "true" }
            }
        };
    }
}
