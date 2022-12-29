﻿using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPluginCore.CodeGeneration
{
    internal class GumGame1CodeGeneratorEarly : Game1CodeGenerator
    {
        bool hasSkia => GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasGumSkiaElements &&
            AppState.Self.HasAddedGumSkiaElements;

        public GumGame1CodeGeneratorEarly() => CodeLocation = FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.BeforeStandardGenerated;

        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            if (hasSkia)
            {
                codeBlock.Line("GumRuntime.InstanceSaveExtensionMethods.CustomObjectCreation = GetSkiaType;");
            }
        }
    }

    internal class GumGame1CodeGenerator : Game1CodeGenerator
    {
        bool hasSkia => GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasGumSkiaElements &&
            AppState.Self.HasAddedGumSkiaElements;



        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            var fileVersion = GlueState.Self.CurrentGlueProject.FileVersion;
            if (fileVersion >= (int)GluxVersions.HasFrameworkElementManager)
            {
                codeBlock.Line("FlatRedBall.FlatRedBallServices.AddManager(FlatRedBall.Forms.Managers.FrameworkElementManager.Self);");
            }

            if(hasSkia)
            {
                codeBlock.Line("GumRuntime.InstanceSaveExtensionMethods.CustomObjectCreation = GetSkiaType;");
            }
        }

        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(hasSkia)
            {
                var function = codeBlock.Function("RenderingLibrary.Graphics.IRenderable", "GetSkiaType", "string name");
                var switchStatement = function.Switch("name");
                switchStatement.CaseNoBreak("\"Arc\"").Line("return new SkiaPlugin.Renderables.RenderableArc();");
                switchStatement.CaseNoBreak("\"ColoredCircle\"").Line("return new SkiaPlugin.Renderables.RenderableCircle();");
                switchStatement.CaseNoBreak("\"RoundedRectangle\"").Line("return new SkiaPlugin.Renderables.RenderableRoundedRectangle();");
                function.Line("return null;");
            }
        }
    }
}
