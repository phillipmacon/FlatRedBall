﻿using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Glue;
using GlueFormsCore.Controls;
using OfficialPlugins.Compiler;
using OfficialPlugins.Compiler.CommandSending;
using OfficialPlugins.Compiler.Managers;
using OfficialPlugins.Compiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPlugins.GameHost.Views
{
    /// <summary>
    /// Interaction logic for GameHostView.xaml
    /// </summary>
    public partial class GameHostView : UserControl
    {
        #region DLLImports
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, UInt32 dwNewLong);

        // from https://docs.microsoft.com/en-us/dotnet/desktop/wpf/advanced/walkthrough-hosting-a-win32-control-in-wpf?view=netframeworkdesktop-4.8
        internal const int
          WS_CHILD = 0x40000000,
          WS_VISIBLE = 0x10000000,
          LBS_NOTIFY = 0x00000001,
          HOST_ID = 0x00000002,
          LISTBOX_ID = 0x00000001,
          WS_VSCROLL = 0x00200000,
          WS_BORDER = 0x00800000;

        [DllImport("user32.dll")]
        static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        #endregion

        #region Fields/Properties

        System.Windows.Forms.Panel winformsPanel;

        CompilerViewModel ViewModel => DataContext as CompilerViewModel;

        // It seems like once the game is moved, we can't get the handle to this again. Not sure why, but it's
        // simple enough to hold on to it
        IntPtr gameHandle;


        #endregion

        #region Events

        public event EventHandler DoItClicked;


        public event EventHandler StopClicked;
        public event EventHandler RestartGameClicked;
        public event EventHandler RestartGameCurrentScreenClicked;
        public event EventHandler RestartScreenClicked;
        public event EventHandler AdvanceOneFrameClicked;
        public event EventHandler PauseClicked;
        public event EventHandler UnpauseClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler FocusOnSelectedObjectClicked;
        public event EventHandler StartInEditModeClicked;
        #endregion

        public GameHostView()
        {
            InitializeComponent();


            winformsPanel = new System.Windows.Forms.Panel();
            winformsPanel.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            winformsPanel.Width = 20;
            winformsPanel.Height = 30;
            winformsPanel.BorderStyle = System.Windows.Forms.BorderStyle.None;

            this.WinformsHost.Child = winformsPanel;

            DataContextChanged += GameHostView_DataContextChanged;
        }

        private void GameHostView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel != null)
            {
                //ViewModel.PropertyChanged += async (sender, args) =>
                //{
                //    //switch (args.PropertyName)
                //    //{
                //    //    //case nameof(ViewModel.GameWindowHeight):
                //    //    //    await RefreshLeftPanelSize();
                //    //    //    break;
                //    //}
                //};
            }
        }

        private void WinformsHost_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
        }

        private async void WinformsHost_Drop(object sender, DragEventArgs e)
        {
            // this doesn't work due to the airspace problem in wpf
            ////https://stackoverflow.com/questions/5978917/render-wpf-control-on-top-of-windowsformshost/5979041#5979041
            //var vm = GlueState.Self.DraggedTreeNode;

            //if (vm != null && ViewModel.IsRunning)
            //{
            //    await DragDropManagerGameWindow.HandleDragDropOnGameWindow(vm);
            //}
        }

        public async Task EmbedHwnd(IntPtr handle)
        {
            SetParent(handle, winformsPanel.Handle);
            gameHandle = handle;
            var succeededMakingGameBorderless = await MakeGameBorderless();

            if(succeededMakingGameBorderless)
            {
                WindowRectangle rectangle = new WindowRectangle();

                WindowMover.GetWindowRect(handle, out rectangle);

                // I used to have this code check if the window was at 0,0,
                // but that doesn't seem to actually work - the loop would run 
                // indefinitely, continually changing the position of the cursor.
                // Now I just do it 5 times and it seems to work
                for (int i = 0; i < 5; i++)
                {
                    var delay = 180;
                    await Task.Delay(delay);

                    var width = (int)WinformsHost.ActualWidth;
                    var height = (int)WinformsHost.ActualHeight;

                    WindowMover.MoveWindow(handle, 0, 0, width, height, true);
                }
            }
        }

        private static async Task<bool> MakeGameBorderless()
        {
            var attemptsToConnect = 0;
            int maxAttempts = 6;
            bool succeeded = false;
            var dto = new Compiler.Dtos.SetBorderlessDto { IsBorderless = true };

            do
            {
                var sendResponse = await CommandSender.Send(dto);
                var response = sendResponse.Succeeded ? sendResponse.Data : string.Empty;

                succeeded = !string.IsNullOrWhiteSpace(response);

                if (!succeeded)
                {
                    await Task.Delay(15);
                    attemptsToConnect++;
                }
            } while (succeeded == false && attemptsToConnect < maxAttempts);

            return succeeded;
        }

        public void AddChild(UIElement child)
        {
            MainGrid.Children.Add(child);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DoItClicked(this, null);
        }

        private void WhileRunningView_StopClicked(object sender, EventArgs e)
        {
            StopClicked?.Invoke(this, null);
        }

        private void WhileRunningView_RestartGameClicked(object sender, EventArgs e)
        {
            RestartGameClicked?.Invoke(this, null);
        }


        private void WinformsHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (ViewModel.IsRunning && ViewModel.IsGenerateGlueControlManagerInGame1Checked && gameHandle != IntPtr.Zero)
            {
                var newWidth = (int)WinformsHost.ActualWidth;
                var newHeight = (int)WinformsHost.ActualHeight;

                WindowMover.MoveWindow(gameHandle, 0, 0, newWidth, newHeight, true);
            }

        }

        private void WhileRunningView_RestartGameCurrentScreenClicked(object sender, EventArgs e)
        {
            RestartGameCurrentScreenClicked?.Invoke(this, null);
        }

        private void WhileRunningView_RestartScreenClicked(object sender, EventArgs e)
        {
            RestartScreenClicked?.Invoke(this, null);
        }

        private void WhileRunningView_AdvanceOneFrameClicked(object sender, EventArgs e)
        {
            AdvanceOneFrameClicked?.Invoke(this, null);
        }

        private void FocusButtonClicked(object sender, RoutedEventArgs e)
        {
            FocusOnSelectedObjectClicked?.Invoke(this, null);
        }

        private void PlayInEditModeClicked(object sender, RoutedEventArgs e)
        {
            StartInEditModeClicked?.Invoke(this, null);
        }

        private void WhileRunningView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void WhileRunningView_PauseClicked(object sender, EventArgs e)
        {
            PauseClicked?.Invoke(this, null);
        }

        private void WhileRunningView_UnpauseClicked(object sender, EventArgs e)
        {
            UnpauseClicked?.Invoke(this, null);
        }


        private void BackgroundGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(e.HeightChanged)
            {
                RefreshLeftPanelSize();
            }
        }

        private void GlueViewSettingsButtonClicked(object sender, RoutedEventArgs e)
        {
            SettingsClicked?.Invoke(this, null);
        }

        // Victor Chelaru Oct 4
        // This bug sucks, and this is the best way I know how to solve it.
        // The bug is - whenever the main Glue window moves, the Cursor position
        // is reported incorrectly by the game. It seems as if the window visibly
        // moves, but the underlying systems still get the cursor position as if the
        // window was in its old position. Not sure why this is, but whenever the Game
        // tab is resized, the problem fixes itself. I tried lots of things to solve this:
        // * Explicitly changing the Width and Height of the window
        // * Updating the layout by itself
        // * calling the code to refresh the internal window
        //public void ReactToMainWindowMoved()
        //{
        //    var oldWidth = WinformsHost.Width;
        //    var actualWidth = WinformsHost.ActualWidth;
        //    WinformsHost.Width = actualWidth;
        //    WinformsHost.UpdateLayout();
        //    WinformsHost_SizeChanged(null, null);
        //    WinformsHost.Width = double.NaN;
        //    WinformsHost.UpdateLayout();
        //}
        // Nothign worked - the cursor was still reported incorrectly. Therefore, to fix it
        // I just explicitly change the left tab size, which sucks but I don't know of a better
        // way to do it.
        // I initially started with a delay of 1000 ms, and got it much lower. Too low and the problem doesn't
        // get solved, too high and the user sees long delays between the flickers.
        const int msDelayBetweenResizes = 5;

        int lastWidth;
        int lastHeight;
        public async void ReactToMainWindowResizeEnd()
        {
            await RefreshLeftPanelSize();
        }

        private async Task RefreshLeftPanelSize()
        {
            var window = MainGlueWindow.Self;
            var areSame = window.Width == lastWidth && window.Height == lastHeight;
            var are0 = lastWidth == 0 && lastHeight == 0;

            lastWidth = window.Width;
            lastHeight = window.Height;
            if (areSame || are0)
            {
                var leftPixel = MainPanelControl.ViewModel.LeftPanelWidth.Value;
                // need to get the VM for the splitter and adjust it:
                MainPanelControl.ViewModel.LeftPanelWidth = new GridLength(leftPixel + 1);
                await Task.Delay(msDelayBetweenResizes);
                MainPanelControl.ViewModel.LeftPanelWidth = new GridLength(leftPixel);
            }
        }

        public void HandleGluxLoaded()
        {
            EditingTools.HandleGluxLoaded();
        }

        public void HandleGluxUnloaded()
        {
            EditingTools.HandleGluxUnloaded();
        }

        public void UpdateToItemSelected() => EditingTools.UpdateToItemSelected();
    }
}
