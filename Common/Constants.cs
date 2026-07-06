namespace Common;

public static class Constants
{
    public const string APPLICATION_VERSION = "2.0.0";
    
    public static readonly int MaxFileParallelism = Math.Min(Environment.ProcessorCount, 2);
}