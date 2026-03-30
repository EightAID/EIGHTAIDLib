using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Utility
{
    public class CsvLoaderBase<T> where T : new()
    {
        public List<T> DataList { get; private set; }

        public void Load(string fileName)
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"CSV file not found: {path}");
                return;
            }

            string csvText = File.ReadAllText(path);
            DataList = Parse(csvText);
        }

        private List<T> Parse(string csvText)
        {
            var list = new List<T>();
            using var reader = new StringReader(csvText);

            string headerLine = reader.ReadLine();
            if (headerLine == null)
            {
                return list;
            }

            string[] headers = headerLine.Split(',');
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                string[] values = line.Split(',');
                var item = new T();
                var fields = typeof(T).GetFields();

                for (int i = 0; i < headers.Length && i < values.Length; i++)
                {
                    foreach (var field in fields)
                    {
                        if (!field.Name.Equals(headers[i], StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        object value = Convert.ChangeType(values[i], field.FieldType);
                        field.SetValue(item, value);
                        break;
                    }
                }

                list.Add(item);
            }

            return list;
        }
    }
}
