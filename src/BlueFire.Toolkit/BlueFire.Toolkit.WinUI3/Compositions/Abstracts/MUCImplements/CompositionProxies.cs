using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Effects;
using Windows.UI;
using CompositionNS = Microsoft.UI.Composition;

namespace BlueFire.Toolkit.WinUI3.Compositions.Abstracts.MUCImplements
{
    internal class CompositionProxies
    {
        internal class CompositorProxy : ICompositor
        {
            private readonly CompositionNS.Compositor compositor;

            public CompositorProxy(CompositionNS.Compositor compositor)
            {
                this.compositor = compositor;
            }

            public object RawObject => compositor;

            public ICompositionBrush CreateBackdropBrush()
            {
                return new CompositionBrushProxy(compositor.CreateBackdropBrush());
            }

            public ICompositionColorBrush CreateColorBrush()
            {
                return new CompositionColorBrushProxy(compositor.CreateColorBrush());
            }

            public ICompositionColorBrush CreateColorBrush(Color color)
            {
                return new CompositionColorBrushProxy(compositor.CreateColorBrush(color));
            }

            public ICompositionColorGradientStop CreateColorGradientStop()
            {
                return new CompositionColorGradientStopProxy(compositor.CreateColorGradientStop());
            }

            public ICompositionColorGradientStop CreateColorGradientStop(float offset, Color color)
            {
                return new CompositionColorGradientStopProxy(compositor.CreateColorGradientStop(offset, color));
            }

            public ICompositionEffectBrush CreateEffectBrush(IGraphicsEffect effect, string[] animatableProperties)
            {
                return new CompositionEffectBrushProxy(compositor.CreateEffectFactory(effect, animatableProperties).CreateBrush());
            }

            public ICompositionLinearGradientBrush CreateLinearGradientBrush()
            {
                return new CompositionLinearGradientBrushProxy(compositor.CreateLinearGradientBrush());
            }

            public ISpriteVisual CreateSpriteVisual()
            {
                return new SpriteVisualProxy(compositor.CreateSpriteVisual());
            }

            public IExpressionAnimation CreateExpressionAnimation()
            {
                return new ExpressionAnimationProxy(compositor.CreateExpressionAnimation());
            }

            public IExpressionAnimation CreateExpressionAnimation(string expression)
            {
                return new ExpressionAnimationProxy(compositor.CreateExpressionAnimation(expression));
            }

            public ICompositionPropertySet CreatePropertySet()
            {
                return new CompositionPropertySetProxy(compositor.CreatePropertySet());
            }
        }

        internal class CompositionObjectProxy : ICompositionObject
        {
            private bool disposedValue;

            private CompositionNS.CompositionObject compositionObject;
            private CompositionPropertySetProxy? properties;

            public CompositionObjectProxy(CompositionNS.CompositionObject compositionObject)
            {
                this.compositionObject = compositionObject;
            }

            protected bool IsDisposed => disposedValue;

            public ICompositionPropertySet Properties => disposedValue ? null! : (properties ??= new CompositionPropertySetProxy(compositionObject.Properties));

            public object RawObject => compositionObject;

            public void StartAnimation(string propertyName, ICompositionAnimation animation)
            {
                compositionObject.StartAnimation(propertyName, (CompositionNS.CompositionAnimation)animation.RawObject);
            }

            public void StopAnimation(string propertyName)
            {
                compositionObject.StopAnimation(propertyName);
            }

            protected virtual void DisposeCore() { }

            public void Dispose()
            {
                if (!disposedValue)
                {
                    disposedValue = true;

                    DisposeCore();

                    properties = null!;

                    compositionObject?.Dispose();
                    compositionObject = null!;
                }
            }
        }

        internal class CompositionAnimationProxy : CompositionObjectProxy, ICompositionAnimation
        {
            public CompositionAnimationProxy(CompositionNS.CompositionAnimation animation)
                : base(animation) { }

            public void ClearAllParameters()
            {
                ((CompositionNS.CompositionAnimation)RawObject).ClearAllParameters();
            }

            public void ClearParameter(string key)
            {
                ((CompositionNS.CompositionAnimation)RawObject).ClearParameter(key);
            }

            public void SetColorParameter(string key, Color value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetColorParameter(key, value);
            }

            public void SetMatrix3x2Parameter(string key, Matrix3x2 value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetMatrix3x2Parameter(key, value);
            }

            public void SetMatrix4x4Parameter(string key, Matrix4x4 value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetMatrix4x4Parameter(key, value);
            }

            public void SetQuaternionParameter(string key, Quaternion value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetQuaternionParameter(key, value);
            }

            public void SetReferenceParameter(string key, ICompositionWrapper compositionObject)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetReferenceParameter(key, (CompositionNS.CompositionObject)compositionObject.RawObject);
            }

            public void SetScalarParameter(string key, float value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetScalarParameter(key, value);
            }

            public void SetVector2Parameter(string key, Vector2 value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetVector2Parameter(key, value);
            }

            public void SetVector3Parameter(string key, Vector3 value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetVector3Parameter(key, value);
            }

            public void SetVector4Parameter(string key, Vector4 value)
            {
                ((CompositionNS.CompositionAnimation)RawObject).SetVector4Parameter(key, value);
            }
        }

        internal class ExpressionAnimationProxy : CompositionAnimationProxy, IExpressionAnimation
        {
            public ExpressionAnimationProxy(CompositionNS.CompositionAnimation animation)
                : base(animation) { }

            public string Expression
            {
                get => ((CompositionNS.ExpressionAnimation)RawObject).Expression;
                set => ((CompositionNS.ExpressionAnimation)RawObject).Expression = value;
            }
        }

        internal class CompositionPropertySetProxy : CompositionObjectProxy, ICompositionPropertySet
        {
            public CompositionPropertySetProxy(CompositionNS.CompositionPropertySet propertySet)
                : base(propertySet) { }

            public void InsertColor(string propertyName, Color value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertColor(propertyName, value);
            }

            public void InsertMatrix3x2(string propertyName, Matrix3x2 value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertMatrix3x2(propertyName, value);
            }

            public void InsertMatrix4x4(string propertyName, Matrix4x4 value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertMatrix4x4(propertyName, value);
            }

            public void InsertQuaternion(string propertyName, Quaternion value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertQuaternion(propertyName, value);
            }

            public void InsertScalar(string propertyName, float value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertScalar(propertyName, value);
            }

            public void InsertVector2(string propertyName, Vector2 value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertVector2(propertyName, value);
            }

            public void InsertVector3(string propertyName, Vector3 value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertVector3(propertyName, value);
            }

            public void InsertVector4(string propertyName, Vector4 value)
            {
                ((CompositionNS.CompositionPropertySet)RawObject).InsertVector4(propertyName, value);
            }

            public CompositionGetValueStatus TryGetColor(string propertyName, out Color value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetColor(propertyName, out value));
            }

            public CompositionGetValueStatus TryGetMatrix3x2(string propertyName, out Matrix3x2 value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetMatrix3x2(propertyName, out value));
            }

            public CompositionGetValueStatus TryGetMatrix4x4(string propertyName, out Matrix4x4 value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetMatrix4x4(propertyName, out value));
            }

            public CompositionGetValueStatus TryGetQuaternion(string propertyName, out Quaternion value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetQuaternion(propertyName, out value));
            }

            public CompositionGetValueStatus TryGetScalar(string propertyName, out float value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetScalar(propertyName, out value));
            }

            public CompositionGetValueStatus TryGetVector2(string propertyName, out Vector2 value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetVector2(propertyName, out value));
            }

            public CompositionGetValueStatus TryGetVector3(string propertyName, out Vector3 value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetVector3(propertyName, out value));
            }

            public CompositionGetValueStatus TryGetVector4(string propertyName, out Vector4 value)
            {
                return MapCompositionGetValueStatus(((CompositionNS.CompositionPropertySet)RawObject).TryGetVector4(propertyName, out value));
            }

            private static CompositionGetValueStatus MapCompositionGetValueStatus(CompositionNS.CompositionGetValueStatus status)
            {
                switch (status)
                {
                    case CompositionNS.CompositionGetValueStatus.Succeeded:
                        return CompositionGetValueStatus.Succeeded;

                    case CompositionNS.CompositionGetValueStatus.TypeMismatch:
                        return CompositionGetValueStatus.TypeMismatch;

                    case CompositionNS.CompositionGetValueStatus.NotFound:
                    default:
                        return CompositionGetValueStatus.NotFound;
                }
            }
        }

        internal class CompositionBrushProxy : CompositionObjectProxy, ICompositionBrush
        {
            public CompositionBrushProxy(CompositionNS.CompositionBrush compositionBrush)
                : base(compositionBrush) { }
        }


        internal class CompositionColorGradientStopProxy : CompositionObjectProxy, ICompositionColorGradientStop
        {
            public CompositionColorGradientStopProxy(CompositionNS.CompositionColorGradientStop stop)
                : base(stop) { }

            public float Offset
            {
                get => ((CompositionNS.CompositionColorGradientStop)RawObject).Offset;
                set => ((CompositionNS.CompositionColorGradientStop)RawObject).Offset = value;
            }

            public Color Color
            {
                get => ((CompositionNS.CompositionColorGradientStop)RawObject).Color;
                set => ((CompositionNS.CompositionColorGradientStop)RawObject).Color = value;
            }
        }

        internal class CompositionEffectBrushProxy : CompositionBrushProxy, ICompositionEffectBrush
        {
            public CompositionEffectBrushProxy(CompositionNS.CompositionEffectBrush effectBrush)
                : base(effectBrush) { }

            public void SetSourceParameter(string name, ICompositionBrush source)
            {
                ((CompositionNS.CompositionEffectBrush)RawObject).SetSourceParameter(name, (CompositionNS.CompositionBrush)source.RawObject);
            }
        }

        internal class CompositionColorBrushProxy : CompositionBrushProxy, ICompositionColorBrush
        {
            public CompositionColorBrushProxy(CompositionNS.CompositionColorBrush colorBrush)
                : base(colorBrush) { }

            public Color Color
            {
                get => ((CompositionNS.CompositionColorBrush)RawObject).Color;
                set => ((CompositionNS.CompositionColorBrush)RawObject).Color = value;
            }
        }

        internal class CompositionLinearGradientBrushProxy : CompositionBrushProxy, ICompositionLinearGradientBrush
        {
            private CompositionColorGradientStopCollectionProxy? colorStops;

            public CompositionLinearGradientBrushProxy(CompositionNS.CompositionLinearGradientBrush brush)
                : base(brush) { }
            public Vector2 StartPoint
            {
                get => ((CompositionNS.CompositionLinearGradientBrush)RawObject).StartPoint;
                set => ((CompositionNS.CompositionLinearGradientBrush)RawObject).StartPoint = value;
            }

            public Vector2 EndPoint
            {
                get => ((CompositionNS.CompositionLinearGradientBrush)RawObject).EndPoint;
                set => ((CompositionNS.CompositionLinearGradientBrush)RawObject).EndPoint = value;
            }

            public CompositionMappingMode MappingMode
            {
                get => MapCompositionMappingMode(((CompositionNS.CompositionLinearGradientBrush)RawObject).MappingMode);
                set => ((CompositionNS.CompositionLinearGradientBrush)RawObject).MappingMode = MapCompositionMappingMode(value);
            }

            public ICompositionColorGradientStopCollection ColorStops => IsDisposed ? null! : (colorStops ??= new CompositionColorGradientStopCollectionProxy(((CompositionNS.CompositionLinearGradientBrush)RawObject).ColorStops));

            protected override void DisposeCore()
            {
                base.DisposeCore();

                colorStops = null!;
            }

            private static CompositionNS.CompositionMappingMode MapCompositionMappingMode(CompositionMappingMode mappingMode)
            {
                switch (mappingMode)
                {
                    case CompositionMappingMode.Relative:
                        return CompositionNS.CompositionMappingMode.Relative;

                    default:
                    case CompositionMappingMode.Absolute:
                        return CompositionNS.CompositionMappingMode.Absolute;
                }
            }

            private static CompositionMappingMode MapCompositionMappingMode(CompositionNS.CompositionMappingMode mappingMode)
            {
                switch (mappingMode)
                {
                    case CompositionNS.CompositionMappingMode.Relative:
                        return CompositionMappingMode.Relative;

                    default:
                    case CompositionNS.CompositionMappingMode.Absolute:
                        return CompositionMappingMode.Absolute;
                }
            }
        }

        internal class CompositionColorGradientStopCollectionProxy : ICompositionColorGradientStopCollection
        {
            private readonly CompositionNS.CompositionColorGradientStopCollection collection;

            public CompositionColorGradientStopCollectionProxy(CompositionNS.CompositionColorGradientStopCollection collection)
            {
                this.collection = collection;
            }

            public ICompositionColorGradientStop this[int index] => new CompositionColorGradientStopProxy(collection[index]);

            public object RawObject => collection;

            public void Add(ICompositionColorGradientStop stop)
            {
                collection.Add((CompositionNS.CompositionColorGradientStop)stop.RawObject);
            }

            public void Clear()
            {
                collection.Clear();
            }

            public void Remove(ICompositionColorGradientStop stop)
            {
                collection.Remove((CompositionNS.CompositionColorGradientStop?)stop?.RawObject);
            }
        }

        internal class SpriteVisualProxy : CompositionObjectProxy, ISpriteVisual
        {
            private VisualCollectionProxy children;
            private ICompositionBrush? compositionBrush;

            public SpriteVisualProxy(CompositionNS.SpriteVisual visual) : base(visual)
            {
                this.children = new VisualCollectionProxy(((CompositionNS.SpriteVisual)RawObject).Children);
            }

            public Vector2 RelativeSizeAdjustment
            {
                get => ((CompositionNS.SpriteVisual)RawObject).RelativeSizeAdjustment;
                set => ((CompositionNS.SpriteVisual)RawObject).RelativeSizeAdjustment = value;
            }

            public IVisualCollection Children => IsDisposed ? null! : (children ??= new VisualCollectionProxy(((CompositionNS.SpriteVisual)RawObject).Children));

            public ICompositionBrush? Brush
            {
                get => GetCompositionBrush();
                set => ((CompositionNS.SpriteVisual)RawObject).Brush = (CompositionNS.CompositionBrush?)value?.RawObject;
            }

            private ICompositionBrush? GetCompositionBrush()
            {
                var brush = ((CompositionNS.SpriteVisual)RawObject).Brush;

                if (brush == null)
                {
                    compositionBrush = null;
                }
                else
                {
                    if (!ReferenceEquals(compositionBrush?.RawObject, brush))
                    {
                        if (brush is CompositionNS.CompositionEffectBrush effectBrush)
                        {
                            compositionBrush = new CompositionEffectBrushProxy(effectBrush);
                        }
                        else if (brush is CompositionNS.CompositionLinearGradientBrush linearGradientBrush)
                        {
                            compositionBrush = new CompositionLinearGradientBrushProxy(linearGradientBrush);
                        }
                        else if (brush is CompositionNS.CompositionColorBrush colorBrush)
                        {
                            compositionBrush = new CompositionColorBrushProxy(colorBrush);
                        }
                        else
                        {
                            compositionBrush = new CompositionBrushProxy(brush);
                        }
                    }
                }
                return compositionBrush;
            }

            protected override void DisposeCore()
            {
                base.DisposeCore();

                children = null!;
                compositionBrush = null!;
            }
        }

        internal class VisualCollectionProxy : IVisualCollection
        {
            private readonly CompositionNS.VisualCollection collection;

            public VisualCollectionProxy(CompositionNS.VisualCollection collection)
            {
                this.collection = collection;
            }

            public object RawObject => collection;

            public void InsertAtTop(IVisual visual)
            {
                collection.InsertAtTop((CompositionNS.Visual)visual.RawObject);
            }

            public void RemoveAll()
            {
                collection.RemoveAll();
            }
        }
    }
}
