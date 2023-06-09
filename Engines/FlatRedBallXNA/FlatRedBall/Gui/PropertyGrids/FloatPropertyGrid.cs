﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Gui.PropertyGrids
{
    #region XML Docs
    /// <summary>
    /// A StructReferencePropertyGrid used by the FlatRedBall Engine when editing floats in a ListDisplayWindow.
    /// </summary>
    #endregion
    public class FloatPropertyGrid : StructReferencePropertyGrid<float>
    {
        #region Fields

        UpDown mUpDown;

        bool mCloseOnEnter = true;

        #endregion

        #region Properties

        public bool CloseOnEnter
        {
            get { return mCloseOnEnter; }
            set { mCloseOnEnter = value; }
        }

        public override float SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;
                if (mUpDown != null &&
                    (GuiManager.Cursor.WindowPushed != mUpDown.UpDownButton))
                {
                    mUpDown.CurrentValue = value;
                }
            }
        }

        #endregion

        #region Event Methods

        private void ChangeFloat(Window callingWindow)
        {
            if (mUpDown != null)
            {
                float outputValue = (float)mUpDown.CurrentValue;
                SelectedObject = outputValue;
                UpdateObject(null);
            }
        }

        private void ChangeFloatAndClose(Window callingWindow)
        {
            ChangeFloat(callingWindow);

            CloseWindow();
        }

        #endregion

        #region Methods

        public FloatPropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor, windowOfObject, indexOfObject)
        {
            ExcludeAllMembers();
            MinimumScaleY = 2;
            ScaleY = 2;
            mUpDown = new UpDown(mCursor);
            mUpDown.ScaleX = 5;
            mUpDown.Precision = 7;

            this.AddWindow(mUpDown);

            mUpDown.ValueChanged += ChangeFloat;

            mUpDown.textBox.EnterPressed += ChangeFloatAndClose;
        }

        #endregion
    }
}
