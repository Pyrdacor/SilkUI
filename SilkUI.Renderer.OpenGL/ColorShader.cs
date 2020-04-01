using System;
using System.Numerics;
namespace SilkUI.Renderer.OpenGL
{
    internal class ColorShader
    {
        private static ColorShader _colorShader = null;
        internal static readonly string DefaultFragmentOutColorName = "outColor";
        internal static readonly string DefaultPositionName = "position";
        internal static readonly string DefaultModelViewMatrixName = "mvMat";
        internal static readonly string DefaultProjectionMatrixName = "projMat";
        internal static readonly string DefaultColorName = "color";
        internal static readonly string DefaultZName = "z";
        internal static readonly string DefaultLayerName = "layer";

        protected ShaderProgram _shaderProgram;
        private readonly string _fragmentOutColorName;
        private readonly string _modelViewMatrixName;
        private readonly string _projectionMatrixName;
        private readonly string _colorName;
        private readonly string _zName;
        private readonly string _positionName;
        private readonly string _layerName;

        // gl_FragColor is deprecated beginning in GLSL version 1.30
        protected static bool HasGLFragColor =>
            State.GLSLVersionMajor == 1 && State.GLSLVersionMinor < 3;

        protected static string GetFragmentShaderHeader()
        {
            string header = $"#version {State.GLSLVersionMajor}{State.GLSLVersionMinor}\n";

            header += "\n";
            header += "#ifdef GL_ES\n";
            header += " precision mediump float;\n";
            header += " precision highp int;\n";
            header += "#endif\n";
            header += "\n";
            
            if (!HasGLFragColor)
                header += $"out vec4 {DefaultFragmentOutColorName};\n";

            return header;
        }

        protected static string GetVertexShaderHeader()
        {
            return $"#version {State.GLSLVersionMajor}{State.GLSLVersionMinor}\n\n";
        }

        protected static string GetInName(bool fragment)
        {
            if (State.GLSLVersionMajor == 1 && State.GLSLVersionMinor < 3)
            {
                if (fragment)
                    return "varying";
                else
                    return "attribute";
            }
            else
                return "in";
        }

        protected static string GetOutName()
        {
            if (State.GLSLVersionMajor == 1 && State.GLSLVersionMinor < 3)
                return "varying";
            else
                return "out";
        }

        private static readonly string[] ColorFragmentShader = new string[]
        {
            GetFragmentShaderHeader(),
            $"{GetInName(true)} vec4 pixelColor;",
            $"",
            $"void main()",
            $"{{",
            $"    {(HasGLFragColor ? "gl_FragColor" : DefaultFragmentOutColorName)} = pixelColor;",
            $"}}"
        };

        private static readonly string[] ColorVertexShader = new string[]
        {
            GetVertexShaderHeader(),
            $"{GetInName(false)} ivec2 {DefaultPositionName};",
            $"{GetInName(false)} uint {DefaultLayerName};",
            $"{GetInName(false)} uvec4 {DefaultColorName};",
            $"uniform float {DefaultZName};",
            $"uniform mat4 {DefaultProjectionMatrixName};",
            $"uniform mat4 {DefaultModelViewMatrixName};",
            $"{GetOutName()} vec4 pixelColor;",
            $"",
            $"void main()",
            $"{{",
            $"    vec2 pos = vec2(float({DefaultPositionName}.x) + 0.49f, float({DefaultPositionName}.y) + 0.49f);",
            $"    pixelColor = vec4({DefaultColorName}.r / 255.0f, {DefaultColorName}.g / 255.0f, {DefaultColorName}.b / 255.0f, {DefaultColorName}.a / 255.0f);",
            $"    ",
            $"    gl_Position = {DefaultProjectionMatrixName} * {DefaultModelViewMatrixName} * vec4(pos, 1.0f - {DefaultZName} - float({DefaultLayerName}) * 0.00001f, 1.0f);",
            $"}}"
        };

        public void UpdateMatrices(bool zoom)
        {
            if (State.CurrentModelViewMatrix != null)
            {
                if (zoom)
                    _shaderProgram.SetInputMatrix(_modelViewMatrixName, State.CurrentModelViewMatrix.Value.ToArray(), true);
                else
                    _shaderProgram.SetInputMatrix(_modelViewMatrixName, State.CurrentUnzoomedModelViewMatrix.Value.ToArray(), true);
            }
            else
            {
                _shaderProgram.SetInputMatrix(_modelViewMatrixName, Matrix4x4.Identity.ToArray(), true);
            }

            if (State.CurrentProjectionMatrix == null)
                throw new InvalidOperationException("No projection matrix is set.");

            _shaderProgram.SetInputMatrix(_projectionMatrixName, State.CurrentProjectionMatrix.Value.ToArray(), true);
        }

        public void Use()
        {
            if (_shaderProgram != ShaderProgram.ActiveProgram)
                _shaderProgram.Use();
        }

        ColorShader()
            : this(DefaultModelViewMatrixName, DefaultProjectionMatrixName, DefaultColorName, DefaultZName,
                  DefaultPositionName, DefaultLayerName, ColorFragmentShader, ColorVertexShader)
        {

        }

        protected ColorShader(string modelViewMatrixName, string projectionMatrixName, string colorName, string zName,
            string positionName, string layerName, string[] fragmentShaderLines, string[] vertexShaderLines)
        {
            _fragmentOutColorName = (State.OpenGLVersionMajor > 2) ? DefaultFragmentOutColorName : "gl_FragColor";

            _modelViewMatrixName = modelViewMatrixName;
            _projectionMatrixName = projectionMatrixName;
            _colorName = colorName;
            _zName = zName;
            _positionName = positionName;
            _layerName = layerName;

            var fragmentShader = new Shader(Shader.Type.Fragment, string.Join("\n", fragmentShaderLines));
            var vertexShader = new Shader(Shader.Type.Vertex, string.Join("\n", vertexShaderLines));

            _shaderProgram = new ShaderProgram(fragmentShader, vertexShader);

            _shaderProgram.SetFragmentColorOutputName(_fragmentOutColorName);
        }

        public ShaderProgram ShaderProgram => _shaderProgram;

        public void SetZ(float z)
        {
            _shaderProgram.SetInput(_zName, z);
        }

        public static ColorShader Instance
        {
            get
            {
                if (_colorShader == null)
                    _colorShader = new ColorShader();

                return _colorShader;
            }
        }
    }
}
