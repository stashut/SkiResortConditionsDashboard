using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiResort.Api.Models;
using SkiResort.Api.Options;
using SkiResort.Api.Realtime;
using SkiResort.Domain.Entities;
using SkiResort.Infrastructure.Data;

namespace SkiResort.Api.Workers;

/// <summary>
/// Background worker that ingests weather messages from SQS and persists them to PostgreSQL.
/// </summary>
public sealed class SqsIngestionWorker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<SqsIngestionWorker> _logger;
    private readonly IAmazonSQS _sqs;
    private readonly IServiceProvider _serviceProvider;
    private readonly SqsIngestionOptions _options;

    public SqsIngestionWorker(
        ILogger<SqsIngestionWorker> logger,
        IAmazonSQS sqs,
        IServiceProvider serviceProvider,
        IOptions<SqsIngestionOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sqs = sqs ?? throw new ArgumentNullException(nameof(sqs));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.QueueUrl))
        {
            _logger.LogWarning("SQS ingestion worker disabled because QueueUrl is not configured.");
            return;
        }

        _logger.LogInformation("Starting SQS ingestion worker for queue {QueueUrl}", _options.QueueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = await ReceiveMessagesAsync(stoppingToken).ConfigureAwait(false);

                if (messages.Count == 0)
                {
                    continue;
                }

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<SkiResortDbContext>();
                var notifier = scope.ServiceProvider.GetRequiredService<ResortUpdateNotifier>();

                foreach (var message in messages)
                {
                    await ProcessMessageAsync(message, dbContext, notifier, stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in SQS ingestion loop.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("SQS ingestion worker is stopping.");
    }

    private async Task<IReadOnlyList<Message>> ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        var request = new ReceiveMessageRequest
        {
            QueueUrl = _options.QueueUrl,
            MaxNumberOfMessages = _options.MaxMessages,
            WaitTimeSeconds = _options.WaitTimeSeconds
        };

        var response = await _sqs.ReceiveMessageAsync(request, cancellationToken).ConfigureAwait(false);
        return response.Messages;
    }

    private async Task ProcessMessageAsync(
        Message message,
        SkiResortDbContext dbContext,
        ResortUpdateNotifier notifier,
        CancellationToken cancellationToken)
    {
        try
        {
            var ingestion = JsonSerializer.Deserialize<WeatherIngestionMessage>(message.Body, SerializerOptions);
            if (ingestion is null || ingestion.ResortId == Guid.Empty)
            {
                _logger.LogWarning("Skipping message with invalid payload: {ReceiptHandle}", message.ReceiptHandle);
                await DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            }

            var exists = await dbContext.Resorts
                .AsNoTracking()
                .AnyAsync(r => r.Id == ingestion.ResortId, cancellationToken)
                .ConfigureAwait(false);

            if (!exists)
            {
                _logger.LogWarning("Skipping message for unknown resort {ResortId}.", ingestion.ResortId);
                await DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
                return;
            }

            var condition = new SnowCondition
            {
                Id = Guid.NewGuid(),
                ResortId = ingestion.ResortId,
                ObservedAt = ingestion.ObservedAt == default ? DateTimeOffset.UtcNow : ingestion.ObservedAt,
                SnowDepthCm = ingestion.SnowDepthCm,
                NewSnowCm = ingestion.NewSnowCm
            };

            dbContext.SnowConditions.Add(condition);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await notifier.NotifyResortConditionsUpdatedAsync(condition.ResortId, cancellationToken)
                .ConfigureAwait(false);

            await DeleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SQS message {ReceiptHandle}", message.ReceiptHandle);
        }
    }

    private Task DeleteMessageAsync(Message message, CancellationToken cancellationToken)
    {
        return _sqs.DeleteMessageAsync(_options.QueueUrl, message.ReceiptHandle, cancellationToken);
    }
}

