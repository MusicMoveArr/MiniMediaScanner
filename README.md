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
   
   Every word gets capatalized (rest of the letters lowercase) except roman letters, all uppercase
   
   Small words are lowercase: of, the, and, in, on, at, for, to, a
   
   Special characters are replaced: – to -, — to -, … to ...
   
   Seperators between words are kept: : - _ / ,
10. TagMissingMetadata - Add missing tags to your media files from AcoustId/MusicBrainz from fingerprints
11. EqualizeMetadata - Equalize (set the same) tag value for the entire album of an artist (mostly to fix issues with albums showing weird/duplicated in Plex/Navidrome etc)
12. FixVersioning - Find media that are using the same track/disc numbering, usually normal version and (AlbumVersion), (Live) etc
    The media with the longest file name and contains TrackFilters will get incremented disc number
    This will make it so the normal version of the album stays at disc 1 but remix(etc) gets disc number 1001+
13. Cover ArtArchive - Grab covers from the CoverArtArchive and save them as cover.jpg
14. Cover Extract - Extract the covers directly from the media files and save them as cover.jpg
15. RemoveTag - Remove specific tags from Artist/Albums
16. RefreshMetadata - Simply do a quick refresh of the metadata for an artist/album
17. SplitArtist - Split Artist is kind of experimental, it will try to split the 2 artist's that have the same name apart into 2 seperate artists

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
dotnet MiniMediaScanner.dll tagmissingmetadata --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Slayer --accoustid xxxxxxxx -w
Tags list it will write when empty/missing:
SCRIPT
barcode
MusicBrainz Artist Id
MusicBrainz Track Id
MusicBrainz Release Artist Id
MusicBrainz Release Group Id
MusicBrainz Release Id
MusicBrainz Album Artist Id
MusicBrainz Album Id
MusicBrainz Album Type
MusicBrainz Album Release Country
MusicBrainz Album Status
Acoustid Id
Date
originaldate
originalyear
Disc Number
Track Number
Total Tracks
MEDIA
```
```
dotnet MiniMediaScanner.dll equalizemediatag --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia"  -a Pendulum -t asin -wt asin -y
Tags available: date, originaldate, originalyear, year, disc, asin, catalognumber
```
```
dotnet MiniMediaScanner.dll normalizefile --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" \
--normalize-artist-name \
--normalize-album-name \
--normalize-title-name \
--overwrite \
--sub-directory-depth 2 \
--rename \
--file-format "{ArtistName} - {AlbumName} - {Tag_Disc:cond:<=1?{Tag_Track:00}|{Tag_Disc:00}-{Tag_Track:00}} - {Title}" \
--directory-format "{ArtistName}/{AlbumName}" \
--directory-seperator "_"
```

## more Example
```
export CONNECTIONSTRING="Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia"
dotnet MiniMediaScanner.dll import -p ~/Music/
dotnet MiniMediaScanner.dll deletedmedia -a deadmau5
dotnet MiniMediaScanner.dll deduplicate -d -a deadmau5
dotnet MiniMediaScanner.dll fingerprint -a deadmau5
dotnet MiniMediaScanner.dll tagmissingmetadata --accoustid xxxxx -w -a deadmau5
dotnet MiniMediaScanner.dll removetag --tag artistsort -a deadmau5
dotnet MiniMediaScanner.dll removetag --tags ["artistsort", "albumartistsortorder"] --artist deadmau5
dotnet MiniMediaScanner.dll coverextract --artist deadmau5
dotnet MiniMediaScanner.dll coverartarchive --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t date -wt date -y --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t originalyear -wt originalyear -y --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t originaldate -wt originaldate -y --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t year -wt year -y --artist deadmau5
dotnet MiniMediaScanner.dll fixversioning -f ["("] --confirm --artist deadmau5

```


# Personal way to fix for example artist Pendulum
What all  of this will do:
1. Import new media (or fully in a few scan)
2. Scan for any delete media, not to not try processing deleted files
3. Fingerprint all the media so the command tagmissingmetadata can be used, because tagmissingmetadata is using the fingerprint and fingerprinted duration
4. tagmissingmetadata command will add any missing tags to the media by using AcoustId/MusicBrainz
5. fixversioning command will set the disc number +1000 (for example disc number 1 to 10001, disc number 5 to 10005) with files containing "("
6. equalizemediatag commands will set the same tags of (date, originalyear, originaldate, catalognumber, asin) for the media of each album, making the media show up correct in order for best viewing/listening experience
```
dotnet MiniMediaScanner.dll import --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" --path "~/nfs_share_music/Pendulum"
dotnet MiniMediaScanner.dll deletedmedia --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum
dotnet MiniMediaScanner.dll fingerprint --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum
dotnet MiniMediaScanner.dll tagmissingmetadata --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum --accoustid xxxxxxxx -w
dotnet MiniMediaScanner.dll fixversioning --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum -f "["("]" -y
dotnet MiniMediaScanner.dll equalizemediatag --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum -t date -wt date -y
dotnet MiniMediaScanner.dll equalizemediatag --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum -t originalyear -wt originalyear -y
dotnet MiniMediaScanner.dll equalizemediatag --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum -t originaldate -wt originaldate -y
dotnet MiniMediaScanner.dll equalizemediatag --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum -t catalognumber -wt catalognumber -y
dotnet MiniMediaScanner.dll equalizemediatag --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum -t asin -wt asin -y

```

# Docker
| Environment Name | Value Example |
|-----:|---------------|
|IMPORT_PATH| ~/Music |
|CRON (Quartz)|0 0 * * * ? *|
|CONNECTIONSTRING|Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia|
