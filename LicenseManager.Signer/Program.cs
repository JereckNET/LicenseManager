﻿using System;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Security;

namespace JereckNET.LicenseManager.Signer {
    class Program {
        public static void Main(string[] args) {
            string applicationName = Assembly.GetExecutingAssembly().GetName().Name;
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            Console.Title = "Jereck.NET Consulting : License Manager";

            Console.WriteLine($"License Manager [Version {currentVersion}]");
            Console.WriteLine("Copyright (c) 2020 Jereck.NET Consulting.  All rights reserved.");

#if     DEBUG && NETFRAMEWORK
            Console.WriteLine("Running as .NET Framework");
#elif   DEBUG && NETCOREAPP                                  
            Console.WriteLine("Running as .NET Core");
#endif
            Console.WriteLine("");

            Manager main = new Manager(new Arguments(args), applicationName);
            main.Run();
        }

        /// <summary>
        /// Gets the console secure password.
        /// </summary>
        /// <returns>The typed password as a <see cref="SecureString"/></returns>
        /// <remarks>Code copied from <see href="https://gist.github.com/huobazi/1039424" /></remarks>
        public static SecureString GetConsoleSecurePassword() {
            SecureString pwd = new SecureString();
            while (true) {
                ConsoleKeyInfo i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter) {
                    Console.WriteLine();
                    break;
                } else if (i.Key == ConsoleKey.Backspace) {
                    if (pwd.Length > 0) {
                        pwd.RemoveAt(pwd.Length - 1);
                        Console.Write("\b \b");
                    }
                } else {
                    if (!char.IsControl(i.KeyChar)) {
                        pwd.AppendChar(i.KeyChar);
                    }
                }
            }
            return pwd;
        }

        /// <summary>
        /// Tests a string to check if it is a valid string representation of a certificate's thumbprint.
        /// </summary>
        /// <param name="value">The string to test.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a valid string representation of a certificate's thumbprint.</returns>
        /// <remarks>The certificate thumbprint must be a 40-digits hexadecimal representation of the 160 bits SHA1 thumbprint.</remarks>
        public static bool IsCertificateThumbprint(string value) {
            return value.Length == 40 && BigInteger.TryParse(value, NumberStyles.HexNumber, null, out _);
        }
    }
}