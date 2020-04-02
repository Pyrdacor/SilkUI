namespace SilkUI.Renderer.OpenGL.Shaders
{
    internal class BlurEllipseShader : ShaderBase, IShaderWithScreenHeight
    {
        private static BlurEllipseShader _blurEllipseShader = null;

        private static readonly string BlurEllipseFragmentShader =
        $@"
            {FragmentShaderHeader}
            in vec4 pixelColor;
            flat in uint scrH;
            flat in uint blurR;
            flat in uint ellipseRoundness;
            flat in ivec2 ellipseOrigin;
            flat in ivec2 ellipseSize;

            float get_blur_range(float x, float y, float w, float h)
            {{
                return pow(2.0f, ellipseRoundness) * (pow(x / w, ellipseRoundness) + pow(y / h, ellipseRoundness));
            }}

            float blur()
            {{
                if (blurR == 0u)
                    return 1.0f; // no blur -> no alpha change

                float r = float(blurR);
                float wi = float(ellipseSize.x);
                float hi = float(ellipseSize.y);
                float wo = wi + 2.0f * r;
                float ho = hi + 2.0f * r;
                float x = abs(gl_FragCoord.x - float(ellipseOrigin.x));
                float y = abs(float(scrH) - gl_FragCoord.y - float(ellipseOrigin.y));
                
                float inner_range = get_blur_range(x, y, wi, hi);
                if (inner_range <= 1.0f)
                    return 1.0f;
                float outer_range = get_blur_range(x, y, wo, ho);
                if (outer_range > 1.0f)
                    discard;
                float xc = 0.0f;
                float yc = y;
                if (x != 0)
                {{
                    float m = y / x;
                    xc = 1.0f / (2.0f * pow(pow(1.0f / wi, ellipseRoundness) + pow(m / hi, ellipseRoundness), 1.0f / ellipseRoundness));
                    yc = m * xc;
                }}
                float dist = sqrt(pow(x - xc, 2.0f) + pow(y - yc, 2.0f));
                return pow(1.0f - dist / r, 2.0f);
            }}
            
            void main()
            {{
                if (ellipseRoundness != 2u || ellipseRoundness != 4u || ellipseRoundness != 8u || ellipseRoundness != 16u)
                    discard; // invalid input -> don't show anything

                {FragmentOutColorName} = vec4(pixelColor.rgb, pixelColor.a * blur());
            }}
        ";

        private static readonly string BlurEllipseVertexShader =
        @$"
            {VertexShaderHeader}
            in ivec2 {PositionName};
            in ivec2 {SizeName};
            in uint {LayerName};
            in uvec4 {ColorName};
            in uint {RoundnessName};
            in uint {BlurRadiusName};
            in ivec2 {OriginName};
            uniform uint {ScreenHeightName};
            uniform float {ZName};
            uniform mat4 {ProjectionMatrixName};
            uniform mat4 {ModelViewMatrixName};
            out vec4 pixelColor;
            flat out uint scrH;
            flat out uint blurR;
            flat out uint ellipseRoundness;
            flat out ivec2 ellipseOrigin;
            flat out ivec2 ellipseSize;
            
            void main()
            {{
                vec2 pos = vec2(float({PositionName}.x) + 0.49f, float({PositionName}.y) + 0.49f);
                pixelColor = vec4({ColorName}.r / 255.0f, {ColorName}.g / 255.0f, {ColorName}.b / 255.0f, {ColorName}.a / 255.0f);
                scrH = {ScreenHeightName};
                blurR = {BlurRadiusName};
                ellipseRoundness = {RoundnessName};
                ellipseOrigin = {OriginName};
                ellipseSize = {SizeName};                
                gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, 1.0f - {ZName} - float({LayerName}) * 0.00001f, 1.0f);
            }}
        ";

        public BlurEllipseShader()
            : base(BlurEllipseFragmentShader, BlurEllipseVertexShader)
        {

        }

        public void SetScreenHeight(uint height)
        {
            _shaderProgram.SetInput(ScreenHeightName, height);
        }

        public static BlurEllipseShader Instance
        {
            get
            {
                if (_blurEllipseShader == null)
                    _blurEllipseShader = new BlurEllipseShader();

                return _blurEllipseShader;
            }
        }
    }
}
