﻿using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Managers;
using OfficialPlugins.MonoGameContent;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.ComponentModel;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;

namespace OfficialPlugins.ContentPipelinePlugin
{
    public class ContentPipelineController
    {
        SettingsSave settings;
        ControlViewModel viewModel;

        static IGlueState GlueState => EditorObjects.IoC.Container.Get<IGlueState>();
        static IGlueCommands GlueCommands => EditorObjects.IoC.Container.Get<IGlueCommands>();

        public SettingsSave Settings
        {
            get { return settings; }
        }

        public void LoadOrCreateSettings()
        {
            settings = SaveLoadLogic.LoadSettings();
            if(settings == null)
            {
                settings = new ContentPipelinePlugin.SettingsSave();
            }
        }

        public void SetViewModel(ControlViewModel viewModel)
        {

            this.viewModel = viewModel;
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;
            if(settings != null)
            {
                viewModel.UseContentPipelineOnPngs = settings.UseContentPipelineOnAllPngs;
            }
            else
            {
                viewModel.UseContentPipelineOnPngs = false;
            }
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var parameterName = e.PropertyName;

            switch(parameterName)
            {
                case nameof(ControlViewModel.UseContentPipelineOnPngs):

                    settings.UseContentPipelineOnAllPngs = viewModel.UseContentPipelineOnPngs;
                    SaveLoadLogic.SaveSettings(settings);

                    RefreshProjects();
                    break;
            }
        }

        public void HandleRefreshClicked(object sender, EventArgs e)
        {
            RefreshProjects();
        }
        
        private void RefreshProjects()
        {
            // store it locally so the task has the same value when executing:
            bool useContentPipeline = settings.UseContentPipelineOnAllPngs;


            TaskManager.Self.AddSync(() =>
            {
                if (useContentPipeline)
                {
                    SetAllPngRfsesToUseContentPipeline();
                }
                else
                {
                    SetAllPngsRfsesToFromFileLoading();
                }
            }, "Changing whether all referenced file saves use content pipeline");


            // remove first:
            if (!useContentPipeline)
            {
                RemoveXnbs();
            }

            TaskManager.Self.AddSync(() =>
            {
                RefreshAllFileMembership(useContentPipeline);
            }, "Refreshing file memberships");

            if (useContentPipeline)
            {
                AddXnbs();
            }

            AddSaveTasks();
        }

        private static void AddSaveTasks()
        {
            TaskManager.Self.AddSync(() =>
            {
                GlueCommands.ProjectCommands.SaveProjects();
                GlueCommands.GluxCommands.SaveGlux();
            },
            "Saving due to RFS changes");
        }

        private void AddXnbs()
        {
            var referencedPngs = GetReferencedPngs();

            var mainProject = GlueState.CurrentMainProject;
            ProjectBase[] syncedProjects = GlueState.SyncedProjects.ToArray();

            foreach (var referencedPng in referencedPngs)
            {
                BuildLogic.Self.TryAddXnbReferencesAndBuild(referencedPng, mainProject, saveProjectAfterAdd: false);

                foreach (var project in syncedProjects)
                {
                    BuildLogic.Self.TryAddXnbReferencesAndBuild(referencedPng, project, saveProjectAfterAdd: false);
                }
            }
        }

        private void RemoveXnbs()
        {
            var referencedPngs = GetReferencedPngs();

            var mainProject = GlueState.CurrentMainProject;
            ProjectBase[] syncedProjects = GlueState.SyncedProjects.ToArray();

            foreach (var referencedPng in referencedPngs)
            {
                BuildLogic.TryRemoveXnbReferences(mainProject, referencedPng, save: false);
                
                foreach (var project in syncedProjects)
                {
                    BuildLogic.TryRemoveXnbReferences(project, referencedPng, save: false);
                }
            }
        }

        private void SetAllPngRfsesToUseContentPipeline()
        {
            var referencedFiles = EditorObjects.IoC.Container.Get<IGlueState>().GetAllReferencedFiles();

            var allPngs = referencedFiles
                .Where(item => FileManager.GetExtension(item.Name) == "png")
                .ToArray();


            foreach (var rfs in allPngs)
            {
                rfs.UseContentPipeline = true;
            }
        }

        private void SetAllPngsRfsesToFromFileLoading()
        {
            var referencedFiles = EditorObjects.IoC.Container.Get<IGlueState>().GetAllReferencedFiles();

            var allRfses = referencedFiles
                .Where(item => FileManager.GetExtension(item.Name) == "png")
                .ToArray();

            foreach(var rfs in allRfses)
            {
                rfs.UseContentPipeline = false;
            }
        }

        private void RefreshAllFileMembership(bool useContentPipeline)
        {
            var referencedPngs = GetReferencedPngs();

            var projectCommands = GlueCommands.ProjectCommands;
            var mainProject = GlueState.CurrentMainProject;

            foreach (var referencedPng in referencedPngs)
            {
                projectCommands.UpdateFileMembershipInProject(mainProject, referencedPng, useContentPipeline: useContentPipeline, shouldLink: false);

                foreach(var project in GlueState.SyncedProjects)
                {
                    projectCommands.UpdateFileMembershipInProject(
                        project, referencedPng, useContentPipeline: useContentPipeline, shouldLink: true);
                }
            }

        }

        public static string[] GetReferencedPngs()
        {
            List<string> referencedFileNames = new List<string>();

            var referencedFiles = EditorObjects.IoC.Container.Get<IGlueState>().GetAllReferencedFiles();

            foreach (var rfs in referencedFiles)
            {
                string absolute = null;
                try
                {
                    absolute = GlueCommands.GetAbsoluteFileName(rfs);
                }
                catch(Exception e)
                {
                    // See if the project has been unloaded:
                    if(GlueState.CurrentGlueProject == null)
                    {
                        break;
                    }
                    else
                    {
                        throw e;
                    }
                }

                if (referencedFileNames.Any(item => Match(item, absolute)) == false)
                {
                    referencedFileNames.Add(absolute);
                    AddReferencedFilesRecursively(absolute, referencedFileNames);
                }
            }

            string[] referencedPngs = new string[0];
            if(GlueState.CurrentGlueProject != null)
            {
                referencedPngs = referencedFileNames
                    .Where(item => FileManager.GetExtension(item) == "png")
                    .Distinct()
                    .Select(item => FileManager.Standardize(item))
                    // Alphabetize for debugging, can get rid of this once this feature works well and I don't need to look at the list anymore:
                    .OrderBy(item =>item)
                    .ToArray();
            }
            // only get the PNGs:
            return referencedPngs;
        }

        private static void AddReferencedFilesRecursively(string absoluteFileName, List<string> referencedFileNames)
        {
            var referencedFiles = 
                EditorObjects.IoC.Container.Get<IGlueCommands>().FileCommands.GetFilesReferencedBy(absoluteFileName, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);
            

            foreach(var file in referencedFiles)
            {
                if (referencedFileNames.Any(item => Match(item, file)) == false)
                {
                    referencedFileNames.Add(file);
                    AddReferencedFilesRecursively(file, referencedFileNames);
                }

            }
        }

        static bool Match(string file1, string file2)
        {
            return FileManager.Standardize(file1).ToLowerInvariant() == FileManager.Standardize(file2).ToLowerInvariant();
        }
    }
}
