using System.Diagnostics;
using System.Security.Cryptography;
using LoraRaspberry.DataHandler;

namespace LoraRaspberry.Communication;

public class CustomLoraNetwork : SingletonWrapper
{
	public class NetworkNode
	{
		public int senderID;
		public ushort parentRelayAddress;
		public bool isRegistered = false;
		public RegistrationAnswer registrationInfo;

	}
	
	private enum NetworkState
	{
		NodeDiscovery,
		RelayDiscovery,
		Registration,
		Data
	}

	private static Random randomGenerator = new Random();
	private static ushort mainAddress;
	private static int mainID;
	
	private static Thread checkPackagesQueue;
	private static Thread customNetworkHandling;

	private static bool continueCheckQueue;
	private static bool continueNetworkHandling;
	private static bool checkNextPackage = false;

	private static NetworkState currentNetworkState = NetworkState.NodeDiscovery;

	private static Dictionary<ushort, NetworkNode> networkNodes;
	private static Queue<ushort> dataNodes;
	private static ushort nextAvailableNodeAddress = 1;
	private static ushort nextNotifiedNodeAddress = 1;
	private static ushort nextRelayRequestedAddress = 1;

	private static ushort numberOfRelays;
	
	private static bool sentDiscoveryRequest = false;
	private static int discoveryTries = 0;
	private static bool receivedDiscoveryAnswer = false;
	private static bool waitingForRelayDiscovery = false;
	private static bool relaysDiscovered = false;
	
	private static bool sentRegistrationRequest = false;
	private static ushort currentRequestedNodeAddress = 0;
	private static bool receivedRegistrationAnswer = false;
	private static bool registrationSuccess = false;

	private static bool sentDataRequest = false;
	private static bool receivedData = false;

	private static DataPackage lastDataPackage;
	private static RegistrationAnswer lastRegistrationAnswer;

	private static Stopwatch watch;
	private static double messageReturnTimeoutInterval = 10;

	protected override void Awake()
	{
		networkNodes = new Dictionary<ushort, NetworkNode>();
		dataNodes = new Queue<ushort>();
		
		Console.WriteLine("CustomLoraNetwork Awake.");
		watch = new Stopwatch();
		
		Program.programExits += Stop;
		continueCheckQueue = true;
		checkPackagesQueue = new Thread(CheckQueue);

		continueNetworkHandling = true;
		customNetworkHandling = new Thread(HandleCustomNetwork);
	}

	protected override void Start()
	{
		Console.WriteLine("CustomLoraNetwork Start.");
		mainAddress = 0; //Fixed for ONE raspberry main Unit
		mainID = randomGenerator.Next();
		checkPackagesQueue.Start();
		customNetworkHandling.Start();
	}
	
	private static void Stop()
	{
		continueCheckQueue = false;
		continueNetworkHandling = false;
	}
	
	private static void CheckQueue()
	{
		while (continueCheckQueue)
		{
			if (!checkNextPackage || !SerialCommunication.TryGetNextPackage(out serialPackage package))
			{
				Thread.Sleep(500);
				continue;
			}
			
			checkNextPackage = false;
            
			try
			{
				HandlePackage(new DataPackage(package));
			}
			catch (Exception e)
			{
				Console.WriteLine($"Message caught: {e.Message}");
			}
		}
	}

	private static void HandleCustomNetwork()
	{
		while (continueNetworkHandling)
		{
			switch (currentNetworkState)
			{
				case NetworkState.NodeDiscovery:
					HandleNodeDiscovery();
					break;
				case NetworkState.RelayDiscovery:
					HandleRelayDiscovery();
					break;
				case NetworkState.Registration:
					HandleRegistration();
					break;
				case NetworkState.Data:
					HandleDataAcquisition();
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	private static void HandleNodeDiscovery()
	{
		if (!sentDiscoveryRequest)
		{
			sentDiscoveryRequest = true;
			
			LoraControl.StartNewPackage(0xFFFF, 0);
			numberOfRelays = 0;
			LoraControl.AddToPackage(numberOfRelays);
			LoraControl.AddToPackage((byte) Package.NetworkNodeDiscoveryRequest);
			LoraControl.AddToPackage(mainID); //SenderID
			LoraControl.AddToPackage(mainID); //MainUnitID
			LoraControl.AddToPackage(mainAddress);
			LoraControl.SendPackage();

			checkNextPackage = true;
			
			watch.Restart();
			Thread.Sleep(1000);
		} else if(receivedDiscoveryAnswer)
		{
			if (SerialCommunication.HasPackage())
			{
				checkNextPackage = true;
				Thread.Sleep(500);
				return;
			}
			
			receivedDiscoveryAnswer = false;

			for (ushort i = nextNotifiedNodeAddress; i < nextAvailableNodeAddress; i++)
			{
				LoraControl.StartNewPackage(0xFFFF, 0);
				numberOfRelays = 0;
				LoraControl.AddToPackage(numberOfRelays);
				LoraControl.AddToPackage((byte) Package.NetworkNodeAddressAssignment);
				LoraControl.AddToPackage(mainID); //SenderID
				LoraControl.AddToPackage(networkNodes[i].senderID);
				LoraControl.AddToPackage(i);
				LoraControl.AddToPackage(mainID); //MainUnitID
				LoraControl.SendPackage();
				
				Thread.Sleep(500);
			}

			nextNotifiedNodeAddress = nextAvailableNodeAddress;
			
			sentDiscoveryRequest = false;
			watch.Reset();
		} else if (watch.Elapsed.TotalSeconds > messageReturnTimeoutInterval)
		{
			sentDiscoveryRequest = false;
			watch.Reset();
			if (discoveryTries < 1)
			{
				discoveryTries++;
				return;
			}
			currentNetworkState = NetworkState.RelayDiscovery;
		}
		else
		{
			Thread.Sleep(1000);
		}
	}

	private static void HandleRelayDiscovery()
	{
		if (!waitingForRelayDiscovery && nextRelayRequestedAddress < nextNotifiedNodeAddress)
		{
			waitingForRelayDiscovery = true;
			
			LoraControl.StartNewPackage(nextRelayRequestedAddress, 0);
			numberOfRelays = 0;
			LoraControl.AddToPackage(numberOfRelays);
			LoraControl.AddToPackage((byte) Package.NetworkRelayDiscoveryRequest);
			LoraControl.AddToPackage(mainID);
			LoraControl.AddToPackage(networkNodes[nextRelayRequestedAddress].senderID);
			LoraControl.AddToPackage(nextAvailableNodeAddress);
			LoraControl.SendPackage();
			nextRelayRequestedAddress++;
			checkNextPackage = true;
			
			watch.Start();

			Thread.Sleep(1000);
		}
		else if (relaysDiscovered)
		{
			relaysDiscovered = false;
			waitingForRelayDiscovery = false;
		}else if (waitingForRelayDiscovery)
		{
			checkNextPackage = true;
			Thread.Sleep(100);
		}
		else
		{
			currentNetworkState = NetworkState.Registration;
		}
	}

	private static List<ushort> GetRelayList(ushort nodeAddress)
	{
		List<ushort> relayList = new() { nodeAddress };
		networkNodes.TryGetValue(nodeAddress, out NetworkNode node);
		while (node.parentRelayAddress > 0)
		{
			nodeAddress = node.parentRelayAddress;
			relayList.Insert(0, nodeAddress);
			networkNodes.TryGetValue(nodeAddress, out node);
		}

		return relayList;
	}
	
	private static void HandleRegistration()
	{
		if (!sentRegistrationRequest)
		{
			sentRegistrationRequest = true;
			registrationSuccess = true;
			foreach (KeyValuePair<ushort, NetworkNode> node in networkNodes.Where(node => !node.Value.isRegistered))
			{
				try
				{
					currentRequestedNodeAddress = node.Key;
					Console.WriteLine($"\nSending Registration Request to {currentRequestedNodeAddress}.");
					List<ushort> relayList = GetRelayList(currentRequestedNodeAddress);
					LoraControl.StartNewPackage(relayList[0], 0);
					ushort listLength = (ushort) (relayList.Count() - 1);
					LoraControl.AddToPackage(listLength);
					for (ushort i = 1; i < relayList.Count(); i++)
					{
						LoraControl.AddToPackage(relayList[i]);
					}
					LoraControl.AddToPackage((byte) Package.RegistrationRequest);
					LoraControl.AddToPackage(mainID);
					LoraControl.SendPackage();

					checkNextPackage = true;
					registrationSuccess = false;
					watch.Restart();
				}
				catch (Exception e)
				{
					Console.WriteLine($"Message caught: {e.Message}");
				}

				break;
			}
		} else if (receivedRegistrationAnswer )//|| watch.Elapsed.TotalSeconds > messageReturnTimeoutInterval)
		{
			sentRegistrationRequest = false;
			messageReturnTimeoutInterval = Math.Max(messageReturnTimeoutInterval, watch.Elapsed.TotalSeconds + 5);
			watch.Reset();
				
			if (receivedRegistrationAnswer)
			{
				receivedRegistrationAnswer = false;
				Console.WriteLine($"\nConfirming Registration of {lastDataPackage.senderId}");
				//Confirm received Registration
				List<ushort> relayList = GetRelayList(currentRequestedNodeAddress);
				LoraControl.StartNewPackage(relayList[0], 0);
				ushort listLength = (ushort) (relayList.Count() - 1);
				LoraControl.AddToPackage(listLength);
				for (ushort i = 1; i < relayList.Count(); i++)
				{
					LoraControl.AddToPackage(relayList[i]);
				}
				LoraControl.AddToPackage((byte) Package.OnRegistrationSuccess);
				LoraControl.AddToPackage(mainID);
				LoraControl.AddToPackage(lastDataPackage.senderId);
				LoraControl.SendPackage();
				
				//TODO:Wait appropriate time for clear channels
				Thread.Sleep(5000);
			}
			else
			{
				Console.WriteLine("Registration Request Timeout!");
			}
		} else if(!registrationSuccess)
		{
			Thread.Sleep(500);
		}
		else
		{
			currentNetworkState = NetworkState.Data;
		}
	}

	private static void HandleDataAcquisition()
	{
		if (!sentDataRequest)
		{
			sentDataRequest = true;
				
			try
			{
				//Take the first address as target and put it at the end of the queue
				currentRequestedNodeAddress = dataNodes.Dequeue();
				dataNodes.Enqueue(currentRequestedNodeAddress);

				if (!networkNodes.TryGetValue(currentRequestedNodeAddress, out NetworkNode node))
				{
					sentDataRequest = false;
					return;
				};
				
				Console.WriteLine("\nSending Data Request.");
				//Sent new Data Request
				List<ushort> relayList = GetRelayList(currentRequestedNodeAddress);
				LoraControl.StartNewPackage(relayList[0], 0);
				ushort listLength = (ushort) (relayList.Count() - 1);
				LoraControl.AddToPackage(listLength);
				for (ushort i = 1; i < relayList.Count(); i++)
				{
					LoraControl.AddToPackage(relayList[i]);
				}
				LoraControl.AddToPackage((byte) Package.DataRequest);
				LoraControl.AddToPackage(mainID);
				LoraControl.AddToPackage(node.registrationInfo.senderID);
				LoraControl.SendPackage();
				checkNextPackage = true;
					
				watch.Start();
			}
			catch (Exception e)
			{
				Console.WriteLine($"Message caught: {e.Message}");
			}
		} else if (receivedData || watch.Elapsed.TotalSeconds > messageReturnTimeoutInterval)
		{
			sentDataRequest = false;
			watch.Reset();
				
			//TODO: Properly control polling interval
			if (receivedData)
			{
				receivedData = false;
				//Poll data every x seconds
				Thread.Sleep(5000);
			}
			else
			{
				Console.WriteLine("Data Request Timeout!");
			}
		} else {
			Thread.Sleep(500);
		}
	}

	private static bool hasIdentified = false;
	private static int maxFakes = 0;
	private static int fakeCounter = 0;
	private static int fakeID = 8;
	private static int senderID = -1;
	private static Dictionary<int, ushort> IDToAddress = new ();
	private static void HandlePackage(DataPackage package)
	{
		lastDataPackage = package;
		switch (package.package)
		{
			////TEST//////
			case NetworkNodeDiscoveryRequest nodeDiscoveryRequest:
			{
				if (senderID < 0)
				{
					senderID = package.senderId;
				}
				if (fakeCounter >= maxFakes || senderID == package.senderId) break;
				
				LoraControl.StartNewPackage(nodeDiscoveryRequest.nodeAddress, 0);
				numberOfRelays = 0;
				LoraControl.AddToPackage(numberOfRelays);
				LoraControl.AddToPackage((byte) Package.NetworkNodeDiscoveryAnswer);
				LoraControl.AddToPackage(fakeID);//SenderID
				LoraControl.AddToPackage(package.senderId);
				LoraControl.SendPackage();
				
				checkNextPackage = true;
				
				break;
			}
			case NetworkNodeAddressAssignment networkNodeAddressAssignment:
			{
				if (networkNodeAddressAssignment.myID != fakeID)
				{
					Console.WriteLine("Received wrong Address Assignment.");
					Console.WriteLine($"Unit {networkNodeAddressAssignment.myID} got assigned {networkNodeAddressAssignment.assignedNodeAddress}");
					return;
				}
				Console.WriteLine($"Unit {networkNodeAddressAssignment.myID} got assigned {networkNodeAddressAssignment.assignedNodeAddress}");
				
				IDToAddress.TryAdd(networkNodeAddressAssignment.myID, networkNodeAddressAssignment.assignedNodeAddress);
				
				//Success of Fake Module
				fakeCounter++;
				fakeID *= 8;
				senderID = package.senderId;
				
				checkNextPackage = true;
				break;
			}
			case NetworkRelayDiscoveryRequest networkRelayDiscoveryRequest:
			{
				if (!IDToAddress.TryGetValue(networkRelayDiscoveryRequest.myID, out ushort address)) break;
				
				ushort startAddress = networkRelayDiscoveryRequest.nextAvailableNodeAddress;
				LoraControl.StartNewPackage(0xFFFF, 0);
				numberOfRelays = 0;
				LoraControl.AddToPackage(numberOfRelays);
				LoraControl.AddToPackage((byte) Package.NetworkRelayDiscoveryAnswer);
				LoraControl.AddToPackage(networkRelayDiscoveryRequest.myID);//SenderID
				LoraControl.AddToPackage(package.senderId);
				ushort nodeNumber = 0;
				if (networkRelayDiscoveryRequest.myID <= 8)
				{
					nodeNumber = 3;
				} else if (networkRelayDiscoveryRequest.myID <= 64)
				{
					nodeNumber = 2;
				}
				else
				{
					nodeNumber = 0;
				}
				LoraControl.AddToPackage(nodeNumber);

				if (nodeNumber == 3)
				{
					LoraControl.AddToPackage(address);
					LoraControl.AddToPackage(startAddress);
					LoraControl.AddToPackage(startAddress + 1);
				} else if (nodeNumber == 2)
				{
					LoraControl.AddToPackage(address);
					LoraControl.AddToPackage(address);
				}

				LoraControl.SendPackage();
				checkNextPackage = true;
				break;
			}

			case RegistrationRequest registrationRequest:
			{
				checkNextPackage = true;
				break;
			}
			case DataRequest dataRequest:
			{
				checkNextPackage = true;
				break;
			}
			case OnRegistrationSuccess onRegistrationSuccess:
			{
				checkNextPackage = true;
				break;
			}
			////TEST//////
			
			case NetworkNodeDiscoveryAnswer nodeDiscoveryAnswer:
			{
				if (nodeDiscoveryAnswer.sentParentID != mainID) break;
				
				networkNodes.Add(nextAvailableNodeAddress++, new NetworkNode()
				{
					parentRelayAddress = 0,
					senderID = package.senderId
				});
				receivedDiscoveryAnswer = true;
				break;
			}
			case NetworkRelayDiscoveryAnswer relayDiscoveryAnswer:
			{
				if (relayDiscoveryAnswer.sentParentID != mainID) break;
				
				if (relayDiscoveryAnswer.numberOfNodes > 0)
				{
					foreach(KeyValuePair<ushort, NetworkNode> networkNode in relayDiscoveryAnswer.networkNodes)
					{
						networkNodes.Add((ushort) (nextAvailableNodeAddress + networkNode.Key), networkNode.Value);
					}
					nextAvailableNodeAddress += relayDiscoveryAnswer.numberOfNodes;
				}
				relaysDiscovered = true;
				break;
			}
			case RegistrationAnswer registrationAnswer:
			{
				//TEST to not intercept the message too early
				/*if (package.senderId != senderID)
				{
					checkNextPackage = true;
					return;
				}*/
				
				
				receivedRegistrationAnswer = true;
				NetworkNode node = networkNodes[currentRequestedNodeAddress];
				node.isRegistered = true;
				node.registrationInfo = registrationAnswer;
				if (registrationAnswer.samples > 0)
				{
					dataNodes.Enqueue(currentRequestedNodeAddress);
				}
				Console.WriteLine($"Successfully handled Registration of {registrationAnswer.samples} samples.");
				break;
			}
			case CustomDataAnswer customDataAnswer:
			{
				//TEST to not intercept the message too early
				/*if (package.senderId != senderID)
				{
					checkNextPackage = true;
					return;
				}*/
				
				Console.WriteLine($"Received Data of {package.senderId}");
				receivedData = true;
				RegistrationAnswer info = networkNodes[currentRequestedNodeAddress].registrationInfo;
				customDataAnswer.TransformBytes(info.datatypes, info.names, info.samples);

				TryWriteInfluxData(customDataAnswer._boolDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._byteDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._ushortDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._shortDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._uintDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._intDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._floatDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._doubleDataPairs, customDataAnswer.timeStamp);
				TryWriteInfluxData(customDataAnswer._stringDataPairs, customDataAnswer.timeStamp);

				Console.WriteLine("Done Writing Data.");
				break;
			}
			
			case UndefinedPackage:
			{
				checkNextPackage = true;
				break;
			}
		}
	}

	private static void TryWriteInfluxData<T>(List<CustomDataAnswer.DataPair<T>> dataPairs, DateTime timeStamp)
	{
		if (dataPairs == null) return;
		
		foreach (CustomDataAnswer.DataPair<T> dataPair in dataPairs)
		{
			InfluxDBHandler.Write(currentRequestedNodeAddress, dataPair.name, dataPair.data, timeStamp);
		}
	}
}