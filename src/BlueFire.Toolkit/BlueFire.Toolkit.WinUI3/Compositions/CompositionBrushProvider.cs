using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;
using WinRT;

namespace BlueFire.Toolkit.WinUI3.Compositions
{
    internal abstract class CompositionBrushProvider : INotifyPropertyChanged, IDisposable
    {
        private static readonly TimeSpan defaultSwitchDuration = TimeSpan.FromMilliseconds(167);
        private static readonly string[] fallbackAnimatableProperties = new[]
        {
            "CrossFadeEffect.CrossFade",
            "FallbackColorEffect.Color",
        };

        private bool disposedValue;
        private DisposableCollection disposableCollection;

        private bool useFallback = false;
        private bool forceUseFallback = false;
        private Color fallbackColor = Color.FromArgb(255, 0, 0, 0);

        private CompositionEasingFunction? fallbackTransitionEasing;
        private CompositionScopedBatch? switchTransitionBatch;

        public CompositionBrushProvider()
        {
            disposableCollection = new DisposableCollection();
            Compositor = WindowsCompositionHelper.Compositor;

            Brush = TraceDisposable(CreateBrush(fallbackAnimatableProperties.Concat(AnimatableProperties ?? Array.Empty<string>())));
        }

        public Compositor Compositor { get; }

        public CompositionBrush Brush { get; }


        public Color FallbackColor
        {
            get => fallbackColor;
            set => SetProperty(ref fallbackColor, value);
        }

        public bool UseFallback
        {
            get => useFallback;
            set => SetProperty(ref useFallback, value);
        }

        protected bool ForceUseFallback
        {
            get => forceUseFallback;
            set => SetProperty(ref forceUseFallback, value);
        }

        protected virtual IReadOnlyList<string> AnimatableProperties { get; } = Array.Empty<string>();

        protected bool SetProperty<T>(ref T prop, T value, [CallerMemberName] string propName = "")
        {
            if (!object.Equals(prop, value))
            {
                prop = value;
                OnPropertyChanged(propName);
                return true;
            }

            return false;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            switch (propName)
            {
                case nameof(FallbackColor):
                    {
                        if (Brush is CompositionColorBrush colorBrush) colorBrush.Color = FallbackColor;
                        else Brush.Properties.InsertColor("FallbackColorEffect.Color", FallbackColor);
                    }
                    break;

                case nameof(UseFallback):
                case nameof(ForceUseFallback):
                    {
                        UpdateUseFallbackState();
                    }
                    break;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        private void UpdateUseFallbackState()
        {
            if (Brush is CompositionColorBrush) return;

            CancelFallbackSwitchAnimation();

            if (ForceUseFallback)
            {
                Brush.Properties.InsertScalar("CrossFadeEffect.CrossFade", 0);
                return;
            }

            if (fallbackTransitionEasing == null)
            {
                fallbackTransitionEasing = Compositor.CreateCubicBezierEasingFunction(new Vector2(0.5f, 0.0f), new Vector2(0.0f, 0.9f));
            }

            var fromValue = 0f;
            var toValue = 0f;

            if (UseFallback)
            {
                fromValue = 1f;
            }
            else
            {
                toValue = 1f;
            }

            var animation = Compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(0.0f, fromValue);
            animation.InsertKeyFrame(1.0f, toValue, fallbackTransitionEasing);
            animation.Duration = defaultSwitchDuration;
            animation.StopBehavior = AnimationStopBehavior.SetToFinalValue;

            var batch = Compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
            batch.Completed += FallbackSwitchAnimation_Completed;
            Brush.StartAnimation("CrossFadeEffect.CrossFade", animation);
            batch.End();

            switchTransitionBatch = batch;
        }

        private void FallbackSwitchAnimation_Completed(object sender, CompositionBatchCompletedEventArgs args)
        {
            CancelFallbackSwitchAnimation(true);
            Brush.Properties.InsertScalar("CrossFadeEffect.CrossFade", UseFallback ? 0f : 1f);
        }

        private void CancelFallbackSwitchAnimation(bool completed = false)
        {
            if (switchTransitionBatch != null)
            {
                if (!completed)
                {
                    switchTransitionBatch.Completed -= FallbackSwitchAnimation_Completed;
                }

                switchTransitionBatch.Dispose();
                switchTransitionBatch = null;
                Brush.StopAnimation("CrossFadeEffect.CrossFade");
            }
        }

        protected ICanvasEffect CreateEffect()
        {
            var coreEffect = TraceDisposable(CreateEffectCore());
            var fallback = TraceDisposable(new ColorSourceEffect()
            {
                Name = "FallbackColorEffect",
                Color = FallbackColor,
            });

            var effect = TraceDisposable(new CrossFadeEffect()
            {
                Name = "CrossFadeEffect",
                Source1 = coreEffect,
                Source2 = fallback,
                CrossFade = 1,
            });

            return effect;
        }

        protected abstract ICanvasEffect CreateEffectCore();

        protected abstract CompositionBrush CreateBrush(IEnumerable<string> animatableProperties);

        [return: NotNullIfNotNull("obj")]
        protected T? TraceDisposable<T>(T? obj, bool onlyDisposeOnDisposing = true) where T : IDisposable
        {
            if (obj is not null)
            {
                if (onlyDisposeOnDisposing)
                {
                    disposableCollection.AddDisposing(obj);
                }
                else
                {
                    disposableCollection.Add(obj);
                }
            }
            return obj;
        }

        protected void ThrowIfDisposed()
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().Name);
        }

        protected virtual void DisposeCore(bool disposing) { }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                try
                {
                    if (disposing)
                    {
                        if (switchTransitionBatch != null)
                        {
                            switchTransitionBatch.Completed -= FallbackSwitchAnimation_Completed;
                            Brush.StopAnimation("CrossFadeEffect.CrossFade");
                        }
                    }

                    switchTransitionBatch?.Dispose();
                    switchTransitionBatch = null;

                    fallbackTransitionEasing?.Dispose();
                    fallbackTransitionEasing = null;

                    DisposeCore(disposing);

                    disposableCollection.Dispose(disposing);
                    disposableCollection = null!;
                }
                finally
                {
                    disposedValue = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        ~CompositionBrushProvider()
        {
            Dispose(false);
        }

        private sealed class DisposableCollection
        {
            private bool disposedValue;
            private List<(WeakReference<IDisposable> obj, Action<IDisposable, bool>? disposeAction)> objs = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<T>(T obj) where T : IDisposable => Add(obj, null);

            public void AddDisposing<T>(T obj) where T : IDisposable => Add(obj, DelegateCache<T>.Action);


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Add<T>(T obj, Action<T, bool>? disposeAction) where T : IDisposable
            {
                lock (objs)
                {
                    Action<IDisposable, bool>? wrapperAction = disposeAction is null ? null :
                        (_obj, _disposing) => disposeAction.Invoke((T)_obj, _disposing);

                    objs.Add((new WeakReference<IDisposable>(obj), wrapperAction));
                }
            }

            public void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    lock (objs)
                    {
                        var hashSet = new HashSet<IDisposable>();
                        for (int i = objs.Count - 1; i >= 0; i--)
                        {
                            var (weakObj, disposeAction) = objs[i];

                            if (weakObj.TryGetTarget(out var obj))
                            {
                                if (disposeAction != null)
                                {
                                    disposeAction.Invoke(obj, disposing);
                                }
                                else
                                {
                                    obj.Dispose();
                                }
                            }
                        }

                        objs.Clear();
                    }

                    disposedValue = true;
                }
            }


            private class DelegateCache<T> where T : IDisposable
            {
                public static Action<T, bool> Action = DisposeOnDisposing;

                private static void DisposeOnDisposing(T disposable, bool disposing)
                {
                    if (disposing) disposable?.Dispose();
                }
            }
        }
    }
}
