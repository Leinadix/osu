dotnet build ./client/osu.Desktop -c Release
dotnet build ./touhosu -c Release
move /Y .\touhosu\osu.Game.Rulesets.Touhosu\bin\Release\net6.0\osu.Game.Rulesets.Touhosu.dll %appdata%\osu-lazer\rulesets