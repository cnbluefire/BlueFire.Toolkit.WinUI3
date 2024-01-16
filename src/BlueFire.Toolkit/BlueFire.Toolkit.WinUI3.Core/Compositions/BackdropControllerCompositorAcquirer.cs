using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinCompositor = Windows.UI.Composition.Compositor;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    public class BackdropControllerCompositorAcquirer : Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop, IDisposable
    {
        private Windows.UI.Composition.CompositionBrush? systemBackdrop;
        private TaskCompletionSource<WinCompositor?> taskSource = new TaskCompletionSource<WinCompositor?>();
        private bool isDisposed;

        Windows.UI.Composition.CompositionBrush? Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop.SystemBackdrop
        {
            get => systemBackdrop;
            set
            {
                lock (this)
                {
                    if (isDisposed) throw new ObjectDisposedException(nameof(BackdropControllerCompositorAcquirer));

                    systemBackdrop = value;
                    var taskSource = this.taskSource;
                    this.taskSource = new TaskCompletionSource<WinCompositor?>();

                    taskSource.SetResult(value?.Compositor);
                }
            }
        }

        public Task<WinCompositor?> WaitForCompositorAsync()
        {
            return taskSource.Task;
        }

        public void Dispose()
        {
            lock (this)
            {
                if (!isDisposed)
                {
                    isDisposed = true;
                    var taskSource = this.taskSource;
                    this.taskSource = null!;

                    taskSource.SetCanceled();
                }
            }
        }
    }
}
