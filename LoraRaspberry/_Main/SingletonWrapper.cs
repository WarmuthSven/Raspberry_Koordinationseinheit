using System.Reflection;
using CsvHelper;

// ReSharper disable once CheckNamespace
namespace LoraRaspberry;

public class SingletonWrapper
{
	private static List<SingletonWrapper>? _singletons;
	private static bool _isInitialized = false;

	public static void InitializeAll()
	{
		if (_isInitialized) return;
		_isInitialized = true;
		Console.WriteLine("Initialize all Singletons.");
		
		Type[] types = Assembly.GetExecutingAssembly().GetTypes();
		_singletons = new List<SingletonWrapper>(types.Length);
		
		// Find all derived classes and add them to the list of singletons
		foreach (Type type in types)
		{
			if (!type.IsSubclassOf(typeof(SingletonWrapper)) || !type.HasConstructor()) continue;
			if (type.GetConstructor(Type.EmptyTypes)?.Invoke(null) is SingletonWrapper singleton)
			{
				_singletons.Add(singleton);
			}
		}

		Console.WriteLine("Awake all Singletons.");
		foreach (SingletonWrapper singleton in _singletons)
		{
			Console.WriteLine($"Awake {singleton}.");
			singleton.Awake();
		}

		Console.WriteLine("Start all Singletons.");
		foreach (SingletonWrapper singleton in _singletons)
		{
			Console.WriteLine($"Start {singleton}.");
			singleton.Start();
		}
	}

	protected virtual void Awake()
	{
		Console.WriteLine("The parent Awake got called.");
	}

	protected virtual void Start()
	{
		Console.WriteLine("The parent Start got called.");
	}
}
