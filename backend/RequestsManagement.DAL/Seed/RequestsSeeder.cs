using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequestsManagement.DAL.Entities;

namespace RequestsManagement.DAL.Seed;

/// <summary>
/// Creates a large, deterministic set of test requests.
/// Inserts in chunks through EF Core: each chunk needs little memory,
/// so seeding works reliably even on LocalDB with limited resources.
/// (SqlBulkCopy was tried first, but on LocalDB it waited indefinitely
/// for a memory grant - RESOURCE_SEMAPHORE - because of the index sorts.)
/// </summary>
public class RequestsSeeder
{
    // Fixed values make every run produce exactly the same data,
    // so performance measurements and tests are reproducible.
    private const int RandomSeed = 20261102;
    private const int ChunkSize = 5_000;
    private const int DaysBack = 730;
    private static readonly DateTime BaseDate = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly string[] Subjects =
    {
        "בקשה להיתר פליטה", "דיווח על חריגה בזיהום אוויר", "בקשה לרישיון עסק",
        "תלונה על מפגע ריח", "דיווח על פסולת מסוכנת", "בקשה לאישור טיפול בשפכים",
        "דיווח על דליפת דלק", "בקשה להיתר רעלים", "תלונה על רעש",
        "בקשה לפטור מהיטל הטמנה", "דיווח שנתי על פליטות", "בקשה לאישור תוכנית מחזור"
    };

    private static readonly string[] Locations =
    {
        "חיפה", "אשדוד", "באר שבע", "נתניה", "קריית גת",
        "חדרה", "רמת חובב", "אילת", "פתח תקווה", "כרמיאל"
    };

    private static readonly string[] OrganizationPrefixes =
    {
        "מפעלי", "חברת", "תעשיות", "מחלבת", "מוסך", "קבוצת", "מרכז מחזור", "מפעל"
    };

    private static readonly string[] OrganizationNames =
    {
        "השרון", "הגליל", "הנגב", "הכרמל", "גלבוע", "ארבל", "תבור", "הירדן", "אורן",
        "אלון", "ברוש", "דקל", "זית", "רימון", "תמר", "גפן", "שקד", "אלה", "ערבה",
        "יזרעאל", "עמק", "חרמון", "מירון", "כנרת", "ים המלח"
    };

    private static readonly string[] Assignees =
    {
        "david.levi", "sara.cohen", "moshe.mizrahi", "rachel.friedman", "yosef.katz",
        "leah.shapiro", "avi.biton", "miriam.peretz", "shlomo.azulay", "esther.golan",
        "yaakov.ben-david", "chana.rosen"
    };

    private readonly RequestsDbContext _context;
    private readonly ILogger<RequestsSeeder> _logger;

    public RequestsSeeder(RequestsDbContext context, ILogger<RequestsSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(int count, bool reset, CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        if (reset)
        {
            _logger.LogInformation("Reset requested. Deleting existing requests and history.");
            await _context.RequestStatusHistory.ExecuteDeleteAsync(cancellationToken);
            await _context.Requests.ExecuteDeleteAsync(cancellationToken);
        }
        else if (await _context.Requests.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Requests table already contains data. Seeding skipped.");
            return;
        }

        _logger.LogInformation("Seeding {Count} requests...", count);

        var random = new Random(RandomSeed);
        var created = 0;

        while (created < count)
        {
            var currentChunkSize = Math.Min(ChunkSize, count - created);
            var chunk = new List<Request>(currentChunkSize);

            for (var i = 0; i < currentChunkSize; i++)
            {
                chunk.Add(CreateRequest(random));
            }

            _context.Requests.AddRange(chunk);
            await _context.SaveChangesAsync(cancellationToken);

            // Release tracked entities so memory stays flat across chunks.
            _context.ChangeTracker.Clear();

            created += currentChunkSize;
            _logger.LogInformation("Seeded {Created}/{Count} requests.", created, count);
        }

        _logger.LogInformation("Seeding completed: {Count} requests created.", count);
    }

    private static Request CreateRequest(Random random)
    {
        var status = PickStatus(random);
        var priority = PickPriority(random);

        var createdAt = BaseDate.AddMinutes(-random.Next(0, DaysBack * 24 * 60));
        var updatedAt = status == RequestStatus.New
            ? createdAt
            : Min(createdAt.AddMinutes(random.Next(1, 30 * 24 * 60)), BaseDate);

        // Most new requests are not assigned yet.
        var assignedTo = status == RequestStatus.New && random.Next(100) < 60
            ? null
            : Assignees[random.Next(Assignees.Length)];

        return new Request
        {
            Title = $"{Subjects[random.Next(Subjects.Length)]} - {Locations[random.Next(Locations.Length)]}",
            OrganizationName = $"{OrganizationPrefixes[random.Next(OrganizationPrefixes.Length)]} {OrganizationNames[random.Next(OrganizationNames.Length)]}",
            Status = status,
            Priority = priority,
            AssignedTo = assignedTo,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    private static RequestStatus PickStatus(Random random)
    {
        var value = random.Next(100);
        return value switch
        {
            < 25 => RequestStatus.New,
            < 55 => RequestStatus.InProgress,
            < 70 => RequestStatus.Waiting,
            _ => RequestStatus.Completed
        };
    }

    private static RequestPriority PickPriority(Random random)
    {
        var value = random.Next(100);
        return value switch
        {
            < 40 => RequestPriority.Low,
            < 80 => RequestPriority.Medium,
            _ => RequestPriority.High
        };
    }

    private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;
}