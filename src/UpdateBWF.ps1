$peaks = (Get-Process | Where {$_.Name -eq "Peaks of Yore"}).path
$lastPeaks = $peaks

Write-Host "waiting for Peaks of Yore to close"

While($peaks.count -ne 0) {
    Start-Sleep -Milliseconds 100
    $lastPeaks = $peaks
    $peaks = (Get-Process | Where {$_.Name -eq "Peaks of Yore"}).path
}

Write-Host "Peaks of Yore is closed"
Write-Host "Updating Bag with Friends"

$peaksPath = (Split-Path -Parent $lastPeaks)

Move-Item -Path ($peaksPath + "\Bag With Friends.dll") -Destination ($peaksPath + "\Mods\Bag With Friends.dll") -Force

$args = Get-Content -Path ($peaksPath + "\LastStartInfo.txt")

if ($args.Length -eq 0) {
    Start-Process -FilePath $lastPeaks -WorkingDirectory $peaksPath
} else {
    Start-Process -FilePath $lastPeaks -WorkingDirectory $peaksPath -ArgumentList (Get-Content -Path ($peaksPath + "\LastStartInfo.txt"))
}

$peaks
$lastPeaks
$peaksPath
$args
