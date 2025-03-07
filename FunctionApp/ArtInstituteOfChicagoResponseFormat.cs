using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtInstituteOfChicagoResponseFormat
{
    public static class ArtInstituteOfChicago
    {
        public static string GetImageUrl(string iiif_url, string image_id)
        {
            return $"{iiif_url}/{image_id}/full/843,/0/default.jpg";
        }
    }

    // Generated with https://json2csharp.com/

    public class ArtInstituteOfChicagoResponse
    {
        public object preference { get; set; }
        public Pagination pagination { get; set; }
        public List<Datum> data { get; set; }
        public Info info { get; set; }
        public Config config { get; set; }
    }

    public class Config
    {
        public string iiif_url { get; set; }
        public string website_url { get; set; }
    }

    public class Datum
    {
        public double _score { get; set; }
        public int id { get; set; }
        public string image_id { get; set; }
        public string title { get; set; }
    }

    public class Info
    {
        public string license_text { get; set; }
        public List<string> license_links { get; set; }
        public string version { get; set; }
    }

    public class Pagination
    {
        public int total { get; set; }
        public int limit { get; set; }
        public int offset { get; set; }
        public int total_pages { get; set; }
        public int current_page { get; set; }
    }
}
