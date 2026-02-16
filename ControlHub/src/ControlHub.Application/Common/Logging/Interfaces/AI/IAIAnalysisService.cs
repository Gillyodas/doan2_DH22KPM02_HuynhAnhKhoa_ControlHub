using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlHub.Application.Common.Logging.Interfaces.AI
{
    public interface IAIAnalysisService
    {
        Task<string> AnalyzeLogsAsync(IEnumerable<LogEntry> logs);
    }
}
