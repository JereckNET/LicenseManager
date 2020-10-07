using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security;

namespace JereckNET.LicenseManager.Signer {
    /// <summary>
    /// Parses the application's command-line arguments.
    /// </summary>
    /// <remarks>Could certainly be optimized and/or more flexible but it is not the main concern for this application.</remarks>
    internal class Arguments {
        public Operations Operation {
            get;
            private set;
        } = Operations.ShowHelp;
        public bool ShowHelp {
            get;
            private set;
        } = false;
        public bool HasError {
            get;
            private set;
        } = false;

        public string PublicKeyFilePath {
            get;
            private set;
        }
        public string PrivateKeyFilePath {
            get;
            private set;
        }
        public string LicenseContentPath {
            get;
            private set;
        }
        public string LicensePath {
            get;
            private set;
        }
        public bool Base64 {
            get;
            private set;
        } = false;
        public int KeySize {
            get;
            private set;
        } = 2048;
        public string Algorithm {
            get;
            private set;
        } = "SHA256";
        public SecureString ImportPassword {
            get;
            internal set;
        }

        public Arguments(params string[] args) {
            if (args.Length > 0) {
                switch (args[0]) {
                    case "/generateKeys":
                        #region /generateKeys arguments
                        if (args.Length < 3 && args.Length > 4) {
                            showInvalidSyntaxError();
                            break;
                        }

                        if (!File.Exists(args[1])) {
                            PublicKeyFilePath = args[1];
                        } else {
                            showError("Public key file already exists");
                        }

                        if (!File.Exists(args[2])) {
                            PrivateKeyFilePath = args[2];
                        } else {
                            showError("Private key file already exists");
                        }

                        parseOptionalArguments(args, 3);

                        Operation = Operations.GenerateKey;
                        break;
                    #endregion

                    case "/sign":
                        #region /sign arguments
                        if (args.Length < 4 && args.Length > 7) {
                            showInvalidSyntaxError();
                            break;
                        }

                        if(Program.IsCertificateThumbprint(args[1])) {
                            PrivateKeyFilePath = args[1];
                        } else {
                            if (File.Exists(args[1])) {
                                PrivateKeyFilePath = args[1];
                            } else {
                                showError("Private key file does not exists");
                            }
                        }

                        if (File.Exists(args[2])) {
                            LicenseContentPath = args[2];
                        } else {
                            showError("License content file does not exists");
                        }

                        LicensePath = args[3];

                        parseOptionalArguments(args, 4);

                        Operation = Operations.Sign;
                        break;
                    #endregion

                    case "/verify":
                        #region /verify arguments
                        if (args.Length != 3) {
                            showInvalidSyntaxError();
                            break;
                        }

                        if (File.Exists(args[1])) {
                            PublicKeyFilePath = args[1];
                        } else {
                            showError("Public key file does not exists");
                        }

                        if (File.Exists(args[2])) {
                            LicensePath = args[2];
                        } else {
                            showError("License file does not exists");
                        }

                        Operation = Operations.Verify;
                        break;
                    #endregion

                    case "/help":
                        #region /help arguments
                        if (args.Length != 1) {
                            showInvalidSyntaxError();
                            break;
                        }

                        Operation = Operations.ShowHelp;
                        break;
                    #endregion

                    default:
                        #region Others
                        showInvalidSyntaxError();

                        break;
                        #endregion
                }
            }

            if (ShowHelp) {
                Console.WriteLine();
            }
        }

        private void parseOptionalArguments(string[] args, int startIndex) {
            for (int n = startIndex; n <= args.Length - 1; n++) {
                if (args[n].ToLower() == "/base64") {
                    Base64 = true;

                } else if (args[n].ToLower().StartsWith("/algorithm:")) {
                    Algorithm = args[n].Split(':')[1];

                    if (!new[] { "SHA", "SHA1", "MD5", "SHA256", "SHA384", "SHA512" }.Contains(Algorithm)) {
                        showInvalidSyntaxError();
                        break;
                    }

                } else if (args[n].ToLower().StartsWith("/password:")) {
                    ImportPassword = new SecureString();
                    args[n].Split(':')[1].ToCharArray().ToList().ForEach(ImportPassword.AppendChar);
                    ImportPassword.MakeReadOnly();


                } else if (args[n].ToLower().StartsWith("/keysize:")) {
                    if (int.TryParse(args[n].Split(':')[1], out int keySize)) {
                        if (keySize < 384 || keySize > 16384 || keySize % 8 != 0) {
                            showError("Key size must be a multiple of 8, between 384 and 16384");
                        } else {
                            KeySize = keySize;
                        }
                    } else {
                        showError("Key size must be an integer");
                    }

                } else {
                    showInvalidSyntaxError();
                    break;
                }
            }
        }

        private void showInvalidSyntaxError() {
            ShowHelp = true;
            showError("Invalid syntax");
        }
        private void showError(string message) {
            HasError = true;
            Console.WriteLine("ERROR : " + message);
        }
    }
}