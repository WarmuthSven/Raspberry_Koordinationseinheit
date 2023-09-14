using System.Runtime.CompilerServices;
using LoraRaspberry.DataHandler;

namespace LoraRaspberry.Communication;

public enum Package : byte
{
	NetworkNodeDiscoveryRequest = 0,
	NetworkNodeDiscoveryAnswer = 1,
	NetworkNodeAddressAssignment = 2,
	NetworkRelayDiscoveryRequest = 3,
	NetworkRelayDiscoveryAnswer = 4,
	RegistrationRequest = 5,
	RegistrationAnswer = 6,
	OnRegistrationSuccess = 7,
	DataRequest = 8,
	CustomDataAnswer = 9,
	DataRandomTest = 10,
	DataAnswerEnvironment = 11,
	DataAnswerGas = 12
}

public class DataPackage
{
	public readonly Package packageId;
	public readonly IDataPackage package;
	
	public readonly int senderId;
	public readonly short rssiValue;

	public DataPackage(serialPackage serialPackage)
	{
		byte[] bytes = serialPackage.bytes;
		int packageSize = serialPackage.size;

		if (packageSize <= 0)
		{
			throw new InvalidDataException("This data package does not contain any data.");
		}
		
		if (serialPackage.containsRssi)
		{
			ByteArrayConverter.ConvertData(bytes, packageSize-1, out rssiValue, 1);
			packageSize -= 1;
			rssiValue += -256 ;
			
			//Console.WriteLine($"{rssiValue}dBm");
		}
		
		int offset = ByteArrayConverter.ConvertData(bytes, 0, out ushort numberOfRelays);
		for (int i = 0; i < numberOfRelays; i++)
		{
			offset = ByteArrayConverter.ConvertData(bytes, offset, out ushort nextAddresses);
		}
		
		offset = ByteArrayConverter.ConvertData(bytes, offset, out short id, 1);
		packageId = (Package) id;
		package = GetPackage(packageId);
		package.timeStamp = serialPackage.timeStamp;
		
		offset = ByteArrayConverter.ConvertData(bytes, offset, out senderId);
		package.senderID = senderId;
		
		Console.WriteLine($"\nPackage {packageId} at time {package.timeStamp:HH:m:s} from {senderId}.");
		Console.WriteLine($"{numberOfRelays} Relays detected.");
		
		int dataSize = packageSize - offset;
		if (package.GetMinByteSize() > dataSize)
		{
			throw new InvalidDataException("This data package is not complete.");
		}
		
		package.SetAndReadBytes(bytes[offset .. packageSize], dataSize);
	}
	
	private static IDataPackage GetPackage(Package packageEnum)
	{
		return packageEnum switch
		{
			Package.NetworkNodeDiscoveryRequest => new NetworkNodeDiscoveryRequest(),
			Package.NetworkNodeDiscoveryAnswer => new NetworkNodeDiscoveryAnswer(),
			Package.NetworkNodeAddressAssignment => new NetworkNodeAddressAssignment(),
			Package.NetworkRelayDiscoveryRequest => new NetworkRelayDiscoveryRequest(),
			Package.NetworkRelayDiscoveryAnswer => new NetworkRelayDiscoveryAnswer(),
			Package.RegistrationRequest => new RegistrationRequest(),
			Package.RegistrationAnswer => new RegistrationAnswer(),
			Package.OnRegistrationSuccess => new OnRegistrationSuccess(),
			Package.DataRequest => new DataRequest(),
			Package.CustomDataAnswer => new CustomDataAnswer(),
			Package.DataAnswerEnvironment => new DataAnswerEnvironment(),
			Package.DataAnswerGas => new DataAnswerGas(),
			Package.DataRandomTest => new DataRandomTest(),
			_ => new UndefinedPackage()
		};
	}
}

public abstract class IDataPackage
{
	public DateTime timeStamp;
	
	protected Byte[] bytes;
	protected int byteSize;
	protected int offset;

	public int senderID;

	public void SetAndReadBytes(byte[] bytes, int byteSize)
	{
		this.bytes = bytes;
		this.byteSize = byteSize;
		offset = 0;
		ReadBytes();
	}

	protected virtual void ReadBytes() {}

	public virtual int GetMinByteSize()
	{
		return 0;
	}
}

public class UndefinedPackage : IDataPackage
{
	protected override void ReadBytes()
	{
		throw new NotImplementedException("This package is undefined.");
	}
}

public class NetworkNodeDiscoveryRequest : IDataPackage
{
	public int mainUnitID;
	public ushort nodeAddress;

	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out mainUnitID);
		offset = ByteArrayConverter.ConvertData(bytes, offset, out nodeAddress);
		Console.WriteLine($"Main Unit ID: {mainUnitID}, Active Address: {nodeAddress}");
	}
}

public class NetworkNodeDiscoveryAnswer : IDataPackage
{
	public int sentParentID;
	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out sentParentID);
		Console.WriteLine($"Parent ID: {sentParentID}");
	}
}

public class NetworkNodeAddressAssignment : IDataPackage
{
	public int myID;
	public ushort assignedNodeAddress;
	public int mainUnitID;

	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out myID);
		offset = ByteArrayConverter.ConvertData(bytes, offset, out assignedNodeAddress);
		offset = ByteArrayConverter.ConvertData(bytes, offset, out mainUnitID);
		Console.WriteLine($"Main Unit ID: {mainUnitID}, my ID: {myID}, new Address: {assignedNodeAddress}");
	}
}

public class NetworkRelayDiscoveryRequest : IDataPackage
{
	public int myID;
	public ushort nextAvailableNodeAddress;

	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out myID);
		offset = ByteArrayConverter.ConvertData(bytes, offset, out nextAvailableNodeAddress);
		Console.WriteLine($"My ID: {myID}, next Address: {nextAvailableNodeAddress}");
	}
}

public class NetworkRelayDiscoveryAnswer : IDataPackage
{
	public int sentParentID;
	public ushort numberOfNodes;
	public Dictionary<ushort, CustomLoraNetwork.NetworkNode> networkNodes;
	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out sentParentID);
		offset = ByteArrayConverter.ConvertData(bytes, offset, out numberOfNodes);
		
		Console.WriteLine($"Parent ID: {sentParentID}, added Nodes: {numberOfNodes}");
		if (numberOfNodes <= 0) return;

		networkNodes = new Dictionary<ushort, CustomLoraNetwork.NetworkNode>();
		
		for (ushort i = 0; i < numberOfNodes; i++)
		{
			offset = ByteArrayConverter.ConvertData(bytes, offset, out ushort parentAddress);
			networkNodes.Add(i, new CustomLoraNetwork.NetworkNode()
			{
				parentRelayAddress = parentAddress
			});
			Console.WriteLine($"Node: {i}, parent Address: {parentAddress}");
		}
	}
}

public class RegistrationRequest : IDataPackage {}

public class RegistrationAnswer : IDataPackage
{
	public short samples = 0;
	public String[] names;
	public Datatype[] datatypes;
	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out samples);
		Console.WriteLine($"Received {samples} registered samples.");
		names = new String[samples];
		datatypes = new Datatype[samples];
		
		for (int i = 0; i < samples; i++)
		{
			offset = ByteArrayConverter.ConvertData(bytes, offset, out Datatype datatype);
			datatypes[i] = datatype;
			
			offset = ByteArrayConverter.ConvertData(bytes, offset, out String name);
			names[i] = name;
			
			Console.WriteLine($"- Registered {name} of type {datatype}");
		}
	}
}

public class OnRegistrationSuccess : IDataPackage {}

public class DataRequest : IDataPackage {}

public class CustomDataAnswer : IDataPackage
{
	public class DataPair<T>
	{
		public DataPair(T data, string name)
		{
			this.data = data;
			this.name = name;
		}

		public T data;
		public string name;
	}

	public List<DataPair<bool>> _boolDataPairs;
	public List<DataPair<byte>> _byteDataPairs;
	public List<DataPair<ushort>> _ushortDataPairs;
	public List<DataPair<short>> _shortDataPairs;
	public List<DataPair<uint>> _uintDataPairs;
	public List<DataPair<int>> _intDataPairs;
	public List<DataPair<float>> _floatDataPairs;
	public List<DataPair<double>> _doubleDataPairs;
	public List<DataPair<string>> _stringDataPairs;

	public void TransformBytes(Datatype[] datatypes, string[] dataNames, int size)
	{
		for (int i = 0; i < size; i++)
		{
			Datatype type = datatypes[i];
			string name = dataNames[i];
			switch (type)
			{
				case Datatype.Bool:
				{
					_boolDataPairs ??= new List<DataPair<bool>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out bool data);
					_boolDataPairs.Add(new DataPair<bool>(data, name));
					break;
				}
				case Datatype.Byte:
				{
					_byteDataPairs ??= new List<DataPair<byte>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out byte data);
					_byteDataPairs.Add(new DataPair<byte>(data, name));
					break;
				}
				case Datatype.UInt:
				{
					_ushortDataPairs ??= new List<DataPair<ushort>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out ushort data);
					_ushortDataPairs.Add(new DataPair<ushort>(data, name));
					break;
				}
				case Datatype.Int:
				{
					_shortDataPairs ??= new List<DataPair<short>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out short data);
					_shortDataPairs.Add(new DataPair<short>(data, name));
					break;
				}
				case Datatype.ULong:
				{
					_uintDataPairs ??= new List<DataPair<uint>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out uint data);
					_uintDataPairs.Add(new DataPair<uint>(data, name));
					break;
				}
				case Datatype.Long:
				{
					_intDataPairs ??= new List<DataPair<int>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out int data);
					_intDataPairs.Add(new DataPair<int>(data, name));
					break;
				}
				case Datatype.Float:
				{
					_floatDataPairs ??= new List<DataPair<float>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out float data);
					_floatDataPairs.Add(new DataPair<float>(data, name));
					break;
				}
				case Datatype.Double:
				{
					_doubleDataPairs ??= new List<DataPair<double>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out double data);
					_doubleDataPairs.Add(new DataPair<double>(data, name));
					break;
				}
				case Datatype.String:
				{
					_stringDataPairs ??= new List<DataPair<string>>();

					offset = ByteArrayConverter.ConvertData(bytes, offset, out string data);
					_stringDataPairs.Add(new DataPair<string>(data, name));
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

}

public class DataRandomTest : IDataPackage
{
	public short randomTest;
	
	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out randomTest);

		Console.WriteLine($"Test: {randomTest}");
	}

	public override int GetMinByteSize()
	{
		return 1 * ByteArrayConverter.cppShortSize;
	}
}

public class DataAnswerEnvironment : IDataPackage
{
	public float temperature;
	public float humidity;
	public string? tempHumidityTest;
	
	protected override void ReadBytes()
	{
		offset = ByteArrayConverter.ConvertData(bytes, offset, out temperature);
		offset = ByteArrayConverter.ConvertData(bytes, offset, out humidity);
		ByteArrayConverter.ConvertData(bytes, offset, out tempHumidityTest);

		InfluxDBHandler.Write(0, "temperature", temperature, timeStamp);
		InfluxDBHandler.Write(0, "humidity", humidity, timeStamp);

		//Console.WriteLine($"Temp: {temperature}, Humid: {humidity}");
		//Console.WriteLine($"StringTest: {tempHumidityTest}");
	}

	public override int GetMinByteSize()
	{
		return 2 * ByteArrayConverter.cppFloatSize + 1;
	}
}

public class DataAnswerGas : IDataPackage {}