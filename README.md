# Raspberry_Koordinationseinheit
## Code Compilation
It Uses C#-Version 11 and the Framework .Net 7.0
You need the following packages to be able to compile the code:

- InfluxDB.Client 4.11.0
- System.Device.Gpio 2.2.0
- System.IO.Ports 7.0.0

## Description

Official Extract of my german Master Thesis with the title "Development of a wireless sensor network for monitoring of deep geological
repositories in the operational phase":
>Zur Ausführung auf dem Raspberry Pi wird das Programm als portable Framework-abhängige Version veröffentlicht.
>Dies funktioniert auf jedem Rechner solange .Net auf dem System installiert ist und läuft.
>Das Programm für den Raspberry Pi hingegen besteht aus einzelnen zusammenhängenden Modulen, die man nicht alleinstehend installieren kann.
>Darunter ist das Hauptprogramm, das die Einteilung in die asynchron funktionierenden Module für das Handling der seriellen Kommunikation mit dem LoRa-Modul, das Koordinieren des Sensornetzwerks und das Schreiben von Daten in die Datenbank koordiniert und ausführt.
>Im Unterschied zum Arduino, der alles außer der seriellen Schnittstelle nur nacheinander bearbeiten kann, besitzt der Raspberry Pi vier Kerne, mit denen er in Echtzeit einzelne Anwendungen gleichzeitig bearbeiten kann.
>Um dies auszunutzen, werden vom Programm mehrere sogenannte Threads, also parallele Anwendungen, gestartet.
>Angefangen bei der seriellen Kommunikation, dient hier ein Thread dafür, permanent das LoRa-Modul auszulesen, ankommende Pakete mit Zeitstempeln zu versehen und für die Weiterverarbeitung in einer Warteschlange zur Verfügung zu stellen.
>Das Netzwerk überprüft in einem weiteren Thread diese Pakete auf Vollständigkeit und wertet die Daten entsprechend der Konventionen aus.
>In einem dritten Thread behält das Netzwerk den Überblick über alle stattfindenden Prozesse und eingegangen Pakete.
>Dort wird festgelegt, ob sich das Netzwerk in der Verbindungsaufbau- oder Datensammelphase befindet und welche Pakete an die einzelnen Sensoren geschickt werden müssen.
>Die anderen Module des Programms haben keine aktiven Aufgaben und dienen als Werkzeuge für die drei oben genannten parallen Prozesse.
>Zu den Werkzeugen gehören die einzelnen Schritte zum Umwandeln von Paketen in nutzbare Informationen, sowie das Ansprechen der Datenbank.
>Zuletzt sei hier noch der SingletonWrapper erwähnt, welcher den zuverlässigen Start des Programms sicherstellt und als einfache Oberklasse von Singletons dient.
>Singletons sind allgemein Klassen, deren Instanzen, wie statische Klassen, überall aus dem Code aufgerufen werden können.
>Sie müssen daher sicher initialisiert werden, bevor sie verwendet werden können.
>Diese Oberklasse ermöglicht eine klare Reihenfolge von Initialisierung durch die Awake-Methode und Starten mit der Start-Methode von untergeordneten Klassen.
>Auf diese Weise kann man sich sicher sein, dass alle notwendigen Strukturen in der Initialisierung erzeugt wurden, bevor beim Programmstart Klassen gegenseitig aufeinander zugreifen.
>Dieses Verhalten ist angelehnt an die MonoBehaviour-Klasse der Spiele-Engine Unity.

Translation by ChatGPT:

To run the program on the Raspberry Pi, it is released as a portable framework-dependent version. This works on any computer as long as .NET is installed on the system.

The Raspberry Pi program consists of interconnected modules that cannot be installed independently. Among these is the main program, which coordinates and executes the division into asynchronously functioning modules for handling serial communication with the LoRa module, coordinating the sensor network, and writing data to the database.

Unlike the Arduino, which can only process everything except the serial interface sequentially, the Raspberry Pi has four cores, allowing it to process individual applications simultaneously in real-time. To take advantage of this, the program launches several threads, or parallel applications.

- **Serial Communication Thread:**
  This thread continuously reads the LoRa module, timestamps incoming packets, and places them in a queue for further processing.

- **Network Thread:**
  Another thread in the network checks these packets for completeness and evaluates the data according to conventions.

- **Process Monitoring Thread:**
  In a third thread, the network keeps track of all ongoing processes and incoming packets. It determines whether the network is in the connection establishment or data collection phase and which packets need to be sent to individual sensors.

The other modules of the program have no active tasks and serve as tools for the three aforementioned parallel processes. These tools include individual steps for converting packets into usable information and accessing the database.

Lastly, the SingletonWrapper ensures the reliable startup of the program and serves as a simple superclass for Singletons. Singletons are generally classes whose instances, like static classes, can be called from anywhere in the code. Therefore, they must be reliably initialized before use. This superclass enables a clear order of initialization through the Awake method and starting with the Start method of subordinate classes.

This behavior is inspired by the MonoBehaviour class of the Unity game engine.
