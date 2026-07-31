using UnityEngine;

namespace DZ.Core
{
    /// <summary>
    /// Raised when the last level in the catalog has been completed.
    /// Hook for a credits screen, menu return, or score summary — right now nothing
    /// subscribes, so the game simply ends on a black screen.
    /// </summary>
    public readonly struct GameCompletedSignal
    {

    }
}
