using System.Collections.Generic;
using NUnit.Framework;
using SFE.SimCore.Data;

namespace SFE.SimCore.Tests
{
    public class MiniJsonTests
    {
        [Test]
        public void Parses_NestedObjectsArraysAndEscapes()
        {
            var o = MiniJson.ParseObject("{\"a\": [1, -2.5e1, true, null], \"b\": {\"s\": \"Gr\\u00fc\\u00dfe \\\"x\\\"\"}}");
            var a = o.Arr("a");
            Assert.AreEqual(1.0, a[0]);
            Assert.AreEqual(-25.0, a[1]);
            Assert.AreEqual(true, a[2]);
            Assert.IsNull(a[3]);
            Assert.AreEqual("Grüße \"x\"", o.Obj("b").Str("s"));
        }

        [Test]
        public void MissingField_ThrowsWithFieldName()
        {
            var o = MiniJson.ParseObject("{}");
            var ex = Assert.Throws<KeyNotFoundException>(() => o.Num("budget"));
            StringAssert.Contains("budget", ex.Message);
        }
    }
}
