using System.Text;

namespace Stress.Tests.Runner;

public static class HtmlReportWriter
{
    public static string Write(
        IReadOnlyList<ScenarioResult> results,
        ResourceMonitor monitor,
        string folder)
    {
        Directory.CreateDirectory(folder);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path      = Path.Combine(folder, $"report_{timestamp}.html");

        var samples = monitor.Samples;
        var markers = monitor.Markers;

        var scenarioRows = string.Join("\n", results.Select(r =>
            $"""
            <tr>
              <td>{r.Name}</td>
              <td>{r.Total:N0}</td>
              <td class="ok">{r.Success:N0} <span class="pct">({r.Success * 100.0 / r.Total:F1}%)</span></td>
              <td class="warn">{r.NotFound:N0} <span class="pct">({r.NotFound * 100.0 / r.Total:F1}%)</span></td>
              <td class="{(r.Failed > 0 ? "fail" : "ok")}">{r.Failed:N0} <span class="pct">({r.Failed * 100.0 / r.Total:F1}%)</span></td>
              <td>{r.Throughput:F1}</td>
              <td>{r.Min}</td>
              <td>{r.P50}</td>
              <td>{r.P95}</td>
              <td>{r.P99}</td>
              <td>{r.Max}</td>
            </tr>
            """));

        var cpuChart = BuildChart("CPU Usage", "%", samples, markers,
        [
            ("CPU %", "#e74c3c", s => s.CpuPercent),
        ]);

        var memChart = BuildChart("Memory", "MB", samples, markers,
        [
            ("Working Set", "#3498db", s => (double)s.WorkingSetMb),
            ("GC Heap",     "#9b59b6", s => (double)s.GcHeapMb),
        ]);

        var threadChart = BuildChart("Thread Pool", "count", samples, markers,
        [
            ("OS Threads",    "#e67e22", s => (double)s.ThreadPoolThreadCount),
            ("Pending Items", "#e74c3c", s => (double)s.ThreadPoolPendingItems),
        ]);

        var dbChart = BuildChart("DB Connections (MySQL)", "connections", samples, markers,
        [
            ("Connected (pool)", "#1abc9c", s => (double)s.DbThreadsConnected),
            ("Running (active)", "#e74c3c", s => (double)s.DbThreadsRunning),
        ]);

        var html = $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8"/>
              <title>Stress Report — {{timestamp}}</title>
              <style>
                *    { box-sizing: border-box; }
                body { font-family: Segoe UI, Arial, sans-serif; background:#f0f2f5; padding:2rem; color:#2c3e50; }
                h1   { margin-bottom:.25rem; }
                h2   { margin:2rem 0 .75rem; font-size:1.1rem; color:#555; border-bottom:2px solid #ddd; padding-bottom:.3rem; }
                .meta{ color:#888; font-size:.85rem; margin-bottom:2rem; }
                table{ border-collapse:collapse; width:100%; background:#fff;
                       box-shadow:0 1px 4px rgba(0,0,0,.08); border-radius:6px; overflow:hidden; }
                th   { background:#2c3e50; color:#fff; padding:10px 14px; text-align:left; font-size:.82rem; }
                td   { padding:9px 14px; border-bottom:1px solid #eee; font-size:.88rem; }
                tr:last-child td { border-bottom:none; }
                tr:hover td { background:#f9f9f9; }
                .ok  { color:#27ae60; font-weight:600; }
                .warn{ color:#f39c12; font-weight:600; }
                .fail{ color:#e74c3c; font-weight:600; }
                .pct { font-weight:400; font-size:.8rem; color:#999; }
                .charts { display:grid; grid-template-columns:1fr 1fr; gap:1.25rem; margin-top:.5rem; }
                .chart-box { background:#fff; border-radius:6px; padding:1rem;
                              box-shadow:0 1px 4px rgba(0,0,0,.08); }
                .chart-title { font-weight:600; font-size:.9rem; margin-bottom:.25rem; }
                .chart-unit  { font-weight:400; color:#888; font-size:.8rem; }
                .legend { font-size:.78rem; margin-bottom:.4rem; }
                .legend span { margin-right:.75rem; }
              </style>
            </head>
            <body>
              <h1>Stress Test Report</h1>
              <p class="meta">Generated: {{DateTime.Now:yyyy-MM-dd HH:mm:ss}} &nbsp;|&nbsp; Scenarios: {{results.Count}} &nbsp;|&nbsp; Samples: {{samples.Count}}</p>

              <h2>Scenario Summary</h2>
              <table>
                <thead>
                  <tr>
                    <th>Scenario</th><th>Total</th><th>200 OK</th><th>404 NotFound</th><th>Errors</th>
                    <th>RPS</th><th>Min (ms)</th><th>P50 (ms)</th><th>P95 (ms)</th>
                    <th>P99 (ms)</th><th>Max (ms)</th>
                  </tr>
                </thead>
                <tbody>{{scenarioRows}}</tbody>
              </table>

              <h2>Resource Usage</h2>
              <div class="charts">
                {{cpuChart}}
                {{memChart}}
                {{threadChart}}
                {{dbChart}}
              </div>
            </body>
            </html>
            """;

        File.WriteAllText(path, html);
        return path;
    }

    // ── SVG chart builder ────────────────────────────────────────────────────

    private static string BuildChart(
        string title,
        string unit,
        IReadOnlyList<ResourceSample> samples,
        IReadOnlyList<(double Elapsed, string Name)> markers,
        IReadOnlyList<(string Label, string Color, Func<ResourceSample, double> Select)> series)
    {
        // SVG coordinate constants
        const int PL = 55, PR = 15, PT = 20, PB = 35;
        const int CW = 590, CH = 130;   // chart area width / height
        const int TW = PL + CW + PR;    // total SVG width
        const int TH = PT + CH + PB;    // total SVG height

        if (samples.Count == 0)
            return $"<div class=\"chart-box\"><div class=\"chart-title\">{title}</div><p style=\"color:#aaa\">No data</p></div>";

        var maxT  = samples[^1].ElapsedSeconds;
        var maxV  = series.SelectMany(s => samples.Select(r => s.Select(r))).Max();
        if (maxV  == 0) maxV = 1;

        double X(double t) => PL + t / maxT * CW;
        double Y(double v) => PT + CH - v / maxV * CH;

        var sb = new StringBuilder();

        // ── legend ───────────────────────────────────────────────────────────
        sb.Append("<div class=\"legend\">");
        foreach (var (label, color, _) in series)
            sb.Append($"<span style=\"color:{color};font-weight:600\">{label}</span>");
        sb.Append("</div>");

        // ── SVG open ─────────────────────────────────────────────────────────
        sb.Append($"<svg viewBox=\"0 0 {TW} {TH}\" xmlns=\"http://www.w3.org/2000/svg\" style=\"width:100%;display:block\">");

        // background
        sb.Append($"<rect x=\"{PL}\" y=\"{PT}\" width=\"{CW}\" height=\"{CH}\" fill=\"#fafafa\" stroke=\"#ddd\"/>");

        // ── Y grid lines & labels (5 intervals) ──────────────────────────────
        for (var i = 0; i <= 4; i++)
        {
            var v  = maxV * i / 4.0;
            var y  = Y(v);
            var lv = maxV >= 100 ? $"{v:F0}" : $"{v:F1}";
            sb.Append($"<line x1=\"{PL}\" y1=\"{y:F1}\" x2=\"{PL + CW}\" y2=\"{y:F1}\" stroke=\"#e8e8e8\" stroke-width=\"1\"/>");
            sb.Append($"<text x=\"{PL - 5}\" y=\"{y + 4:F1}\" text-anchor=\"end\" font-size=\"10\" fill=\"#888\">{lv}</text>");
        }

        // ── X axis labels (every 30 s or fewer) ──────────────────────────────
        var step = maxT <= 60 ? 15 : maxT <= 180 ? 30 : 60;
        for (var t = 0.0; t <= maxT + 0.5; t += step)
        {
            var x = X(Math.Min(t, maxT));
            sb.Append($"<text x=\"{x:F1}\" y=\"{PT + CH + 16}\" text-anchor=\"middle\" font-size=\"10\" fill=\"#888\">{t:F0}s</text>");
        }

        // Y axis unit label (rotated)
        sb.Append($"<text x=\"{PL - 40}\" y=\"{PT + CH / 2}\" " +
                  $"transform=\"rotate(-90,{PL - 40},{PT + CH / 2})\" " +
                  $"text-anchor=\"middle\" font-size=\"10\" fill=\"#aaa\">{unit}</text>");

        // ── Scenario boundary markers ────────────────────────────────────────
        foreach (var (elapsed, name) in markers)
        {
            if (elapsed > maxT) continue;
            var x = X(elapsed);
            sb.Append($"<line x1=\"{x:F1}\" y1=\"{PT}\" x2=\"{x:F1}\" y2=\"{PT + CH}\" " +
                      $"stroke=\"#f39c12\" stroke-width=\"1\" stroke-dasharray=\"4,3\"/>");
            sb.Append($"<text x=\"{x + 3:F1}\" y=\"{PT + 11}\" font-size=\"8\" fill=\"#f39c12\">{name}</text>");
        }

        // ── Data polylines ───────────────────────────────────────────────────
        foreach (var (_, color, select) in series)
        {
            var points = string.Join(" ", samples.Select(s =>
                $"{X(s.ElapsedSeconds):F1},{Y(select(s)):F1}"));
            sb.Append($"<polyline points=\"{points}\" fill=\"none\" stroke=\"{color}\" stroke-width=\"2\" stroke-linejoin=\"round\"/>");
        }

        sb.Append("</svg>");

        return $$"""
            <div class="chart-box">
              <div class="chart-title">{{title}} <span class="chart-unit">({{unit}})</span></div>
              {{sb}}
            </div>
            """;
    }
}
