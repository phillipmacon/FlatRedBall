using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.IO;

#if FRB_XNA
using Microsoft.Xna.Framework;
#endif

#if SILVERLIGHT
using System.Windows.Resources;
#endif


namespace FlatRedBall.IO.Csv
{
    #region XML Docs
    /// <summary>
    /// Class providing methods for interacting with .CSV spreadsheet files.
    /// </summary>
    #endregion
    public static class CsvFileManager
    {
        public static char Delimiter = ',';

#if FRB_RAW
        public static string ContentManagerName = "Global";
#else
        public static string ContentManagerName = FlatRedBallServices.GlobalContentManager;
#endif

        public static List<object> CsvDeserializeList(Type typeOfElement, string fileName)
        {

            List<object> listOfObjects = new List<object>();


            CsvDeserializeList(typeOfElement, fileName, listOfObjects);

            return listOfObjects;
        }

        public static void CsvDeserializeList(Type typeOfElement, string fileName, IList listToPopulate)
        {
            RuntimeCsvRepresentation rcr = CsvDeserializeToRuntime(fileName);

            rcr.CreateObjectList(typeOfElement, listToPopulate, ContentManagerName);
        }

        public static void CsvDeserializeDictionary<KeyType, ValueType>(string fileName, Dictionary<KeyType, ValueType> dictionaryToPopulate)
        {
            RuntimeCsvRepresentation rcr = null;

            CsvDeserializeDictionary(fileName, dictionaryToPopulate, out rcr);
        }

        public static void CsvDeserializeDictionary<KeyType, ValueType>(string fileName, Dictionary<KeyType, ValueType> dictionaryToPopulate, out RuntimeCsvRepresentation rcr)
        {
            rcr = CsvDeserializeToRuntime(fileName);

            rcr.CreateObjectDictionary<KeyType, ValueType>(dictionaryToPopulate, ContentManagerName);
        }

        public static RuntimeCsvRepresentation CsvDeserializeToRuntime(string fileName)
        {
            return CsvDeserializeToRuntime<RuntimeCsvRepresentation>(fileName);
        }

        public static T CsvDeserializeToRuntime<T>(string fileName) where T : RuntimeCsvRepresentation, new()
        {
            if (FileManager.IsRelative(fileName))
            {
                fileName = FileManager.MakeAbsolute(fileName);
            }

			#if ANDROID || IOS
			fileName = fileName.ToLowerInvariant();
			#endif

            FileManager.ThrowExceptionIfFileDoesntExist(fileName);

            T runtimeCsvRepresentation = null;

            string extension = FileManager.GetExtension(fileName).ToLower();
            if (extension == "csv" || extension == "txt")
            {
#if SILVERLIGHT || XBOX360 || WINDOWS_PHONE || MONOGAME
                
                Stream stream = FileManager.GetStreamForFile(fileName);

#else
                // Creating a filestream then using that enables us to open files that are open by other apps.
                FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#endif
                System.IO.StreamReader streamReader = new StreamReader(stream);


                using (CsvReader csv = new CsvReader(streamReader, true, Delimiter, CsvReader.DefaultQuote, CsvReader.DefaultEscape, CsvReader.DefaultComment, true, CsvReader.DefaultBufferSize))
                {
                    runtimeCsvRepresentation = new T();

                    string[] fileHeaders = csv.GetFieldHeaders();
                    runtimeCsvRepresentation.Headers = new CsvHeader[fileHeaders.Length];

                    for (int i = 0; i < fileHeaders.Length; i++)
                    {
                        runtimeCsvRepresentation.Headers[i] = new CsvHeader(fileHeaders[i]);
                    }

                    int numberOfHeaders = runtimeCsvRepresentation.Headers.Length;

                    runtimeCsvRepresentation.Records = new List<string[]>();

                    int recordIndex = 0;
                    int columnIndex = 0;
                    string[] newRecord = null;
                    try
                    {
                        while (csv.ReadNextRecord())
                        {


                            newRecord = new string[numberOfHeaders];
                            if (recordIndex == 123)
                            {
                                int m = 3;
                            }
                            bool anyNonEmpty = false;
                            for (columnIndex = 0; columnIndex < numberOfHeaders; columnIndex++)
                            {
                                string record = csv[columnIndex];

                                newRecord[columnIndex] = record;
                                if (record != "")
                                {
                                    anyNonEmpty = true;
                                }
                            }

                            if (anyNonEmpty)
                            {
                                runtimeCsvRepresentation.Records.Add(newRecord);
                            }
                            recordIndex++;
                        }
                    }
                    catch (Exception e)
                    {
                        string message = 
                            "Error reading record " + recordIndex + " at column " + columnIndex;

                        if(columnIndex != 0 && newRecord != null)
                        {
                            foreach(string s in newRecord)
                            {

                                    message += "\n" + s;
                            }
                        }

                        throw new Exception(message, e);

                    }
                }

                // Vic says - not sure how this got here, but it causes a crash!
                //streamReader.DiscardBufferedData();

                FileManager.Close(streamReader);
                streamReader.Dispose();
                FileManager.Close(stream);
                stream.Dispose();


#if XBOX360
                if (FileManager.IsFileNameInUserFolder(fileName))
                {
                    FileManager.DisposeLastStorageContainer();
                }
#endif
            }

            return runtimeCsvRepresentation;
        }


        public static void Serialize(RuntimeCsvRepresentation rcr, string fileName)
        {
            if (rcr == null)
                throw new ArgumentNullException("rcr");

            string toSave = rcr.GenerateCsvString(Delimiter);

            FileManager.SaveText(toSave, fileName);
        }

        private static void AppendMemberValue(StringBuilder stringBuilder, ref bool first, Type type, Object valueAsObject)
        {
            if (first)
                first = false;
            else
                stringBuilder.Append(" ,");

            String value;
            bool isString = false;

            if (type == typeof(string)) //check if the value is a string if so, it should be surrounded in quotes
            {
                isString = true;
            }

            if (valueAsObject == null)
            {
                value = "";
                stringBuilder.Append(value);
            }
            else                                  //if not null, append the value
            {
                if (isString)
                {
                    stringBuilder.Append("\"");
                }

                value = valueAsObject.ToString();
                value = value.Replace('\n', ' ');      //replace newlines 

                stringBuilder.Append(value);

                if (isString)
                {
                    stringBuilder.Append("\"");
                }
            }
        }
    }
}