﻿using System;
using FlatRedBall.Glue.SaveClasses;
using System.Collections.Generic;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGluxCommands
    {
        IScreenCommands ScreenCommands
        {
            get;
        }

        IEntityCommands EntityCommands
        {
            get;
        }

        IProjectCommands ProjectCommands { get; }

        void SaveGlux(bool sendPluginRefreshCommand = true);

        /// <summary>
        /// Adds a task to save the project with TaskExecutionPreference.AddOrMoveToEnd
        /// </summary>
        void SaveGluxTask();

        /// <summary>
        /// Adds a new file to the Glue project in global content. This method updates the in-memory GlueProjectSave,
        /// adds the file to the Visual Studio project, and refreshes the global content tree node.
        /// This method does not save the .glux.
        /// </summary>
        /// <param name="fileToAdd">The file name relative to the Glue project. 
        /// For example, to add a png file to GlobalContent, the file name might be "GlobalContent/MyFile.png"</param>
        /// <param name="includeDirectoryInGlobalContentInName">Whether to include the subdirectory 
        /// in the name of the newly-created file.</param>
        /// <returns>The new ReferencedFileSave.</returns>
        ReferencedFileSave AddReferencedFileToGlobalContent(string fileToAdd, bool includeDirectoryInGlobalContentInName);

        ReferencedFileSave GetReferencedFileSaveFromFile(string fileName);

#if GLUE
        ReferencedFileSave AddSingleFileTo(string fileName, string rfsName, string extraCommandLineArguments,
            EditorObjects.SaveClasses.BuildToolAssociation buildToolAssociation, bool isBuiltFile, string options, 
            IElement sourceElement, string directoryOfTreeNode);
        // SourceType sourceType, string sourceClassType, string sourceFile, string objectName, string sourceNameInFile, string sourceClassGenericType
        NamedObjectSave AddNewNamedObjectToSelectedElement(ViewModels.AddObjectViewModel addObjectViewModel);
        NamedObjectSave AddNewNamedObjectTo(ViewModels.AddObjectViewModel addObjectViewModel, IElement element, NamedObjectSave namedObject);

        bool SetPluginRequirement(Interfaces.IPlugin plugin, bool requiredByProject);
        bool GetPluginRequirement(Interfaces.IPlugin plugin);
#endif

        bool MoveEntityToDirectory(EntitySave entitySave, string newRelativeDirectory);


        // was:

        ValidationResponse AddNewCustomClass(string className, out CustomClassSave customClassSave);

        void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove);
        void RemoveReferencedFile(ReferencedFileSave referencedFileToRemove, List<string> additionalFilesToRemove, bool regenerateCode);

        void RemoveNamedObject(NamedObjectSave namedObjectToRemove, bool performSave = true, bool updateUi = true,
            List<string> additionalFilesToRemove = null);


        void SetVariableOn(NamedObjectSave nos, string memberName, Type memberType, object value);
        void SaveSettings();
    }
}
