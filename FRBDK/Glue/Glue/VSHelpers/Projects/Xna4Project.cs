﻿using System;
using System.Collections.Generic;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class Xna4Project : VisualStudioProject
    {
        public Xna4Project(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "Xna4"; } }

        public override string PrecompilerDirective { get { return "XNA4_WINDOWS"; } }

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                           {
                               @"Xna4Pc\FlatRedBall.Content.dll",
                               @"Xna4Pc\FlatRedBall.dll"
                           };
            }
        }

        public override string FolderName
        {
            get { return "Xna4"; }
        }


        protected override string ContentProjectDirectory
        {
            get
            {
                var contentDirectory = FileManager.GetDirectory(Directory);
                contentDirectory += Name + "Content";

                return contentDirectory;
            }
        }

        public override void LoadContentProject()
        {
            List<string> files = FileManager.GetAllFilesInDirectory(ContentProjectDirectory, "contentproj", 0);

            if (files.Count != 0)
            {
                ContentProject = ProjectCreator.LoadXnaProjectFor(this, files[0]);
            }
            else
            {
                ContentProject = this;
            }
        }

        public override string NeededVisualStudioVersion
        {
            get { return "10.0"; }
        }
    }
}
