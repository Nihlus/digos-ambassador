#!/bin/bash

rm Content/Databases/*.db
rm -rf Migrations

dotnet ef migrations add InitialCreate -v && dotnet ef database update -v
