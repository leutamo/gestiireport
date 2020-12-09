using System;
using CommandLine;

namespace Gestii
{
    class Options
    {        
        [Option("client", HelpText = "the client (the first part of your url)")]
        public string Client { get; set; }

        [Option]
        public string ApiKey { get; set; }

        [Option]
        public string Empresa { get; set; }

        [Option]
        public string User { get; set; }

        [Option]
        public string Password { get; set; }

        [Option]
        public bool Verbose { get; set; }

        [Option("lastid", Default = 0, HelpText = "Set the lastid proccesed")]
        public int LastId { get; set; }

        [Option]
        public string Directory { get; set; }

        [Option("save", Default = true, HelpText = "Set this to false if you dont want to remember the key or the last id")]
        public bool Save { get; set; }

    }
}