using System.Runtime.InteropServices;
using System.IO;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Writes;

namespace LoraRaspberry.DataHandler;

public class InfluxDBHandler : SingletonWrapper
{
	
	public static string bucket = "Erster Test";
	public static string org = "Lehrstuhl fuer Endlagersicherheit";
	public static string allAccessToken = "";
	public static InfluxDBClient? dbClient;

	protected override void Awake()
	{
		Console.WriteLine("InfluxDB Awake.");
		string filePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../influxdbKey.txt")); //Path to textfile
		Console.WriteLine($"Searching for Token in {filePath}");
		try
		{
			allAccessToken = System.IO.File.ReadLines(filePath).First(); //Reads first line of file
			allAccessToken = allAccessToken.Replace(" ", ""); //Remove all spaces
			Console.WriteLine($"Token is {allAccessToken}");
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error reading AllAccessToken: " + ex.Message);
		}

		dbClient = new InfluxDBClient("http://localhost:8086", allAccessToken);
		Program.programExits += () =>
		{
			dbClient.Dispose();
			Console.WriteLine("dbClient disposed!");
		};
	}
	
	protected override void Start()
	{
		Console.WriteLine("InfluxDB Start.");
	}

	public static void Write<T>(ushort id, string fieldName, T data, DateTime timeStamp)
	{
		var point = PointData
			.Measurement("Arduino")
			.Tag("ID", id.ToString())
			.Tag("Name", "RelHumTempTest2")
			.Field(fieldName, data)
			.Timestamp(timeStamp, WritePrecision.Ns);
		
		using (var writeApi = dbClient.GetWriteApi())
		{
			writeApi.WritePoint(point, bucket, org);
		}
	}
}