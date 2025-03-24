#!/bin/pwsh
dotnet tool run nuget-license -- -i ../../kensaku.sln -t -o Json | mlr --ijson --opprint unsparsify then cut -f PackageId,PackageProjectUrl,Copyright,Authors,License,LicenseUrl > NugetLicenses.txt
