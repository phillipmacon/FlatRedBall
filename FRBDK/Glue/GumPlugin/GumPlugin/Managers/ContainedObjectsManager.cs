﻿using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.Managers
{
    public class ContainedObjectsManager : Singleton<ContainedObjectsManager>
    {
        public bool HandleTryAddContainedObjects(string absoluteFile, List<string> availableObjects)
        {
            string extension = FileManager.GetExtension(absoluteFile);
            bool isEitherScreenOrComponent = extension == GumProjectSave.ComponentExtension ||
                extension == GumProjectSave.ScreenExtension;

            // We don't want the "Entire File" option
            // Victor Chelaru Feb 24
            // Why not? This clears out
            // all entire objects! Like if
            // the user selects a .scnx file.
            // This is bad, so we should not clear
            // this unless the file is a .gumx or .gusx
            // or .gucx or .gutx
            // Update March 11, 2015
            // Actually we're no longer
            // going to use the weird "this"
            // syntax and instead just use the
            // entire file syntax.
            //if (extension == GumProjectSave.ScreenExtension ||
            //    extension == GumProjectSave.ComponentExtension ||
            //    extension == GumProjectSave.StandardExtension ||
            //    extension == GumProjectSave.ProjectExtension)
            //{
            //    availableObjects.Clear();
            //}
            // Update March 12, 2015
            // Actually we do want to clear everything if it's a .gucx, and only add the Entire File with the runtime type
            if(extension == GumProjectSave.ComponentExtension)
            {
                availableObjects.Clear();

            }

            if (isEitherScreenOrComponent)
            {

                ElementSave element = null;


                if (extension == GumProjectSave.ComponentExtension)
                {
                    element =
                        FileManager.XmlDeserialize<ComponentSave>(absoluteFile);
                }
                else
                {
                    element =
                        FileManager.XmlDeserialize<Gum.DataTypes.ScreenSave>(absoluteFile);
                }

                // Victor Chelaru, November 1, 2015
                // Initially I used a "this" syntax to
                // get access to the Screen casted as its
                // current type. But I didn't like the "this" 
                // syntax, so I removed it to replace it with "Entire File".
                // Unfortunately, Entire File does a simple assignment, but screens
                // are loaded as IDB's, not as their runtime type. Maybe this should change
                // in the future (which would require their runtime types to inherit from IDBs),
                // but in the meantime, I'm going to revert back to using the "this" syntax so that
                // Screens can be casted appropriately:
                //availableObjects.Add("Entire File (" + element.Name + "Runtime" + ")");
                // only add this if it's an IDB:
                var rfsAti = GlueState.Self.CurrentReferencedFileSave?.GetAssetTypeInfo();
                if ( rfsAti == AssetTypeInfoManager.Self.ScreenIdbAti)
                {
                    availableObjects.Add("this (" + 
                        GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(element) + ")");
                }
                else if(rfsAti != null)
                {
                    availableObjects.Add(
                        $"Entire File ({rfsAti.RuntimeTypeName})");
                }


                foreach (var instance in element.Instances)
                {
                    var elementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);

                    string gueType = "";

                    if (GueDerivingClassCodeGenerator.Self.ShouldGenerateRuntimeFor(elementSave))
                    {
                        gueType = GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(instance, element);
                    }
                    else
                    {
                        gueType = "Gum.Wireframe.GraphicalUiElement";
                    }
                    availableObjects.Add(instance.Name + " (" + gueType + ")");

                }

            }



            return isEitherScreenOrComponent;
        }
    }
}
