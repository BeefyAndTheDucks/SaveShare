using Octodiff.Diagnostics;

namespace Common;

public class AbsoluteProgressReporter(IProgress<double> baseProgress, double startProgress, double endProgress) : IProgressReporter
{
    public static AbsoluteProgressReporter? From(IProgress<double>? baseProgress, double startProgress,
        double endProgress)
    {
        if (baseProgress == null)
            return null;
        return new AbsoluteProgressReporter(baseProgress, startProgress, endProgress);
    }
    
    public void ReportProgress(string operation, long currentPosition, long total)
    {
        double relativeProgress = (double)currentPosition / total;
        double absoluteProgress = double.Lerp(startProgress, endProgress, relativeProgress);
        baseProgress.Report(absoluteProgress);
    }
}