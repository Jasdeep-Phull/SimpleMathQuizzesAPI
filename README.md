CORS is not set up on the SPA or the API. The SPA and API were tested with CORS disabled.
Tested on chrome with CORS disabled using: 'chrome.exe --disable-web-security --user-data-dir=~/chromeTemp'
(from folder: 'C:\Program Files\Google\Chrome\Application')


The following warning is displayed when rebuilding the project:
"warning NU1701: Package 'DataAnnotationsExtensions 5.0.1.27' was restored using '.NETFramework,Version=v4.6.1, .NETFramework,Version=v4.6.2, .NETFramework,Version=v4.7, .NETFramework,Version=v4.7.1, .NETFramework,Version=v4.7.2, .NETFramework,Version=v4.8, .NETFramework,Version=v4.8.1' instead of the project target framework 'net8.0'. This package may not be fully compatible with your project."
The [Min()] data annotation from this NuGet package has been tested, and it is currently (27/03/2024) working as intended.
