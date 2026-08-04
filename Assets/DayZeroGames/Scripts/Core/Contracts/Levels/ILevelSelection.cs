namespace DZ.Core.Contracts
{
    public interface ILevelSelection
    {
        bool HasSelection { get; }
        int SelectedIndex { get; }

        void SelectLevel(int index);
        void Clear();
    }
}
