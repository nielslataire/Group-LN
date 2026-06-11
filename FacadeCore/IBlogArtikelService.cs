using BOCore;

namespace FacadeCore
{
    public interface IBlogArtikelService
    {
        GetResponse<BlogArtikelBO> GetArtikelen(bool alleenGepubliceerd = false);
        GetResponse<BlogArtikelBO> GetArtikelById(int id);
        GetResponse<BlogArtikelBO> GetArtikelBySlug(string slug);
        Response InsertUpdate(BlogArtikelBO bo);
        Response InsertUpdateBlok(BlogArtikelBlokBO bo);
        Response DeleteArtikel(int id);
        Response DeleteBlok(int id);
    }
}
