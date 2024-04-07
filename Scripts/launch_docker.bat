cd ..

:: builds the dotnet application in release mode, then publishes that release build
dotnet publish --force /property:Configuration=Release


docker stop baguette-container
docker rm baguette-container

docker build --tag baguette-image .\

docker run -d --restart=always --name baguette-container baguette-image

:: set /p DUMMY=Hit ENTER to continue...