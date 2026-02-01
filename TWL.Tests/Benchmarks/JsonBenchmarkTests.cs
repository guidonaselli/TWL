using System.Diagnostics;
using System.Text;
using System.Text.Json;
using TWL.Shared.Net.Messages;
using TWL.Shared.Net.Network;
using Xunit.Abstractions;

namespace TWL.Tests.Benchmarks;

public class JsonBenchmarkTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ITestOutputHelper _output;

    public JsonBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void BenchmarkDeserialization()
    {
        // Setup payload
        var message = new ServerMessage
        {
            MessageType = ServerMessageType.CombatResult,
            Payload = "{\"targetId\":123,\"damage\":50,\"isCrit\":true}"
        };
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
        var iterations = 100_000;

        _output.WriteLine($"Payload size: {jsonBytes.Length} bytes");
        _output.WriteLine($"Iterations: {iterations}");

        // Warmup
        DeserializeBad(jsonBytes);
        DeserializeCurrent(jsonBytes);
        DeserializeSourceGen(jsonBytes);

        // Benchmark Bad
        var beforeBad = GC.GetTotalAllocatedBytes();
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            DeserializeBad(jsonBytes);
        }

        sw.Stop();
        var afterBad = GC.GetTotalAllocatedBytes();
        var badTime = sw.ElapsedMilliseconds;
        var badAlloc = afterBad - beforeBad;
        _output.WriteLine($"Bad (String+Deserialize): {badTime} ms, {badAlloc / 1024 / 1024} MB allocated");

        // Benchmark Current
        var beforeCurrent = GC.GetTotalAllocatedBytes();
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            DeserializeCurrent(jsonBytes);
        }

        sw.Stop();
        var afterCurrent = GC.GetTotalAllocatedBytes();
        var currentTime = sw.ElapsedMilliseconds;
        var currentAlloc = afterCurrent - beforeCurrent;
        _output.WriteLine($"Current (Span+Deserialize): {currentTime} ms, {currentAlloc / 1024 / 1024} MB allocated");

        // Benchmark SourceGen
        var beforeSourceGen = GC.GetTotalAllocatedBytes();
        sw.Restart();
        for (var i = 0; i < iterations; i++)
        {
            DeserializeSourceGen(jsonBytes);
        }

        sw.Stop();
        var afterSourceGen = GC.GetTotalAllocatedBytes();
        var sourceGenTime = sw.ElapsedMilliseconds;
        var sourceGenAlloc = afterSourceGen - beforeSourceGen;
        _output.WriteLine($"SourceGen (Context): {sourceGenTime} ms, {sourceGenAlloc / 1024 / 1024} MB allocated");

        // Comparison
        if (badTime > 0)
        {
            var currentImprovement = (double)(badTime - currentTime) / badTime * 100;
            _output.WriteLine($"Current vs Bad Improvement: {currentImprovement:F2}%");

            var sourceGenImprovement = (double)(badTime - sourceGenTime) / badTime * 100;
            _output.WriteLine($"SourceGen vs Bad Improvement: {sourceGenImprovement:F2}%");

            if (currentTime > 0)
            {
                var sourceGenVsCurrentImprovement = (double)(currentTime - sourceGenTime) / currentTime * 100;
                _output.WriteLine($"SourceGen vs Current Improvement: {sourceGenVsCurrentImprovement:F2}%");
            }
        }
    }

    private ServerMessage? DeserializeBad(byte[] buffer)
    {
        // Simulate the bad code: Encoding.UTF8.GetString + Deserialize<T>(string)
        var json = Encoding.UTF8.GetString(buffer);
        return JsonSerializer.Deserialize<ServerMessage>(json, _jsonOptions);
    }

    private ServerMessage? DeserializeCurrent(byte[] buffer)
    {
        // Current optimized code: Deserialize<T>(ReadOnlySpan<byte>)
        return JsonSerializer.Deserialize<ServerMessage>(new ReadOnlySpan<byte>(buffer), _jsonOptions);
    }

    private ServerMessage? DeserializeSourceGen(byte[] buffer)
    {
        // Future optimized code: Deserialize<T> using Source Gen Context
        return JsonSerializer.Deserialize(new ReadOnlySpan<byte>(buffer), AppJsonContext.Default.ServerMessage);
    }
}