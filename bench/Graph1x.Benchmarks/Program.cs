using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Graph1x.Benchmarks.StorageBenchmarks).Assembly).Run(args);
