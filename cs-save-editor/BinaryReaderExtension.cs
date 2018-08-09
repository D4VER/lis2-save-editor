﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace cs_save_editor
{
    public static class BinaryReaderExtension
    {
        public static string ReadUE4String(this BinaryReader reader)
        {
            int length = reader.ReadInt32();
            if (length == 0) return String.Empty;
            return new string(reader.ReadChars(length)).Remove(length - 1);
        }

        //Function for all property types, if we manage to make a parser
        public static dynamic ParseProperty(this BinaryReader reader, bool inArray = false, string arrElType="", string structElType = "")
        {
            string name = "";
            if (!inArray)
            {
                name = reader.ReadUE4String();
                if (name == "None") return new NoneProperty();
            }

            string type = "";
            if (!inArray)
            {
                type = reader.ReadUE4String();
            }
            else
            {
                type = arrElType;
            }
            switch(type)
            {
                case "BoolProperty":
                    {
                        if (!inArray)
                        {
                            bool value = Convert.ToBoolean(reader.ReadBytes(10)[8]);
                            return new BoolProperty { Name = name, Type = type, Value = value };
                        }

                        return reader.ReadByte(); //bool is represented by 1 byte
                        
                    }
                case "NameProperty":
                    {
                        if (!inArray)
                        {
                            int length = reader.ReadInt32();
                            byte[] unkbytes = reader.ReadBytes(5);
                            string value = reader.ReadUE4String();
                            return new NameProperty { Name = name, Type = type, Length = length, UnkBytes = unkbytes, Value = value };
                        }

                        return reader.ReadUE4String();

                    }
                case "StrProperty":
                    {
                        int length = reader.ReadInt32();
                        byte[] unkbytes = reader.ReadBytes(5);
                        string value = reader.ReadUE4String();
                        return new StrProperty { Name = name, Length = length, Type = type, UnkBytes = unkbytes, Value = value };
                    }
                case "ByteProperty":
                    {
                        if (!inArray)
                        {
                            int length = reader.ReadInt32(); //it is always 1 bytes, but still
                            byte[] unkbytes = reader.ReadBytes(14); //not knowing what to do. this is 4 bytes, followed by None str, followed by 1 byte
                            byte value = reader.ReadByte();
                            return new ByteProperty { Name = name, Length = length, Type = type, UnkBytes = unkbytes, Value = value };
                        }

                        return reader.ReadByte();
                    }
                case "IntProperty":
                    {
                        if (!inArray)
                        {
                            int length = reader.ReadInt32(); //it is always 4 bytes, but still
                            byte[] unkbytes = reader.ReadBytes(5);
                            int value = reader.ReadInt32();
                            return new IntProperty { Name = name, Length = length, UnkBytes = unkbytes, Type = type, Value = value };
                        }

                        return reader.ReadInt32();
                    }
                case "UInt32Property":
                    {
                        if (!inArray)
                        {
                            int length = reader.ReadInt32(); //it is always 4 bytes, but still
                            byte[] unkbytes = reader.ReadBytes(5);
                            uint value = reader.ReadUInt32();
                            return new UInt32Property { Name = name, Length = length, UnkBytes = unkbytes, Type = type, Value = value };
                        }

                        return reader.ReadUInt32();
                    }
                case "FloatProperty":
                    {
                        if (!inArray)
                        {
                            int length = reader.ReadInt32(); //it is always 4 bytes, but still
                            byte [] unkbytes = reader.ReadBytes(5);
                            float value = reader.ReadSingle();
                            return new FloatProperty { Name = name, Length = length, UnkBytes = unkbytes, Type = type, Value = value };
                        }

                        return reader.ReadSingle();
                        
                    }
                case "EnumProperty":
                    {
                        if (!inArray)
                        {
                            long length = reader.ReadInt64();
                            string eltype = reader.ReadUE4String();
                            byte unkbyte = reader.ReadByte();
                            string value = reader.ReadUE4String();
                            return new EnumProperty
                            {
                                Length = length,
                                Type = type,
                                ElementType = eltype,
                                Name = name,
                                UnkByte = unkbyte,
                                Value = value
                            };
                        }

                        return reader.ReadUE4String(); //just the value

                    }
                case "StructProperty":
                    {
                        if (!inArray)
                        {
                            long length = reader.ReadInt64();
                            string eltype = reader.ReadUE4String();
                            byte[] unkbytes = reader.ReadBytes(17);
                            long end = reader.BaseStream.Position + length;
                            Dictionary<string, dynamic> value = new Dictionary<string, dynamic>();
                            switch (eltype)
                            {
                                case "Quat":
                                    {
                                        value.Add("Quat", new Quaternion ( reader.ReadSingle(),
                                                                        reader.ReadSingle(),
                                                                        reader.ReadSingle(),
                                                                        reader.ReadSingle() ));
                                        break;
                                    }
                                case "Vector":
                                    {
                                        value.Add("Vector", new Vector3(reader.ReadSingle(), 
                                                                      reader.ReadSingle(), 
                                                                      reader.ReadSingle()));
                                        break;
                                    }
                                case "Guid":
                                    {
                                        value.Add("Guid", new Guid(reader.ReadBytes(16)));
                                        break;
                                    }
                                case "DateTime":
                                    {
                                        value.Add("DateTime", DateTime.FromFileTime(reader.ReadInt64()).AddYears(-1600));
                                        reader.ReadUE4String(); //"None". don't know if all datetime props are delimited by this or it's just the end of the file at play
                                        break;
                                    }
                                default:
                                    {
                                        while (reader.BaseStream.Position < end)
                                        {
                                            var test = reader.BaseStream.Position;
                                            dynamic child = reader.ParseProperty();
                                            if (child.Type != "None")
                                            {
                                                value.Add(child.Name, child);
                                            }
                                        }
                                        break;
                                    }
                            }
           
                            return new StructProperty { Name = name, Length = length, UnkBytes = unkbytes, Type = type, ElementType = eltype, Value = value };
                        }

                        Dictionary<string, dynamic> s_value = new Dictionary<string, dynamic>();

                        switch (structElType)
                        {
                            case "Quat":
                                {
                                    s_value.Add("Quat", new Quaternion(reader.ReadSingle(),
                                                                    reader.ReadSingle(),
                                                                    reader.ReadSingle(),
                                                                    reader.ReadSingle()));
                                    break;
                                }
                            case "Vector":
                                {
                                    s_value.Add("Vector", new Vector3(reader.ReadSingle(),
                                                                  reader.ReadSingle(),
                                                                  reader.ReadSingle()));
                                    break;
                                }
                            case "Guid":
                                {
                                    s_value.Add("Guid", new Guid(reader.ReadBytes(16)));
                                    break;
                                }
                            case "DateTime":
                                {
                                    s_value.Add("DateTime", DateTime.FromFileTime(reader.ReadInt64()).AddYears(-1600));
                                    //do we need the None here?
                                    break;
                                }
                            default:
                                {
                                    while (true)
                                    {
                                        var test = reader.BaseStream.Position;
                                        dynamic child = reader.ParseProperty();
                                        if (child.Type == "None") break; //"None" i.e reached end of struct 

                                        s_value.Add(child.Name, child);
                                    }
                                    break;
                                }
                        }

                        return s_value;
                    }

                case "ArrayProperty":
                    {
                        long length = reader.ReadInt64();
                        string eltype = reader.ReadUE4String();
                        byte unkbyte = reader.ReadByte();
                        int count = reader.ReadInt32();

                        List<dynamic> value = new List<dynamic> ();

                        if (eltype == "StructProperty")
                        {
                            Dictionary<string, object> struct_info = new Dictionary<string, object>()
                            {
                                { "struct_name", reader.ReadUE4String() },
                                {"struct_type", reader.ReadUE4String() },
                                {"struct_length", reader.ReadInt64() },
                                {"struct_eltype", reader.ReadUE4String() },
                                {"struct_unkbytes", reader.ReadBytes(17) }
                            };

                            value.Add(struct_info);

                            for (int i = 0; i < count; i++)
                            {
                                value.Add(reader.ParseProperty(true, eltype, struct_info["struct_eltype"].ToString()));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < count; i++)
                            {
                                value.Add(reader.ParseProperty(true, eltype));
                            }
                        }
                        
                        return new ArrayProperty { Name = name, Length = length, Type = type, ElementType = eltype, ElementCount = count, UnkByte = unkbyte, Value = value};
                    }
                case "MapProperty":
                    {
                        long length = reader.ReadInt64();
                        string keytype = reader.ReadUE4String();
                        string valtype = reader.ReadUE4String();
                        byte[] unkbytes = reader.ReadBytes(5);
                        int count = reader.ReadInt32();
                        Dictionary<object, object> value = new Dictionary<object, object>();

                        if (keytype == "StructProperty" && valtype == "StructProperty")
                        {
                            for (int i = 0; i < count; i++) //FactAssets. add guid and struct containing entire fact asset
                            {
                                value.Add(new Guid(reader.ReadBytes(16)).ToString(), reader.ParseProperty(true, valtype));
                            }
                        }
                        else
                        {
                            for (int i = 0; i < count; i++)
                            {
                                value.Add(reader.ParseProperty(true, keytype), reader.ParseProperty(true, valtype));
                            }
                        }
                        return new MapProperty { Name = name, Type = type, KeyType = keytype, Length = length, ValType = valtype, UnkBytes = unkbytes, ElementCount = count, Value = value };
                    }
                    //unimplemented yet due to lack of data
                case "TextProperty":
                    {
                        long length = reader.ReadInt64();
                        byte[] value = reader.ReadBytes(6); //for now. there are only 2 text properties anyway
                        return new TextProperty { Name = name, Length = length, Type = type, Value = value };
                    }
                case "SetProperty": //only ONE such property in the file, and it's still empty :D 
                    {
                        long length = reader.ReadInt64(); //maybe
                        string eltype = reader.ReadUE4String(); //likely
                        byte[] unkbytes = reader.ReadBytes(9);
                        return new SetProperty { Name = name, Length = length, Type = type, ElementType = eltype, UnkBytes = unkbytes };
                    }
                default:
                    {
                        throw new InvalidDataException("Unknown type: " + type);
                    }
            }
        }

        /*

        public static Dictionary<string, InventoryItem> ParseInventorySection(this BinaryReader reader)
        {
            long section_length = reader.ParseStructProperty().Length;
            long section_end_addr = reader.BaseStream.Position + section_length;
            NameProperty name;
            IntProperty qty;

            Dictionary<string, InventoryItem> items = new Dictionary<string, InventoryItem>();

            while (reader.BaseStream.Position < section_end_addr)
            {
                name = reader.ParseNameProperty();
                qty = reader.ParseIntProperty();
                reader.ReadUE4String(); //"None"
                items[name.Value] = new InventoryItem { Name = name, Quantity = qty };
            }

            return items;
        }

        public static List<SeenNotification> ParseAlreadySeenNotifs(this BinaryReader reader)
        {
            int count = reader.ReadInt32();

            List<SeenNotification> notifs = new List<SeenNotification>();

            for (int i = 0; i < count; i++)
            {
                notifs.Add(new SeenNotification
                {
                    Name = new NameProperty { Name = "Notification", Address = reader.BaseStream.Position, Value = reader.ReadUE4String() },
                    Times = new IntProperty { Name = "SeenNotifTimes", Address = reader.BaseStream.Position, Value = reader.ReadInt32() }
                });
            }

            return notifs;
        }

        public static FactAsset ParseFactAsset(this BinaryReader reader)
        {
            reader.BaseStream.Seek(101, SeekOrigin.Current); //The GUID of the fact asset and some misc bytes

            Dictionary<string, BoolFact> BoolFacts = new Dictionary<string, BoolFact>();
            Dictionary<string, IntFact> IntFacts = new Dictionary<string, IntFact>();
            Dictionary<string, FloatFact> FloatFacts = new Dictionary<string, FloatFact>();
            Dictionary<string, EnumFact> EnumFacts = new Dictionary<string, EnumFact>();

            reader.ParseArrayProperty();
            //bool facts section
            long section_len = reader.ParseStructProperty().Length;
            long section_end = reader.BaseStream.Position + section_len;

            while (reader.BaseStream.Position < section_end)
            {
                BoolProperty value = reader.ParseBoolProperty();
                NameProperty name = reader.ParseNameProperty();
                BoolFacts.Add(name.Value, new BoolFact { Name = name, Value = value });
                reader.ParseStructProperty(); //The GUID's "container"
                reader.ReadBytes(16); //GUID itself
                reader.ReadUE4String(); //"None"
            }

            reader.ParseArrayProperty();
            //int facts section
            section_len = reader.ParseStructProperty().Length;
            section_end = reader.BaseStream.Position + section_len;

            while (reader.BaseStream.Position < section_end)
            {
                IntProperty value = reader.ParseIntProperty();
                NameProperty name = reader.ParseNameProperty();
                IntFacts.Add(name.Value, new IntFact { Name = name, Value = value });
                reader.ParseStructProperty(); //The GUID's "container"
                reader.ReadBytes(16); //GUID itself
                reader.ReadUE4String(); //"None"
            }

            reader.ParseArrayProperty();
            //float facts section
            section_len = reader.ParseStructProperty().Length;
            section_end = reader.BaseStream.Position + section_len;

            while (reader.BaseStream.Position < section_end)
            {
                FloatProperty value = reader.ParseFloatProperty();
                NameProperty name = reader.ParseNameProperty();
                FloatFacts.Add(name.Value, new FloatFact { Name = name, Value = value });
                reader.ParseStructProperty(); //The GUID's "container"
                reader.ReadBytes(16); //GUID itself
                reader.ReadUE4String(); //"None"
            }

            reader.ParseArrayProperty();
            //enum facts section
            section_len = reader.ParseStructProperty().Length;
            section_end = reader.BaseStream.Position + section_len;

            while (reader.BaseStream.Position < section_end)
            {
                ////EnumProperty value = reader.ParseEnumProperty();
                //NameProperty name = reader.ParseNameProperty();
                //FloatFacts.Add(name.Value, new FloatFact { Name = name, Value = value });
                //reader.ParseStructProperty(); //The GUID's "container"
                //reader.ReadBytes(16); //GUID itself
                //reader.ReadUE4String(); //"None"
                //throw new InvalidDataException("Found enum data! at "+reader.BaseStream.Position.ToString());
                reader.ReadBytes(Convert.ToInt32(section_len));
            }

            //parse KeepFact
            BoolFact kfv = reader.ParseOuterBoolFact();
            BoolFacts.Add(kfv.Name.Value, kfv);
            reader.ReadUE4String(); //"None"

            return new FactAsset(BoolFacts, IntFacts, FloatFacts, EnumFacts);
        }

        public static List<StreamingLevelPackage> ParseLevelPackages (this BinaryReader reader)
        {
            int count = reader.ParseArrayProperty().ElementCount; //"WorldStreamingSaveData"
            reader.ParseStructProperty(); //"WorldStreamingSaveData"

            List<StreamingLevelPackage> packs = new List<StreamingLevelPackage>();

            for (int i = 0; i < count; i++)
            {
                packs.Add(new StreamingLevelPackage
                {
                    Name = reader.ParseNameProperty(),
                    ShouldBeLoaded = reader.ParseBoolProperty(),
                    ShouldBeVisible = reader.ParseBoolProperty(),
                    ShouldBlockOnLoad = reader.ParseBoolProperty(),
                    HasLoadedLevel = reader.ParseBoolProperty(),
                    IsVisible = reader.ParseBoolProperty()
                });
                reader.ReadUE4String(); //"None"
            }

            return packs; 
        }

        */
    }
}