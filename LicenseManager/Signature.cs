using System.Security.Cryptography;
using System.Xml.Serialization;

namespace JereckNET.LicenseManager {
    /// <summary>
    /// Represents the signature of a <see cref="License"/>.
    /// </summary>
    public class Signature {
        private string _algorithmName;
        private HashAlgorithm _algorithm;

        /// <summary>
        /// The signature data.
        /// </summary>
        [XmlText]
        public byte[] Content { 
            get;
            set;
        }

        /// <summary>
        /// The name of the algorithm used to generate the signature data.
        /// </summary>
        [XmlAttribute("algorithm")]
        public string AlgorithmName {
            get {
                return _algorithmName;
            }
            set {
                _algorithmName = value;
                _algorithm = HashAlgorithm.Create(value);
            }
        }

        /// <summary>
        /// The algorithm used to generate the signature data.
        /// </summary>
        [XmlIgnore]
        public HashAlgorithm Algorithm {
            get {
                return _algorithm;
            }
        }

        /// <summary>
        /// Creates en ampty <see cref="Signature"/> using the SHA-256 signature algorithm.
        /// </summary>
        public Signature() {
            _algorithmName = "SHA256";
            _algorithm = SHA256.Create();
        }
        /// <summary>
        /// Creates en ampty <see cref="Signature"/> using the requested signature algoritm.
        /// </summary>
        /// <param name="AlgorithmName">The name of the <see cref="HashAlgorithm"/> used as the signature algorithm.</param>
        public Signature(string AlgorithmName) {
            _algorithmName = AlgorithmName;
            _algorithm = HashAlgorithm.Create(AlgorithmName);
        }
    }
}
