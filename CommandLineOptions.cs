#nullable enable

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace acsetup
{
    class CommandLineOptions
    {
        [Option('p', "tmpl-dest", Required = false, HelpText = "テンプレートの出力先フォルダ")]
        public string TemplateDest { get; set; } = ".";

        [Option('t', "test-dest", Required = false, HelpText = "テストの出力先フォルダ")]
        public string TestDest { get; set; } = ".";
    }
}
