# ![License Manager Logo](images/Manager.png) License Manager

License Manager is a tool that allows developpers to easily create software license files with a digital signature.

* [Installation](#installation)
  * [To use signed license files in your application](#to-use-signed-license-files-in-your-application)
  * [To create and sign license files](#to-create-and-sign-license-files)
* [User Guide](#user-guide)
  * [With XML keys](#with-xml-keys)
  * [With X.509 Certificates](#with-x.509-certificates)
* [Samples](#samples)
* [License](#license)

## Installation
### To use signed license files in your application
| Component                     | Package                                                                                                                                              |
|-------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|
| `JereckNET.LicenseManager` | [![LicenseHerald NuGet Package](https://img.shields.io/nuget/v/JereckNET.LicenseManager.svg)](https://www.nuget.org/packages/JereckNET.LicenseHerald) |

Install License Manager by searching for 'LicenseManager'  in the NuGet package manager, or using the Package Manager Console:

```
PM > Install-Package JereckNET.LicenseManager
```

### To create and sign license files
Download the [latest release](https://github.com/JereckNET/LicenseManager/releases) and unzip the file in your preferred directory.

## User Guide
### With XML keys
1. Generate signing keys :
```
LicenseManager.exe /generateKeys Public_Key.xml Private_Key.xml
```
2. Store `Private_Key.xml`in a secure location.
3. Include the content of `Public_Key.xml` within your application (hard-coded, as resource, ...).
4. Generate the license content as any text-based format you want (CSV, XML, JSON, Base64, ...) and store it in a distinct file.
5. Sign the content with your private key :
```
LicenseManager.exe /sign Private_Key.xml License_Content.dat License.lic
```
6. Load the content of the signed license in your application and validate it :
```csharp
private static bool verifyLicense(string _licenseFilePath) {
    string publicKey = Properties.Resources.PublicKey;

    License licenseToVerify = License.Load(_licenseFilePath);

    bool? result = licenseToVerify?.Verify(publicKey);

    return result ?? false;
}
```

The way to generate the license content, how to transfer request and license between you and your customer, and what to do in your application with or without license is up to you.

### With X.509 Certificates
 In the [to-do list](https://github.com/JereckNET/LicenseManager/issues/2) ...

## Samples
In the [to-do list](https://github.com/JereckNET/LicenseManager/issues/3) ...
## License

License Manager is licensed under the MIT License - the details are at [LICENSE.md](LICENSE.md)