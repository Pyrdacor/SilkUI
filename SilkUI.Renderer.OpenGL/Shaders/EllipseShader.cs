namespace SilkUI.Renderer.OpenGL.Shaders
{
    internal class EllipseShader : ShaderBase, IShaderWithScreenHeight
    {
        private static EllipseShader _ellipseShader = null;

        private static readonly string EllipseFragmentShader =
        $@"
            {FragmentShaderHeader}
            in vec4 pixelColor;
            flat in uint scrH;
            flat in uint ellipseRoundness;
            flat in ivec2 ellipseOrigin;
            flat in ivec2 ellipseSize;
            
            void main()
            {{
                if (ellipseRoundness != 2u || ellipseRoundness != 4u || ellipseRoundness != 8u || ellipseRoundness != 16u)
                    discard; // invalid input -> don't show anything

                float w = float(ellipseSize.x);
                float h = float(ellipseSize.y);
                float x = abs(gl_FragCoord.x - float(ellipseOrigin.x));
                float y = abs(float(scrH) - gl_FragCoord.y - float(ellipseOrigin.y));
                float range = pow(2.0f, ellipseRoundness) * (pow(x / w, ellipseRoundness) + pow(y / h, ellipseRoundness));

                if (range > 1.0f)
                    discard; // outside the ellipse

                {FragmentOutColorName} = pixelColor;
            }}
        ";

        private static readonly string EllipseVertexShader =
        @$"
            {VertexShaderHeader}
            in ivec2 {PositionName};
            in ivec2 {SizeName};
            in uint {LayerName};
            in uvec4 {ColorName};
            in uint {RoundnessName};
            in ivec2 {OriginName};
            uniform uint {ScreenHeightName};
            uniform float {ZName};
            uniform mat4 {ProjectionMatrixName};
            uniform mat4 {ModelViewMatrixName};
            out vec4 pixelColor;
            flat out uint scrH;
            flat out uint ellipseRoundness;
            flat out ivec2 ellipseOrigin;
            flat out ivec2 ellipseSize;
            
            void main()
            {{
                vec2 pos = vec2(float({PositionName}.x) + 0.49f, float({PositionName}.y) + 0.49f);
                pixelColor = vec4({ColorName}.r / 255.0f, {ColorName}.g / 255.0f, {ColorName}.b / 255.0f, {ColorName}.a / 255.0f);
                scrH = {ScreenHeightName};
                ellipseRoundness = {RoundnessName};
                ellipseOrigin = {OriginName};
                ellipseSize = {SizeName};                
                gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, 1.0f - {ZName} - float({LayerName}) * 0.00001f, 1.0f);
            }}
        ";

        public EllipseShader()
            : base(EllipseFragmentShader, EllipseVertexShader)
        {

        }

        public void SetScreenHeight(uint height)
        {
            _shaderProgram.SetInput(ScreenHeightName, height);
        }

        public static EllipseShader Instance
        {
            get
            {
                if (_ellipseShader == null)
                    _ellipseShader = new EllipseShader();

                return _ellipseShader;
            }
        }
    }
}
