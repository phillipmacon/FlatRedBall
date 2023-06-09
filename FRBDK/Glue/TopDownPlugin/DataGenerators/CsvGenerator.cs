﻿using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopDownPlugin.Models;
using TopDownPlugin.ViewModels;

namespace TopDownPlugin.DataGenerators
{
    public class CsvGenerator : Singleton<CsvGenerator>
    {
        #region Fields/Properties

        public const string StrippedCsvFile = "TopDownValues";
        public const string RelativeCsvFile = StrippedCsvFile + ".csv";

        #endregion

        public FilePath CsvFileFor(EntitySave entity)
        {
            string absoluteFileName = GlueCommands.Self.FileCommands.GetContentFolder(entity) + RelativeCsvFile;
            return absoluteFileName;
        }

        internal void GenerateFor(EntitySave entity, TopDownEntityViewModel viewModel, CsvHeader[] lastHeaders)
        {
            string contents = GenerateCsvContents(entity, viewModel, lastHeaders);

            string fileName = CsvFileFor(entity).FullPath;

            GlueCommands.Self.TryMultipleTimes(() =>
            {
                FileManager.SaveText(contents, fileName);
            });
        }

        private string GenerateCsvContents(EntitySave entity, TopDownEntityViewModel viewModel, CsvHeader[] headers)
        {
            List<TopDownValues> values = new List<TopDownValues>();

            foreach(var valuesViewModel in viewModel.TopDownValues)
            {
                var topDownValues = valuesViewModel.ToValues();

                values.Add(topDownValues);
            }

            RuntimeCsvRepresentation rcr = RuntimeCsvRepresentation.FromList(values);


            if(headers != null)
            {
                rcr.Headers = headers;

                for(int rowIndex = 0; rowIndex < rcr.Records.Count; rowIndex++)
                {
                    var row = rcr.Records[rowIndex];
                    var topDownValues = values[rowIndex];

                    var rowRecordAsList = row.ToList();

                    for (int columnIndex = row.Length; columnIndex < headers.Length; columnIndex++)
                    {
                        var headerName = headers[columnIndex].Name;

                        if (topDownValues.AdditionalValues.ContainsKey(headerName))
                        {
                            var value = topDownValues.AdditionalValues[headerName] as TypedValue;

                            // does this need to account for culture?
                            rowRecordAsList.Add(value?.Value?.ToString());

                        }
                    }

                    rcr.Records[rowIndex] = rowRecordAsList.ToArray();
                }
            }

            // assume header[0] is name, so make it required:
            if(rcr.Headers.Length > 0)
            {
                rcr.Headers[0].IsRequired = true;
                rcr.Headers[0].OriginalText = rcr.Headers[0].Name + " (string, required)";
            }

            var toReturn = rcr.GenerateCsvString();


            return toReturn;
        }


    }
}
