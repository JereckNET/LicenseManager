# User Guide
## Table of contents
* [Introduction]
* [Generating the keys]
  * [XML keys]
  * [X.509 Certificates]
* [Signing a license]
  * [Using XML Keys]
  * [Using a PFX file]
  * [Using the certificate store]
* [Validating the license]
  * [In the console]
  * [In your application]
* [Complete usage]

## Introduction
The public and private keys used to sign and validate the licenses come in different flavors.

|            Type                      |          Pros                |           Cons               |
| ------------------------------------ | ---------------------------- | ---------------------------- |
| Pure XML                             | Easier to generate           | Non-standard, less secure¹   |
| CRT (public) & PFX  (Private)        | More secure², standardized³  | Harder to generate           |
| CRT (public) & Local Store (Private) | Same as above                | Harder to move the private key between computers |

¹ The security of the private key relies only in keeping it private.

² The private key is password protected, can be limited in time.

³ Existing certificates (Code signing, "SSL", ...) can be re-used.


## Generating the keys
### XML keys
1. To generate the XML keys, you will use the _Licence Manager_ application :
```
LicenseManager.exe /generateKeys "Public Key.xml" "Private Key.xml"
```
2. You will then need to store `Private Key.xml`in a secure location.

3. You should include the content of `Public Key.xml` within your application (hard-coded, as resource, ...).
    <br />**NB:** If you only put the `Public Key.xml` in the application folder, a malicious end-user could simply replace it with its own to sign the license file himselft.

### X.509 Certificates
To request a certificate from a Certification Authority, you will need to generate a [certificate request](https://en.wikipedia.org/wiki/Certificate_signing_request) which is outside of the scope of this guide.

You can also create self-signed certificates with PowerShell :
1. Generate the self-signed certificate:
    ```powershell
    $cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -subject "License Manager" -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(10) -Type Custom -KeySpec Signature
    ```
    I have tested the application with `Custom`, `DocumentEncryptionCert` and `CodeSigningCert` types.
    You can use any string for the subject field, it is not used by the application. 

2. Export the public certificate and include it within your application.
    ```powershell
    Export-Certificate -Cert $cert -FilePath "License Manager.crt" 
    ```
    **NB:** If you only put the `License Manager.crt` in the application folder, a malicious end-user could simply replace it with its own to sign the license file himselft.

The private key is stored in the current user's local certificate store. 
Note the certificate thumbprint : 
```powershell
Write-Host $cert.Thumbprint
```
**NB:** To use the certificate store, make sure you use the ```-KeySpec Signature``` flag when creating the certificate.

To use a separate, password-protected, PFX file, you have to export the private key :
```powershell
$password = ConvertTo-SecureString -String "1234" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "License Manager.pfx"  -Password $password
```
**NB:** I would recommend the use of a different password ;-)

## Signing a license
First, you have to generate a "license payload" file. This file should contain any data needed in your application to identify a licensed user (name, company, serial number, ...).
The content of this file is specific to your application and is outside of the scope of this guide.

When the payload is generated, you will sign it with your private key before sending it to the licensed end-user.

The signed license file can either be a pure XML-file, or a Base64 encoded one.

### Using XML Keys
```
LicenseManager.exe /sign "Private Key.xml" "License Content.dat" "License.lic"
```

### Using a PFX file
```
LicenseManager.exe /sign "License Manager.pfx" "License Content.dat" "License.lic"
```
The application will require you to type in the PFX password (don't worry if nothing appears on screen as you type, it is by design).

You can also pass the password as a command-line argument. It's easier to automate the process, but less secure as the password is passed as clear text.

### Using the certificate store
You will need to provide the certificate thumbprint that you copied earlier.
```
LicenseManager.exe /sign "60B5E67F7F8DFB65677289B6334268A141139EDB" "License Content.dat" "License.lic"
```
No password is required as the local certificate store is protected by your session login.


## Validating the license
Once the license is signed, you will need to verify its validity.
You can check it with the console application, and, of course, from inside your own application.

### In the console
Check the license file against you public key, either as XML of as X.509 (.crt)
```
LicenseManager.exe /verify "Public Key.xml" "License.lic"
```

### In your application
Install the client component (see [Installation](README.md#Installation)), then load your public key and the license file.
```csharp
private bool verifyLicense(string _licenseFilePath) {
    string publicKey = Properties.Resources.PublicKey;

    License licenseToVerify = License.Load(_licenseFilePath);
    //TODO Add your exception handling if licenseToVerify is null

    bool result = licenseToVerify.Verify(publicKey);
    
    return result;
}
```
What to do in your application afterwards is up to you.



## Complete usage
When started without arguments (or with incorrect arguments), the application will show the complete arguments reference :

```
License Manager.exe [{ /generateKeys | /signLicense | /verifyLicense } <options>]

/generateKeys <Public Key Path> <Private Key Path> [/keySize:<Key Size>]
        Generates a public/private keys pair and stores them in the specified files.

        Public Key Path         The path of the file that will contain the generated public key.
        Private Key Path        The path of the file that will contain the generated private key.
        Key Size                The size of the keys. Must be a multiple of 8, between 384 and 16384 [Default: 2048].

/sign <Private Key Path> <License Content Path> <License Path> [/algorithm:<Algorithm>] [/base64] [/password:<Password>]
        Uses the specified private key to sign the license content and generates a signed license file.

        Private Key Path        The path of the file that contains the private key.
        License Content Path    The path of the file that contains the license content.
        License Path            The path of the file that will contain the signed license.
        Algorithm               The algorithm to use for the signature process [Default: SHA256].
                                  Accepted values are : SHA, SHA1, MD5, SHA256, SHA384, SHA512
        /base64                 Generates a Base64-encoded file instead of an XML file.
        Password                The password for the private key file.

/sign <Certificate Thumbprint> <License Content Path> <License Path> [/algorithm:<Algorithm>] [/base64]
        Uses the private key in the certificate store to sign the license content and generates a signed license file.

        Certificate Thumbprint  The thumbprint of the certificate containing the private key in the certificate store.
                                  The programm will look first in the current user's store, then in the computer's.
                                  The certificate key MUST be marked as exportable.
        License Content Path    The path of the file that contains the license content.
        License Path            The path of the file that will contain the signed license.
        Algorithm               The algorithm to use for the signature process [Default: SHA256].
                                  Accepted values are : SHA, SHA1, MD5, SHA256, SHA384, SHA512
        /base64                 Generates a Base64-encoded file instead of an XML file.

/verify <Public Key Path> <License Path>
        Checks the validity of a license file against a specified public key.

        Public Key Path         The path of the file that contains the public key.
        License Path            The path of the file that contains the license to check.
```