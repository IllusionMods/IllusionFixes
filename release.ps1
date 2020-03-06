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

& robocopy ($dir + "\BepInEx\plugins\IllusionFixes\") ($copy + "\plugins\IllusionFixes") "AI_*.*" /R:5 /W:5 
& robocopy ($dir + "\BepInEx\patchers\") ($copy + "\patchers") /R:5 /W:5     

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_AIGirl_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")
New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

& robocopy ($dir + "\BepInEx\plugins\IllusionFixes\") ($copy + "\plugins\IllusionFixes") "EC_*.*" /R:5 /W:5     
& robocopy ($dir + "\BepInEx\patchers\") ($copy + "\patchers") /R:5 /W:5     

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_EmotionCreators_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")  
#New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

& robocopy ($dir + "\BepInEx\plugins\IllusionFixes\") ($copy + "\plugins\IllusionFixes") "KK_*.*" /R:5 /W:5     
#& robocopy ($dir + "\BepInEx\patchers\") ($copy + "\patchers") /R:5 /W:5     

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_Koikatsu_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")  
#New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

& robocopy ($dir + "\BepInEx\plugins\IllusionFixes\") ($copy + "\plugins\IllusionFixes") "HS_*.*" /R:5 /W:5     
#& robocopy ($dir + "\BepInEx\patchers\") ($copy + "\patchers") /R:5 /W:5     

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_HoneySelect_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse
New-Item -ItemType Directory -Force -Path ($copy + "\plugins\IllusionFixes")  
#New-Item -ItemType Directory -Force -Path ($copy + "\patchers") 

& robocopy ($dir + "\BepInEx\plugins\IllusionFixes\") ($copy + "\plugins\IllusionFixes") "PH_*.*" /R:5 /W:5     
#& robocopy ($dir + "\BepInEx\patchers\") ($copy + "\patchers") /R:5 /W:5     

Compress-Archive -Path $copy -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "out\" + "IllusionFixes_PlayHome_" + $ver + ".zip")


Remove-Item -Force -Path ($dir + "\copy") -Recurse