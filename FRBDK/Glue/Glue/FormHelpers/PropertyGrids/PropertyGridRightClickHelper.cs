﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using System.Drawing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using GlueSaveClasses;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public static class PropertyGridRightClickHelper
    {
        #region Fields

        static MenuItem mSetDefaultMenuItem;
        static MenuItem mExposeVariable;

        static MenuItem mUseCustomRectangle;
        static MenuItem mUseFullScreen;

        static CustomVariable mHighlightedCustomVariable;
        #endregion

        public static void Initialize()
        {
            mSetDefaultMenuItem = new MenuItem("Set to Default", SetDefaultClick);

            mExposeVariable = new MenuItem("Expose Variable", ExposeVariableClick);

            mUseCustomRectangle = new MenuItem("Use Custom Rectangle", UseCustomRectangleClick);
            mUseFullScreen = new MenuItem("Use Full Screen", UseFullScreenClick);
        }

        public static void ReactToRightClick()
        {
            #region Get the context menu
            System.Windows.Forms.PropertyGrid propertyGrid = MainGlueWindow.Self.PropertyGrid;
            mHighlightedCustomVariable = null;

            if (propertyGrid.ContextMenu == null)
            {
                propertyGrid.ContextMenu = new ContextMenu();
            }


            ContextMenu contextMenu = propertyGrid.ContextMenu;
            contextMenu.MenuItems.Clear();
            #endregion


            string label = propertyGrid.SelectedGridItem.Label;


            #region If there is a current StateSave

            if (EditorLogic.CurrentStateSave != null)
            {
                // Assume that it's a variable
                contextMenu.MenuItems.Add(mSetDefaultMenuItem);
            }

            #endregion

            #region If there is a current CustomVariable

            if (EditorLogic.CurrentCustomVariable != null)
            {
                // Assume that it's a variable
                contextMenu.MenuItems.Add(mSetDefaultMenuItem);
            }

            #endregion

            #region If there is a current NamedObject

            else if (EditorLogic.CurrentNamedObject != null)
            {
                NamedObjectSave namedObject = EditorLogic.CurrentNamedObject;

                // Is this a variable
                if (namedObject.GetCustomVariable(label) != null)
                {
                    contextMenu.MenuItems.Add(mSetDefaultMenuItem);
                }

                else if (namedObject.IsLayer && label ==
                    "DestinationRectangle")
                {
                    if (namedObject.DestinationRectangle == null ||
                        !namedObject.DestinationRectangle.HasValue)
                    {
                        contextMenu.MenuItems.Add(mUseCustomRectangle);
                    }
                    else
                    {
                        contextMenu.MenuItems.Add(mUseFullScreen);
                    }
                }


            }

            #endregion

            #region If there is a current Entity Save (to be checked *after* the checks above)

            else if (EditorLogic.CurrentElement != null)
            {
                if (EditorLogic.CurrentTreeNode.IsRootCustomVariablesNode())
                {
                    CustomVariable customVariable = EditorLogic.CurrentElement.GetCustomVariable(label);

                    if(customVariable != null)
                    {
                        mHighlightedCustomVariable = customVariable;
                        contextMenu.MenuItems.Add(mSetDefaultMenuItem);
                    }
                }
                else if (EditorLogic.CurrentEntitySave != null)
                {
                    
                    EntitySave sourceEntitySave = EditorLogic.CurrentEntitySave;

                    if (label == "ImplementsIVisible" && sourceEntitySave != null && sourceEntitySave.ImplementsIVisible
                        && sourceEntitySave.GetCustomVariable("Visible") == null
                        )
                    {
                        contextMenu.MenuItems.Add(mExposeVariable);
                    }
                    else if (label == "BaseEntity" && !string.IsNullOrEmpty(EditorLogic.CurrentEntitySave.BaseEntity))
                    {
                        contextMenu.MenuItems.Add("Go to definition", GoToDefinitionClick);

                    }
                }
            }

            #endregion



            PluginManager.ReactToPropertyGridRightClick(propertyGrid, contextMenu);
        }

        private static void SetDefaultClick(object sender, EventArgs e)
        {
            // set default, reset default, set to default, reset to default
            if (mHighlightedCustomVariable != null)
            {
                mHighlightedCustomVariable.DefaultValue = null;
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }
            else if (EditorLogic.CurrentStateSave != null)
            {
                StateSave stateSave = EditorLogic.CurrentStateSave;

                string valueToChange = MainGlueWindow.Self.PropertyGrid.SelectedGridItem.Label;
                if (valueToChange.Contains(" set"))
                {
                    valueToChange = valueToChange.Substring(0, valueToChange.IndexOf(" set"));
                }
                for (int i = stateSave.InstructionSaves.Count - 1; i > -1; i--)
                {
                    if (stateSave.InstructionSaves[i].Member == valueToChange)
                    {
                        stateSave.InstructionSaves.RemoveAt(i);
                        break;
                    }
                }
            }
            else if (EditorLogic.CurrentCustomVariable != null)
            {
                EditorLogic.CurrentCustomVariable.DefaultValue = null;
                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }
            else
            {
                NamedObjectSave currentNamedObject = EditorLogic.CurrentNamedObject;

                string variableToSet = MainGlueWindow.Self.PropertyGrid.SelectedGridItem.Label;

                SetVariableToDefault(currentNamedObject, variableToSet);
            }

            ElementViewWindow.GenerateSelectedElementCode();

            GluxCommands.Self.SaveGlux();

            MainGlueWindow.Self.PropertyGrid.Refresh();
        }

        public static void SetVariableToDefault(NamedObjectSave currentNamedObject, string variableToSet)
        {
            // July 13, 2014
            // This used to simply set the value to null, but why don't we remove it if it exists?
            // This way if an error is introduced by some plugin that sets the type to something invalid
            // the user can still remove it through this option and recover the type later.
            //currentNamedObject.SetPropertyValue(variableToSet, null);
            currentNamedObject.InstructionSaves.RemoveAll(item => item.Member == variableToSet);

            if (currentNamedObject.GetCustomVariable(variableToSet) != null)
            {
                // See if this variable is tunneled into in this element.
                // If so, set that value too.
                CustomVariableInNamedObject cvino = currentNamedObject.GetCustomVariable(variableToSet);
                object value = cvino.Value;

                foreach (CustomVariable customVariable in EditorLogic.CurrentElement.CustomVariables)
                {
                    if (customVariable.SourceObject == currentNamedObject.InstanceName &&
                        customVariable.SourceObjectProperty == variableToSet)
                    {
                        customVariable.DefaultValue = value;
                        break;
                    }
                }

                GlueCommands.Self.RefreshCommands.RefreshPropertyGrid();
            }
        }

        private static void ExposeVariableClick(object sender, EventArgs e)
        {
            System.Windows.Forms.PropertyGrid propertyGrid = MainGlueWindow.Self.PropertyGrid;

            string label = propertyGrid.SelectedGridItem.Label;

            if (label == "ImplementsIVisible")
            {
                // We're going to make a bool Visible for this now.

                CustomVariable newVariable = new CustomVariable();
                newVariable.Name = "Visible";
                newVariable.Type = "bool";


                RightClickHelper.CreateAndAddNewVariable(
                    newVariable);

            }
        }

        private static void GoToDefinitionClick(object sender, EventArgs e)
        {
            string baseName = EditorLogic.CurrentElement.BaseElement;

            TreeNode entityNode = GlueState.Self.Find.EntityTreeNode(baseName);

            ElementViewWindow.SelectedNode = entityNode;
        }

        private static void UseCustomRectangleClick(object sender, EventArgs e)
        {

            FloatRectangle rectangle = new FloatRectangle(0, 0,
                ProjectManager.GlueProjectSave.ResolutionWidth,
                ProjectManager.GlueProjectSave.ResolutionHeight);

            EditorLogic.CurrentNamedObject.DestinationRectangle = rectangle;

            ElementViewWindow.GenerateSelectedElementCode();
            GluxCommands.Self.SaveGlux();
            MainGlueWindow.Self.PropertyGrid.Refresh();
        }

        private static void UseFullScreenClick(object sender, EventArgs e)
        {
            EditorLogic.CurrentNamedObject.DestinationRectangle = null;

            ElementViewWindow.GenerateSelectedElementCode();
            GluxCommands.Self.SaveGlux();
            MainGlueWindow.Self.PropertyGrid.Refresh();
        }


    }
}
