using System;
using System.IO;

namespace JereckNET.LicenseManager.Signer {
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

        public Arguments(params string[] args) {
            if (args.Length > 0) {
                switch (args[0]) {
                    case "/generateKeys":
                        //TODO KeySize
                        #region /generateKeys arguments
                        if (args.Length != 3) {
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

                        Operation = Operations.GenerateKey;
                        break;
                    #endregion

                    case "/sign":
                        #region /sign arguments
                        if (args.Length == 5) {
                            if (args[4] == "/base64") {
                                Base64 = true;
                            } else {
                                showInvalidSyntaxError();
                                break;
                            }
                        } else if (args.Length != 4) {
                            showInvalidSyntaxError();
                            break;
                        }

                        if (File.Exists(args[1])) {
                            PrivateKeyFilePath = args[1];
                        } else {
                            showError("Private key file does not exists");
                        }

                        if (File.Exists(args[2])) {
                            LicenseContentPath = args[2];
                        } else {
                            showError("License content file does not exists");
                        }

                        LicensePath = args[3];

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