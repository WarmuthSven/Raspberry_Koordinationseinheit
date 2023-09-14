using System.Net.Sockets;
using System.Text;

namespace LoraRaspberry.Communication;

public class LoraControl : SingletonWrapper
{
	private static byte[] byteBuffer;
	private static byte headerOffset;
	private static byte lengthHeaderPlace;
	private static byte size;

	protected override void Awake()
	{
		byteBuffer = new byte[243];
	}

	public static void StartNewPackage(ushort address, byte channel)
	{
		size = 0;
		
		if (channel > 83)
		{
			channel = 83;
		}

		//Convert Address to Big Endian (High Bytes first)
		byteBuffer[0] = (byte) (address >> 8);
		byteBuffer[1] = (byte) (address & 0xFF);
		size += 2;
		AddToPackage(channel);
		lengthHeaderPlace = size;
		size += 1; // message length as byte
		headerOffset = size;
	}

	public static void SendPackage()
	{
		byteBuffer[lengthHeaderPlace] = (byte) (size - headerOffset);
		SerialCommunication.Write(byteBuffer, size);
	}

	public static void AddToPackage(byte[] convertedData)
	{
		Array.Copy(convertedData, 0, byteBuffer, size, convertedData.Length);
		size += (byte) convertedData.Length; // byte conversion trims length to less than 256
	}
	
	public static void AddToPackage (bool data)
	{
		AddToPackage(BitConverter.GetBytes(data));
	}
	
	public static void AddToPackage (byte data)
	{
		byteBuffer[size] = data;
		size += 1;
	}
	
	public static void AddToPackage (ushort data)
	{
		AddToPackage(BitConverter.GetBytes(data));
	}
	
	public static void AddToPackage (short data)
	{
		AddToPackage(BitConverter.GetBytes(data));
	}
	
	public static void AddToPackage (uint data)
	{
		AddToPackage(BitConverter.GetBytes(data));
	}
	
	public static void AddToPackage (int data)
	{
		AddToPackage(BitConverter.GetBytes(data));
	}
	
	public static void AddToPackage (float data)
	{
		AddToPackage(BitConverter.GetBytes(data));
	}
	
	public static void AddToPackage (double data)
	{
		AddToPackage(BitConverter.GetBytes(data));
	}
	
	public static void AddToPackage (string data)
	{
		AddToPackage((byte) data.Length);
		AddToPackage(Encoding.UTF8.GetBytes(data));
	}
}