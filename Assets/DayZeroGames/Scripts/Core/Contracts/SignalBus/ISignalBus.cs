using System;

namespace DZ.Core.Contracts
{
    public interface ISignalBus
    {
        void Publish<T>(T eventData) where T : struct;
        void Subscribe<T>(Action<T> listener) where T : struct;
        void Unsubscribe<T>(Action<T> listener) where T : struct;
    }
}
