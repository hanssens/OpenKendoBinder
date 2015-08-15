using System.Collections.Generic;
using NUnit.Framework;
using OpenKendoBinder.Containers.Json;
using OpenKendoBinder.ModelBinder;

namespace OpenKendoBinder.UnitTests
{
    [TestFixture]
    class AggregateHelperUnitTests
    {
        [Test]
        public void AggregateHelper_TestMapNull()
        {
            IEnumerable<AggregateObject> objects = AggregateHelper.Map(null);
            Assert.IsNull(objects);
        }
    }
}