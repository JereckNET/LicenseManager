using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace JereckNET.LicenseManager {
    /// <summary>
    /// Represents a signable licence file or <see cref="Stream"/>.
    /// </summary>
    [XmlRoot("license")]
    public class License {
        private const string START_OF_FILE = "-----BEGIN LICENSE-----";
        private const string END_OF_FILE = "-----END LICENSE-----";

        /// <summary>
        /// Creates a new <see cref="License"/> instance with SHA256 signature algorithm.
        /// </summary>
        public License(){
            Signature = new Signature("SHA256");
        }
        /// <summary>
        /// Creates a new <see cref="License"/> instance.
        /// </summary>
        /// <param name="Algorithm">The algorithm used to sign licenses. <br />Supported values are : SHA, SHA1, MD5, SHA256, SHA384, SHA512</param>
        /// <remarks>Supported algoritms are provided by <see cref="HashAlgorithm"/> and the list could be expanded by the framework.</remarks>
        public License(string Algorithm) {
            Signature = new Signature(Algorithm);
        }

        #region Properties
        /// <summary>
        /// The license payload as a byte array.
        /// </summary>
        [XmlElement("content")]
        public byte[] Content {
            get;
            set;
        }

        /// <summary>
        /// The license <see cref="Signature"/>.
        /// </summary>
        [XmlElement("signature")]
        public Signature Signature {
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
            return Save(FilePath, out _, Base64Encode);
        }
        /// <summary>
        /// Saves a signed license file to the file system.
        /// </summary>
        /// <param name="FilePath">The path of the signed license file.</param>
        /// <param name="Error">The exception that prevented the file to be saved.<br />Will be <see langword="null"/> if the function returns <see langword="true"/>.</param>
        /// <param name="Base64Encode"><see langword="true"/> to generate a Base64 encoded file.</param>
        /// <returns><see langword="true"/> if the license was saved successfully.</returns>
        public bool Save(string FilePath, out Exception Error, bool Base64Encode = false) {
            bool result;
            try {
                using (FileStream fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write)) {
                    result = Save(fs, out Error, Base64Encode);
                }

            } catch (Exception ex) {
                Debugger.Log(1, "Save", ex.Message);
                Error = ex;
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
            return Save(Destination, out _, Base64Encode);
        }
        /// <summary>
        /// Saves a signed license file to a stream.
        /// </summary>
        /// <param name="Destination">The <see cref="Stream"/> for the signed license.</param>
        /// <param name="Error">The exception that prevented the <see cref="Stream"/> to be saved.<br />Will be <see langword="null"/> if the function returns <see langword="true"/>.</param>
        /// <param name="Base64Encode"><see langword="true"/> to generate a Base64 encoded content.</param>
        /// <returns><see langword="true"/> if the license was saved successfully.</returns>
        public bool Save(Stream Destination, out Exception Error, bool Base64Encode = false) {
            Error = null;
            bool result;
            string content;

            try {
                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces();
                namespaces.Add("", "");

                StringBuilder buffer = new StringBuilder();
                XmlWriterSettings settings = new XmlWriterSettings {
                    Indent = true
                };

                using (XmlWriter writer = XmlWriter.Create(buffer, settings)) {
                    new XmlSerializer(typeof(License)).Serialize(writer, this, namespaces);
                }

                content = buffer.ToString();

                if (Base64Encode) {
                    content = base64Encode(content);
                }

                using (StreamWriter streamWriter = new StreamWriter(Destination, Encoding.UTF8)) {
                    streamWriter.Write(content);
                }
                result = true;

            } catch (Exception ex) {
                Debugger.Log(1, "Save", ex.Message);
                Error = ex;
                result = false;
            }

            return result;
        }

        #endregion

        #region Verify()
        /// <summary>
        /// Checks the validity of the signature.
        /// </summary>
        /// <param name="PublicKey">The XML encoded public key for your application license.<br />This key can be distributed freely.</param>
        /// <param name="KeySize">The key size used to sign the content.<br/>Defaults to : <strong>2048</strong></param>
        /// <returns><see langword="true"/> if the <see cref="Signature"/> corresponds to the <see cref="Content"/>.</returns>
        public bool Verify(string PublicKey, int KeySize = 2048) {
            return Verify(PublicKey, out _, KeySize);
        }
        /// <summary>
        /// Checks the validity of the signature.
        /// </summary>
        /// <param name="PublicKey">The XML encoded public key for your application license.<br />This key can be distributed freely.</param>
        /// <param name="Error">The exception that prevented the signature to be verified.<br />Will be <see langword="null"/> if the function returns <see langword="true"/>.</param>
        /// <param name="KeySize">The key size used to sign the content.<br/>Defaults to : <strong>2048</strong></param>
        /// <returns><see langword="true"/> if the <see cref="Signature"/> corresponds to the <see cref="Content"/>.</returns>
        public bool Verify(string PublicKey, out Exception Error, int KeySize = 2048) {
            Error = null;
            bool result;

            try {
                using (RSACryptoServiceProvider csp = new RSACryptoServiceProvider(KeySize)) {
                    csp.FromXmlString(PublicKey);

                    result = csp.VerifyData(Content, Signature.Algorithm, Signature.Content);
                }

            } catch (Exception ex) {
                Debugger.Log(1, "Verify", ex.Message);
                Error = ex;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Checks the validity of the signature.
        /// </summary>
        /// <param name="Certificate">The <see cref="X509Certificate2"/> containing the public key used to sign your application license.<br />This certificate can be distributed freely assuming it does not contains the private key.</param>
        /// <returns><see langword="true"/> if the <see cref="Signature"/> corresponds to the <see cref="Content"/>.</returns>
        public bool Verify(X509Certificate2 Certificate) {
            return Verify(Certificate, out _);
        }
        /// <summary>
        /// Checks the validity of the signature.
        /// </summary>
        /// <param name="Certificate">The <see cref="X509Certificate2"/> containing the public key used to sign your application license.<br />This certificate can be distributed freely assuming it does not contains the private key.</param>
        /// <param name="Error">The exception that prevented the signature to be verified.<br />Will be <see langword="null"/> if the function returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> if the <see cref="Signature"/> corresponds to the <see cref="Content"/>.</returns>
        public bool Verify(X509Certificate2 Certificate, out Exception Error) {
            bool result;
            Error = null;

            try {
                using (RSACryptoServiceProvider csp = (RSACryptoServiceProvider)Certificate.PublicKey.Key) {
                    result = csp.VerifyData(Content, Signature.Algorithm, Signature.Content);
                }

            } catch (Exception ex) {
                Debugger.Log(1, "Verify", ex.Message);
                Error = ex;
                result = false;
            }

            return result;
        }
        #endregion

        #region Sign()
        /// <summary>
        /// Generate the license signature and stores it in the <see cref="Signature"/> property.
        /// </summary>
        /// <param name="PrivateKey">The XML encoded private key for your application license.<br />This key <strong>MUST NEVER</strong> be distributed.</param>
        /// <returns><see langword="true"/> if the signature was successfully created.</returns>
        public bool Sign(string PrivateKey) {
            return Sign(PrivateKey, out _);
        }
        /// <summary>
        /// Generate the license signature and stores it in the <see cref="Signature"/> property.
        /// </summary>
        /// <param name="PrivateKey">The XML encoded private key for your application license.<br />This key <strong>MUST NEVER</strong> be distributed.</param>
        /// <param name="Error">The exception that prevented the payload to be signed.<br />Will be <see langword="null"/> if the function returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> if the signature was successfully created.</returns>
        public bool Sign(string PrivateKey, out Exception Error) {
            Error = null;
            bool result;

            try {
                using (RSACryptoServiceProvider csp = new RSACryptoServiceProvider()) {
                    csp.FromXmlString(PrivateKey);

                    Signature.Content = csp.SignData(Content, Signature.Algorithm);

                    result = true;
                }
            } catch (Exception ex) {
                Debugger.Log(1, "Sign", ex.Message);
                Error = ex;
                Signature = null;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Generate the license signature and stores it in the <see cref="Signature"/> property.
        /// </summary>
        /// <param name="Certificate">The <see cref="X509Certificate2"/> containing the private key used to sign your application license.<br />This certificate <strong>MUST NEVER</strong> be distributed.</param>
        /// <param name="Error">The exception that prevented the payload to be signed.<br />Will be <see langword="null"/> if the function returns <see langword="true"/>.</param>
        /// <returns><see langword="true"/> if the signature was successfully created.</returns>
        /// <exception cref="ArgumentException">Thrown if the <see cref="X509Certificate2"/> does not contains a private key.</exception>
        public bool Sign(X509Certificate2 Certificate) {
            return Sign(Certificate, out _);
        }
        /// <summary>
        /// Generate the license signature and stores it in the <see cref="Signature"/> property.
        /// </summary>
        /// <param name="Certificate">The <see cref="X509Certificate2"/> containing the private key used to sign your application license.<br />This certificate <strong>MUST NEVER</strong> be distributed.</param>
        /// <returns><see langword="true"/> if the signature was successfully created.</returns>
        /// <exception cref="ArgumentException">Thrown if the <see cref="X509Certificate2"/> does not contains a private key.</exception>
        public bool Sign(X509Certificate2 Certificate, out Exception Error) {
            if (!Certificate.HasPrivateKey)
                throw new ArgumentException("The certificate must contain a private key", nameof(Certificate));

            RSA privateKey = Certificate.GetRSAPrivateKey();
            string privateKeyXml = privateKey.ToXmlString(true);

            return Sign(privateKeyXml, out Error);
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

        #region Content conversion
        /// <summary>
        /// Assumes that content data is XML and deserialize it as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The license content type defined in the client application.</typeparam>
        /// <returns>The license data deserialized as <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidCastException">Thrown if license data is not an XML serialization of <typeparamref name="T"/>.</exception>
        public T GetContentFromXML<T>() {
            try {
                using (MemoryStream ms = new MemoryStream(Content)) {
                    T data;

                    using (XmlReader xr = XmlReader.Create(ms)) {
                        data = (T)new XmlSerializer(typeof(T)).Deserialize(xr);
                    }

                    return data;
                }
            }catch(Exception ex) {
                throw new InvalidCastException($"License data could not be casted as {typeof(T)}", ex);
            }
        }
        #endregion
    }
}
