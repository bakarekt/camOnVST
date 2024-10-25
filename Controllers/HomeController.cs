using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KTPM.Controllers
{
    internal class HomeController : BaseController
    {
        public object Index()
        {
            return View();
        }
    }
}
