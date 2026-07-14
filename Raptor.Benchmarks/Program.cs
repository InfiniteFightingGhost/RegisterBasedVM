using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Running;

namespace Raptor.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        if (args.Contains("--fast"))
        {
            // Restore stdout which might be redirected inside setups, and mute stderr logging
            var stdout = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(stdout);
            Console.SetError(TextWriter.Null);

            Console.WriteLine("[Raptor Fast VM Benchmark Suite - Release Mode]");
            Console.WriteLine("=================================================");



            // 1. Instruction Latency
            Console.WriteLine("\n[Instruction Latency]");
            var latency = new InstructionLatencyBenchmark();
            latency.Setup();
            // warmup
            for (int i = 0; i < 100; i++)
                latency.Benchmark_Baseline();
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Baseline();
            sw.Stop();
            double baselineMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine($"Baseline Loop: {baselineMs * 1000.0:F2} us");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Add();
            sw.Stop();
            double addMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine($"ADD Latency: {((addMs - baselineMs) / 50000.0) * 1000000.0:F2} ns");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Sub();
            sw.Stop();
            double subMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine($"SUB Latency: {((subMs - baselineMs) / 50000.0) * 1000000.0:F2} ns");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Mul();
            sw.Stop();
            double mulMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine($"MUL Latency: {((mulMs - baselineMs) / 50000.0) * 1000000.0:F2} ns");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Div();
            sw.Stop();
            double divMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine($"DIV Latency: {((divMs - baselineMs) / 50000.0) * 1000000.0:F2} ns");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Sqrt();
            sw.Stop();
            double sqrtMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine(
                $"SQRT Latency: {((sqrtMs - baselineMs) / 50000.0) * 1000000.0:F2} ns"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Fisr();
            sw.Stop();
            double fisrMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine(
                $"FISR Latency: {((fisrMs - baselineMs) / 50000.0) * 1000000.0:F2} ns"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Rand();
            sw.Stop();
            double randMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine(
                $"RAND Latency: {((randMs - baselineMs) / 50000.0) * 1000000.0:F2} ns"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Loadc();
            sw.Stop();
            double loadcMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine(
                $"LOADC Latency: {((loadcMs - baselineMs) / 50000.0) * 1000000.0:F2} ns"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Move();
            sw.Stop();
            double moveMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine(
                $"MOVE Latency: {((moveMs - baselineMs) / 50000.0) * 1000000.0:F2} ns"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                latency.Benchmark_Jump();
            sw.Stop();
            double jumpMs = sw.Elapsed.TotalMilliseconds / 200.0;
            Console.WriteLine(
                $"JUMP Latency: {((jumpMs - baselineMs) / 50000.0) * 1000000.0:F2} ns"
            );

            // 2. Control Flow
            Console.WriteLine("\n[Control Flow & Branch Prediction]");
            var cf = new ControlFlowBenchmark();
            cf.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                cf.Benchmark_PredictableBranch();
            sw.Stop();
            double predMs = sw.Elapsed.TotalMilliseconds / 100.0;

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                cf.Benchmark_UnpredictableBranch();
            sw.Stop();
            double unpredMs = sw.Elapsed.TotalMilliseconds / 100.0;
            Console.WriteLine($"Predictable:   {predMs:F4} ms");
            Console.WriteLine($"Unpredictable: {unpredMs:F4} ms (diff: {unpredMs - predMs:F4} ms)");

            // 3. Call Stack
            Console.WriteLine("\n[Call Stack & FFI]");
            var cs = new CallStackBenchmark();
            cs.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                cs.CallStack_Depth_10();
            sw.Stop();
            Console.WriteLine($"Recursion Depth 10: {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                cs.CallStack_Depth_30();
            sw.Stop();
            Console.WriteLine($"Recursion Depth 30: {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                cs.Ffi_DirectBind();
            sw.Stop();
            Console.WriteLine($"FFI Direct Bind:    {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                cs.Ffi_TypedWrapper();
            sw.Stop();
            Console.WriteLine($"FFI Typed Wrapper:  {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                cs.Ffi_Fallback();
            sw.Stop();
            Console.WriteLine($"FFI Reflection:     {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            // 4. Memory
            Console.WriteLine("\n[Memory & Allocator]");
            var mem = new MemoryBenchmark();
            mem.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                mem.Memory_ArrayAccess();
            sw.Stop();
            Console.WriteLine($"Array Access:       {sw.Elapsed.TotalMilliseconds / 100.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                mem.Memory_AllocDeallocClean();
            sw.Stop();
            Console.WriteLine($"Alloc/Free Clean:   {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                mem.Memory_AllocDeallocChurn();
            sw.Stop();
            Console.WriteLine($"Alloc/Free Churn:   {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            // 5. Register Pressure
            Console.WriteLine("\n[Register Pressure]");
            var rp = new RegisterPressureBenchmark();
            rp.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                rp.Registers_Pressure_4();
            sw.Stop();
            Console.WriteLine($"4 Registers:   {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                rp.Registers_Pressure_64();
            sw.Stop();
            Console.WriteLine($"64 Registers:  {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                rp.Registers_Pressure_128();
            sw.Stop();
            Console.WriteLine($"128 Registers: {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            // 6. Lifecycle
            Console.WriteLine("\n[Script Lifecycle Phases]");
            var life = new LifecycleBenchmark();
            life.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                life.Lifecycle_Compile();
            sw.Stop();
            Console.WriteLine(
                $"Parse/Compile: {sw.Elapsed.TotalMilliseconds / 1000.0 * 1000.0:F2} us"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++)
                life.Lifecycle_Verify();
            sw.Stop();
            Console.WriteLine(
                $"Verify:        {sw.Elapsed.TotalMilliseconds / 2000.0 * 1000.0:F2} us"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 2000; i++)
                life.Lifecycle_Load();
            sw.Stop();
            Console.WriteLine(
                $"Pin/Load:      {sw.Elapsed.TotalMilliseconds / 2000.0 * 1000.0:F2} us"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                life.Lifecycle_Execute();
            sw.Stop();
            Console.WriteLine($"Execute:       {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            // 7. Verifier
            Console.WriteLine("\n[Verifier Scaling & Safety]");
            var ver = new VerifierBenchmark();
            ver.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                ver.Verifier_Scale_100();
            sw.Stop();
            Console.WriteLine(
                $"Verify 100 Insts:  {sw.Elapsed.TotalMilliseconds / 1000.0 * 1000.0:F2} us"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 500; i++)
                ver.Verifier_Scale_1000();
            sw.Stop();
            Console.WriteLine(
                $"Verify 1000 Insts: {sw.Elapsed.TotalMilliseconds / 500.0 * 1000.0:F2} us"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                ver.Verifier_Scale_10000();
            sw.Stop();
            Console.WriteLine(
                $"Verify 10000 Insts:{sw.Elapsed.TotalMilliseconds / 100.0 * 1000.0:F2} us"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                ver.Verifier_Safety_InvalidJump();
            sw.Stop();
            Console.WriteLine(
                $"Reject InvalidJump:{sw.Elapsed.TotalMilliseconds / 1000.0 * 1000.0:F2} us"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 1000; i++)
                ver.Verifier_Safety_InvalidMemory();
            sw.Stop();
            Console.WriteLine(
                $"Reject LargeAlloc: {sw.Elapsed.TotalMilliseconds / 1000.0 * 1000.0:F2} us"
            );

            // 8. Gameplay
            Console.WriteLine("\n[Gameplay Systems]");
            var game = new GameplayBenchmark();
            game.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                game.Gameplay_EcsUpdate();
            sw.Stop();
            Console.WriteLine(
                $"ECS Update (1000 entities): {sw.Elapsed.TotalMilliseconds / 100.0:F4} ms"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                game.Gameplay_GridPathfinding();
            sw.Stop();
            Console.WriteLine(
                $"BFS wavefront path search:  {sw.Elapsed.TotalMilliseconds / 100.0:F4} ms"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                game.Gameplay_DialogueTree();
            sw.Stop();
            Console.WriteLine(
                $"Dialogue conditions check:  {sw.Elapsed.TotalMilliseconds / 100.0:F4} ms"
            );

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                game.Gameplay_InventorySort();
            sw.Stop();
            Console.WriteLine(
                $"Inventory Sort (100 items): {sw.Elapsed.TotalMilliseconds / 100.0:F4} ms"
            );

            // 9. Multithreaded
            Console.WriteLine("\n[Multithreaded Scaling]");
            var mt = new MultithreadedBenchmark();
            mt.Setup();
            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                mt.Multithreaded_Scale_1();
            sw.Stop();
            Console.WriteLine($"1 VM instance:  {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                mt.Multithreaded_Scale_4();
            sw.Stop();
            Console.WriteLine($"4 VM instances: {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 200; i++)
                mt.Multithreaded_Scale_8();
            sw.Stop();
            Console.WriteLine($"8 VM instances: {sw.Elapsed.TotalMilliseconds / 200.0:F4} ms");

            // 10. VM Consolidated Benchmarks (Original VmBenchmarks suite)
            Console.WriteLine("\n[VM Consolidated Benchmarks]");
            var vmBench = new VmBenchmarks();
            vmBench.Setup();
            Console.SetOut(stdout);

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_Fibonacci();
            sw.Stop();
            Console.WriteLine($"Fibonacci (50k):      {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
                vmBench.Benchmark_MonteCarlo();
            sw.Stop();
            Console.WriteLine($"Monte Carlo (100k):   {sw.Elapsed.TotalMilliseconds / 10.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_Perceptron();
            sw.Stop();
            Console.WriteLine($"Perceptron (1k ep):   {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
                vmBench.Benchmark_RayTracerSingleFrame();
            sw.Stop();
            Console.WriteLine($"RayTracer (32x32):    {sw.Elapsed.TotalMilliseconds / 10.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_FfiDirectBind();
            sw.Stop();
            Console.WriteLine($"FFI Direct Bind:      {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_FfiTypedWrapper();
            sw.Stop();
            Console.WriteLine($"FFI Typed Wrapper:    {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_InternalCall();
            sw.Stop();
            Console.WriteLine($"Internal Call:        {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_FfiFallback();
            sw.Stop();
            Console.WriteLine($"FFI Fallback (Refl):  {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_PhysicsMovement();
            sw.Stop();
            Console.WriteLine($"Physics Movement:     {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 50; i++)
                vmBench.Benchmark_CombatDamage();
            sw.Stop();
            Console.WriteLine($"Combat Damage:        {sw.Elapsed.TotalMilliseconds / 50.0:F4} ms");

            Console.WriteLine("=================================================");
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

        // After benchmarks finish, run post-processing consolidation!
        ConsolidateReports();
    }

    private static void ConsolidateReports()
    {
        string artifactsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../BenchmarkDotNet.Artifacts/results");
        if (!Directory.Exists(artifactsDir))
        {
            // Try workspace root
            artifactsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts/results");
            if (!Directory.Exists(artifactsDir))
            {
                Console.WriteLine("[Raptor Consolidation Warning] BenchmarkDotNet.Artifacts/results directory not found. Skipping report merge.");
                return;
            }
        }

        Console.WriteLine("\n[Raptor Report Consolidation] Merging individual benchmark files...");

        var mdFiles = Directory.GetFiles(artifactsDir, "Raptor.Benchmarks.*-report-github.md");
        var csvFiles = Directory.GetFiles(artifactsDir, "Raptor.Benchmarks.*-report.csv");
        var htmlFiles = Directory.GetFiles(artifactsDir, "Raptor.Benchmarks.*-report.html");

        if (mdFiles.Length == 0)
        {
            Console.WriteLine("[Raptor Consolidation] No report files found to merge.");
            return;
        }

        // 1. Merge Markdown Reports
        var mdOutput = new System.Text.StringBuilder();
        mdOutput.AppendLine("# Raptor VM Benchmark Consolidated Report");
        mdOutput.AppendLine($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        mdOutput.AppendLine();

        foreach (var file in mdFiles.OrderBy(f => f))
        {
            string suiteName = Path.GetFileNameWithoutExtension(file)
                                   .Replace("Raptor.Benchmarks.", "")
                                   .Replace("-report-github", "");
            
            mdOutput.AppendLine($"## {suiteName}");
            mdOutput.AppendLine();
            
            var lines = File.ReadAllLines(file);
            // Skip header info (first 10 lines or lines before the table)
            bool tableStarted = false;
            foreach (var line in lines)
            {
                if (line.StartsWith("|"))
                {
                    tableStarted = true;
                }
                if (tableStarted)
                {
                    mdOutput.AppendLine(line);
                }
            }
            mdOutput.AppendLine();
        }
        string mdPath = Path.Combine(artifactsDir, "Consolidated-report.md");
        File.WriteAllText(mdPath, mdOutput.ToString());
        Console.WriteLine($"Merged Markdown saved to: {mdPath}");

        // 2. Merge CSV Reports
        var csvOutput = new System.Text.StringBuilder();
        bool headerWritten = false;
        foreach (var file in csvFiles.OrderBy(f => f))
        {
            var lines = File.ReadAllLines(file);
            if (lines.Length == 0) continue;

            if (!headerWritten)
            {
                // Write header: Suite, Method, Mean, etc.
                csvOutput.AppendLine($"\"Suite\",{lines[0]}");
                headerWritten = true;
            }

            string suiteName = Path.GetFileNameWithoutExtension(file)
                                   .Replace("Raptor.Benchmarks.", "")
                                   .Replace("-report", "");

            for (int i = 1; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    csvOutput.AppendLine($"\"{suiteName}\",{lines[i]}");
                }
            }
        }
        string csvPath = Path.Combine(artifactsDir, "Consolidated-report.csv");
        File.WriteAllText(csvPath, csvOutput.ToString());
        Console.WriteLine($"Merged CSV saved to: {csvPath}");

        // 3. Generate Beautiful HTML Dashboard
        GenerateHtmlDashboard(artifactsDir, htmlFiles);
    }

    private static void GenerateHtmlDashboard(string artifactsDir, string[] htmlFiles)
    {
        string csvPath = Path.Combine(artifactsDir, "Consolidated-report.csv");
        if (!File.Exists(csvPath)) return;

        var lines = File.ReadAllLines(csvPath);
        if (lines.Length <= 1) return;

        // Group rows by suite name
        var suites = new Dictionary<string, List<string[]>>();
        var headers = lines[0].Split(',').Select(h => h.Trim('"')).ToArray();

        for (int i = 1; i < lines.Length; i++)
        {
            var row = ParseCsvRow(lines[i]);
            if (row.Length == 0) continue;
            string suite = row[0];
            if (!suites.ContainsKey(suite))
            {
                suites[suite] = new List<string[]>();
            }
            suites[suite].Add(row.Skip(1).ToArray());
        }

        // Build premium dark-mode dashboard HTML
        var html = new System.Text.StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("    <title>Raptor VM Benchmark Consolidated Dashboard</title>");
        html.AppendLine("    <meta charset=\"utf-8\" />");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />");
        html.AppendLine("    <link href=\"https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700&display=swap\" rel=\"stylesheet\" />");
        html.AppendLine("    <style>");
        html.AppendLine("        :root {");
        html.AppendLine("            --bg: #0b0f19;");
        html.AppendLine("            --card: #151b2c;");
        html.AppendLine("            --primary: #6366f1;");
        html.AppendLine("            --primary-glow: rgba(99, 102, 241, 0.15);");
        html.AppendLine("            --text: #f3f4f6;");
        html.AppendLine("            --text-muted: #9ca3af;");
        html.AppendLine("            --border: #222d44;");
        html.AppendLine("            --success: #10b981;");
        html.AppendLine("            --accent: #a855f7;");
        html.AppendLine("        }");
        html.AppendLine("        * { box-sizing: border-box; margin: 0; padding: 0; }");
        html.AppendLine("        body {");
        html.AppendLine("            font-family: 'Outfit', sans-serif;");
        html.AppendLine("            background: var(--bg);");
        html.AppendLine("            color: var(--text);");
        html.AppendLine("            line-height: 1.5;");
        html.AppendLine("            padding: 40px 20px;");
        html.AppendLine("        }");
        html.AppendLine("        .container {");
        html.AppendLine("            max-width: 1200px;");
        html.AppendLine("            margin: 0 auto;");
        html.AppendLine("        }");
        html.AppendLine("        header {");
        html.AppendLine("            margin-bottom: 40px;");
        html.AppendLine("            border-bottom: 1px solid var(--border);");
        html.AppendLine("            padding-bottom: 24px;");
        html.AppendLine("        }");
        html.AppendLine("        h1 {");
        html.AppendLine("            font-size: 2.5rem;");
        html.AppendLine("            font-weight: 700;");
        html.AppendLine("            background: linear-gradient(135deg, #6366f1 0%, #a855f7 100%);");
        html.AppendLine("            -webkit-background-clip: text;");
        html.AppendLine("            -webkit-text-fill-color: transparent;");
        html.AppendLine("            margin-bottom: 8px;");
        html.AppendLine("        }");
        html.AppendLine("        .meta { color: var(--text-muted); font-size: 0.95rem; }");
        html.AppendLine("        .tabs {");
        html.AppendLine("            display: flex;");
        html.AppendLine("            flex-wrap: wrap;");
        html.AppendLine("            gap: 12px;");
        html.AppendLine("            margin-bottom: 30px;");
        html.AppendLine("        }");
        html.AppendLine("        .tab-btn {");
        html.AppendLine("            background: var(--card);");
        html.AppendLine("            border: 1px solid var(--border);");
        html.AppendLine("            color: var(--text-muted);");
        html.AppendLine("            padding: 12px 20px;");
        html.AppendLine("            border-radius: 8px;");
        html.AppendLine("            cursor: pointer;");
        html.AppendLine("            font-weight: 500;");
        html.AppendLine("            transition: all 0.25s ease;");
        html.AppendLine("        }");
        html.AppendLine("        .tab-btn:hover {");
        html.AppendLine("            border-color: var(--primary);");
        html.AppendLine("            color: var(--text);");
        html.AppendLine("            background: var(--primary-glow);");
        html.AppendLine("        }");
        html.AppendLine("        .tab-btn.active {");
        html.AppendLine("            border-color: var(--primary);");
        html.AppendLine("            color: var(--text);");
        html.AppendLine("            background: var(--primary);");
        html.AppendLine("            box-shadow: 0 0 15px rgba(99, 102, 241, 0.4);");
        html.AppendLine("        }");
        html.AppendLine("        .suite-card {");
        html.AppendLine("            display: none;");
        html.AppendLine("            background: var(--card);");
        html.AppendLine("            border: 1px solid var(--border);");
        html.AppendLine("            border-radius: 12px;");
        html.AppendLine("            padding: 30px;");
        html.AppendLine("            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.25);");
        html.AppendLine("            animation: fadeIn 0.4s ease forwards;");
        html.AppendLine("        }");
        html.AppendLine("        .suite-card.active { display: block; }");
        html.AppendLine("        @keyframes fadeIn {");
        html.AppendLine("            from { opacity: 0; transform: translateY(10px); }");
        html.AppendLine("            to { opacity: 1; transform: translateY(0); }");
        html.AppendLine("        }");
        html.AppendLine("        h2 { margin-bottom: 20px; font-weight: 600; font-size: 1.5rem; }");
        html.AppendLine("        .table-wrapper { overflow-x: auto; }");
        html.AppendLine("        table {");
        html.AppendLine("            width: 100%;");
        html.AppendLine("            border-collapse: collapse;");
        html.AppendLine("            text-align: left;");
        html.AppendLine("        }");
        html.AppendLine("        th, td { padding: 14px 18px; border-bottom: 1px solid var(--border); }");
        html.AppendLine("        th {");
        html.AppendLine("            color: var(--text-muted);");
        html.AppendLine("            font-weight: 600;");
        html.AppendLine("            text-transform: uppercase;");
        html.AppendLine("            font-size: 0.8rem;");
        html.AppendLine("            letter-spacing: 0.05em;");
        html.AppendLine("        }");
        html.AppendLine("        td { font-size: 0.95rem; }");
        html.AppendLine("        tr:hover td { background: rgba(255, 255, 255, 0.02); }");
        html.AppendLine("        .highlight { color: var(--success); font-weight: 600; }");
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        html.AppendLine("        <header>");
        html.AppendLine("            <h1>Raptor VM Consolidated Dashboard</h1>");
        html.AppendLine($"            <p class=\"meta\">Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss} | BenchmarkDotNet Test Suite</p>");
        html.AppendLine("        </header>");
        html.AppendLine("        <div class=\"tabs\">");

        // Tabs
        int idx = 0;
        foreach (var suite in suites.Keys)
        {
            string activeClass = idx == 0 ? " active" : "";
            html.AppendLine($"            <button class=\"tab-btn{activeClass}\" onclick=\"showSuite('{suite}', this)\">{suite}</button>");
            idx++;
        }
        html.AppendLine("        </div>");

        // Suite cards
        idx = 0;
        foreach (var pair in suites)
        {
            string activeClass = idx == 0 ? " active" : "";
            html.AppendLine($"        <div id=\"suite-{pair.Key}\" class=\"suite-card{activeClass}\">");
            html.AppendLine($"            <h2>{pair.Key} Results</h2>");
            html.AppendLine("            <div class=\"table-wrapper\">");
            html.AppendLine("                <table>");
            html.AppendLine("                    <thead>");
            html.AppendLine("                        <tr>");
            
            // th headers (skip the first "Suite" header)
            for (int h = 1; h < headers.Length; h++)
            {
                html.AppendLine($"                            <th>{headers[h]}</th>");
            }
            html.AppendLine("                        </tr>");
            html.AppendLine("                    </thead>");
            html.AppendLine("                    <tbody>");
            
            foreach (var row in pair.Value)
            {
                html.AppendLine("                        <tr>");
                for (int col = 0; col < row.Length; col++)
                {
                    string cellVal = row[col];
                    string tdClass = col == 0 ? " class=\"highlight\"" : "";
                    html.AppendLine($"                            <td{tdClass}>{cellVal}</td>");
                }
                html.AppendLine("                        </tr>");
            }
            html.AppendLine("                    </tbody>");
            html.AppendLine("                </table>");
            html.AppendLine("            </div>");
            html.AppendLine("        </div>");
            idx++;
        }

        html.AppendLine("    </div>");
        html.AppendLine("    <script>");
        html.AppendLine("        function showSuite(suiteId, btn) {");
        html.AppendLine("            var cards = document.getElementsByClassName('suite-card');");
        html.AppendLine("            for (var i = 0; i < cards.length; i++) {");
        html.AppendLine("                cards[i].classList.remove('active');");
        html.AppendLine("            }");
        html.AppendLine("            var btns = document.getElementsByClassName('tab-btn');");
        html.AppendLine("            for (var i = 0; i < btns.length; i++) {");
        html.AppendLine("                btns[i].classList.remove('active');");
        html.AppendLine("            }");
        html.AppendLine("            document.getElementById('suite-' + suiteId).classList.add('active');");
        html.AppendLine("            btn.classList.add('active');");
        html.AppendLine("        }");
        html.AppendLine("    </script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        string htmlPath = Path.Combine(artifactsDir, "Consolidated-report.html");
        File.WriteAllText(htmlPath, html.ToString());
        Console.WriteLine($"Dashboard HTML saved to: {htmlPath}");
    }

    private static string[] ParseCsvRow(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var currentToken = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentToken.ToString().Trim(' ', '"'));
                currentToken.Clear();
            }
            else
            {
                currentToken.Append(c);
            }
        }
        result.Add(currentToken.ToString().Trim(' ', '"'));
        return result.ToArray();
    }
}
