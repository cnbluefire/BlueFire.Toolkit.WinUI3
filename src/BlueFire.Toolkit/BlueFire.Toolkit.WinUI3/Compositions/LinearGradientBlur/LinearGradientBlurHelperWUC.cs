using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Effects;
using CompositionNS = Windows.UI.Composition;
using static BlueFire.Toolkit.WinUI3.Compositions.Abstracts.WUCImplements.CompositionProxies;

namespace BlueFire.Toolkit.WinUI3.Compositions.LinearGradientBlur
{
    internal class LinearGradientBlurHelperWUC : LinearGradientBlurHelperBase<CompositionNS.Visual, CompositionNS.CompositionPropertySet>
    {
        public LinearGradientBlurHelperWUC(CompositionNS.Compositor compositor) : base(new CompositorProxy(compositor))
        {
        }

        protected override IGraphicsEffectSource CreateEffectSourceParameter(string name)
        {
            return new CompositionNS.CompositionEffectSourceParameter(name);
        }
    }
}