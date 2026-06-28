namespace Common;

public class AggregateProgress(IProgress<double> outputProgress)
{
    private readonly List<AggregateProgressItem> _items = [];

    public static AggregateProgress? From(IProgress<double>? outputProgress)
    {
        return outputProgress != null ? new AggregateProgress(outputProgress) : null;
    }
    
    public IProgress<double> CreateProgressItem()
    {
        AggregateProgressItem progress = new();
        _items.Add(progress);
        progress.Reported += OnReport;
        return progress;
    }

    private void OnReport(double value)
    {
        double average = _items.Average(x => x.Progress);
        outputProgress.Report(average);
    }

    private class AggregateProgressItem : IProgress<double>
    {
        internal double Progress { get; private set; }
        internal event Action<double>? Reported;
        
        public void Report(double value)
        {
            Progress = value;
            Reported?.Invoke(value);
        }
    }
}
