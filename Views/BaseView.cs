using System;
using System.Collections.Generic;
using System.Linq;
using System.Mvc;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KTPM.Views
{
    internal class BaseView<TView> : IView
        where TView : UIElement, new()
    {
        private TView _view;

        public BaseView()
        {
            _view = new TView();
        }

        public void Render(object model)
        {
            // Do nothing
        }

        public object Content
        {
            get { return _view; }
        }

        public ViewDataDictionary ViewBag { get; set; }
    }
}
