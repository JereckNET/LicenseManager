using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JereckNET.LicenseManager.Sample {
    public struct Organization {
        public string Name;
        public string Country;
    }
    public class LicenseData {
        public string UserName;
        public bool IsIndividual;
        public Organization Organization;
        public DateTime ExpirationDate;

        public override string ToString() {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\t{UserName}");
            if (!IsIndividual) {
                sb.AppendLine($"\t{Organization.Name} ({Organization.Country})");
            }
            sb.AppendLine($"\tValid until : {ExpirationDate.ToShortDateString()}");

            return sb.ToString();
        }
    }
}
