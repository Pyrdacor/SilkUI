using System;
using System.Collections.Generic;
using System.Drawing;

namespace SilkUI
{
    public abstract class Control
    {
        public const int MinWidth = 4;
        public const int MinHeight = 4;
        private Component _parent;
        protected bool NeedsRedraw { get; private set; } = true;
        internal virtual ControlRenderer ControlRenderer => Parent?.ControlRenderer;
        internal virtual InputEventManager InputEventManager => Parent?.InputEventManager;
        internal protected ControlStyle Style { get; } = new ControlStyle();


        #region Lifecycle Hooks

        public event EventHandler Init;
        public event EventHandler AfterContentInit;
        public event EventHandler AfterViewInit;
        public event RenderEventHandler Render;
        public event EventHandler Destroy;

        protected virtual void OnInit()
        {
            Init?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnAfterContentInit()
        {
            AfterContentInit?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnAfterViewInit()
        {
            AfterViewInit?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnDestroy()
        {
            Destroy?.Invoke(this, EventArgs.Empty);
        }

        #endregion


        #region Control Properties

        private readonly Dictionary<string, IControlProperty> _controlProperties = new Dictionary<string, IControlProperty>();        

        #region State

        private BoolProperty _visible = new BoolProperty(nameof(Visible), true);
        private BoolProperty _enabled = new BoolProperty(nameof(Enabled), true);
        private BoolProperty _hovered = new BoolProperty(nameof(Hovered), false);
        private BoolProperty _focused = new BoolProperty(nameof(Focused), false);

        public bool Visible
        {
            get
            {
                if (Parent == null && !(this is RootComponent))
                    return false;

                return _visible.Value ?? true;
            }
            set => _visible.Value = value;
        }
        public bool Enabled
        {
            get => _enabled.Value ?? true;
            set => _enabled.Value = value;
        }
        public bool Hovered
        {
            get => Enabled && Visible && _hovered.HasValue && _hovered.Value.Value;
            internal set => _hovered.Value = value && Enabled && Visible;
        }
        public bool Focused
        {
            get => Enabled && Visible && _focused.HasValue && _focused.Value.Value;
            internal set => _focused.Value = value && Enabled && Visible;
        }

        #endregion

        #region Metrics

        private readonly IntProperty _width = new IntProperty(nameof(Width), 0);
        private readonly IntProperty _height = new IntProperty(nameof(Height), 0);
        private readonly IntProperty _x = new IntProperty(nameof(X), 0);
        private readonly IntProperty _y = new IntProperty(nameof(Y), 0);

        public int X
        {
            get => _x.Value ?? 0;
            set => _x.Value = value;
        }

        public int Y
        {
            get => _y.Value ?? 0;
            set => _y.Value = value;
        }

        public int Width
        {
            get => _width.Value ?? 0;
            set => _width.Value = Math.Max(MinWidth, value);
        }

        public int Height
        {
            get => _height.Value ?? 0;
            set => _height.Value = Math.Max(MinHeight, value);
        }

        /// <summary>
        /// Location relative to the parent control.
        /// </summary>
        public Point Location
        {
            get => new Point(X, Y);
            set
            {
                int oldX = X;
                int oldY = Y;
                using (new DisableChangeEventContext(_x, _y))
                {
                    X = value.X;
                    Y = value.Y;
                }
                if (oldX != X || oldY != Y)
                    OnPositionChanged();
            }
        }

        /// <summary>
        /// Absolute location which is relative to the main view.
        /// </summary>
        public Point AbsoluteLocation
        {
            get => Parent == null ? Location : Parent.AbsoluteLocation.Add(Location);
        }

        public Size Size
        {
            get => new Size(Width, Height);
            set
            {
                int oldWidth = Width;
                int oldHeight = Height;
                using (new DisableChangeEventContext(_width, _height))
                {
                    Width = value.Width;
                    Height = value.Height;
                }
                if (oldWidth != Width || oldHeight != Height)
                    OnSizeChanged();
            }
        }

        /// <summary>
        /// Rectangular area relative to the parent control.
        /// </summary>
        public Rectangle ClientRectangle
        {
            get => new Rectangle(Location, Size);
            set
            {
                Location = value.Location;
                Size = value.Size;
            }
        }

        /// <summary>
        /// Absolute rectangle which is positioned relative to the main view.
        /// </summary>
        public Rectangle AbsoluteRectangle
        {
            get => new Rectangle(AbsoluteLocation, Size);
        }

        /// <summary>
        /// Absolute content rectangle without the border and with padding.
        /// </summary>
        public Rectangle AbsoluteContentRectangle
        {
            get
            {
                var absoluteRectangle = AbsoluteRectangle;
                var padding = Style.Get<AllDirectionStyleValue<int>>("padding", 0);

                int leftBorderSize = GetBorderSize(StyleDirection.Left);
                int topBorderSize = GetBorderSize(StyleDirection.Top);
                absoluteRectangle.Offset(padding.Left - leftBorderSize, padding .Top - topBorderSize);
                absoluteRectangle.Width += leftBorderSize + GetBorderSize(StyleDirection.Right) - padding.Left - padding.Right;
                absoluteRectangle.Height += topBorderSize + GetBorderSize(StyleDirection.Bottom) - padding.Top - padding.Bottom;

                return absoluteRectangle;
            }
        }

        /// <summary>
        /// Content rectangle without the border and with padding relative to the parent control.
        /// </summary>
        public Rectangle ContentRectangle
        {
            get
            {
                var contentRectangle = ClientRectangle;
                var padding = Style.Get<AllDirectionStyleValue<int>>("padding", 0);

                int leftBorderSize = GetBorderSize(StyleDirection.Left);
                int topBorderSize = GetBorderSize(StyleDirection.Top);
                contentRectangle.Offset(padding.Left - leftBorderSize, padding.Top - topBorderSize);
                contentRectangle.Width += leftBorderSize + GetBorderSize(StyleDirection.Right) - padding.Left - padding.Right;
                contentRectangle.Height += topBorderSize + GetBorderSize(StyleDirection.Bottom) - padding.Top - padding.Bottom;

                return contentRectangle;
            }
        }

        /// <summary>
        /// Width of the content in pixels.
        /// </summary>
        public int ContentWidth
        {
            get;
            set;
        }

        /// <summary>
        /// Height of the content in pixels.
        /// </summary>
        public int ContentHeight
        {
            get;
            set;
        }

        /// <summary>
        /// Updates the layout of this control and
        /// of all descendant controls if 'deep' is true.
        /// </summary>
        /// <param name="up"></param>
        protected void UpdateLayout(bool deep)
        {
            var widthDimension = Style.Get<Dimension>("width");
            var heightDimension = Style.Get<Dimension>("height");

            int width = (int)widthDimension.GetValue(Parent != null ? (uint)Parent.Width : 0u);
            int height = (int)heightDimension.GetValue(Parent != null ? (uint)Parent.Height : 0u);

            if (width != 0)
                Width = width;
            if (height != 0)
                Height = height;

            Rectangle? parentContentRect = Parent == null ? (Rectangle?)null : Parent.ContentRectangle;

            // TODO: add more and better placement/layout options through styles (flex, grid, etc)
            // TODO: should fill still use margins?
            if (widthDimension.DimensionType == Dimension.Type.Fill)
                X = parentContentRect.HasValue ? parentContentRect.Value.X : 0;
            if (heightDimension.DimensionType == Dimension.Type.Fill)
                Y = parentContentRect.HasValue ? parentContentRect.Value.Y : 0;

            if (deep)
                UpdateChildrenLayout(true, out _, out _);
        }

        private void UpdateChildrenLayout(bool deep, out int minX, out int minY)
        {
            minX = Width;
            minY = Height;
            int maxX = 0;
            int maxY = 0;

            foreach (var child in InternalChildren)
            {
                child.UpdateLayout(deep);

                var childRect = child.ClientRectangle;

                minX = Math.Min(minX, childRect.Left);
                minY = Math.Min(minY, childRect.Top);
                maxX = Math.Max(maxX, childRect.Right);
                maxY = Math.Max(maxY, childRect.Bottom);
            }

            ContentWidth = Math.Max(0, maxX - minX);
            ContentHeight = Math.Max(0, maxY - minY);
        }

        public void FitToContent()
        {
            UpdateChildrenLayout(true, out int minX, out int minY);
            Width = ContentWidth + Math.Max(0, Width - AbsoluteContentRectangle.Width);
            Height = ContentHeight + Math.Max(0, Height - AbsoluteContentRectangle.Height);

            foreach (var child in InternalChildren)
            {
                child.X -= minX;
                child.Y -= minY;
                child.UpdateLayout(false);
            }
        }

        public int GetBorderSize(StyleDirection direction)
        {
            var borderStyles = Style.Get<AllDirectionStyleValue<BorderLineStyle>>("border.linestyle", BorderLineStyle.None);
            var borderSizes = Style.Get<AllDirectionStyleValue<int>>("border.size", 0);

            return direction switch
            {
                StyleDirection.Top => GetBorderSize(borderStyles.Top, borderSizes.Top),
                StyleDirection.Right => GetBorderSize(borderStyles.Right, borderSizes.Right),
                StyleDirection.Bottom => GetBorderSize(borderStyles.Bottom, borderSizes.Bottom),
                StyleDirection.Left => GetBorderSize(borderStyles.Left, borderSizes.Left),
                _ => throw new ArgumentException($"Invalid style direction `{direction}`.")
            };
        }

        public int GetBorderSize(BorderLineStyle borderStyle, int borderSize)
        {
            return borderStyle switch
            {
                BorderLineStyle.None => 0,
                _ => borderSize
            };
        }

        protected void OnPositionChanged()
        {
            UpdateLayout(true);

            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected void OnSizeChanged()
        {
            UpdateLayout(true);

            SizeChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler PositionChanged;
        public event EventHandler SizeChanged;

        #endregion

        #endregion


        #region Events

        public event PropagatedEventHandler ParentChanged;
        public event PropagatedEventHandler VisibilityChanged;
        public event PropagatedEventHandler EnabledChanged;
        public event PropagatedEventHandler FocusedChanged;
        public event PropagatedEventHandler GainFocus;
        public event PropagatedEventHandler LostFocus;
        public event PropagatedEventHandler HoveredChanged;
        public event PropagatedEventHandler Enter;
        public event PropagatedEventHandler Leave;
        public event MouseMoveEventHandler MouseMove;
        public event MouseButtonEventHandler MouseDown;
        public event MouseButtonEventHandler MouseUp;
        public event MouseButtonEventHandler MouseClick;
        public event MouseButtonEventHandler MouseDoubleClick;
        public event MouseButtonEventHandler MouseDownOutside;
        public event MouseButtonEventHandler MouseUpOutside;
        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;


        internal virtual void OnParentChanged()
        {
            ParentChanged?.Invoke(this, new PropagatedEventArgs());
            CheckStyleChanges();
            UpdateLayout(true);
        }

        internal virtual void OnVisibilityChanged()
        {
            VisibilityChanged?.Invoke(this, new PropagatedEventArgs());
            CheckStyleChanges();
        }

        internal virtual void OnEnabledChanged()
        {
            EnabledChanged?.Invoke(this, new PropagatedEventArgs());
            CheckStyleChanges();
        }

        internal virtual void OnFocusedChanged()
        {
            FocusedChanged?.Invoke(this, new PropagatedEventArgs());

            if (Focused)
                GainFocus?.Invoke(this, new PropagatedEventArgs());
            else
                LostFocus?.Invoke(this, new PropagatedEventArgs());

            CheckStyleChanges();
        }

        internal virtual void OnHoveredChanged()
        {
            HoveredChanged?.Invoke(this, new PropagatedEventArgs());

            if (Hovered)
                Enter?.Invoke(this, new PropagatedEventArgs());
            else
                Leave?.Invoke(this, new PropagatedEventArgs());

            CheckStyleChanges();
        }

        internal void OnMouseMove(MouseMoveEventArgs args)
        {
            MouseMove?.Invoke(this, args);
        }

        internal void OnMouseDown(MouseButtonEventArgs args)
        {
            MouseDown?.Invoke(this, args);
        }

        internal void OnMouseUp(MouseButtonEventArgs args)
        {
            MouseUp?.Invoke(this, args);
        }

        internal void OnMouseClick(MouseButtonEventArgs args)
        {
            MouseClick?.Invoke(this, args);
        }

        internal void OnMouseDoubleClick(MouseButtonEventArgs args)
        {
            MouseDoubleClick?.Invoke(this, args);
        }

        internal void OnMouseUpOutside(MouseButtonEventArgs args)
        {
            MouseUpOutside?.Invoke(this, args);
        }

        internal void OnMouseDownOutside(MouseButtonEventArgs args)
        {
            MouseDownOutside?.Invoke(this, args);
        }

        internal void OnKeyDown(KeyEventArgs args)
        {
            KeyDown?.Invoke(this, args);
        }

        internal void OnKeyUp(KeyEventArgs args)
        {
            KeyUp?.Invoke(this, args);
        }

        #endregion


        public string Id { get; internal set; }
        public List<string> Classes { get; } = new List<string>();
        internal ControlList InternalChildren { get; }
        public Component Parent
        {
            get => _parent;
            internal set
            {
                if (_parent != value)
                {
                    _parent = value;
                    OnParentChanged();
                }
            }
        }
 
        protected Control(string id)
        {
            Id = id;
            InternalChildren = new ControlList(this);

            // Register control properties
            RegisterControlProperty(_x);
            RegisterControlProperty(_y);
            RegisterControlProperty(_width);
            RegisterControlProperty(_height);

            _x.ValueChanged += OnPositionChanged;
            _y.ValueChanged += OnPositionChanged;
            _width.ValueChanged += OnSizeChanged;
            _height.ValueChanged += OnSizeChanged;
        }

        internal void DestroyControl()
        {
            foreach (var child in InternalChildren)
                child.DestroyControl();

            OnDestroy();

            if (Parent != null)
                Parent.InternalChildren.Remove(this);

            _x.ValueChanged -= OnPositionChanged;
            _y.ValueChanged -= OnPositionChanged;
            _width.ValueChanged -= OnSizeChanged;
            _height.ValueChanged -= OnSizeChanged;

            InputEventManager?.UnregisterControl(this);

            DestroyView();
        }

        protected void RegisterControlProperty<T>(ControlProperty<T> property)
        {
            _controlProperties.Add(property.Name, property);
        }

        protected virtual void OnRender(RenderEventArgs args)
        {

        }

        internal void InitControl()
        {
            InputEventManager?.RegisterControl(this);

            // TODO
            OnInit();

            _visible.InternalValueChanged += OnVisibilityChanged;
            _enabled.InternalValueChanged += OnEnabledChanged;
            _focused.InternalValueChanged += OnFocusedChanged;
            _hovered.InternalValueChanged += OnHoveredChanged;

            OnAfterContentInit();

            InitView();

            foreach (var child in InternalChildren)
                child.InitControl();

            OnAfterViewInit();
        }

        internal virtual void InitView()
        {
            // Note: For all children InitView is called as well
            // after this. So only update the control's layout!
            UpdateLayout(false);
        }

        internal virtual void DestroyView()
        {

        }

        public void Invalidate()
        {
            foreach (var child in InternalChildren)
                child.Invalidate();

            ControlRenderer.ForceRedraw = true;
            NeedsRedraw = true;
        }

        internal void RenderControl(RenderEventArgs args = null)
        {
            if (!Visible)
                return;

            args ??= new RenderEventArgs(ControlRenderer);

            if (!NeedsRedraw)
            {
                ControlRenderer.SkipControlDrawing(this);
                RenderChildControls(args);
                return;
            }
           
            OnRender(args);
            Render?.Invoke(this, args);
            RenderChildControls(args);

            if (NeedsRedraw && (Parent == null || !Parent.NeedsRedraw))
                ResetInvalidation();
        }

        /// <summary>
        /// Converts an absolute position (= relative to the root component)
        /// to a position relative to the client area of the control.
        /// </summary>
        public Point PointToClient(Point absolutePosition)
        {
            return absolutePosition.Sub(AbsoluteLocation);
        }

        /// <summary>
        /// Converts a client position (= relative to the control's client area)
        /// to an absolute position which is relative to the root component.
        /// </summary>
        public Point PointFromClient(Point clientPosition)
        {
            return clientPosition.Add(AbsoluteLocation);
        }

        /// <summary>
        /// Checks if the given position is inside the control.
        /// The position is considered to be in relation to the client area.
        /// </summary>
        public bool ContainsPoint(Point position)
        {
            return new Rectangle(Point.Empty, Size).Contains(position);
        }

        protected void RenderChildControls(RenderEventArgs args)
        {
            foreach (var child in InternalChildren)
                child.RenderControl(args);
        }

        /// <summary>
        /// Retrieves the child at the given position.
        /// </summary>
        /// <param name="position">The position relative to the control's client area.</param>
        /// <param name="deepSearch">If false only direct children are considered otherwise all descendants.</param>
        /// <returns>The child control or null if none is found at the position.</returns>
        public Control GetChildAtPoint(Point position, bool deepSearch, bool allowDisabled = true)
        {
            return GetChildAtAbsolutePosition(PointFromClient(position), deepSearch, allowDisabled);
        }

        /// <summary>
        /// Retrieves the child at the given absolute position.
        /// </summary>
        /// <param name="position">The position relative to the root component.</param>
        /// <param name="deepSearch">If false only direct children are considered otherwise all descendants.</param>
        /// <returns>The child control or null if none is found at the position.</returns>
        public Control GetChildAtAbsolutePosition(Point position, bool deepSearch, bool allowDisabled = true)
        {
            foreach (var child in InternalChildren)
            {
                if (child.Visible && (child.Enabled || allowDisabled) && child.AbsoluteRectangle.Contains(position))
                {
                    if (deepSearch)
                        return child.GetChildAtAbsolutePosition(position, true) ?? child;
                    else
                        return child;
                }
            }

            return null;
        }

        protected void ResetInvalidation()
        {
            foreach (var child in InternalChildren)
                child.ResetInvalidation();

            ControlRenderer.ForceRedraw = false;
            NeedsRedraw = false;
        }

        internal abstract void CheckStyleChanges();

        protected void OverrideStyle<T>(string name, T value)
        {
            // AllDirectionStyleValue<ColorValue> will fail to convert from/to System.Drawing.Color
            // so we will convert colors to a ColorValue here.
            if (typeof(T) == typeof(System.Drawing.Color))
            {
                OverrideStyle(name, new ColorValue((Color)(object)value));
                return;
            }

            Style.SetProperty(name, value, true);
        }

        protected void OverrideStyleIfUndefined<T>(string name, T value)
        {
            // AllDirectionStyleValue<ColorValue> will fail to convert from/to System.Drawing.Color
            // so we will convert colors to a ColorValue here.
            if (typeof(T) == typeof(System.Drawing.Color))
            {
                OverrideStyleIfUndefined(name, new ColorValue((Color)(object)value));
                return;
            }

            Style.SetProperty(name, Style.GetFromStyle<T>(name, value), false);
        }
    }

    public static class ControlExtensions
    {
        public static Control WithClasses(this Control control, params string[] classes)
        {
            control.Classes.AddRange(classes);
            return control;
        }

        public static void AddTo(this Control control, Component component)
        {
            component.InternalChildren.Add(control);
            control.Parent = component;
        }
    }
}
