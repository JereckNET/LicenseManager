## User Guide
### With XML keys
1. Generate signing keys :
```
LicenseManager.exe /generateKeys Public_Key.xml Private_Key.xml
```
2. Store `Private_Key.xml`in a secure location.
3. Include the content of `Public_Key.xml` within your application (hard-coded, as resource, ...).
4. Generate the license content and store it in a distinct file.
5. Sign the content with your private key :
```
LicenseManager.exe /sign Private_Key.xml License_Content.dat License.lic
```
6. Load the content of the signed license in your application and validate it :
```csharp
private static bool verifyLicense(string _licenseFilePath) {
    string publicKey = Properties.Resources.PublicKey;

    License licenseToVerify = License.Load(_licenseFilePath);
    //TODO Add your exception handling if licenseToVerify is null

    bool result = licenseToVerify.Verify(publicKey);
    
    return result;
}
```

The way to generate the license content, how to transfer request and license between you and your customer, and what to do in your application with or without license is up to you.

### With X.509 Certificates
1. Generate signing certifiates:
```powershell
$cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -subject "License Manager" -KeyExportPolicy Exportable -NotAfter (Get-Date).AddYears(10) -Type Custom -KeySpec Signature
```
2. Export the public certificate
```powershell
Export-Certificate -Cert $cert -FilePath "License Manager.crt" 
```
3. Export the private key
```powershell
$mypwd = ConvertTo-SecureString -String "1234" -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath "License Manager.pfx"  -Password $mypwd
```
3. **or** Get the certificate thumbprint
```powershell
Write-Host $cert.Thumbprint
```


### Complete usage
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