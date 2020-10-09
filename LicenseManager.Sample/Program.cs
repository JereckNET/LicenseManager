using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace JereckNET.LicenseManager.Sample {
    class Program {
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable IDE0060 // Remove unused parameter
        static void Main(string[] args) {
            Console.WriteLine("Use case 1");
            sample1();

            Console.WriteLine("Use case 2");
            sample2();

            Console.WriteLine("Use case 3");
            sample3();
        }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Use case 1 : 
        ///     XML data
        ///     Signed with XML Public Key
        ///     Valid Signature
        ///     Saved as Base64
        ///     Read as byte[]
        /// </summary>
        private static void sample1() {
            bool status;
            License license;
            LicenseData data;

            Console.WriteLine("Loading \"License1.lic\" : ");
            license = License.Load(@"Licenses\License1.lic");
            status = license.Verify(Properties.Resources.LicensePublicKey);
            Console.WriteLine("Signature validity : " + status);

            if (status) {
                using (MemoryStream ms = new MemoryStream(license.Content)) {
                    using (XmlReader xr = XmlReader.Create(ms)) {
                        data = (LicenseData)new XmlSerializer(typeof(LicenseData)).Deserialize(xr);
                    }
                }

                Console.WriteLine("Product licensed to :");
                Console.WriteLine(data.ToString());
            }
        }

        /// <summary>
        /// Use case 2 : 
        ///     XML data
        ///     Signed with XML Public Key
        ///     Valid Signature
        ///     Saved as Base64
        ///     Read from <see cref="License.GetContentFromXML{T}"/>
        /// </summary>
        private static void sample2() {
            bool status;
            License license;
            LicenseData data;

            Console.WriteLine("Loading \"License1.lic\" : ");
            license = License.Load(@"Licenses\License1.lic");
            status = license.Verify(Properties.Resources.LicensePublicKey);
            Console.WriteLine("Signature validity : " + status);

            if (status) {
                data = license.GetContentFromXML<LicenseData>();

                Console.WriteLine("Product licensed to :");
                Console.WriteLine(data.ToString());
            }
        }

        /// <summary>
        /// Use case 3 : 
        ///     JSON data
        ///     Signed with PFX Public Key
        ///     Valid Signature
        ///     Saved as Base64
        ///     Read as string
        /// </summary>
        private static void sample3() {
            bool status;
            License license;

            Console.WriteLine("Loading \"License2.lic\" : ");
            license = License.Load(@"Licenses\License2.lic");
            using (X509Certificate2 certificate = new X509Certificate2(Properties.Resources.LicensePublicCertificateCRT)) {
                status = license.Verify(certificate);
            }

            Console.WriteLine("Signature validity : " + status);

            if (status) {

                string s = Encoding.UTF8.GetString(license.Content);

                Console.WriteLine("Product licensed to :");
                Console.WriteLine(s.ToString());
            }
        }
    }
}
