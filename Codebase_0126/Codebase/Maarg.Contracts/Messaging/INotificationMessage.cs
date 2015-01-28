using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Maarg.Contracts
{
    public interface INotificationMessage
    {
        string TemplateId { get; set; }
        IDictionary<string, string> ParameterMap { get; }
        Stream Body { get; set; }
        RoutingInfo RoutingInfo { get; set; }
    }
}
