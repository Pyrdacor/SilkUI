namespace SilkUI.Renderer.OpenGL
{
    internal class TextureShader : ColorShader
    {
        protected static string TextureLookupFunction =>
            State.GLSLVersionMajor == 1 || State.GLSLVersionMinor < 3 ? "texture2D" : "texture";

        private static TextureShader _textureShader = null;
        internal static readonly string DefaultTexCoordName = "texCoord";
        internal static readonly string DefaultSamplerName = "sampler";
        internal static readonly string DefaultColorKeyName = "colorKey";
        internal static readonly string DefaultColorOverlayName = "color";
        internal static readonly string DefaultAtlasSizeName = "atlasSize";

        private readonly string _texCoordName;
        private readonly string _samplerName;
        private readonly string _colorKeyName;
        private readonly string _colorOverlayName;
        private readonly string _atlasSizeName;

        private static readonly string[] TextureFragmentShader = new string[]
        {
            GetFragmentShaderHeader(),
            $"uniform vec3 {DefaultColorKeyName} = vec3(1, 0, 1);",
            $"uniform vec4 {DefaultColorOverlayName} = vec4(1, 1, 1, 1);",
            $"uniform sampler2D {DefaultSamplerName};",
            $"{GetInName(true)} vec2 varTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec4 pixelColor = {TextureLookupFunction}({DefaultSamplerName}, varTexCoord);",
            $"    ",
            $"    if (pixelColor.r == {DefaultColorKeyName}.r && pixelColor.g == {DefaultColorKeyName}.g && pixelColor.b == {DefaultColorKeyName}.b)",
            $"        pixelColor.a = 0;",
            $"    else",
            $"        pixelColor *= {DefaultColorOverlayName};",
            $"    ",
            $"    if (pixelColor.a < 0.5)",
            $"        discard;",
            $"    else",
            $"        {(HasGLFragColor ? "gl_FragColor" : DefaultFragmentOutColorName)} = pixelColor;",
            $"}}"
        };

        private static readonly string[] TextureVertexShader = new string[]
        {
            GetVertexShaderHeader(),
            $"{GetInName(false)} ivec2 {DefaultPositionName};",
            $"{GetInName(false)} ivec2 {DefaultTexCoordName};",
            $"{GetInName(false)} uint {DefaultLayerName};",
            $"uniform uvec2 {DefaultAtlasSizeName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"{GetOutName()} vec2 varTexCoord;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 atlasFactor = vec2(1.0f / {DefaultAtlasSizeName}.x, 1.0f / {DefaultAtlasSizeName}.y);",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    varTexCoord = vec2({DefaultTexCoordName}.x, {DefaultTexCoordName}.y);",
            $"    ",
            $"    varTexCoord *= atlasFactor;",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        TextureShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultZName, DefaultPositionName, 
                  DefaultTexCoordName, DefaultSamplerName, DefaultColorKeyName, DefaultColorOverlayName,
                  DefaultAtlasSizeName, DefaultLayerName, TextureFragmentShader, TextureVertexShader)
        {

        }

        protected TextureShader(string modelViewMatrixName, string projectionMatrixName, string zName,
            string positionName, string texCoordName, string samplerName, string colorKeyName, string colorOverlayName,
            string atlasSizeName, string layerName, string[] fragmentShaderLines, string[] vertexShaderLines)
            : base(modelViewMatrixName, projectionMatrixName, DefaultColorName, zName, positionName, layerName,
                fragmentShaderLines, vertexShaderLines)
        {
            _texCoordName = texCoordName;
            _samplerName = samplerName;
            _colorKeyName = colorKeyName;
            _colorOverlayName = colorOverlayName;
            _atlasSizeName = atlasSizeName;
        }

        public void SetSampler(int textureUnit = 0)
        {
            _shaderProgram.SetInput(_samplerName, textureUnit);
        }

        public void SetColorKey(float r, float g, float b)
        {
            _shaderProgram.SetInputVector3(_colorKeyName, r, g, b);
        }

        public void SetColorOverlay(float r, float g, float b, float a)
        {
            _shaderProgram.SetInputVector4(_colorOverlayName, r, g, b, a);
        }

        public void SetAtlasSize(uint width, uint height)
        {
            _shaderProgram.SetInputVector2(_atlasSizeName, width, height);
        }

        public new static TextureShader Instance
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
