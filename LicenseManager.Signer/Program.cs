using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace JereckNET.LicenseManager.Signer {
    class Program {

        public static void Main(String[] args) {
            string applicationName = Assembly.GetExecutingAssembly().GetName().Name;
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;

            Console.Title = "Jereck.NET Consulting : License Manager";

            Console.WriteLine($"License Manager [Version {currentVersion}]");
            Console.WriteLine("Copyright (c) 2020 Jereck.NET Consulting.  All rights reserved.");
            Console.WriteLine("");

            string operation = "/help";
            if (args.Length > 0) {
                operation = args[0];
            }

            switch (operation) {
                case "/generateKeys":
                    generateKeys(args[1], args[2]);
                    Console.WriteLine("Keys created.");

                    break;

                case "/sign":
                    bool base64 = false;

                    if (args.Length == 5 && args[4] == "/base64") {
                        base64 = true;
                    }

                    signLicense(args[1], args[2], args[3], base64);
                    Console.WriteLine("License content signed.");

                    break;

                case "/verify":
                    bool result = verifyLicense(args[1], args[2]);

                    if (result) {
                        Console.WriteLine("The license signature is valid.");
                    } else {
                        Console.WriteLine("The license signature is not valid.");
                    }

                    break;

                case "/help":
                default:
                    showUsage(applicationName);

                    break;
            }
        }

        private static void showUsage(string applicationName) {
            Console.WriteLine("Usage: ");
            Console.WriteLine($"{applicationName}.exe [{{ /generateKeys | /signLicense | /verifyLicense }} <options>]");
            Console.WriteLine("");

            Console.WriteLine("/generateKeys <Public Key Path> <Private Key Path>");
            Console.WriteLine("\tGenerates a public/private keys pair and stores them in the specified files.");
            Console.WriteLine("");
            Console.WriteLine("\t<Public Key Path>\tThe path of the file that will contain the generated public key");
            Console.WriteLine("\t<Private Key Path>\tThe path of the file that will contain the generated private key");
            Console.WriteLine("");

            Console.WriteLine("/sign <Private Key Path> <License Content Path> <License Path> [/base64]");
            Console.WriteLine("\tUses the specified private key to sign the license content and generates a signed license file.");
            Console.WriteLine("");
            Console.WriteLine("\t<Private Key Path>\tThe path of the file that contains the private key");
            Console.WriteLine("\t<License Content Path>\tThe path of the file that contains the license content");
            Console.WriteLine("\t<License Path>\t\tThe path of the file that will contain the signed license");
            Console.WriteLine("\t/base64\t\t\tWill generate a Base64-encoded file instead of an XML file.");
            Console.WriteLine("");

            Console.WriteLine("/verify <Public Key Path> <License Path>");
            Console.WriteLine("");
            Console.WriteLine("\tChecks the validity of a license file against a specified public key");
            Console.WriteLine("");
            Console.WriteLine("\t<Public Key Path>\tThe path of the file that contains the public key");
            Console.WriteLine("\t<License Path>\t\tThe path of the file that contains the license to check");
            Console.WriteLine("");
        }

        private static void generateKeys(string publicKeyFilePath, string privateKeyFilePath, int keySize = 2048) {
            string publicKey;
            string privateKey;

            using (RSACryptoServiceProvider provider = new RSACryptoServiceProvider(keySize)) {

                publicKey = provider.ToXmlString(false);
                privateKey = provider.ToXmlString(true);
            }

            using (StreamWriter sw = File.CreateText(publicKeyFilePath)) {
                sw.Write(publicKey);
                sw.Close();
            }
            using (StreamWriter sw = File.CreateText(privateKeyFilePath)) {
                sw.Write(privateKey);
                sw.Close();
            }
        }

        private static void signLicense(string privateKeyFilePath, string licenseContentPath, string licenseFilePath, bool base64) {
            string privateKey = File.ReadAllText(privateKeyFilePath);
            string licenseContent = File.ReadAllText(licenseContentPath);

            License newLicense = new License() {
                Content = licenseContent
            };

            newLicense.Sign(privateKey);
            newLicense.Save(licenseFilePath, base64);
        }
        private static bool verifyLicense(string _publicKeyFilePath, string _licenseFilePath) {
            string publicKey = File.ReadAllText(_publicKeyFilePath);

            License licenseToVerify = License.Load(_licenseFilePath);

            bool? result = licenseToVerify?.Verify(publicKey);

            return result ?? false;
        }

    }
}

