using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WishLister.Utils;

public interface IServiceContainer
{
    T GetService<T>();
}
