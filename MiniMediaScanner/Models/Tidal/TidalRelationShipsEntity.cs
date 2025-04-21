namespace MiniMediaScanner.Models.Tidal;

public class TidalRelationShipsEntity
{
    public TidalAlbumItemsEntity Items { get; set; }
    public TidalRelationShipsAlbumsEntity Albums { get; set; }
    public TidalRelationShipsArtistsEntity Artists { get; set; }
}