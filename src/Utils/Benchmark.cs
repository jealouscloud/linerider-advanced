using System;
using System.Diagnostics;

/// <summary>
/// Simple static class for easy benchmarking
/// </summary>
public static class Benchmark
{
    private static Stopwatch _stopwatch;
    private static string _stopwatchstring;
    public static TimeSpan BenchmarkFunction(Action function, string name)
    {
        var sw = Stopwatch.StartNew();
        function();
        sw.Stop();
        Debug.WriteLine(sw.Elapsed);
        return sw.Elapsed;
    }

    public static void Start()
    {
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
        _stopwatchstring = null;
    }
    public static void Start(string name)
    {
        _stopwatchstring = name;
        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }
    public static TimeSpan Stop()
    {
        _stopwatch.Stop();
        if (string.IsNullOrEmpty(_stopwatchstring))
            Debug.WriteLine(_stopwatch.Elapsed);
        else
            Debug.WriteLine(_stopwatchstring + " elapsed: " + _stopwatch.Elapsed);
        _stopwatchstring = null;
        return _stopwatch.Elapsed;
    }
}