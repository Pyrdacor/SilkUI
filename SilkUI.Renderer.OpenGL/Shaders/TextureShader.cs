namespace SilkUI.Renderer.OpenGL.Shaders
{
    internal class TextureShader : ShaderBase
    {
        private static TextureShader _textureShader = null;

        private static readonly string TextureFragmentShader =
        $@"
            {FragmentShaderHeader}
            uniform vec3 { ColorKeyName } = vec3(1, 0, 1);
            uniform sampler2D { SamplerName };
            in vec2 varTexCoord;
            in vec4 overlayColor;
            in float visible;
            
            void main()
            {{
                if (visible < 0.5f)
                    discard;

                vec4 pixelColor = texture({SamplerName}, varTexCoord);
                
                if (pixelColor.r == {ColorKeyName}.r && pixelColor.g == {ColorKeyName}.g && pixelColor.b == {ColorKeyName}.b)
                    discard;
                
                {FragmentOutColorName} = pixelColor * overlayColor;
            }}
        ";

        private static readonly string TextureVertexShader =
        $@"
            {VertexShaderHeader}
            in ivec2 {PositionName};
            in ivec2 {TexCoordName};
            in uint {LayerName};
            in uvec4 {ColorName};
            in uint {ClipRectXName}; // Set to 0xffffffff to avoid clipping
            in uint {ClipRectYName};
            in ivec2 {ClipRectSizeName};
            uniform uvec2 {AtlasSizeName};
            uniform mat4 {ProjectionMatrixName};
            uniform mat4 {ModelViewMatrixName};
            out vec2 varTexCoord;
            out vec4 overlayColor;
            out float visible;

            bool clip(vec2 pos, vec4 clipRect)
            {{
                return  pos.x >= clipRect.x && pos.x <= clipRect.x + clipRect.z - 0.5f &&
                        pos.y >= clipRect.y && pos.y <= clipRect.y + clipRect.w - 0.5f;
            }}
            
            void main()
            {{
                vec2 atlasFactor = vec2(1.0f / {AtlasSizeName}.x, 1.0f / {AtlasSizeName}.y);
                vec2 pos = vec2(float({PositionName}.x) + 0.49f, float({PositionName}.y) + 0.49f);
                varTexCoord = vec2({TexCoordName}.x, {TexCoordName}.y);
                varTexCoord *= atlasFactor;
                overlayColor = vec4({ColorName}.r / 255.0f, {ColorName}.g / 255.0f, {ColorName}.b / 255.0f, {ColorName}.a / 255.0f);
                gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, 1.0f - float({LayerName}) * 0.00001f, 1.0f);
                visible = {ClipRectXName} == 0xffffffffu || clip(pos, vec4(vec2({ClipRectXName}, {ClipRectYName}), {ClipRectSizeName})) ? 1.0f : 0.0f;
            }}
        ";

        public TextureShader()
            : base(TextureFragmentShader, TextureVertexShader)
        {

        }

        public void SetSampler(int textureUnit = 0)
        {
            _shaderProgram.SetInput(SamplerName, textureUnit);
        }

        public void SetColorKey(float r, float g, float b)
        {
            _shaderProgram.SetInputVector3(ColorKeyName, r, g, b);
        }

        public void SetAtlasSize(uint width, uint height)
        {
            _shaderProgram.SetInputVector2(AtlasSizeName, width, height);
        }

        public static TextureShader Instance
        {
            get
            {
                if (_textureShader == null)
                    _textureShader = new TextureShader();

                return _textureShader;
            }
        }
    }
}
