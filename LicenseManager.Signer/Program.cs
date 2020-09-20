using System;
using System.Reflection;

namespace JereckNET.LicenseManager.Signer {
    class Program {
        public static void Main(string[] args) {
            string applicationName = Assembly.GetExecutingAssembly().GetName().Name;
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            Console.Title = "Jereck.NET Consulting : License Manager";

            Console.WriteLine($"License Manager [Version {currentVersion}]");
            Console.WriteLine("Copyright (c) 2020 Jereck.NET Consulting.  All rights reserved.");
            Console.WriteLine("");

            Manager main = new Manager(new Arguments(args), applicationName);
            main.Run();
        }
    }
}