using System.Net.Http.Headers;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace ptr727.LanguageTags.Create;

internal static class HttpClientFactory
{
    // Retry
    private const int RetryMaxAttempts = 3;
    private static readonly TimeSpan s_retryBaseDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan s_retryMaxDelay = TimeSpan.FromSeconds(30);

    // Circuit breaker
    private const double CircuitBreakerFailureRatio = 0.1;
    private const int CircuitBreakerMinimumThroughput = 10;
    private static readonly TimeSpan s_circuitBreakerSamplingDuration = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan s_circuitBreakerBreakDuration = TimeSpan.FromSeconds(30);

    // Connection pool
    private static readonly TimeSpan s_connectionLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan s_connectionIdleTimeout = TimeSpan.FromMinutes(2);

    // HttpClient
    private static readonly TimeSpan s_httpClientTimeout = TimeSpan.FromSeconds(120);

    private static readonly Lazy<HttpClient> s_httpClient = new(CreateHttpClient);

    // Returns the shared singleton HttpClient; all callers share the connection pool and circuit breaker state.
    internal static HttpClient GetHttpClient() => s_httpClient.Value;

    private static ResilienceHandler CreateResilienceHandler() =>
        new(
            new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddRetry(
                    new RetryStrategyOptions<HttpResponseMessage>
                    {
                        MaxRetryAttempts = RetryMaxAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        UseJitter = true,
                        Delay = s_retryBaseDelay,
                        MaxDelay = s_retryMaxDelay,
                        ShouldHandle = args =>
                            ValueTask.FromResult(IsTransientFailure(args.Outcome)),
                        OnRetry = args =>
                        {
                            Log.Logger.Warning(
                                "HTTP retry attempt {Attempt} after {Delay}ms: {Outcome}",
                                args.AttemptNumber,
                                args.RetryDelay.TotalMilliseconds,
                                args.Outcome
                            );
                            return ValueTask.CompletedTask;
                        },
                    }
                )
                .AddCircuitBreaker(
                    new CircuitBreakerStrategyOptions<HttpResponseMessage>
                    {
                        FailureRatio = CircuitBreakerFailureRatio,
                        MinimumThroughput = CircuitBreakerMinimumThroughput,
                        SamplingDuration = s_circuitBreakerSamplingDuration,
                        BreakDuration = s_circuitBreakerBreakDuration,
                        ShouldHandle = args =>
                            ValueTask.FromResult(IsTransientFailure(args.Outcome)),
                        OnOpened = args =>
                        {
                            Log.Logger.Warning(
                                "Circuit breaker opened for {Duration}s: {Outcome}",
                                args.BreakDuration.TotalSeconds,
                                args.Outcome
                            );
                            return ValueTask.CompletedTask;
                        },
                        OnClosed = _ =>
                        {
                            Log.Logger.Information("Circuit breaker closed.");
                            return ValueTask.CompletedTask;
                        },
                        OnHalfOpened = _ =>
                        {
                            Log.Logger.Debug("Circuit breaker half-opened.");
                            return ValueTask.CompletedTask;
                        },
                    }
                )
                .Build()
        )
        {
            InnerHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = s_connectionLifetime,
                PooledConnectionIdleTimeout = s_connectionIdleTimeout,
                AutomaticDecompression = System.Net.DecompressionMethods.All,
            },
        };

    private static bool IsTransientFailure(Outcome<HttpResponseMessage> outcome) =>
        outcome.Exception is not null
            ? outcome.Exception is not (OperationCanceledException or BrokenCircuitException)
            : outcome.Result is not null && (int)outcome.Result.StatusCode is 408 or 429 or >= 500;

    // Creates a new HttpClient instance; each caller gets an independent resilience handler
    // and circuit breaker state. Callers should store and reuse the returned instance.
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "HttpClient takes ownership of the handler and disposes it when the client is disposed."
    )]
    internal static HttpClient CreateHttpClient()
    {
        HttpClient httpClient = new(CreateResilienceHandler()) { Timeout = s_httpClientTimeout };
        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(AssemblyInfo.AppName, AssemblyInfo.ReleaseVersion)
        );
        return httpClient;
    }
}
