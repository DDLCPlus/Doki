using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace UniLib
{
    [Serializable]
    public class PersistentSaveFileEntry
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public object Value { get; set; }
    }

    public class PersistentSaveFile
    {
        private FileStream stream { get; set; }
        private BinaryFormatter formatter { get; set; }

        public PersistentSaveFile(string path)
        {
            stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            formatter = new BinaryFormatter();
        }

        public List<PersistentSaveFileEntry> LoadEntries()
        {
            List<PersistentSaveFileEntry> ret = new List<PersistentSaveFileEntry>();

            object persistentObject = formatter.Deserialize(stream);

            Console.WriteLine(persistentObject.GetType().ToString());

            if (persistentObject != null)
            {
                Type persistentObjectType = persistentObject.GetType();
                PropertyInfo[] properties = persistentObjectType.GetProperties();

                foreach (PropertyInfo property in properties)
                {
                    ret.Add(new PersistentSaveFileEntry()
                    {
                        Name = property.Name,
                        Type = property.PropertyType,
                        Value = property.GetValue(persistentObject)
                    });
                }
            }

            return ret;
        }
    }
}
