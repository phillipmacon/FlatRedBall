﻿using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Utilities;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin.Controllers
{
    public class CollisionRelationshipViewModelController
    {
        public static CollisionRelationshipViewModel CreateViewModel()
        {
            var viewModel = new CollisionRelationshipViewModel();
            viewModel.PropertyChanged += HandleViewModelPropertyChanged;

            return viewModel;
        }

        public static void RefreshAvailableCollisionObjects(IElement element, CollisionRelationshipViewModel viewModel)
        {
            viewModel.FirstCollisionItemSource.Clear();
            viewModel.SecondCollisionItemSource.Clear();

            var namedObjects = element.AllNamedObjects.ToArray();

            List<string> names = new List<string>();

            // consider:
            // 1. Individual ICollidables
            // 2. Lists of ICollidables
            // 3. TileShapeCollections
            // 4. ShapeCollections

            foreach (var nos in namedObjects)
            {
                bool shouldConsider = GetIfCanBeReferencedByRelationship(nos);

                if (shouldConsider)
                {
                    //names.Add(nos.InstanceName);
                    viewModel.FirstCollisionItemSource.Add(nos.InstanceName);
                    viewModel.SecondCollisionItemSource.Add(nos.InstanceName);

                }
            }
        }

        public static bool GetIfCanBeReferencedByRelationship(NamedObjectSave nos)
        {
            if(nos == null)
            {
                throw new ArgumentNullException(nameof(nos));
            }
            var nosElement = nos.GetReferencedElement();
            var nosAti = nos.GetAssetTypeInfo();

            var entity = nosElement as EntitySave;

            var shouldConsider = false;

            if (entity?.ImplementsICollidable == true)
            {
                shouldConsider = true;
            }

            if (!shouldConsider)
            {
                // See if it's a list of ICollidables
                shouldConsider = nos != null &&
                    nos.SourceType == SourceType.FlatRedBallType &&
                    nos.SourceClassType == "PositionedObjectList<T>" &&
                    !string.IsNullOrEmpty(nos.SourceClassGenericType) &&
                    ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType)?.ImplementsICollidable == true;
            }

            if (!shouldConsider)
            {
                shouldConsider = nosAti?.QualifiedRuntimeTypeName.QualifiedType ==
                    "FlatRedBall.TileCollisions.TileShapeCollection";
            }

            if (!shouldConsider)
            {
                shouldConsider = nosAti?.QualifiedRuntimeTypeName.QualifiedType ==
                    "FlatRedBall.Math.Geometry.ShapeCollection";
            }

            return shouldConsider;
        }

        static bool CanHaveSubCollisions(NamedObjectSave nos)
        {
            EntitySave entity = null;
            if (nos != null)
            {
                return GetEntitySaveReferencedBy(nos)?.ImplementsICollidable == true;
            }
            return false;
        }

        public static EntitySave GetEntitySaveReferencedBy(NamedObjectSave nos)
        {
            if (nos != null)
            {
                if (nos.SourceType == SourceType.Entity)
                {
                    return ObjectFinder.Self.GetEntitySave(nos.SourceClassType);
                }

                // if it's a list, make sure it's a list of collidables:
                if (nos.IsList && !string.IsNullOrEmpty(nos.SourceClassGenericType))
                {
                    var sourceClassGenericType = nos.SourceClassGenericType;

                    var entitySave = ObjectFinder.Self.GetEntitySave(sourceClassGenericType);
                    return entitySave;
                }
            }
            return null;
        }

        static bool IsShape(NamedObjectSave nos)
        {
            if(nos.SourceType == SourceType.FlatRedBallType)
            {
                return
                    nos.SourceClassType == "AxisAlignedRectangle" ||
                    nos.SourceClassType == "Circle" ||
                    nos.SourceClassType == "Line" ||
                    nos.SourceClassType == "Polygon";
            }
            return false;
        }

        public static void RefreshSubcollisionObjects(IElement element, CollisionRelationshipViewModel viewModel)
        {
            var firstNos = element.GetNamedObject(viewModel.FirstCollisionName);
            var secondNos = element.GetNamedObject(viewModel.SecondCollisionName);


            var canFirstHaveSubCollisions = CanHaveSubCollisions(firstNos);
            var canSecondHaveSubCollisions = CanHaveSubCollisions(secondNos);

            viewModel.FirstSubCollisionEnabled = canFirstHaveSubCollisions;
            viewModel.SecondSubCollisionEnabled = canSecondHaveSubCollisions;

            if(canFirstHaveSubCollisions)
            {
                var firstEntity = GetEntitySaveReferencedBy(firstNos);

                var list = GetAvailableValues(firstEntity);

                var oldValue = viewModel.FirstSubCollisionSelectedItem;

                viewModel.FirstSubCollisionItemsSource.Clear();
                foreach(var item in list)
                {
                    viewModel.FirstSubCollisionItemsSource.Add(item);
                }

                if(list.Contains(oldValue))
                {
                    viewModel.FirstSubCollisionSelectedItem = oldValue;
                }
            }
            else
            {
                viewModel.FirstSubCollisionSelectedItem = null;
            }

            if (canSecondHaveSubCollisions)
            {
                var secondEntity = GetEntitySaveReferencedBy(secondNos);

                var list = GetAvailableValues(secondEntity);

                var oldValue = viewModel.SecondSubCollisionSelectedItem;

                viewModel.SecondSubCollisionItemsSource.Clear();
                foreach(var item in list)
                {
                    viewModel.SecondSubCollisionItemsSource.Add(item);
                }

                if(list.Contains(oldValue))
                {
                    viewModel.SecondSubCollisionSelectedItem = oldValue;
                }
            }
            else
            {
                viewModel.SecondSubCollisionSelectedItem = null;
            }
        }

        private static List<string> GetAvailableValues(EntitySave firstEntity)
        {
            List<string> availableValues = new List<string>();
            availableValues.Add(CollisionRelationshipViewModel.EntireObject);

            foreach (var nos in firstEntity.AllNamedObjects)
            {
                if (IsShape(nos))
                {
                    availableValues.Add(nos.InstanceName);
                }
            }
            return availableValues;
        }

        public static void HandleGlueObjectPropertyChanged(string changedMember, object oldValue)
        {
            var element = GlueState.Self.CurrentElement;
            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            if (changedMember == nameof(NamedObjectSave.InstanceName) && namedObject != null)
            {
                UpdateCollisionRelationshipsInThisElement(element, oldValue);

                UpdateOtherObjectsIfChangedObjectIsShapeInCollidable(element, (string)oldValue);
            }
        }

        private static void UpdateOtherObjectsIfChangedObjectIsShapeInCollidable(IElement element, string oldName)
        {
            var asEntitySave = element as EntitySave;

            if(asEntitySave != null && asEntitySave.ImplementsICollidable)
            {
                var namedObject = GlueState.Self.CurrentNamedObjectSave;
                string newName = namedObject.InstanceName;

                if(IsShape(namedObject))
                {
                    // a shape was renamed. This could change the name of another collision relationship
                    var collisionRelationshipAti = AssetTypeInfoManager.Self.CollisionRelationshipAti;

                    foreach(var container in GlueState.Self.CurrentGlueProject.AllElements())
                    {
                        var collisionRelationships = container.AllNamedObjects
                            .Where(item =>
                            {
                                TryFixSourceClassType(item);
                                return item.GetAssetTypeInfo() == collisionRelationshipAti;
                            })
                            .ToArray();

                        foreach(var relationship in collisionRelationships)
                        {
                            var shouldRename = relationship
                                .Properties
                                .GetValue<bool>(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled));
                            if(shouldRename)
                            {
                                string firstNosName = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));

                                string secondNosName = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));

                                var firstNos = container.GetNamedObjectRecursively(firstNosName);
                                var secondNos = container.GetNamedObjectRecursively(secondNosName);

                                var firstElement = GetEntitySaveReferencedBy(firstNos);
                                var secondElement = GetEntitySaveReferencedBy(secondNos);

                                var firstSubCollision = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem));

                                var secondSubCollision = relationship
                                    .Properties
                                    .GetValue<string>(nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem));


                                var shouldApplyAutoName = false;
                                if (firstElement == element && firstSubCollision == oldName)
                                {
                                    relationship.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem), newName);
                                    shouldApplyAutoName = true;
                                }
                                if (secondElement == element && secondSubCollision == oldName)
                                {
                                    relationship.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem), newName);
                                    shouldApplyAutoName = true;
                                }

                                if(shouldApplyAutoName)
                                { 
                                    // we could test if it should be renamed but...why not just do a rename. We can be lazy here.
                                    // If there is no change in the name, it won't re-save or regenerate:
                                    TryApplyAutoName(container, relationship);
                                }

                            }


                        }
                    }



                }
            }
        }

        private static void UpdateCollisionRelationshipsInThisElement(IElement element, object oldValue)
        {
            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            var collisionRelationshipAti = AssetTypeInfoManager.Self.CollisionRelationshipAti;
            var allNamedObjects = element.AllNamedObjects.ToArray();
            var collisionRelationships = allNamedObjects
                .Where(item =>
                {
                    TryFixSourceClassType(item);
                    return item.GetAssetTypeInfo() == collisionRelationshipAti;
                })
                .ToArray();


            var oldName = (string)oldValue;
            bool changedAny = false;
            var newName = namedObject.InstanceName;

            string GetFirstCollision(NamedObjectSave nos)
            {
                return nos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.FirstCollisionName));
            }

            string GetSecondCollision(NamedObjectSave nos)
            {
                return nos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));
            }

            var withFirst = collisionRelationships
                .Where(item => GetFirstCollision(item) == oldName)
                .ToArray();

            foreach (var item in withFirst)
            {
                item.Properties.SetValue(nameof(CollisionRelationshipViewModel.FirstCollisionName), newName);
                changedAny = true;
                GlueCommands.Self.PrintOutput($"Renaming {item.FieldName}.{oldName} to {item.FieldName}.{newName}");
                TryFixSourceClassType(item);
                TryApplyAutoName(element, item);
            }

            var withSecond = collisionRelationships
                .Where(item => GetSecondCollision(item) == oldName)
                .ToArray();

            foreach (var item in withSecond)
            {
                item.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionName), newName);
                changedAny = true;
                GlueCommands.Self.PrintOutput($"Renaming {item.FieldName}.{oldName} to {item.FieldName}.{newName}");
                TryFixSourceClassType(item);
                TryApplyAutoName(element, item);
            }

            if (changedAny)
            {
                GlueCommands.Self.GluxCommands.SaveGluxTask();
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCodeTask();
            }
        }

        public static void TryFixSourceClassType(NamedObjectSave selectedNos)
        {

            if (selectedNos.SourceClassType == "CollisionRelationship" ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollisionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsPositionedObjectRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsListRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsPositionedObjectRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.PositionedObjectVsShapeCollection") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsShapeCollectionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.ListVsListRelationship") == true ||

                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableListVsTileShapeCollectionRelationship") == true ||
                selectedNos.SourceClassType?.StartsWith("FlatRedBall.Math.Collision.CollidableVsTileShapeCollectionRelationship") == true ||

                selectedNos.SourceClassType?.StartsWith("CollisionRelationship<") == true)
            {
                selectedNos.SourceClassType = AssetTypeInfoManager.Self.CollisionRelationshipAti
                    .QualifiedRuntimeTypeName.PlatformFunc(selectedNos);
            }
        }

        private static void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var viewModel = sender as CollisionRelationshipViewModel;

            ////////////////Early Out///////////////////
            if (viewModel.IsUpdatingFromGlueObject)
            {
                return;
            }

            //////////////End Early Out/////////////////

            var namedObject = GlueState.Self.CurrentNamedObjectSave;

            var element = GlueState.Self.CurrentElement;
            var nos = viewModel.GlueObject as NamedObjectSave;
            switch (e.PropertyName)
            {
                case nameof(viewModel.FirstCollisionName):
                case nameof(viewModel.SecondCollisionName):
                    CollisionRelationshipViewModelController.TryFixSourceClassType(nos);

                    RefreshSubcollisionObjects(element, viewModel);

                    TryApplyAutoName(element, namedObject);

                    if(TryFixMassesForTileShapeCollisionRelationship(element, nos))
                    {
                        viewModel.UpdateFromGlueObject();
                    }

                    break;
                case nameof(viewModel.FirstSubCollisionSelectedItem):
                    TryApplyAutoName(element, namedObject);

                    break;
                case nameof(viewModel.SecondSubCollisionSelectedItem):
                    TryApplyAutoName(element, namedObject);

                    break;
                case nameof(viewModel.IsAutoNameEnabled):
                    TryApplyAutoName(element, namedObject);
                    break;

                case nameof(viewModel.CollisionType):
                    if (TryFixMassesForTileShapeCollisionRelationship(element, nos))
                    {
                        viewModel.UpdateFromGlueObject();
                    }
                    break;
            }
        }

        public static void TryApplyAutoName(IElement element, NamedObjectSave namedObject)
        {
            var isAutoNameEnabled = namedObject.Properties.GetValue<bool>(nameof(CollisionRelationshipViewModel.IsAutoNameEnabled));
            if(isAutoNameEnabled)
            {
                var desiredName = GetAutoName(namedObject);

                bool nameExists = false;
                do
                {
                    nameExists = element.AllNamedObjects
                        .Any(item => item != namedObject &&
                                     item.InstanceName == desiredName);

                    if(nameExists)
                    {
                        if(StringFunctions.HasNumberAtEnd(desiredName))
                        {
                            desiredName = StringFunctions.IncrementNumberAtEnd(desiredName);
                        }
                        else
                        {
                            desiredName = desiredName + "2";
                        }
                    }
                } while (nameExists);

                if (desiredName != namedObject.InstanceName)
                {
                    namedObject.InstanceName = desiredName;

                    GlueCommands.Self.RefreshCommands.RefreshUi(element);
                    GlueCommands.Self.GluxCommands.SaveGluxTask();
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                }
            }
        }

        public static bool TryFixMassesForTileShapeCollisionRelationship(IElement selectedElement, NamedObjectSave collisionRelationshipNos)
        {
            // at this point "inverting" has already happened so check if the 2nd object is a tile shape collection

            var secondName = collisionRelationshipNos.Properties.GetValue<string>(nameof(CollisionRelationshipViewModel.SecondCollisionName));
            var secondObjectInstance = selectedElement.GetNamedObjectRecursively(secondName);

            var didMakeChange = false;

            if (secondObjectInstance?.SourceClassType?.Contains("TileShapeCollection") == true)
            {
                // set the first object's mass to 1, the 2nd object's mass to 1:
                var shouldSetTo0 =
                    collisionRelationshipNos.Properties.Any(item => item.Name == nameof(CollisionRelationshipViewModel.FirstCollisionMass)) == false ||
                    collisionRelationshipNos.Properties.GetValue<float>(nameof(CollisionRelationshipViewModel.FirstCollisionMass)) != 0;

                if (shouldSetTo0)
                {
                    collisionRelationshipNos.Properties.SetValuePersistIfDefault(nameof(CollisionRelationshipViewModel.FirstCollisionMass), 0f);
                    didMakeChange = true;
                }

                var shouldSetTo1 =
                    collisionRelationshipNos.Properties.Any(item => item.Name == nameof(CollisionRelationshipViewModel.SecondCollisionMass)) == false ||
                    collisionRelationshipNos.Properties.GetValue<float>(nameof(CollisionRelationshipViewModel.SecondCollisionMass)) != 1;

                if (shouldSetTo1)
                {
                    collisionRelationshipNos.Properties.SetValue(nameof(CollisionRelationshipViewModel.SecondCollisionMass), 1.0f);
                    didMakeChange = true;
                }
            }

            if(didMakeChange)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCodeTask();
                GlueCommands.Self.GluxCommands.SaveGluxTask();
            }

            return didMakeChange;
        }

        public static string GetAutoName(NamedObjectSave namedObject)
        {

            var firstName = namedObject.Properties.GetValue<string>(
                nameof(CollisionRelationshipViewModel.FirstCollisionName));
            var firstSub = namedObject.Properties.GetValue<string>(nameof(
                CollisionRelationshipViewModel.FirstSubCollisionSelectedItem));
            if(firstSub == CollisionRelationshipViewModel.EntireObject)
            {
                firstSub = null;
            }

            var secondName = namedObject.Properties.GetValue<string>(nameof(
                CollisionRelationshipViewModel.SecondCollisionName));
            var secondSub = namedObject.Properties.GetValue<string>(nameof(
                CollisionRelationshipViewModel.SecondSubCollisionSelectedItem));
            if(secondSub == CollisionRelationshipViewModel.EntireObject)
            {
                secondSub = null;
            }

            return $"{firstName}{firstSub}Vs{secondName}{secondSub}";
        }
    }
}
