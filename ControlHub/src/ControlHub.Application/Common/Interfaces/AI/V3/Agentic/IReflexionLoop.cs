namespace ControlHub.Application.Common.Interfaces.AI.V3.Agentic
{
    /// <summary>
    /// Reflexion Loop interface - Enables self-correction through reflection.
    /// Based on "Reflexion: Language Agents with Verbal Reinforcement Learning".
    /// </summary>
    public interface IReflexionLoop
    {
        /// <summary>
        /// Analyze previous attempt and suggest corrections.
        /// </summary>
        /// <param name="state">Current agent state with execution history</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Reflexion result with analysis and suggestions</returns>
        Task<ReflexionResult> ReflectAsync(IAgentState state, CancellationToken ct = default);
    }

    /// <summary>
    /// Result from reflexion analysis.
    /// </summary>
    public record ReflexionResult(
        /// <summary>Analysis of what went wrong</summary>
        string Analysis,

        /// <summary>Suggested corrections</summary>
        string Corrections,

        /// <summary>Should retry execution</summary>
        bool ShouldRetry,

        /// <summary>Confidence in the correction</summary>
        float Confidence
    );
}
