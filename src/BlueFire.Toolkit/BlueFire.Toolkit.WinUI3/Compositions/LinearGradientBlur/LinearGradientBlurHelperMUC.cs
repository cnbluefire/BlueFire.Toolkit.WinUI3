using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Effects;
using CompositionNS = Microsoft.UI.Composition;
using static BlueFire.Toolkit.WinUI3.Compositions.Abstracts.MUCImplements.CompositionProxies;

namespace BlueFire.Toolkit.WinUI3.Compositions.LinearGradientBlur
{
    internal class LinearGradientBlurHelperMUC : LinearGradientBlurHelperBase<CompositionNS.Visual, CompositionNS.CompositionPropertySet>
    {
        public LinearGradientBlurHelperMUC(CompositionNS.Compositor compositor) : base(new CompositorProxy(compositor))
        {
        }

        protected override IGraphicsEffectSource CreateEffectSourceParameter(string name)
        {
            return new CompositionNS.CompositionEffectSourceParameter(name);
        }
    }
}