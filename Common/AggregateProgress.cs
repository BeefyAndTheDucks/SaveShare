namespace Common;

public class AggregateProgress(IProgress<double> outputProgress, int initialItemCount = 0)
{
    private readonly Lock _lock = new();
    private readonly List<AggregateProgressItem> _items = new(initialItemCount);

    public static AggregateProgress? From(IProgress<double>? outputProgress, int initialItemCount = 0)
    {
        return outputProgress != null ? new AggregateProgress(outputProgress, initialItemCount) : null;
    }
    
    public IProgress<double> CreateProgressItem()
    {
        AggregateProgressItem progress = new();

        lock (_lock)
        {
            _items.Add(progress);
        }
        
        progress.Reported += OnReport;
        return progress;
    }

    private void OnReport(double value)
    {
        double average;

        lock (_lock)
        {
            average = _items.Sum(x => x.Progress) / Math.Max(initialItemCount, _items.Count);
        }
        
        outputProgress.Report(average);
    }

    private class AggregateProgressItem : IProgress<double>
    {
        private readonly Lock _lock = new();
        private double _progress;
        internal double Progress
        {
            get
            {
                lock (_lock)
                {
                    return _progress;
                }
            }
        }
        
        internal event Action<double>? Reported;
        
        public void Report(double value)
        {
            lock (_lock)
            {
                _progress = value;
            }
            Reported?.Invoke(value);
        }
    }
}
