using System.ComponentModel;

namespace SilkUI
{
    public struct ShadowStyle
    {
        [DefaultValue(false)]
        public bool? Visible;
        [DefaultValue(0)]
        public int? XOffset;
        [DefaultValue(0)]
        public int? YOffset;
        [DefaultValue(0)]
        public int? BlurRadius;
        [DefaultValue(0)]
        public int? SpreadRadius;
        [DefaultValue(false)]
        public bool? Inset;
        [DefaultValue("#000000")]
        public ColorValue? Color;
    }
}