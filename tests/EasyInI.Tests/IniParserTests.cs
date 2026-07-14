using System;
using System.Collections.Generic;
using Xunit;

namespace EasyIni.Tests
{
    public class IniParserTests
    {
        [Fact]
        public void Parse_SimpleKeyValue()
        {
            var ini = IniParser.Parse("key=value");
            Assert.Equal("value", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_SectionWithKeys()
        {
            var ini = IniParser.Parse(@"
[server]
host=localhost
port=3306
");
            Assert.Equal("localhost", ini["server"]["host"].Value);
            Assert.Equal("3306", ini["server"]["port"].Value);
        }

        [Fact]
        public void Parse_MultipleSections()
        {
            var ini = IniParser.Parse(@"
[server]
host=localhost

[database]
name=mydb
");
            Assert.True(ini.ContainsSection("server"));
            Assert.True(ini.ContainsSection("database"));
            Assert.Equal("localhost", ini["server"]["host"].Value);
            Assert.Equal("mydb", ini["database"]["name"].Value);
        }

        [Fact]
        public void Parse_GlobalSection()
        {
            var ini = IniParser.Parse(@"
app_name=MyApp
version=1.0

[server]
host=localhost
");
            Assert.Equal("MyApp", ini.Global["app_name"].Value);
            Assert.Equal("1.0", ini.Global["version"].Value);
            Assert.Equal("localhost", ini["server"]["host"].Value);
        }

        [Fact]
        public void Parse_Comments_Semicolon()
        {
            var ini = IniParser.Parse(@"
; This is a comment
key=value
");
            Assert.Equal("value", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_Comments_Hash()
        {
            var ini = IniParser.Parse(@"
# Hash comment
key=value
");
            Assert.Equal("value", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_InlineComment()
        {
            var ini = IniParser.Parse("key=value ; inline comment");
            var entry = ini.Global.GetEntry("key");
            Assert.Equal("value", entry.Value.Value);
            Assert.Equal("inline comment", entry.InlineComment);
        }

        [Fact]
        public void Parse_CommentPreservation_LeadingComments()
        {
            var ini = IniParser.Parse(@"
; Database configuration
; Modify with care
[server]
host=localhost
");
            var section = ini["server"];
            Assert.Equal(2, section.LeadingComments.Count);
            Assert.Equal(" Database configuration", section.LeadingComments[0]);
            Assert.Equal(" Modify with care", section.LeadingComments[1]);
        }

        [Fact]
        public void Parse_CommentPreservation_EntryLeadingComment()
        {
            var ini = IniParser.Parse(@"
[server]
; Connection hostname
host=localhost
; Connection port
port=3306
");
            var hostEntry = ini["server"].GetEntry("host");
            var portEntry = ini["server"].GetEntry("port");
            Assert.Single(hostEntry.LeadingComments);
            Assert.Equal(" Connection hostname", hostEntry.LeadingComments[0]);
            Assert.Single(portEntry.LeadingComments);
            Assert.Equal(" Connection port", portEntry.LeadingComments[0]);
        }

        [Fact]
        public void Parse_QuotedValues()
        {
            var ini = IniParser.Parse("key=\"value with spaces\"");
            Assert.Equal("value with spaces", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_QuotedValueWithSpecialChars()
        {
            var ini = IniParser.Parse("key=\"value;with=special#chars\"");
            Assert.Equal("value;with=special#chars", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_QuotedValueWithEscapedQuote()
        {
            var ini = IniParser.Parse("key=\"he said \"\"hello\"\"\"");
            Assert.Equal("he said \"hello\"", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_WhitespaceTrimming()
        {
            var ini = IniParser.Parse("  key  =  value  ");
            Assert.Equal("key", ini.Global.GetEntry("key").Key);
            Assert.Equal("value", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_EmptyKeyValue()
        {
            var ini = IniParser.Parse("key=");
            Assert.Equal("", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_CaseInsensitive_Default()
        {
            var ini = IniParser.Parse(@"
[SERVER]
host=localhost

[server]
port=3306
");
            // Sections merge (case-insensitive)
            Assert.True(ini.ContainsSection("SERVER"));
            Assert.True(ini.ContainsSection("server"));
            Assert.Equal("localhost", ini["server"]["host"].Value);
            Assert.Equal("3306", ini["server"]["port"].Value);
        }

        [Fact]
        public void Parse_CaseSensitive()
        {
            var options = new IniParseOptions { CaseSensitive = true };
            var ini = IniParser.Parse(@"
[SERVER]
host=localhost

[server]
port=3306
", options);

            Assert.True(ini.ContainsSection("SERVER"));
            Assert.True(ini.ContainsSection("server"));
            Assert.Equal("localhost", ini["SERVER"]["host"].Value);
            Assert.Equal("3306", ini["server"]["port"].Value);
        }

        [Fact]
        public void Parse_DuplicateKey_Overwrite()
        {
            var options = new IniParseOptions { DuplicateKeyBehavior = DuplicateKeyBehavior.Overwrite };
            var ini = IniParser.Parse("key=first\nkey=second", options);
            Assert.Equal("second", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_DuplicateKey_Throw()
        {
            var options = new IniParseOptions { DuplicateKeyBehavior = DuplicateKeyBehavior.Throw };
            Assert.Throws<IniParseException>(() =>
                IniParser.Parse("key=first\nkey=second", options));
        }

        [Fact]
        public void Parse_DuplicateKey_Ignore()
        {
            var options = new IniParseOptions { DuplicateKeyBehavior = DuplicateKeyBehavior.Ignore };
            var ini = IniParser.Parse("key=first\nkey=second", options);
            Assert.Equal("first", ini.Global["key"].Value);
        }

        [Fact]
        public void Parse_EmptyString()
        {
            var ini = IniParser.Parse("");
            Assert.NotNull(ini);
            Assert.Equal(0, ini.Global.Count);
            Assert.Equal(0, ini.SectionCount);
        }

        [Fact]
        public void Parse_OnlyComments()
        {
            var ini = IniParser.Parse("; Just a comment");
            Assert.NotNull(ini);
            Assert.Equal(0, ini.Global.Count);
        }

        [Fact]
        public void TryParse_Success()
        {
            IniData result;
            string error;
            bool success = IniParser.TryParse("key=value", out result, out error);
            Assert.True(success);
            Assert.Null(error);
            Assert.Equal("value", result.Global["key"].Value);
        }

        [Fact]
        public void TryParse_Failure()
        {
            IniData result;
            string error;
            bool success = IniParser.TryParse("key=first\nkey=second",
                new IniParseOptions { DuplicateKeyBehavior = DuplicateKeyBehavior.Throw },
                out result, out error);
            Assert.False(success);
            Assert.NotNull(error);
            Assert.Null(result);
        }

        [Fact]
        public void ToString_RoundTrip()
        {
            var original = @"
; Server configuration
[server]
host=localhost
port=3306
";
            var ini = IniParser.Parse(original);
            var output = ini.ToString();

            var reparsed = IniParser.Parse(output);
            Assert.Equal("localhost", reparsed["server"]["host"].Value);
            Assert.Equal("3306", reparsed["server"]["port"].Value);
        }

        [Fact]
        public void ToString_PreservesComments()
        {
            var original = @"
; Database config
[server]
host=localhost
; The port number
port=3306 ; default port
";
            var ini = IniParser.Parse(original);
            var output = ini.ToString();

            Assert.Contains("; Database config", output);
            Assert.Contains("; The port number", output);
            Assert.Contains("; default port", output);
        }

        [Fact]
        public void ToString_QuotesSpecialValues()
        {
            var ini = new IniData();
            ini.Global["path"] = new IniValue("C:\\Program Files;special");
            var output = ini.ToString();
            Assert.Contains("\"", output);
        }

        [Fact]
        public void PartiallyModify_ThenSave()
        {
            var original = @"
; Top comment
[app]
name=MyApp
version=1.0
; Performance settings
[performance]
threads=4
timeout=30
";
            var ini = IniParser.Parse(original);
            ini["app"]["version"] = new IniValue("2.0");
            ini["performance"]["threads"] = new IniValue("8");

            var output = ini.ToString();
            Assert.Contains("; Top comment", output);
            Assert.Contains("version=2.0", output);
            Assert.Contains("threads=8", output);
            Assert.Contains("timeout=30", output);
            Assert.Contains("; Performance settings", output);
        }

        [Fact]
        public void AddNewSection_AndSave()
        {
            var ini = new IniData();
            ini.Global["app_name"] = new IniValue("MyApp");
            ini.AddOrGetSection("server").Set("host", "localhost");

            var output = ini.ToString();
            Assert.Contains("app_name=MyApp", output);
            Assert.Contains("[server]", output);
            Assert.Contains("host=localhost", output);
        }

        [Fact]
        public void Section_CountAndKeys()
        {
            var ini = IniParser.Parse(@"
[section1]
a=1
b=2
[section2]
c=3
");
            Assert.Equal(2, ini["section1"].Count);
            var keys = new List<string>(ini["section1"].Keys);
            Assert.Contains("a", keys);
            Assert.Contains("b", keys);
        }
    }
}
