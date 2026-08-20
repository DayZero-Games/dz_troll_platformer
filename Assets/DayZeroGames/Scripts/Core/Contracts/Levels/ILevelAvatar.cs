using UnityEngine;

namespace DZ.Core
{
    /// <summary>
    /// Whatever the level currently treats as "the player": the real player in a normal
    /// level, or a puppet object in a level that hands control to something else.
    /// Level-side systems (the exit door) talk to this so they never have to care which
    /// one they got.
    /// </summary>
    public interface ILevelAvatar
    {
        Transform Transform { get; }
        SpriteRenderer SpriteRenderer { get; }
        bool IsDead { get; }

        /// Freeze it and stop it reading input.
        void Lock();

        /// Hand control back.
        void Unlock();
    }
}
