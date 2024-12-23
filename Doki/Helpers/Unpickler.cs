using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Doki.Helpers
{
    //CREDIT TO KIZBY SHE MADE THIS IN HER TESTDDLCMOD - IT JUST WORKS SO WELL IM PORTING IT OVER
    public class Unpickler
    {
        private byte[] pickled;
        private int offset;
        private bool stop;
        private Stack<PythonObj> stack;
        private PythonObj mark = new PythonObj();
        private Dictionary<string, PythonObj> memo = new Dictionary<string, PythonObj>();

        public bool ok { get; private set; }

        private static Dictionary<string, HashSet<string>> seenFields = new Dictionary<string, HashSet<string>>();
        private static HashSet<string> seenClass(string cls)
        {
            if (!seenFields.ContainsKey(cls))
            {
                seenFields.Add(cls, new HashSet<string>());
            }
            return seenFields[cls];
        }

        private static Dictionary<char, Action<Unpickler>> dispatch = new Dictionary<char, Action<Unpickler>>()
        {
            { '(', unpickler => // MARK
                {
                    unpickler.stack.Push(unpickler.mark);
                }
            },
            { '.', unpickler => // STOP
                {
                    unpickler.stop = true;
                }
            },
            { '0', unpickler => // POP
                {
                    unpickler.stack.Pop();
                }
            },
            { '1', unpickler => // POP_MARK
                {
                    while(unpickler.stack.Pop() != unpickler.mark) ;
                }
            },
            { '2', unpickler => // DUP
                {
                    unpickler.stack.Push(unpickler.stack.Peek());
                }
            },
            { 'F', unpickler => // FLOAT
                {
                    unpickler.stack.Push(new PythonObj(float.Parse(unpickler.ReadLine())));
                }
            },
            { 'I', unpickler => // INT
                {
                    var val = unpickler.ReadLine();
                    if (val == "00")
                    {
                        unpickler.stack.Push(new PythonObj(false));
                    } else if (val == "01")
                    {
                        unpickler.stack.Push(new PythonObj(true));
                    } else
                    {
                        var num = BigInteger.Parse(val);
                        if (num <= int.MaxValue)
                        {
                            unpickler.stack.Push(new PythonObj((int)num));
                        } else
                        {
                            unpickler.stack.Push(new PythonObj(num));
                        }
                    }
                }
            },
            { 'J', unpickler => // BININT
                {
                    unpickler.stack.Push(new PythonObj((int)unpickler.ParseInt(4)));
                }
            },
            { 'K', unpickler => // BININT1
                {
                    unpickler.stack.Push(new PythonObj((int)unpickler.ParseInt(1)));
                }
            },
            { 'L', unpickler => // LONG
                {
                    var val = unpickler.ReadLine();

                    BigInteger num = BigInteger.Zero;
                    if (val != "")
                    {
                        num = BigInteger.Parse(val);
                    }
                    unpickler.stack.Push(new PythonObj(num));
                }
            },
            { 'M', unpickler => // BININT2
                {
                    unpickler.stack.Push(new PythonObj((int)unpickler.ParseInt(2)));
                }
            },
            { 'N', unpickler => // NONE
                {
                    unpickler.stack.Push(new PythonObj());
                }
            },
            { 'P', unpickler => // PERSID
                {
                    Console.WriteLine("Unhandled unpickling case: " + "PERSID");
                    unpickler.ok = false;
                }
            },
            { 'Q', unpickler => // BINPERSID
                {
                    Console.WriteLine("Unhandled unpickling case: " + "BINPERSID");
                    unpickler.ok = false;
                }
            },
            { 'R', unpickler => // REDUCE
                {
                    Console.WriteLine("Unhandled unpickling case: " + "REDUCE");
                    unpickler.ok = false;
                }
            },
            { 'S', unpickler => // STRING
                {
                    var val = unpickler.ReadLine();
                    Console.WriteLine("Encountered raw string: " + val);
                    var unescaped = Regex.Unescape(val.Substring(1, val.Length - 2));
                    Console.WriteLine("Unescaped as: " + unescaped);
                    unpickler.stack.Push(new PythonObj(unescaped));
                }
            },
            { 'T', unpickler => // BINSTRING
                {
                    var len = unpickler.ParseInt(4);
                    var bytes = new byte[len];
                    Array.Copy(unpickler.pickled, unpickler.offset, bytes, 0, bytes.Length);
                    unpickler.offset += bytes.Length;
                    unpickler.stack.Push(new PythonObj(Encoding.Default.GetString(bytes)));
                }
            },
            { 'U', unpickler => // SHORT_BINSTRING
                {
                    var len = unpickler.ParseInt(1);
                    var bytes = new byte[len];
                    Array.Copy(unpickler.pickled, unpickler.offset, bytes, 0, bytes.Length);
                    unpickler.offset += bytes.Length;
                    unpickler.stack.Push(new PythonObj(Encoding.Default.GetString(bytes)));
                }
            },
            { 'V', unpickler => // UNICODE
                {
                    var val = unpickler.ReadLine();
                    Console.WriteLine("Encountered raw unicode string: " + val);
                    var unescaped = Regex.Unescape(val);
                    Console.WriteLine("Unescaped as: " + unescaped);
                    unpickler.stack.Push(new PythonObj(unescaped));
                }
            },
            { 'X', unpickler => // BINUNICODE
                {
                    var len = unpickler.ParseInt(4);
                    var bytes = new byte[len];
                    Array.Copy(unpickler.pickled, unpickler.offset, bytes, 0, bytes.Length);
                    unpickler.offset += bytes.Length;
                    unpickler.stack.Push(new PythonObj(Encoding.UTF8.GetString(bytes)));
                }
            },
            { 'a', unpickler => // APPEND
                {
                    var val = unpickler.stack.Pop();
                    unpickler.stack.Peek().List.Add(val);
                }
            },
            { 'b', unpickler => // BUILD
                {
                    var rawState = unpickler.stack.Pop();
                    var inst = unpickler.stack.Peek();
                    if (inst.Type != PythonObj.ObjType.NEWOBJ)
                    {
                        Console.WriteLine("Trying to build off a " + inst.Type + " instead of a NEWOBJ?");
                        unpickler.ok = false;
                        return;
                    }
                    Action<PythonObj, PythonObj> setField = (key, value) =>
                    {
                        seenClass(inst.Name).Add(key.String);
                        inst.Fields[key.String] = value;
                    };
                    Action<PythonObj> buildFromState = null;
                    Dictionary<string, Action<PythonObj>> stateBuilders = new Dictionary<string, Action<PythonObj>>()
                    {
                        {"renpy.ast.PyCode", state =>
                            {
                                setField(new PythonObj("source"), state.Tuple[1]);
                                setField(new PythonObj("location"), state.Tuple[2]);
                                setField(new PythonObj("mode"), state.Tuple[3]);
                                setField(new PythonObj("bytecode"), new PythonObj());
                            }
                        },
                    };
                    if (stateBuilders.ContainsKey(inst.Name))
                    {
                        buildFromState = stateBuilders[inst.Name];
                    }
                    else
                    {
                        buildFromState = state =>
                        {
                            switch (state.Type)
                            {
                                case PythonObj.ObjType.NONE: break;
                                case PythonObj.ObjType.TUPLE:
                                    if (state.Tuple.Count != 2)
                                    {
                                        Console.WriteLine("Weird tuple in unpickler (count = " + state.Tuple.Count + ")");
                                        unpickler.ok = false;
                                        return;
                                    }
                                    buildFromState(state.Tuple[0]);
                                    buildFromState(state.Tuple[1]);
                                    break;
                                case PythonObj.ObjType.DICTIONARY:
                                    foreach(var entry in state.Dictionary)
                                    {
                                        setField(entry.Key, entry.Value);
                                    }
                                    break;
                                default:
                                    Console.WriteLine("State is a " + state.Type + "; wtf?");
                                    unpickler.ok = false;
                                    break;
                            }
                        };
                    }
                    buildFromState(rawState);
                }
            },
            { 'c', unpickler => // GLOBAL
                {
                    var module = unpickler.ReadLine();
                    var name = unpickler.ReadLine();
                    var qualifiedName = module + "." + name;
                    unpickler.stack.Push(new PythonObj(qualifiedName, true));
                }
            },
            { 'd', unpickler => // DICT
                {
                    var dict = new Dictionary<PythonObj, PythonObj>();
                    while (unpickler.stack.Peek() != unpickler.mark)
                    {
                        var val = unpickler.stack.Pop();
                        var key = unpickler.stack.Pop();
                        dict[key] = val;
                    }
                    unpickler.stack.Pop(); // pop the mark
                    unpickler.stack.Push(new PythonObj(dict));
                }
            },
            { '}', unpickler => // EMPTY_DICT
                {
                    unpickler.stack.Push(new PythonObj(new Dictionary<PythonObj, PythonObj>()));
                }
            },
            { 'e', unpickler => // APPENDS
                {
                    var toAppend = new List<PythonObj>();
                    while (unpickler.stack.Peek() != unpickler.mark)
                    {
                        toAppend.Add(unpickler.stack.Pop());
                    }
                    toAppend.Reverse(); // took them off the stack in reverse order, so fix it
                    unpickler.stack.Pop(); // pop the mark
                    unpickler.stack.Peek().List.AddRange(toAppend);
                }
            },
            { 'g', unpickler => // GET
                {
                    var line = unpickler.ReadLine();
                    unpickler.stack.Push(unpickler.memo[line]);
                }
            },
            { 'h', unpickler => // BINGET
                {
                    var index = unpickler.ParseInt(1);
                    unpickler.stack.Push(unpickler.memo[index.ToString()]);
                }
            },
            { 'i', unpickler => // INST
                {
                    Console.WriteLine("Unhandled unpickling case: " + "INST");
                    unpickler.ok = false;
                }
            },
            { 'j', unpickler => // LONG_BINGET
                {
                    var index = unpickler.ParseInt(4);
                    unpickler.stack.Push(unpickler.memo[index.ToString()]);
                }
            },
            { 'l', unpickler => // LIST
                {
                    var list = new List<PythonObj>();
                    while (unpickler.stack.Peek() != unpickler.mark)
                    {
                        list.Add(unpickler.stack.Pop());
                    }
                    list.Reverse(); // took them off the stack in reverse order, so fix it
                    unpickler.stack.Pop(); // pop the mark
                    unpickler.stack.Push(new PythonObj(list));
                }
            },
            { ']', unpickler => // EMPTY_LIST
                {
                    unpickler.stack.Push(new PythonObj(new List<PythonObj>()));
                }
            },
            { 'o', unpickler => // OBJ
                {
                    Console.WriteLine("Unhandled unpickling case: " + "OBJ");
                    unpickler.ok = false;
                }
            },
            { 'p', unpickler => // PUT
                {
                    unpickler.memo[unpickler.ReadLine()] = unpickler.stack.Peek();
                }
            },
            { 'q', unpickler => // BINPUT
                {
                    unpickler.memo[unpickler.ParseInt(1).ToString()] = unpickler.stack.Peek();
                }
            },
            { 'r', unpickler => // LONG_BINPUT
                {
                    unpickler.memo[unpickler.ParseInt(4).ToString()] = unpickler.stack.Peek();
                }
            },
            { 's', unpickler => // SETITEM
                {
                    var val = unpickler.stack.Pop();
                    var key = unpickler.stack.Pop();
                    unpickler.stack.Peek().Dictionary[key] = val;
                }
            },
            { 't', unpickler => // TUPLE
                {
                    var list = new List<PythonObj>();
                    while (unpickler.stack.Peek() != unpickler.mark)
                    {
                        list.Add(unpickler.stack.Pop());
                    }
                    list.Reverse(); // took them off the stack in reverse order, so fix it
                    unpickler.stack.Pop(); // pop the mark
                    unpickler.stack.Push(new PythonObj(list, true));
                }
            },
            { ')', unpickler => // EMPTY_TUPLE
                {
                    unpickler.stack.Push(new PythonObj(new List<PythonObj>(), true));
                }
            },
            { 'u', unpickler => // SETITEMS
                {
                    var toInsert = new Dictionary<PythonObj, PythonObj>();
                    while (unpickler.stack.Peek() != unpickler.mark)
                    {
                        var val = unpickler.stack.Pop();
                        var key = unpickler.stack.Pop();
                        toInsert[key] = val;
                    }
                    unpickler.stack.Pop(); // pop the mark
                    var dict = unpickler.stack.Peek().Dictionary;
                    foreach (var entry in toInsert) {
                        dict[entry.Key] = entry.Value;
                    }
                }
            },
            { 'G', unpickler => // BINFLOAT
                {
                    unpickler.stack.Push(new PythonObj(BitConverter.ToDouble(unpickler.pickled, unpickler.offset)));
                    unpickler.offset += 8;
                }
            },
            { '\x80', unpickler => // PROTO
                {
                    var val = unpickler.pickled[unpickler.offset++];
                    if (val != 2)
                    {
                        Console.WriteLine("Apparently need to support pickle protocol " + val);
                        unpickler.ok = false;
                    }
                }
            },
            { '\x81', unpickler => // NEWOBJ
                {
                    var args = unpickler.stack.Pop();
                    var cls = unpickler.stack.Pop();
                    if (cls.Type != PythonObj.ObjType.CLASS)
                    {
                        Console.WriteLine("Trying to instantiate a " + cls.Type + " instead of a CLASS");
                        unpickler.ok = false;
                        return;
                    }
                    unpickler.stack.Push(new PythonObj(cls.Name, args));
                }
            },
            { '\x82', unpickler => // EXT1
                {
                    Console.WriteLine("Unhandled unpickling case: " + "EXT1");
                    unpickler.ok = false;
                }
            },
            { '\x83', unpickler => // EXT2
                {
                    Console.WriteLine("Unhandled unpickling case: " + "EXT2");
                    unpickler.ok = false;
                }
            },
            { '\x84', unpickler => // EXT4
                {
                    Console.WriteLine("Unhandled unpickling case: " + "EXT4");
                    unpickler.ok = false;
                }
            },
            { '\x85', unpickler => // TUPLE1
                {
                    var list = new List<PythonObj>();
                    list.Add(unpickler.stack.Pop());
                    unpickler.stack.Push(new PythonObj(list, true));
                }
            },
            { '\x86', unpickler => // TUPLE2
                {
                    var list = new List<PythonObj>();
                    list.Add(unpickler.stack.Pop());
                    list.Add(unpickler.stack.Pop());
                    list.Reverse();
                    unpickler.stack.Push(new PythonObj(list, true));
                }
            },
            { '\x87', unpickler => // TUPLE3
                {
                    var list = new List<PythonObj>();
                    list.Add(unpickler.stack.Pop());
                    list.Add(unpickler.stack.Pop());
                    list.Add(unpickler.stack.Pop());
                    list.Reverse();
                    unpickler.stack.Push(new PythonObj(list, true));
                }
            },
            { '\x88', unpickler => // NEWTRUE
                {
                    unpickler.stack.Push(new PythonObj(true));
                }
            },
            { '\x89', unpickler => // NEWFALSE
                {
                    unpickler.stack.Push(new PythonObj(false));
                }
            },
            { '\x8a', unpickler => // LONG1
                {
                    var length = unpickler.ParseInt(1);
                    unpickler.stack.Push(new PythonObj(new BigInteger(unpickler.GetBytes((int)length))));
                }
            },
            { '\x8b', unpickler => // LONG4
                {
                    var length = unpickler.ParseInt(4);
                    unpickler.stack.Push(new PythonObj(new BigInteger(unpickler.GetBytes((int)length))));
                }
            },
        };

        public static PythonObj Unpickle(byte[] pickled)
        {
            var state = new Unpickler(pickled);
            if (!state.ok || state.stack.Count == 0)
            {
                Console.WriteLine("Failed to unpickle");
                return null;
            }
            if (state.offset != pickled.Length)
            {
                Console.WriteLine(pickled.Length - state.offset + " extra bytes at the end of the pickled data?");
            }
            return state.stack.Pop();
        }
        public static PythonObj UnpickleZlibBytes(byte[] compressed, int offset = 0, int length = -1)
        {
            // skip first two bytes for the zlib header
            var start = offset + 2;
            if (length == -1)
            {
                length = compressed.Length - start;
            }
            else
            {
                length -= 2;
            }

            // *could* inflate directly into a buffer that we scale up as needed, but it's small enough it's
            // fine to just inflate it again once we know the length
            int decompressedLength = 0;
            using (var memStream = new MemoryStream(compressed, start, length))
            {
                var zipBytes = new byte[1000];
                using (var zipStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    int read;
                    do
                    {
                        read = zipStream.Read(zipBytes, 0, zipBytes.Length);
                        if (read > 0)
                        {
                            decompressedLength += read;
                        }
                    } while (read > 0);
                }
            }

            byte[] pickled = new byte[decompressedLength];
            using (var memStream = new MemoryStream(compressed, start, length))
            {
                using (var zipStream = new DeflateStream(memStream, CompressionMode.Decompress))
                {
                    zipStream.Read(pickled, 0, pickled.Length);
                }
            }

            return Unpickle(pickled);
        }

        public static void DumpSeenFields()
        {
            foreach (var entry in seenFields)
            {
                Console.WriteLine(entry.Key + ": " + string.Join(", ", entry.Value));
            }
        }

        private Unpickler(byte[] pickled)
        {
            this.pickled = pickled;
            offset = 0;
            ok = true;
            stop = false;
            stack = new Stack<PythonObj>();

            while (ok && !stop)
            {
                dispatch[(char)pickled[offset++]](this);
            }
        }

        private string ReadLine()
        {
            string line = "";
            while ((char)pickled[offset++] != '\n')
            {
                line += (char)pickled[offset - 1];
            }
            return line;
        }

        private long ParseInt(int bytes)
        {
            long result;
            if (bytes == 8)
            {
                result = BitConverter.ToInt64(pickled, offset);
            }
            else if (bytes == 4)
            {
                result = BitConverter.ToInt32(pickled, offset);
            }
            else if (bytes == 2)
            {
                result = BitConverter.ToInt16(pickled, offset);
            }
            else if (bytes == 1)
            {
                result = pickled[offset];
            }
            else
            {
                Console.WriteLine("Unhandled byte length in ParseInt!");
                ok = false;
                return 0;
            }
            offset += bytes;
            return result;
        }

        private byte[] GetBytes(int count)
        {
            byte[] bytes = new byte[count];
            Array.Copy(pickled, offset, bytes, 0, count);
            offset += count;
            return bytes;
        }
    }
    public class PythonObj
    {
        public ObjType Type { get; private set; }
        private bool _Bool;
        public bool Bool
        {
            get => Type == ObjType.BOOL ? _Bool : throw new MemberAccessException("Asked for Bool of a " + Type);
            private set => _Bool = value;
        }
        private double _Float;
        public double Float
        {
            get => Type == ObjType.FLOAT ? _Float : throw new MemberAccessException("Asked for Float of a " + Type);
            private set => _Float = value;
        }
        private int _Int;
        public int Int
        {
            get => Type == ObjType.INT ? _Int : throw new MemberAccessException("Asked for Int of a " + Type);
            private set => _Int = value;
        }
        private BigInteger _Long;
        public BigInteger Long
        {
            get => Type == ObjType.LONG ? _Long : throw new MemberAccessException("Asked for Long of a " + Type);
            private set => _Long = value;
        }
        private string _String;
        public string String
        {
            get => Type == ObjType.STRING ? _String : throw new MemberAccessException("Asked for String of a " + Type);
            private set => _String = value;
        }
        private List<PythonObj> _List;
        public List<PythonObj> List
        {
            get => Type == ObjType.LIST ? _List : throw new MemberAccessException("Asked for List of a " + Type);
            private set => _List = value;
        }
        private Dictionary<PythonObj, PythonObj> _Dictionary;
        public Dictionary<PythonObj, PythonObj> Dictionary
        {
            get => Type == ObjType.DICTIONARY ? _Dictionary : throw new MemberAccessException("Asked for Dictionary of a " + Type);
            private set => _Dictionary = value;
        }
        private PythonObj _Args;
        public PythonObj Args
        {
            get => Type == ObjType.NEWOBJ ? _Args : throw new MemberAccessException("Asked for Args of a " + Type);
            private set => _Args = value;
        }
        private Dictionary<string, PythonObj> _Fields;
        public Dictionary<string, PythonObj> Fields
        {
            get => Type == ObjType.NEWOBJ ? _Fields : throw new MemberAccessException("Asked for Fields of a " + Type);
            private set => _Fields = value;
        }
        public List<PythonObj> Tuple => Type == ObjType.TUPLE ? _List : throw new MemberAccessException("Asked for Tuple of a " + Type);
        public string Name => Type == ObjType.NEWOBJ || Type == ObjType.CLASS ? _String : Type.ToString();

        public PythonObj()
        {
            Type = ObjType.NONE;
        }
        public PythonObj(bool val)
        {
            Type = ObjType.BOOL;
            Bool = val;
        }
        public PythonObj(double val)
        {
            Type = ObjType.FLOAT;
            Float = val;
        }
        public PythonObj(int val)
        {
            Type = ObjType.INT;
            Int = val;
        }
        public PythonObj(BigInteger val)
        {
            Type = ObjType.LONG;
            Long = val;
        }
        public PythonObj(string val, bool isClass = false)
        {
            Type = isClass ? ObjType.CLASS : ObjType.STRING;
            String = val;
        }
        // tuples are immutable, so are worth distinguishing
        public PythonObj(List<PythonObj> val, bool tuple = false)
        {
            Type = tuple ? ObjType.TUPLE : ObjType.LIST;
            List = val;
        }
        public PythonObj(Dictionary<PythonObj, PythonObj> val)
        {
            Type = ObjType.DICTIONARY;
            Dictionary = val;
        }
        public PythonObj(string name, PythonObj args)
        {
            Type = ObjType.NEWOBJ;
            String = name;
            Args = args;
            // lets us set attributes later
            Fields = new Dictionary<string, PythonObj>();
        }

        public override string ToString()
        {
            return ToString("");
        }
        private string ToString(string indent)
        {
            var nextIndent = indent + " ";
            switch (Type)
            {
                case ObjType.NONE: return "<none>";
                case ObjType.BOOL: return Bool.ToString();
                case ObjType.FLOAT: return Float.ToString();
                case ObjType.INT: return Int.ToString();
                case ObjType.LONG: return Long.ToString();
                case ObjType.STRING: return "'" + String.Replace("\n", "\n" + nextIndent) + "'";
                case ObjType.LIST:
                    {
                        var result = "[\n";
                        foreach (var item in List)
                        {
                            result += nextIndent + item.ToString(nextIndent) + "\n";
                        }
                        return result + indent + "]";
                    }
                case ObjType.DICTIONARY:
                    {
                        var result = "{\n";
                        foreach (var item in Dictionary)
                        {
                            result += nextIndent + item.Key.ToString(nextIndent) + ": ";
                            result += item.Value.ToString(nextIndent) + "\n";
                        }
                        return result + indent + "}";
                    }
                case ObjType.TUPLE:
                    {
                        var result = "(\n";
                        foreach (var item in Tuple)
                        {
                            result += nextIndent + item.ToString(nextIndent) + "\n";
                        }
                        return result + indent + ")";
                    }
                case ObjType.CLASS: return "class " + Name;
                case ObjType.NEWOBJ:
                    {
                        var result = "obj " + Name + Args.ToString(nextIndent) + "{\n";
                        foreach (var item in Fields)
                        {
                            result += nextIndent + item.Key.Replace("\n", "\n" + nextIndent) + ": ";
                            result += item.Value.ToString(nextIndent) + "\n";
                        }
                        return result + indent + "}";
                    }
            }
            Console.WriteLine("Invalid type in PythonObj.ToString()");
            return "";
        }

        public int ToInt()
        {
            if (Type == ObjType.INT)
            {
                return Int;
            }
            else if (Type == ObjType.LONG)
            {
                return (int)(uint)Long;
            }
            Console.WriteLine("Asking for Long value of " + Type);
            return -1;
        }

        public override bool Equals(object obj)
        {
            if (obj is PythonObj pobj)
            {
                if (Type != pobj.Type)
                {
                    return false;
                }
                switch (Type)
                {
                    case ObjType.BOOL: return Bool == pobj.Bool;
                    case ObjType.FLOAT: return Float == pobj.Float;
                    case ObjType.INT: return Int == pobj.Int;
                    case ObjType.LONG: return Long == pobj.Long;
                    case ObjType.STRING: return String == pobj.String;
                    case ObjType.LIST: return List == pobj.List;
                    case ObjType.DICTIONARY: return Dictionary == pobj.Dictionary;
                    case ObjType.TUPLE:
                        // tuples are immutable, so compare their contents
                        if (List.Count != pobj.List.Count)
                        {
                            return false;
                        }
                        for (var i = 0; i < List.Count; ++i)
                        {
                            if (!List[i].Equals(pobj.List[i]))
                            {
                                return false;
                            }
                        }
                        return true;
                    case ObjType.NONE: return base.Equals(obj);
                    case ObjType.CLASS: return Name.Equals(pobj.Name);
                    case ObjType.NEWOBJ: return Name.Equals(pobj.Name) && Fields.Equals(pobj.Fields);
                }
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hashCode = -1617958758;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + _Bool.GetHashCode();
            hashCode = hashCode * -1521134295 + _Float.GetHashCode();
            hashCode = hashCode * -1521134295 + _Int.GetHashCode();
            hashCode = hashCode * -1521134295 + _Long.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_String);
            hashCode = hashCode * -1521134295 + EqualityComparer<List<PythonObj>>.Default.GetHashCode(_List);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<PythonObj, PythonObj>>.Default.GetHashCode(_Dictionary);
            hashCode = hashCode * -1521134295 + EqualityComparer<PythonObj>.Default.GetHashCode(_Args);
            hashCode = hashCode * -1521134295 + EqualityComparer<Dictionary<string, PythonObj>>.Default.GetHashCode(_Fields);
            return hashCode;
        }

        public enum ObjType
        {
            NONE,
            BOOL,
            FLOAT,
            INT,
            LONG,
            STRING,
            LIST,
            DICTIONARY,
            TUPLE,
            CLASS,
            NEWOBJ,
        }
    }
}
