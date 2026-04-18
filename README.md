# MiniMedia Scanner
MiniMedia's Scanner - Media Organizer
"Mini" Media - a funny way of saying "I have too much media" to just handle my music in a normal way

Inspired of beets and other tools to manage media

Loving the work I do? buy me a coffee https://buymeacoffee.com/musicmovearr

# Roadmap
This roadmap will be ongoing as the project keeps going
- [x] All commands are Asynchronous
- [x] All commands support Environment Variables
- [x] Docker support
- [ ] Generate BPM
- [ ] Generate Replay gain
- [ ] Generate Waveforms (maybe? need to see how/which servers can benefit from this)
- [x] Implement Spotify's API for Tags (ongoing)
- [x] Implement Tidal's API for Tags (ongoing)
- [ ] Implement Discogs's API for Tags
- [ ] Implement Beatport's API for Tags
- [x] Implement Deezer's API for Tags
- [ ] Improve Missing songs command
- [ ] Command to cleanup/merge tags (e.g. AlbumArtist, album_artist...)

# Features
1. Postgres support
2. Import Full/Partially your entire library multi-threaded
3. MusicBrainz support (+cached in Postgres)
4. Spotify support (+cached in Postgres)
5. Tidal support (+cached in Postgres)
6. Deezer support (+cached in Postgres)
7. All commands are multi-threaded for performance
8. Low memory footprint

# Commands
1. Import - Import Full/Partially your entire library multi-threaded
2. UpdateMB - Update MusicBrainz media info in postgres
3. UpdateSpotify - Update/Add artists into the database from Spotify's API
4. UpdateTidal - Update/Add artists into the database from Tidal's API
5. UpdateDeezer - Update/Add artists into the database from Deezer's API
6. Missing - Query potentially missing songs from artists by checking with MusicBrainz
7. DeletedMedia - Check which media is deleted locally, optionally remove from database as well
8. Convert - Convert for example FLAC > M4A, FLAC > MP3 etc
9. Fingerprint - Fingerprint all the media by generating AcoustID / AcoustID Fingerprint
10. DeDuplicate - DeDuplicate files locally that contain at the end of the filename, (1).mp3, (2).m4a etc
11. NormalizeFile - Normalize/Standardize all your media file names to a common standard
   
   Every word gets capatalized (rest of the letters lowercase) except roman letters, all uppercase
   
   Small words are lowercase: of, the, and, in, on, at, for, to, a
   
   Special characters are replaced: – to -, — to -, … to ...
   
   Seperators between words are kept: : - _ / ,
   
12. TagMissingMetadata - Add missing tags to your media files from AcoustId/MusicBrainz from fingerprints
13. TagMissingSpotifyMetadata - Add missing tags to your media files from Spotify
14. TagMissingTidalMetadata - Add missing tags to your media files from Tidal
15. TagMissingDeezerMetadata - Add missing tags to your media files from Deezer
16. EqualizeMetadata - Equalize (set the same) tag value for the entire album of an artist (mostly to fix issues with albums showing weird/duplicated in Plex/Navidrome etc)
17. FixVersioning - Find media that are using the same track/disc numbering, usually normal version and (AlbumVersion), (Live) etc
    The media with the longest file name and contains TrackFilters will get incremented disc number
    This will make it so the normal version of the album stays at disc 1 but remix(etc) gets disc number 1001+
18. Cover ArtArchive - Grab covers from the CoverArtArchive and save them as cover.jpg
19. Cover Extract - Extract the covers directly from the media files and save them as cover.jpg
20. RemoveTag - Remove specific tags from Artist/Albums
21. RefreshMetadata - Simply do a quick refresh of the metadata for an artist/album
22. SplitArtist - Split Artist is kind of experimental, it will try to split the 2 artist's that have the same name apart into 2 seperate artists
23. Stats - Show basic stats of your database
24. SplitTag - Split specific tag's into single value fields by specific seperator like ';'
25. FixCollections - Fix collections by adding the missing artist to the Artists tag
24. GroupTaggingDeezerMetadata - Group Tagging metadata per Album - Deezer
25. GroupTaggingMBMetadata - Group Tagging metadata per Album - MusicBrainz
26. GroupTaggingSpotifyMetadata - Group Tagging metadata per Album - Spotify
27. GroupTaggingTidalMetadata - Group Tagging metadata per Album - Tidal

# FAQ
### Why is there no confirm question on tagging files
Unlike MusicBrainz Picard and other tools, this projects truly respects the already existing tags

Tagging with the databases of MusicBrainz, Spotify, Tidal there must be a +80% match against the Artist, Album, track title

When an Album/Track contains a number, there must have a 100% match because "Best of 2020" can match "Best of 2021", same for year/volume numbers etc

So far tagging with a lot of files I have not found a single mismatch by this approach

# Examples
```
dotnet MiniMediaScanner.dll import --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" --path "~/Music" 
```
```
dotnet MiniMediaScanner.dll updatemb --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia"
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
dotnet MiniMediaScanner.dll equalizemediatag --connection-string "Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia" -a Pendulum -t asin -w asin -y
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
dotnet MiniMediaScanner.dll import --path "~/Music/"
dotnet MiniMediaScanner.dll deletedmedia -a deadmau5
dotnet MiniMediaScanner.dll deduplicate -d -a deadmau5
dotnet MiniMediaScanner.dll fingerprint -a deadmau5
dotnet MiniMediaScanner.dll tagmissingmetadata --accoustid xxxxx -w -a deadmau5
dotnet MiniMediaScanner.dll tagmissingspotifymetadata -w -a deadmau5
dotnet MiniMediaScanner.dll removetag --tag "artistsort" -a deadmau5
dotnet MiniMediaScanner.dll removetag --tags "artistsort" "albumartistsortorder" --artist deadmau5
dotnet MiniMediaScanner.dll coverextract --artist deadmau5
dotnet MiniMediaScanner.dll coverartarchive --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t date -w date -y --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t originalyear -w originalyear -y --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t originaldate -w originaldate -y --artist deadmau5
dotnet MiniMediaScanner.dll equalizemediatag -t year -w year -y --artist deadmau5
dotnet MiniMediaScanner.dll fixversioning -f "(" --confirm --artist deadmau5
dotnet MiniMediaScanner.dll updatespotify --spotify-client-id "xxxxx" --spotify-secret-id "xxxxxx" --artist deadmau5
dotnet MiniMediaScanner.dll stats
dotnet MiniMediaScanner.dll splittag --tag AlbumArtist ----write-tag ARTISTS -s ; ----overwrite-tag ----update-read-tag --update-write-tag-original-value -y -a deadmau5
```

# Split Tag explained
The SplitTag command will split multi-value tags as a single value into the same or another tag

Some media like the "abuse" the wrong tags for another purpose like filling AlbumArtist/Artist tag with the value of ARTISTS

To fix this problem easy, we can tell it to read the tag AlbumArtist (-t/--tag AlbumArtist) and use it's original value (-R/--update-read-tag-original-value) to write to ARTISTS (-w/--write-tag ARTISTS)

We used in this example as well (-W/--update-write-tag-original-value)to write the original value to ARTISTS from AlbumArtist, otherwise ARTISTS will become as well a single value field

To automate this process you can tell it to automatically confirm with (-y)

-s/--seperator, is used to tell how to seperate the values, in most cases it's ';'

```
dotnet MiniMediaScanner.dll splittag --tag AlbumArtist --write-tag ARTISTS -s ; --overwrite-tag --update-read-tag --update-write-tag-original-value -y --artist deadmau5
```

Output example,
It will move value "deadmau5;187 Lockdown" that was in AlbumArtist over to the ARTISTS
And write only the single value into AlbumArtist
```
Checking artist 'deadmau5', found 1 tracks to process
Updating tag 'ARTISTS' value '' => 'deadmau5;187 Lockdown'
Updating tag 'AlbumArtist' value 'deadmau5;187 Lockdown' => 'deadmau5'
Confirm changes? (Y/y or N/n)
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
export CONNECTIONSTRING="Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia"
dotnet MiniMediaScanner.dll import --path "~/nfs_share_music/Pendulum"
dotnet MiniMediaScanner.dll deletedmedia -a Pendulum
dotnet MiniMediaScanner.dll fingerprint -a Pendulum
dotnet MiniMediaScanner.dll tagmissingmetadata -a Pendulum --accoustid xxxxxxxx -w
dotnet MiniMediaScanner.dll fixversioning -a Pendulum -f "(" -y
dotnet MiniMediaScanner.dll equalizemediatag -a Pendulum -t date -w date -y
dotnet MiniMediaScanner.dll equalizemediatag -a Pendulum -t originalyear -w originalyear -y
dotnet MiniMediaScanner.dll equalizemediatag -a Pendulum -t originaldate -w originaldate -y
dotnet MiniMediaScanner.dll equalizemediatag -a Pendulum -t catalognumber -w catalognumber -y
dotnet MiniMediaScanner.dll equalizemediatag -a Pendulum -t asin -w asin -y

```

# Docker Compose example
Refresh the metadata every 6 hours, change the volume(s), connectionstring accordingly to your environment

Look below specifically for Environment variables per command to see which options are available to you
```
services:
  minimediascanner:
    image: musicmovearr/minimediascanner:main
    container_name: minimediascanner
    restart: unless-stopped
    environment:
      - PUID=1000
      - PGID=1000
      - COMMAND=refreshmetadata
      - CONNECTIONSTRING=Host=192.168.1.2;Username=postgres;Password=postgres;Database=minimedia
      - CRON=0 0 */6 ? * *
    volumes:
      - ~/Music:~/Music
```

# Quickstart with docker
A bit of a simplistic guide to get you started quick

This example does not use persistant storage for postgres, please host it somewhere with persistant storage otherwise you will need to re-import when postgres shutsdown

```
# install docker (on ArchLinux)
sudo pacman -Syy docker docker-compose docker-buildx
sudo usermod -aG docker $USER
sudo systemctl enable docker
sudo systemctl start docker

# create internal docker network
sudo docker network create -d bridge net-minimedia
```
start the following command where db.sql is or change $(pwd) with the directory where db.sql is
```
#pull source code
cd /tmp
git clone https://github.com/MusicMoveArr/MiniMediaScanner.git
cd /tmp/MiniMediaScanner/MiniMediaScanner
```
```
# start postgres (start in a different shell)
sudo docker run --rm -it \
--network net-minimedia \
--hostname postgres \
-e POSTGRES_PASSWORD=mysecretpassword \
-v $(pwd)/db.sql:/docker-entrypoint-initdb.d/init.sql \
postgres
```
Import your music, change the directory at /music (before :) to your music directory
```
# import /music
sudo docker run --rm -it --network net-minimedia --name minimediascanner \
-e CONNECTIONSTRING="Host=postgres;Username=postgres;Password=mysecretpassword;Database=postgres;Pooling=true;MinPoolSize=5;MaxPoolSize=100;" \
-e COMMAND="import" \
-e IMPORT_PATH="/music" \
-v ~/nfs_share_music/Pendulum/:/music \
musicmovearr/minimediascanner:main
```
Get artist information from MusicBrainz incase it wasn't done by importing the music

In the following example it will get artist information of artist "Pendulum"
```
# get MusicBrainz data (it tagging fails)
sudo docker run --rm -it --network net-minimedia --name minimediascanner \
-e CONNECTIONSTRING="Host=postgres;Username=postgres;Password=mysecretpassword;Database=postgres;Pooling=true;MinPoolSize=5;MaxPoolSize=100;" \
-e COMMAND="updatemb" \
-e UPDATEMB_ARTIST="Pendulum" \
musicmovearr/minimediascanner:main
```
Example of tagging music with MusicBrainz, get your own AcoustId ApiKey at https://acoustid.org to replace the "xxxxxxx"
```
# tag music with MusicBrainz
sudo docker run --rm -it --network net-minimedia --name minimediascanner \
-e CONNECTIONSTRING="Host=postgres;Username=postgres;Password=mysecretpassword;Database=postgres;Pooling=true;MinPoolSize=5;MaxPoolSize=100;" \
-e COMMAND="tagmissingmetadata" \
-e TAGMISSINGMETADATA_ACOUSTID="xxxxxxx" \
-e TAGMISSINGMETADATA_WRITE="true" \
-v ~/nfs_share_music/Pendulum/:/music \
musicmovearr/minimediascanner:main
```
Some other commands for example,
```
# get cover art
sudo docker run --rm -it --network net-minimedia --name minimediascanner \
-e CONNECTIONSTRING="Host=postgres;Username=postgres;Password=mysecretpassword;Database=postgres;Pooling=true;MinPoolSize=5;MaxPoolSize=100;" \
-e COMMAND="coverartarchive" \
-v ~/nfs_share_music/Pendulum/:/music \
musicmovearr/minimediascanner:main
```
```
# show some stats
sudo docker run --rm -it --network net-minimedia --name minimediascanner \
-e CONNECTIONSTRING="Host=postgres;Username=postgres;Password=mysecretpassword;Database=postgres;Pooling=true;MinPoolSize=5;MaxPoolSize=100;" \
-e COMMAND="stats" \
musicmovearr/minimediascanner:main
```

# Acoustid Check Command
```
USAGE
  dotnet MiniMediaScanner.dll acoustidcheck --connection-string <value> --acoustid-clientkey <value> [options]

DESCRIPTION
  Check your submissions statuses from AcoustId

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* --acoustid-clientkey  AcoustId ClientKey for authentication Environment variable: ACOUSTIDCHECK_CLIENTKEY. 
  -h|--help         Shows help text. 
```

# Acoustid Submit Command
```
USAGE
  dotnet MiniMediaScanner.dll acoustidsubmit --connection-string <value> --acoustid-clientkey <value> --acoustid-userkey <value> [options]

DESCRIPTION
  Check your submissions statuses from AcoustId

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* --acoustid-clientkey  AcoustId ClientKey for authentication Environment variable: ACOUSTIDSUBMISSION_CLIENTKEY. 
* --acoustid-userkey  AcoustId UserKey for authentication Environment variable: ACOUSTIDSUBMISSION_USERKEY. 
  -h|--help         Shows help text. 
```

# Convert Command
```
USAGE
  dotnet MiniMediaScanner.dll convert --connection-string <value> --from-extension <value> --to-extension <value> --codec <value> --bitrate <value> [options]

DESCRIPTION
  Convert media for example FLAC > M4A

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -f|--from-extension  From extension. Environment variable: CONVERT_FROM_EXTENSION. 
* -t|--to-extension  To extension. Environment variable: CONVERT_TO_EXTENSION. 
* -c|--codec        Codec e.g. aac. Environment variable: CONVERT_CODEC. 
* -b|--bitrate      Bitrate e.g. 320k. Environment variable: CONVERT_BITRATE. 
  -a|--artist       Artistname Environment variable: CONVERT_ARTIST. 
  -h|--help         Shows help text. 
```

# Cover Art Archive Command
```
USAGE
  dotnet MiniMediaScanner.dll coverartarchive --connection-string <value> [options]

DESCRIPTION
  Download Cover art from the Cover Art Archive (only Album cover supported)

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: COVERARTARCHIVE_ARTIST. 
  -b|--album        target Album Environment variable: COVERARTARCHIVE_ALBUM. 
  -f|--filename     File name e.g. cover.jpg. Environment variable: COVERARTARCHIVE_FILENAME. Default: "cover.jpg".
  -h|--help         Shows help text. 
```

# Cover Art Deezer Command
```
USAGE
  dotnet MiniMediaScanner.dll coverartdeezer --connection-string <value> [options]

DESCRIPTION
  Download Cover art from Deezer for Artist and Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: COVERARTDEEZER_ARTIST. 
  -b|--album        target Album Environment variable: COVERARTDEEZER_ALBUM. 
  -f|--album-filename  Filename e.g. cover.jpg. Environment variable: COVERARTDEEZER_ALBUM_FILENAME. Default: "cover.jpg".
  -g|--artist-filename  Filename e.g. cover.jpg. Environment variable: COVERARTDEEZER_ARTIST_FILENAME. Default: "cover.jpg".
  -h|--help         Shows help text. 
```

# Cover Art Discogs Command
```
USAGE
  dotnet MiniMediaScanner.dll coverartdiscogs --connection-string <value> --discogs-token <value> [options]

DESCRIPTION
  Download Cover art from Discogs for Artist and Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* --discogs-token   The Discogs token required to get the covers of Discogs Environment variable: COVERARTDISCOGS_TOKEN. 
  -a|--artist       Artistname Environment variable: COVERARTDISCOGS_ARTIST. 
  -b|--album        target Album Environment variable: COVERARTDISCOGS_ALBUM. 
  -f|--album-filename  Filename e.g. cover.jpg. Environment variable: COVERARTDISCOGS_ALBUM_FILENAME. Default: "cover.jpg".
  -g|--artist-filename  Filename e.g. cover.jpg. Environment variable: COVERARTDISCOGS_ARTIST_FILENAME. Default: "cover.jpg".
  -h|--help         Shows help text. 
```

# Cover Art Spotify Command
```
USAGE
  dotnet MiniMediaScanner.dll coverartspotify --connection-string <value> [options]

DESCRIPTION
  Download Cover art from Spotify for Artist and Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: COVERARTSPOTIFY_ARTIST. 
  -b|--album        target Album Environment variable: COVERARTSPOTIFY_ALBUM. 
  -f|--album-filename  Filename e.g. cover.jpg. Environment variable: COVERARTSPOTIFY_ALBUM_FILENAME. Default: "cover.jpg".
  -g|--artist-filename  Filename e.g. cover.jpg. Environment variable: COVERARTSPOTIFY_ARTIST_FILENAME. Default: "cover.jpg".
  -h|--help         Shows help text. 
```

# Cover Art Tidal Command
```
USAGE
  dotnet MiniMediaScanner.dll coverarttidal --connection-string <value> [options]

DESCRIPTION
  Download Cover art from Tidal for Artist and Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: COVERARTTIDAL_ARTIST. 
  -b|--album        target Album Environment variable: COVERARTTIDAL_ALBUM. 
  -f|--album-filename  Filename e.g. cover.jpg. Environment variable: COVERARTTIDAL_ALBUM_FILENAME. Default: "cover.jpg".
  -g|--artist-filename  Filename e.g. cover.jpg. Environment variable: COVERARTTIDAL_ARTIST_FILENAME. Default: "cover.jpg".
  -h|--help         Shows help text. 
```

# Cover Extract Command
```
USAGE
  dotnet MiniMediaScanner.dll coverextract --connection-string <value> [options]

DESCRIPTION
  Extract Cover art from the media files (only Album cover supported)

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: COVEREXTRACT_ARTIST. 
  -b|--album        target Album Environment variable: COVEREXTRACT_ALBUM. 
  -f|--filename     File name e.g. cover.jpg. Environment variable: COVEREXTRACT_FILENAME. Default: "cover.jpg".
  -h|--help         Shows help text. 
```

# DeDuplicate Command
```
USAGE
  dotnet MiniMediaScanner.dll deduplicate --connection-string <value> [options]

DESCRIPTION
  Check for duplicated music per album and delete optionally

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: DEDUPLICATE_ARTIST. 
  -d|--delete       Delete duplicate file Environment variable: DEDUPLICATE_DELETE. Default: "False".
  -A|--accuracy     Filename matching accuracy, 98% is recommended Environment variable: DEDUPLICATE_ACCURACY. Default: "98".
  --acoustfingerprint-accuracy  Acoust Fingerprint matching accuracy, 99% is recommended, 98% and lower can mismatch real fast, think of remixes etc Environment variable: DEDUPLICATE_ACOUSTFINGERPRINT_ACCURACY. Default: "99".
  -e|--extensions   Extensions to keep, in order, first found extension is kept (extensions must be without '.') Environment variable: DEDUPLICATE_EXTENSIONS. Default: "flac", "m4a", "wav", "aaif", "opus", "mp3".
  --check-extensions  Check for duplicate filenames with difference file extensions Environment variable: DEDUPLICATE_CHECK_EXTENSIONS. Default: "False".
  --check-versions  Check for duplicate filename versions, files ending with (1), (2), (3) and so on Environment variable: DEDUPLICATE_CHECK_VERSIONS. Default: "False".
  --check-album-duplicates  Check for duplicate files per album (works better for multi-drive / MergerFS Setups) with a accuracy of X % given with --accuracy or environment variable DEDUPLICATE_ACCURACY Environment variable: DEDUPLICATE_CHECK_EXTENSIONS. Default: "False".
  --check-album-extensions  Similar to --check-extensions with the difference of better support for multi-drive / MergerFS Setups, --check-extensions checks the full path, this option checks per album Environment variable: DEDUPLICATE_CHECK_ALBUM_EXTENSIONS. Default: "False".
  --check-album-extensions-acoustfingerprint  Similar to --check-extensions with the difference of better support for multi-drive / MergerFS Setups, --check-extensions checks the full path, this option checks per album Environment variable: DEDUPLICATE_CHECK_ALBUM_EXTENSIONS_ACOUSTFINGERPRINT. Default: "False".
  -h|--help         Shows help text. 
```

# DeDuplicate Singles Command
```
USAGE
  dotnet MiniMediaScanner.dll deduplicatesingles --connection-string <value> [options]

DESCRIPTION
  Check for duplicated music, specifically Singles, and delete if the same song already exists in an album optionally

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: DEDUPLICATESINGLES_ARTIST. 
  -d|--delete       Delete duplicate file Environment variable: DEDUPLICATESINGLES_DELETE. Default: "False".
  -h|--help         Shows help text. 
```

# Deleted Media Command
```
USAGE
  dotnet MiniMediaScanner.dll deletedmedia --connection-string <value> [options]

DESCRIPTION
  Check for deleted/missing music files on disk

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: DELETEDMEDIA_ARTIST. 
  -b|--album        target Album Environment variable: DELETEDMEDIA_ALBUM. 
  -r|--remove       Remove records from database. Environment variable: DELETEDMEDIA_REMOVE. Default: "True".
  -h|--help         Shows help text. 
```

# Equalize Mediatag Command
```
USAGE
  dotnet MiniMediaScanner.dll equalizemediatag --connection-string <value> --tag <value> [options]

DESCRIPTION
  Equalize MediaTags of albums from artists to fix issues with albums showing weird/duplicated in Plex/Navidrome etc, 
Tags available: date, originaldate, originalyear, year, disc, asin, catalognumber

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -t|--tag          Tag. Environment variable: EQUALIZEMEDIATAG_TAG. 
  -a|--artist       Artistname Environment variable: EQUALIZEMEDIATAG_ARTIST. 
  -b|--album        target Album Environment variable: EQUALIZEMEDIATAG_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: EQUALIZEMEDIATAG_CONFIRM. Default: "False".
  -w|--writetag     Tag to write to, if not set, the tag to read from (-t/--tag) is used to write to. Environment variable: EQUALIZEMEDIATAG_WRITETAG. 
  -h|--help         Shows help text. 
```

# Fingerprint Command
```
USAGE
  dotnet MiniMediaScanner.dll fingerprint --connection-string <value> [options]

DESCRIPTION
  Re-fingerprint media

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: FINGERPRINT_ARTIST. 
  -b|--album        target Album Environment variable: FINGERPRINT_ALBUM. 
  -h|--help         Shows help text. 
```

# Fix Collections Command
```
USAGE
  dotnet MiniMediaScanner.dll fixcollections --connection-string <value> --albumregex <value> --addartist <value> [options]

DESCRIPTION
  Fix collections by adding the missing artist to the Artists tag

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -b|--albumregex   Target album(s) with regex Environment variable: FIXCOLLECTIONS_ALBUMREGEX. 
* -W|--addartist    Add the missing artist to the Artists tag Environment variable: FIXCOLLECTIONS_ADDARTIST. 
  -a|--artist       Artistname Environment variable: FIXCOLLECTIONS_ARTIST. 
  -l|--label        Target label to find songs belonging to a collection Environment variable: FIXCOLLECTIONS_LABEL. 
  -H|--copyright    Target copyright to find songs belonging to a collection Environment variable: FIXCOLLECTIONS_COPYRIGHT. 
  -y|--confirm      Always confirm automatically. Environment variable: FIXCOLLECTIONS_CONFIRM. Default: "False".
  -h|--help         Shows help text. 
```

# Fix Versioning Command
```
USAGE
  dotnet MiniMediaScanner.dll fixversioning --connection-string <value> [options]

DESCRIPTION
  Find media that are using the same track/disc numbering, usually normal version and (AlbumVersion), (Live) etc. 
The media with the longest file name and contains TrackFilters will get incremented disc number. 
This will make it so the normal version of the album stays at disc 1 but remix(etc) gets disc number 1001+

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: FIXVERSIONING_ARTIST. 
  -b|--album        target Album Environment variable: FIXVERSIONING_ALBUM. 
  -d|--disc-increment  Disc increment for remixes (+1000 recommended). Environment variable: FIXVERSIONING_DISC_INCREMENT. Default: "1000".
  -f|--track-filters  Filter names to apply to tracks, .e.g. (remixed by ...). Environment variable: FIXVERSIONING_TRACK_FILTERS. 
  -y|--confirm      Always confirm automatically. Environment variable: FIXVERSIONING_CONFIRM. Default: "False".
  -h|--help         Shows help text. 
```

# Group Tagging Deezer Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll grouptaggingdeezermetadata --connection-string <value> [options]

DESCRIPTION
  Group Tagging metadata per Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: GROUPTAGGINGDEEZERMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: GROUPTAGGINGDEEZERMETADATA_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: GROUPTAGGINGDEEZERMETADATA_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: GROUPTAGGINGDEEZERMETADATA_OVERWRITE_TAG. Default: "True".
  --overwrite-artist  Overwrite the Artist name when tagging from Deezer. Environment variable: GROUPTAGGINGDEEZERMETADATA_OVERWRITEARTIST. Default: "False".
  --overwrite-album-artist  Overwrite the Album Artist name when tagging from Deezer. Environment variable: GROUPTAGGINGDEEZERMETADATA_OVERWRITEALBUMARTIST. Default: "False".
  --overwrite-album  Overwrite the Album name when tagging from Deezer. Environment variable: GROUPTAGGINGDEEZERMETADATA_OVERWRITEALBUM. Default: "False".
  --overwrite-track  Overwrite the Track name when tagging from Deezer. Environment variable: GROUPTAGGINGDEEZERMETADATA_OVERWRITETRACK. Default: "False".
  --threads         The amount of threads to use. Environment variable: GROUPTAGGINGDEEZERMETADATA_THREADS. Default: "1".
  -h|--help         Shows help text. 
```

# Group Tagging MusicBrainz Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll grouptaggingmbmetadata --connection-string <value> [options]

DESCRIPTION
  Group Tagging metadata per Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: GROUPTAGGINGMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: GROUPTAGGINGMETADATA_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: GROUPTAGGINGMETADATA_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: GROUPTAGGINGMETADATA_OVERWRITE_TAG. Default: "False".
  -h|--help         Shows help text. 
```

# Group Tagging Spotify Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll grouptaggingspotifymetadata --connection-string <value> [options]

DESCRIPTION
  Group Tagging metadata per Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: GROUPTAGGINGSPOTIFYMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: GROUPTAGGINGSPOTIFYMETADATA_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: GROUPTAGGINGSPOTIFYMETADATA_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: GROUPTAGGINGSPOTIFYMETADATA_OVERWRITE_TAG. Default: "True".
  --overwrite-artist  Overwrite the Artist name when tagging from Spotify. Environment variable: GROUPTAGGINGSPOTIFYMETADATA_OVERWRITEARTIST. Default: "False".
  --overwrite-album-artist  Overwrite the Album Artist name when tagging from Spotify. Environment variable: GROUPTAGGINGSPOTIFYMETADATA_OVERWRITEALBUMARTIST. Default: "False".
  --overwrite-album  Overwrite the Album name when tagging from Spotify. Environment variable: GROUPTAGGINGSPOTIFYMETADATA_OVERWRITEALBUM. Default: "False".
  --overwrite-track  Overwrite the Track name when tagging from Spotify. Environment variable: GROUPTAGGINGSPOTIFYMETADATA_OVERWRITETRACK. Default: "False".
  -h|--help         Shows help text. 
```

# Group Tagging Tidal Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll grouptaggingtidalmetadata --connection-string <value> [options]

DESCRIPTION
  Group Tagging metadata per Album

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: GROUPTAGGINGTIDALMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: GROUPTAGGINGTIDALMETADATA_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: GROUPTAGGINGTIDALMETADATA_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: GROUPTAGGINGTIDALMETADATA_OVERWRITE_TAG. Default: "True".
  --overwrite-artist  Overwrite the Artist name when tagging from Tidal. Environment variable: GROUPTAGGINGTIDALMETADATA_OVERWRITEARTIST. Default: "False".
  --overwrite-album-artist  Overwrite the Album Artist name when tagging from Tidal. Environment variable: GROUPTAGGINGTIDALMETADATA_OVERWRITEALBUMARTIST. Default: "False".
  --overwrite-album  Overwrite the Album name when tagging from Tidal. Environment variable: GROUPTAGGINGTIDALMETADATA_OVERWRITEALBUM. Default: "False".
  --overwrite-track  Overwrite the Track name when tagging from Tidal. Environment variable: GROUPTAGGINGTIDALMETADATA_OVERWRITETRACK. Default: "False".
  -h|--help         Shows help text. 
```


# Import Command
```
USAGE
  dotnet MiniMediaScanner.dll import --connection-string <value> --path <value> [options]

DESCRIPTION
  Import music to your database

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -p|--path         From the directory. Environment variable: IMPORT_PATH. 
  -M|--update-mb    Update MusicBrainz. Environment variable: IMPORT_UPDATE_MB. Default: "True".
  -f|--force        Force import even if files did not change on disk. Environment variable: IMPORT_FORCE. Default: "False".
  --prevent-update-within-days  Prevent updating existing artists within x days from the last pull/update Environment variable: IMPORT_PREVENT_UPDATE_WITHIN_DAYS. Default: "7".
  --split-artists   Split artists based on external artist id's from MusicBrainz, Deezer etc. This prevents merging 2 different artists together. Environment variable: IMPORT_SPLIT_ARTISTS. Default: "False".
  -h|--help         Shows help text. 
```

# Missing Command
```
USAGE
  dotnet MiniMediaScanner.dll missing --connection-string <value> [options]

DESCRIPTION
  Check for missing music

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: MISSING_ARTIST. 
  -p|--provider     Provider can be either MusicBrainz or Spotify. Environment variable: MISSING_PROVIDER. Default: "musicbrainz".
  -o|--output       Output format, tags available: {Artist} {Album} {Track} {ArtistUrl} {AlbumUrl} {TrackUrl}. Environment variable: MISSING_OUTPUT. Default: "{Artist} - {Album} - {Track}".
  -F|--filterout    Filterout names from the output. Environment variable: MISSING_FILTEROUT. Default: .
  -e|--extension    When the specific file extension (mp3, opus, wav...) is not found, it's considered missing. Environment variable: MISSING_EXTENSION. Default: "".
  --file            Save the missing tracks list to a file. Environment variable: MISSING_FILE. 
  --file-append     Append to the file instead of a overwrite. Environment variable: MISSING_FILE_APPEND. Default: "False".
  -h|--help         Shows help text. 
```

# Move Untagged File Command
```
USAGE
  dotnet MiniMediaScanner.dll moveuntaggedfile --connection-string <value> --target-folder <value> --file-format <value> --directory-format <value> [options]

DESCRIPTION
  Move untagged files to another directory

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -T|--target-folder  Move the untagged files to this target folder. Environment variable: MOVEUNTAGGEDFILES_TARGET_FOLDER. 
* -f|--file-format  file format {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}. Environment variable: MOVEUNTAGGEDFILES_FILE_FORMAT. 
* -D|--directory-format  directory format {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}. Environment variable: MOVEUNTAGGEDFILES_DIRECTORY_FORMAT. 
  -a|--artist       Artistname Environment variable: MOVEUNTAGGEDFILES_ARTIST. 
  -b|--album        target Album Environment variable: MOVEUNTAGGEDFILES_ALBUM. 
  -w|--overwrite    overwrite existing files. Environment variable: MOVEUNTAGGEDFILES_OVERWRITE. Default: "False".
  -S|--directory-seperator  Directory Seperator replacer, replace '/' '\' to .e.g. '_'. Environment variable: MOVEUNTAGGEDFILES_DIRECTORY_SEPARATOR. Default: "_".
  --dry-run         Dry run, no changes will be made Environment variable: MOVEUNTAGGEDFILES_DRY_RUN. Default: "False".
  -h|--help         Shows help text. 
```

# Normalize File Command
```
USAGE
  dotnet MiniMediaScanner.dll normalizefile --connection-string <value> [options]

DESCRIPTION
  Normalize/Standardize all your media file names to a common standard. 
Every word gets capatalized (rest of the letters lowercase) except roman letters, all uppercase.
Small words are lowercase: of, the, and, in, on, at, for, to, a
Special characters are replaced: – to -, — to -, … to ...
Seperators between words are kept: : - _ / ,

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: NORMALIZEFILE_ARTIST. 
  -b|--album        target Album Environment variable: NORMALIZEFILE_ALBUM. 
  -A|--normalize-artist-name  normalize Artistname Environment variable: NORMALIZEFILE_NORMALIZE_ARTIST_NAME. Default: "False".
  -B|--normalize-album-name  normalize Albumname Environment variable: NORMALIZEFILE_NORMALIZE_ALBUM_NAME. Default: "False".
  -T|--normalize-title-name  normalize music Title Environment variable: NORMALIZEFILE_NORMALIZE_TITLE_NAME. Default: "False".
  -w|--overwrite    overwrite existing files. Environment variable: NORMALIZEFILE_OVERWRITE. Default: "False".
  -s|--sub-directory-depth  sub-directory depth to root-folder. Environment variable: NORMALIZEFILE_SUB_DIRECTORY_DEPTH. Default: "0".
  -r|--rename       rename file. Environment variable: NORMALIZEFILE_RENAME. Default: "False".
  -f|--file-format  rename file format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}. Environment variable: NORMALIZEFILE_FILE_FORMAT. Default: "".
  -D|--directory-format  rename directory format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}. Environment variable: NORMALIZEFILE_DIRECTORY_FORMAT. Default: "".
  -S|--directory-seperator  Directory Seperator replacer, replace '/' '\' to .e.g. '_'. Environment variable: NORMALIZEFILE_DIRECTORY_SEPARATOR. Default: "_".
  -h|--help         Shows help text. 
```

# Refresh Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll refreshmetadata --connection-string <value> [options]

DESCRIPTION
  Refresh metadata from files into the database

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: REFRESHMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: REFRESHMETADATA_ALBUM. 
  -h|--help         Shows help text. 
```

# Remove Tag Command
```
USAGE
  dotnet MiniMediaScanner.dll removetag --connection-string <value> [options]

DESCRIPTION
  Remove tags

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: REMOVETAG_ARTIST. 
  -b|--album        target Album Environment variable: REMOVETAG_ALBUM. 
  -t|--tag          The tag to remove from media. Environment variable: REMOVETAG_TAG. 
  -T|--tags         The tags to remove from media. Environment variable: REMOVETAG_TAGS. 
  -y|--confirm      Always confirm automatically. Environment variable: REMOVETAG_CONFIRM. Default: "False".
  -h|--help         Shows help text. 
```

# Rename File Command
```
USAGE
  dotnet MiniMediaScanner.dll renamefile --connection-string <value> [options]

DESCRIPTION
  Rename media files based on format

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: NORMALIZEFILE_ARTIST. 
  -b|--album        target Album Environment variable: NORMALIZEFILE_ALBUM. 
  -w|--overwrite    overwrite existing files. Environment variable: NORMALIZEFILE_OVERWRITE. Default: "False".
  -s|--sub-directory-depth  sub-directory depth to root-folder. Environment variable: NORMALIZEFILE_SUB_DIRECTORY_DEPTH. Default: "0".
  -f|--file-format  rename file format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}. Environment variable: NORMALIZEFILE_FILE_FORMAT. Default: "".
  -D|--directory-format  rename directory format (required for renaming) {MetadataId} {Path} {Title} {AlbumId} {ArtistName} {AlbumName} {Tag_AllJsonTags} {Tag_Track} {Tag_TrackCount} {Tag_Disc} {Tag_DiscCount}. Environment variable: NORMALIZEFILE_DIRECTORY_FORMAT. Default: "".
  -S|--directory-seperator  Directory Seperator replacer, replace '/' '\' to .e.g. '_'. Environment variable: NORMALIZEFILE_DIRECTORY_SEPARATOR. Default: "_".
  -h|--help         Shows help text. 
```

# Split Artist Command
```
USAGE
  dotnet MiniMediaScanner.dll splitartist --connection-string <value> --artist-format <value> [options]

DESCRIPTION
  Split an artist the best we can based on MusicBrainzArtistId tag, if multiple artists use the same name.
Tags available: MusicBrainzRemoteId, Name, Country, Type, Date

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -f|--artist-format  artist format for splitting the 2 artists apart. Environment variable: SPLITARTIST_ARTIST_FORMAT. 
  -a|--artist       Artistname Environment variable: SPLITARTIST_ARTIST. 
  -b|--album        target Album Environment variable: SPLITARTIST_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: SPLITARTIST_CONFIRM. Default: "False".
  -h|--help         Shows help text. 
```

# Split Tag Command
```
USAGE
  dotnet MiniMediaScanner.dll splittag --connection-string <value> --tag <value> [options]

DESCRIPTION
  Split the target media tag by the seperator

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -t|--tag          Tag. Environment variable: SPLITTAG_TAG. 
  -a|--artist       Artistname Environment variable: SPLITTAG_ARTIST. 
  -b|--album        target Album Environment variable: SPLITTAG_ALBUM. 
  -w|--write-tag    Tag to write to, if not set, the tag to read from (-t/--tag) is used to write to. Environment variable: SPLITTAG_WRITE_TAG. 
  -r|--update-read-tag  Update as well the tag that was being read. Environment variable: SPLITTAG_UPDATE_READ_TAG. Default: "False".
  -R|--update-read-tag-original-value  Update the read tag with the original tag value. Environment variable: SPLITTAG_UPDATE_READ_TAG_ORIGINAL_VALUE. Default: "False".
  -W|--update-write-tag-original-value  Update the read tag with the original tag value. Environment variable: SPLITTAG_UPDATE_WRITE_TAG_ORIGINAL_VALUE. Default: "False".
  -y|--confirm      Always confirm automatically. Environment variable: SPLITTAG_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: SPLITTAG_OVERWRITE_TAG. Default: "False".
  -s|--seperator    Split seperator. Environment variable: SPLITTAG_SEPARATOR. Default: ";".
  -h|--help         Shows help text. 
```

# Stats Command
```
USAGE
  dotnet MiniMediaScanner.dll stats --connection-string <value> [options]

DESCRIPTION
  Get statistics about your media in the database

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -h|--help         Shows help text. 
```

# Tag Missing Deezer Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll tagmissingdeezermetadata --connection-string <value> [options]

DESCRIPTION
  Tag missing metadata using Deezer, optionally write to file

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: TAGMISSINGDEEZERMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: TAGMISSINGDEEZERMETADATA_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: TAGMISSINGDEEZERMETADATA_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: TAGMISSINGDEEZERMETADATA_OVERWRITE_TAG. Default: "True".
  --overwrite-artist  Overwrite the Artist name when tagging from Deezer. Environment variable: TAGMISSINGDEEZERMETADATA_OVERWRITEARTIST. Default: "False".
  --overwrite-album-artist  Overwrite the Album Artist name when tagging from Deezer. Environment variable: TAGMISSINGDEEZERMETADATA_OVERWRITEALBUMARTIST. Default: "False".
  --overwrite-album  Overwrite the Album name when tagging from Deezer. Environment variable: TAGMISSINGDEEZERMETADATA_OVERWRITEALBUM. Default: "False".
  --overwrite-track  Overwrite the Track name when tagging from Deezer. Environment variable: TAGMISSINGDEEZERMETADATA_OVERWRITETRACK. Default: "False".
  -h|--help         Shows help text. 
```

# Tag Missing Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll tagmissingmetadata --connection-string <value> --accoustid <value> [options]

DESCRIPTION
  Tag missing metadata using AcousticBrainz, only tries already fingerprinted media, optionally write to file

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -A|--accoustid    AccoustId API Key, required for getting data from MusicBrainz. Environment variable: TAGMISSINGMETADATA_ACOUSTID. 
  -a|--artist       Artistname Environment variable: TAGMISSINGMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: TAGMISSINGMETADATA_ALBUM. 
  -w|--write        Write missing metadata to media on disk. Environment variable: TAGMISSINGMETADATA_WRITE. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: TAGMISSINGMETADATA_OVERWRITE_TAG. Default: "True".
  --match-percentage-tags  The percentage used for tagging, how accurate it must match with MusicBrainz. Environment variable: TAGMISSINGMETADATA_MATCH_PERCENTAGE_TAGS. Default: "80".
  --match-percentage-acoustid  The percentage used for AcoustId, how accurate it must match with AcoustId (for sound accuracy). Environment variable: TAGMISSINGMETADATA_MATCH_PERCENTAGE_ACOUSTID. Default: "98".
  -h|--help         Shows help text. 
```

# Tag Missing Spotify Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll tagmissingspotifymetadata --connection-string <value> [options]

DESCRIPTION
  Tag missing metadata using Spotify, optionally write to file

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: TAGMISSINGSPOTIFYMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: TAGMISSINGSPOTIFYMETADATA_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: TAGMISSINGSPOTIFYMETADATA_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: TAGMISSINGSPOTIFYMETADATA_OVERWRITE_TAG. Default: "True".
  --overwrite-artist  Overwrite the Artist name when tagging from Spotify. Environment variable: TAGMISSINGSPOTIFYMETADATA_OVERWRITEARTIST. Default: "False".
  --overwrite-album-artist  Overwrite the Album Artist name when tagging from Spotify. Environment variable: TAGMISSINGSPOTIFYMETADATA_OVERWRITEALBUMARTIST. Default: "False".
  --overwrite-album  Overwrite the Album name when tagging from Spotify. Environment variable: TAGMISSINGSPOTIFYMETADATA_OVERWRITEALBUM. Default: "False".
  --overwrite-track  Overwrite the Track name when tagging from Spotify. Environment variable: TAGMISSINGSPOTIFYMETADATA_OVERWRITETRACK. Default: "False".
  -h|--help         Shows help text. 
```

# Tag Missing Tidal Metadata Command
```
USAGE
  dotnet MiniMediaScanner.dll tagmissingtidalmetadata --connection-string <value> [options]

DESCRIPTION
  Tag missing metadata using Tidal, optionally write to file

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: TAGMISSINGTIDALMETADATA_ARTIST. 
  -b|--album        target Album Environment variable: TAGMISSINGTIDALMETADATA_ALBUM. 
  -y|--confirm      Always confirm automatically. Environment variable: GROUPTAGGINGTIDALMETADATA_CONFIRM. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: TAGMISSINGTIDALMETADATA_OVERWRITE_TAG. Default: "True".
  --overwrite-artist  Overwrite the Artist name when tagging from Tidal. Environment variable: TAGMISSINGTIDALMETADATA_OVERWRITEARTIST. Default: "False".
  --overwrite-album-artist  Overwrite the Album Artist name when tagging from Tidal. Environment variable: TAGMISSINGTIDALMETADATA_OVERWRITEALBUMARTIST. Default: "False".
  --overwrite-album  Overwrite the Album name when tagging from Tidal. Environment variable: TAGMISSINGTIDALMETADATA_OVERWRITEALBUM. Default: "False".
  --overwrite-track  Overwrite the Track name when tagging from Tidal. Environment variable: TAGMISSINGTIDALMETADATA_OVERWRITETRACK. Default: "False".
  -h|--help         Shows help text. 
```

# Untagged File Command
```
USAGE
  dotnet MiniMediaScanner.dll untaggedfile --connection-string <value> [options]

DESCRIPTION
  Get a list of untagged files from a artist

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: UNTAGGEDFILES_ARTIST. 
  -b|--album        target Album Environment variable: UNTAGGEDFILES_ALBUM. 
  -p|--providers    Providers can be MusicBrainz, Spotify, Tidal, Deezer. Environment variable: UNTAGGEDFILES_PROVIDERS. Default: "musicbrainz".
  -o|--output       Output format, tags available: {Artist} {Album} {Track} {ArtistUrl} {AlbumUrl} {TrackUrl}. Environment variable: UNTAGGEDFILES_OUTPUT. Default: "{Artist} - {Album} - {Track}".
  -F|--filterout    Filterout names from the output. Environment variable: UNTAGGEDFILES_FILTEROUT. Default: .
  --file            Save the missing tracks list to a file. Environment variable: UNTAGGEDFILES_FILE. 
  --file-append     Append to the file instead of a overwrite. Environment variable: UNTAGGEDFILES_FILE_APPEND. Default: "False".
  -h|--help         Shows help text. 
```


# Update Deezer Command
```
USAGE
  dotnet MiniMediaScanner.dll updatedeezer --connection-string <value> [options]

DESCRIPTION
  Update Deezer metadata

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artist filter to update. Environment variable: UPDATEDEEZER_ARTIST. 
  --proxy-file      HTTP/HTTPS Proxy/Proxies to use to access Deezer. Environment variable: PROXY_FILE. 
  --proxy           HTTP/HTTPS Proxy to use to access Deezer. Environment variable: PROXY. 
  --proxy-mode      Proxy Mode: Random, RoundRobin, StickyTillError, RotateTime, PerArtist. Environment variable: PROXY_MODE. Default: "StickyTillError".
  --save-track-token  Save the track_token that is returned by the Deezer API into the database, disabling this saves space in postgres. Environment variable: UPDATEDEEZER_SAVE_TRACK_TOKEN. Default: "True".
  --save-preview-url  Save the preview_url that is returned by the Deezer API into the database, disabling this saves space in postgres. Environment variable: UPDATEDEEZER_SAVE_PREVIEW_URL. Default: "True".
  --threads         The amount of threads to use. Environment variable: UPDATEDEEZER_THREADS. Default: "1".
  --prevent-update-within-days  Prevent updating existing artists within x days from the last pull/update Environment variable: UPDATEDEEZER_PREVENT_UPDATE_WITHIN_DAYS. Default: "7".
  -h|--help         Shows help text. 
```

# UpdateMB (MusicBrainz) Command
```
USAGE
  dotnet MiniMediaScanner.dll updatemb --connection-string <value> [options]

DESCRIPTION
  Update MusicBrainz metadata

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artist filter to update. Environment variable: UPDATEMB_ARTIST. 
  --prevent-update-within-days  Prevent updating existing artists within x days from the last pull/update Environment variable: UPDATEMB_PREVENT_UPDATE_WITHIN_DAYS. Default: "7".
  -h|--help         Shows help text. 
```

# Update Soundcloud Command
```
USAGE
  dotnet MiniMediaScanner.dll updatesoundcloud --connection-string <value> --client-id <value> [options]

DESCRIPTION
  Update SoundCloud metadata

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -c|--client-id    SoundCloud Client Id, to use for the SoundCloud API. Environment variable: UPDATESOUNDCLOUD_CLIENT_ID. 
  -a|--artist       Artist filter to update. Environment variable: UPDATESOUNDCLOUD_ARTIST. 
  --prevent-update-within-days  Prevent updating existing artists within x days from the last pull/update Environment variable: UPDATESOUNDCLOUD_PREVENT_UPDATE_WITHIN_DAYS. Default: "7".
  --artist-file     Read from a file line by line to import the artist names Environment variable: UPDATESOUNDCLOUD_ARTIST_FILE. 
  -h|--help         Shows help text. 
```

# Update Spotify Command (Broken unless you pay I guess)
```
USAGE
  dotnet MiniMediaScanner.dll updatespotify --connection-string <value> --spotify-client-id <values...> --spotify-secret-id <values...> [options]

DESCRIPTION
  Update Spotify metadata

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -c|--spotify-client-id  Spotify Client Id, to use for the Spotify API. Environment variable: UPDATESPOTIFY_SPOTIFY_CLIENT_ID. 
* -s|--spotify-secret-id  Spotify Secret Id, to use for the Spotify API. Environment variable: UPDATESPOTIFY_SPOTIFY_SECRET_ID. 
  -a|--artist       Artist filter to update. Environment variable: UPDATESPOTIFY_ARTIST. 
  -D|--api-delay    Api Delay in seconds after each API call to prevent rate limiting. Environment variable: UPDATESPOTIFY_API_DELAY. Default: "10".
  --prevent-update-within-days  Prevent updating existing artists within x days from the last pull/update Environment variable: UPDATESPOTIFY_PREVENT_UPDATE_WITHIN_DAYS. Default: "7".
  --artist-file     Read from a file line by line to import the artist names Environment variable: UPDATESPOTIFY_ARTIST_FILE. 
  -h|--help         Shows help text. 
```

# Update Tidal Command
```
USAGE
  dotnet MiniMediaScanner.dll updatetidal --connection-string <value> --tidal-client-id <values...> --tidal-secret-id <values...> [options]

DESCRIPTION
  Update Tidal metadata

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -c|--tidal-client-id  Tidal Client Id, to use for the Tidal API. Environment variable: UPDATETIDAL_TIDAL_CLIENT_ID. 
* -s|--tidal-secret-id  Tidal Secret Id, to use for the Tidal API. Environment variable: UPDATETIDAL_TIDAL_SECRET_ID. 
  -a|--artist       Artist filter to update. Environment variable: UPDATETIDAL_ARTIST. 
  -G|--country-code  Tidal's CountryCodes (e.g. US, FR, NL, DE etc). Environment variable: UPDATETIDAL_COUNTRY_CODE. 
  --proxy-file      HTTP/HTTPS Proxy/Proxies to use to access Deezer. Environment variable: PROXY_FILE. 
  --proxy           HTTP/HTTPS Proxy to use to access Deezer. Environment variable: PROXY. 
  --proxy-mode      Proxy Mode: Random, RoundRobin, StickyTillError, RotateTime, PerArtist. Environment variable: PROXY_MODE. Default: "StickyTillError".
  --prevent-update-within-days  Prevent updating existing artists within x days from the last pull/update Environment variable: UPDATETIDAL_PREVENT_UPDATE_WITHIN_DAYS. Default: "7".
  --artist-file     Read from a file line by line to import the artist names Environment variable: UPDATETIDAL_ARTIST_FILE. 
  --update-nonpulled-artists  Update artists that have not been pulled fully before, first Environment variable: UPDATETIDAL_NONPULLED_ARTISTS. Default: "False".
  --ignore-artist-album-amount  Ignore artists that have over a certain amount of albums, >500 albums is not normal Environment variable: UPDATETIDAL_IGNORE_ARTIST_ALBUM_AMOUNT. Default: "500".
  -h|--help         Shows help text. 
```
