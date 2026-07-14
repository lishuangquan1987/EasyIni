using Xunit;

namespace EasyIni.Tests
{
    [IniSection("db")]
    public class DatabaseConfig
    {
        [IniProperty("host")]
        public string Host { get; set; }

        [IniProperty("port")]
        public int Port { get; set; }

        [IniProperty("pooling")]
        public bool Pooling { get; set; }
    }

    [IniSection("app")]
    public class MultiSectionConfig
    {
        [IniProperty("name", Section = "app")]
        public string AppName { get; set; }

        [IniProperty("host", Section = "server")]
        public string ServerHost { get; set; }

        [IniProperty("port", Section = "server")]
        public int ServerPort { get; set; }
    }

    public class NoAttributeConfig
    {
        [IniProperty("version")]
        public string Version { get; set; }

        [IniProperty("timeout")]
        public int Timeout { get; set; }
    }

    public class IniSerializerTests
    {
        [Fact]
        public void Deserialize_SingleSection()
        {
            var ini = IniParser.Parse(@"
[db]
host=localhost
port=5432
pooling=true
");
            var config = IniSerializer.Deserialize<DatabaseConfig>(ini);
            Assert.Equal("localhost", config.Host);
            Assert.Equal(5432, config.Port);
            Assert.True(config.Pooling);
        }

        [Fact]
        public void Deserialize_FromString()
        {
            var config = IniSerializer.Deserialize<DatabaseConfig>(@"
[db]
host=myhost
port=9999
pooling=false
");
            Assert.Equal("myhost", config.Host);
            Assert.Equal(9999, config.Port);
            Assert.False(config.Pooling);
        }

        [Fact]
        public void Deserialize_MultiSection()
        {
            var ini = IniParser.Parse(@"
[app]
name=MyApp
[server]
host=localhost
port=8080
");
            var config = IniSerializer.Deserialize<MultiSectionConfig>(ini);
            Assert.Equal("MyApp", config.AppName);
            Assert.Equal("localhost", config.ServerHost);
            Assert.Equal(8080, config.ServerPort);
        }

        [Fact]
        public void Deserialize_MissingKey_UsesDefault()
        {
            var ini = IniParser.Parse("[db]\nhost=localhost\n");
            var config = IniSerializer.Deserialize<DatabaseConfig>(ini);
            Assert.Equal("localhost", config.Host);
            Assert.Equal(0, config.Port);
            Assert.False(config.Pooling);
        }

        [Fact]
        public void Deserialize_NoSectionAttribute_UsesClassName()
        {
            var ini = IniParser.Parse(@"
[NoAttributeConfig]
version=2.0
timeout=30
");
            var config = IniSerializer.Deserialize<NoAttributeConfig>(ini);
            Assert.Equal("2.0", config.Version);
            Assert.Equal(30, config.Timeout);
        }

        [Fact]
        public void Serialize_SingleSection()
        {
            var config = new DatabaseConfig
            {
                Host = "prod-server",
                Port = 3306,
                Pooling = true
            };
            var ini = IniSerializer.Serialize(config);
            Assert.Equal("prod-server", ini["db"]["host"].Value);
            Assert.Equal("3306", ini["db"]["port"].Value);
            Assert.Equal("True", ini["db"]["pooling"].Value);
        }

        [Fact]
        public void SerializeToString_RoundTrip()
        {
            var config = new DatabaseConfig
            {
                Host = "test",
                Port = 1234,
                Pooling = false
            };
            var text = IniSerializer.SerializeToString(config);
            var restored = IniSerializer.Deserialize<DatabaseConfig>(text);
            Assert.Equal("test", restored.Host);
            Assert.Equal(1234, restored.Port);
            Assert.False(restored.Pooling);
        }

        [Fact]
        public void Serialize_MultiSection()
        {
            var config = new MultiSectionConfig
            {
                AppName = "TestApp",
                ServerHost = "127.0.0.1",
                ServerPort = 9000
            };
            var ini = IniSerializer.Serialize(config);
            Assert.Equal("TestApp", ini["app"]["name"].Value);
            Assert.Equal("127.0.0.1", ini["server"]["host"].Value);
            Assert.Equal("9000", ini["server"]["port"].Value);
        }

        [Fact]
        public void Deserialize_PartialMapping_IgnoresExtraKeys()
        {
            var ini = IniParser.Parse(@"
[db]
host=localhost
port=5432
pooling=true
extra_key=ignored
");
            var config = IniSerializer.Deserialize<DatabaseConfig>(ini);
            // Should not throw, extra keys are ignored
            Assert.Equal("localhost", config.Host);
            Assert.Equal(5432, config.Port);
        }
    }
}
