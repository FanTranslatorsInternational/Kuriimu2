using Komponent.IO;
using Komponent.IO.Streams;
using Kontract.Models.Archive;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace plugin_capcom.Archives
{
    public class GtCPac
    {
        public IList<IArchiveFileInfo> Load(Stream input)
        {
            using var br = new BinaryReaderX(input, true);
            var _tableOffset = br.ReadMultiple<GtCPacHeader>(7);
            var result = new List<IArchiveFileInfo>();
            var y = 0;
            for (int i = 0; i < _tableOffset.Count; i++)
            {
                var table = _tableOffset[i];

                br.BaseStream.Position = table.tableOffset;
                br.BaseStream.Position += 0x14;
                var dataOffset = br.ReadInt32();
                var _yekbFiles = new List<GtCPacYEKBFile>();
                //System.Diagnostics.Debug.WriteLine("Fuckin chicken shit");
                //System.Diagnostics.Debug.WriteLine(br.BaseStream.Position + " needs to be < " + dataOffset);
                br.BaseStream.Position = table.tableOffset + 0x20;
                var totalFiles = dataOffset / 8;
                for (int z = 0; z < totalFiles; z++)
                {
                    try
                    {
                        //var file = br.ReadType<GtCPacYEKBFile>();
                        var offsetArray = br.ReadBytes(4);
                    if (offsetArray[offsetArray.Length - 1] == 0x80)
                    {
                        offsetArray[offsetArray.Length - 1] = 0x00;
                    }
                    var sizeArray = br.ReadBytes(4);
                    if (sizeArray[sizeArray.Length - 1] == 0x80)
                    {
                        sizeArray[sizeArray.Length - 1] = 0x00;
                    }
                    var size = BitConverter.ToInt32(sizeArray);
                    var offset = BitConverter.ToInt32(offsetArray);

                    //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(offsetArray));
                    //System.Diagnostics.Debug.WriteLine(offset);
                    //System.Diagnostics.Debug.WriteLine(BitConverter.ToString(sizeArray));
                    //System.Diagnostics.Debug.WriteLine(size);

                    //System.Diagnostics.Debug.WriteLine(ObjectDumper.Dump(file));

                    if (size == 0)
                    {
                        break;
                    }

                    //_yekbFiles.Add(file);
                    var fileOffset = offset + dataOffset + table.tableOffset;

                        var subStream = new SubStream(input, fileOffset, size);
                        //System.Diagnostics.Debug.WriteLine("Creating file " + y);
                        var filename = "table" + i + "file" + z + ".bin";
                        result.Add(new ArchiveFileInfo(subStream, filename));
                    } catch
                    {
                        var filename = "table" + i + "file" + z + ".bin";
                        System.Diagnostics.Debug.WriteLine("Unable to create " + filename);
                    }

                    y = y+1;
                }



            }
            // Add files

            //while (br.BaseStream.Position < _yekbHeader.dataOffset)
            //{
            //    var yekbFile = br.ReadType<GtCPacYEKBFile>();
            //    yekbFiles.Add(yekbFile);
            //}
            //var table1StartingOffset = _header.table1Offset + _yekbHeader.dataOffset;
            //var i = 0;
            //foreach (var yekbFile in yekbFiles)
            //{
            //    try
            //    {
            //        var startingFileOffset = table1StartingOffset + yekbFile.offset;
            //        var subStream = new SubStream(input, startingFileOffset, startingFileOffset + yekbFile.offset + yekbFile.size);
            //        result.Add(new ArchiveFileInfo(subStream, "file" + i + ".bin"));
            //    } catch
            //    {

            //    }
            //    i++;
            //}

            return result;
        }
    }


public class ObjectDumper
    {
        private int _level;
        private readonly int _indentSize;
        private readonly StringBuilder _stringBuilder;
        private readonly List<int> _hashListOfFoundElements;

        private ObjectDumper(int indentSize)
        {
            _indentSize = indentSize;
            _stringBuilder = new StringBuilder();
            _hashListOfFoundElements = new List<int>();
        }

        public static string Dump(object element)
        {
            return Dump(element, 2);
        }

        public static string Dump(object element, int indentSize)
        {
            var instance = new ObjectDumper(indentSize);
            return instance.DumpElement(element);
        }

        private string DumpElement(object element)
        {
            if (element == null || element is ValueType || element is string)
            {
                Write(FormatValue(element));
            }
            else
            {
                var objectType = element.GetType();
                if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                {
                    Write("{{{0}}}", objectType.FullName);
                    _hashListOfFoundElements.Add(element.GetHashCode());
                    _level++;
                }

                var enumerableElement = element as IEnumerable;
                if (enumerableElement != null)
                {
                    foreach (object item in enumerableElement)
                    {
                        if (item is IEnumerable && !(item is string))
                        {
                            _level++;
                            DumpElement(item);
                            _level--;
                        }
                        else
                        {
                            if (!AlreadyTouched(item))
                                DumpElement(item);
                            else
                                Write("{{{0}}} <-- bidirectional reference found", item.GetType().FullName);
                        }
                    }
                }
                else
                {
                    MemberInfo[] members = element.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var memberInfo in members)
                    {
                        var fieldInfo = memberInfo as FieldInfo;
                        var propertyInfo = memberInfo as PropertyInfo;

                        if (fieldInfo == null && propertyInfo == null)
                            continue;

                        var type = fieldInfo != null ? fieldInfo.FieldType : propertyInfo.PropertyType;
                        object value = fieldInfo != null
                                           ? fieldInfo.GetValue(element)
                                           : propertyInfo.GetValue(element, null);

                        if (type.IsValueType || type == typeof(string))
                        {
                            Write("{0}: {1}", memberInfo.Name, FormatValue(value));
                        }
                        else
                        {
                            var isEnumerable = typeof(IEnumerable).IsAssignableFrom(type);
                            Write("{0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");

                            var alreadyTouched = !isEnumerable && AlreadyTouched(value);
                            _level++;
                            if (!alreadyTouched)
                                DumpElement(value);
                            else
                                Write("{{{0}}} <-- bidirectional reference found", value.GetType().FullName);
                            _level--;
                        }
                    }
                }

                if (!typeof(IEnumerable).IsAssignableFrom(objectType))
                {
                    _level--;
                }
            }

            return _stringBuilder.ToString();
        }

        private bool AlreadyTouched(object value)
        {
            if (value == null)
                return false;

            var hash = value.GetHashCode();
            for (var i = 0; i < _hashListOfFoundElements.Count; i++)
            {
                if (_hashListOfFoundElements[i] == hash)
                    return true;
            }
            return false;
        }

        private void Write(string value, params object[] args)
        {
            var space = new string(' ', _level * _indentSize);

            if (args != null)
                value = string.Format(value, args);

            _stringBuilder.AppendLine(space + value);
        }

        private string FormatValue(object o)
        {
            if (o == null)
                return ("null");

            if (o is DateTime)
                return (((DateTime)o).ToShortDateString());

            if (o is string)
                return string.Format("\"{0}\"", o);

            if (o is char && (char)o == '\0')
                return string.Empty;

            if (o is ValueType)
                return (o.ToString());

            if (o is IEnumerable)
                return ("...");

            return ("{ }");
        }
    }

}
