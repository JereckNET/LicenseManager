using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace JereckNET.LicenseManager.Signer {
    internal class Manager {
        private readonly Arguments _arguments;
        private readonly string _applicationName;

        public Manager(Arguments arguments, string applicationName) {
            _arguments = arguments;
            _applicationName = applicationName;
        }
        public void Run() {
            bool result;

            if (_arguments.HasError) {
                if (_arguments.ShowHelp) {
                    showUsage(_applicationName);
                }
            } else {
                switch (_arguments.Operation) {
                    case Operations.GenerateKey:
                        generateKeys(_arguments.PublicKeyFilePath, _arguments.PrivateKeyFilePath);
                        Console.WriteLine("Keys created.");

                        break;

                    case Operations.Sign:
                        result = signLicense(_arguments.PrivateKeyFilePath, _arguments.LicenseContentPath, _arguments.LicensePath, _arguments.Base64);
                        if (result) {
                            Console.WriteLine("License content signed.");
                        } else {
                            Console.WriteLine("License content could not be signed.");
                        }

                        break;

                    case Operations.Verify:
                        result = verifyLicense(_arguments.PublicKeyFilePath, _arguments.LicensePath);

                        if (result) {
                            Console.WriteLine("The license signature is valid.");
                        } else {
                            Console.WriteLine("The license signature is not valid.");
                        }

                        break;

                    case Operations.ShowHelp:
                        showUsage(_applicationName);
                        break;
                }
            }

        }

        private void showUsage(string applicationName) {
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

        private void generateKeys(string publicKeyFilePath, string privateKeyFilePath) {
            string publicKey;
            string privateKey;

            using (RSACryptoServiceProvider provider = new RSACryptoServiceProvider(2048)) {
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

        private bool signLicense(string privateKeyFilePath, string licenseContentPath, string licenseFilePath, bool base64, int keySize = 2048) {
            bool result = false;
            
            string licenseContent = File.ReadAllText(licenseContentPath);

            License newLicense = new License() {
                Content = licenseContent
            };

            if (new FileInfo(privateKeyFilePath).Extension != ".pfx"){

                string privateKey = File.ReadAllText(privateKeyFilePath);

                result = newLicense.Sign(privateKey, keySize);
            } else {
                SecureString importPassword;

                if (true) {
                    Console.Write("Please type the import password: ");
                    importPassword = Program.GetConsoleSecurePassword();
                }

                try {
                    X509Certificate2 certificate = new X509Certificate2(privateKeyFilePath, importPassword,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

                    result = newLicense.Sign(certificate);
                }catch(CryptographicException ex) {
                    Console.WriteLine("ERROR : " + ex.Message);
                }
            }

            if(result)
                newLicense.Save(licenseFilePath, base64);

            return result;
        }
        private bool verifyLicense(string publicKeyFilePath, string licenseFilePath) {
            bool result;

            License licenseToVerify = License.Load(licenseFilePath);

            if (licenseToVerify == null)
                throw new ArgumentException("License file could not be read.");

            if (new FileInfo(publicKeyFilePath).Extension != ".crt") {
                string publicKey = File.ReadAllText(publicKeyFilePath);

                result = licenseToVerify.Verify(publicKey);
            } else {
                try {
                    X509Certificate2 certificate = new X509Certificate2(publicKeyFilePath);

                    result = licenseToVerify.Verify(certificate);
                } catch (CryptographicException ex) {
                    Console.WriteLine("ERROR : " + ex.Message);
                    result = false;
                }
            }

            return result;
        }
    }
}