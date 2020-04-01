using System.ComponentModel;

namespace SilkUI
{
    public struct Style
    {
        #region Background

        public BackgroundStyle? Background;
        [DefaultValue("#ffffff")]
        public ColorValue? BackgroundColor;

        #endregion


        #region Border

        public BorderStyle? Border;
        public BorderSideStyle? BorderTop;
        public BorderSideStyle? BorderRight;
        public BorderSideStyle? BorderBottom;
        public BorderSideStyle? BorderLeft;
        [DefaultValue(0)]
        public AllDirectionStyleValue<int>? BorderSize;
        [DefaultValue(SilkUI.BorderLineStyle.None)]
        public AllDirectionStyleValue<BorderLineStyle>? BorderLineStyle;
        [DefaultValue("#000000")]
        public AllDirectionStyleValue<ColorValue>? BorderColor;

        #endregion

        
        #region Shadow

        public ShadowStyle? Shadow;
        [DefaultValue(false)]
        public bool? ShadowVisible;
        [DefaultValue(0)]
        public int? ShadowXOffset;
        [DefaultValue(0)]
        public int? ShadowYOffset;
        [DefaultValue(0)]
        public int? ShadowBlurRadius;
        [DefaultValue(0)]
        public int? ShadowSpreadRadius;
        [DefaultValue(false)]
        public bool? ShadowInset;
        [DefaultValue("#000000")]
        public ColorValue? ShadowColor;

        #endregion
    }
}
