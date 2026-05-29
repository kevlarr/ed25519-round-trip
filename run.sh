set -e

echo "\nUsing BouncyCastle to generate and sign\n"
cd ./dotnet
dotnet run
echo "\nVerifying signature with Go\n"
cd ../go
go run main.go
echo
echo "Now with Chaos in the driver's seat\n"
cd ../dotnet
dotnet run --chaos
echo "\nVerifying signature with Go\n"
cd ../go
go run main.go
