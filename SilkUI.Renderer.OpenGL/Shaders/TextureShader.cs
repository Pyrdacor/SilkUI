﻿namespace SilkUI.Renderer.OpenGL.Shaders
{
    internal class TextureShader : ShaderBase
    {
        private static TextureShader _textureShader = null;

        private static readonly string TextureFragmentShader =
        $@"
            {FragmentShaderHeader}
            uniform vec3 { ColorKeyName } = vec3(1, 0, 1);
            uniform vec4 { ColorName } = vec4(1, 1, 1, 1);
            uniform sampler2D { SamplerName };
            in vec2 varTexCoord;
            
            void main()
            {{
                vec4 pixelColor = texture({SamplerName}, varTexCoord);
                
                if (pixelColor.r == {ColorKeyName}.r && pixelColor.g == {ColorKeyName}.g && pixelColor.b == {ColorKeyName}.b)
                    discard;
                
                {FragmentOutColorName} = pixelColor * {ColorName};
            }}
        ";

        private static readonly string TextureVertexShader =
        $@"
            {VertexShaderHeader}
            in ivec2 {PositionName};
            in ivec2 {TexCoordName};
            in uint {LayerName};
            uniform float {ZName};
            uniform mat4 {ProjectionMatrixName};
            uniform mat4 {ModelViewMatrixName};
            out vec2 varTexCoord;
            
            void main()
            {{
                vec2 pos = vec2(float({PositionName}.x) + 0.49f, float({PositionName}.y) + 0.49f);
                varTexCoord = vec2({TexCoordName}.x, {TexCoordName}.y);
                gl_Position = {ProjectionMatrixName} * {ModelViewMatrixName} * vec4(pos, 1.0f - {ZName} - float({LayerName}) * 0.00001f, 1.0f);
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

        public void SetColorOverlay(float r, float g, float b, float a)
        {
            _shaderProgram.SetInputVector4(ColorName, r, g, b, a);
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
