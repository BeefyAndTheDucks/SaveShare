using Octodiff.Diagnostics;

namespace Common;

public class ProgressReporterToIProgress(IProgress<double> progress) : IProgressReporter
{
    private double _last;
    
    public static IProgressReporter From(IProgress<double>? progress)
    {
        return progress != null ? new ProgressReporterToIProgress(progress) : NullProgressReporter.Instance;
    }
    
    public void ReportProgress(string operation, long currentPosition, long total)
    {
        double progressValue = (double)currentPosition / total;
        if (progressValue - _last < 0.01 && progressValue < 1)
            return;
        
        _last = progressValue;
        
        progress.Report(progressValue);
    }
}