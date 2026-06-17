using BOCore;
using System.Collections.Generic;

namespace FacadeCore
{
    public interface IBlogArtikelService
    {
        GetResponse<BlogArtikelBO> GetArtikelen(bool alleenGepubliceerd = false);
        GetResponse<BlogArtikelBO> GetArtikelById(int id);
        GetResponse<BlogArtikelBO> GetArtikelBySlug(string slug);
        Response InsertUpdate(BlogArtikelBO bo);
        Response InsertUpdateBlok(BlogArtikelBlokBO bo);
        Response UpdateBlokkenVolgorde(int artikelId, List<int> sortedIds);
        Response InsertUpdateFaq(BlogArtikelFaqBO bo);
        Response UpdateFaqVolgorde(int artikelId, List<int> sortedIds);
        Response DeleteArtikel(int id);
        Response DeleteBlok(int id);
        Response DeleteFaq(int id);
    }
}
