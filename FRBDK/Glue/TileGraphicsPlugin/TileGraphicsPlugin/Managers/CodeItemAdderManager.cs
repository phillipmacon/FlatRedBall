﻿using FlatRedBall.Glue.VSHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TileGraphicsPlugin.Managers
{
    public class CodeItemAdderManager : FlatRedBall.Glue.Managers.Singleton<CodeItemAdderManager>
    {
        CodeBuildItemAdder mTileGraphicsAdder;
        CodeBuildItemAdder mTileCollisionAdder;
        CodeBuildItemAdder mTileEntityAdder;

        public void AddFilesToCodeBuildItemAdder()
        {
            mTileGraphicsAdder = new CodeBuildItemAdder();
            mTileGraphicsAdder.OutputFolderInProject = "TileGraphics";
            mTileGraphicsAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/AnimationChainContainer.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/LayeredTileMap.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/LayeredTileMapAnimation.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/MapDrawableBatch.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/Tileset.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/ReducedTileMapInfo.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TileAnimationFrame.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TileNodeNetworkCreator.cs");



            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/AbstractMapLayer.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/ExternalTileset.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/MapLayer.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/MapTileset.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/MapTilesetTile.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/NamedValue.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TiledMapSave.Conversion.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TiledMapSave.Serialization.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TileAnimation.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TiledMapToShapeCollectionConverter.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TilesetExtensionMethods.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TilesetExtensionMethods.cs");
            mTileGraphicsAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/ReducedTileMapInfo.TiledMapSave.cs");

            


            mTileCollisionAdder = new CodeBuildItemAdder();
            mTileCollisionAdder.OutputFolderInProject = "TileCollisions";
            mTileCollisionAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileCollisionAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TileShapeCollection.cs");
            mTileCollisionAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/CollidableListVsTileShapeCollectionRelationship.cs");
            mTileCollisionAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/CollidableVsTileShapeCollectionRelationship.cs");
            mTileCollisionAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/CollisionManagerTileShapeCollectionExtensions.cs");

            mTileEntityAdder = new CodeBuildItemAdder();
            mTileEntityAdder.OutputFolderInProject = "TileEntities";
            mTileEntityAdder.AddFileBehavior = AddFileBehavior.AlwaysCopy;
            mTileEntityAdder.Add("TileGraphicsPlugin/EmbeddedCodeFiles/TileEntityInstantiator.cs");
        }


        public void UpdateCodeInProjectPresence()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            mTileGraphicsAdder.PerformAddAndSave(assembly);
            mTileCollisionAdder.PerformAddAndSave(assembly);
            mTileEntityAdder.PerformAddAndSave(assembly);
        }

    }



}
