﻿using FlatRedBall.Gui;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Forms.Controls.Primitives
{
    public abstract class RangeBase : FrameworkElement
    {
        #region Fields/Properties

        protected Button thumb;

        protected GraphicalUiElement Track => thumb.Visual.EffectiveParentGue;

        /// <summary>
        /// Represents the X or Y offset of the cursor relative to the thumb when the thumb was grabbed.
        /// If the element is horizontal, this is an X value. If the element is vertical, this is a Y value.
        /// </summary>
        protected float cursorGrabOffsetRelativeToThumb = 0;

        public double LargeChange { get; set; }
        public double SmallChange { get; set; }

        double minimum = 0;
        public double Minimum
        {
            get { return minimum; }
            set
            {
                var oldValue = minimum;
                minimum = value;

                OnMinimumChanged(oldValue, minimum);
            }
        }

        double maximum = 1;
        public double Maximum
        {
            get { return maximum; }
            set
            {
                var oldValue = maximum;
                maximum = value;

                OnMaximumChanged(oldValue, maximum);
            }
        }

        double value;
        public double Value
        {
            get
            {
                return value;
            }
            set
            {
#if DEBUG
                if(double.IsNaN(value))
                {
                    throw new InvalidOperationException("Can't set the ScrollBar Value to NaN");
                }
#endif
                var oldValue = this.value;
                if(oldValue != value)
                {

                    this.value = value;

                    this.value = System.Math.Min(this.value, Maximum);
                    this.value = System.Math.Max(this.value, Minimum);


                    OnValueChanged(oldValue, this.value);

                    ValueChanged?.Invoke(this, null);
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler ValueChanged;

        #endregion

        #region Initialize

        public RangeBase() : base() { }

        public RangeBase(GraphicalUiElement visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();

            var thumbVisual = this.Visual.GetGraphicalUiElementByName("ThumbInstance");
#if DEBUG
            if(thumbVisual == null)
            {
                throw new Exception($"The {this.GetType().Name} Gum object must have a button called ThumbInstance");

            }
#endif

            if(thumbVisual.FormsControlAsObject == null)
            {
                thumb = new Button(thumbVisual);
            }
            else
            {
                thumb = thumbVisual.FormsControlAsObject as Button;
            }
            thumb.Push += HandleThumbPush;
            thumb.Visual.DragOver += HandleThumbRollOver;
            // do this before assigning any values like Minimum, Maximum
            var thumbHeight = thumb.ActualHeight;

            Visual.RollOver += HandleTrackRollOver;

            // read the height values and infer the Value and ViewportSize based on a 0 - 100

            Minimum = 0;
            Maximum = 100;
            SmallChange = 10;
            Value = 0;

        }

        #endregion

        protected abstract void HandleThumbPush(object sender, EventArgs e);

        private void HandleThumbRollOver(IWindow obj)
        {
            var cursor = GuiManager.Cursor;

            if (cursor.WindowPushed == thumb.Visual)
            {
                UpdateThumbPositionToCursorDrag(cursor);
            }
        }

        private void HandleTrackRollOver(IWindow window)
        {
            var cursor = GuiManager.Cursor;

            if (cursor.WindowPushed == thumb.Visual)
            {
                UpdateThumbPositionToCursorDrag(cursor);
            }
        }

        protected virtual void OnMaximumChanged(double oldMaximum, double newMaximum)
        {
            if(Value > Maximum && Maximum >= Minimum)
            {
                Value = Maximum;
            }
        }
        protected virtual void OnMinimumChanged(double oldMinimum, double newMinimum)
        {
            if(Value < Minimum && Minimum <= Maximum)
            {
                Value = Minimum;
            }
        }

        protected virtual void OnValueChanged(double oldValue, double newValue) { }

        protected abstract void UpdateThumbPositionToCursorDrag(Cursor cursor);

    }
}
