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
- [ ] Implement Deezer's API for Tags
- [ ] Improve Missing songs command
- [ ] Command to cleanup/merge tags (e.g. AlbumArtist, album_artist...)

# Features
1. Postgres support
2. Import Full/Partially your entire library multi-threaded
3. MusicBrainz support (+cached in Postgres)
4. Spotify support (+cached in Postgres)
5. Tidal support (+cached in Postgres)
6. All commands are multi-threaded for performance
7. Low memory footprint

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
11. TagMissingSpotifyMetadata - Add missing tags to your media files from Spotify
12. EqualizeMetadata - Equalize (set the same) tag value for the entire album of an artist (mostly to fix issues with albums showing weird/duplicated in Plex/Navidrome etc)
13. FixVersioning - Find media that are using the same track/disc numbering, usually normal version and (AlbumVersion), (Live) etc
    The media with the longest file name and contains TrackFilters will get incremented disc number
    This will make it so the normal version of the album stays at disc 1 but remix(etc) gets disc number 1001+
14. Cover ArtArchive - Grab covers from the CoverArtArchive and save them as cover.jpg
15. Cover Extract - Extract the covers directly from the media files and save them as cover.jpg
16. RemoveTag - Remove specific tags from Artist/Albums
17. RefreshMetadata - Simply do a quick refresh of the metadata for an artist/album
18. SplitArtist - Split Artist is kind of experimental, it will try to split the 2 artist's that have the same name apart into 2 seperate artists
19. Stats - Show basic stats of your database
20. UpdateSpotify - Update/Add artists into the database from Spotify's API
21. SplitTag - Split specific tag's into single value fields by specific seperator like ';'
22. UpdateTidal - Update/Add artists into the database from Tidal's API
23. FixCollections - Fix collections by adding the missing artist to the Artists tag

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
  Check for duplicated music and delete optionally

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: DEDUPLICATE_ARTIST. 
  -d|--delete       Delete duplicate file Environment variable: DEDUPLICATE_DELETE. Default: "False".
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
  dotnet MiniMediaScanner.dll fixcollections --connection-string <value> [options]

DESCRIPTION
  Fix collections by adding the missing artist to the Artists tag

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
  -a|--artist       Artistname Environment variable: FIXCOLLECTIONS_ARTIST. 
  -l|--label        Target label to find songs belonging to a collection Environment variable: FIXCOLLECTIONS_LABEL. 
  -H|--copyright    Target copyright to find songs belonging to a collection. Environment variable: FIXCOLLECTIONS_COPYRIGHT.
  -b|--albumregex   Target album(s) with regex. Environment variable: FIXCOLLECTIONS_ALBUMREGEX. 
  -W|--addartist    Add the missing artist to the Artists tag.  Environment variable: FIXCOLLECTIONS_ADDARTIST. 
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

# Import Command
```
USAGE
  dotnet MiniMediaScanner.dll import --connection-string <value> --path <value> [options]

DESCRIPTION
  Import music to your database

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -p|--path         From the directory. Environment variable: IMPORT_PATH. 
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
  -w|--write        Write missing metadata to media on disk. Environment variable: TAGMISSINGSPOTIFYMETADATA_WRITE. Default: "False".
  -o|--overwrite-tag  Overwrite existing tag values. Environment variable: TAGMISSINGSPOTIFYMETADATA_OVERWRITE_TAG. Default: "True".
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
  -h|--help         Shows help text. 
```

# Update Spotify Command
```
USAGE
  dotnet MiniMediaScanner.dll updatespotify --connection-string <value> --spotify-client-id <value> --spotify-secret-id <value> [options]

DESCRIPTION
  Update Spotify metadata

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -c|--spotify-client-id  Spotify Client Id, to use for the Spotify API. Environment variable: UPDATESPOTIFY_SPOTIFY_CLIENT_ID. 
* -s|--spotify-secret-id  Spotify Secret Id, to use for the Spotify API. Environment variable: UPDATESPOTIFY_SPOTIFY_SECRET_ID. 
  -a|--artist       Artist filter to update. Environment variable: UPDATESPOTIFY_ARTIST. 
  -D|--api-delay    Api Delay in seconds after each API call to prevent rate limiting. Environment variable: UPDATESPOTIFY_API_DELAY. Default: "10".
  -h|--help         Shows help text. 
```


# Update Tidal Command
```
USAGE
  dotnet MiniMediaScanner.dll updatetidal --connection-string <value> --tidal-client-id <value> --tidal-secret-id <value> [options]

DESCRIPTION
  Update Tidal metadata

OPTIONS
* -C|--connection-string  ConnectionString for Postgres database. Environment variable: CONNECTIONSTRING. 
* -c|--tidal-client-id  Tidal Client Id, to use for the Tidal API. Environment variable: UPDATETIDAL_TIDAL_CLIENT_ID. 
* -s|--tidal-secret-id  Tidal Secret Id, to use for the Tidal API. Environment variable: UPDATETIDAL_TIDAL_SECRET_ID. 
  -a|--artist       Artist filter to update. Environment variable: UPDATETIDAL_ARTIST. 
  -G|--country-code  Tidal's CountryCode (e.g. US, FR, NL, DE etc). Environment variable: UPDATETIDAL_COUNTRY_CODE. Default: "US".
  -h|--help         Shows help text. 

```
