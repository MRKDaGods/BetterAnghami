namespace MRK.UI
{
    /// <summary>
    /// One piece of the self-healing UI, plugged into the <see cref="BetterUiReconciler"/>. A step
    /// bakes in its own data and hands back the JS for its <c>ensure</c> function, which the
    /// reconciler re-runs on every pass to put the UI back whenever Anghami drops it.
    /// </summary>
    public interface IUiStep
    {
        /// <summary>
        /// Name of the global JS function this step defines (the reconciler calls it every pass).
        /// Must be a unique JS identifier; the function must be idempotent and cheap.
        /// </summary>
        string EnsureFunctionName { get; }

        /// <summary>
        /// This step's JS: the <see cref="EnsureFunctionName"/> function plus any helpers it needs.
        /// Every step shares one closure, so name things per-step to avoid clashes.
        /// </summary>
        Task<string> BuildJavaScriptAsync();
    }
}
