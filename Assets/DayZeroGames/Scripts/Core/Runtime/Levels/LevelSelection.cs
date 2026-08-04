using DZ.Core.Contracts;

namespace DZ.Core
{
    public sealed class LevelSelection : ILevelSelection
    {
        public const int NoSelection = -1;

        public int SelectedIndex { get; private set; } = NoSelection;
        public bool HasSelection => SelectedIndex >= 0;

        public void SelectLevel(int index) => SelectedIndex = index;
        public void Clear() => SelectedIndex = NoSelection;
    }
}
