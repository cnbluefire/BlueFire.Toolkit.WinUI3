using BlueFire.Toolkit.WinUI3.Graphics;
using BlueFire.Toolkit.WinUI3.Text;
using Microsoft.UI;
using Microsoft.UI.Composition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace BlueFire.Toolkit.WinUI3.Controls.FormattedTextRenderers
{
    internal abstract class FormattedTextRendererLineVisual
    {
        private const string FadeOutColorWidth = "10f";

        private bool disposedValue;
        private CompositionGraphicsDeviceHolder? deviceHolder;
        private CompositionPropertySet? propSet;
        private Color textColor;
        private Color textSecondaryColor;
        private Color textStrokeColor;
        private Color textStrokeSecondaryColor;

        private CompositionLinearGradientBrush textProgressBrush = null!;
        private CompositionLinearGradientBrush strokeProgressBrush = null!;

        private CompositionColorGradientStop[] gradientStops = null!;
        private CompositionColorGradientStop[] strokeGradientStops = null!;

        private ExpressionAnimation progress1Bind = null!;
        private ExpressionAnimation progress2Bind = null!;

        private ExpressionAnimation textColorBind = null!;
        private ExpressionAnimation textSecondaryColorBind = null!;

        private ExpressionAnimation strokeColorBind = null!;
        private ExpressionAnimation strokeSecondaryColorBind = null!;

        private DropShadow? dropShadow;

        public void Initialize(CompositionGraphicsDeviceHolder deviceHolder)
        {
            this.deviceHolder = deviceHolder;

            var compositor = deviceHolder.Compositor;
            var defaultColor = Color.FromArgb(0, 255, 255, 255);

            propSet = compositor.CreatePropertySet();
            propSet.InsertScalar("Progress", Random.Shared.NextSingle());
            //propSet.InsertScalar("Progress", 0f);
            propSet.InsertColor(nameof(TextColor), defaultColor);
            propSet.InsertColor(nameof(TextSecondaryColor), defaultColor);
            propSet.InsertColor(nameof(TextStrokeColor), defaultColor);
            propSet.InsertColor(nameof(TextStrokeSecondaryColor), defaultColor);

            textProgressBrush = compositor.CreateLinearGradientBrush();
            textProgressBrush.StartPoint = Vector2.Zero;
            textProgressBrush.EndPoint = new Vector2(1, 0);
            textProgressBrush.MappingMode = CompositionMappingMode.Relative;

            strokeProgressBrush = compositor.CreateLinearGradientBrush();
            strokeProgressBrush.StartPoint = Vector2.Zero;
            strokeProgressBrush.EndPoint = new Vector2(1, 0);
            strokeProgressBrush.MappingMode = CompositionMappingMode.Relative;

            gradientStops = new[]
            {
                compositor.CreateColorGradientStop(0, Colors.Transparent),
                compositor.CreateColorGradientStop(0, Colors.Transparent),
                compositor.CreateColorGradientStop(0, Colors.Transparent),
                compositor.CreateColorGradientStop(2, Colors.Transparent),
            };

            for (int i = 0; i < gradientStops.Length; i++)
            {
                textProgressBrush.ColorStops.Add(gradientStops[i]);
            }

            strokeGradientStops = new[]
            {
                compositor.CreateColorGradientStop(0, Colors.Transparent),
                compositor.CreateColorGradientStop(0, Colors.Transparent),
                compositor.CreateColorGradientStop(0, Colors.Transparent),
                compositor.CreateColorGradientStop(2, Colors.Transparent),
            };

            for (int i = 0; i < strokeGradientStops.Length; i++)
            {
                strokeProgressBrush.ColorStops.Add(strokeGradientStops[i]);
            }

            Initialize();

            textColorBind = compositor.CreateExpressionAnimation($"propSet.{nameof(TextColor)}");
            textSecondaryColorBind = compositor.CreateExpressionAnimation($"propSet.{nameof(TextSecondaryColor)}");

            strokeColorBind = compositor.CreateExpressionAnimation($"propSet.{nameof(TextStrokeColor)}");
            strokeSecondaryColorBind = compositor.CreateExpressionAnimation($"propSet.{nameof(TextStrokeSecondaryColor)}");

            progress1Bind = compositor.CreateExpressionAnimation("Clamp(propSet.Progress, 0, 1)");
            progress2Bind = compositor.CreateExpressionAnimation($"propSet.Progress <= 0 ? 0 : (Clamp(propSet.Progress, 0, 1) + (visual.Size.X == 0 ? 0 : ({FadeOutColorWidth} / visual.Size.X)))");

            textColorBind.SetReferenceParameter("propSet", propSet);
            textSecondaryColorBind.SetReferenceParameter("propSet", propSet);
            strokeColorBind.SetReferenceParameter("propSet", propSet);
            strokeSecondaryColorBind.SetReferenceParameter("propSet", propSet);
            progress1Bind.SetReferenceParameter("propSet", propSet);
            progress2Bind.SetReferenceParameter("propSet", propSet);
            progress2Bind.SetReferenceParameter("visual", Visual);

            gradientStops[0].StartAnimation("Color", textColorBind);
            gradientStops[1].StartAnimation("Color", textColorBind);
            gradientStops[1].StartAnimation("Offset", progress1Bind);
            gradientStops[2].StartAnimation("Color", textSecondaryColorBind);
            gradientStops[2].StartAnimation("Offset", progress2Bind);
            gradientStops[3].StartAnimation("Color", textSecondaryColorBind);

            strokeGradientStops[0].StartAnimation("Color", strokeColorBind);
            strokeGradientStops[1].StartAnimation("Color", strokeColorBind);
            strokeGradientStops[1].StartAnimation("Offset", progress1Bind);
            strokeGradientStops[2].StartAnimation("Color", strokeSecondaryColorBind);
            strokeGradientStops[2].StartAnimation("Offset", progress2Bind);
            strokeGradientStops[3].StartAnimation("Color", strokeSecondaryColorBind);
        }

        protected virtual void Initialize() { }

        protected CompositionGraphicsDeviceHolder DeviceHolder => deviceHolder!;

        protected bool IsDisposed => disposedValue;


        public Color TextColor
        {
            get => textColor;
            set
            {
                if (textColor != value)
                {
                    textColor = value;
                    propSet!.InsertColor(nameof(TextColor), value);
                    OnTextColorChanged(value);
                }
            }
        }

        protected virtual void OnTextColorChanged(Color value)
        {

        }

        public Color TextSecondaryColor
        {
            get => textSecondaryColor;
            set
            {
                if (textSecondaryColor != value)
                {
                    textSecondaryColor = value;
                    propSet!.InsertColor(nameof(TextSecondaryColor), value);
                    OnTextSecondaryColorChanged(value);
                }
            }
        }

        protected virtual void OnTextSecondaryColorChanged(Color value)
        {

        }

        public Color TextStrokeColor
        {
            get => textStrokeColor;
            set
            {
                if (textStrokeColor != value)
                {
                    textStrokeColor = value;
                    propSet!.InsertColor(nameof(TextStrokeColor), value);
                    OnTextStrokeColorChanged(value);
                }
            }
        }

        protected virtual void OnTextStrokeColorChanged(Color value)
        {

        }

        public Color TextStrokeSecondaryColor
        {
            get => textStrokeSecondaryColor;
            set
            {
                if (textStrokeSecondaryColor != value)
                {
                    textStrokeSecondaryColor = value;
                    propSet!.InsertColor(nameof(TextStrokeSecondaryColor), value);
                    OnTextStrokeSecondaryColorChanged(value);
                }
            }
        }

        protected virtual void OnTextStrokeSecondaryColorChanged(Color value)
        {

        }


        public abstract Visual Visual { get; }

        public DropShadow? DropShadow
        {
            get => dropShadow;
            set
            {
                if (dropShadow != value)
                {
                    dropShadow = value;
                    OnDropShadowChanged(value);
                }
            }
        }

        protected virtual void OnDropShadowChanged(DropShadow? value)
        {

        }

        public void StartProgressAnimation(CompositionAnimation animation)
        {
            if (disposedValue) return;

            propSet!.StartAnimation("Progress", animation);
        }

        public void StopProgressAnimation()
        {
            if (disposedValue) return;

            propSet!.StopAnimation("Progress");
        }

        protected CompositionPropertySet GetPropertySet()
        {
            return propSet!;
        }

        protected CompositionBrush GetTextBrush()
        {
            return textProgressBrush!;
        }

        protected CompositionBrush GetStrokeBrush()
        {
            return strokeProgressBrush!;
        }

        public abstract void UpdateLineVisual(
            FormattedText.FormattedTextLineGlyphRuns lineGlyphRuns,
            double textStrokeThickness,
            Point startPointOffset,
            double rasterizationScale,
            bool isColorFontEnabled);

        protected virtual void DisposeCore(bool disposing)
        {

        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                dropShadow = null;
                DisposeCore(disposing);

                if (gradientStops != null)
                {
                    for (int i = 0; i < gradientStops.Length; i++)
                    {
                        gradientStops[i].StopAnimation("Offset");
                        gradientStops[i].StopAnimation("Color");
                    }
                }

                if (strokeGradientStops != null)
                {
                    for (int i = 0; i < strokeGradientStops.Length; i++)
                    {
                        strokeGradientStops[i].StopAnimation("Offset");
                        strokeGradientStops[i].StopAnimation("Color");
                    }
                }

                textProgressBrush?.Dispose();
                textProgressBrush = null!;

                strokeProgressBrush?.Dispose();
                strokeProgressBrush = null!;


                progress1Bind?.Dispose();
                progress1Bind = null!;

                progress2Bind?.Dispose();
                progress2Bind = null!;

                textColorBind?.Dispose();
                textColorBind = null!;

                textSecondaryColorBind?.Dispose();
                textSecondaryColorBind = null!;

                strokeColorBind?.Dispose();
                strokeColorBind = null!;

                strokeSecondaryColorBind?.Dispose();
                strokeSecondaryColorBind = null!;

                if (gradientStops != null)
                {
                    for (int i = 0; i < gradientStops.Length; i++)
                    {
                        gradientStops[i].Dispose();
                    }
                }

                if (strokeGradientStops != null)
                {
                    for (int i = 0; i < strokeGradientStops.Length; i++)
                    {
                        strokeGradientStops[i].Dispose();
                    }
                }

                gradientStops = null!;
                strokeGradientStops = null!;

                propSet?.Dispose();
                propSet = null;
                deviceHolder = null;

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // ~FormattedTextRendererLineVisual()
        // {
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetPixelSize(double epx, double scale)
        {
            return (int)(epx * scale);
        }

    }
}
