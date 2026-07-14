using System;
using System.Text;

namespace EasyInI.Samples
{
    // === POCO models for strongly-typed mapping demo ===

    [IniSection("server")]
    public class ServerConfig
    {
        [IniProperty("host")]
        public string Host { get; set; }

        [IniProperty("port")]
        public int Port { get; set; }

        [IniProperty("ssl")]
        public bool UseSsl { get; set; }

        [IniProperty("timeout")]
        public int Timeout { get; set; }
    }

    [IniSection("app")]
    public class MultiSectionAppConfig
    {
        [IniProperty("name", Section = "app")]
        public string AppName { get; set; }

        [IniProperty("version", Section = "app")]
        public string Version { get; set; }

        [IniProperty("host", Section = "database")]
        public string DbHost { get; set; }

        [IniProperty("port", Section = "database")]
        public int DbPort { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== EasyInI Samples ===\n");

            Sample1_BasicParsing();
            Sample2_DictionaryStyleAccess();
            Sample3_TypeConversion();
            Sample4_CommentPreservation();
            Sample5_PartialModifyAndSave();
            Sample6_QuotedValues();
            Sample7_CaseSensitivity();
            Sample8_DuplicateKeyHandling();
            Sample9_StronglyTypedDeserialization();
            Sample10_MultiSectionMapping();
            Sample11_ManualConstruction();
            Sample12_TryParse();
            Sample13_TypedAccessWithDefaults();
            Sample14_CreateFromScratch();

            Sample15_HashComments();
            Sample16_RemoveEntry();
            Sample17_NamesWithSpaces();
            Sample18_EmptySection();
            Sample19_CommentsOnly();
            Sample20_SectionMerge();
            Sample21_NullAndEmptyValues();
            Sample22_KeyWithoutValue();
            Sample23_TrailingComments();
            Sample24_FileRoundTrip();
            Sample25_EncodingSupport();
            Sample26_DeepCommentBlocks();
            Sample27_SectionInlineComment();

            Console.WriteLine("\nAll samples completed.");
            Console.ReadLine();
        }

        /// <summary>
        /// 1. Basic INI parsing: sections, keys, global entries.
        /// </summary>
        static void Sample1_BasicParsing()
        {
            Console.WriteLine("--- 1. Basic Parsing ---");

            var ini = IniParser.Parse(@"
app_name=EasyInI
version=1.0

[server]
host=localhost
port=3306
");

            Console.WriteLine("Global app_name = " + ini.Global["app_name"]);
            Console.WriteLine("Global version  = " + ini.Global["version"]);
            Console.WriteLine("Server host     = " + ini["server"]["host"]);
            Console.WriteLine("Server port     = " + ini["server"]["port"]);
            Console.WriteLine("Section count   = " + ini.SectionCount);
            Console.WriteLine();
        }

        /// <summary>
        /// 2. Dictionary-style access: indexer, ContainsKey, iteration.
        /// </summary>
        static void Sample2_DictionaryStyleAccess()
        {
            Console.WriteLine("--- 2. Dictionary-Style Access ---");

            var ini = IniParser.Parse(@"
[db]
host=192.168.1.1
port=5432
max_conn=100
");

            var db = ini["db"];

            // Check key existence
            Console.WriteLine("Contains 'host': " + db.ContainsKey("host"));
            Console.WriteLine("Contains 'missing': " + db.ContainsKey("missing"));

            // Iterate all entries
            foreach (var entry in db)
            {
                Console.WriteLine("  " + entry.Key + " = " + entry.Value);
            }

            // Get entry object (includes comments)
            var hostEntry = db.GetEntry("host");
            Console.WriteLine("Entry key: " + hostEntry.Key + ", value: " + hostEntry.Value);
            Console.WriteLine();
        }

        /// <summary>
        /// 3. Type conversion: Get&lt;T&gt;, implicit operators, enums.
        /// </summary>
        static void Sample3_TypeConversion()
        {
            Console.WriteLine("--- 3. Type Conversion ---");

            var ini = IniParser.Parse(@"
[settings]
retries=3
enabled=true
ratio=0.75
mode=Production
timeout=
");

            // Typed Get<T>
            int retries = ini["settings"].Get<int>("retries");
            bool enabled = ini["settings"].Get<bool>("enabled");
            double ratio = ini["settings"].Get<double>("ratio");
            string mode = ini["settings"].Get<string>("mode");

            Console.WriteLine("retries = " + retries + " (int)");
            Console.WriteLine("enabled = " + enabled + " (bool)");
            Console.WriteLine("ratio   = " + ratio + " (double)");
            Console.WriteLine("mode    = " + mode + " (string)");

            // Implicit conversions via IniValue
            IniValue v = ini["settings"]["retries"];
            int retries2 = v;  // implicit int
            Console.WriteLine("Implicit int: " + retries2);

            // Get with default — key doesn't exist
            int missing = ini["settings"].Get<int>("missing_key", -1);
            Console.WriteLine("missing_key default: " + missing);

            // Get with default — conversion failure
            int bad = ini["settings"].Get<int>("enabled", -1);  // "true" -> int fails
            Console.WriteLine("bad conversion default: " + bad);
            Console.WriteLine();
        }

        /// <summary>
        /// 4. Comment preservation — leading and inline comments survive round-trips.
        /// </summary>
        static void Sample4_CommentPreservation()
        {
            Console.WriteLine("--- 4. Comment Preservation ---");

            var original = @"
; Server configuration
; Edit with caution
[server]
; Connection address
host=localhost
port=3306 ; default port
timeout=30
";

            Console.WriteLine("=== Original ===");
            Console.WriteLine(original);

            var ini = IniParser.Parse(original);

            // Inspect comments
            var section = ini["server"];
            Console.WriteLine("Section leading comments: " + section.LeadingComments.Count);
            foreach (var c in section.LeadingComments)
                Console.WriteLine("  ;" + c);

            var portEntry = section.GetEntry("port");
            Console.WriteLine("Port inline comment: " + portEntry.InlineComment);

            var hostEntry = section.GetEntry("host");
            Console.WriteLine("Host leading comments: " + hostEntry.LeadingComments.Count);

            // Modify a value
            ini["server"]["port"] = new IniValue("5432");

            Console.WriteLine("\n=== After modifying port -> 5432 ===");
            Console.WriteLine(ini.ToString());
            Console.WriteLine();
        }

        /// <summary>
        /// 5. Partial modification — change one value, everything else stays intact.
        /// </summary>
        static void Sample5_PartialModifyAndSave()
        {
            Console.WriteLine("--- 5. Partial Modify & Save ---");

            var original = @"
; Application settings
[app]
name=MyApp
version=1.0
; Window settings
[window]
width=1024
height=768
fullscreen=false
";

            var ini = IniParser.Parse(original);

            // Modify just a few values
            ini["app"]["version"] = new IniValue("2.0");
            ini["window"]["fullscreen"] = new IniValue("true");
            ini["window"]["width"] = new IniValue("1920");

            // Add a new key
            ini["window"].Set("maximized", "true");

            // Add a new section
            ini.AddOrGetSection("logging").Set("level", "debug");

            Console.WriteLine("=== After partial modifications ===");
            Console.WriteLine(ini.ToString());
            Console.WriteLine();
        }

        /// <summary>
        /// 6. Quoted values — values with special chars are quoted on save.
        /// </summary>
        static void Sample6_QuotedValues()
        {
            Console.WriteLine("--- 6. Quoted Values ---");

            var ini = IniParser.Parse(@"
[paths]
program_files=""C:\Program Files;special""
connection=""Server=local;Port=3306;Database=mydb""
");

            // Read quoted values
            Console.WriteLine("program_files = " + ini["paths"]["program_files"]);
            Console.WriteLine("connection    = " + ini["paths"]["connection"]);

            // Write a value that needs quoting
            var data = new IniData();
            data.AddOrGetSection("paths")
                .Set("custom", "C:\\MyApp\\bin;version=2.0");

            string output = data.ToString();
            Console.WriteLine("\nQuoted output:\n" + output);

            // Re-parse should recover the original
            var reparse = IniParser.Parse(output);
            Console.WriteLine("Reparsed: " + reparse["paths"]["custom"]);
            Console.WriteLine();
        }

        /// <summary>
        /// 7. Case-sensitivity — configure per parse.
        /// </summary>
        static void Sample7_CaseSensitivity()
        {
            Console.WriteLine("--- 7. Case Sensitivity ---");

            var iniText = @"
[SERVER]
HOST=prod.example.com
[server]
host=dev.example.com
";

            // Default: case-insensitive — sections merge
            var ci = IniParser.Parse(iniText);
            Console.WriteLine("Case-insensitive (default):");
            Console.WriteLine("  Section count: " + ci.SectionCount); // 1
            Console.WriteLine("  host = " + ci["server"]["host"]);    // dev.example.com (overwritten)

            // Case-sensitive — separate sections
            var csOptions = new IniParseOptions { CaseSensitive = true };
            var cs = IniParser.Parse(iniText, csOptions);
            Console.WriteLine("Case-sensitive:");
            Console.WriteLine("  Section count: " + cs.SectionCount); // 2
            Console.WriteLine("  SERVER.HOST = " + cs["SERVER"]["HOST"]);
            Console.WriteLine("  server.host = " + cs["server"]["host"]);
            Console.WriteLine();
        }

        /// <summary>
        /// 8. Duplicate key handling — Overwrite, Throw, Ignore.
        /// </summary>
        static void Sample8_DuplicateKeyHandling()
        {
            Console.WriteLine("--- 8. Duplicate Key Handling ---");

            var dupText = "name=Alice\nname=Bob\n";

            // Overwrite (default)
            var overwrite = IniParser.Parse(dupText);
            Console.WriteLine("Overwrite: name = " + overwrite.Global["name"]); // Bob

            // Ignore
            var ignoreOpts = new IniParseOptions { DuplicateKeyBehavior = DuplicateKeyBehavior.Ignore };
            var ignore = IniParser.Parse(dupText, ignoreOpts);
            Console.WriteLine("Ignore: name = " + ignore.Global["name"]); // Alice

            // Throw
            var throwOpts = new IniParseOptions { DuplicateKeyBehavior = DuplicateKeyBehavior.Throw };
            try
            {
                IniParser.Parse(dupText, throwOpts);
                Console.WriteLine("Throw: should not reach here");
            }
            catch (IniParseException ex)
            {
                Console.WriteLine("Throw: " + ex.Message);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 9. Strongly-typed deserialization — map INI to POCO.
        /// </summary>
        static void Sample9_StronglyTypedDeserialization()
        {
            Console.WriteLine("--- 9. Strongly-Typed Deserialization ---");

            var ini = IniParser.Parse(@"
[server]
host=api.example.com
port=443
ssl=true
timeout=30
");

            var config = IniSerializer.Deserialize<ServerConfig>(ini);

            Console.WriteLine("Host:    " + config.Host);
            Console.WriteLine("Port:    " + config.Port);
            Console.WriteLine("SSL:     " + config.UseSsl);
            Console.WriteLine("Timeout: " + config.Timeout);

            // Serialize back
            config.Port = 8080;
            config.UseSsl = false;
            var roundTripped = IniSerializer.SerializeToString(config);
            Console.WriteLine("\nSerialized back:\n" + roundTripped);

            // Deserialize again to verify
            var restored = IniSerializer.Deserialize<ServerConfig>(roundTripped);
            Console.WriteLine("Restored port: " + restored.Port + ", ssl: " + restored.UseSsl);
            Console.WriteLine();
        }

        /// <summary>
        /// 10. Multi-section mapping — properties across different sections.
        /// </summary>
        static void Sample10_MultiSectionMapping()
        {
            Console.WriteLine("--- 10. Multi-Section Mapping ---");

            var ini = IniParser.Parse(@"
[app]
name=SuperApp
version=3.2.1
[database]
host=db.internal
port=5432
");

            var config = IniSerializer.Deserialize<MultiSectionAppConfig>(ini);
            Console.WriteLine("App:  " + config.AppName + " v" + config.Version);
            Console.WriteLine("DB:   " + config.DbHost + ":" + config.DbPort);

            // Serialize the multi-section config
            var text = IniSerializer.SerializeToString(config);
            Console.WriteLine("\nSerialized multi-section:\n" + text);
            Console.WriteLine();
        }

        /// <summary>
        /// 11. Manual construction — build IniData programmatically.
        /// </summary>
        static void Sample11_ManualConstruction()
        {
            Console.WriteLine("--- 11. Manual Construction ---");

            var ini = new IniData();

            // Global entries
            ini.Global["created_by"] = new IniValue("EasyInI Samples");
            ini.Global["created_at"] = new IniValue(DateTime.Now.ToString("yyyy-MM-dd"));

            // Server section
            var server = ini.AddSection("server");
            server.Set("host", "example.com");
            server.Set("port", "443");
            server.Set("ssl", "true");

            // Add comments to an entry
            var hostEntry = server.GetEntry("host");
            hostEntry.LeadingComments.Add(" Production server");
            hostEntry.InlineComment = "do not change";

            Console.WriteLine(ini.ToString());
            Console.WriteLine();
        }

        /// <summary>
        /// 12. Safe parsing with TryParse.
        /// </summary>
        static void Sample12_TryParse()
        {
            Console.WriteLine("--- 12. TryParse (Safe Parsing) ---");

            // Valid input
            IniData result;
            string error;
            if (IniParser.TryParse("key=value", out result, out error))
            {
                Console.WriteLine("Valid parse OK: " + result.Global["key"]);
            }

            // Invalid input with throw behavior
            var opts = new IniParseOptions { DuplicateKeyBehavior = DuplicateKeyBehavior.Throw };
            if (!IniParser.TryParse("a=1\na=2", opts, out result, out error))
            {
                Console.WriteLine("Invalid parse caught: " + error);
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 13. Typed access with defaults — safe Get&lt;T&gt; with fallback.
        /// </summary>
        static void Sample13_TypedAccessWithDefaults()
        {
            Console.WriteLine("--- 13. Typed Access with Defaults ---");

            var ini = IniParser.Parse(@"
[server]
host=localhost
port=3306
");

            // Section-level default
            int timeout = ini["server"].Get<int>("timeout", 30);
            Console.WriteLine("timeout (default): " + timeout);

            // Data-level default (section missing)
            int port = ini.Get<int>("missing_section", "port", 8080);
            Console.WriteLine("missing_section.port (default): " + port);

            // Data-level default (key exists)
            int realPort = ini.Get<int>("server", "port", -1);
            Console.WriteLine("server.port (real): " + realPort);
            Console.WriteLine();
        }

        /// <summary>
        /// 14. Create INI from scratch programmatically.
        /// </summary>
        static void Sample14_CreateFromScratch()
        {
            Console.WriteLine("--- 14. Create From Scratch ---");

            var ini = new IniData();

            // Add sections
            ini.AddOrGetSection("app").Set("name", "NewApp");
            ini.AddOrGetSection("app").Set("version", "0.0.1");

            var db = ini.AddOrGetSection("database");
            db.Set("host", "localhost");
            db.Set("port", "5432");
            db.Set("name", "mydb");
            db.Set("pooling", "true");

            var cache = ini.AddOrGetSection("cache");
            cache.Set("provider", "redis");
            cache.Set("ttl", "3600");
            cache.Set("prefix", "app:");

            Console.WriteLine(ini.ToString());
            Console.WriteLine();

            // Remove a section
            ini.RemoveSection("cache");
            Console.WriteLine("After removing [cache] section:");
            Console.WriteLine(ini.ToString());
            Console.WriteLine();
        }

        /// <summary>
        /// 15. Hash comments — # style comments work same as ;
        /// </summary>
        static void Sample15_HashComments()
        {
            Console.WriteLine("--- 15. Hash Comments (# style) ---");

            var ini = IniParser.Parse(@"
# Server configuration
# Edit carefully
[server]
# bind address
host=0.0.0.0
port=8080  # default HTTP port
");
            Console.WriteLine("Section comments:");
            foreach (var c in ini["server"].LeadingComments)
                Console.WriteLine("  #" + c);

            var portEntry = ini["server"].GetEntry("port");
            Console.WriteLine("Port inline comment: " + portEntry.InlineComment);
            Console.WriteLine("Port value: " + portEntry.Value);
            Console.WriteLine();
        }

        /// <summary>
        /// 16. Remove entry from section.
        /// </summary>
        static void Sample16_RemoveEntry()
        {
            Console.WriteLine("--- 16. Remove Entry ---");

            var ini = IniParser.Parse(@"
[app]
name=OldName
version=1.0
deprecated=yes
");

            Console.WriteLine("Before remove, count: " + ini["app"].Count);
            bool removed = ini["app"].Remove("deprecated");
            Console.WriteLine("Removed 'deprecated': " + removed);
            Console.WriteLine("Contains 'deprecated': " + ini["app"].ContainsKey("deprecated"));
            Console.WriteLine("After remove, count: " + ini["app"].Count);
            Console.WriteLine("Keys: " + string.Join(", ", ini["app"].Keys));
            Console.WriteLine();
        }

        /// <summary>
        /// 17. Section name with spaces, key with spaces.
        /// </summary>
        static void Sample17_NamesWithSpaces()
        {
            Console.WriteLine("--- 17. Names with Spaces ---");

            var ini = IniParser.Parse(@"
[my server]
host name=production
port number=8080
");
            // Section name is trimmed to "my server"
            AssertSection(ini, "my server", "host name", "production");
            AssertSection(ini, "my server", "port number", "8080");
            Console.WriteLine("OK: section 'my server' with spaced keys parsed correctly");
            Console.WriteLine();
        }

        /// <summary>
        /// 18. Section with no entries.
        /// </summary>
        static void Sample18_EmptySection()
        {
            Console.WriteLine("--- 18. Empty Section ---");

            var ini = IniParser.Parse(@"
[empty_section]
[other]
key=value
");
            Console.WriteLine("Section count: " + ini.SectionCount);
            Console.WriteLine("Contains 'empty_section': " + ini.ContainsSection("empty_section"));
            Console.WriteLine("empty_section entry count: " + ini["empty_section"].Count);
            Console.WriteLine("other entry count: " + ini["other"].Count);
            Console.WriteLine();
        }

        /// <summary>
        /// 19. Only comments, no actual data.
        /// </summary>
        static void Sample19_CommentsOnly()
        {
            Console.WriteLine("--- 19. Comments Only ---");

            var ini = IniParser.Parse(@"
; Just a comment
; Another comment
");
            Console.WriteLine("Section count: " + ini.SectionCount);
            Console.WriteLine("Global entry count: " + ini.Global.Count);
            Console.WriteLine("ToString: '" + ini.ToString().Trim() + "'");
            Console.WriteLine("(empty output — comments without following entries are discarded)");
            Console.WriteLine();
        }

        /// <summary>
        /// 20. Section merge — duplicate sections combine entries.
        /// </summary>
        static void Sample20_SectionMerge()
        {
            Console.WriteLine("--- 20. Section Merge ---");

            var ini = IniParser.Parse(@"
; comment block 1
[server]
host=first
; comment block 2
[server]
port=second
");
            Console.WriteLine("Section count: " + ini.SectionCount); // 1
            var section = ini["server"];
            Console.WriteLine("Entry count: " + section.Count);       // 2
            Console.WriteLine("host = " + section["host"]);
            Console.WriteLine("port = " + section["port"]);
            Console.WriteLine("Section comments: " + section.LeadingComments.Count);
            Console.WriteLine();
        }

        /// <summary>
        /// 21. IniValue with null and empty handling.
        /// </summary>
        static void Sample21_NullAndEmptyValues()
        {
            Console.WriteLine("--- 21. Null & Empty Values ---");

            var v1 = new IniValue(null);
            Console.WriteLine("null.HasValue = " + v1.HasValue);
            Console.WriteLine("null.Get<int>(42) = " + v1.Get<int>(42));
            Console.WriteLine("null.ToString() = '" + v1.ToString() + "'");

            var v2 = new IniValue("");
            Console.WriteLine("\nempty.HasValue = " + v2.HasValue);
            Console.WriteLine("empty.Value = '" + v2.Value + "'");

            var v3 = new IniValue();
            Console.WriteLine("\ndefault.HasValue = " + v3.HasValue);
            Console.WriteLine();
        }

        /// <summary>
        /// 22. Key without equals sign (treated as key with empty value).
        /// </summary>
        static void Sample22_KeyWithoutValue()
        {
            Console.WriteLine("--- 22. Key Without Value ---");

            var ini = IniParser.Parse(@"
flag_enabled
[server]
host=localhost
optional_setting
");
            Console.WriteLine("Global 'flag_enabled': '" + ini.Global["flag_enabled"] + "'");
            Console.WriteLine("Server 'optional_setting': '" + ini["server"]["optional_setting"] + "'");
            Console.WriteLine();
        }

        /// <summary>
        /// 23. Trailing comments (no following entry).
        /// </summary>
        static void Sample23_TrailingComments()
        {
            Console.WriteLine("--- 23. Trailing Comments ---");

            var ini = IniParser.Parse(@"
[server]
host=localhost
; this comment has no entry after it
");
            Console.WriteLine("Server entry count: " + ini["server"].Count);
            Console.WriteLine("host = " + ini["server"]["host"].Value);
            Console.WriteLine("(trailing comment discarded — no entry to attach to)");
            Console.WriteLine();
        }

        /// <summary>
        /// 24. Save to file and re-parse (file round trip).
        /// </summary>
        static void Sample24_FileRoundTrip()
        {
            Console.WriteLine("--- 24. File Round Trip ---");

            var original = @"# config
[app]
name=TestApp
port=9000
";
            var ini = IniParser.Parse(original);
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "easyini_sample.ini");
            ini.Save(path);

            var reRead = IniParser.ParseFile(path);
            Console.WriteLine("Re-read name: " + reRead["app"]["name"]);
            Console.WriteLine("Re-read port: " + reRead["app"]["port"]);

            // Clean up
            System.IO.File.Delete(path);
            Console.WriteLine("OK: round trip successful");
            Console.WriteLine();
        }

        /// <summary>
        /// 25. Encoding — save with specific encoding, re-read with same encoding.
        /// </summary>
        static void Sample25_EncodingSupport()
        {
            Console.WriteLine("--- 25. Encoding Support ---");

            var ini = new IniData();
            ini.AddOrGetSection("i18n").Set("greeting", "你好世界");

            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "easyini_utf8.ini");

            // Save with UTF-8
            ini.Save(path, Encoding.UTF8);

            // Re-read with explicit encoding
            var reRead = IniParser.ParseFile(path, Encoding.UTF8, null);
            Console.WriteLine("UTF-8 greeting: " + reRead["i18n"]["greeting"]);

            System.IO.File.Delete(path);
            Console.WriteLine("OK: encoding preserved");
            Console.WriteLine();
        }

        /// <summary>
        /// 26. Deeply nested comment blocks (multiple blank lines).
        /// </summary>
        static void Sample26_DeepCommentBlocks()
        {
            Console.WriteLine("--- 26. Deep Comment Blocks ---");

            var ini = IniParser.Parse(@"
; Header comment

; Spaced comment
key=value
");
            var entry = ini.Global.GetEntry("key");
            Console.WriteLine("Leading comments count: " + entry.LeadingComments.Count);
            Console.WriteLine("Comment[0]: '" + entry.LeadingComments[0] + "'");
            Console.WriteLine("Comment[1]: '" + entry.LeadingComments[1] + "'");
            Console.WriteLine("Comment[2]: '" + entry.LeadingComments[2] + "'");
            Console.WriteLine();
        }

        /// <summary>
        /// 27. Section-level inline comments.
        /// </summary>
        static void Sample27_SectionInlineComment()
        {
            Console.WriteLine("--- 27. Section Inline Comment ---");

            var ini = IniParser.Parse(@"
[server] ; main production server
host=localhost
");
            Console.WriteLine("Section: " + ini["server"].Name);
            Console.WriteLine("host: " + ini["server"]["host"].Value);
            Console.WriteLine("(section inline comment is currently not preserved)");
            Console.WriteLine();
        }

        private static void AssertSection(IniData ini, string section, string key, string expected)
        {
            if (ini[section][key].Value != expected)
                Console.WriteLine("FAIL: [" + section + "]." + key + " = " + ini[section][key].Value + ", expected " + expected);
        }
    }
}
