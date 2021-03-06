﻿using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

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
                        generateKeys(_arguments.PublicKeyFilePath, _arguments.PrivateKeyFilePath, _arguments.KeySize);
                        Console.WriteLine("Keys created.");

                        break;

                    case Operations.Sign:
                        try {
                            result = signLicense(_arguments.PrivateKeyFilePath, _arguments.LicenseContentPath, _arguments.LicensePath, _arguments.Base64, _arguments.Algorithm, _arguments.ImportPassword);

                            if (result) {
                                Console.WriteLine("License content signed.");
                            } else {
                                Console.WriteLine("License content could not be signed.");
                            }
                        } catch (Exception ex) {
                            Console.WriteLine("ERROR : " + ex.Message);
                        }

                        break;

                    case Operations.Verify:
                        try {
                            result = verifyLicense(_arguments.PublicKeyFilePath, _arguments.LicensePath);

                            if (result) {
                                Console.WriteLine("The license signature is valid.");
                            } else {
                                Console.WriteLine("The license signature is not valid.");
                            }
                        } catch (Exception ex) {
                            Console.WriteLine("ERROR : " + ex.Message);
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
            Console.WriteLine($"{applicationName}.exe [{{ /generateKeys | /sign | /verify }} <options>]");
            Console.WriteLine("");

            Console.WriteLine("/generateKeys <Public Key Path> <Private Key Path> [/keySize:<Key Size>]");
            Console.WriteLine("\tGenerates a public/private keys pair and stores them in the specified files.");
            Console.WriteLine("");
            Console.WriteLine("\tPublic Key Path\t\tThe path of the file that will contain the generated public key.");
            Console.WriteLine("\tPrivate Key Path\tThe path of the file that will contain the generated private key.");
            Console.WriteLine("\tKey Size\t\tThe size of the keys. Must be a multiple of 8, between 384 and 16384 [Default: 2048].");
            Console.WriteLine("");

            Console.WriteLine("/sign <Private Key Path> <License Content Path> <License Path> [/algorithm:<Algorithm>] [/base64] [/password:<Password>]");
            Console.WriteLine("\tUses the specified private key to sign the license content and generates a signed license file.");
            Console.WriteLine("");
            Console.WriteLine("\tPrivate Key Path\tThe path of the file that contains the private key.");
            Console.WriteLine("\tLicense Content Path\tThe path of the file that contains the license content.");
            Console.WriteLine("\tLicense Path\t\tThe path of the file that will contain the signed license.");
            Console.WriteLine("\tAlgorithm\t\tThe algorithm to use for the signature process [Default: SHA256].");
            Console.WriteLine("\t\t\t\t  Accepted values are : SHA, SHA1, MD5, SHA256, SHA384, SHA512");
            Console.WriteLine("\t/base64\t\t\tGenerates a Base64-encoded file instead of an XML file.");
            Console.WriteLine("\tPassword\t\tThe password for the private key file.");
            Console.WriteLine("");

            Console.WriteLine("/sign <Certificate Thumbprint> <License Content Path> <License Path> [/algorithm:<Algorithm>] [/base64]");
            Console.WriteLine("\tUses the private key in the certificate store to sign the license content and generates a signed license file.");
            Console.WriteLine("");
            Console.WriteLine("\tCertificate Thumbprint\tThe thumbprint of the certificate containing the private key in the certificate store.");
            Console.WriteLine("\t\t\t\t  The programm will look first in the current user's store, then in the computer's.");
            Console.WriteLine("\t\t\t\t  The certificate key MUST be marked as exportable.");
            Console.WriteLine("\tLicense Content Path\tThe path of the file that contains the license content.");
            Console.WriteLine("\tLicense Path\t\tThe path of the file that will contain the signed license.");
            Console.WriteLine("\tAlgorithm\t\tThe algorithm to use for the signature process [Default: SHA256].");
            Console.WriteLine("\t\t\t\t  Accepted values are : SHA, SHA1, MD5, SHA256, SHA384, SHA512");
            Console.WriteLine("\t/base64\t\t\tGenerates a Base64-encoded file instead of an XML file.");
            Console.WriteLine("");

            Console.WriteLine("/verify <Public Key Path> <License Path>");
            Console.WriteLine("\tChecks the validity of a license file against a specified public key.");
            Console.WriteLine("");
            Console.WriteLine("\tPublic Key Path\t\tThe path of the file that contains the public key.");
            Console.WriteLine("\tLicense Path\t\tThe path of the file that contains the license to check.");
            Console.WriteLine("");
        }

        private void generateKeys(string publicKeyFilePath, string privateKeyFilePath, int keySize) {
            string publicKey;
            string privateKey;

            RSA rsa;
#if NETFRAMEWORK
            rsa = new RSACryptoServiceProvider(keySize);
#elif NETCOREAPP
            rsa = RSA.Create();
            rsa.KeySize = keySize;
#endif

            publicKey = rsa.ToXmlString(false);
            privateKey = rsa.ToXmlString(true);
            rsa.Dispose();

            using (StreamWriter sw = File.CreateText(publicKeyFilePath)) {
                sw.Write(publicKey);
                sw.Close();
            }
            using (StreamWriter sw = File.CreateText(privateKeyFilePath)) {
                sw.Write(privateKey);
                sw.Close();
            }
        }
        private bool signLicense(string privateKeyFilePath, string licenseContentPath, string licenseFilePath, bool base64, string algorithm, SecureString importPassword = null) {
            bool result;

            byte[] licenseContent = File.ReadAllBytes(licenseContentPath);

            License newLicense = new License(algorithm) {
                Content = licenseContent
            };

            if (Program.IsCertificateThumbprint(privateKeyFilePath)) {
                using (X509Certificate2 certificate = findClientCertificate(privateKeyFilePath)) {
                    if (certificate == null)
                        throw new FileNotFoundException("Certificate not found in certificate stores.", privateKeyFilePath);

                    result = newLicense.Sign(certificate);
                }

            } else if (new FileInfo(privateKeyFilePath).Extension != ".pfx") {

                string privateKey = File.ReadAllText(privateKeyFilePath);

                result = newLicense.Sign(privateKey);
            } else {
                if (importPassword == null) {
                    Console.Write("Please type the import password: ");
                    importPassword = Program.GetConsoleSecurePassword();
                }

                using (X509Certificate2 certificate = new X509Certificate2(privateKeyFilePath, importPassword,
                    X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet)) {

                    result = newLicense.Sign(certificate);
                }
            }

            if (result)
                newLicense.Save(licenseFilePath, base64);

            return result;
        }
        private bool verifyLicense(string publicKeyFilePath, string licenseFilePath) {
            bool result;

            License licenseToVerify = License.Load(licenseFilePath);

            if (licenseToVerify == null)
                throw new ArgumentException("License file could not be read.");

            if (new FileInfo(publicKeyFilePath).Extension == ".crt") {
                using (X509Certificate2 certificate = new X509Certificate2(publicKeyFilePath)) {
                    result = licenseToVerify.Verify(certificate);
                }

            }else if (new FileInfo(publicKeyFilePath).Extension == ".cer") {

                byte[] CERData = fromBase64Certificate(publicKeyFilePath);
                using (X509Certificate2 certificate = new X509Certificate2(CERData)) {
                    result = licenseToVerify.Verify(certificate);
                }

            } else {
                string publicKey = File.ReadAllText(publicKeyFilePath);

                result = licenseToVerify.Verify(publicKey);
            }

            return result;
        }

        private static byte[] fromBase64Certificate(string publicKeyFilePath) {
            string content = File.ReadAllText(publicKeyFilePath);

            StringBuilder base64Content = new StringBuilder();

            using (StringReader sr = new StringReader(content)) {
                string currentLine = sr.ReadLine();

                if (currentLine.StartsWith("-")) // Skip "-----BEGIN CERTIFICATE-----" line
                    currentLine = sr.ReadLine();

                while (currentLine != null && !currentLine.StartsWith("-")) {  // Skip "-----END CERTIFICATE-----" line
                    base64Content.Append(currentLine);

                    currentLine = sr.ReadLine();
                }

                string base64License = base64Content.ToString();
                byte[] data = Convert.FromBase64String(base64License);

                return data;
            }
        }
        private static X509Certificate2 findClientCertificate(string thumbprint) {
            return findClientCertificate(thumbprint, StoreLocation.CurrentUser) ?? findClientCertificate(thumbprint, StoreLocation.LocalMachine);
        }
        private static X509Certificate2 findClientCertificate(string thumbprint, StoreLocation location) {
            using (X509Store store = new X509Store(location)) {
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection certs = store.Certificates.Find(
                    X509FindType.FindByThumbprint, thumbprint, false);

                store.Close();

                if (certs.Count == 0) {
                    return null;
                } else {
                    return certs[0];
                }
            }
        }
    }
}