﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Miko
{
    public partial class Toolbar
    {
        [Parameter] public RenderFragment ChildContent { get; set; }
    }
}
