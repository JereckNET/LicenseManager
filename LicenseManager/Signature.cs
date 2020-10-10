using System.Security.Cryptography;
using System.Xml.Serialization;

namespace JereckNET.LicenseManager {
    /// <summary>
    /// Represents the signature of a <see cref="License"/>.
    /// </summary>
    public class Signature {
        private string _algorithm;
        private HashAlgorithmName _algorithmName;

        /// <summary>
        /// The signature data.
        /// </summary>
        [XmlText]
        public byte[] Content { 
            get;
            set;
        }

        /// <summary>
        /// The algorithm used to generate the signature data.
        /// </summary>
        [XmlAttribute("algorithm")]
        public string Algorithm {
            get {
                return _algorithm;
            }
            set {
                _algorithm = value;
                _algorithmName = new HashAlgorithmName(Algorithm);
            }
        }

        /// <summary>
        /// The algorithm used to generate the signature data.
        /// </summary>
        [XmlIgnore]
        public HashAlgorithmName AlgorithmName {
            get {
                return _algorithmName;
            }
        }

        /// <summary>
        /// Creates en ampty <see cref="Signature"/> using the SHA-256 signature algorithm.
        /// </summary>
        public Signature() {
            _algorithm = "SHA256";
            _algorithmName = HashAlgorithmName.SHA256;
        }
        /// <summary>
        /// Creates en ampty <see cref="Signature"/> using the requested signature algoritm.
        /// </summary>
        /// <param name="AlgorithmName">The <see cref="HashAlgorithmName"/> used as the signature algorithm.</param>
        public Signature(string AlgorithmName) {
            _algorithm = AlgorithmName;
            _algorithmName = new HashAlgorithmName(AlgorithmName);
        }
    }
}
