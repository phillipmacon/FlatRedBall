using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math.Geometry
{
    #region XML Docs
    /// <summary>
    /// A rectangle class using floats for its bounds.  
    /// </summary>
    #endregion
    public class FloatRectangle : IEquatable<FloatRectangle>
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// A Rectangle with its top-left point at (0,0) with a width and height of 1.
        /// </summary>
        #endregion
        public static FloatRectangle Default = new FloatRectangle();
        public static FloatRectangle Invalid = new FloatRectangle(float.NaN, float.NaN, float.NaN, float.NaN);
        public float Top;
        public float Bottom;
        public float Left;
        public float Right;

        #endregion


        public FloatRectangle()
        {
            Top = 0;
            Bottom = 1;
            Left = 0;
            Right = 1;
        }

        public FloatRectangle(float top, float bottom, float left, float right)
        {
            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
        }

        public override string ToString()
        {
            return "Top:" + Top + " Left:" + Left + " Bottom:" + Bottom + " Right:" + Right;
        }

        #region IEquatable<FloatRectangle> Members

        public bool Equals(FloatRectangle other)
        {
            return this.Top == other.Top &&
                this.Bottom == other.Bottom &&
                this.Left == other.Left &&
                this.Right == other.Right;
        }

        #endregion
    }
}
