namespace SilkUI.Renderer.OpenGL.Shaders
{
    internal class BlurRectShader : ShaderBase, IShaderWithScreenHeight
    {
        private static BlurRectShader _blurRectShader = null;

        private static readonly string BlurRectFragmentShader =
        $@"
            {FragmentShaderHeader}
            in vec4 pixelColor;
            flat in uint scrH;
            flat in uint blurR;
            flat in ivec2 blurOuterSize;
            flat in ivec2 rectOrigin;
             
            float blur()
            {{
                if (blurR == 0u)
                    return 1.0f; // no blur -> no alpha change

                float r = float(blurR);
                float w = float(blurOuterSize.x);
                float h = float(blurOuterSize.y);
                float x = abs(gl_FragCoord.x - float(rectOrigin.x));
                float y = abs(float(scrH) - gl_FragCoord.y - float(rectOrigin.y));
                float wi = w - 2.0f * r;
                float hi = h - 2.0f * r;

                if (x <= wi * 0.5f && y <= hi * 0.5f)
                    return 1.0f;
                if (x > w * 0.5f && y > h * 0.5f)
                    discard;

                float xDist = x - wi * 0.5f;
                float yDist = y - hi * 0.5f;
                return pow(1.0f - max(xDist, yDist) / r, 2.0f);
            }}
            
            void main()
            {{
                {FragmentOutColorName} = vec4(pixelColor.rgb, pixelColor.a * blur());
            }}
        ";

        private static readonly string BlurRectVertexShader =
        @$"
            {VertexShaderHeader}
            in ivec2 {PositionName};
            in ivec2 {SizeName};
            in uint {LayerName};
            in uvec4 {ColorName};
            in uint {BlurRadiusName};
            in ivec2 {OriginName};
            uniform uint {ScreenHeightName};
            uniform mat4 {ProjectionMatrixName};
            uniform mat4 {ModelViewMatrixName};
            out vec4 pixelColor;
            flat out uint scrH;
            flat out uint blurR;
            flat out ivec2 blurOuterSize;
            flat out ivec2 rectOrigin;
            
            void main()
            {{
                vec2 pos = vec2(float({PositionName}.x) + 0.49f, float({PositionName}.y) + 0.49f);
                pixelColor = vec4({ColorName}.r / 255.0f, {ColorName}.g / 255.0f, {ColorName}.b / 255.0f, {ColorName}.a / 255.0f);
                scrH = {ScreenHeightName};
                blurR = {BlurRadiusName};
                blurOuterSize = {SizeName};
                rectOrigin = {OriginName};
                gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, 1.0f - float({LayerName}) * 0.00001f, 1.0f);
            }}
        ";

        public BlurRectShader()
            : base(BlurRectFragmentShader, BlurRectVertexShader)
        {

        }

        public void SetScreenHeight(uint height)
        {
            _shaderProgram.SetInput(ScreenHeightName, height);
        }

        public static BlurRectShader Instance
        {
            get
            {
                if (_blurRectShader == null)
                    _blurRectShader = new BlurRectShader();

                return _blurRectShader;
            }
        }
    }
}
