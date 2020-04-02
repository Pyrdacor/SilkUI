using System.Drawing;
using System;
using System.Numerics;
using Silk.NET.OpenGL;

namespace SilkUI.Renderer.OpenGL
{
    internal class Context
    {
        private int _width = -1;
        private int _height = -1;
        private Rotation _rotation = Rotation.None;
        private Matrix4x4 _modelViewMatrix = Matrix4x4.Identity;
        private Matrix4x4 _unzoomedModelViewMatrix = Matrix4x4.Identity;
        private float _zoom = 0.0f;
        private Color _backgroundColor = Color.Gray;

        public Context(RenderDimensionReference dimensions)
        {
            // We need at least OpenGL 3.1 for instancing, shaders and primitive restart.
            if (State.OpenGLVersionMajor < 3 || (State.OpenGLVersionMajor == 3 && State.OpenGLVersionMinor < 1))
                throw new NotSupportedException($"OpenGL version 3.1 is required for rendering. Your version is {State.OpenGLVersionMajor}.{State.OpenGLVersionMinor}.");

            State.Gl.ClearColor(_backgroundColor);

            State.Gl.Enable(EnableCap.DepthTest);
            State.Gl.DepthFunc(DepthFunction.Lequal);

            State.Gl.Disable(EnableCap.Blend); // will be enabled later
            State.Gl.BlendEquationSeparate(GLEnum.FuncAdd, GLEnum.FuncAdd);
            State.Gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.Zero);

            Resize(dimensions.Width, dimensions.Height);

            dimensions.DimensionsChanged += () => Resize(dimensions.Width, dimensions.Height);
        }

        private static Matrix4x4 CreateOrtho2D(float left, float right, float top, float bottom, float near = -1.0f, float far = 1.0f)
        {
            // width
            float w = right - left;
            // height
            float h = top - bottom; // swap y so 0,0 for drawing is in the upper-left corner
            // depth
            float d = far - near;

            return new Matrix4x4()
            {
                M11 = 2.0f / w,   M12 = 0.0f,       M13 = 0.0f,       M14 = -(right + left) / w,
                M21 = 0.0f,       M22 = 2.0f / h,   M23 = 0.0f,       M24 = -(bottom + top) / h,
                M31 = 0.0f,       M32 = 0.0f,       M33 = 2.0f / d,   M34 = -(far + near) / d,
                M41 = 0.0f,       M42 = 0.0f,       M43 = 0.0f,       M44 = 1.0f
            };
        }

        public void Resize(int width, int height)
        {
            State.ClearMatrices();
            State.PushModelViewMatrix(Matrix4x4.Identity);
            State.PushUnzoomedModelViewMatrix(Matrix4x4.Identity);
            State.PushProjectionMatrix(CreateOrtho2D(0.0f, width, 0.0f, height, 0.0f, 1.0f));

            _width = width;
            _height = height;

            State.Gl.Viewport(new Size(_width, _height));

            SetRotation(_rotation, true);
        }

        public float Zoom
        {
            get => _zoom;
            set
            {
                if (Util.FloatEqual(value, _zoom) || value < 0.0f)
                    return;

                _zoom = value;

                ApplyMatrix();
            }
        }

        public Color BackgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor == value)
                    return;

                _backgroundColor = value;
                State.Gl.ClearColor(_backgroundColor);
            }
        }

        public void SetRotation(Rotation rotation, bool forceUpdate = false)
        {
            if (forceUpdate || rotation != _rotation)
            {
                _rotation = rotation;

                ApplyMatrix();
            }
        }

        void ApplyMatrix()
        {
            State.RestoreModelViewMatrix(_modelViewMatrix);
            State.PopModelViewMatrix();
            State.RestoreUnzoomedModelViewMatrix(_unzoomedModelViewMatrix);
            State.PopUnzoomedModelViewMatrix();

            if (_rotation == Rotation.None)
            {
                _modelViewMatrix = Matrix4x4.Identity;
            }
            else
            {
                var rotationDegree = 0.0f;

                switch (_rotation)
                {
                    case Rotation.Deg90:
                        rotationDegree = 90.0f;
                        break;
                    case Rotation.Deg180:
                        rotationDegree = 180.0f;
                        break;
                    case Rotation.Deg270:
                        rotationDegree = 270.0f;
                        break;
                    default:
                        break;
                }

                var x = 0.5f * _width;
                var y = 0.5f * _height;
                const float deg2rad = (float)(Math.PI / 180.0);

                if (_rotation != Rotation.Deg180) // 90° or 270°
                {
                    float factor = (float)_height / (float)_width;
                    _modelViewMatrix =
                        Matrix4x4.CreateTranslation(x, y, 0.0f) *
                        Matrix4x4.CreateRotationZ(rotationDegree * deg2rad) *
                        Matrix4x4.CreateScale(factor, 1.0f / factor, 1.0f) *
                        Matrix4x4.CreateTranslation(-x, -y, 0.0f);
                }
                else // 180°
                {
                    _modelViewMatrix =
                        Matrix4x4.CreateTranslation(x, y, 0.0f) *
                        Matrix4x4.CreateRotationZ(rotationDegree * deg2rad) *
                        Matrix4x4.CreateTranslation(-x, -y, 0.0f);
                }
            }

            _unzoomedModelViewMatrix = _modelViewMatrix;

            State.PushUnzoomedModelViewMatrix(_unzoomedModelViewMatrix);

            if (!Util.FloatEqual(_zoom, 0.0f))
            {
                var x = 0.5f * _width;
                var y = 0.5f * _height;

                _modelViewMatrix = Matrix4x4.CreateTranslation(x, y, 0.0f) *
                    Matrix4x4.CreateScale(1.0f + _zoom * 0.5f, 1.0f + _zoom * 0.5f, 1.0f) *
                    Matrix4x4.CreateTranslation(-x, -y, 0.0f) *
                    _modelViewMatrix;
            }

            State.PushModelViewMatrix(_modelViewMatrix);
        }
    }
}
