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

            result.Add(new Data { Site = "site", User = "username", Password = "password", });
            result.Add(new Data { Site = "site", User = "username", Password = "password", });
            result.Add(new Data { Site = "site", User = "username", Password = "password", });
            result.Add(new Data { Site = "site", User = "username", Password = "password", });
            result.Add(new Data { Site = "site", User = "username", Password = "password", });
            result.Add(new Data { Site = "site", User = "username", Password = "password", });
            result.Add(new Data { Site = "site", User = "username", Password = "password", });
            result.Add(new Data { Site = "site", User = "username", Password = "password", });

            return result;
        }
    }
}
