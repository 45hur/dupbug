docker stop lmdbdupbug
docker rm lmdbdupbug
docker build . -t lmdbdupbug
docker run lmdbdupbug