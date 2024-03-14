using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.Core.Extensions
{
    internal class WeakEventListener<TInstance, TSender, TEventArgs> where TInstance : class
    {
        private WeakReference<TInstance>? weakInstance;
        private Action<TInstance, TSender?, TEventArgs>? onEventAction;
        private Action<WeakEventListener<TInstance, TSender, TEventArgs>>? onDetachAction;

        internal WeakEventListener(
            TInstance instance,
            Action<TInstance, TSender?, TEventArgs> onEventAction,
            Action<WeakEventListener<TInstance, TSender, TEventArgs>> onDetachAction)
        {
            weakInstance = new WeakReference<TInstance>(instance);
            this.onEventAction = onEventAction;
            this.onDetachAction = onDetachAction;
        }

        internal void OnEvent(TSender? sender, TEventArgs eventArgs)
        {
            if (weakInstance == null) return;

            if (weakInstance.TryGetTarget(out var target))
            {
                onEventAction?.Invoke(target, sender, eventArgs);
            }
            else
            {
                Detach();
            }
        }

        internal void Detach()
        {
            weakInstance = null;
            var onDetachAction = this.onDetachAction;
            this.onDetachAction = null;
            this.onEventAction = null;

            onDetachAction?.Invoke(this);
        }
    }
}
