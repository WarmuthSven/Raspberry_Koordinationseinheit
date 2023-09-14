using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Device.Gpio;
using System.Text;

using System.Threading.Tasks;

namespace LoraRaspberry.Communication;

public struct serialPackage
{
    public bool containsRssi;
    public byte[] bytes;
    public int size;
    public DateTime timeStamp;
}

public class SerialCommunication : SingletonWrapper
{
    public static SerialCommunication? Singleton;
    
    private static SerialPort? _serialPort;
    private static string? _port;

    private const string _windowsPort = "COM3";
    private const string _linuxPort = "/dev/ttyAMA0"; //Hardware Serial Port UART

    private const int M0Pin = 22;
    private const int M1Pin = 27;
    
    private const int _timeoutLimit = 500;
    
    private static Stopwatch watch;
    private static long minMessageWaitTime;

    private static Queue<serialPackage> _serialPackagesQueue;
    
    private static Thread checkByteQueue;
    private static bool keepReadingBytes;

    protected override void Awake()
    {
        if (Singleton != null) return;
        Singleton = this;
        Console.WriteLine("Awake SerialCommunication.");

        watch = new Stopwatch();
        
        DebugPorts();
        Program.programExits += Stop;

        _serialPackagesQueue = new Queue<serialPackage>(10);
        
        keepReadingBytes = true;
        checkByteQueue = new Thread(ReadBytes);
        
        if (_port == null)
        {
            Console.WriteLine("Preferred Port not available. Choose another!");
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Console.WriteLine("Setting up GPIO.");
            using GpioController controller = new ();
            controller.OpenPin(M0Pin, PinMode.Output);
            controller.Write(M0Pin, PinValue.Low);
            controller.OpenPin(M1Pin, PinMode.Output);
            controller.Write(M1Pin, PinValue.Low);
            Console.WriteLine("Successfully set up GPIO.");
        }
        
        _serialPort = new SerialPort(_port, 9600, Parity.None, 8, StopBits.One);
        _serialPort.Handshake = Handshake.None;
        _serialPort.ReadTimeout = _timeoutLimit;
        _serialPort.WriteTimeout = _timeoutLimit;
        _serialPort.Open();
        
        checkByteQueue.Start();
        Console.WriteLine("Serial Communication was started!");
    }

    protected override void Start()
    {
    }

    private static void Stop()
    {
        _serialPort?.Close();
        while (_serialPackagesQueue.Count > 0)
        {
        }

        keepReadingBytes = false;
    }

    public static bool TryGetNextPackage(out serialPackage package)
    {
        if (_serialPackagesQueue.Count <= 0)
        {
            package = new serialPackage();
            return false;
        }

        package = _serialPackagesQueue.Dequeue();
        return true;
    }

    public static bool HasPackage()
    {
        return _serialPackagesQueue.Count > 0;
    }

    private static void ReadBytes()
    {
        DateTime timeStamp = DateTime.Now;
        bool receiveRssi = true;
        int incomingBytes = 0;
        int readBytes = 0;
        byte[] bytes = new byte[1];
        while (keepReadingBytes)
        {
            int bytesToRead = _serialPort.BytesToRead;
            if (bytesToRead <= 0)
            {
                Thread.Sleep(500);
                continue;
            }

            if (incomingBytes <= 0)
            {
                incomingBytes = _serialPort.ReadByte();
                //Receive RSSI Value at the end
                if (receiveRssi) incomingBytes++;
                
                bytesToRead -= 1;
                timeStamp = DateTime.Now;
                bytes = new byte[incomingBytes];
            }
            
            try
            {
                bytesToRead = readBytes + bytesToRead > incomingBytes ? incomingBytes - readBytes : bytesToRead;
                _serialPort.Read(bytes, readBytes, bytesToRead);
                readBytes += bytesToRead;
            }
            catch (TimeoutException)
            {
            }

            if (readBytes < incomingBytes) continue;
            
            _serialPackagesQueue.Enqueue(new serialPackage()
            {
                containsRssi = receiveRssi,
                bytes = bytes,
                size = incomingBytes,
                timeStamp = timeStamp
            });
            
            //Console.WriteLine($"Serial Package arrived at {timeStamp:HH:m:s}");
            //Console.WriteLine($"Contains {incomingBytes} Bytes.");
            
            incomingBytes = 0;
            readBytes = 0;
        }
    }

    public static void Write(string message)
    {
        Console.WriteLine("Message " + message + " sent!");
        _serialPort?.WriteLine(message);
    }

    public static void Write(byte[] byteBuffer, int byteSize)
    {
        if (_serialPort is not { IsOpen: true }) return;

        while (_serialPort.BytesToWrite > 0 || watch.ElapsedMilliseconds < minMessageWaitTime)
        {
            //Wait for the buffer to clear to send separate messages
        }
        Console.WriteLine($"\nSending {byteSize} bytes at {DateTime.Now:HH:m:s}!");
        _serialPort?.Write(byteBuffer, 0 ,byteSize);
        minMessageWaitTime = byteSize * 40;
        watch.Reset();
        watch.Start();
    }

    private static void DebugPorts()
    {
        string[] portNames = SerialPort.GetPortNames();
        string preferredPort = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? _linuxPort : _windowsPort;

        Console.WriteLine("Available Ports:");
        foreach (string port in portNames)
        {
            Console.WriteLine($"{port}");

            if (!string.Equals(port, preferredPort)) continue;
            Console.WriteLine($"Was Chosen.");

            _port = port;
        }
    }
}