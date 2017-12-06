#!/bin/bash

rm Content/Databases/*.db
rm -rf Migrations

dotnet ef migrations add InitialCreate --context GlobalInfoContext -o Migrations/Global -v && dotnet ef database update --context GlobalInfoContext -v
dotnet ef migrations add InitialCreate --context LocalInfoContext -o Migrations/Local -v
