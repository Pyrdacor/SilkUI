using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal static class State
    {
        public static readonly int OpenGLVersionMajor = 0;
        public static readonly int OpenGLVersionMinor = 0;
        public static readonly int GLSLVersionMajor = 0;
        public static readonly int GLSLVersionMinor = 0;
        public static readonly GL Gl = null;
        public static bool ShadersAvailable => OpenGLVersionMajor >= 2 && GLSLVersionMajor > 0;

        private static Stack<Matrix4x4> _projectionMatrixStack = new Stack<Matrix4x4>();
        private static Stack<Matrix4x4> _modelViewMatrixStack = new Stack<Matrix4x4>();
        private static Stack<Matrix4x4> _unzoomedModelViewMatrixStack = new Stack<Matrix4x4>();

        static State()
        {
            Gl = GL.GetApi();

            var openGLVersion = Gl.GetString(StringName.Version).TrimStart();

            Regex versionRegex = new Regex(@"([0-9]+)\.([0-9]+)", RegexOptions.Compiled);

            var match = versionRegex.Match(openGLVersion);

            if (!match.Success || match.Index != 0 || match.Groups.Count < 3)
            {
                throw new Exception("OpenGL is not supported or the version could not be determined.");
            }

            OpenGLVersionMajor = int.Parse(match.Groups[1].Value);
            OpenGLVersionMinor = int.Parse(match.Groups[2].Value);

            if (OpenGLVersionMajor >= 2) // glsl is supported since OpenGL 2.0
            {
                var glslVersion = Gl.GetString(StringName.ShadingLanguageVersion);

                match = versionRegex.Match(glslVersion);

                if (match.Success && match.Index == 0 && match.Groups.Count >= 3)
                {
                    GLSLVersionMajor = int.Parse(match.Groups[1].Value);
                    GLSLVersionMinor = int.Parse(match.Groups[2].Value);
                }
            }
        }

        public static void PushProjectionMatrix(Matrix4x4 matrix)
        {
            _projectionMatrixStack.Push(matrix);
        }

        public static void PushModelViewMatrix(Matrix4x4 matrix)
        {
            _modelViewMatrixStack.Push(matrix);
        }

        public static void PushUnzoomedModelViewMatrix(Matrix4x4 matrix)
        {
            _unzoomedModelViewMatrixStack.Push(matrix);
        }

        public static Matrix4x4 PopProjectionMatrix()
        {
            return _projectionMatrixStack.Pop();
        }

        public static Matrix4x4 PopModelViewMatrix()
        {
            return _modelViewMatrixStack.Pop();
        }

        public static Matrix4x4 PopUnzoomedModelViewMatrix()
        {
            return _unzoomedModelViewMatrixStack.Pop();
        }

        public static void RestoreProjectionMatrix(Matrix4x4 matrix)
        {
            if (_projectionMatrixStack.Contains(matrix))
            {
                while (CurrentProjectionMatrix != matrix)
                    _projectionMatrixStack.Pop();
            }
            else
                PushProjectionMatrix(matrix);
        }

        public static void RestoreModelViewMatrix(Matrix4x4 matrix)
        {
            if (_modelViewMatrixStack.Contains(matrix))
            {
                while (CurrentModelViewMatrix != matrix)
                    _modelViewMatrixStack.Pop();
            }
            else
                PushModelViewMatrix(matrix);
        }

        public static void RestoreUnzoomedModelViewMatrix(Matrix4x4 matrix)
        {
            if (_unzoomedModelViewMatrixStack.Contains(matrix))
            {
                while (CurrentUnzoomedModelViewMatrix != matrix)
                    _unzoomedModelViewMatrixStack.Pop();
            }
            else
                PushUnzoomedModelViewMatrix(matrix);
        }

        public static void ClearMatrices()
        {
            _projectionMatrixStack.Clear();
            _modelViewMatrixStack.Clear();
        }

        public static Matrix4x4? CurrentProjectionMatrix => _projectionMatrixStack.Count == 0 ? (Matrix4x4?)null : _projectionMatrixStack.Peek();
        public static Matrix4x4? CurrentModelViewMatrix => _modelViewMatrixStack.Count == 0 ? (Matrix4x4?)null : _modelViewMatrixStack.Peek();
        public static Matrix4x4? CurrentUnzoomedModelViewMatrix => _unzoomedModelViewMatrixStack.Count == 0 ? (Matrix4x4?)null : _unzoomedModelViewMatrixStack.Peek();
    }
}
