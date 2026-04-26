namespace Common;

public sealed class DelegateProgress<T>(DelegateProgress<T>.ProgressDelegate onProgress) : IProgress<T>
{
    public delegate void ProgressDelegate(T progress);
    
    public void Report(T value)
    {
        onProgress.Invoke(value);
    }
}