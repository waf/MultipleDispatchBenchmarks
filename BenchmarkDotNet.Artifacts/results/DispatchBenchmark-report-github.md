``` ini

BenchmarkDotNet=v0.10.5, OS=Windows 10.0.14393
Processor=Intel Core i7-3615QM CPU 2.30GHz (Ivy Bridge), ProcessorCount=8
Frequency=2241013 Hz, Resolution=446.2268 ns, Timer=TSC
dotnet cli version=1.0.3
  [Host]     : .NET Core 4.6.25009.03, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.25009.03, 64bit RyuJIT


```
 |                    Method |      Mean |     Error |    StdDev |
 |-------------------------- |----------:|----------:|----------:|
 |        DictionaryDispatch | 122.20 us | 1.0371 us | 1.8701 us |
 |           DynamicDispatch |  65.21 us | 0.0799 us | 0.0708 us |
 |     IfElsePatternDispatch |  15.24 us | 0.4141 us | 0.6070 us |
 | SwitchCasePatternDispatch |  15.73 us | 0.0294 us | 0.0275 us |
 |           VisitorDispatch |  17.32 us | 0.0279 us | 0.0261 us |
