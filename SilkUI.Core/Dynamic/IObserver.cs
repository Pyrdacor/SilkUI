using System;

namespace SilkUI
{
    public interface IObserver<T>
    {
        void Next(T value);
        void Error(Exception exception);
        void Complete();
    }
}