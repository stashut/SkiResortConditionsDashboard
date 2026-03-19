using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SkiResort.Api.Observability;

// Criterion 8: Application metrics and traces for CloudWatch via OpenTelemetry
public static class ObservabilityConstants
{
    public const string ActivitySourceName = "SkiResort.Api";
    public const string MeterName = "SkiResort.Api.Metrics";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> SqsMessagesProcessed =
        Meter.CreateCounter<long>("sqs_messages_processed_total");

    public static readonly Histogram<double> SqsProcessingDurationMs =
        Meter.CreateHistogram<double>("sqs_processing_duration_ms");

    public static readonly UpDownCounter<long> ActiveSignalRConnections =
        Meter.CreateUpDownCounter<long>("signalr_active_connections");
}

