using System;

namespace auth_proxy
{
    public class Destination
    {
        public Uri Uri { get; set; }
        public bool RequiresAuthentication { get; set; }
        public string Role { get; set; }
    }
}