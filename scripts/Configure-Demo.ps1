$ErrorActionPreference='Stop'
$target=Join-Path (Split-Path $PSScriptRoot -Parent) 'LibraryOnline\Web.Local.config'
if(Test-Path -LiteralPath $target){Write-Host 'Đã có cấu hình demo cục bộ. Thay mật khẩu tài khoản qua website nếu DB đã tồn tại.';return}
$secret=Read-Host 'Chọn mật khẩu demo (>=10 ký tự, chữ hoa/thường, số, ký tự đặc biệt)' -AsSecureString
$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($secret)
try {
 $value=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
 if($value.Length -lt 10 -or $value -cnotmatch '[A-Z]' -or $value -cnotmatch '[a-z]' -or $value -notmatch '[0-9]' -or $value -notmatch '[^a-zA-Z0-9]'){throw 'Mật khẩu chưa đạt yêu cầu.'}
 $document=New-Object System.Xml.XmlDocument
 $document.LoadXml('<appSettings><add key="DemoPassword" value="" /></appSettings>')
 $document.appSettings.add.SetAttribute('value',$value)
 $document.Save($target)
 Write-Host 'Đã lưu cấu hình cục bộ, không đưa lên Git.'
} finally {[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer);$value=$null}
