﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.StandardTypes;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Parsing;
using Glue;
using System.Drawing;
using FlatRedBall.Glue.SaveClasses;
using System.IO;
using FlatRedBall.Utilities;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.VSHelpers;
using System.Diagnostics;
using FlatRedBall.Glue.Plugins;
using EditorObjects.Parsing;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using System.ComponentModel;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Performance.Measurement;
using FlatRedBall.Glue.Navigation;
using FlatRedBall.Glue.AutomatedGlue;


namespace FlatRedBall.Glue.FormHelpers
{
    public static partial class ElementViewWindow
    {
        #region Fields

        static List<string> mDirectoriesToIgnore = new List<string>();

        static TreeNode mEntityNode;
        static TreeNode mScreenNode;
        static TreeNode mGlobalContentNode;
        //static TreeNode mUnreferencedContent;

        static TreeView mTreeView;

        static TreeNode mStartUpScreen;

        #region Screen Node Colors

        public static Color StartupScreenColor = Color.DarkRed;
        public static Color RequiredScreenColor = Color.DarkGreen;

        #endregion

        public static Color RegularBackgroundColor = Color.Black;
        public static Color MissingObjectColor = Color.Red;
        public static Color AutoGeneratedColor = Color.Red;

        public static Color StateCategoryColor = Color.Orange;
        public static Color FolderColor = Color.Orange;

        #region Object Node Colors

        public static Color SetByDerivedColor = Color.FromArgb(80, 100, 255);
        public static Color DefinedByBaseColor = Color.Yellow;
        public static Color LayerObjectColor = Color.GreenYellow;
        public static Color InstantiatedByBase = Color.Green;

        public static Color IsContainerColor = Color.Pink;

        public static Color DisabledColor = Color.Gray;

        #endregion


        #endregion

        #region Properties

        public static TreeNode EntitiesTreeNode
        {
            get { return mEntityNode; }
        }

        public static TreeNode ScreensTreeNode
        {
            get
            {
                return mScreenNode;
            }
        }

        public static IEnumerable<ScreenTreeNode> AllScreens
        {
            get
            {
                for (int i = 0; i < mScreenNode.Nodes.Count; i++)
                {
                    yield return (ScreenTreeNode)mScreenNode.Nodes[i];
                }
            }
        }

        public static IEnumerable<EntityTreeNode> AllEntities
        {
            get
            {
                foreach (EntityTreeNode entityTreeNode in mEntityNode.AllEntitiesIn())
                {
                    yield return entityTreeNode;
                }
            }
        }

        public static List<string> DirectoriesToIgnore
        {
            get { return mDirectoriesToIgnore; }
        }

        public static TreeNode StartUpScreen
        {
            get { return mStartUpScreen; }
            set
            {
                if (value != mStartUpScreen)
                {
                    if (mStartUpScreen != null)
                    {
                        mStartUpScreen.BackColor = RegularBackgroundColor;
                        // un-bold the previous startup
                    }

                    mStartUpScreen = value;

                    if (mStartUpScreen != null)
                    {
                        // now bold the current startup
                        mStartUpScreen.BackColor = StartupScreenColor;

                        ProjectManager.StartUpScreen = ((ScreenSave)mStartUpScreen.Tag).Name;
                    }

                    // Changing the startup screen will change the NextScreen for the
                    // required screen, so we have to regenerate that code now
                    foreach (ScreenSave screenInProject in ProjectManager.GlueProjectSave.Screens)
                    {
                        if (screenInProject.IsRequiredAtStartup)
                        {
                            CodeWriter.GenerateCode(screenInProject);
                            break;
                        }
                    }


                }

            }
        }



        public static TreeNode GlobalContentFileNode
        {
            get { return mGlobalContentNode; }
        }

        public static TreeNode SelectedNode
        {
            get
            {
                return MainGlueWindow.Self.ElementTreeView.SelectedNode;
            }
            set
            {
                MainGlueWindow.Self.ElementTreeView.SelectedNode = value;
            }
        }

        public static TreeNode TreeNodeDraggedOff
        {
            get;
            set;
        }

        public static bool SuppressSelectionEvents
        {
            get;
            set;
        }


        #endregion



        #region Methods

        public static void Initialize(TreeView treeView,  TreeNode entityNode, TreeNode screenNode, TreeNode globalContentNode)
        {
            mDirectoriesToIgnore.Add(".svn");
            mTreeView = treeView;
            mEntityNode = entityNode;

            
            mScreenNode = screenNode;

            
            mGlobalContentNode = globalContentNode;

            mEntityNode.SelectedImageKey = "master_entity.png";
            mEntityNode.ImageKey = "master_entity.png";

            mScreenNode.SelectedImageKey = "master_screen.png";
            mScreenNode.ImageKey = "master_screen.png";

            mGlobalContentNode.SelectedImageKey = "master_file.png";
            mGlobalContentNode.ImageKey = "master_file.png";
        }


        public static void SuspendLayout()
        {
            mTreeView.SuspendLayout();
        }

        public static void ResumeLayout()
        {
            mTreeView.ResumeLayout();
        }

        public static void AfterSelect()
        {
            // tree node click
            TreeNode node = SelectedNode;

            bool isCode = !string.IsNullOrEmpty(EditorLogic.CurrentCodeFile);

            #region Make translation info on this file if necessary

            if (isCode)
            {
                ProjectManager.GlueProjectSave.CreateTranslatedFileSaveIfNecessary(
                    EditorLogic.CurrentCodeFile);

                MainGlueWindow.Self.PropertyGrid.SelectedObject = ProjectManager.GlueProjectSave.GetTranslatedFileSave(
                    EditorLogic.CurrentCodeFile);
            }

            #endregion

            PropertyGridHelper.UpdateDisplayedPropertyGridProperties();

            #region Show the appropriate preview

            if (isCode)
            {
                if (FileManager.FileExists(EditorLogic.CurrentCodeFile))
                {
                    MainGlueWindow.Self.CodePreviewTextBox.Text = FileManager.FromFileText(EditorLogic.CurrentCodeFile);
                }
                else
                {
                    MessageBox.Show("Could not find the file\n\n" + EditorLogic.CurrentCodeFile);
                    // This file no longer exists, so let's refresh the property grid
                    EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
                }
            }
            else
            {
                MainGlueWindow.Self.CodePreviewTextBox.Text = null;
            }

            #endregion


            EditorLogic.TakeSnapshot();

            bool wasFocused = mTreeView?.Focused == true;
            VisibilityManager.ReactivelySetItemViewVisibility();
            if (!SuppressSelectionEvents)
            {
                PluginManager.ReactToItemSelect(node);
            }
            // ReactivelySetItemViewVisibility may add or remove controls, and as a result the
            // list view may lose focus. We dont' want that to happen so we will explicitly put
            // focus on the control:
            if(wasFocused)
            {
                mTreeView.Focus(); 
            }
        }


        public static void ShowAllElementVariablesInPropertyGrid()
        {
            var element = GlueState.Self.CurrentElement;

            PropertyGridDisplayer displayer = new PropertyGridDisplayer();
            displayer.Instance = element;
            displayer.ExcludeAllMembers();
            displayer.RefreshOnTimer = false;
            
            displayer.PropertyGrid = MainGlueWindow.Self.PropertyGrid;


            foreach (CustomVariable variable in element.CustomVariables)
            {
                Type type = variable.GetRuntimeType();
                if (type == null)
                {
                    type = typeof(string);
                }

                string name = variable.Name;
                object value = variable.DefaultValue;
                TypeConverter converter = variable.GetTypeConverter(element);


                displayer.IncludeMember(name, type,
                    delegate(object sender, MemberChangeArgs args)
                    {
                        element.GetCustomVariableRecursively(name).DefaultValue = args.Value;
                    }
                    ,
                    delegate()
                    {
                        return element.GetCustomVariableRecursively(name).DefaultValue;
                    },
                    converter);
            
            }
            MainGlueWindow.Self.PropertyGrid.SelectedObject = displayer;
        }


        public static EntityTreeNode AddEntity(EntitySave entitySave, bool generateCode = true)
        {
            Section.GetAndStartContextAndTime("Constructor");
            EntityTreeNode treeNode = new EntityTreeNode(FileManager.RemovePath(entitySave.Name));

            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Code file");

            treeNode.CodeFile = entitySave.Name + ".cs";

            Section.EndContextAndTime();
            Section.GetAndStartContextAndTime("Add node");

            TreeNode treeNodeToAddTo = null;
            bool succeeded = true;

            if (entitySave.Name.StartsWith("Entities/") == false &&
                entitySave.Name.StartsWith("Entities\\") == false)
            {
                GlueGui.ShowMessageBox(
                    "The Entity named " + entitySave.Name + " must have a name that starts with \"Entities/\"");
                succeeded = false;
            }
            if (succeeded)
            {
                string containingDirectory = FileManager.MakeRelative(FileManager.GetDirectory(entitySave.Name));

                if (containingDirectory == "Entities/")
                {
                    treeNodeToAddTo = mEntityNode;
                }
                else
                {

                    string directory = containingDirectory.Substring("Entities/".Length);

                    treeNodeToAddTo = GlueState.Self.Find.TreeNodeForDirectoryOrEntityNode(
                        directory, mEntityNode);
                    if (treeNodeToAddTo == null && !string.IsNullOrEmpty(directory))
                    {
                        // If it's null that may mean the directory doesn't exist.  We should make it
                        string absoluteDirectory = ProjectManager.MakeAbsolute(containingDirectory);
                        if (!Directory.Exists(absoluteDirectory))
                        {
                            Directory.CreateDirectory(absoluteDirectory);

                        }
                        AddDirectoryNodes(FileManager.RelativeDirectory + "Entities/", mEntityNode);

                        // now try again
                        treeNodeToAddTo = GlueState.Self.Find.TreeNodeForDirectoryOrEntityNode(
                            directory, mEntityNode);
                    }
                }

                // Someone in the chat room got a crash on the Add call.  Not sure why
                // so adding these to help find out what's up.
                if (treeNodeToAddTo == null)
                {
                    throw new NullReferenceException("treeNodeToAddTo is null.  This is bad");
                }
                if (treeNode == null)
                {
                    throw new NullReferenceException("treeNode is null.  This is bad");
                }


                treeNodeToAddTo.Nodes.Add(treeNode);
                treeNodeToAddTo.Nodes.SortByTextConsideringDirectories();

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Set generated");
                string generatedFile = entitySave.Name + ".Generated.cs";

                if (FileManager.FileExists(generatedFile))
                {
                    treeNode.GeneratedCodeFile = generatedFile;
                }

                Section.EndContextAndTime();
                Section.GetAndStartContextAndTime("Sort");
                SortEntities();

                Section.EndContextAndTime();

                treeNode.EntitySave = entitySave;

            }
            return treeNode;
        }



        public static BaseElementTreeNode AddScreen(ScreenSave screenSave)
        {
            string screenFileName = screenSave.Name + ".cs";
            string screenFileWithoutExtension = FileManager.RemoveExtension(screenFileName);

            var screenTreeNode = new ScreenTreeNode(FileManager.RemovePath(screenFileWithoutExtension));
            screenTreeNode.CodeFile = screenFileName;

            mScreenNode.Nodes.Add(screenTreeNode);

            string generatedFile = screenFileWithoutExtension + ".Generated.cs";
            screenTreeNode.GeneratedCodeFile = generatedFile;

            screenTreeNode.SaveObject = screenSave;

            int desiredIndex = ProjectManager.GlueProjectSave.Screens.IndexOf(screenSave);
            if(desiredIndex < mScreenNode.Nodes.Count && mScreenNode.Nodes[desiredIndex] != mScreenNode)
            {
                mScreenNode.Nodes.Remove(screenTreeNode);
                mScreenNode.Nodes.Insert(desiredIndex, screenTreeNode);
            }

            return screenTreeNode;
        }

     
        public static void GenerateSelectedElementCode()
        {
            if (EditorLogic.CurrentElement != null)
            {
                CodeWriter.GenerateCode(EditorLogic.CurrentElement);
            }
            else if (EditorLogic.CurrentReferencedFile != null)
            {
                ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;
                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();

                if (rfs.IsCsvOrTreatedAsCsv)
                {
                    CsvCodeGenerator.GenerateAndSaveDataClass(EditorLogic.CurrentReferencedFile, rfs.CsvDelimiter);
                }
            }

            // Even though we may have generated a Screen/Entity, we still might want to see if we should update 
            // CSVs:
            if (EditorLogic.CurrentReferencedFile != null)
            {
                ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;
                if (rfs.Name.ToLower().EndsWith(".csv"))
                {
                    CsvCodeGenerator.GenerateAndSaveDataClass(EditorLogic.CurrentReferencedFile, rfs.CsvDelimiter);
                }
            }

        }

        public static void Invoke(Delegate method)
        {
            mTreeView.Invoke(method);
        }

        public static void RemoveEntity(EntitySave entityToRemove)
        {
            TreeNode treeNode = GlueState.Self.Find.TreeNodeForDirectory(
                FileManager.GetDirectory(entityToRemove.Name));


            for (int i = 0; i < treeNode.Nodes.Count; i++)
            {
                EntityTreeNode entityTreeNode = treeNode.Nodes[i] as EntityTreeNode;


                if (entityTreeNode != null && entityTreeNode.EntitySave == entityToRemove)
                {
                    treeNode.Nodes.RemoveAt(i);
                    break;
                }
            }
        }

        public static void RemoveScreen(ScreenSave screenToRemove)
        {
            for (int i = 0; i < mScreenNode.Nodes.Count; i++)
            {
                if (((BaseElementTreeNode)mScreenNode.Nodes[i]).SaveObject == screenToRemove)
                {
                    mScreenNode.Nodes.RemoveAt(i);
                    break;
                }
            }

        }

        public static void UpdateCurrentObjectReferencedTreeNodes()
        {
            if (EditorLogic.CurrentElementTreeNode != null)
            {
                EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
            }
            else if (EditorLogic.CurrentTreeNode != null && EditorLogic.CurrentTreeNode.Root().IsGlobalContentContainerNode())
            {
                ElementViewWindow.UpdateGlobalContentTreeNodes(false);
            }
        }

        public static void UpdateNodeToListIndex(EntitySave entitySave)
        {

            EntityTreeNode entityTreeNode = GlueState.Self.Find.EntityTreeNode(entitySave);

            TreeNode parentTreeNode = entityTreeNode.Parent;

            bool wasSelected = MainGlueWindow.Self.ElementTreeView.SelectedNode == entityTreeNode;

            parentTreeNode.Nodes.SortByTextConsideringDirectories();

            if (wasSelected)
            {
                MainGlueWindow.Self.ElementTreeView.SelectedNode = entityTreeNode;
            }
        }

        public static void UpdateNodeToListIndex(ScreenSave screenSave)
        {
            ScreenTreeNode screenTreeNode = GlueState.Self.Find.ScreenTreeNode(screenSave);

            bool wasSelected = MainGlueWindow.Self.ElementTreeView.SelectedNode == screenTreeNode;

            int desiredIndex = ProjectManager.GlueProjectSave.Screens.IndexOf(screenSave);

            mScreenNode.Nodes.Remove(screenTreeNode);
            mScreenNode.Nodes.Insert(desiredIndex, screenTreeNode);

            if (wasSelected)
            {
                MainGlueWindow.Self.ElementTreeView.SelectedNode = screenTreeNode;
            }
        }

        public static void UpdateGlobalContentTreeNodes(bool performSave)
        {
            #region Loop through all referenced files.  Create a tree node if needed, or remove it from the project if the file doesn't exist.

            for (int i = 0; i < ProjectManager.GlueProjectSave.GlobalFiles.Count; i++)
            {
                ReferencedFileSave rfs = ProjectManager.GlueProjectSave.GlobalFiles[i];

                TreeNode nodeForFile = GetTreeNodeForGlobalContent(rfs, mGlobalContentNode);

                #region If there is no tree node for this file, make one

                if (nodeForFile == null)
                {
                    string fullFileName = ProjectManager.MakeAbsolute(rfs.Name, true);

                    if (FileManager.FileExists(fullFileName))
                    {
                        nodeForFile = new TreeNode(FileManager.RemovePath(rfs.Name));

                        nodeForFile.ImageKey = "file.png";
                        nodeForFile.SelectedImageKey = "file.png";

                        string absoluteRfs = ProjectManager.MakeAbsolute(rfs.Name, true);

                        TreeNode nodeToAddTo = GlueState.Self.Find.TreeNodeForDirectory(FileManager.GetDirectory(absoluteRfs));

                        if (nodeToAddTo == null)
                        {
                            nodeToAddTo = GlobalContentFileNode;
                        }

                        nodeToAddTo.Nodes.Add(nodeForFile);

                        nodeToAddTo.Nodes.SortByTextConsideringDirectories();

                        nodeForFile.Tag = rfs;
                    }

                    else
                    {
                        ProjectManager.GlueProjectSave.GlobalFiles.RemoveAt(i);
                        // Do we want to do this?
                        // ProjectManager.GlueProjectSave.GlobalContentHasChanged = true;

                        i--;
                    }
                }

                #endregion

                #region else, there is already one

                else
                {
                    string textToSet = FileManager.RemovePath(rfs.Name);
                    if (nodeForFile.Text != textToSet)
                    {
                        nodeForFile.Text = textToSet;
                    }
                }

                #endregion
            }

            #endregion

            #region Do cleanup - remove tree nodes that exist but represent objects no longer in the project


            for (int i = mGlobalContentNode.Nodes.Count - 1; i > -1; i--)
            {
                TreeNode treeNode = mGlobalContentNode.Nodes[i];

                RemoveGlobalContentTreeNodesIfNecessary(treeNode);
            }

            #endregion

            if (performSave)
            {
                ProjectManager.SaveProjects();
            }

        }

        private static void RemoveGlobalContentTreeNodesIfNecessary(TreeNode treeNode)
        {
            if (treeNode.IsDirectoryNode())
            {
                string directory = treeNode.GetRelativePath();

                directory = ProjectManager.MakeAbsolute(directory, true);


                if (!Directory.Exists(directory))
                {
                    // The directory isn't here anymore, so kill it!
                    treeNode.Parent.Nodes.Remove(treeNode);

                }
                else
                {
                    // The directory is valid, but let's check subdirectories
                    for (int i = treeNode.Nodes.Count - 1; i > -1; i--)
                    {
                        RemoveGlobalContentTreeNodesIfNecessary(treeNode.Nodes[i]);
                    }
                }
            }
            else // assume content for now
            {

                ReferencedFileSave referencedFileSave = treeNode.Tag as ReferencedFileSave;

                if (!ProjectManager.GlueProjectSave.GlobalFiles.Contains(referencedFileSave))
                {
                    treeNode.Parent.Nodes.Remove(treeNode);
                }
                else
                {
                    // The RFS may be contained, but see if the file names match
                    string rfsName = FileManager.Standardize(referencedFileSave.Name, null, false).ToLower();
                    string treeNodeFile = FileManager.Standardize(treeNode.GetRelativePath(), null, false).ToLower();

                    // We first need to make sure that the file is part of GlobalContentFiles.
                    // If it is, then we may have tree node in the wrong folder, so let's get rid
                    // of it.  If it doesn't start with globalcontent/ then we shouldn't remove it here.
                    if (rfsName.StartsWith("globalcontent/") &&  rfsName != treeNodeFile)
                    {
                        treeNode.Parent.Nodes.Remove(treeNode);
                    }
                }
            }
        }

        public static TreeNode GetTreeNodeForGlobalContent(ReferencedFileSave rfs, TreeNode nodeToStartAt)
        {



            TreeNode containerTreeNode = nodeToStartAt;

            if (rfs.Name.ToLower().StartsWith("globalcontent/") && nodeToStartAt == mGlobalContentNode)
            {
                string directory = FileManager.GetDirectoryKeepRelative(rfs.Name);

                int globalContentConstLength = "globalcontent/".Length;

                if (directory.Length > globalContentConstLength)
                {

                    string directoryToLookFor = directory.Substring(globalContentConstLength, directory.Length - globalContentConstLength);

                    containerTreeNode = GlueState.Self.Find.TreeNodeForDirectoryOrEntityNode(directoryToLookFor, nodeToStartAt);
                }
            }


            if (rfs.Name.ToLower().StartsWith("content/globalcontent/") && nodeToStartAt == mGlobalContentNode)
            {
                string directory = FileManager.GetDirectoryKeepRelative(rfs.Name);

                int globalContentConstLength = "content/globalcontent/".Length;

                if (directory.Length > globalContentConstLength)
                {

                    string directoryToLookFor = directory.Substring(globalContentConstLength, directory.Length - globalContentConstLength);

                    containerTreeNode = GlueState.Self.Find.TreeNodeForDirectoryOrEntityNode(directoryToLookFor, nodeToStartAt);
                }
            }


            if (containerTreeNode != null)
            {
                for (int i = 0; i < containerTreeNode.Nodes.Count; i++)
                {
                    TreeNode subnode = containerTreeNode.Nodes[i];

                    if (subnode.Tag == rfs)
                    {
                        return subnode;
                    }
                    //else if (subnode.IsDirectoryNode())
                    //{
                    //    TreeNode foundNode = GetTreeNodeForGlobalContent(rfs, subnode);

                    //    if (foundNode != null)
                    //    {
                    //        return foundNode;
                    //    }
                    //}
                }
            }
            return null;
        }

        internal static void AddDirectoryNodes()
        {
            AddDirectoryNodes(FileManager.RelativeDirectory + "Entities/", mEntityNode);

            #region Add global content directories

            string contentDirectory = FileManager.RelativeDirectory;

            if (ProjectManager.ContentProject != null)
            {
                contentDirectory = ProjectManager.ContentProject.GetAbsoluteContentFolder();
            }

            AddDirectoryNodes(contentDirectory + "GlobalContent/", mGlobalContentNode);
            #endregion
        }

        internal static void AddDirectoryNodes(string parentDirectory, TreeNode parentTreeNode)
        {
            if (Directory.Exists(parentDirectory))
            {
                string[] directories = Directory.GetDirectories(parentDirectory);

                foreach (string directory in directories)
                {
                    string relativePath = FileManager.MakeRelative(directory, parentDirectory);

                    string nameOfNewNode = relativePath;

                    if (relativePath.Contains('/'))
                    {
                        nameOfNewNode = relativePath.Substring(0, relativePath.IndexOf('/'));
                    }

                    if (!mDirectoriesToIgnore.Contains(nameOfNewNode))
                    {

                        TreeNode treeNode = GlueState.Self.Find.TreeNodeForDirectoryOrEntityNode(relativePath, parentTreeNode);

                        if (treeNode == null)
                        {
                            treeNode = parentTreeNode.Nodes.Add(FileManager.RemovePath(directory));
                        }

                        treeNode.ImageKey = "folder.png";
                        treeNode.SelectedImageKey = "folder.png";

                        treeNode.ForeColor = ElementViewWindow.FolderColor;

                        AddDirectoryNodes(parentDirectory + relativePath + "/", treeNode);
                    }
                }

                // Now see if there are any directory tree nodes that don't have a matching directory

                // Let's make the directories lower case
                for (int i = 0; i < directories.Length; i++)
                {
                    directories[i] = FileManager.Standardize(directories[i]).ToLower();

                    if (!directories[i].EndsWith("/") && !directories[i].EndsWith("\\"))
                    {
                        directories[i] = directories[i] + "/";
                    }
                }

                bool isGlobalContent = parentTreeNode.Root().IsGlobalContentContainerNode();


                for (int i = parentTreeNode.Nodes.Count - 1; i > -1; i--)
                {
                    TreeNode treeNode = parentTreeNode.Nodes[i];

                    if (treeNode.IsDirectoryNode())
                    {

                        string directory = ProjectManager.MakeAbsolute(treeNode.GetRelativePath(), isGlobalContent);

                        directory = FileManager.Standardize(directory.ToLower());

                        if (!directories.Contains(directory))
                        {
                            parentTreeNode.Nodes.RemoveAt(i);
                        }
                    }
                }
            }
        }

        internal static void SortEntities()
        {
            mEntityNode.Nodes.SortByTextConsideringDirectories();
        }


        public static void ElementDoubleClicked()
        {
            TreeNode selectedNode = SelectedNode;

            if (selectedNode != null)
            {
                string text = selectedNode.Text;


                

                #region Double-clicked a file
                string extension = FileManager.GetExtension(text);
                string sourceExtension = null;

                if (EditorLogic.CurrentReferencedFile != null && !string.IsNullOrEmpty(EditorLogic.CurrentReferencedFile.SourceFile))
                {
                    sourceExtension = FileManager.GetExtension(EditorLogic.CurrentReferencedFile.SourceFile);
                }
                if (EditorLogic.CurrentReferencedFile != null && !string.IsNullOrEmpty(extension))
                {
                    string application = "";

                    ReferencedFileSave currentReferencedFileSave = EditorLogic.CurrentReferencedFile;
                    string fileName;

                    if (currentReferencedFileSave != null && currentReferencedFileSave.OpensWith != "<DEFAULT>")
                    {
                        application = currentReferencedFileSave.OpensWith;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(sourceExtension))
                        {
                            application = EditorData.FileAssociationSettings.GetApplicationForExtension(sourceExtension);
                        }
                        else
                        {
                            application = EditorData.FileAssociationSettings.GetApplicationForExtension(extension);
                        }
                    }

                    if (currentReferencedFileSave != null)
                    {
                        if (!string.IsNullOrEmpty(currentReferencedFileSave.SourceFile))
                        {
                            fileName = "\"" + ProjectManager.MakeAbsolute(ProjectManager.ContentDirectoryRelative + currentReferencedFileSave.SourceFile, true) + "\"";
                        }
                        else
                        {
                            fileName = "\"" + ProjectManager.MakeAbsolute(ProjectManager.ContentDirectoryRelative + currentReferencedFileSave.Name) + "\"";
                        }
                    }
                    else
                    {
                        fileName = "\"" + ProjectManager.MakeAbsolute(text) + "\"";
                    }

                    if (string.IsNullOrEmpty(application) || application == "<DEFAULT>")
                    {
                        try
                        {
                            ProcessManager.OpenProcess(fileName, null);
                        }
                        catch (Exception ex)
                        {
                            System.Windows.Forms.MessageBox.Show("Error opening " + fileName + "\nTry navigating to this file and opening it through explorer");


                        }
                    }
                    else
                    {
                        bool applicationFound = true;
                        try
                        {
                            application = FileManager.Standardize(application);
                        }
                        catch
                        {
                            applicationFound = false;
                        }

                        if (!System.IO.File.Exists(application) || applicationFound == false)
                        {
                            string error = "Could not find the application\n\n" + application;

                            System.Windows.Forms.MessageBox.Show(error);
                        }
                        else
                        {
                            ProcessManager.OpenProcess(application, fileName);
                        }
                    }
                }

                #endregion
                
                #region Double-clicked a named object

                else if (selectedNode.IsNamedObjectNode())
                {
                    NamedObjectSave nos = selectedNode.Tag as NamedObjectSave;

                    if (nos.SourceType == SourceType.Entity)
                    {
                        TreeNode entityNode = GlueState.Self.Find.EntityTreeNode(nos.SourceClassType);

                        SelectedNode = entityNode;

                    }
                    else if (nos.SourceType == SourceType.FlatRedBallType && nos.IsGenericType)
                    {
                        // Is this an entity?
                        EntitySave genericEntityType = ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType);

                        if (genericEntityType != null)
                        {
                            SelectedNode = GlueState.Self.Find.EntityTreeNode(genericEntityType);
                        }

                    }
                    else if (nos.SourceType == SourceType.File && !string.IsNullOrEmpty(nos.SourceFile))
                    {
                        ReferencedFileSave rfs = nos.GetContainer().GetReferencedFileSave(nos.SourceFile);
                        TreeNode treeNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(rfs);

                        SelectedNode = treeNode;
                    }
                }

                #endregion

                #region Double-clicked a CustomVariable
                else if (selectedNode.IsCustomVariable())
                {
                    CustomVariable customVariable = EditorLogic.CurrentCustomVariable;

                    if (!string.IsNullOrEmpty(customVariable.SourceObject))
                    {
                        NamedObjectSave namedObjectSave = EditorLogic.CurrentElement.GetNamedObjectRecursively(customVariable.SourceObject);

                        if (namedObjectSave != null)
                        {
                            SelectedNode = GlueState.Self.Find.NamedObjectTreeNode(namedObjectSave);
                        }

                    }
                }

                #endregion

                #region Double-click an Event

                else if (selectedNode.IsEventResponseTreeNode())
                {
                    EventResponseSave ers = EditorLogic.CurrentEventResponseSave;

                    if (!string.IsNullOrEmpty(ers.SourceObject))
                    {
                        NamedObjectSave namedObjectSave = EditorLogic.CurrentElement.GetNamedObjectRecursively(ers.SourceObject);

                        if (namedObjectSave != null)
                        {
                            SelectedNode = GlueState.Self.Find.NamedObjectTreeNode(namedObjectSave);
                        }
                    }
                }


                #endregion

                #region Double-click an Enity/Screen

                else if (selectedNode.IsElementNode())
                {
                    IElement element = selectedNode.Tag as IElement;

                    string baseObject = element.BaseElement;

                    if (!string.IsNullOrEmpty(baseObject))
                    {
                        IElement baseElement = ObjectFinder.Self.GetIElement(baseObject);

                        SelectedNode = GlueState.Self.Find.ElementTreeNode(baseElement);
                    }
                }

                #endregion

                #region Code

                else if(selectedNode.IsCodeNode())
                {
                    var fileName = selectedNode.Text;

                    var absolute = GlueState.Self.CurrentGlueProjectDirectory + fileName;

                    if(System.IO.File.Exists(absolute))
                    {
                        System.Diagnostics.Process.Start(absolute);
                    }

                }

                #endregion
            }

        }

        #endregion

        public static void UpdateChangedElements()
        {
            foreach (var element in from entitySave in ProjectManager.GlueProjectSave.Entities 
                                    where entitySave.HasChanged 
                                    select entitySave)
            {
                var elementTreeNode = GlueState.Self.Find.ElementTreeNode(element);
                if (elementTreeNode != null)
                {
                    elementTreeNode.UpdateReferencedTreeNodes();
                    CodeWriter.GenerateCode(element);
                }
                element.HasChanged = false;
            }

            foreach (var element in from screenSave in ProjectManager.GlueProjectSave.Screens 
                                    where screenSave.HasChanged 
                                    select screenSave)
            {
                var elementTreeNode = GlueState.Self.Find.ElementTreeNode(element);
                if (elementTreeNode != null)
                {
                    elementTreeNode.UpdateReferencedTreeNodes();
                    CodeWriter.GenerateCode(element);
                }
                element.HasChanged = false;
            }

            if (ProjectManager.GlueProjectSave.GlobalContentHasChanged)
            {
                UpdateGlobalContentTreeNodes(true);
                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
            }
        }
    }
}
