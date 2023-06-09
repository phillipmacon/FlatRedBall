﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Localization;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math.Geometry;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Graphics.Particle;
using FlatRedBall.Math.Splines;
using FlatRedBall.Glue.RuntimeObjects.File;
using FlatRedBall.Glue.IO;

namespace FlatRedBall.Glue.RuntimeObjects
{
    public static class NamedObjectManager
    {
        
        public static LoadedFile LoadObjectForNos(NamedObjectSave namedObjectSave, IElement elementSave, Layer layerToPutOn,
            PositionedObjectList<ElementRuntime> listToPopulate, ElementRuntime elementRuntime)
        {
            if (string.IsNullOrEmpty(namedObjectSave.SourceName) || 
                namedObjectSave.SourceName == "<NONE>" || 
                namedObjectSave.SetByDerived)
            {
                return null;
            }
            else
            {

                var toReturn = LoadObject(namedObjectSave, elementSave, layerToPutOn, listToPopulate, elementRuntime);

                return toReturn;

            }
        }

        private static LoadedFile LoadObject(NamedObjectSave namedObjectSave, IElement elementSave, 
            Layer layerToPutOn,
            PositionedObjectList<ElementRuntime> listToPopulate, ElementRuntime elementRuntime)
        {
            int length = namedObjectSave.SourceName.Length;
            // need to use the last index of ( in case the name has a "(" in it)
            //int indexOfType = namedObjectSave.SourceName.IndexOf("(");
            // add 1 to not include the opening paren
            int indexOfType = namedObjectSave.SourceName.LastIndexOf("(") + 1;

            // subtract 1 to remove the last paren
            string objectType = namedObjectSave.SourceName.Substring(indexOfType, length - (indexOfType) - 1);
            string sourceFile = namedObjectSave.SourceFile;

            bool pullsFromEntireObject = false;
            ReferencedFileSave rfs = elementSave.GetReferencedFileSaveRecursively(sourceFile);

            var rfsFilePath = ElementRuntime.ContentDirectory + rfs.Name;

            // This is the original file that contains the object that is going to be cloned from.
            var loadedObjectToPullFrom =
                elementRuntime.ReferencedFileRuntimeList.LoadedRfses.FirstOrDefault(item => item.FilePath ==
                rfsFilePath);

            pullsFromEntireObject = loadedObjectToPullFrom != null;

            if (loadedObjectToPullFrom == null)
            {
                loadedObjectToPullFrom = elementRuntime.ReferencedFileRuntimeList.LoadReferencedFileSave(rfs, true, elementSave);
            }
            ElementRuntime newElementRuntime = CreateNewOrGetExistingElementRuntime(namedObjectSave, layerToPutOn, 
                listToPopulate, elementRuntime);

            object toAddTo = null;

            if (!namedObjectSave.IsEntireFile)
            {
                // we need to clone the container
                toAddTo = newElementRuntime.ReferencedFileRuntimeList.CreateAndAddEmptyCloneOf(loadedObjectToPullFrom);
            }

            Layer layerToAddTo = GetLayerToAddTo(namedObjectSave, layerToPutOn, elementRuntime);

            LoadedFile toReturn = null;
            if (loadedObjectToPullFrom != null)
            {
                // This might be null if the NOS references a file that doesn't exist.
                // This is usually not a valid circumstance but it's something that can
                // occur with tools modifying the .glux and not properly verifying that the
                // file exists.  GView should tolerate this invalid definition.

                toReturn = CreateRuntimeObjectForNamedObject(namedObjectSave, elementSave, elementRuntime, 
                    objectType,
                    loadedObjectToPullFrom,
                    newElementRuntime,
                    toAddTo,
                    layerToAddTo, rfs, pullsFromEntireObject);

                if(toReturn != null)
                {
                    elementRuntime.ReferencedFileRuntimeList.Add(toReturn);
                }

            }
            newElementRuntime.DirectObjectReference = toReturn?.RuntimeObject;


            return toReturn;

        }

        private static object GetObjectIfFileIsContained(string sourceFile, ElementRuntime elementRuntime)
        {
            if(elementRuntime.EntireScenes.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireScenes[sourceFile];
            }

            if(elementRuntime.EntireShapeCollections.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireShapeCollections[sourceFile];
            }

            if(elementRuntime.EntireEmitterLists.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireEmitterLists[sourceFile];
            }


            if(elementRuntime.EntireNodeNetworks.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireNodeNetworks[sourceFile];
            }

            if(elementRuntime.EntireSplineLists.ContainsKey(sourceFile))
            {
                return elementRuntime.EntireSplineLists[sourceFile];
            }

            return null;
        }

        private static void AddObjectToManagers(object loadedObject)
        {
            if (loadedObject is Scene)
            {
                ((Scene)loadedObject).AddToManagers();
            }
            else if (loadedObject is ShapeCollection)
            {
                ((ShapeCollection)loadedObject).AddToManagers();
            }
            else if (loadedObject is EmitterList)
            {
                ((EmitterList)loadedObject).AddToManagers();
            }
            else if (loadedObject is SplineList)
            {
                ((SplineList)loadedObject).AddToManagers();
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        

        private static ElementRuntime CreateNewOrGetExistingElementRuntime(NamedObjectSave namedObjectSave, Layer layerToPutOn, PositionedObjectList<ElementRuntime> listToPopulate, ElementRuntime parent)
        {
            ElementRuntime newOrExisting = null;
            for (int i = 0; i < listToPopulate.Count; i++)
            {
                if (listToPopulate[i].AssociatedNamedObjectSave == namedObjectSave)
                {
                    newOrExisting = listToPopulate[i];
                    break;
                }
            }

            if (newOrExisting == null)
            {
                newOrExisting = new ElementRuntime();
                newOrExisting.Initialize(null, layerToPutOn, namedObjectSave, parent.CreationOptions.OnBeforeVariableSet, parent.CreationOptions.OnAfterVariableSet);
                newOrExisting.Name = namedObjectSave.InstanceName;
                listToPopulate.Add(newOrExisting);
            }

            return newOrExisting;
        }


        /// <summary>
        ///  Creates a runtime object and adds the LoadedFile to the element.
        /// </summary>
        /// <param name="objectToLoad"></param>
        /// <param name="container"></param>
        /// <param name="elementRuntime"></param>
        /// <param name="objectType"></param>
        /// <param name="objectJustLoaded"></param>
        /// <param name="newElementRuntime"></param>
        /// <param name="toAddTo"></param>
        /// <param name="layerToAddTo"></param>
        /// <param name="rfs"></param>
        /// <param name="pullsFromEntireNamedObject"></param>
        /// <returns></returns>
        private static LoadedFile CreateRuntimeObjectForNamedObject(NamedObjectSave objectToLoad, 
            IElement container, ElementRuntime elementRuntime, string objectType,
            LoadedFile objectJustLoaded,
            ElementRuntime newElementRuntime, 
            object toAddTo,
            Layer layerToAddTo, ReferencedFileSave rfs, bool pullsFromEntireNamedObject)
        {
            var fileName = ElementRuntime.ContentDirectory + objectToLoad.SourceFile;


            bool shouldClone = rfs != null && (rfs.IsSharedStatic && !(container is ScreenSave)) && !pullsFromEntireNamedObject;
            LoadedFile toReturn = null;

            // This could have a ( in the name in a file like .scnx, so use the last (
            //int indexOfType = objectToLoad.SourceName.IndexOf("(");
            int indexOfType = objectToLoad.SourceName.LastIndexOf("(");

            string objectName = objectToLoad.SourceName.Substring(0, indexOfType - 1);

            switch (objectType)
            {
                case "Scene":
                    {
                        Scene scene = objectJustLoaded.RuntimeObject as Scene;

                        foreach (Text text in scene.Texts)
                        {
                            text.AdjustPositionForPixelPerfectDrawing = true;
                            if (ObjectFinder.Self.GlueProject.UsesTranslation)
                            {
                                text.DisplayText = LocalizationManager.Translate(text.DisplayText);
                            }
                        }

                        if (shouldClone)
                        {
                            scene = scene.Clone();
                            elementRuntime.EntireScenes.Add(objectToLoad.SourceFile, scene);

                            var loadedFile = new LoadedFile();
                            loadedFile.RuntimeObject = scene;
                            loadedFile.FilePath = objectToLoad.SourceFile;
                            loadedFile.ReferencedFileSave = rfs;
                            toReturn = loadedFile;

                            newElementRuntime.ReferencedFileRuntimeList.Add(loadedFile);

                            scene.AddToManagers(layerToAddTo);

                        }
                    }
                    break;


                case "Sprite":
                    {
                        Sprite loadedSprite = null;
                        Scene scene = objectJustLoaded.RuntimeObject as Scene;
                        if (scene != null)
                        {
                            loadedSprite = scene.Sprites.FindByName(objectName);
                        }

                        if (loadedSprite == null)
                        {
                            System.Windows.Forms.MessageBox.Show("There is a missing Sprite called\n\n" + objectName + "\n\n" +
                                "in the object\n\n" + elementRuntime.Name + "\n\n" +
                                "This probably happened if someone changed the name of a Sprite in a .scnx file but didn't update " +
                                "the associated object in Glue", "Missing Sprite");
                        }
                        else
                        {
                            if (shouldClone)
                            {
                                loadedSprite = loadedSprite.Clone();
                                (toAddTo as Scene).Sprites.Add(loadedSprite);
                                SpriteManager.AddToLayer(loadedSprite, layerToAddTo);

                                var loadedFile = new LoadedFile();

                                loadedFile.RuntimeObject = loadedSprite;
                                loadedFile.FilePath = objectJustLoaded.FilePath;
                                loadedFile.DataModel = objectJustLoaded.DataModel;
                                loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;

                                toReturn = loadedFile;
                            }
                        }
                    }
                    break;


                case "SpriteFrame":
                    {
                        Scene scene = objectJustLoaded.RuntimeObject as Scene;
                        SpriteFrame loadedSpriteFrame = scene.SpriteFrames.FindByName(objectName);
                        if (loadedSpriteFrame != null)
                        {
                            if (shouldClone)
                            {
                                loadedSpriteFrame = loadedSpriteFrame.Clone();
                                (toAddTo as Scene).SpriteFrames.Add(loadedSpriteFrame);
                                SpriteManager.AddToLayer(loadedSpriteFrame, layerToAddTo);

                                var loadedFile = new LoadedFile();

                                loadedFile.RuntimeObject = loadedSpriteFrame;
                                loadedFile.FilePath = objectJustLoaded.FilePath;
                                loadedFile.DataModel = objectJustLoaded.DataModel;
                                loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                                toReturn = loadedFile;
                            }
                        }
                    }
                    break;

                case "SpriteGrid":
                    {
                        Scene scene = objectJustLoaded.RuntimeObject as Scene;
                        SpriteGrid spriteGrid = null;
                        for (int i = 0; i < scene.SpriteGrids.Count; i++)
                        {
                            if (scene.SpriteGrids[i].Name == objectName)
                            {
                                spriteGrid = scene.SpriteGrids[i];
                                break;
                            }
                        }
                        if (spriteGrid != null)
                        {
                            if (shouldClone)
                            {
                                spriteGrid = spriteGrid.Clone();
                                (toAddTo as Scene).SpriteGrids.Add(spriteGrid);
                                spriteGrid.Layer = layerToAddTo;
                                spriteGrid.PopulateGrid();
                                spriteGrid.RefreshPaint();
                                spriteGrid.Manage();

                                var loadedFile = new LoadedFile();

                                loadedFile.RuntimeObject = spriteGrid;
                                loadedFile.FilePath = objectJustLoaded.FilePath;
                                loadedFile.DataModel = objectJustLoaded.DataModel;
                                loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                                toReturn = loadedFile;
                            }


                        }
                    }
                    break;
                    
                case "Text":
                    {
                        Scene scene = objectJustLoaded.RuntimeObject as Scene;

                        Text loadedText = scene.Texts.FindByName(objectName);
                        if (loadedText != null)
                        {
                            if (shouldClone)
                            {
                                loadedText = loadedText.Clone();
                                (toAddTo as Scene).Texts.Add(loadedText);

                                TextManager.AddToLayer(loadedText, layerToAddTo);

                                var loadedFile = new LoadedFile();

                                loadedFile.RuntimeObject = loadedText;
                                loadedFile.FilePath = objectJustLoaded.FilePath;
                                loadedFile.DataModel = objectJustLoaded.DataModel;
                                loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                                toReturn = loadedFile;

                            }
                            loadedText.AdjustPositionForPixelPerfectDrawing = true;
                            if (LocalizationManager.HasDatabase)
                            {
                                loadedText.DisplayText = LocalizationManager.Translate(loadedText.DisplayText);
                            }
                        }
                    }
                    break;
                case "ShapeCollection":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded.RuntimeObject as ShapeCollection;

                        if (shouldClone)
                        {
                            shapeCollection = shapeCollection.Clone();
                            elementRuntime.EntireShapeCollections.Add(objectToLoad.SourceFile, shapeCollection);

                            newElementRuntime.ReferencedFileRuntimeList.LoadedShapeCollections.Add(shapeCollection);

                            shapeCollection.AddToManagers(layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = shapeCollection;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                        // Most cases are handled below in an AttachTo method, but 
                        // ShapeCollection isn't a PositionedObject so we have to do it manually here
                        if (objectToLoad.AttachToContainer)
                        {
                            shapeCollection.AttachTo(elementRuntime, true);
                        }
                    }
                    break;

                case "AxisAlignedCube":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded.RuntimeObject as ShapeCollection;
                        AxisAlignedCube loadedAxisAlignedCube = shapeCollection.AxisAlignedCubes.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedAxisAlignedCube = loadedAxisAlignedCube.Clone();
                            (toAddTo as ShapeCollection).AxisAlignedCubes.Add(loadedAxisAlignedCube);
                            ShapeManager.AddToLayer(loadedAxisAlignedCube, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = loadedAxisAlignedCube;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }

                    }
                    break;


                case "AxisAlignedRectangle":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded.RuntimeObject as ShapeCollection;
                        AxisAlignedRectangle loadedAxisAlignedRectangle = shapeCollection.AxisAlignedRectangles.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedAxisAlignedRectangle = loadedAxisAlignedRectangle.Clone();
                            (toAddTo as ShapeCollection).AxisAlignedRectangles.Add(loadedAxisAlignedRectangle);
                            ShapeManager.AddToLayer(loadedAxisAlignedRectangle, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = loadedAxisAlignedRectangle ;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                    }
                    break;

                case "Circle":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded.RuntimeObject as ShapeCollection;
                        Circle loadedCircle = shapeCollection.Circles.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedCircle = loadedCircle.Clone();
                            (toAddTo as ShapeCollection).Circles.Add(loadedCircle);
                            ShapeManager.AddToLayer(loadedCircle, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = loadedCircle;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                    }
                    break;

                case "Polygon":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded.RuntimeObject as ShapeCollection;
                        Polygon loadedPolygon = shapeCollection.Polygons.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedPolygon = loadedPolygon.Clone();
                            (toAddTo as ShapeCollection).Polygons.Add(loadedPolygon);
                            ShapeManager.AddToLayer(loadedPolygon, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = loadedPolygon;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                    }
                    break;

                case "Sphere":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded.RuntimeObject as ShapeCollection;
                        Sphere loadedSphere = shapeCollection.Spheres.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedSphere = loadedSphere.Clone();
                            (toAddTo as ShapeCollection).Spheres.Add(loadedSphere);
                            ShapeManager.AddToLayer(loadedSphere, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = loadedSphere;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                    }
                    break;

                case "Capsule2D":
                    {
                        ShapeCollection shapeCollection = objectJustLoaded.RuntimeObject as ShapeCollection;
                        Capsule2D loadedCapsule2D = shapeCollection.Capsule2Ds.FindByName(objectName);


                        if (shouldClone)
                        {
                            loadedCapsule2D = loadedCapsule2D.Clone();
                            (toAddTo as ShapeCollection).Capsule2Ds.Add(loadedCapsule2D);
                            ShapeManager.AddToLayer(loadedCapsule2D, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = loadedCapsule2D;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                    }
                    break;
                case "Emitter":
                    {
                        EmitterList emitterList = objectJustLoaded.RuntimeObject as EmitterList;
                        Emitter loadedEmitter = emitterList.FindByName(objectName);

                        if (shouldClone && loadedEmitter != null)
                        {
                            loadedEmitter = loadedEmitter.Clone();
                            (toAddTo as EmitterList).Add(loadedEmitter);
                            SpriteManager.AddEmitter(loadedEmitter, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = loadedEmitter;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                    }
                    break;
                case "EmitterList":
                    {
                        EmitterList emitterList = objectJustLoaded.RuntimeObject as EmitterList;

                        if(shouldClone && emitterList != null)
                        {
                            emitterList = emitterList.Clone();

                            foreach(var item in emitterList)
                            {
                                SpriteManager.AddEmitter(item);
                            }

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = emitterList;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                    }
                    break;
                case "NodeNetwork":
                    {
                        NodeNetwork nodeNetwork = objectJustLoaded.RuntimeObject as NodeNetwork;

                        if (shouldClone)
                        {
                            nodeNetwork = nodeNetwork.Clone();

                            elementRuntime.EntireNodeNetworks.Add(objectToLoad.SourceFile, nodeNetwork);

                            newElementRuntime.ReferencedFileRuntimeList.LoadedNodeNetworks.Add(nodeNetwork);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = nodeNetwork;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }

                    }
                    break;
                case "SplineList":
                    {
                        SplineList splineList = objectJustLoaded.RuntimeObject as SplineList;

                        if (shouldClone)
                        {
                            splineList = splineList.Clone();

                            elementRuntime.EntireSplineLists.Add(splineList.Name, splineList);

                            foreach (var spline in splineList)
                            {
                                spline.CalculateVelocities();
                                spline.CalculateAccelerations();
                            }

                            splineList.AddToManagers();
                            

                            splineList[0].UpdateShapes();

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = splineList;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }


                    }
                    break;

                case "Spline":
                    {
                        SplineList splineList = objectJustLoaded.RuntimeObject as SplineList;
                        Spline spline = splineList.FirstOrDefault(item=>item.Name == objectName);

                        
                        if (shouldClone && spline != null)
                        {
                            spline = spline.Clone();
                            (toAddTo as SplineList).Add(spline);

                            // Eventually support layers?
                            //ShapeManager.AddToLayer(spline, layerToAddTo);

                            var loadedFile = new LoadedFile();

                            loadedFile.RuntimeObject = spline;
                            loadedFile.FilePath = objectJustLoaded.FilePath;
                            loadedFile.DataModel = objectJustLoaded.DataModel;
                            loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                            toReturn = loadedFile;

                        }
                        spline.CalculateVelocities();
                        spline.CalculateAccelerations();
                        
                    }
                    break;
            }

            if(toReturn == null)
            {
                foreach(var manager in ReferencedFileRuntimeList.FileManagers)
                {
                    var objectFromFile = manager.TryGetObjectFromFile(elementRuntime.ReferencedFileRuntimeList.LoadedRfses, rfs, objectType, objectName);

                    if(objectFromFile != null)
                    {

                        var runtimeObject = objectFromFile;

                        var loadedFile = new LoadedFile();

                        loadedFile.RuntimeObject = runtimeObject;
                        loadedFile.FilePath = objectJustLoaded.FilePath;
                        loadedFile.DataModel = objectJustLoaded.DataModel;
                        loadedFile.ReferencedFileSave = objectJustLoaded.ReferencedFileSave;
                        toReturn = loadedFile;


                        break;
                    }
                }
            }


            if (toReturn != null && objectToLoad.AttachToContainer)
            {
                if (toReturn.RuntimeObject is PositionedObject)
                {
                    // If the object is already attached to something, that means it
                    // came from a file, so we don't want to re-attach it.
                    PositionedObject asPositionedObject = toReturn.RuntimeObject as PositionedObject;
                    if (asPositionedObject.Parent == null)
                    {
                        asPositionedObject.AttachTo(elementRuntime, true);
                    }
                }
                else if (toReturn.RuntimeObject is Scene)
                {
                    ((Scene)toReturn.RuntimeObject).AttachAllDetachedTo(elementRuntime, true);
                }
                else if (toReturn.RuntimeObject is ShapeCollection)
                {
                    ((ShapeCollection)toReturn.RuntimeObject).AttachAllDetachedTo(elementRuntime, true);
                }
            }


            return toReturn;
        }



        private static Layer GetLayerToAddTo(NamedObjectSave objectToLoad, Layer layerToPutOn, ElementRuntime elementRuntime)
        {
            Layer layerToAddTo = layerToPutOn;

            if (!string.IsNullOrEmpty(objectToLoad.LayerOn))
            {

                if (objectToLoad.LayerOn == "Under Everything (Engine Layer)")
                {
                    layerToAddTo = SpriteManager.UnderAllDrawnLayer;
                }
                else if(objectToLoad.LayerOn == "Top Layer (Engine Layer)")
                {
                    layerToAddTo = SpriteManager.TopLayer;
                }
                else
                {
                    ElementRuntime layerContainer = elementRuntime.GetContainedElementRuntime(objectToLoad.LayerOn);
                    if (layerContainer == null)
                    {
                        System.Windows.Forms.MessageBox.Show(
                            "Could not find a Layer by the name \"" + objectToLoad.LayerOn + "\" in the object " + objectToLoad, 
                            "Layer not found");
                    }
                    else
                    {
                        layerToAddTo = ((Layer)layerContainer.DirectObjectReference);
                    }
                }

            }
            return layerToAddTo;
        }




    }
}
