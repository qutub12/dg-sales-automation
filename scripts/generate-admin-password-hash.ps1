param([Parameter(Mandatory=$true)][SecureString]$Password)
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password)
try { $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
if ($plain.Length -lt 12) { throw "Password must contain at least 12 characters." }
$iterations = 210000
$salt = [Security.Cryptography.RandomNumberGenerator]::GetBytes(16)
$hash = [Security.Cryptography.Rfc2898DeriveBytes]::Pbkdf2($plain, $salt, $iterations, [Security.Cryptography.HashAlgorithmName]::SHA256, 32)
"pbkdf2-sha256`$$iterations`$$([Convert]::ToBase64String($salt))`$$([Convert]::ToBase64String($hash))"
