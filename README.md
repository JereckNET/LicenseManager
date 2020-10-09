# ![License Manager Logo](images/Manager.png) License Manager

License Manager is a tool that allows developers to easily create software license files with a digital signature.

* [Installation](#installation)
  * [To use signed license files in your application](#to-use-signed-license-files-in-your-application)
  * [To create and sign license files](#to-create-and-sign-license-files)
* [User Guide](#user-guide)
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
See [USAGE](USAGE.md) file.

## Samples
The easiest way is to use a XML public key and XML payload :
```csharp
License license = License.Load(@"Licenses\License1.lic");
bool status = license.Verify(Properties.Resources.LicensePublicKey);
if (status) {
    LicenseData data = license.GetContentFromXML<LicenseData>();
    // Do your own thing
}
```

Additional use cases are available in the [LicenseManager.Sample](/LicenseManager.Sample/) project.

## License

License Manager is licensed under the MIT License - the details are at [LICENSE.md](LICENSE.md)
