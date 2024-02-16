using BlueFire.Toolkit.WinUI3.Graphics;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI;
using Microsoft.UI.Composition;
using System.Numerics;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using FormattedTextLineGlyphRuns = BlueFire.Toolkit.WinUI3.Text.FormattedText.FormattedTextLineGlyphRuns;

namespace BlueFire.Toolkit.WinUI3.Controls.FormattedTextRenderers
{
    internal interface IFormattedTextRenderer : IDisposable
    {
        Visual RootVisual { get; }

        DropShadow? DropShadow { get; set; }

        void Update(IReadOnlyList<FormattedTextLineGlyphRuns>? lineGlyphRuns, Size layoutSize, Color textColor, Color textSecondaryColor, Color strokeColor, Color strokeSecondaryColor, double strokeThickness, double rasterizationScale, bool isColorFontEnabled);
    }

    internal class FormattedTextRenderer<T> : IFormattedTextRenderer where T : FormattedTextRendererLineVisual, new()
    {
        private bool disposedValue;

        private CompositionGraphicsDeviceHolder deviceHolder;
        private List<T> lineVisuals;
        private ContainerVisual rootVisual;
        private DropShadow? dropShadow;

        internal FormattedTextRenderer(CompositionGraphicsDeviceHolder deviceHolder)
        {
            this.deviceHolder = deviceHolder;
            this.lineVisuals = new List<T>();
            rootVisual = deviceHolder.Compositor.CreateContainerVisual();
        }

        protected CompositionGraphicsDeviceHolder DeviceHolder => deviceHolder;

        public Visual RootVisual => rootVisual;

        public DropShadow? DropShadow
        {
            get => dropShadow;
            set
            {
                if (dropShadow != value)
                {
                    dropShadow = value;
                    lock (lineVisuals)
                    {
                        for (int i = 0; i < lineVisuals.Count; i++)
                        {
                            lineVisuals[i].DropShadow = dropShadow;
                        }
                    }
                }
            }
        }

        public void Update(
            IReadOnlyList<FormattedTextLineGlyphRuns>? lineGlyphRuns,
            Size layoutSize,
            Color textColor,
            Color textSecondaryColor,
            Color strokeColor,
            Color strokeSecondaryColor,
            double strokeThickness,
            double rasterizationScale,
            bool isColorFontEnabled)
        {
            rootVisual.Size = new Vector2((float)(layoutSize.Width), (float)(layoutSize.Height));
            rootVisual.Children.RemoveAll();

            if (lineGlyphRuns == null)
            {
                lock (lineVisuals)
                {
                    while (lineVisuals.Count > 10)
                    {
                        lineVisuals[^1].Dispose();
                        lineVisuals.RemoveAt(lineVisuals.Count - 1);
                    }
                }
            }
            else
            {
                var lineCount = lineGlyphRuns.Count;

                lock (lineVisuals)
                {
                    if (lineVisuals.Count > lineCount)
                    {
                        for (int i = lineVisuals.Count - 1; i >= lineCount; i--)
                        {
                            lineVisuals[i].Dispose();
                            lineVisuals.RemoveAt(i);
                        }
                    }
                    else if (lineVisuals.Count < lineCount)
                    {
                        for (int i = lineVisuals.Count; i < lineCount; i++)
                        {
                            var lv = new T();
                            lv.Initialize(deviceHolder);
                            lv.DropShadow = dropShadow;
                            lineVisuals.Add(lv);
                        }
                    }
                }

                using var brush = new CanvasSolidColorBrush(
                    deviceHolder.CanvasDevice,
                    Color.FromArgb(255, 255, 255, 255));

                float offsetY = 0;

                for (int i = 0; i < lineCount; i++)
                {
                    var lg = lineGlyphRuns[i];

                    var lv = lineVisuals[i];

                    lv.TextColor = textColor;
                    lv.TextSecondaryColor = textSecondaryColor;
                    lv.TextStrokeColor = strokeColor;
                    lv.TextStrokeSecondaryColor = strokeSecondaryColor;

                    lv.Visual.Offset = new Vector3((float)lg.Bounds.Left, (float)lg.Bounds.Top, 0);

                    lv.UpdateLineVisual(
                        lg,
                        strokeThickness,
                        new Point(0, -offsetY),
                        rasterizationScale,
                        isColorFontEnabled);

                    rootVisual.Children.InsertAtTop(lv.Visual);

                    offsetY += lg.CanvasLineMetrics.Height;
                }
            }
        }

        protected virtual void DisposeCore(bool disposing) { }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                DisposeCore(disposing);

                lock (lineVisuals)
                {
                    for (int i = 0; i < lineVisuals.Count; i++)
                    {
                        lineVisuals[i].Dispose();
                    }
                    lineVisuals.Clear();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}