<#
.SYNOPSIS
    Encrypts or decrypts a connection string using AES-256 matching ConnectionStringProtector in DLPManagementSystem.
.EXAMPLE
    .\protect-connectionstring.ps1 -ConnectionString "Server=161.97.90.171,8797;Database=DLPSystem;User Id=DLPSystem;Password=DLP#Sys_9vR!72qL@4xN_2026;TrustServerCertificate=True;"
.EXAMPLE
    .\protect-connectionstring.ps1 -ConnectionString "Encrypted:..." -Decrypt
#>
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$ConnectionString,

    [switch]$Decrypt,

    [string]$Key = 'DLP#Sys_Key$2026_SecuredDbConn!!'
)

$keyBytes = [System.Text.Encoding]::UTF8.GetBytes($Key)
if ($keyBytes.Length -ne 32) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $keyBytes = $sha.ComputeHash($keyBytes)
}

if ($Decrypt) {
    if (-not $ConnectionString.StartsWith("Encrypted:")) {
        Write-Output $ConnectionString
        return
    }
    $base64 = $ConnectionString.Substring(10).Trim()
    $combined = [Convert]::FromBase64String($base64)
    if ($combined.Length -le 16) {
        throw "Invalid encrypted connection string payload."
    }
    $iv = New-Object byte[] 16
    $cipherBytes = New-Object byte[] ($combined.Length - 16)
    [Array]::Copy($combined, 0, $iv, 0, 16)
    [Array]::Copy($combined, 16, $cipherBytes, 0, $combined.Length - 16)

    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $keyBytes
    $aes.IV = $iv
    $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
    $decryptor = $aes.CreateDecryptor($keyBytes, $iv)
    $plainBytes = $decryptor.TransformFinalBlock($cipherBytes, 0, $cipherBytes.Length)
    $result = [System.Text.Encoding]::UTF8.GetString($plainBytes)
    Write-Output $result
} else {
    $aes = [System.Security.Cryptography.Aes]::Create()
    $aes.Key = $keyBytes
    $aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
    $aes.Padding = [System.Security.Cryptography.PaddingMode]::PKCS7
    $aes.GenerateIV()
    $iv = $aes.IV

    $plainBytes = [System.Text.Encoding]::UTF8.GetBytes($ConnectionString)
    $encryptor = $aes.CreateEncryptor($keyBytes, $iv)
    $cipherBytes = $encryptor.TransformFinalBlock($plainBytes, 0, $plainBytes.Length)

    $combined = New-Object byte[] ($iv.Length + $cipherBytes.Length)
    [Array]::Copy($iv, 0, $combined, 0, $iv.Length)
    [Array]::Copy($cipherBytes, 0, $combined, $iv.Length, $cipherBytes.Length)

    $result = "Encrypted:" + [Convert]::ToBase64String($combined)
    Write-Output $result
}
