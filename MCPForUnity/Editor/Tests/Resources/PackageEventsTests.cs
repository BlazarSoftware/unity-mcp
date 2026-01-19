using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MCPForUnity.Editor.Resources.Packages;
using static MCPForUnityTests.Editor.TestUtilities;

namespace MCPForUnityTests.Editor.Resources
{
    public class PackageEventsTests
    {
        [Test]
        public void GetPackageStatus_ReturnsPackageInfo()
        {
            // Act
            var result = ToJObject(PackageEvents.HandleCommand(new JObject
            {
                ["action"] = "get_status"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["isListing"]);

            // If listing is complete, check packages
            if (!data.Value<bool>("isListing"))
            {
                Assert.IsNotNull(data["packages"]);
            }
        }

        [Test]
        public void GetOngoingOperations_ReturnsOperations()
        {
            // Act
            var result = ToJObject(PackageEvents.HandleCommand(new JObject
            {
                ["action"] = "get_operations"
            }));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
            var data = result["data"] as JObject;
            Assert.IsNotNull(data);
            Assert.IsNotNull(data["count"]);
            Assert.IsNotNull(data["operations"]);
            Assert.IsNotNull(data["hasOngoingOperations"]);
        }

        [Test]
        public void DefaultAction_ReturnsStatus()
        {
            // Act - no action specified, should default to get_status
            var result = ToJObject(PackageEvents.HandleCommand(new JObject()));

            // Assert
            Assert.IsTrue(result.Value<bool>("success"), result.ToString());
        }
    }
}
