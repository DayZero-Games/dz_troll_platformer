using DZ.Core;

namespace DZ.Core.Contracts
{
    public enum LevelControlTarget
    {
        Player,
        Puppet
    }

    public interface ILevelRuntimeActions
    {
        void SetInvertControls(bool inverted);
        void SetGravityScale(float gravityScale);
        void SetJumpRules(int maxAirJumps, float jumpForceMultiplier);
        void SetJumpEnabled(bool enabled);
        void ApplyRuntimeRules(LevelRules rules);
        void RestoreCatalogRules();
        bool SwitchControl(LevelControlTarget target);
    }
}
