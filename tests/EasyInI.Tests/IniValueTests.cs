using Xunit;

namespace EasyInI.Tests
{
    public class IniValueTests
    {
        [Fact]
        public void Get_Int()
        {
            var v = new IniValue("42");
            Assert.Equal(42, v.Get<int>());
        }

        [Fact]
        public void Get_Double()
        {
            var v = new IniValue("3.14");
            Assert.Equal(3.14, v.Get<double>(), 2);
        }

        [Fact]
        public void Get_Bool_True()
        {
            var v = new IniValue("true");
            Assert.True(v.Get<bool>());
        }

        [Fact]
        public void Get_Bool_False()
        {
            var v = new IniValue("false");
            Assert.False(v.Get<bool>());
        }

        [Fact]
        public void Get_DefaultOnFailure()
        {
            var v = new IniValue("not_a_number");
            Assert.Equal(100, v.Get<int>(100));
        }

        [Fact]
        public void Get_DefaultWhenNull()
        {
            var v = new IniValue(null);
            Assert.False(v.HasValue);
            Assert.Equal(10, v.Get<int>(10));
        }

        [Fact]
        public void ImplicitConversion_String()
        {
            IniValue v = new IniValue("hello");
            string s = v;
            Assert.Equal("hello", s);
        }

        [Fact]
        public void ImplicitConversion_FromString()
        {
            IniValue v = "world";
            Assert.Equal("world", v.Value);
        }

        [Fact]
        public void ImplicitConversion_Int()
        {
            IniValue v = new IniValue("42");
            int i = v;
            Assert.Equal(42, i);
        }

        [Fact]
        public void ImplicitConversion_Bool()
        {
            IniValue v = new IniValue("true");
            bool b = v;
            Assert.True(b);
        }
    }
}
