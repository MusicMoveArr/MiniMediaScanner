# MiniMedia Scanner
MiniMedia's Scanner - Media Organizer
"Mini" Media - a funny way of saying "I have too much media" to just handle my music in a normal way

Inspired of beets and other tools to manage media

Loving the work I do? buy me a coffee https://buymeacoffee.com/musicmovearr

# Features
1. Postgres support
2. Import Full/Partially your entire library multi-threaded
3. MusicBrainz support (+cached in Postgres)
4. All commands are multi-threaded for performance
5. Low memory footprint

# Commands
1. Import - Import Full/Partially your entire library multi-threaded
2. UpdateMB - Update MusicBrainz media info in postgres
3. Missing - Query potentially missing songs from artists by checking with MusicBrainz
4. DeletedMedia - Check which media is deleted locally, optionally remove from database as well
5. Convert - Convert for example FLAC > M4A, FLAC > MP3 etc
6. Fingerprint - Fingerprint all the media by generating AcoustID / AcoustID Fingerprint
7. DeDuplicate - DeDuplicate files locally that contain at the end of the filename, (1).mp3, (2).m4a etc
8. NormalizeFile - Normalize/Standardize all your media file names to a common standard

# Examples
```
dotnet MiniMediaScanner.dll import --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" --path "~/Music" 
```
```
dotnet MiniMediaScanner.dll updatemb --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia"
```
```
dotnet MiniMediaScanner.dll updatemb --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a "["\Slayer\"]"
```
```
dotnet MiniMediaScanner.dll missing --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Slayer
```
```
dotnet MiniMediaScanner.dll deletedmedia --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia"
```
```
dotnet MiniMediaScanner.dll convert --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" --from-extension flac --to-extension m4a --codec aac --bitrate 320k
```
```
dotnet MiniMediaScanner.dll fingerprint --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia"
```
```
dotnet MiniMediaScanner.dll deduplicate --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Slayer --delete
```
```
dotnet MiniMediaScanner.dll normalizefile --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" \
--normalize-artist-name \
--normalize-album-name \
--normalize-title-name \
--overwrite \
--sub-directory-depth 2 \
--rename \
--file-format "{artist} - {album} - {track} - {title}" \
--directory-format "{artist}/{album}" \
--directory-seperator "_"
```
