using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiDemo.Models
{
    public class ResultPageModel
    {
        public string ImageName { get; set; }

        private ResultPageModel() { }
        public ResultPageModel(string imgName)
        {
            ImageName = imgName;
        }
    }
}
