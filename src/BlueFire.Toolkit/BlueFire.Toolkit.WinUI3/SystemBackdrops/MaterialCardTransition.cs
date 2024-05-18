using BlueFire.Toolkit.WinUI3.Compositions;
using Microsoft.UI;
using Windows.UI.Composition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueFire.Toolkit.WinUI3.SystemBackdrops
{
    public class MaterialCardTransition
    {
        private IReadOnlyList<(CompositionAnimationGroup animation, CompositionObject target)>? animation;
        private readonly MaterialCardVisualHelper visualHelper;
        private CompositionScopedBatch? scopedBatch;

        internal MaterialCardTransition(MaterialCardVisualHelper visualHelper)
        {
            Compositor = WindowsCompositionHelper.Compositor;
            this.visualHelper = visualHelper;
            animation = CreateCompositionAnimation(visualHelper);
        }

        private protected Compositor Compositor { get; }

        internal bool IsPlaying => scopedBatch != null;

        public void Begin()
        {
            if (!IsPlaying && animation != null)
            {
                Stop();

                var scopedBatch = Compositor.CreateScopedBatch(CompositionBatchTypes.AllAnimations);
                this.scopedBatch = scopedBatch;
                scopedBatch.Completed += ScopedBatch_Completed;

                foreach (var (animation, target) in animation)
                {
                    target.StartAnimationGroup(animation);
                }

                scopedBatch.End();
            }
        }

        private void ScopedBatch_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            Stop();
        }

        public void Stop()
        {
            if (animation != null)
            {
                var scopedBatch = this.scopedBatch;
                if (scopedBatch != null)
                {
                    this.scopedBatch = null;

                    foreach (var (animation, target) in animation)
                    {
                        target.StopAnimationGroup(animation);
                    }

                    if (scopedBatch != null)
                    {
                        scopedBatch.Completed -= ScopedBatch_Completed;
                        scopedBatch.Dispose();
                    }
                }
            }
        }

        private protected virtual IReadOnlyList<(CompositionAnimationGroup animation, CompositionObject target)>? CreateCompositionAnimation(MaterialCardVisualHelper visualHelper)
        {
            return null;
        }

        public event EventHandler? Completed;
    }

    internal class MaterialCardShowTransition : MaterialCardTransition
    {
        internal MaterialCardShowTransition(MaterialCardVisualHelper visualHelper) : base(visualHelper)
        {
        }

        private protected override IReadOnlyList<(CompositionAnimationGroup animation, CompositionObject target)>? CreateCompositionAnimation(MaterialCardVisualHelper visualHelper)
        {
            return base.CreateCompositionAnimation(visualHelper);
        }
    }
}
