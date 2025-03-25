using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.Extensions
{
    internal class WeakObjectPool<T> where T : class
    {
        private readonly Func<T> createFunc;
        private ConcurrentQueue<WeakReference<T>> _items = new();
        private T? _fastItem;
        private int _maxCapacity;
        private int _numItems;

        public WeakObjectPool(Func<T> createFunc) : this(createFunc, Environment.ProcessorCount * 2)
        {
        }

        public WeakObjectPool(Func<T> createFunc, int maximumRetained)
        {
            ArgumentNullException.ThrowIfNull(createFunc);

            this.createFunc = createFunc;
            _maxCapacity = maximumRetained - 1;
        }

        public T Get()
        {
            var item = _fastItem;
            if (item == null && Interlocked.CompareExchange(ref _fastItem, null, item) != item)
            {
                WeakReference<T>? weakTarget = null;
                while (_items.TryDequeue(out weakTarget))
                {
                    Interlocked.Decrement(ref _numItems);

                    if (weakTarget.TryGetTarget(out item))
                    {
                        return item;
                    }
                }

                return createFunc.Invoke();
            }
            return item!;
        }

        public void Return(T obj)
        {
            if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
            {
                if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
                {
                    _items.Enqueue(new WeakReference<T>(obj));
                }
                else
                {
                    // slow
                    foreach (var item in _items)
                    {
                        if (!item.TryGetTarget(out _))
                        {
                            item.SetTarget(obj);
                            return;
                        }
                    }
                }

                // no room, clean up the count and drop the object on the floor
                Interlocked.Decrement(ref _numItems);
            }
        }
    }
}
