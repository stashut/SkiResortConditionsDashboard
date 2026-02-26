using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace SkiResort.Infrastructure.Reports;

public sealed class SnowComparisonRow
{
    public Guid ResortId { get; init; }
    public string ResortName { get; init; } = string.Empty;
    public DateTimeOffset ObservedAt { get; init; }
    public decimal SnowDepthCm { get; init; }
    public decimal NewSnowCm { get; init; }
}

public interface ISnowComparisonReportRepository
{
    Task<IReadOnlyList<SnowComparisonRow>> GetSnowComparisonAsync(
        IReadOnlyCollection<Guid> resortIds,
        CancellationToken cancellationToken = default);
}

public sealed class SnowComparisonReportRepository : ISnowComparisonReportRepository
{
    private readonly string _connectionString;

    public SnowComparisonReportRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<IReadOnlyList<SnowComparisonRow>> GetSnowComparisonAsync(
        IReadOnlyCollection<Guid> resortIds,
        CancellationToken cancellationToken = default)
    {
        if (resortIds == null || resortIds.Count == 0)
        {
            return Array.Empty<SnowComparisonRow>();
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var fromInclusive = DateTimeOffset.UtcNow.AddDays(-7);

        const string sql = @"
select
    r.id as ""ResortId"",
    r.name as ""ResortName"",
    sc.observedat as ""ObservedAt"",
    sc.snowdepthcm as ""SnowDepthCm"",
    sc.newsnowcm as ""NewSnowCm""
from snowconditions sc
join resorts r on sc.resortid = r.id
where sc.observedat >= @FromInclusive
  and sc.resortid = any(@ResortIds)
order by r.name, sc.observedat;
";

        var command = new CommandDefinition(
            sql,
            new
            {
                ResortIds = resortIds.ToArray(),
                FromInclusive = fromInclusive
            },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<SnowComparisonRow>(command);
        return rows.ToList();
    }
}

