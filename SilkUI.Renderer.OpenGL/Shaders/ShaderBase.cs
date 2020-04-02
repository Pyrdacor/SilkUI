using System;
using System.Numerics;

namespace SilkUI.Renderer.OpenGL.Shaders
{
    internal abstract class ShaderBase
    {
        internal static readonly string FragmentOutColorName = "outColor";
        internal static readonly string PositionName = "position";
        internal static readonly string ModelViewMatrixName = "mvMat";
        internal static readonly string ProjectionMatrixName = "projMat";
        internal static readonly string ZName = "z";
        internal static readonly string LayerName = "layer";
        internal static readonly string ColorName = "color";
        internal static readonly string OriginName = "origin";
        internal static readonly string SizeName = "size";
        internal static readonly string ScreenHeightName = "screenHeight";
        internal static readonly string BlurRadiusName = "blurRadius";
        internal static readonly string RoundnessName = "roundness";
        internal static readonly string SamplerName = "sampler";
        internal static readonly string TexCoordName = "texCoord";
        internal static readonly string ColorKeyName = "colorKey";
        internal static readonly string AtlasSizeName = "atlasSize";

        protected ShaderProgram _shaderProgram;

        protected static string FragmentShaderHeader =>
        @$"
            #version {State.GLSLVersionMajor}{State.GLSLVersionMinor}

            #ifdef GL_ES
                precision mediump float;
                precision highp int;
            #endif

            out vec4 {FragmentOutColorName};
        " + "\n\n";

        protected static string VertexShaderHeader =>
            $"#version {State.GLSLVersionMajor}{State.GLSLVersionMinor}\n\n";

        public void UpdateMatrices(bool zoom)
        {
            if (State.CurrentModelViewMatrix != null)
            {
                if (zoom)
                    _shaderProgram.SetInputMatrix(ModelViewMatrixName, State.CurrentModelViewMatrix.Value.ToArray(), true);
                else
                    _shaderProgram.SetInputMatrix(ModelViewMatrixName, State.CurrentUnzoomedModelViewMatrix.Value.ToArray(), true);
            }
            else
            {
                _shaderProgram.SetInputMatrix(ModelViewMatrixName, Matrix4x4.Identity.ToArray(), true);
            }

            if (State.CurrentProjectionMatrix == null)
                throw new InvalidOperationException("No projection matrix is set.");

            _shaderProgram.SetInputMatrix(ProjectionMatrixName, State.CurrentProjectionMatrix.Value.ToArray(), true);
        }

        public void Use()
        {
            if (_shaderProgram != ShaderProgram.ActiveProgram)
                _shaderProgram.Use();
        }

        protected ShaderBase(string fragmentShaderContent, string vertexShaderContent)
        {
            var fragmentShader = new Shader(Shader.Type.Fragment, fragmentShaderContent);
            var vertexShader = new Shader(Shader.Type.Vertex, vertexShaderContent);

            _shaderProgram = new ShaderProgram(fragmentShader, vertexShader);

            _shaderProgram.SetFragmentColorOutputName(FragmentOutColorName);
        }

        public ShaderProgram ShaderProgram => _shaderProgram;

        public void SetZ(float z)
        {
            _shaderProgram.SetInput(ZName, z);
        }
    }
}
