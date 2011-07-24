set current_dir=%~dp0
sc create AutoExtractor binPath= "%current_dir%bin\debug\AE.Service.exe" start= auto
sc start AutoExtractor
pause