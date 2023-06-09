﻿using GlueView.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using GlueView.Facades;
using GlueView.Forms;
using GlueView.EmbeddedPlugins.CameraControlsPlugin.ViewModels;
using System.ComponentModel;
using FlatRedBall.Content.Scene;
using FlatRedBall;

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin
{
    [Export(typeof(GlueViewPlugin))]
    public class MainPlugin : GlueViewPlugin
    {
        #region Fields/Properties

        BoundsLogic boundsLogic;
        CameraViewModel guidesViewModel;
        CameraControl cameraControl;

        double lastCameraRefresh = 0;
        double updateFrequency = 2;

        public override string FriendlyName
        {
            get
            {
                return "Camera Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        #endregion

        public override void StartUp()
        {
            boundsLogic = new CameraControlsPlugin.BoundsLogic();

            guidesViewModel = new CameraViewModel();
            guidesViewModel.PropertyChanged += HandleGuidesPropertyChanged;

            guidesViewModel.CellSize = 20;

            cameraControl = new CameraControl(guidesViewModel);

            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm(
                "Camera", -1, cameraControl, this);

            this.ElementLoaded += HandleElementLoaded;

            this.Update += HandleUpdate;
            this.ElementRemoved += HandleElementRemoved;
        }

        private void HandleElementRemoved()
        {
            PersistGuidesProperties();

            PersistCameraValues();

            base.SavePersistentDataForElements();
        }

        private void PersistGuidesProperties()
        {
            PersistentDataForCurrentElement.CellSize = guidesViewModel.CellSize;
            PersistentDataForCurrentElement.ShowOrigin = guidesViewModel.ShowOrigin;
            PersistentDataForCurrentElement.ShowGrid = guidesViewModel.ShowGrid;

        }

        private void PersistCameraValues()
        {
            CameraSave cameraSave = CameraSave.FromCamera(Camera.Main, false);
            PersistentDataForCurrentElement.CameraSave = cameraSave;

        }

        private void HandleUpdate(object sender, EventArgs e)
        {
            EditorObjects.CameraMethods.MouseCameraControl(FlatRedBall.Camera.Main);
            // This causes more problems than is worth
            //EditorObjects.CameraMethods.KeyboardCameraControl(SpriteManager.Camera);

            if(TimeManager.SecondsSince(lastCameraRefresh) > updateFrequency)
            {
                lastCameraRefresh = TimeManager.CurrentTime;
                cameraControl.UpdateDisplayedValues();
            }
        }

        private void HandleGuidesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CameraViewModel.ShowOrigin):
                    boundsLogic.ShowOrigin = guidesViewModel.ShowOrigin;
                    break;
                case nameof(CameraViewModel.ShowGrid):
                    boundsLogic.ShowGrid = guidesViewModel.ShowGrid;
                    break;
                case nameof(CameraViewModel.CellSize):
                    boundsLogic.CellSize = guidesViewModel.CellSize;
                    break;
            }
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        private void HandleElementLoaded(object sender, EventArgs e)
        {
            if (DataHasMember(PersistentDataForCurrentElement, "CellSize"))
            {
                guidesViewModel.CellSize = (int)PersistentDataForCurrentElement.CellSize;
                guidesViewModel.ShowOrigin = PersistentDataForCurrentElement.ShowOrigin;
                guidesViewModel.ShowGrid = PersistentDataForCurrentElement.ShowGrid;

                // Can't do a direct assignment because .CameraSave is deserialized as an expando object
                CameraSave cameraSave = new CameraSave();// PersistentDataForCurrentElement.CameraSave;

                cameraSave.AspectRatio = (float)PersistentDataForCurrentElement.CameraSave.AspectRatio;
                cameraSave.FarClipPlane = (float)PersistentDataForCurrentElement.CameraSave.FarClipPlane;
                cameraSave.NearClipPlane = (float)PersistentDataForCurrentElement.CameraSave.NearClipPlane;
                cameraSave.Orthogonal = PersistentDataForCurrentElement.CameraSave.Orthogonal;
                cameraSave.OrthogonalHeight = (float)PersistentDataForCurrentElement.CameraSave.OrthogonalHeight;
                cameraSave.OrthogonalWidth = (float)PersistentDataForCurrentElement.CameraSave.OrthogonalWidth;
                cameraSave.X = (float)PersistentDataForCurrentElement.CameraSave.X;
                cameraSave.Y = (float)PersistentDataForCurrentElement.CameraSave.Y;
                cameraSave.Z = (float)PersistentDataForCurrentElement.CameraSave.Z;


                cameraSave.SetCamera(Camera.Main);
            }
            boundsLogic.HandleElementLoaded();
        }
    }
}
