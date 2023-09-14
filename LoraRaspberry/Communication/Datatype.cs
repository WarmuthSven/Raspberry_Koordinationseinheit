using System.Text;

namespace LoraRaspberry.Communication
{
    public enum Datatype : byte{
        Undefined = 0,
        Bool = 1,
        Byte = 2,
        UInt = 3,
        Int = 4,
        ULong = 5,
        Long = 6,
        Float = 7,
        Double = 8, 
        String = 9
    }
    
    public static class ByteArrayConverter
    {
        public const int cppBoolSize = 1;
        public const int cppByteSize = 1;
        public const int cppUShortSize = 2;
        public const int cppShortSize = 2; //int in c++
        public const int cppULongSize = 4; //uint in c#
        public const int cppLongSize = 4; // int in c#
        public const int cppULongLongSize = 8; //ulong in c#
        public const int cppLongLongSize = 8; //long in c#
        public const int cppFloatSize = 4;
        public const int cppDoubleSize = 8;

        private static Dictionary<Datatype, int> datatypeSizes = new()
        {
            [Datatype.Undefined] = 0,
            [Datatype.Bool] = cppBoolSize,
            [Datatype.Byte] = cppByteSize,
            [Datatype.UInt] = cppUShortSize,
            [Datatype.Int] = cppShortSize,
            [Datatype.ULong] = cppULongSize,
            [Datatype.Long] = cppLongSize,
            [Datatype.Float] = cppFloatSize,
            [Datatype.Double] = cppDoubleSize,
            [Datatype.String] = 0

        };

        public static int GetSizeOf(Datatype type)
        {
            if (!datatypeSizes.TryGetValue(type, out int size))
            {
                size = 0;
            }

            return size;
        }

        /*public static int WriteBytes<T>(byte[] byteArray, int offset, T data)
        {
            int size = GetSizeOf(type);

            
            return offset + size;
        }*/
        
        public static int ConvertData(byte[] byteArray, int offset, out bool data)
        {
            int size = cppBoolSize;
            data = BitConverter.ToBoolean( byteArray[offset .. (offset + size)]);
            //Console.WriteLine($"Bool {data}");
            return offset + size;
        }
        
        public static int ConvertData(byte[] byteArray, int offset, out Datatype data)
        {
            offset = ConvertData(byteArray, offset, out ushort convertedData, cppByteSize);
            data = (Datatype) convertedData;
            return offset;
        }
        
        public static int ConvertData(byte[] byteArray, int offset, out byte data)
        {
            offset = ConvertData(byteArray, offset, out ushort convertedData, cppByteSize);
            data = (byte) convertedData;
            return offset;
        }

        public static int ConvertData(byte[] byteArray, int offset, out short data, int size = cppShortSize)
        {
            data = size == 1 ? Convert.ToInt16(byteArray[offset]) : BitConverter.ToInt16(new ReadOnlySpan<byte>(byteArray, offset, size));
            //Console.WriteLine($"Short {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out ushort data, int size = cppUShortSize)
        {
            data = size == 1 ? Convert.ToUInt16(byteArray[offset]) : BitConverter.ToUInt16(new ReadOnlySpan<byte>(byteArray, offset, size));
            //Console.WriteLine($"UShort {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out int data, int size = cppLongSize)
        {
            data = size == 1 ? Convert.ToInt32(byteArray[offset]) : BitConverter.ToInt32(new ReadOnlySpan<byte>(byteArray, offset, size));
            //Console.WriteLine($"int/cppLong {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out uint data, int size = cppULongSize)
        {
            data = size == 1 ? Convert.ToUInt32(byteArray[offset]) : BitConverter.ToUInt32(new ReadOnlySpan<byte>(byteArray, offset, size));
            //Console.WriteLine($"uint/cppULong {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out long data, int size = cppLongLongSize)
        {
            data = size == 1 ? Convert.ToInt64(byteArray[offset]) : BitConverter.ToInt64(new ReadOnlySpan<byte>(byteArray, offset, size));
            //Console.WriteLine($"long/cppLong Long {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out ulong data, int size = cppULongLongSize)
        {
            data = size == 1 ? Convert.ToUInt64(byteArray[offset]) : BitConverter.ToUInt64(new ReadOnlySpan<byte>(byteArray, offset, size));
            //Console.WriteLine($"ulong/cppULong Long {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out float data, int size = cppFloatSize)
        {
            data = BitConverter.ToSingle(byteArray[offset .. (offset + size)]);
            //Console.WriteLine($"Float {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out double data, int size = cppDoubleSize)
        {
            data = BitConverter.ToDouble(byteArray[offset .. (offset + size)]);
            //Console.WriteLine($"Double {data}");
            return offset + size;
        }

        public static int ConvertData(byte[] byteArray, int offset, out string data)
        {
            offset = ConvertData(byteArray, offset, out int stringSize, 1);
            data = Encoding.UTF8.GetString(byteArray[offset .. (offset + stringSize)]);
            //Console.WriteLine($"String {data}");
            return offset + stringSize;
        }
    }
}