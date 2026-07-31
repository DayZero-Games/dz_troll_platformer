using UnityEngine;

namespace DZ.Core
{
    /// <summary>
    /// Raised by an exit door once its sequence finishes: "the player went through me."
    /// Deliberately carries no index — a level prefab does not know where it sits in the
    /// catalog, and duplicating that would drift the moment the catalog is reordered.
    /// LevelFlowController turns this into an indexed <see cref="LevelCompletedSignal"/>.
    /// </summary>
    public readonly struct LevelExitReachedSignal
    {

    }
}
