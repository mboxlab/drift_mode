```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
Intel Core i9-9900KF CPU 3.60GHz (Coffee Lake), 1 CPU, 16 logical and 8 physical cores
.NET SDK 8.0.401
  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2


```
| Method   | Keys | Mean          | Error      | StdDev     | Ratio |
|--------- |----- |--------------:|-----------:|-----------:|------:|
| **Curve**    | **1**    |     **55.156 ns** |  **0.0776 ns** |  **0.0688 ns** |  **1.00** |
| AltCurve | 1    |      3.384 ns |  0.0713 ns |  0.0632 ns |  0.06 |
|          |      |               |            |            |       |
| **Curve**    | **2**    |    **109.973 ns** |  **0.1830 ns** |  **0.1528 ns** |  **1.00** |
| AltCurve | 2    |     15.369 ns |  0.1992 ns |  0.1863 ns |  0.14 |
|          |      |               |            |            |       |
| **Curve**    | **5**    |    **170.725 ns** |  **0.3472 ns** |  **0.3248 ns** |  **1.00** |
| AltCurve | 5    |     16.564 ns |  0.0287 ns |  0.0239 ns |  0.10 |
|          |      |               |            |            |       |
| **Curve**    | **25**   |    **273.834 ns** |  **0.2214 ns** |  **0.1729 ns** |  **1.00** |
| AltCurve | 25   |     18.720 ns |  0.0782 ns |  0.0653 ns |  0.07 |
|          |      |               |            |            |       |
| **Curve**    | **50**   |    **221.819 ns** |  **1.1613 ns** |  **0.9697 ns** |  **1.00** |
| AltCurve | 50   |     20.825 ns |  0.0494 ns |  0.0413 ns |  0.09 |
|          |      |               |            |            |       |
| **Curve**    | **100**  |    **289.034 ns** |  **2.9633 ns** |  **2.7719 ns** |  **1.00** |
| AltCurve | 100  |     23.611 ns |  0.1488 ns |  0.1392 ns |  0.08 |
|          |      |               |            |            |       |
| **Curve**    | **1000** | **12,965.685 ns** | **46.4060 ns** | **41.1378 ns** | **1.000** |
| AltCurve | 1000 |     27.605 ns |  0.0890 ns |  0.0743 ns | 0.002 |
