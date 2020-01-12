if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$copy = $dir + "\copy\BepInEx" 

$ver = [System.Diagnostics.FileVersionInfo]::GetVersionInfo((Get-ChildItem -Path ($dir + "\BepInEx\patchers\*") -Force)[0]).FileVersion.ToString()

New-Item -ItemType Directory -Force -Path ($dir + "\out")  

Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")
New-Item -ItemType Directory -Force -Path ($copy + "\patchers")

Copy-Item -Path ($dir + "\BepInEx\plugins\IllusionFixes\AI_*.*") -Destination ($copy + "\plugins\IllusionFixes") -Force 
Copy-Item -Path ($dir + "\BepInEx\patchers\*") -Destination ($copy + "\patchers") -Force 

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_AIGirl_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")
New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

Copy-Item -Path ($dir + "\BepInEx\plugins\IllusionFixes\EC_*.*") -Destination ($copy + "\plugins\IllusionFixes") -Force 
Copy-Item -Path ($dir + "\BepInEx\patchers\*") -Destination ($copy + "\patchers") -Force 

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_EmotionCreators_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")  
#New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

Copy-Item -Path ($dir + "\BepInEx\plugins\IllusionFixes\KK_*.*") -Destination ($copy + "\plugins\IllusionFixes") -Force 
#Copy-Item -Path ($dir + "\BepInEx\patchers\*") -Destination ($copy + "\patchers") -Force 

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_Koikatsu_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")  
#New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

Copy-Item -Path ($dir + "\BepInEx\plugins\IllusionFixes\HS_*.*") -Destination ($copy + "\plugins\IllusionFixes") -Force 
#Copy-Item -Path ($dir + "\BepInEx\patchers\*") -Destination ($copy + "\patchers") -Force 

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_HoneySelect_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")  
#New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

Copy-Item -Path ($dir + "\BepInEx\plugins\IllusionFixes\PH_*.*") -Destination ($copy + "\plugins\IllusionFixes") -Force 
#Copy-Item -Path ($dir + "\BepInEx\patchers\*") -Destination ($copy + "\patchers") -Force 

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_PlayHome_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse