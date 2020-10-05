using System.Xml.Serialization;

namespace JereckNET.LicenseManager {
    public class Signature {
        
        [XmlText]
        public byte[] Content { 
            get;
            set;
        }

        [XmlAttribute("algorithm")]
        public string Algorithm {
            get;
            set;
        }

        public Signature() {
            Algorithm = "SHA256";
        }
    }
}
