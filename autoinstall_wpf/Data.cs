using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace autoinstall_wpf
{
    class Data
    {
        public string Site { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string link { get; set; }

        public static List<Data> getData()
        {
            List<Data> result = new List<Data>();

            result.Add(new Data { Site = "mistwo", User = "bkotwo", Password = "bkotwo", });
            result.Add(new Data { Site = "misone", User = "bkoone", Password = "bkoone", });
            result.Add(new Data { Site = "bko-dis", User = "bkoces", Password = "bkoces", });
            result.Add(new Data { Site = "serv1osm", User = "bkoosm", Password = "bkoosm", });
            result.Add(new Data { Site = "bko-mis", User = "bkoprop", Password = "bkoprop", });
            result.Add(new Data { Site = "misdou", User = "bkodou", Password = "bkodou", });
            result.Add(new Data { Site = "webcsp", User = "bkocsp", Password = "bkocsp", });
            result.Add(new Data { Site = "webdou2", User = "bkodou2", Password = "bkodou2", });

            return result;
        }
    }
}
