namespace SilkUI.Renderer.OpenGL.Shaders
{
    internal class PolygonShader : ShaderBase
    {
        private static PolygonShader _polygonShader = null;

        private static readonly string PolygonFragmentShader =
        $@"
            {FragmentShaderHeader}
            in vec4 pixelColor;

            void main()
            {{
                {FragmentOutColorName} = pixelColor;
            }}
        ";

        private static readonly string PolygonVertexShader =
        $@"
            {VertexShaderHeader}
            in ivec2 {PositionName};
            in uint {LayerName};
            in uvec4 {ColorName};
            uniform mat4 {ProjectionMatrixName};
            uniform mat4 {ModelViewMatrixName};
            out vec4 pixelColor;
            void main()
            {{
                vec2 pos = vec2(float({PositionName}.x) + 0.49f, float({PositionName}.y) + 0.49f);
                pixelColor = vec4({ColorName}.r / 255.0f, {ColorName}.g / 255.0f, {ColorName}.b / 255.0f, {ColorName}.a / 255.0f);
                gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, 1.0f - float({LayerName}) * 0.00001f, 1.0f);
            }}
        ";

        public PolygonShader()
            : base(PolygonFragmentShader, PolygonVertexShader)
        {

        }

        public static PolygonShader Instance
        {
            get
            {
                if (_polygonShader == null)
                    _polygonShader = new PolygonShader();

                return _polygonShader;
            }
        }
    }
}
