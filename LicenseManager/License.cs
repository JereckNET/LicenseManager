using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace JereckNET.LicenseManager {
    public class License {
        private const string START_OF_FILE =    "-----BEGIN LICENSE-----";
        private const string END_OF_FILE =      "-----END LICENSE-----";

        #region Properties
        /// <summary>
        /// The license payload. Can be any serializable content (Text, XML, Json, Base64, ...)
        /// </summary>
        public string Content {
            get;
            set;
        }

        /// <summary>
        /// The license signature.
        /// </summary>
        public byte[] Signature {
            get;
            set;
        }
        #endregion

        #region Load()
        /// <summary>
        /// Loads a signed license file from the file system.
        /// </summary>
        /// <param name="FilePath">The path of the license file.</param>
        /// <returns>The <see cref="License"/> object corresponding to the file loaded or <see langword="null"/> if the file does not exists or if there is an error reading it.</returns>
        public static License Load(string FilePath) {
            License result;
            try {
                if (!File.Exists(FilePath)) {
                    result = null;
                } else {
                    using (FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read)) {
                        result = Load(fs);
                    }
                }
            } catch (Exception ex) {
                Debugger.Log(1, "Load", ex.Message);
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Loads a signed license file from a stream.
        /// </summary>
        /// <param name="Source">The <see cref="Stream"/> of the license.</param>
        /// <returns>The <see cref="License"/> object corresponding to the stream loaded or <see langword="null"/> if there is an error reading the License.</returns>
        public static License Load(Stream Source) {
            License result;
            try {
                string streamContent;

                using (StreamReader sr = new StreamReader(Source, Encoding.UTF8)) {
                    streamContent = sr.ReadToEnd();
                }

                if (streamContent.Length == 0)
                    throw new EndOfStreamException("Stream is empty");

                char first = streamContent[0];

                if (first == '-') {
                    streamContent = base64Decode(streamContent);
                }

                using (XmlReader xtr = XmlReader.Create(new StringReader(streamContent))) {
                    result = (License)new XmlSerializer(typeof(License)).Deserialize(xtr);

                }
            } catch (Exception ex) {
                Debugger.Log(1, "Load", ex.Message);
                result = null;
            }
            return result;
        }

        #endregion

        #region Save()
        /// <summary>
        /// Saves a signed license file to the file system.
        /// </summary>
        /// <param name="FilePath">The path of the signed license file.</param>
        /// <param name="Base64Encode"><see langword="true"/> to generate a Base64 encoded file.</param>
        /// <returns><see langword="true"/> if the license was saved successfully.</returns>
        public bool Save(string FilePath, bool Base64Encode = false) {
            bool result;
            try {
                using (FileStream fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write)) {
                    result = Save(fs, Base64Encode);
                }

            } catch (Exception ex) {
                Debugger.Log(1, "Save", ex.Message);
                result = false;
            }
            return result;
        }

        /// <summary>
        /// Saves a signed license file to a stream.
        /// </summary>
        /// <param name="Destination">The <see cref="Stream"/> for the signed license.</param>
        /// <param name="Base64Encode"><see langword="true"/> to generate a Base64 encoded content.</param>
        /// <returns><see langword="true"/> if the license was saved successfully.</returns>
        public bool Save(Stream Destination, bool Base64Encode = false) {
            bool result;
            string content;

            try {
                using (StringWriter sw = new StringWriter()) {
                    using (XmlTextWriter xmlTextWriter = new XmlTextWriter(sw)) {
                        new XmlSerializer(typeof(License)).Serialize(xmlTextWriter, this);
                    }

                    content = sw.ToString();
                }

                if (Base64Encode) {
                    content = base64Encode(content);
                }

                using (StreamWriter streamWriter = new StreamWriter(Destination, Encoding.UTF8)) {
                    streamWriter.Write(content);
                }
                result = true;

            } catch (Exception ex) {
                Debugger.Log(1, "Save", ex.Message);
                result = false;
            }
            return result;
        }

        #endregion

        #region Verify()
        /// <summary>
        /// Checks the validity of the signature
        /// </summary>
        /// <param name="PublicKey">The XML encoded public key for your application license.<br />This key can be distributed freely.</param>
        /// <param name="KeySize">The key size used to sign the content.<br/>Default to : <strong>2048</strong></param>
        /// <returns><see langword="true"/> if the <see cref="Signature"/> corresponds to the <see cref="Content"/>. </returns>
        public bool Verify(string PublicKey, int KeySize = 2048) {
            bool result = false;
            try {
                using (RSACryptoServiceProvider csp = new RSACryptoServiceProvider(KeySize)) {
                    csp.FromXmlString(PublicKey);

                    result = Verify(csp);
                }
            } catch (Exception ex) {
                Debugger.Log(1, "Verify", ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Checks the validity of the signature
        /// </summary>
        /// <param name="PublicKey">The public key for your application license, as extracted from an X.509 Certificate.<br />This key can be distributed freely.</param>
        /// <returns></returns>
        public bool Verify(AsymmetricAlgorithm PublicKey) {
            bool result = false;
            try {
                using (RSACryptoServiceProvider csp = (RSACryptoServiceProvider)PublicKey) {
                    result = Verify(csp);
                }

            } catch (Exception ex) {
                Debugger.Log(1, "Verify", ex.Message);
            }
            return result;
        }

        /// <summary>
        /// Checks the validity of the signature
        /// </summary>
        /// <param name="csp">The <see cref="RSACryptoServiceProvider"/> used to sign the license.</param>
        /// <returns><see langword="true"/> if the <see cref="Signature"/> corresponds to the <see cref="Content"/>. </returns>
        private bool Verify(RSACryptoServiceProvider csp) {
            bool result;
            try {

                byte[] data = Encoding.UTF8.GetBytes(Content);
                result = csp.VerifyData(data, SHA256.Create(), Signature);
            } catch (Exception ex) {
                Debugger.Log(1, "Verify", ex.Message);
                result = false;
            }
            return result;
        }
        #endregion

        #region Sign()
        /// <summary>
        /// Generate the license signature and stores it in the <see cref="Signature"/> property.
        /// </summary>
        /// <param name="PrivateKey">The XML encoded private key for your application license. <br />This key <strong>MUST NEVER</strong> be distributed.</param>
        /// <param name="KeySize">The key size used to sign the content.<br/>Default to : <strong>2048</strong></param>
        public void Sign(string PrivateKey, int KeySize = 2048) {
            try {
                using (RSACryptoServiceProvider csp = new RSACryptoServiceProvider(KeySize)) {
                    csp.FromXmlString(PrivateKey);

                    Sign(csp);
                }
            } catch (Exception ex) {
                Debugger.Log(1, "Sign", ex.Message);
                this.Signature = null;
            }
        }

        /// <summary>
        /// Generate the license signature and stores it in the <see cref="Signature"/> property.
        /// </summary>
        /// <param name="PrivateKey">The private key for your application license, as extracted from an X.509 Certificate. <br />This key <strong>MUST NEVER</strong> be distributed.</param>
        public void Sign(AsymmetricAlgorithm PrivateKey) {
            try {
                using (RSACryptoServiceProvider csp = (RSACryptoServiceProvider)PrivateKey) {
                    Sign(csp);
                }
            } catch (Exception ex) {
                Debugger.Log(1, "Sign", ex.Message);
                this.Signature = null;
            }
        }

        /// <summary>
        /// Generate the license signature and stores it in the <see cref="Signature"/> property.
        /// </summary>
        /// <param name="csp">The <see cref="RSACryptoServiceProvider"/> used to sign the license.</param>
        private void Sign(RSACryptoServiceProvider csp) {
            try {
                byte[] data = Encoding.UTF8.GetBytes(this.Content);

                byte[] signature = csp.SignData(data, SHA256.Create());

                Signature = signature;
            } catch (Exception ex) {
                Debugger.Log(1, "Sign", ex.Message);
                this.Signature = null;
            }
        }
        #endregion

        #region Base64
        private static string base64Encode(string content) {
            StringBuilder licenseBuilder = new StringBuilder();
            byte[] licenseData = Encoding.UTF8.GetBytes(content);

            licenseBuilder.AppendLine(START_OF_FILE);

            string base64License = Convert.ToBase64String(licenseData);
            string[] fixedLenghtLines = Regex.Matches(base64License, ".{1,64}").Cast<Match>().Select(m => m.Value).ToArray();

            foreach (string line in fixedLenghtLines) {
                licenseBuilder.AppendLine(line);
            }

            licenseBuilder.AppendLine(END_OF_FILE);

            content = licenseBuilder.ToString();
            return content;
        }
        private static string base64Decode(string streamContent) {
            using (StringReader sr = new StringReader(streamContent)) {
                string currentLine = sr.ReadLine();

                if (currentLine != START_OF_FILE)
                    throw new FormatException("License stream format is not recognized");

                StringBuilder base64Content = new StringBuilder();

                currentLine = sr.ReadLine();

                while (!currentLine.Equals(END_OF_FILE)) {
                    base64Content.Append(currentLine);

                    currentLine = sr.ReadLine();

                    if (currentLine == null)
                        throw new FormatException("License stream format is not recognized");
                }

                string base64License = base64Content.ToString();
                byte[] data = Convert.FromBase64String(base64License);

                return Encoding.UTF8.GetString(data);
            }
        }
        #endregion
    }
}
